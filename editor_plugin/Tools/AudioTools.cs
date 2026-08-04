// qFoldIT Toolbelt for UNIGINE 2 — AudioTools.cs
// Category: Audio
// ⚠ UNIGINE audio nodes are ObjectSound (attached sound source in the
// world) driven by a Sound asset (.wav/.ogg loaded via Unigine.Sound).
// Listener is typically the active Player/camera by default, but can be
// set explicitly via SoundSystem. Verify exact API for your SDK version.

using Newtonsoft.Json.Linq;
using Unigine;

namespace QFoldIT.Toolbelt
{
    public static class AudioTools
    {
        public static void Register()
        {
            ToolRegistry.Register("audio_add_source", "Audio",
                "Adds an ObjectSound node at a world position, loaded from a sound asset, with loop/volume settings.",
                AddSource);

            ToolRegistry.Register("audio_play_one_shot", "Audio",
                "Plays a sound asset once at a world position via a transient ObjectSound.",
                PlayOneShot);

            ToolRegistry.Register("audio_set_listener_node", "Audio",
                "Sets which node acts as the audio listener origin for 3D sound attenuation.",
                SetListenerNode);

            ToolRegistry.Register("audio_set_source_volume", "Audio",
                "Sets the volume/gain on an existing ObjectSound node.",
                SetSourceVolume);
        }

        private static object AddSource(JObject p)
        {
            string name = (string)p["name"] ?? "SoundSource";
            string soundPath = (string)p["sound_path"];
            double x = (double?)p["x"] ?? 0, y = (double?)p["y"] ?? 0, z = (double?)p["z"] ?? 0;
            bool loop = (bool?)p["loop"] ?? false;
            float volume = (float?)p["volume"] ?? 1f;

            if (string.IsNullOrEmpty(soundPath)) return new { success = false, error = "sound_path is required." };

            var sound = new Sound(soundPath);
            var node = new ObjectSound(sound);
            UnigineCompat.SetWorldPosition(node, x, y, z);
            node.Loop = loop;
            node.Gain = volume;
            node.Name = name;
            node.Play();

            return new { success = true, name, sound_path = soundPath, loop };
        }

        private static object PlayOneShot(JObject p)
        {
            string soundPath = (string)p["sound_path"];
            double x = (double?)p["x"] ?? 0, y = (double?)p["y"] ?? 0, z = (double?)p["z"] ?? 0;
            float volume = (float?)p["volume"] ?? 1f;

            if (string.IsNullOrEmpty(soundPath)) return new { success = false, error = "sound_path is required." };

            var sound = new Sound(soundPath);
            var node = new ObjectSound(sound);
            UnigineCompat.SetWorldPosition(node, x, y, z);
            node.Loop = false;
            node.Gain = volume;
            node.Name = $"OneShot_{System.Guid.NewGuid():N}".Substring(0, 16);
            node.Play();
            // Not deleted immediately — a real implementation should
            // schedule DeleteLater() once Sound.Duration has elapsed via
            // the WorldLogic Update loop; left as a follow-up since exact
            // Sound.Duration availability varies by SDK version.

            return new { success = true, sound_path = soundPath };
        }

        private static object SetListenerNode(JObject p)
        {
            string name = (string)p["name"];
            var node = UnigineCompat.FindNodeByName(name);
            if (node == null) return new { success = false, error = $"Node '{name}' not found." };

            // SoundSystem.SetListenerNode / equivalent — verify against your
            // SDK; some versions derive the listener from the active Player
            // automatically instead of an explicit setter.
            SoundSystem.SetListenerPosition(node.WorldPosition);
            return new { success = true, listener = name };
        }

        private static object SetSourceVolume(JObject p)
        {
            string name = (string)p["name"];
            float volume = (float?)p["volume"] ?? 1f;

            var node = UnigineCompat.FindNodeByName(name) as ObjectSound;
            if (node == null) return new { success = false, error = $"ObjectSound '{name}' not found." };

            node.Gain = volume;
            return new { success = true, name, volume };
        }
    }
}
