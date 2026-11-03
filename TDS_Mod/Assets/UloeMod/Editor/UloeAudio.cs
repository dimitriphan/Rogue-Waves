// Uloe asset generator: procedurally synthesised sound effects (44.1 kHz, 16-bit, mono like the game's clips).
// TEMPORARY SOUNDS: electric zaps (buzzing saws), glass (inharmonic tings), a plug going in (a clack and a rising hum).
// Loudness is set at runtime by UloeData.SoundVolume (Code/02_Data.cs) to sit with the game's own sounds. The footstep and
// landing in game are the template's own (Code/04_Content.cs); these two are written for a later swap.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UloeBuild
{
    public static class UloeAudio
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
                new Sfx { Name = "Uloe_Zap", Use = "Uloe's spark, a Photon's hit", Make = Zap },
                new Sfx { Name = "Uloe_Beam", Use = "A Magnifying Glass fires its beam", Make = Beam },
                new Sfx { Name = "Uloe_Plugin", Use = "PLUG IN: a plug clacks home, the bulb hums up", Make = Plugin },
                new Sfx { Name = "Uloe_Unplug", Use = "Pulling the plug, the cord snapping", Make = Unplug },
                new Sfx { Name = "Uloe_Flash", Use = "A lit Mirror flashes at what hit it", Make = Flash },
                new Sfx { Name = "Uloe_Pop", Use = "A Photon appears or fades", Make = Pop },
                new Sfx { Name = "Uloe_Burst", Use = "Short Circuit: a Photon bursts", Make = Burst },
                new Sfx { Name = "Uloe_Nullify", Use = "Glare: a hit does nothing", Make = Nullify },
                new Sfx { Name = "Uloe_Build", Use = "A building goes up, an upgrade lands", Make = Build },
                new Sfx { Name = "Uloe_Strike", Use = "A Flood Light strikes its node", Make = Strike },
                new Sfx { Name = "Uloe_Landing", Use = "Vessel lands (a clunk, glass rattling)", Make = Landing },
                new Sfx { Name = "Uloe_Footstep", Use = "The lamp's foot comes down", Make = Footstep },
                new Sfx { Name = "Uloe_Death", Use = "Vessel death: the bulb pops", Make = Death },
            };
        }

        // ---------------------------------------------------------------- building blocks (as Sprigg's)

        static float[] Buf(float seconds) { return new float[Mathf.CeilToInt(seconds * Rate)]; }
        static System.Random rng = new System.Random(17);
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

        // A small glassy ting: inharmonic partials, quick strike, short ring.
        static void Ding(float[] b, float start, float f, float amp, float decay)
        {
            Tone(b, start, decay * 4f, f, f, amp, 0.002f, decay, 0f, 0.5f);
            Tone(b, start, decay * 3f, f * 2.4f, f * 2.4f, amp * 0.35f, 0.002f, decay * 0.6f);
            Tone(b, start, decay * 2f, f * 4.1f, f * 4.1f, amp * 0.15f, 0.001f, decay * 0.3f);
        }

        // Mains buzz: a soft saw at f, its level following an envelope.
        static void Buzz(float[] b, float start, float dur, float f0, float f1, float amp, float attack, float decay)
        {
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            double phase = 0;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(f0, f1, Mathf.Clamp01(t / dur));
                phase += f / Rate;
                float saw = (float)(2.0 * (phase - Math.Floor(phase)) - 1.0);
                b[s0 + i] += saw * amp * Env(t, attack, decay);
            }
        }

        // A crackle: a run of tiny random clicks.
        static void Crackle(float[] b, float start, float dur, int clicks, float amp)
        {
            for (int i = 0; i < clicks; i++)
            {
                float at = start + (float)rng.NextDouble() * dur;
                Hiss(b, at, 0.006f, amp, 0.0003f, 0.0015f, 9000, 6000, 3000);
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

        static float[] Zap()
        {
            var b = Buf(0.22f);
            Buzz(b, 0, 0.16f, 880, 440, 0.5f, 0.002f, 0.05f);
            Crackle(b, 0, 0.12f, 12, 0.7f);
            Tone(b, 0, 0.1f, 1760, 880, 0.25f, 0.001f, 0.03f);
            return Normalize(b, 0.6f);
        }

        // A hot thin hum that brightens and is gone.
        static float[] Beam()
        {
            var b = Buf(0.3f);
            Tone(b, 0, 0.26f, 1320, 1760, 0.5f, 0.01f, 0.08f, 0.3f);
            Hiss(b, 0, 0.2f, 0.25f, 0.01f, 0.06f, 7000, 9000, 3000);
            return Normalize(b, 0.5f);
        }

        // A plug going home: a clack, a little spit of crackle, then the mains hum climbing up to full.
        static float[] Plugin()
        {
            var b = Buf(0.8f);
            Hiss(b, 0, 0.03f, 1f, 0.0005f, 0.008f, 3500, 1500, 500);
            Tone(b, 0, 0.05f, 300, 180, 0.6f, 0.0005f, 0.015f);
            Crackle(b, 0.03f, 0.15f, 14, 0.5f);
            Buzz(b, 0.08f, 0.7f, 60, 120, 0.25f, 0.2f, 0.3f);
            Tone(b, 0.1f, 0.65f, 240, 960, 0.2f, 0.25f, 0.25f, 0.3f);
            Ding(b, 0.45f, 2093f, 0.2f, 0.06f);
            return Normalize(b, 0.7f);
        }

        // The plug comes out: a click, the hum falling away.
        static float[] Unplug()
        {
            var b = Buf(0.45f);
            Hiss(b, 0, 0.02f, 0.9f, 0.0005f, 0.006f, 5000, 2500, 800);
            Tone(b, 0.01f, 0.35f, 640, 120, 0.4f, 0.002f, 0.1f, 0.2f);
            Crackle(b, 0, 0.1f, 8, 0.4f);
            return Normalize(b, 0.6f);
        }

        // A camera flash: the capacitor's whine going up, a pop of air.
        static float[] Flash()
        {
            var b = Buf(0.3f);
            Tone(b, 0, 0.14f, 2000, 5200, 0.35f, 0.04f, 0.06f);
            Hiss(b, 0.1f, 0.14f, 0.7f, 0.001f, 0.03f, 6000, 2000, 1200);
            return Normalize(b, 0.55f);
        }

        static float[] Pop()
        {
            var b = Buf(0.18f);
            Tone(b, 0, 0.14f, 660, 1320, 0.7f, 0.003f, 0.05f, 0.2f);
            Crackle(b, 0, 0.06f, 4, 0.3f);
            return Normalize(b, 0.5f);
        }

        // A bulb bursts: a sharp crack, tinkling glass, a thump of air.
        static float[] Burst()
        {
            var b = Buf(0.7f);
            Hiss(b, 0, 0.06f, 1f, 0.0005f, 0.015f, 8000, 3000, 1000);
            Tone(b, 0, 0.3f, 110, 50, 0.8f, 0.002f, 0.08f);
            float[] tings = { 3136f, 3951f, 2637f, 4699f, 3520f };
            for (int i = 0; i < tings.Length; i++) Ding(b, 0.05f + i * 0.07f, tings[i], 0.2f - i * 0.02f, 0.05f);
            Crackle(b, 0.02f, 0.25f, 18, 0.45f);
            return Normalize(b, 0.8f);
        }

        // A hit turned aside: a clear ring off glass.
        static float[] Nullify()
        {
            var b = Buf(0.5f);
            Ding(b, 0, 2349f, 0.7f, 0.08f);
            Ding(b, 0.04f, 3520f, 0.3f, 0.06f);
            return Normalize(b, 0.6f);
        }

        // A bulb screwed in: three squeaky turns and a clink.
        static float[] Build()
        {
            var b = Buf(0.5f);
            for (int i = 0; i < 3; i++)
            {
                Tone(b, i * 0.07f, 0.05f, 1400 + i * 200, 1800 + i * 200, 0.2f, 0.004f, 0.015f);
                Hiss(b, i * 0.07f, 0.05f, 0.3f, 0.004f, 0.015f, 5000, 3000, 2000);
            }
            Ding(b, 0.24f, 1760f, 0.5f, 0.07f);
            return Normalize(b, 0.6f);
        }

        static float[] Strike()
        {
            var b = Buf(0.18f);
            Ding(b, 0, 1046f, 0.5f, 0.03f);
            Hiss(b, 0, 0.04f, 0.5f, 0.001f, 0.01f, 3000, 1200, 500);
            return Normalize(b, 0.5f);
        }

        static float[] Landing()
        {
            var b = Buf(0.6f);
            Hiss(b, 0, 0.2f, 1f, 0.004f, 0.05f, 1400, 300);
            Tone(b, 0, 0.25f, 110, 55, 0.8f, 0.003f, 0.08f);
            Ding(b, 0.05f, 3136f, 0.2f, 0.05f);
            Ding(b, 0.14f, 2637f, 0.15f, 0.05f);
            return Normalize(b);
        }

        static float[] Footstep()
        {
            var b = Buf(0.1f);
            Tone(b, 0, 0.06f, 160, 90, 0.5f, 0.001f, 0.015f);
            Ding(b, 0.005f, 1568f, 0.12f, 0.015f);
            return Normalize(b, 0.5f);
        }

        // The filament goes: a buzz, a pop, glass on the floor.
        static float[] Death()
        {
            var b = Buf(1.1f);
            Buzz(b, 0, 0.25f, 120, 60, 0.4f, 0.01f, 0.1f);
            Hiss(b, 0.22f, 0.08f, 1f, 0.0005f, 0.02f, 8000, 3000, 1000);
            float[] tings = { 3951f, 3136f, 4186f, 2637f, 3520f, 2349f };
            for (int i = 0; i < tings.Length; i++) Ding(b, 0.28f + i * 0.09f, tings[i], 0.25f - i * 0.03f, 0.06f);
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
