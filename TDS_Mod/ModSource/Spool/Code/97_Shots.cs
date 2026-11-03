#if SPOOL_SHOTS
// Debug build only (-Define SPOOL_SHOTS, with SPOOL_SELFTEST): captures TDS's own frame (never the desktop) at
// WaitForEndOfFrame and logs it as "[SpoolDump] <name> <w>x<h> <part>/<parts> <base64 RGBA, bottom row first>"
// lines; ModSource/Spool/Tools/Decode-Dump.ps1 turns them back into PNGs.
public static class SpoolShots
{
    const int Chunk = 8000;

    static void Emit(string name, int w, int h, Color32[] px)
    {
        byte[] bytes = new byte[w * h * 4];
        for (int i = 0; i < px.Length && i < w * h; i++)
        {
            bytes[i * 4] = px[i].r; bytes[i * 4 + 1] = px[i].g; bytes[i * 4 + 2] = px[i].b; bytes[i * 4 + 3] = px[i].a;
        }
        string b64 = Convert.ToBase64String(bytes);
        int parts = (b64.Length + Chunk - 1) / Chunk;
        for (int p = 0; p < parts; p++)
            Debug.Log("[SpoolDump] " + name + " " + w + "x" + h + " " + (p + 1) + "/" + parts + " " + b64.Substring(p * Chunk, Math.Min(Chunk, b64.Length - p * Chunk)));
    }

    public static IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        int w = Screen.width, h = Screen.height;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        int k = Mathf.Max(1, Mathf.CeilToInt(w / 960f));
        int ow = w / k, oh = h / k;
        Color32[] src = tex.GetPixels32();
        UnityEngine.Object.Destroy(tex);
        Color32[] dst = new Color32[ow * oh];
        for (int y = 0; y < oh; y++)
            for (int x = 0; x < ow; x++)
                dst[y * ow + x] = src[(y * k) * w + x * k];
        Emit("shot_" + name, ow, oh, dst);
        Debug.Log("[SpoolDump] INFO captured " + name);
    }

    // A close-up from a temporary camera that copies the game camera's settings and heading, tipped down steeply and
    // placed 'distance' back from 'target', rendered into a texture (the game's own camera is left alone). Argo 1.0's.
    public static IEnumerator Close(string name, Vector3 target, float distance)
    {
        yield return new WaitForEndOfFrame();
        Camera main = Camera.main;
        GameObject go = new GameObject("Spool_Shot_Camera");
        Camera cam = go.AddComponent<Camera>();
        if (main != null)
            cam.CopyFrom(main);
        Vector3 heading = main != null ? Vector3.ProjectOnPlane(main.transform.forward, Vector3.up).normalized : new Vector3(0.6f, 0f, 0.8f);
        Vector3 fwd = (heading * 0.55f + Vector3.down).normalized;
        cam.transform.rotation = Quaternion.LookRotation(fwd, heading);
        cam.transform.position = target - fwd * distance;
        if (cam.orthographic)
            cam.orthographicSize = distance * 0.4f;
        cam.nearClipPlane = 0.05f;
        const int w = 960, h = 540;
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        try { cam.Render(); }
        catch (Exception e) { Debug.Log("[SpoolDump] INFO close-up failed: " + e.Message); }
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        UnityEngine.Object.Destroy(go);
        Color32[] px = tex.GetPixels32();
        UnityEngine.Object.Destroy(tex);
        Emit("close_" + name, w, h, px);
        Debug.Log("[SpoolDump] INFO captured close-up " + name);
    }
}
#endif
