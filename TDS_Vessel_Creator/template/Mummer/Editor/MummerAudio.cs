// Mummer asset generator: procedurally synthesised sound effects (44.1 kHz, 16-bit, mono like the game's clips).
// Felt and cloth (filtered noise), the brass bell on the costume's point, thread twangs, a reel's ratchet, needle
// stitches. Loudness is set at runtime by MummerData.SoundVolume to sit with the game's own sounds.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MummerBuild
{
    public static class MummerAudio
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
                new Sfx { Name = "Mummer_Landing", Use = "Vessel lands (soft cloth plop and the bell)", Make = Landing },
                new Sfx { Name = "Mummer_Footstep", Use = "Hop landing", Make = Footstep },
                new Sfx { Name = "Mummer_Peekaboo", Use = "Melee swipe from under the hem", Make = Peekaboo },
                new Sfx { Name = "Mummer_Hookthrow", Use = "HOOK: the thread is cast", Make = Hookthrow },
                new Sfx { Name = "Mummer_Reel", Use = "HOOK: the foe is reeled in", Make = Reel },
                new Sfx { Name = "Mummer_Bell", Use = "The bell on the costume's point", Make = Bell },
                new Sfx { Name = "Mummer_Scrap", Use = "A Scrap reaches a Loom", Make = Scrap },
                new Sfx { Name = "Mummer_Stitch", Use = "A Stage stitches an Understudy", Make = Stitch },
                new Sfx { Name = "Mummer_Puppetpop", Use = "An Understudy steps out", Make = Puppetpop },
                new Sfx { Name = "Mummer_Puppetdown", Use = "An Understudy falls (stuffing)", Make = Puppetdown },
                new Sfx { Name = "Mummer_Grab", Use = "The Marionette Cross takes hold", Make = Grab },
                new Sfx { Name = "Mummer_Curtain", Use = "A foe pushes through a Curtain", Make = Curtain },
                new Sfx { Name = "Mummer_Finalact", Use = "Final Act: the curtain drops", Make = Finalact },
                new Sfx { Name = "Mummer_Loom", Use = "The Loom's shuttle clacks (mining)", Make = Loom },
                new Sfx { Name = "Mummer_Unravel", Use = "Thread unravels at a new Tide", Make = Unravel },
                new Sfx { Name = "Mummer_Costume", Use = "A costume is unlocked", Make = Costume },
                new Sfx { Name = "Mummer_Death", Use = "Vessel death", Make = Death },
                new Sfx { Name = "Mummer_Bolt", Use = "A Ballista Understudy fires", Make = Bolt },
            };
        }

        // ---------------------------------------------------------------- building blocks (as Sprigg's)

        static float[] Buf(float seconds) { return new float[Mathf.CeilToInt(seconds * Rate)]; }
        static System.Random rng = new System.Random(11);
        static float Noise() { return (float)(rng.NextDouble() * 2.0 - 1.0); }
        static float Env(float t, float attack, float decay) { return t < attack ? t / attack : Mathf.Exp(-(t - attack) / decay); }

        static void Tone(float[] b, float start, float dur, float f0, float f1, float amp, float attack, float decay, float harmonic = 0f, float h3 = 0f)
        {
            double phase = 0;
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(f0, f1, Mathf.Clamp01(t / dur));
                phase += 2.0 * Math.PI * f / Rate;
                float s = (float)Math.Sin(phase) + harmonic * (float)Math.Sin(phase * 2.0) + h3 * (float)Math.Sin(phase * 2.76);
                b[s0 + i] += s * amp * Env(t, attack, decay);
            }
        }

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

        // A small brass bell: inharmonic partials, quick strike, long ring.
        static void Ding(float[] b, float start, float f, float amp, float decay)
        {
            Tone(b, start, decay * 4f, f, f, amp, 0.002f, decay, 0f, 0.5f);
            Tone(b, start, decay * 3f, f * 2.4f, f * 2.4f, amp * 0.35f, 0.002f, decay * 0.6f);
            Tone(b, start, decay * 2f, f * 4.1f, f * 4.1f, amp * 0.15f, 0.001f, decay * 0.3f);
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

        static float[] Landing()
        {
            var b = Buf(0.9f);
            Hiss(b, 0, 0.25f, 1f, 0.004f, 0.06f, 1400, 300);
            Tone(b, 0, 0.25f, 110, 55, 0.8f, 0.003f, 0.08f);
            Ding(b, 0.12f, 1480f, 0.25f, 0.18f);
            return Normalize(b);
        }

        static float[] Footstep()
        {
            var b = Buf(0.1f);
            Hiss(b, 0, 0.1f, 1f, 0.002f, 0.02f, 1200, 400);
            Tone(b, 0, 0.06f, 140, 90, 0.3f, 0.001f, 0.015f);
            return Normalize(b, 0.5f);
        }

        static float[] Peekaboo()
        {
            var b = Buf(0.3f);
            Hiss(b, 0, 0.18f, 1f, 0.04f, 0.05f, 900, 4200, 600);
            Hiss(b, 0.16f, 0.08f, 0.6f, 0.001f, 0.02f, 3000, 1200);
            Tone(b, 0.16f, 0.1f, 260, 140, 0.4f, 0.001f, 0.03f);
            return Normalize(b, 0.7f);
        }

        static float[] Hookthrow()
        {
            var b = Buf(0.45f);
            Hiss(b, 0, 0.3f, 0.8f, 0.05f, 0.08f, 1500, 6500, 900);
            Tone(b, 0.18f, 0.25f, 880, 1320, 0.35f, 0.002f, 0.06f, 0.3f);
            Ding(b, 0.22f, 1760f, 0.12f, 0.08f);
            return Normalize(b, 0.7f);
        }

        // A ratchet: quick clicks with a taut twang under them.
        static float[] Reel()
        {
            var b = Buf(0.6f);
            for (int i = 0; i < 9; i++)
                Hiss(b, i * 0.045f, 0.02f, 0.9f, 0.0005f, 0.004f, 6000, 3000, 1500);
            Tone(b, 0, 0.45f, 196, 330, 0.35f, 0.01f, 0.15f, 0.4f);
            return Normalize(b, 0.7f);
        }

        static float[] Bell()
        {
            var b = Buf(0.9f);
            Ding(b, 0, 1568f, 0.6f, 0.2f);
            return Normalize(b, 0.6f);
        }

        static float[] Scrap()
        {
            var b = Buf(0.22f);
            Hiss(b, 0, 0.08f, 0.6f, 0.002f, 0.02f, 4000, 2000, 800);
            Tone(b, 0.02f, 0.15f, 1175, 1760, 0.5f, 0.002f, 0.05f, 0.2f);
            return Normalize(b, 0.6f);
        }

        // Needle and thread: a few quick pulls and a little rising flourish.
        static float[] Stitch()
        {
            var b = Buf(0.6f);
            for (int i = 0; i < 4; i++)
            {
                Hiss(b, i * 0.08f, 0.06f, 0.7f, 0.004f, 0.02f, 5000, 2500, 1200);
                Tone(b, i * 0.08f + 0.01f, 0.05f, 700 + i * 120, 900 + i * 120, 0.25f, 0.002f, 0.015f);
            }
            float[] notes = { 784f, 988f, 1175f };
            for (int i = 0; i < notes.Length; i++) Tone(b, 0.32f + i * 0.06f, 0.25f, notes[i], notes[i], 0.3f, 0.004f, 0.1f, 0.2f);
            return Normalize(b, 0.65f);
        }

        static float[] Puppetpop()
        {
            var b = Buf(0.25f);
            Tone(b, 0, 0.2f, 520, 1040, 0.8f, 0.004f, 0.06f, 0.25f);
            Hiss(b, 0, 0.05f, 0.4f, 0.001f, 0.01f, 3000, 1500);
            return Normalize(b, 0.6f);
        }

        static float[] Puppetdown()
        {
            var b = Buf(0.5f);
            Hiss(b, 0, 0.35f, 1f, 0.003f, 0.1f, 2200, 400, 200);
            Tone(b, 0.02f, 0.3f, 330, 150, 0.4f, 0.005f, 0.08f);
            return Normalize(b, 0.7f);
        }

        static float[] Grab()
        {
            var b = Buf(0.55f);
            Tone(b, 0, 0.5f, 220, 216, 0.6f, 0.001f, 0.12f, 0.5f, 0.2f);
            Tone(b, 0.01f, 0.4f, 330, 327, 0.4f, 0.001f, 0.1f, 0.3f);
            Hiss(b, 0, 0.05f, 0.4f, 0.001f, 0.01f, 5000, 2000, 1000);
            return Normalize(b, 0.7f);
        }

        static float[] Curtain()
        {
            var b = Buf(0.4f);
            Hiss(b, 0, 0.38f, 1f, 0.06f, 0.1f, 600, 2400, 250);
            return Normalize(b, 0.6f);
        }

        static float[] Finalact()
        {
            var b = Buf(1.3f);
            Hiss(b, 0, 0.5f, 0.9f, 0.08f, 0.12f, 500, 2800, 200);
            Tone(b, 0.45f, 0.5f, 80, 45, 1f, 0.003f, 0.12f);
            Hiss(b, 0.45f, 0.4f, 0.6f, 0.002f, 0.1f, 1500, 400);
            Ding(b, 0.6f, 1046f, 0.3f, 0.25f);
            Ding(b, 0.7f, 1568f, 0.2f, 0.22f);
            return Normalize(b, 0.8f);
        }

        static float[] Loom()
        {
            var b = Buf(0.18f);
            Tone(b, 0, 0.08f, 900, 600, 0.6f, 0.001f, 0.012f, 0.4f);
            Tone(b, 0.07f, 0.08f, 750, 500, 0.5f, 0.001f, 0.012f, 0.4f);
            Hiss(b, 0, 0.15f, 0.3f, 0.01f, 0.04f, 2500, 1200, 600);
            return Normalize(b, 0.55f);
        }

        static float[] Unravel()
        {
            var b = Buf(0.7f);
            for (int i = 0; i < 6; i++) Hiss(b, i * 0.07f, 0.05f, 0.7f, 0.002f, 0.015f, 5000, 2500, 1500);
            Tone(b, 0.05f, 0.6f, 660, 330, 0.35f, 0.01f, 0.2f, 0.2f);
            return Normalize(b, 0.6f);
        }

        static float[] Costume()
        {
            var b = Buf(1.0f);
            float[] notes = { 784f, 988f, 1175f, 1568f };
            for (int i = 0; i < notes.Length; i++) Tone(b, i * 0.09f, 0.6f, notes[i], notes[i], 0.4f, 0.004f, 0.2f, 0.25f);
            Ding(b, 0.36f, 2093f, 0.25f, 0.2f);
            return Normalize(b, 0.7f);
        }

        static float[] Death()
        {
            var b = Buf(1.3f);
            Hiss(b, 0, 0.4f, 0.6f, 0.01f, 0.12f, 2500, 300, 200);
            float[] notes = { 659f, 587f, 523f, 392f };
            for (int i = 0; i < notes.Length; i++) Tone(b, 0.05f + i * 0.17f, 0.6f, notes[i], notes[i] * 0.97f, 0.4f, 0.006f, 0.25f, 0.2f);
            Ding(b, 0.75f, 1046f, 0.2f, 0.3f);
            return Normalize(b, 0.7f);
        }

        static float[] Bolt()
        {
            var b = Buf(0.2f);
            Tone(b, 0, 0.1f, 520, 260, 0.8f, 0.001f, 0.03f, 0.3f);
            Hiss(b, 0, 0.12f, 0.5f, 0.01f, 0.03f, 2500, 5000, 900);
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
