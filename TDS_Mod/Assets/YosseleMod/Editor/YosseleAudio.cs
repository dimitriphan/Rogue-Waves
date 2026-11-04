// Yossele asset generator: procedurally synthesised sound effects (44.1 kHz, 16-bit, mono like the game's clips).
// TEMPORARY SOUNDS. Clay thuds, earth and leaves (filtered noise), a mortar's hollow pomf, soft wooden creaks, small
// chimes for growth and healing, a soft green zap for the mouth laser. Every clip is normalised; loudness is set at run time by YosseleData.SoundVolume.
// Suggested volumes (clip rms x volume near the game's own levels, docs/03 6; keep them accents):
//   Yossele_Zap 0.03, Yossele_Thud 0.04, Yossele_Lob 0.03, Yossele_Pop 0.025, Yossele_Seeds 0.02, Yossele_Creak 0.03,
//   Yossele_Rustle 0.03, Yossele_Fertilize 0.035, Yossele_Pick 0.03, Yossele_Dig 0.012, Yossele_Mend 0.025,
//   Yossele_Death 0.06. (The walk keeps the template's footstep: an animation frame's sound is a game sound name.)
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YosseleBuild
{
    public static class YosseleAudio
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
                new Sfx { Name = "Yossele_Zap", Use = "Yossele's mouth laser fires", Make = Zap },
                new Sfx { Name = "Yossele_Thud", Use = "A Cordyfriend's swing lands", Make = Thud },
                new Sfx { Name = "Yossele_Lob", Use = "The Mushroom Cannon lobs a spore bomb", Make = Lob },
                new Sfx { Name = "Yossele_Pop", Use = "A spore bomb bursts", Make = Pop },
                new Sfx { Name = "Yossele_Seeds", Use = "The Dandelion Tree's seeds fly out", Make = Seeds },
                new Sfx { Name = "Yossele_Creak", Use = "The Stonehenge's vines creep forward", Make = Creak },
                new Sfx { Name = "Yossele_Rustle", Use = "A building gains a Growth stage", Make = Rustle },
                new Sfx { Name = "Yossele_Fertilize", Use = "FERTILIZE: compost pressed into the ground", Make = Fertilize },
                new Sfx { Name = "Yossele_Pick", Use = "An aura fruit is picked up", Make = Pick },
                new Sfx { Name = "Yossele_Dig", Use = "The Pachira's roots dig at ore (mining)", Make = Dig },
                new Sfx { Name = "Yossele_Mend", Use = "Healing: Natural Remedy, Sustaining Practices", Make = Mend },
                new Sfx { Name = "Yossele_Death", Use = "Vessel death (clay crumbles)", Make = Death },
            };
        }

        // ---------------------------------------------------------------- building blocks

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

        // A crackle: short clicks of filtered noise (dry clay, twigs).
        static void Crackle(float[] b, float start, float dur, int n, float amp, int seed)
        {
            var r = new System.Random(seed);
            for (int i = 0; i < n; i++)
                Hiss(b, start + (float)r.NextDouble() * dur, 0.012f, amp * (0.5f + 0.5f * (float)r.NextDouble()), 0.0005f, 0.003f, 4000, 2500, 900);
        }

        static float[] Thud()
        {
            var b = Buf(0.4f);
            Hiss(b, 0, 0.12f, 0.5f, 0.03f, 0.03f, 600, 2200, 300);          // the swing
            Tone(b, 0.1f, 0.28f, 120, 48, 1f, 0.002f, 0.07f, 0.3f);         // the slam
            Hiss(b, 0.1f, 0.25f, 0.7f, 0.002f, 0.06f, 1500, 300);           // dirt
            Crackle(b, 0.11f, 0.1f, 5, 0.4f, 3);
            return Normalize(b, 0.8f);
        }

        static float[] Lob()
        {
            var b = Buf(0.45f);
            Tone(b, 0, 0.18f, 140, 260, 0.9f, 0.004f, 0.05f, 0.5f);         // hollow mortar pomf
            Hiss(b, 0, 0.12f, 0.6f, 0.002f, 0.03f, 900, 500, 150);
            Hiss(b, 0.08f, 0.35f, 0.25f, 0.05f, 0.1f, 2000, 5000, 1200);    // the bomb whistles away
            return Normalize(b, 0.7f);
        }

        // A soft laser: a falling sine sweep with a buzzy third harmonic and a breath of high noise, short and round.
        static float[] Zap()
        {
            var b = Buf(0.3f);
            Tone(b, 0, 0.22f, 1500, 420, 0.7f, 0.004f, 0.07f, 0f, 0.25f);
            Tone(b, 0.005f, 0.16f, 760, 300, 0.35f, 0.003f, 0.05f);
            Hiss(b, 0, 0.12f, 0.15f, 0.002f, 0.03f, 6000, 3000, 1500);
            return Normalize(b, 0.55f);
        }

        static float[] Pop()
        {
            var b = Buf(0.55f);
            Tone(b, 0, 0.08f, 300, 90, 0.8f, 0.001f, 0.025f);
            Hiss(b, 0, 0.5f, 0.9f, 0.004f, 0.14f, 2500, 500, 200);          // spores puff out
            return Normalize(b, 0.7f);
        }

        static float[] Seeds()
        {
            var b = Buf(0.5f);
            Hiss(b, 0, 0.45f, 0.8f, 0.06f, 0.12f, 3000, 7000, 2000);
            for (int i = 0; i < 6; i++) Tone(b, 0.02f + i * 0.05f, 0.08f, 1800 + i * 150, 2400 + i * 150, 0.12f, 0.002f, 0.02f);
            return Normalize(b, 0.6f);
        }

        // A slow wooden creak: a low tone wobbling in pitch, with crackle.
        static float[] Creak()
        {
            var b = Buf(0.7f);
            double ph = 0;
            for (int i = 0; i < (int)(0.6f * Rate); i++)
            {
                float t = i / (float)Rate;
                float f = 170f + 40f * Mathf.Sin(t * 23f) + 30f * t;
                ph += 2.0 * Math.PI * f / Rate;
                float saw = (float)((ph / Math.PI) % 2.0) - 1f;
                b[i] += saw * 0.35f * Env(t, 0.05f, 0.25f) * (0.6f + 0.4f * Mathf.Sin(t * 70f));
            }
            Hiss(b, 0, 0.6f, 0.2f, 0.05f, 0.2f, 1200, 800, 300);
            Crackle(b, 0.05f, 0.5f, 7, 0.35f, 5);
            return Normalize(b, 0.6f);
        }

        static float[] Rustle()
        {
            var b = Buf(0.8f);
            for (int i = 0; i < 4; i++) Hiss(b, i * 0.06f, 0.15f, 0.6f, 0.02f, 0.04f, 3500, 6000, 1800);
            float[] notes = { 523f, 659f, 784f };
            for (int i = 0; i < notes.Length; i++) Tone(b, 0.18f + i * 0.08f, 0.45f, notes[i], notes[i], 0.3f, 0.005f, 0.15f, 0.15f);
            return Normalize(b, 0.6f);
        }

        static float[] Fertilize()
        {
            var b = Buf(0.8f);
            Hiss(b, 0, 0.2f, 1f, 0.01f, 0.05f, 700, 300, 120);              // squelch of compost
            Tone(b, 0.01f, 0.12f, 200, 120, 0.5f, 0.004f, 0.04f);
            float[] notes = { 659f, 880f, 1175f };
            for (int i = 0; i < notes.Length; i++) Tone(b, 0.22f + i * 0.07f, 0.4f, notes[i], notes[i] * 1.01f, 0.28f, 0.004f, 0.12f, 0.2f);
            Hiss(b, 0.22f, 0.3f, 0.2f, 0.05f, 0.1f, 5000, 7000, 3000);
            return Normalize(b, 0.65f);
        }

        static float[] Pick()
        {
            var b = Buf(0.6f);
            Tone(b, 0, 0.06f, 420, 700, 0.6f, 0.001f, 0.02f, 0.3f);         // pluck
            Ding(b, 0.05f, 1318f, 0.4f, 0.12f);
            Ding(b, 0.12f, 1760f, 0.3f, 0.12f);
            return Normalize(b, 0.6f);
        }

        static float[] Dig()
        {
            var b = Buf(0.25f);
            Hiss(b, 0, 0.2f, 1f, 0.01f, 0.05f, 900, 400, 200);
            Crackle(b, 0.02f, 0.15f, 4, 0.5f, 9);
            Tone(b, 0, 0.06f, 110, 80, 0.3f, 0.002f, 0.02f);
            return Normalize(b, 0.55f);
        }

        static float[] Mend()
        {
            var b = Buf(0.9f);
            float[] notes = { 784f, 988f, 1318f };
            for (int i = 0; i < notes.Length; i++) Tone(b, i * 0.09f, 0.6f, notes[i], notes[i], 0.3f, 0.01f, 0.22f, 0.1f);
            Hiss(b, 0, 0.5f, 0.1f, 0.1f, 0.15f, 6000, 8000, 4000);
            return Normalize(b, 0.55f);
        }

        static float[] Death()
        {
            var b = Buf(1.3f);
            Crackle(b, 0, 0.6f, 18, 0.6f, 21);                               // clay cracks
            Hiss(b, 0.05f, 0.7f, 0.6f, 0.02f, 0.2f, 1800, 300, 150);         // crumbles
            Tone(b, 0.1f, 0.5f, 110, 50, 0.7f, 0.004f, 0.15f, 0.3f);
            float[] notes = { 523f, 466f, 392f };
            for (int i = 0; i < notes.Length; i++) Tone(b, 0.4f + i * 0.18f, 0.5f, notes[i], notes[i] * 0.98f, 0.25f, 0.01f, 0.2f, 0.15f);
            return Normalize(b, 0.75f);
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
