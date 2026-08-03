// qFoldIT Toolbelt for UNIGINE 2 — ToolbeltBootstrap.cs
//
// Wire this in as a WorldLogic (add it as a world script / autostart
// component) so the listener starts with the Editor and gets pumped every
// frame. If you'd rather run it as a pure Editor extension outside any
// specific world, move Init()/Update()/Shutdown() into your Editor plugin's
// equivalent lifecycle hooks — the ToolbeltListener class itself has no
// WorldLogic dependency.

using Unigine;

namespace QFoldIT.Toolbelt
{
    public class ToolbeltBootstrap : WorldLogic
    {
        private ToolbeltListener _listener;

        public override int Init()
        {
            ToolRegistry.RegisterAll();

            int port = 8766;
            var envPort = System.Environment.GetEnvironmentVariable("UNIGINE_TOOLBELT_PORT");
            if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out var parsed))
                port = parsed;

            _listener = new ToolbeltListener(port);
            _listener.Start();

            Log.Message($"[qFoldIT Toolbelt] Ready — {ToolRegistry.ListTools().Count} tools registered, listening on 127.0.0.1:{port}.\n");
            return 1;
        }

        public override int Update()
        {
            _listener?.PumpMainThread();
            return 1;
        }

        public override int Shutdown()
        {
            _listener?.Stop();
            return 1;
        }
    }
}
