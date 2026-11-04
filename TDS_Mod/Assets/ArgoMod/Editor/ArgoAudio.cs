// Argo asset generator: procedurally synthesised sound effects (44.1 kHz, 16-bit, mono like the game's clips). Clips
// are normalised here; the mod sets each one's playback volume at run time to a target loudness matched to the game's
// own sounds for the same job (ArgoData.SoundTarget), so nothing here needs hand-tuning for level.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ArgoBuild
{
    public static class ArgoAudio
    {
        const int Rate = 44100;

        public class Sfx
        {
            public string Name, Use;
            public Func<float[]> Make;
        }

        public static List<Sfx> All()
        {
            return new List<Sfx>
            {
                new Sfx { Name = "Argo_Swing", Use = "Wrench swing (the vessel's Attack)", Make = Swing },
                new Sfx { Name = "Argo_Clang", Use = "The swing carries on into a second enemy", Make = Clang },
                new Sfx { Name = "Argo_Sonar", Use = "SONAR ping", Make = Sonar },
                new Sfx { Name = "Argo_Scatter", Use = "Scatter Cannon volley", Make = Scatter },
                new Sfx { Name = "Argo_Harpoon", Use = "Harpoon Gun shot", Make = Harpoon },
                new Sfx { Name = "Argo_Lance", Use = "Leviathan Lance shot", Make = Lance },
                new Sfx { Name = "Argo_Drill", Use = "Drill Rig bites ore", Make = Drill },
                new Sfx { Name = "Argo_Fathom", Use = "A Drill Rig sinks a Fathom; Gusher", Make = Fathom },
                new Sfx { Name = "Argo_Quake", Use = "Quake and Aftershock", Make = Quake },
                new Sfx { Name = "Argo_Tune", Use = "A Naut starts tuning a tower", Make = Tune },
                new Sfx { Name = "Argo_Naut", Use = "A Naut is sent or appears", Make = Naut },
                new Sfx { Name = "Argo_Dive", Use = "Full Crew dive, bubbles on building", Make = Dive },
                new Sfx { Name = "Argo_Death", Use = "Vessel or building lost", Make = Death },
                new Sfx { Name = "Argo_Upgrade", Use = "Upgrade applied (on top of the game's own)", Make = Upgrade },
                new Sfx { Name = "Argo_Build", Use = "A building is placed", Make = Build },
                new Sfx { Name = "Argo_Winch", Use = "A harpoon reels its foe in; Double Barrel loads", Make = Winch },
                new Sfx { Name = "Argo_Splash", Use = "Deep Water placed, FLOOD adds a pool", Make = Splash },
                new Sfx { Name = "Argo_Pin", Use = "Anchor Line pins a foe (a stun beside a building rings brighter)", Make = Pin },
                new Sfx { Name = "Argo_Gurgle", Use = "Trench drags a foe under", Make = Gurgle },
            };
        }

        // ---------------------------------------------------------------- building blocks

        static float[] Buf(float seconds) { return new float[Mathf.CeilToInt(seconds * Rate)]; }
        static System.Random rng = new System.Random(11);
        static float Noise() { return (float)(rng.NextDouble() * 2.0 - 1.0); }
        static float Env(float t, float attack, float decay) { return t < attack ? t / attack : Mathf.Exp(-(t - attack) / decay); }

        static void Tone(float[] b, float start, float dur, float f0, float f1, float amp, float attack, float decay, float harmonic = 0f, float third = 0f)
        {
            double phase = 0;
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(f0, f1, Mathf.Clamp01(t / dur));
                phase += 2.0 * Math.PI * f / Rate;
                float s = (float)Math.Sin(phase) + harmonic * (float)Math.Sin(phase * 2.0) + third * (float)Math.Sin(phase * 2.76);
                b[s0 + i] += s * amp * Env(t, attack, decay);
            }
        }

        // Filtered noise burst; cutoff sweeps from c0 to c1 (one-pole low-pass, optional high-pass).
        static void Hiss(float[] b, float start, float dur, float amp, float attack, float decay, float c0, float c1, float hp = 0f)
        {
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            float lp = 0f, hpState = 0f, prev = 0f;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float cut = Mathf.Lerp(c0, c1, Mathf.Clamp01(t / dur));
                float a = 1f - Mathf.Exp(-2f * Mathf.PI * cut / Rate);
                lp += a * (Noise() - lp);
                float x = lp;
                if (hp > 0f)
                {
                    float ah = Mathf.Exp(-2f * Mathf.PI * hp / Rate);
                    hpState = ah * (hpState + x - prev);
                    prev = x;
                    x = hpState;
                }
                b[s0 + i] += x * amp * Env(t, attack, decay);
            }
        }

        static void Bubbles(float[] b, float start, float span, int count, float amp)
        {
            for (int i = 0; i < count; i++)
            {
                float st = start + (float)rng.NextDouble() * span;
                float f = 380f + (float)rng.NextDouble() * 650f;
                Tone(b, st, 0.07f, f, f * 1.9f, amp, 0.003f, 0.025f);
            }
        }

        static float[] Normalize(float[] b, float peak = 0.8f)
        {
            float m = 0.0001f;
            foreach (var s in b) m = Mathf.Max(m, Mathf.Abs(s));
            for (int i = 0; i < b.Length; i++) b[i] = b[i] / m * peak;
            int fade = Mathf.Min(b.Length / 4, 400);
            for (int i = 0; i < fade; i++) b[b.Length - 1 - i] *= i / (float)fade;
            return b;
        }

        // ---------------------------------------------------------------- the sounds

        static float[] Swing()
        {
            var b = Buf(0.28f);
            Hiss(b, 0, 0.28f, 1f, 0.09f, 0.07f, 500, 2600, 250);
            return Normalize(b, 0.6f);
        }

        static float[] Clang()
        {
            var b = Buf(0.5f);
            Hiss(b, 0, 0.03f, 0.7f, 0.0005f, 0.006f, 9000, 5000);
            Tone(b, 0, 0.5f, 640, 630, 0.5f, 0.001f, 0.12f, 0.3f, 0.35f);
            Tone(b, 0, 0.4f, 1310, 1300, 0.25f, 0.001f, 0.07f);
            return Normalize(b, 0.7f);
        }

        static float[] Sonar()
        {
            var b = Buf(1.4f);
            Tone(b, 0, 1.1f, 1480, 1400, 0.8f, 0.004f, 0.32f, 0.08f);
            Tone(b, 0.38f, 0.9f, 1480, 1400, 0.28f, 0.004f, 0.28f);
            Tone(b, 0.72f, 0.6f, 1480, 1400, 0.1f, 0.004f, 0.22f);
            Hiss(b, 0, 0.6f, 0.05f, 0.05f, 0.2f, 2500, 900, 400);
            return Normalize(b, 0.7f);
        }

        static float[] Scatter()
        {
            var b = Buf(0.6f);
            Tone(b, 0, 0.35f, 95, 42, 1f, 0.002f, 0.1f);
            Hiss(b, 0, 0.45f, 0.9f, 0.001f, 0.08f, 2600, 300);
            Hiss(b, 0.02f, 0.25f, 0.35f, 0.002f, 0.05f, 7000, 3000, 2000);
            return Normalize(b, 0.8f);
        }

        static float[] Harpoon()
        {
            var b = Buf(0.45f);
            Tone(b, 0, 0.35f, 230, 170, 0.8f, 0.001f, 0.09f, 0.5f);
            Hiss(b, 0, 0.3f, 0.5f, 0.02f, 0.08f, 1200, 4000, 700);
            Hiss(b, 0, 0.02f, 0.5f, 0.0005f, 0.005f, 6000, 3000);
            return Normalize(b, 0.75f);
        }

        static float[] Lance()
        {
            var b = Buf(0.9f);
            Tone(b, 0, 0.5f, 80, 38, 1f, 0.003f, 0.16f);
            Tone(b, 0.02f, 0.8f, 600, 1800, 0.3f, 0.05f, 0.3f);
            Hiss(b, 0, 0.6f, 0.6f, 0.002f, 0.14f, 3000, 400);
            return Normalize(b, 0.8f);
        }

        static float[] Drill()
        {
            var b = Buf(0.35f);
            Hiss(b, 0, 0.35f, 0.8f, 0.01f, 0.12f, 1800, 900, 300);
            Tone(b, 0, 0.32f, 62, 58, 0.5f, 0.01f, 0.12f, 0.6f, 0.4f);
            for (int i = 0; i < 5; i++) Hiss(b, i * 0.055f, 0.02f, 0.4f, 0.0005f, 0.006f, 5000, 2500);
            return Normalize(b, 0.6f);
        }

        static float[] Fathom()
        {
            var b = Buf(1.6f);
            Tone(b, 0, 1.5f, 110, 108, 0.8f, 0.004f, 0.55f, 0.35f, 0.3f);
            Tone(b, 0, 1.2f, 262, 260, 0.25f, 0.004f, 0.4f);
            Bubbles(b, 0.15f, 0.8f, 10, 0.12f);
            return Normalize(b, 0.75f);
        }

        static float[] Quake()
        {
            var b = Buf(1.0f);
            Hiss(b, 0, 1.0f, 1f, 0.02f, 0.3f, 380, 70);
            Tone(b, 0, 0.5f, 55, 35, 0.8f, 0.004f, 0.18f);
            Hiss(b, 0.05f, 0.4f, 0.2f, 0.01f, 0.1f, 2500, 800, 600);
            return Normalize(b, 0.8f);
        }

        static float[] Tune()
        {
            var b = Buf(0.42f);
            for (int i = 0; i < 4; i++)
            {
                float st = i * 0.085f;
                Hiss(b, st, 0.02f, 0.7f, 0.0005f, 0.004f, 7000, 4000, 1500);
                Tone(b, st, 0.05f, 2100, 2000, 0.25f, 0.0005f, 0.012f);
            }
            Tone(b, 0.34f, 0.08f, 1800, 2400, 0.2f, 0.002f, 0.03f);
            return Normalize(b, 0.6f);
        }

        // A ratchet winding in: five quick dull clicks over a low creak of rope.
        static float[] Winch()
        {
            var b = Buf(0.4f);
            for (int i = 0; i < 5; i++)
            {
                float st = i * 0.07f;
                Hiss(b, st, 0.018f, 0.6f, 0.0005f, 0.004f, 4000, 2500, 900);
                Tone(b, st, 0.04f, 900, 820, 0.25f, 0.0005f, 0.01f);
            }
            Tone(b, 0, 0.36f, 150, 190, 0.15f, 0.03f, 0.2f, 0.3f);
            return Normalize(b, 0.55f);
        }

        static float[] Naut()
        {
            var b = Buf(0.2f);
            Tone(b, 0, 0.16f, 420, 950, 0.8f, 0.004f, 0.05f, 0.15f);
            return Normalize(b, 0.6f);
        }

        static float[] Dive()
        {
            var b = Buf(0.9f);
            Hiss(b, 0, 0.8f, 0.3f, 0.05f, 0.3f, 1500, 600, 300);
            Bubbles(b, 0, 0.7f, 18, 0.25f);
            return Normalize(b, 0.65f);
        }

        static float[] Death()
        {
            var b = Buf(1.3f);
            Hiss(b, 0, 0.1f, 0.7f, 0.001f, 0.025f, 6000, 2000);
            float[] notes = { 523.3f, 440f, 349.2f, 261.6f };
            for (int i = 0; i < notes.Length; i++) Tone(b, 0.06f + i * 0.17f, 0.7f, notes[i], notes[i] * 0.97f, 0.5f, 0.006f, 0.26f, 0.3f);
            Bubbles(b, 0.2f, 0.9f, 8, 0.15f);
            return Normalize(b, 0.75f);
        }

        static float[] Upgrade()
        {
            var b = Buf(0.7f);
            Tone(b, 0, 0.6f, 659.3f, 659.3f, 0.5f, 0.004f, 0.2f, 0.3f);
            Tone(b, 0.1f, 0.6f, 987.8f, 987.8f, 0.45f, 0.004f, 0.22f, 0.25f);
            Bubbles(b, 0.05f, 0.3f, 4, 0.08f);
            return Normalize(b, 0.7f);
        }

        static float[] Build()
        {
            var b = Buf(0.5f);
            Tone(b, 0, 0.35f, 300, 280, 0.6f, 0.001f, 0.08f, 0.4f, 0.3f);
            Hiss(b, 0, 0.12f, 0.8f, 0.001f, 0.03f, 2200, 500);
            Tone(b, 0.12f, 0.3f, 90, 60, 0.6f, 0.003f, 0.08f);
            return Normalize(b, 0.75f);
        }

        // 2.0 (Deep Water, Anchor Line, Trench)
        static float[] Splash()
        {
            var b = Buf(0.6f);
            Hiss(b, 0f, 0.5f, 0.6f, 0.005f, 0.12f, 3500f, 700f, 250f);
            Tone(b, 0f, 0.15f, 320f, 140f, 0.25f, 0.002f, 0.05f);
            Bubbles(b, 0.08f, 0.45f, 7, 0.25f);
            return Normalize(b, 0.7f);
        }

        static float[] Pin()
        {
            var b = Buf(0.45f);
            Tone(b, 0f, 0.1f, 140f, 70f, 0.7f, 0.001f, 0.04f);
            Tone(b, 0.01f, 0.4f, 620f, 600f, 0.3f, 0.002f, 0.12f, 0.4f, 0.2f);
            Hiss(b, 0f, 0.06f, 0.3f, 0.001f, 0.02f, 6000f, 2500f, 400f);
            return Normalize(b, 0.65f);
        }

        static float[] Gurgle()
        {
            var b = Buf(0.9f);
            Tone(b, 0f, 0.8f, 160f, 60f, 0.4f, 0.05f, 0.3f);
            Bubbles(b, 0f, 0.8f, 12, 0.35f);
            return Normalize(b, 0.6f);
        }

        // ---------------------------------------------------------------- WAV writer

        public static void SaveWav(string path, float[] samples)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int dataLen = samples.Length * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + dataLen);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                w.Write(dataLen);
                foreach (var s in samples) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
            }
        }
    }
}
