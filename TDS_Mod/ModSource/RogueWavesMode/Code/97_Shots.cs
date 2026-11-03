#if ROGUEWAVESMODE_SHOTS
// Debug build only (-Define ROGUEWAVESMODE_SHOTS, with ROGUEWAVESMODE_SELFTEST): captures TDS's own frame (never the desktop) at
// WaitForEndOfFrame and logs it as "[RogueWavesModeDump] <name> <w>x<h> <part>/<parts> <base64 RGBA, bottom row first>"
// lines; ModSource/Spool/Tools/Decode-Dump.ps1 (with -Tag RogueWavesModeDump) turns them back into PNGs.
public static class RWShots
{
    const int Chunk = 8000;

    public static IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        int w = Screen.width, h = Screen.height;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        int k = Mathf.Max(1, Mathf.CeilToInt(w / 1280f));
        int ow = w / k, oh = h / k;
        Color32[] src = tex.GetPixels32();
        UnityEngine.Object.Destroy(tex);
        byte[] bytes = new byte[ow * oh * 4];
        for (int y = 0; y < oh; y++)
            for (int x = 0; x < ow; x++)
            {
                Color32 c = src[(y * k) * w + x * k];
                int i = (y * ow + x) * 4;
                bytes[i] = c.r; bytes[i + 1] = c.g; bytes[i + 2] = c.b; bytes[i + 3] = 255;
            }
        string b64 = Convert.ToBase64String(bytes);
        int parts = (b64.Length + Chunk - 1) / Chunk;
        for (int p = 0; p < parts; p++)
            Debug.Log("[RogueWavesModeDump] shot_" + name + " " + ow + "x" + oh + " " + (p + 1) + "/" + parts + " " + b64.Substring(p * Chunk, Math.Min(Chunk, b64.Length - p * Chunk)));
    }
}
#endif
