// qFoldIT Toolbelt for UNIGINE 2 — ToolbeltListener.cs
//
// In-editor HTTP listener that mcp_server.py (the external MCP bridge)
// relays calls to. Runs a background HttpListener thread and marshals
// every request onto the Editor's main update thread before touching any
// Unigine World/Node/Material API, since those are not guaranteed
// thread-safe off the main loop.
//
// Load this as an Editor plugin / autostart WorldLogic component so it is
// listening before mcp_server.py's first request arrives.
//
// NOTE ON API SURFACE: this file uses only System.Net / System.Threading —
// no Unigine-specific calls — so it should compile against any recent
// UNIGINE 2 C# SDK without adjustment. The Unigine-specific tool
// implementations live in Tools/*.cs and dispatch through ToolRegistry.

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QFoldIT.Toolbelt
{
    /// <summary>
    /// One HTTP request waiting to be executed on the main thread.
    /// </summary>
    internal class PendingRequest
    {
        public string Endpoint;
        public JObject Body;
        public ManualResetEventSlim Done = new ManualResetEventSlim(false);
        public string ResponseJson;
    }

    public class ToolbeltListener
    {
        private const int LogRingCapacity = 500;

        private HttpListener _listener;
        private Thread _httpThread;
        private readonly ConcurrentQueue<PendingRequest> _pending = new ConcurrentQueue<PendingRequest>();
        private readonly ConcurrentQueue<string> _logRing = new ConcurrentQueue<string>();
        private volatile bool _running;

        public int Port { get; }

        public ToolbeltListener(int port = 8766)
        {
            Port = port;
        }

        public void Start()
        {
            if (_running) return;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
            _running = true;

            _httpThread = new Thread(HttpLoop) { IsBackground = true, Name = "QFoldIT.Toolbelt.HttpListener" };
            _httpThread.Start();

            Log($"qFoldIT Toolbelt listener started on 127.0.0.1:{Port}");
        }

        public void Stop()
        {
            _running = false;
            try { _listener?.Stop(); _listener?.Close(); } catch { /* best-effort shutdown */ }
            Log("qFoldIT Toolbelt listener stopped.");
        }

        private void HttpLoop()
        {
            while (_running)
            {
                HttpListenerContext ctx;
                try { ctx = _listener.GetContext(); }
                catch (Exception) { if (!_running) return; else continue; }

                try
                {
                    string endpoint = ctx.Request.Url.AbsolutePath.Trim('/');
                    string bodyText;
                    using (var reader = new System.IO.StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                        bodyText = reader.ReadToEnd();

                    JObject body = string.IsNullOrWhiteSpace(bodyText) ? new JObject() : JObject.Parse(bodyText);

                    var req = new PendingRequest { Endpoint = endpoint, Body = body };
                    _pending.Enqueue(req);
                    req.Done.Wait(TimeSpan.FromSeconds(20));

                    string responseJson = req.ResponseJson ??
                        JsonConvert.SerializeObject(new { success = false, error = "Timed out waiting for main-thread dispatch." });

                    var buffer = Encoding.UTF8.GetBytes(responseJson);
                    ctx.Response.ContentType = "application/json";
                    ctx.Response.ContentLength64 = buffer.Length;
                    ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    ctx.Response.OutputStream.Close();
                }
                catch (Exception ex)
                {
                    Log($"[ERROR] HTTP handling failed: {ex.Message}");
                    try { ctx.Response.StatusCode = 500; ctx.Response.OutputStream.Close(); } catch { }
                }
            }
        }

        /// <summary>
        /// Call once per frame from your WorldLogic.Update() (or Editor update
        /// callback). Drains queued HTTP requests and executes them safely on
        /// the main thread via ToolRegistry.Dispatch.
        /// </summary>
        public void PumpMainThread()
        {
            while (_pending.TryDequeue(out var req))
            {
                try
                {
                    switch (req.Endpoint)
                    {
                        case "run_tool":
                            {
                                string toolName = (string)req.Body["tool"];
                                JObject toolParams = (JObject)(req.Body["params"] ?? new JObject());
                                object result = ToolRegistry.Dispatch(toolName, toolParams);
                                req.ResponseJson = JsonConvert.SerializeObject(result);
                                break;
                            }
                        case "list_tools":
                            req.ResponseJson = JsonConvert.SerializeObject(new { success = true, tools = ToolRegistry.ListTools() });
                            break;
                        case "get_log":
                            int maxLines = (int?)req.Body["max_lines"] ?? 100;
                            req.ResponseJson = JsonConvert.SerializeObject(new { success = true, lines = GetLogTail(maxLines) });
                            break;
                        default:
                            req.ResponseJson = JsonConvert.SerializeObject(new { success = false, error = $"Unknown endpoint '{req.Endpoint}'." });
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log($"[ERROR] Tool dispatch failed: {ex.Message}");
                    req.ResponseJson = JsonConvert.SerializeObject(new { success = false, error = ex.Message });
                }
                finally
                {
                    req.Done.Set();
                }
            }
        }

        private void Log(string message)
        {
            _logRing.Enqueue($"[{DateTime.UtcNow:HH:mm:ss}] {message}");
            while (_logRing.Count > LogRingCapacity) _logRing.TryDequeue(out _);
        }

        private string[] GetLogTail(int maxLines)
        {
            var arr = _logRing.ToArray();
            int start = Math.Max(0, arr.Length - maxLines);
            var slice = new string[arr.Length - start];
            Array.Copy(arr, start, slice, 0, slice.Length);
            return slice;
        }
    }
}
