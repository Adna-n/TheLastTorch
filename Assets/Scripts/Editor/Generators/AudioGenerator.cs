// AudioGenerator: synthesizes every sound effect and loop from maths (sine waves, filtered noise,
// pitch sweeps, envelopes) and writes them as 16-bit mono 44.1 kHz WAV files in Assets/Generated/Audio.
// Loops are made seamless by cross-fading their tail into their start. Fixed random seeds keep every
// run identical. BuildSoundList() hands the clips to the AudioManager in each scene.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace LastTorch.EditorTools
{
    public static class AudioGenerator
    {
        public const string Folder = "Assets/Generated/Audio";
        const int SR = 44100;
        const float TwoPi = Mathf.PI * 2f;

        public static void GenerateAll()
        {
            TorchLoop();
            AmbientDrone();
            for (int i = 1; i <= 4; i++) Footstep(i);
            PickupGem();
            PickupOil();
            PickupKey();
            DoorGrind();
            DoorSlam();
            SpikeUp();
            DartWhoosh();
            BoulderRoll();
            Hurt();
            HeartHum();
            CollapseRumble();
            LowFuelBeat();
            WinJingle();
            LoseSting();
            UiClick();
            UiHover();
        }

        public static string PathOf(string name) => $"{Folder}/{AssetCategories.Audio(name)}/{name}.wav";
        public static AudioClip Load(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(PathOf(name));

        /// <summary>The SoundId -> clip table given to every scene's AudioManager.</summary>
        public static List<SoundEntry> BuildSoundList()
        {
            var list = new List<SoundEntry>();
            void Add(SoundId id, float volume, params string[] names)
            {
                var clips = new List<AudioClip>();
                foreach (var n in names) { var c = Load(n); if (c != null) clips.Add(c); }
                list.Add(new SoundEntry { id = id, clips = clips.ToArray(), volume = volume });
            }
            Add(SoundId.TorchLoop, 0.5f, "torch_loop");
            Add(SoundId.AmbientDrone, 0.8f, "ambient_drone");
            Add(SoundId.Footstep, 0.6f, "footstep_1", "footstep_2", "footstep_3", "footstep_4");
            Add(SoundId.PickupGem, 0.7f, "pickup_gem");
            Add(SoundId.PickupOil, 0.8f, "pickup_oil");
            Add(SoundId.PickupKey, 0.8f, "pickup_key");
            Add(SoundId.DoorGrind, 0.8f, "door_grind");
            Add(SoundId.DoorSlam, 1f, "door_slam");
            Add(SoundId.SpikeUp, 0.6f, "spike_up");
            Add(SoundId.DartWhoosh, 0.7f, "dart_whoosh");
            Add(SoundId.BoulderRoll, 1f, "boulder_roll");
            Add(SoundId.Hurt, 0.8f, "hurt");
            Add(SoundId.HeartHum, 0.6f, "heart_hum");
            Add(SoundId.CollapseRumble, 0.9f, "collapse_rumble");
            Add(SoundId.LowFuelBeat, 0.8f, "low_fuel_beat");
            Add(SoundId.WinJingle, 0.9f, "win_jingle");
            Add(SoundId.LoseSting, 0.9f, "lose_sting");
            Add(SoundId.UiClick, 0.6f, "ui_click");
            Add(SoundId.UiHover, 0.35f, "ui_hover");
            return list;
        }

        // ------------------------------------------------------------------ DSP helpers

        static float Noise(Random r) => (float)(r.NextDouble() * 2.0 - 1.0);
        static float Rand(Random r, float min, float max) => min + (float)r.NextDouble() * (max - min);
        static int Len(float seconds) => Mathf.RoundToInt(seconds * SR);

        /// <summary>One-pole low-pass filter (removes high frequencies above ~cutoff).</summary>
        class LowPass
        {
            float y;
            readonly float a;
            public LowPass(float cutoff) { a = 1f - Mathf.Exp(-TwoPi * cutoff / SR); }
            public float Process(float x) { y += a * (x - y); return y; }
        }

        /// <summary>State-variable band-pass filter with a movable centre frequency.</summary>
        class BandPass
        {
            float low, band, f;
            readonly float q;
            public BandPass(float centre, float q) { this.q = q; SetCentre(centre); }
            public void SetCentre(float centre) { f = 2f * Mathf.Sin(Mathf.PI * Mathf.Min(centre, SR / 6f) / SR); }
            public float Process(float x)
            {
                low += f * band;
                float high = x - low - q * band;
                band += f * high;
                return band;
            }
        }

        /// <summary>Makes a seamless loop: the extra samples after loopLength are faded into the start.</summary>
        static float[] Loop(float[] generated, int loopLength)
        {
            int fade = generated.Length - loopLength;
            var result = new float[loopLength];
            for (int i = 0; i < loopLength; i++)
            {
                if (i < fade)
                {
                    float t = i / (float)fade;
                    result[i] = generated[i] * t + generated[loopLength + i] * (1f - t);
                }
                else result[i] = generated[i];
            }
            return result;
        }

        /// <summary>Adds a decaying sine "thump" with a falling pitch.</summary>
        static void AddThump(float[] buf, int start, float freqStart, float freqEnd, float decay, float amp, bool wrap = false)
        {
            float phase = 0f;
            int len = Len(decay * 6f);
            for (int s = 0; s < len; s++)
            {
                int idx = start + s;
                if (wrap) idx %= buf.Length;
                else if (idx >= buf.Length) break;
                float t = s / (float)SR;
                float f = freqEnd + (freqStart - freqEnd) * Mathf.Exp(-t / 0.04f);
                phase += TwoPi * f / SR;
                float attack = Mathf.Clamp01(t / 0.003f);
                buf[idx] += Mathf.Sin(phase) * Mathf.Exp(-t / decay) * attack * amp;
            }
        }

        /// <summary>Adds a short decaying sine note with a couple of harmonics.</summary>
        static void AddNote(float[] buf, int start, float freq, float decay, float amp, float h2 = 0.3f, float h3 = 0.1f, float attack = 0.005f)
        {
            int len = Len(decay * 6f);
            for (int s = 0; s < len; s++)
            {
                int idx = start + s;
                if (idx >= buf.Length) break;
                float t = s / (float)SR;
                float env = Mathf.Exp(-t / decay) * Mathf.Clamp01(t / attack);
                float w = TwoPi * freq * t;
                buf[idx] += (Mathf.Sin(w) + h2 * Mathf.Sin(2f * w) + h3 * Mathf.Sin(3f * w)) * env * amp;
            }
        }

        static void Save(string name, float[] samples, float peak = 0.9f)
        {
            float max = 0.0001f;
            foreach (float s in samples) max = Mathf.Max(max, Mathf.Abs(s));
            float gain = peak / max;

            EditorUtil.EnsureFolder($"{Folder}/{AssetCategories.Audio(name)}");
            string assetPath = PathOf(name);
            string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);

            using (var fs = new FileStream(fullPath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int dataBytes = samples.Length * 2;
                bw.Write(new[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + dataBytes);
                bw.Write(new[] { 'W', 'A', 'V', 'E' });
                bw.Write(new[] { 'f', 'm', 't', ' ' });
                bw.Write(16);            // fmt chunk size
                bw.Write((short)1);      // PCM
                bw.Write((short)1);      // mono
                bw.Write(SR);
                bw.Write(SR * 2);        // byte rate
                bw.Write((short)2);      // block align
                bw.Write((short)16);     // bits per sample
                bw.Write(new[] { 'd', 'a', 't', 'a' });
                bw.Write(dataBytes);
                foreach (float s in samples)
                    bw.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * gain * 32767f), -32768, 32767));
            }
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        // ------------------------------------------------------------------ sounds

        public static void TorchLoop()
        {
            var r = new Random(11);
            int loop = Len(4f);
            var g = new float[loop + Len(0.25f)];
            var lp = new LowPass(700f);
            var lp2 = new LowPass(220f);
            for (int i = 0; i < g.Length; i++)
            {
                float n = Noise(r);
                g[i] = lp.Process(n) * 0.5f + lp2.Process(n) * 0.9f;
            }
            for (int k = 0; k < 90; k++) // crackle pops
            {
                int start = r.Next(g.Length);
                int len = r.Next(60, 500);
                float amp = Rand(r, 0.2f, 1f);
                float prev = 0f;
                for (int s = 0; s < len && start + s < g.Length; s++)
                {
                    float x = Noise(r);
                    g[start + s] += (x - prev) * amp * Mathf.Exp(-s / (len * 0.25f));
                    prev = x;
                }
            }
            Save("torch_loop", Loop(g, loop), 0.8f);
        }

        public static void AmbientDrone()
        {
            var r = new Random(22);
            const float seconds = 20f;
            int loop = Len(seconds);
            var g = new float[loop + Len(0.5f)];
            var air = new LowPass(400f);
            for (int i = 0; i < g.Length; i++)
            {
                float t = i / (float)SR;
                g[i] = 0.35f * Mathf.Sin(TwoPi * 55f * t) * (0.7f + 0.3f * Mathf.Sin(TwoPi * t / seconds))
                     + 0.25f * Mathf.Sin(TwoPi * 55.25f * t)
                     + 0.22f * Mathf.Sin(TwoPi * 82.5f * t) * (0.6f + 0.4f * Mathf.Sin(TwoPi * 2f * t / seconds + 1f))
                     + 0.15f * Mathf.Sin(TwoPi * 110f * t) * (0.5f + 0.5f * Mathf.Sin(TwoPi * 3f * t / seconds + 2f))
                     + 0.08f * air.Process(Noise(r));
            }
            for (int k = 0; k < 9; k++) // water drips with echoes
            {
                int start = r.Next(loop);
                float f0 = Rand(r, 900f, 1500f);
                float amp = Rand(r, 0.25f, 0.5f);
                float[] echoTimes = { 0f, 0.11f, 0.23f, 0.37f };
                float[] echoAmps = { 1f, 0.4f, 0.2f, 0.1f };
                for (int e = 0; e < echoTimes.Length; e++)
                {
                    int s0 = start + Len(echoTimes[e]);
                    float phase = 0f;
                    int len = Len(0.07f);
                    for (int s = 0; s < len; s++)
                    {
                        float t = s / (float)SR;
                        phase += TwoPi * f0 * (1f + 1.5f * s / len) / SR;
                        g[(s0 + s) % loop] += Mathf.Sin(phase) * Mathf.Exp(-t / 0.012f) * amp * echoAmps[e];
                    }
                }
            }
            Save("ambient_drone", Loop(g, loop), 0.7f);
        }

        public static void Footstep(int variant)
        {
            var r = new Random(30 + variant);
            var g = new float[Len(0.22f)];
            var lp = new LowPass(300f + variant * 70f);
            float prev = 0f;
            for (int i = 0; i < g.Length; i++)
            {
                float t = i / (float)SR;
                float n = Noise(r);
                float env = Mathf.Clamp01(t / 0.003f) * Mathf.Exp(-t / 0.035f);
                g[i] = lp.Process(n) * env * 2.5f + (n - prev) * Mathf.Exp(-t / 0.015f) * 0.12f;
                prev = n;
            }
            AddThump(g, 0, 120f + variant * 10f, 65f + variant * 6f, 0.03f, 0.5f);
            Save("footstep_" + variant, g, 0.8f);
        }

        public static void PickupGem()
        {
            var g = new float[Len(0.6f)];
            float[] notes = { 1046.5f, 1318.5f, 1568f, 2093f };
            for (int i = 0; i < notes.Length; i++) AddNote(g, Len(i * 0.06f), notes[i], 0.09f, 1f, 0.3f, 0.05f);
            Save("pickup_gem", g);
        }

        public static void PickupOil()
        {
            var r = new Random(44);
            var g = new float[Len(0.65f)];
            float[] starts = { 0f, 0.15f, 0.31f };
            for (int k = 0; k < starts.Length; k++)
            {
                int s0 = Len(starts[k]);
                int len = Len(0.15f);
                float phase = 0f;
                float f0 = 260f - k * 25f;
                for (int s = 0; s < len && s0 + s < g.Length; s++)
                {
                    float t = s / (float)SR;
                    float f = 100f + (f0 - 100f) * Mathf.Exp(-t / 0.05f) + 40f * Mathf.Sin(TwoPi * 30f * t);
                    phase += TwoPi * f / SR;
                    float env = Mathf.Sin(Mathf.PI * s / len);
                    g[s0 + s] += Mathf.Sin(phase) * env * (1f - k * 0.2f);
                }
            }
            var lp = new LowPass(500f);
            for (int i = 0; i < g.Length; i++) g[i] += lp.Process(Noise(r)) * 0.15f;
            Save("pickup_oil", g, 0.85f);
        }

        public static void PickupKey()
        {
            var g = new float[Len(1.8f)];
            float[] chord = { 523.25f, 659.25f, 783.99f };
            float[] ratios = { 1f, 2f, 2.76f, 5.4f };
            float[] amps = { 1f, 0.5f, 0.35f, 0.15f };
            float[] decays = { 0.6f, 0.4f, 0.25f, 0.12f };
            for (int i = 0; i < g.Length; i++)
            {
                float t = i / (float)SR;
                float attack = Mathf.Clamp01(t / 0.002f);
                float sum = 0f;
                foreach (float f in chord)
                    for (int p = 0; p < ratios.Length; p++)
                        sum += Mathf.Sin(TwoPi * f * ratios[p] * t) * amps[p] * Mathf.Exp(-t / decays[p]);
                g[i] = sum * attack;
            }
            Save("pickup_key", g, 0.8f);
        }

        public static void DoorGrind()
        {
            var r = new Random(55);
            float seconds = 2.2f;
            int len = Len(seconds);
            var brown = new float[Len(seconds * 1.6f)];
            float b = 0f;
            for (int i = 0; i < brown.Length; i++) { b = (b + Noise(r) * 0.02f) * 0.998f; brown[i] = b; }

            var g = new float[len];
            var band = new BandPass(300f, 0.5f);
            float pos = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SR;
                pos += 1f + 0.35f * Mathf.Sin(TwoPi * 1.1f * t) + 0.15f * Mathf.Sin(TwoPi * 3.3f * t);
                float sample = brown[Mathf.Min((int)pos, brown.Length - 1)];
                float chatter = Mathf.Abs(Mathf.Sin(TwoPi * 9f * t));
                float env = Mathf.Clamp01(t / 0.15f) * Mathf.Clamp01((seconds - t) / 0.3f);
                g[i] = (sample * 8f + band.Process(Noise(r)) * 0.3f * chatter) * env;
            }
            Save("door_grind", g, 0.85f);
        }

        public static void DoorSlam()
        {
            var r = new Random(66);
            var g = new float[Len(1.4f)];
            AddThump(g, 0, 100f, 38f, 0.25f, 1f);
            var lp = new LowPass(1200f);
            var rumble = new LowPass(150f);
            for (int i = 0; i < g.Length; i++)
            {
                float t = i / (float)SR;
                float n = Noise(r);
                g[i] += lp.Process(n) * Mathf.Exp(-t / 0.06f) * 1.2f + rumble.Process(n) * Mathf.Exp(-t / 0.5f) * 1.5f;
            }
            for (int k = 0; k < 8; k++) // small debris ticks
            {
                int s0 = Len(Rand(r, 0.1f, 0.9f));
                for (int s = 0; s < 200 && s0 + s < g.Length; s++) g[s0 + s] += Noise(r) * Mathf.Exp(-s / 40f) * 0.25f;
            }
            Save("door_slam", g, 0.95f);
        }

        public static void SpikeUp()
        {
            var r = new Random(77);
            var g = new float[Len(0.3f)];
            var band = new BandPass(3000f, 0.4f);
            float prev = 0f;
            for (int i = 0; i < g.Length; i++)
            {
                float t = i / (float)SR;
                float n = Noise(r);
                g[i] = (n - prev) * Mathf.Exp(-t / 0.03f) * 0.6f
                     + band.Process(n) * (t < 0.12f ? Mathf.Sin(Mathf.PI * t / 0.12f) : 0f) * 0.8f
                     + (Mathf.Sin(TwoPi * 2600f * t) + Mathf.Sin(TwoPi * 3900f * t)) * Mathf.Exp(-t / 0.08f) * 0.12f;
                prev = n;
            }
            Save("spike_up", g, 0.8f);
        }

        public static void DartWhoosh()
        {
            var r = new Random(88);
            float seconds = 0.45f;
            var g = new float[Len(seconds)];
            var band = new BandPass(4500f, 0.6f);
            for (int i = 0; i < g.Length; i++)
            {
                float t = i / (float)SR;
                band.SetCentre(4000f * Mathf.Exp(-t * 5f) + 500f);
                float env = Mathf.Pow(Mathf.Sin(Mathf.PI * t / seconds), 1.5f);
                g[i] = band.Process(Noise(r)) * env;
            }
            Save("dart_whoosh", g, 0.8f);
        }

        public static void BoulderRoll()
        {
            var r = new Random(99);
            int loop = Len(3f);
            var g = new float[loop + Len(0.3f)];
            var lp = new LowPass(150f);
            var deep = new LowPass(60f);
            for (int i = 0; i < g.Length; i++)
            {
                float t = i / (float)SR;
                float n = Noise(r);
                g[i] = lp.Process(n) * (0.7f + 0.3f * Mathf.Sin(TwoPi * 4f * t)) * 2f + deep.Process(n) * 3f;
            }
            for (float t = 0.1f; t < 3.2f; t += Rand(r, 0.28f, 0.45f)) AddThump(g, Len(t), 70f, 45f, 0.08f, 0.25f);
            Save("boulder_roll", Loop(g, loop), 0.9f);
        }

        public static void Hurt()
        {
            var r = new Random(111);
            var g = new float[Len(0.4f)];
            var lp = new LowPass(2500f);
            float phase = 0f;
            for (int i = 0; i < g.Length; i++)
            {
                float t = i / (float)SR;
                float f = 160f + 260f * Mathf.Exp(-t / 0.08f);
                phase += TwoPi * f / SR;
                float square = Mathf.Sign(Mathf.Sin(phase)) * 0.4f;
                g[i] = (lp.Process(square) + Noise(r) * Mathf.Exp(-t / 0.05f) * 0.3f) * Mathf.Exp(-t / 0.18f);
            }
            Save("hurt", g, 0.85f);
        }

        public static void HeartHum()
        {
            const float seconds = 4f;
            int len = Len(seconds);
            var g = new float[len];
            float[] freqs = { 220f, 277.25f, 330f, 440f, 660f };
            float[] amps = { 0.4f, 0.3f, 0.3f, 0.25f, 0.1f };
            const float vibrato = 5f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SR;
                float sum = 0f;
                for (int k = 0; k < freqs.Length; k++)
                {
                    float depth = freqs[k] * 0.004f;
                    float phase = TwoPi * freqs[k] * t + depth / vibrato * Mathf.Sin(TwoPi * vibrato * t);
                    float shimmer = 0.8f + 0.2f * Mathf.Sin(TwoPi * 0.5f * t + k);
                    sum += Mathf.Sin(phase) * amps[k] * shimmer;
                }
                g[i] = sum;
            }
            Save("heart_hum", g, 0.7f); // every frequency fits a whole number of cycles in 4 s, so it loops cleanly
        }

        public static void CollapseRumble()
        {
            var r = new Random(122);
            int loop = Len(4f);
            var g = new float[loop + Len(0.3f)];
            var lp = new LowPass(80f);
            var mid = new LowPass(200f);
            for (int i = 0; i < g.Length; i++)
            {
                float t = i / (float)SR;
                float n = Noise(r);
                g[i] = lp.Process(n) * 4f + mid.Process(n) * 1.2f * (0.6f + 0.4f * Mathf.Sin(TwoPi * 0.75f * t));
            }
            for (int k = 0; k < 10; k++)
            {
                int s0 = r.Next(loop);
                AddThump(g, s0, Rand(r, 70f, 100f), Rand(r, 40f, 55f), 0.15f, Rand(r, 0.4f, 0.8f), true);
                var crunch = new LowPass(2000f);
                for (int s = 0; s < Len(0.2f); s++)
                    g[(s0 + s) % g.Length] += crunch.Process(Noise(r)) * Mathf.Exp(-s / (float)Len(0.05f)) * 0.6f;
            }
            Save("collapse_rumble", Loop(g, loop), 0.95f);
        }

        public static void LowFuelBeat()
        {
            var g = new float[Len(0.7f)];
            AddThump(g, 0, 90f, 55f, 0.09f, 1f);
            AddThump(g, Len(0.2f), 85f, 52f, 0.09f, 0.7f);
            Save("low_fuel_beat", g, 0.8f);
        }

        public static void WinJingle()
        {
            var g = new float[Len(2.4f)];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < notes.Length; i++) AddNote(g, Len(i * 0.14f), notes[i], 0.35f, 0.8f, 0.25f, 0.1f);
            foreach (float f in notes) AddNote(g, Len(0.62f), f, 0.7f, 0.5f, 0.25f, 0.1f, 0.02f);
            Save("win_jingle", g, 0.85f);
        }

        public static void LoseSting()
        {
            var g = new float[Len(2.6f)];
            float[] notes = { 329.63f, 261.63f, 220f };
            for (int i = 0; i < notes.Length; i++)
            {
                AddNote(g, Len(i * 0.38f), notes[i], 0.6f, 0.8f, 0.4f, 0.2f, 0.03f);
                AddNote(g, Len(i * 0.38f), notes[i] + 1.5f, 0.6f, 0.4f, 0.4f, 0.2f, 0.03f); // detuned twin = chorus
            }
            AddNote(g, Len(1.14f), 110f, 0.8f, 0.6f, 0.5f, 0.2f, 0.05f);
            var lp = new LowPass(1800f);
            for (int i = 0; i < g.Length; i++) g[i] = lp.Process(g[i]);
            Save("lose_sting", g, 0.85f);
        }

        public static void UiClick()
        {
            var g = new float[Len(0.06f)];
            AddNote(g, 0, 1600f, 0.012f, 1f, 0.3f, 0f, 0.001f);
            Save("ui_click", g, 0.6f);
        }

        public static void UiHover()
        {
            var g = new float[Len(0.05f)];
            AddNote(g, 0, 900f, 0.01f, 1f, 0.1f, 0f, 0.001f);
            Save("ui_hover", g, 0.4f);
        }
    }
}
