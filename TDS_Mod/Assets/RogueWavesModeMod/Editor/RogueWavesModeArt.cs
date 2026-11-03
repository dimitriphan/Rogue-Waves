// Temporary art of the mode part, drawn by code in the pack's palette (docs/STYLE.md, docs/TEMP_ART.md): VENDCINT's sheet and
// the 12 x 12 icons of the shop, the votes and the starter Directions and Cardinals. Build All Assets writes each file into
// ModSource/RogueWavesMode/Art/Temporary/game_size/ unless it is there already, so a hand-made file of the same name stays.
// Final art goes one folder up and wins.
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RogueWavesModeBuild
{
    public static class RogueWavesModeArt
    {
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
        {
            { 'C', new Color32(0xE2, 0x72, 0x5B, 255) }, { 'c', new Color32(0xB5, 0x52, 0x40, 255) }, { 'k', new Color32(0xEE, 0x9A, 0x86, 255) },
            { 'D', new Color32(0x2E, 0x4A, 0x62, 255) }, { 'd', new Color32(0x22, 0x38, 0x4B, 255) },
            { 'F', new Color32(0xE9, 0xF1, 0xEE, 255) }, { 'f', new Color32(0xC3, 0xD8, 0xD3, 255) },
            { 'S', new Color32(0xD8, 0xC4, 0x9A, 255) }, { 's', new Color32(0xA8, 0x93, 0x6A, 255) }, { 'n', new Color32(0xEF, 0xE0, 0xBE, 255) },
            { 'T', new Color32(0x3F, 0x8F, 0x96, 255) }, { 't', new Color32(0x32, 0x73, 0x78, 255) }, { 'l', new Color32(0x6D, 0xB4, 0xB9, 255) },
            { 'P', new Color32(0xF4, 0xEE, 0xF8, 255) }, { 'p', new Color32(0xC9, 0xE4, 0xF2, 255) },
            { 'G', new Color32(0x6A, 0xBE, 0x30, 255) }, { 'g', new Color32(0x4F, 0x8E, 0x26, 255) },
            { 'Y', new Color32(0xE8, 0xC4, 0x6C, 255) }, { 'y', new Color32(0xC4, 0x9C, 0x46, 255) },
            { 'B', new Color32(0x8A, 0x90, 0x9C, 255) }, { 'b', new Color32(0x5F, 0x64, 0x70, 255) },
        };

        static readonly Dictionary<string, string[]> Icons = new Dictionary<string, string[]>
        {
            // The shop's buttons and the votes.
            { "RogueWaves_Shop", new[] { "..SSSSSSSS..", ".SnnnnnnnnS.", ".SDDDDDDDDS.", ".SDPdCdTdDS.", ".SDdddddDDS.", ".SDTdPdCdDS.", ".SDdddddDDS.", ".SDDDDDDDDS.", ".SnnnnnbbnS.", ".SdddddnnnS.", ".SSSSSSSSSS.", "..s......s.." } },
            { "RogueWaves_Fund", new[] { "............", "...CC.......", "..CCCCCCC...", ".CCCCCCCCC..", "..CCCCCCC...", "...CC.......", ".......TT...", "...TTTTTTT..", "..TTTTTTTTT.", "...TTTTTTT..", ".......TT...", "............" } },
            { "RogueWaves_Board", new[] { "............", ".ssssssssss.", ".sSSSCCSSSs.", ".sSSSCCSSSs.", ".sSbbbbbbSs.", ".sSSSSSSSSs.", ".sSbbbbbSSs.", ".sSSSSSSSSs.", ".sSbbbbbbSs.", ".sSSSSSSSSs.", ".ssssssssss.", "............" } },
            { "RogueWaves_Leave", new[] { ".....F......", ".....FF.....", ".....FFF....", ".....FFFF...", ".....FFFFF..", ".....FFFFFF.", ".....F......", "............", ".SSSSSSSSSS.", "..ssssssss..", "TTlTTTTlTTTT", "tTTTtTTTTtTT" } },
            { "RogueWaves_Stay", new[] { ".....BB.....", "....B..B....", ".....BB.....", ".....BB.....", "...BBBBBB...", ".....BB.....", ".....BB.....", "B....BB....B", "BB...BB...BB", ".BB..BB..BB.", "..BBBBBBBB..", "....BBBB...." } },
            // The starter Cardinals (docs/scope/12's NEEDS line, defaults).
            { "Cardinal_Hull", new[] { "....s..s....", "...sSssSs...", "..sSSSSSSs..", "s.SSSnSSSS.s", ".sSSnSSnSSs.", "s.SSSSSSSS.s", ".sSSnSSnSSs.", "s.SSSSSSSS.s", "..sSSSSSSs..", "...sSssSs...", "....s..s....", "............" } },
            { "Cardinal_Legs", new[] { "............", ".....FFF....", "....FFfFF...", "....FFFFF...", ".....FfF....", "...TTTTTTT..", "..TlTTTTTTT.", "..TTTTtTTTT.", "...t....t...", "..SSS..SSS..", "..sss..sss..", "............" } },
            { "Cardinal_Rising", new[] { "............", "............", "..........T.", ".........TT.", "........TTT.", ".......TTTT.", "......TTTTT.", ".....TlTTTT.", "....TTTTlTT.", "...TlTTTTTT.", "..TTTTlTTTT.", "DDDDDDDDDDDD" } },
            { "Cardinal_Puffer", new[] { "............", "...s.ss.s...", "..YYYYYYYY..", ".sYYYYFdYYs.", "..YYYYFFYY..", ".sYYYYYYYYYY", "..YnnnnYYY..", ".sYnnnnnYYs.", "..YYnnnYYY..", "...s.ss.s...", "............", "............" } },
            { "Cardinal_Clock", new[] { "...SSSSSS...", "....ssss....", "....FFFF....", "....FTTF....", ".....FF.....", ".....TF.....", "....F..F....", "...F.TT.F...", "...FTTTTF...", "...FTTTTF...", "....ssss....", "...SSSSSS..." } },
            { "Cardinal_Bell", new[] { ".....ss.....", "....s..s....", "....ccss....", "...cCCCcs...", "...CCkCCs...", "...CCCCcs...", "...CCCCcs...", "..CCCCCCcs..", "..cccccccs..", ".....Bb.....", ".....bb.....", "............" } },
            { "Cardinal_Wind", new[] { "............", ".FFFFFFF....", "........F...", "..FFFFF..F..", ".......F.F..", "FFFFFF.F.F..", "......FF..F.", ".FFFFFFFFF..", "..........F.", ".FFFFFFF..F.", ".........F..", "............" } },
            // The starter Directions (docs/scope/11's NEEDS line, defaults).
            { "Direction_Barnacle", new[] { "............", "....sSSs....", "...sSnnSs...", "..sSnddnSs..", "..SndddnnS..", "..SnndnnnS..", ".sSSnnnnSSs.", "sSSSSSSSSSSs", "DDDDDDDDDDDD", "dDDDdDDDdDDD", "............", "............" } },
            { "Direction_Mend", new[] { "............", "....FFFF....", "....FCCF....", "....FCCF....", ".FFFFCCFFFF.", ".FCCCCCCCCF.", ".FCCCCCCCCF.", ".FFFFCCFFFF.", "lTTTFCCFTTTl", "TTTlFFFFlTTT", "tTTTTTTTTTtT", "DDDDDDDDDDDD" } },
            { "Direction_Spring", new[] { "............", ".....PP.....", "....PPPP....", "...PPppPP...", ".....TT.....", "....TlTT....", "...TlTTTT...", "..TlTTTTTT..", ".TTTTTtTTTT.", "lTTTtTTTTTtT", "TTtTTTTtTTTT", "DDDDDDDDDDDD" } },
            { "Direction_Cost", new[] { "............", "....YYYY....", "...YyyyyY...", "..YyYYYyyY..", "..YyYyYYyY..", "..YyYYyyyY..", "..YyyyYYyY..", "..YyYYYyyY..", "...YyyyyY...", "....YYYY....", "............", "............" } },
            { "Direction_Refit", new[] { "..........b.", ".........bB.", "........bBb.", ".......bBb..", "......bBb...", ".....bBb....", "..b.bBb.....", "..BbBb......", "..BBb.......", ".bBb........", "bBb.........", "bb.........." } },
            { "Direction_Interest", new[] { "............", ".........YY.", ".........YY.", "......YY.YY.", "......YY.YY.", "...YY.YY.YY.", "...YY.YY.YY.", "YY.YY.YY.YY.", "YY.YY.YY.YY.", "yy.yy.yy.yy.", "............", "............" } },
            { "Direction_Gallop", new[] { "............", "..T.....SSS.", ".TT....SSSSS", "TTTT..SSSsSS", ".....SSSSSS.", "..TSSSSSSS..", ".TTSSSSSs...", "TTTSSSSSs...", "...SS..SS...", "...s....s...", "..ss....ss..", "............" } },
            { "Direction_Regulars", new[] { "............", "..ss..ss..ss", ".sSSssSSssSS", ".sSSssSSssSS", "..TT..CC..YY", ".TTTTCCCCYYY", ".TTTTCCCCYYY", ".TTTTCCCCYYY", ".t..tc..cy..", "............", "SSSSSSSSSSSS", "ssssssssssss" } },
            { "Direction_Longline", new[] { "...b........", "..bb........", ".b.b........", "...b.FFFFFFF", "...b........", "...b........", "...b.......F", "...b.......F", "...b......F.", "...b.....F..", "...b...SF...", "..sbs..S...." } },
            { "Direction_Packedsnow", new[] { "............", "............", "............", "....FFFF....", "...FFFFFF...", "..FFFfFFFF..", ".FFFFFFfFFF.", ".FfFFFFFFFF.", "FFFFFfFFFFFF", "fFFFFFFFfFFF", "ffffffffffff", "............" } },
            { "Direction_Skipping", new[] { "............", "............", "..SS........", ".SSSS.......", "..ss..S.....", "....T.......", "TTTTTT..SS..", "lTTTTTTSSSS.", "TTlTTTTTss.S", "TTTTlTTTTTT.", "tTTTTTTlTTTT", "DDDDDDDDDDDD" } },
        };

        // Icons for what a vessel part sells: the part registers the Direction, this library holds its picture.
        public static void WriteMissing(string folder)
        {
            Directory.CreateDirectory(folder);
            foreach (KeyValuePair<string, string[]> kv in Icons)
            {
                string path = Path.Combine(folder, kv.Key + ".png");
                if (!File.Exists(path))
                    File.WriteAllBytes(path, Icon(kv.Value));
            }
            string sheet = Path.Combine(folder, "Vendcint-Sheet.png");
            if (!File.Exists(sheet))
                File.WriteAllBytes(sheet, Vendcint());
        }

        static byte[] Icon(string[] rows)
        {
            int h = rows.Length, w = rows[0].Length;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color32 c;
                    t.SetPixel(x, h - 1 - y, x < rows[y].Length && Palette.TryGetValue(rows[y][x], out c) ? c : Clear);
                }
            byte[] png = t.EncodeToPNG();
            Object.DestroyImmediate(t);
            return png;
        }

        // ------------------------------------------------------------ VENDCINT: six frames, the same four rows

        const int Cell = 32, Cols = 6, Rows = 4;
        static Color32[] px;
        static int ox, oy;

        static void Put(int x, int y, char c)
        {
            int ax = ox + x, ay = oy + y;
            if (ax < 0 || ay < 0 || ax >= Cell * Cols || ay >= Cell * Rows)
                return;
            px[(Cell * Rows - 1 - ay) * Cell * Cols + ax] = c == '.' ? Clear : Palette[c];
        }

        static void Box(int x, int y, int w, int h, char c)
        {
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    Put(x + i, y + j, c);
        }

        static void Frame(int f)
        {
            // The cabinet: lit from the upper left, shade on the right and below; two stubby feet.
            Box(7, 3, 18, 26, 'S');
            Box(7, 3, 1, 26, 'n');
            Box(7, 3, 18, 1, 'n');
            Box(24, 4, 1, 25, 's');
            Box(7, 28, 18, 1, 's');
            Box(9, 29, 3, 2, 's');
            Box(20, 29, 3, 2, 's');
            // The face on its display: two foam eyes, and a mouth that hums.
            Box(9, 5, 14, 6, 'd');
            Box(9, 5, 14, 1, 'D');
            int look = f == 1 || f == 2 ? -1 : f == 4 ? 1 : 0;
            bool blink = f == 3;
            if (blink)
            {
                Box(12, 8, 2, 1, 'F');
                Box(18, 8, 2, 1, 'F');
            }
            else
            {
                Box(12 + look, 7, 2, 2, 'F');
                Box(18 + look, 7, 2, 2, 'F');
            }
            Box(14, 10, 4, 1, f % 2 == 0 ? 'l' : 'T');
            // The window: three shelves of washed-up goods; a Pure Aura pearl bobs on the middle shelf.
            Box(9, 12, 11, 12, 'D');
            Box(10, 13, 9, 10, 'd');
            Box(10, 16, 9, 1, 'D');
            Box(10, 19, 9, 1, 'D');
            Box(10, 22, 9, 1, 'D');
            Box(11, 14, 2, 2, 'C');
            Box(14, 14, 2, 2, 'T');
            Box(17, 14, 1, 2, 'Y');
            int bob = f == 0 || f == 5 ? 0 : f == 1 || f == 4 ? -1 : 0;
            Box(14, 17 + bob, 2, 2, 'P');
            Put(14, 17 + bob, 'p');
            Box(11, 17, 2, 2, 'l');
            Box(17, 17, 1, 2, 'C');
            Box(11, 20, 1, 2, 'F');
            Box(13, 20, 2, 2, 'Y');
            Box(16, 20, 2, 2, 'T');
            // Glass glint, sliding across.
            int g = 10 + (f * 2) % 8;
            Put(g, 13, 'l');
            Put(g + 1, 14, 'l');
            // The buttons and the coin slot; the tray below.
            Box(21, 13, 2, 2, 'C');
            Box(21, 16, 2, 2, 'T');
            Box(21, 19, 2, 1, 'b');
            Put(21 + (f % 2), 22, f % 3 == 0 ? 'Y' : 'y');
            Box(10, 25, 12, 3, 'b');
            Box(10, 25, 12, 1, 'd');
        }

        static byte[] Vendcint()
        {
            int w = Cell * Cols, h = Cell * Rows;
            px = new Color32[w * h];
            for (int r = 0; r < Rows; r++)
                for (int f = 0; f < Cols; f++)
                {
                    ox = f * Cell;
                    oy = r * Cell;
                    Frame(f);
                }
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.SetPixels32(px);
            t.Apply(false);
            byte[] png = t.EncodeToPNG();
            Object.DestroyImmediate(t);
            return png;
        }
    }
}
