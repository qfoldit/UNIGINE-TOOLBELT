// qFoldIT Toolbelt for UNIGINE 2 — UnigineCompat.cs
//
// ⚠ READ THIS BEFORE WIRING TOOLS AGAINST YOUR SDK VERSION ⚠
//
// UNIGINE's C# API (UnigineSharp) has real differences between SDK versions
// — property vs. method transform access, exact primitive mesh asset paths
// shipped in core content, and material assignment calls have all changed
// across 2.x releases. This file is deliberately the ONLY place that talks
// directly to Unigine.* types for node/material/transform operations, so
// that adapting the whole toolbelt to your exact installed SDK version is a
// one-file change instead of hunting through nine Tools/*.cs files.
//
// Verify every call below against Help → API Documentation inside your
// UNIGINE 2 SDK Browser (or https://developer.unigine.com/docs/) for your
// installed version (2.20 / 2.21 as referenced by MCPBridge Plugin) before
// relying on this in a real project. The method signatures here reflect the
// commonly documented UnigineSharp shape as of the 2.20/2.21 line; adjust
// as needed.

using System;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class UnigineCompat
    {
        /// <summary>
        /// Core-content primitive mesh paths. UNIGINE ships primitive meshes
        /// under core/meshes/ in the default content pack — confirm these
        /// exact filenames exist in your project's mounted core content;
        /// some SDK versions use "_lod0" suffixes or a "primitives/" subfolder.
        /// </summary>
        public static readonly System.Collections.Generic.Dictionary<string, string> PrimitiveMeshPaths =
            new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "box",      "core/meshes/box.mesh" },
            { "sphere",   "core/meshes/sphere.mesh" },
            { "cylinder", "core/meshes/cylinder.mesh" },
            { "capsule",  "core/meshes/capsule.mesh" },
            { "plane",    "core/meshes/plane.mesh" },
            { "cone",     "core/meshes/cone.mesh" },
            { "torus",    "core/meshes/torus.mesh" },
        };

        public static Node CreatePrimitive(string primitiveType, double x, double y, double z, float scale = 1f, string name = null)
        {
            if (!PrimitiveMeshPaths.TryGetValue(primitiveType, out var meshPath))
                throw new ArgumentException($"Unknown primitive type '{primitiveType}'. Valid: {string.Join(", ", PrimitiveMeshPaths.Keys)}");

            var mesh = new Mesh();
            mesh.Load(meshPath);
            var obj = new ObjectMeshStatic(mesh);

            SetWorldPosition(obj, x, y, z);
            SetUniformScale(obj, scale);

            if (!string.IsNullOrEmpty(name))
                obj.Name = name;

            return obj;
        }

        public static void SetWorldPosition(Node node, double x, double y, double z)
        {
            // World coordinates in UNIGINE use double precision (dvec3).
            node.SetWorldTransform(Unigine.Math.dmat4.Translate(new Unigine.Math.dvec3(x, y, z)));
        }

        public static Unigine.Math.dvec3 GetWorldPosition(Node node)
        {
            return node.WorldPosition;
        }

        /// <summary>
        /// Sets position, rotation, and scale in ONE atomic operation via
        /// dmat4.Compose. This is the only correct way to set more than one
        /// of these three components on the same node: composing them
        /// sequentially via separate SetWorldTransform calls (the previous
        /// implementation of this file) is a real bug, not just a style
        /// choice — e.g. calling "set position (10,0,0)" followed by "set
        /// scale 2" via left-multiplying a Scale matrix onto the existing
        /// WorldTransform also scales the translation component, moving
        /// the node to (20,0,0) instead of (10,0,0) whenever position is
        /// non-origin. Always call this instead of chaining
        /// SetWorldPosition/SetEulerRotation/SetUniformScale on a node that
        /// already has a non-identity transform.
        /// </summary>
        public static void SetTransform(Node node, double x, double y, double z,
            float pitchDeg, float yawDeg, float rollDeg,
            float scaleX, float scaleY, float scaleZ)
        {
            var pos = new Unigine.Math.dvec3(x, y, z);
            var rot = Unigine.Math.quat.Euler(pitchDeg, yawDeg, rollDeg);
            var scale = new Unigine.Math.vec3(scaleX, scaleY, scaleZ);
            node.SetWorldTransform(Unigine.Math.dmat4.Compose(pos, rot, scale));
        }

        /// <summary>
        /// Safe ONLY on a freshly created node whose transform is still
        /// identity (rotation=0, scale=1) — e.g. immediately after `new
        /// ObjectMeshStatic(...)`/`new NodeDummy()`, before anything else
        /// has touched its transform. For any node that might already have
        /// a rotation or non-uniform scale applied, use SetTransform
        /// instead so those components aren't silently reset.
        /// </summary>
        public static void SetUniformScale(Node node, float scale)
        {
            var pos = node.WorldPosition;
            node.SetWorldTransform(Unigine.Math.dmat4.Compose(pos, Unigine.Math.quat.IDENTITY, new Unigine.Math.vec3(scale, scale, scale)));
        }

        /// <summary>
        /// Same non-destructive-only-on-fresh-nodes caveat as SetUniformScale.
        /// </summary>
        public static void SetEulerRotation(Node node, float pitchDeg, float yawDeg, float rollDeg)
        {
            var pos = node.WorldPosition;
            var rot = Unigine.Math.quat.Euler(pitchDeg, yawDeg, rollDeg);
            node.SetWorldTransform(Unigine.Math.dmat4.Compose(pos, rot, new Unigine.Math.vec3(1, 1, 1)));
        }

        public static void ApplyMaterialColor(ObjectMeshStatic obj, int surfaceIndex, float r, float g, float b, float a, bool emissive, float emissiveStrength)
        {
            var baseMaterial = Materials.FindMaterial("mesh_base");
            if (baseMaterial == null)
            {
                Log.Warning("[qFoldIT Toolbelt] 'mesh_base' material not found — check your material library name for this SDK version.\n");
                return;
            }
            var instance = baseMaterial.Inherit();
            instance.SetParameterFloat4("albedo_color", new Unigine.Math.vec4(r, g, b, a));
            if (emissive)
            {
                instance.SetParameterFloat4("emission_color", new Unigine.Math.vec4(r, g, b, 1f));
                instance.SetParameterFloat("emission_scale", emissiveStrength);
            }
            obj.SetMaterial(instance, surfaceIndex);
        }

        public static Node FindNodeByName(string name)
        {
            // World.GetNodeByName / a full-tree search — exact call name varies
            // by SDK version; some expose World.GetNode(int id) plus a name
            // index, others a direct name lookup. Adjust to match yours.
            return World.GetNodeByName(name);
        }

        public static Node[] GetAllWorldNodes()
        {
            int count = World.GetNumChilds();
            var result = new System.Collections.Generic.List<Node>();
            void Walk(Node n)
            {
                result.Add(n);
                for (int i = 0; i < n.NumChilds; i++)
                    Walk(n.GetChild(i));
            }
            for (int i = 0; i < count; i++)
                Walk(World.GetChild(i));
            return result.ToArray();
        }

        public static Node CreateLight(string lightType, double x, double y, double z, float r, float g, float b, float intensity, string name = null)
        {
            // Unigine has distinct light node classes rather than one Light
            // type with an enum, unlike Unity. Map the requested type to the
            // matching class. Verify exact constructor signatures for your
            // SDK version (some take a radius/cutoff argument directly).
            Node light = lightType.ToLowerInvariant() switch
            {
                "directional" or "world" or "sun" => new LightWorld(),
                "point" or "omni" => new LightOmni(1.0f),
                "spot" => new LightProj(new Unigine.Math.mat4(), Materials.FindMaterial("light_proj")),
                _ => new LightOmni(1.0f),
            };

            SetWorldPosition(light, x, y, z);
            if (light is LightOmni lo) { lo.Color = new Unigine.Math.vec4(r, g, b, 1f); }
            if (light is LightWorld lw) { lw.Color = new Unigine.Math.vec4(r, g, b, 1f); }
            if (!string.IsNullOrEmpty(name)) light.Name = name;

            World.AddChild(light);
            return light;
        }

        /// <summary>
        /// Lightweight persisted key/value store used by TagsLayersTools and
        /// StampTools for data UNIGINE has no first-class manager for
        /// (arbitrary string tags, in particular). Backed by a JSON file
        /// under Saved/QFoldIT_Toolbelt/ so it survives Editor restarts.
        /// </summary>
        public static string SavedDataDir =>
            System.IO.Path.Combine(Engine.Get().GetSourceDataPath() ?? ".", "Saved", "QFoldIT_Toolbelt");
    }
}

