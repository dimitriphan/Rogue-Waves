// Spool asset generator: procedurally synthesised sound effects (44.1 kHz, 16-bit, mono like the game's clips).
// TEMPORARY SOUNDS: plush and cloth (filtered noise), Spool's zipper mouth (quick clicks), needle and thread pulls, pins.
// Loudness is set at runtime by SpoolData.SoundVolume (Code/02_Data.cs) to sit with the game's own sounds. The footstep
// and landing in game are the template's own (Code/04_Content.cs); these two are written for a later swap.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpoolBuild
{
    public static class SpoolAudio
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
                new Sfx { Name = "Spool_Swat", Use = "Melee swat of a white glove (Spool, a Stitched, a Seam, a Mangled, the Thread Head)", Make = Swat },
                new Sfx { Name = "Spool_Stitch", Use = "A Cookie Tin stitches a Crafted, Thread is delivered, a building goes up", Make = Stitch },
                new Sfx { Name = "Spool_Pop", Use = "A Crafted steps out, a Mangled leaps in", Make = Pop },
                new Sfx { Name = "Spool_Down", Use = "A Crafted or the Thread Head falls (stuffing)", Make = Down },
                new Sfx { Name = "Spool_Burst", Use = "One Last Surprise, Burst Seams: a Crafted bursts", Make = Burst },
                new Sfx { Name = "Spool_Tuft", Use = "Thread falls on the ground", Make = Tuft },
                new Sfx { Name = "Spool_Deliver", Use = "Thread reaches the Yarn Stash", Make = Deliver },
                new Sfx { Name = "Spool_Prick", Use = "A Pin Cushion pricks its attacker; a TARGET or SPOT is set", Make = Prick },
                new Sfx { Name = "Spool_Patch", Use = "PATCH: needle and thread", Make = Patch },
                new Sfx { Name = "Spool_Needle", Use = "A Dart throws a needle", Make = Needle },
                new Sfx { Name = "Spool_Spin", Use = "A Loom or a Pin Cushion spins Thread", Make = Spin },
                new Sfx { Name = "Spool_Landing", Use = "Vessel lands (a plush plop, the zipper snaps shut)", Make = Landing },
                new Sfx { Name = "Spool_Footstep", Use = "A plush foot touches down", Make = Footstep },
                new Sfx { Name = "Spool_Death", Use = "Vessel death", Make = Death },
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

        // A small metal ting (a pin, a needle): inharmonic partials, quick strike, short ring.
        static void Ding(float[] b, float start, float f, float amp, float decay)
        {
            Tone(b, start, decay * 4f, f, f, amp, 0.002f, decay, 0f, 0.5f);
            Tone(b, start, decay * 3f, f * 2.4f, f * 2.4f, amp * 0.35f, 0.002f, decay * 0.6f);
            Tone(b, start, decay * 2f, f * 4.1f, f * 4.1f, amp * 0.15f, 0.001f, decay * 0.3f);
        }

        // Spool's zipper mouth: a run of quick clicks.
        static void Zip(float[] b, float start, int clicks, float gap, float amp)
        {
            for (int i = 0; i < clicks; i++)
            {
                Hiss(b, start + i * gap, 0.012f, amp, 0.0005f, 0.003f, 7000, 4000, 2000);
                Tone(b, start + i * gap, 0.01f, 2400 + i * 60, 2000 + i * 60, amp * 0.3f, 0.0005f, 0.003f);
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

        static float[] Swat()
        {
            var b = Buf(0.3f);
            Hiss(b, 0, 0.16f, 1f, 0.04f, 0.04f, 900, 4200, 600);
            Hiss(b, 0.14f, 0.08f, 0.7f, 0.001f, 0.02f, 2000, 800);
            Tone(b, 0.14f, 0.1f, 200, 110, 0.5f, 0.001f, 0.03f);
            return Normalize(b, 0.7f);
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

        static float[] Pop()
        {
            var b = Buf(0.25f);
            Tone(b, 0, 0.2f, 520, 1040, 0.8f, 0.004f, 0.06f, 0.25f);
            Hiss(b, 0, 0.05f, 0.4f, 0.001f, 0.01f, 3000, 1500);
            return Normalize(b, 0.6f);
        }

        static float[] Down()
        {
            var b = Buf(0.5f);
            Hiss(b, 0, 0.35f, 1f, 0.003f, 0.1f, 2200, 400, 200);
            Tone(b, 0.02f, 0.3f, 330, 150, 0.4f, 0.005f, 0.08f);
            return Normalize(b, 0.7f);
        }

        // A seam gives: a tearing rip, then a soft whump of stuffing.
        static float[] Burst()
        {
            var b = Buf(0.7f);
            for (int i = 0; i < 7; i++) Hiss(b, i * 0.018f, 0.03f, 0.8f, 0.001f, 0.008f, 6000, 3000, 1500);
            Tone(b, 0.12f, 0.4f, 90, 45, 1f, 0.003f, 0.12f);
            Hiss(b, 0.12f, 0.5f, 0.8f, 0.004f, 0.15f, 1800, 300, 150);
            return Normalize(b, 0.8f);
        }

        static float[] Tuft()
        {
            var b = Buf(0.18f);
            Hiss(b, 0, 0.1f, 0.6f, 0.003f, 0.03f, 2500, 900, 400);
            Tone(b, 0.01f, 0.12f, 880, 1320, 0.4f, 0.002f, 0.04f, 0.2f);
            return Normalize(b, 0.5f);
        }

        static float[] Deliver()
        {
            var b = Buf(0.45f);
            Hiss(b, 0, 0.12f, 0.5f, 0.01f, 0.04f, 1800, 900, 300);
            Tone(b, 0.05f, 0.25f, 988f, 988f, 0.4f, 0.003f, 0.08f, 0.2f);
            Tone(b, 0.13f, 0.3f, 1319f, 1319f, 0.4f, 0.003f, 0.1f, 0.2f);
            return Normalize(b, 0.6f);
        }

        static float[] Prick()
        {
            var b = Buf(0.25f);
            Ding(b, 0, 2637f, 0.5f, 0.03f);
            Hiss(b, 0, 0.03f, 0.5f, 0.0005f, 0.006f, 8000, 5000, 3000);
            return Normalize(b, 0.6f);
        }

        // PATCH: the zipper opens, three quick pulls of thread, a bright little flourish.
        static float[] Patch()
        {
            var b = Buf(0.8f);
            Zip(b, 0, 6, 0.018f, 0.6f);
            for (int i = 0; i < 3; i++)
            {
                Hiss(b, 0.14f + i * 0.09f, 0.07f, 0.7f, 0.004f, 0.025f, 5500, 2500, 1200);
                Tone(b, 0.15f + i * 0.09f, 0.06f, 640 + i * 160, 880 + i * 160, 0.25f, 0.002f, 0.02f);
            }
            float[] notes = { 659f, 880f, 1319f };
            for (int i = 0; i < notes.Length; i++) Tone(b, 0.45f + i * 0.05f, 0.3f, notes[i], notes[i], 0.3f, 0.004f, 0.1f, 0.2f);
            return Normalize(b, 0.7f);
        }

        static float[] Needle()
        {
            var b = Buf(0.2f);
            Hiss(b, 0, 0.12f, 0.6f, 0.01f, 0.03f, 3000, 7000, 1500);
            Ding(b, 0.01f, 3136f, 0.25f, 0.02f);
            return Normalize(b, 0.55f);
        }

        // A spinning whir: quick clicks that speed up, a thin rising hum under them.
        static float[] Spin()
        {
            var b = Buf(0.35f);
            float t = 0f;
            for (int i = 0; i < 8; i++) { Hiss(b, t, 0.012f, 0.7f, 0.0005f, 0.003f, 5000, 3000, 1500); t += 0.045f - i * 0.004f; }
            Tone(b, 0, 0.3f, 330, 520, 0.3f, 0.01f, 0.1f, 0.3f);
            return Normalize(b, 0.55f);
        }

        static float[] Landing()
        {
            var b = Buf(0.6f);
            Hiss(b, 0, 0.25f, 1f, 0.004f, 0.06f, 1400, 300);
            Tone(b, 0, 0.25f, 110, 55, 0.8f, 0.003f, 0.08f);
            Zip(b, 0.08f, 5, 0.012f, 0.5f);
            return Normalize(b);
        }

        static float[] Footstep()
        {
            var b = Buf(0.1f);
            Hiss(b, 0, 0.1f, 1f, 0.002f, 0.02f, 1200, 400);
            Tone(b, 0, 0.06f, 140, 90, 0.3f, 0.001f, 0.015f);
            return Normalize(b, 0.5f);
        }

        // Stuffing spills and the zipper slowly gives, a falling sigh of notes.
        static float[] Death()
        {
            var b = Buf(1.3f);
            Hiss(b, 0, 0.4f, 0.6f, 0.01f, 0.12f, 2500, 300, 200);
            Zip(b, 0.05f, 8, 0.03f, 0.4f);
            float[] notes = { 659f, 587f, 523f, 392f };
            for (int i = 0; i < notes.Length; i++) Tone(b, 0.1f + i * 0.17f, 0.6f, notes[i], notes[i] * 0.97f, 0.4f, 0.006f, 0.25f, 0.2f);
            return Normalize(b, 0.7f);
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
