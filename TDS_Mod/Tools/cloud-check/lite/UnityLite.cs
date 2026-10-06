// A small working subset of UnityEngine, so the vessel generators' editor code (Px, Character, Lowpoly, Models, Audio)
// runs under Mono here and its output can be looked at. Behaviour matches Unity for everything those files use.
using System;
using System.Collections.Generic;
using System.IO;

namespace UnityEngine
{
    public static class Mathf
    {
        public const float PI = (float)Math.PI, Deg2Rad = PI / 180f, Rad2Deg = 180f / PI, Infinity = float.PositiveInfinity, Epsilon = 1.401298E-45f;
        public static float Abs(float f) { return Math.Abs(f); }
        public static int Abs(int f) { return Math.Abs(f); }
        public static float Min(float a, float b) { return a < b ? a : b; }
        public static float Min(params float[] v) { float m = v[0]; foreach (var x in v) if (x < m) m = x; return m; }
        public static int Min(int a, int b) { return a < b ? a : b; }
        public static int Min(params int[] v) { int m = v[0]; foreach (var x in v) if (x < m) m = x; return m; }
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static float Max(params float[] v) { float m = v[0]; foreach (var x in v) if (x > m) m = x; return m; }
        public static int Max(int a, int b) { return a > b ? a : b; }
        public static int Max(params int[] v) { int m = v[0]; foreach (var x in v) if (x > m) m = x; return m; }
        public static float Clamp(float v, float a, float b) { return v < a ? a : v > b ? b : v; }
        public static int Clamp(int v, int a, int b) { return v < a ? a : v > b ? b : v; }
        public static float Clamp01(float v) { return v < 0f ? 0f : v > 1f ? 1f : v; }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static float LerpUnclamped(float a, float b, float t) { return a + (b - a) * t; }
        public static float InverseLerp(float a, float b, float v) { return a != b ? Clamp01((v - a) / (b - a)) : 0f; }
        public static float Pow(float f, float p) { return (float)Math.Pow(f, p); }
        public static float Sqrt(float f) { return (float)Math.Sqrt(f); }
        public static float Sin(float f) { return (float)Math.Sin(f); }
        public static float Cos(float f) { return (float)Math.Cos(f); }
        public static float Tan(float f) { return (float)Math.Tan(f); }
        public static float Atan(float f) { return (float)Math.Atan(f); }
        public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public static float Asin(float f) { return (float)Math.Asin(f); }
        public static float Acos(float f) { return (float)Math.Acos(f); }
        public static float Exp(float f) { return (float)Math.Exp(f); }
        public static float Log(float f) { return (float)Math.Log(f); }
        public static float Log(float f, float b) { return (float)Math.Log(f, b); }
        public static float Sign(float f) { return f >= 0f ? 1f : -1f; }
        public static float Round(float f) { return (float)Math.Round(f, MidpointRounding.ToEven); }
        public static int RoundToInt(float f) { return (int)Math.Round(f, MidpointRounding.ToEven); }
        public static float Floor(float f) { return (float)Math.Floor(f); }
        public static int FloorToInt(float f) { return (int)Math.Floor(f); }
        public static float Ceil(float f) { return (float)Math.Ceiling(f); }
        public static int CeilToInt(float f) { return (int)Math.Ceiling(f); }
        public static float Repeat(float t, float len) { return Clamp(t - Floor(t / len) * len, 0f, len); }
        public static float PingPong(float t, float len) { t = Repeat(t, len * 2f); return len - Abs(t - len); }
        public static float SmoothStep(float a, float b, float t) { t = Clamp01(t); t = -2f * t * t * t + 3f * t * t; return b * t + a * (1f - t); }
        public static bool Approximately(float a, float b) { return Abs(b - a) < Max(1E-06f * Max(Abs(a), Abs(b)), Epsilon * 8f); }
        public static float MoveTowards(float c, float t, float d) { return Abs(t - c) <= d ? t : c + Sign(t - c) * d; }
        public static float PerlinNoise(float x, float y) { double v = Math.Sin(x * 12.9898 + y * 78.233) * 43758.5453; return (float)(v - Math.Floor(v)); }
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static Vector2 one { get { return new Vector2(1, 1); } }
        public static Vector2 up { get { return new Vector2(0, 1); } }
        public static Vector2 right { get { return new Vector2(1, 0); } }
        public float magnitude { get { return Mathf.Sqrt(x * x + y * y); } }
        public float sqrMagnitude { get { return x * x + y * y; } }
        public Vector2 normalized { get { float m = magnitude; return m > 1E-05f ? new Vector2(x / m, y / m) : zero; } }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator -(Vector2 a) { return new Vector2(-a.x, -a.y); }
        public static Vector2 operator *(Vector2 a, float d) { return new Vector2(a.x * d, a.y * d); }
        public static Vector2 operator *(float d, Vector2 a) { return new Vector2(a.x * d, a.y * d); }
        public static Vector2 operator /(Vector2 a, float d) { return new Vector2(a.x / d, a.y / d); }
        public static float Dot(Vector2 a, Vector2 b) { return a.x * b.x + a.y * b.y; }
        public static float Distance(Vector2 a, Vector2 b) { return (a - b).magnitude; }
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { t = Mathf.Clamp01(t); return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t); }
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
        public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0); }
        public override string ToString() { return "(" + x.ToString("F2") + ", " + y.ToString("F2") + ")"; }
    }

    public struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public static Vector2Int operator +(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x + b.x, a.y + b.y); }
    }

    public struct Vector3Int
    {
        public int x, y, z;
        public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
        public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
        public static Vector3 one { get { return new Vector3(1, 1, 1); } }
        public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public static Vector3 down { get { return new Vector3(0, -1, 0); } }
        public static Vector3 right { get { return new Vector3(1, 0, 0); } }
        public static Vector3 left { get { return new Vector3(-1, 0, 0); } }
        public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
        public static Vector3 back { get { return new Vector3(0, 0, -1); } }
        public float this[int i] { get { return i == 0 ? x : i == 1 ? y : z; } set { if (i == 0) x = value; else if (i == 1) y = value; else z = value; } }
        public float magnitude { get { return Mathf.Sqrt(x * x + y * y + z * z); } }
        public float sqrMagnitude { get { return x * x + y * y + z * z; } }
        public Vector3 normalized { get { float m = magnitude; return m > 1E-05f ? new Vector3(x / m, y / m, z / m) : zero; } }
        public void Normalize() { this = normalized; }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator -(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
        public static Vector3 operator *(Vector3 a, float d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator *(float d, Vector3 a) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator /(Vector3 a, float d) { return new Vector3(a.x / d, a.y / d, a.z / d); }
        public static bool operator ==(Vector3 a, Vector3 b) { return (a - b).sqrMagnitude < 9.99999944E-11f; }
        public static bool operator !=(Vector3 a, Vector3 b) { return !(a == b); }
        public override bool Equals(object o) { return o is Vector3 && this == (Vector3)o; }
        public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode() << 2 ^ z.GetHashCode() >> 2; }
        public static float Dot(Vector3 a, Vector3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
        public static Vector3 Cross(Vector3 a, Vector3 b) { return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x); }
        public static float Distance(Vector3 a, Vector3 b) { return (a - b).magnitude; }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { t = Mathf.Clamp01(t); return a + (b - a) * t; }
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) { return a + (b - a) * t; }
        public static Vector3 Scale(Vector3 a, Vector3 b) { return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z); }
        public static Vector3 Normalize(Vector3 v) { return v.normalized; }
        public static Vector3 Min(Vector3 a, Vector3 b) { return new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z)); }
        public static Vector3 Max(Vector3 a, Vector3 b) { return new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z)); }
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 n) { float m = Dot(n, n); return m < 1e-12f ? v : v - n * (Dot(v, n) / m); }
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t) { return Lerp(a, b, t).normalized * Mathf.Lerp(a.magnitude, b.magnitude, t); }
        public override string ToString() { return "(" + x.ToString("F2") + ", " + y.ToString("F2") + ", " + z.ToString("F2") + ")"; }
        public string ToString(string f) { return "(" + x.ToString(f) + ", " + y.ToString(f) + ", " + z.ToString(f) + ")"; }
    }

    public struct Vector4 { public float x, y, z, w; public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; } }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity { get { return new Quaternion(0, 0, 0, 1); } }
        // Unity's Euler: rotate around z, then x, then y (applied to a vector in that order).
        public static Quaternion Euler(float ex, float ey, float ez)
        {
            Quaternion qx = AngleAxis(ex, Vector3.right), qy = AngleAxis(ey, Vector3.up), qz = AngleAxis(ez, Vector3.forward);
            return qy * qx * qz;
        }
        public static Quaternion Euler(Vector3 e) { return Euler(e.x, e.y, e.z); }
        public static Quaternion AngleAxis(float deg, Vector3 axis)
        {
            axis = axis.normalized; float h = deg * Mathf.Deg2Rad * 0.5f, s = Mathf.Sin(h);
            return new Quaternion(axis.x * s, axis.y * s, axis.z * s, Mathf.Cos(h));
        }
        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            return new Quaternion(a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y, a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z,
                                  a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x, a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        }
        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            Vector3 u = new Vector3(q.x, q.y, q.z);
            Vector3 t = Vector3.Cross(u, v) * 2f;
            return v + t * q.w + Vector3.Cross(u, t);
        }
        public static Quaternion Inverse(Quaternion q) { return new Quaternion(-q.x, -q.y, -q.z, q.w); }
        public static Quaternion LookRotation(Vector3 fwd, Vector3 up)
        {
            fwd = fwd.normalized; Vector3 r = Vector3.Cross(up, fwd).normalized; Vector3 u = Vector3.Cross(fwd, r);
            float m00 = r.x, m01 = u.x, m02 = fwd.x, m10 = r.y, m11 = u.y, m12 = fwd.y, m20 = r.z, m21 = u.z, m22 = fwd.z;
            float tr = m00 + m11 + m22; Quaternion q;
            if (tr > 0) { float s = Mathf.Sqrt(tr + 1f) * 2f; q = new Quaternion((m21 - m12) / s, (m02 - m20) / s, (m10 - m01) / s, 0.25f * s); }
            else if (m00 > m11 && m00 > m22) { float s = Mathf.Sqrt(1f + m00 - m11 - m22) * 2f; q = new Quaternion(0.25f * s, (m01 + m10) / s, (m02 + m20) / s, (m21 - m12) / s); }
            else if (m11 > m22) { float s = Mathf.Sqrt(1f + m11 - m00 - m22) * 2f; q = new Quaternion((m01 + m10) / s, 0.25f * s, (m12 + m21) / s, (m02 - m20) / s); }
            else { float s = Mathf.Sqrt(1f + m22 - m00 - m11) * 2f; q = new Quaternion((m02 + m20) / s, (m12 + m21) / s, 0.25f * s, (m10 - m01) / s); }
            return q;
        }
        public static Quaternion LookRotation(Vector3 fwd) { return LookRotation(fwd, Vector3.up); }
        public static Quaternion FromToRotation(Vector3 a, Vector3 b)
        {
            a = a.normalized; b = b.normalized; Vector3 c = Vector3.Cross(a, b); float d = Vector3.Dot(a, b);
            if (d < -0.9999f) { Vector3 ax = Vector3.Cross(Vector3.right, a); if (ax.sqrMagnitude < 1e-6f) ax = Vector3.Cross(Vector3.up, a); return AngleAxis(180f, ax); }
            Quaternion q = new Quaternion(c.x, c.y, c.z, 1f + d); float m = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return new Quaternion(q.x / m, q.y / m, q.z / m, q.w / m);
        }
    }

    public struct Matrix4x4
    {
        public float m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33;
        public static Matrix4x4 identity { get { var m = new Matrix4x4(); m.m00 = m.m11 = m.m22 = m.m33 = 1f; return m; } }
        public static Matrix4x4 TRS(Vector3 p, Quaternion q, Vector3 s)
        {
            Vector3 cx = q * new Vector3(s.x, 0, 0), cy = q * new Vector3(0, s.y, 0), cz = q * new Vector3(0, 0, s.z);
            var m = new Matrix4x4();
            m.m00 = cx.x; m.m10 = cx.y; m.m20 = cx.z; m.m01 = cy.x; m.m11 = cy.y; m.m21 = cy.z; m.m02 = cz.x; m.m12 = cz.y; m.m22 = cz.z;
            m.m03 = p.x; m.m13 = p.y; m.m23 = p.z; m.m33 = 1f;
            return m;
        }
        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
        {
            var r = new Matrix4x4();
            float[,] A = a.Arr(), B = b.Arr(), R = new float[4, 4];
            for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) { float s = 0; for (int k = 0; k < 4; k++) s += A[i, k] * B[k, j]; R[i, j] = s; }
            r.Set(R); return r;
        }
        float[,] Arr() { return new float[,] { { m00, m01, m02, m03 }, { m10, m11, m12, m13 }, { m20, m21, m22, m23 }, { m30, m31, m32, m33 } }; }
        void Set(float[,] R) { m00 = R[0, 0]; m01 = R[0, 1]; m02 = R[0, 2]; m03 = R[0, 3]; m10 = R[1, 0]; m11 = R[1, 1]; m12 = R[1, 2]; m13 = R[1, 3]; m20 = R[2, 0]; m21 = R[2, 1]; m22 = R[2, 2]; m23 = R[2, 3]; m30 = R[3, 0]; m31 = R[3, 1]; m32 = R[3, 2]; m33 = R[3, 3]; }
        public Vector3 MultiplyPoint3x4(Vector3 p) { return new Vector3(m00 * p.x + m01 * p.y + m02 * p.z + m03, m10 * p.x + m11 * p.y + m12 * p.z + m13, m20 * p.x + m21 * p.y + m22 * p.z + m23); }
        public Vector3 MultiplyPoint(Vector3 p) { return MultiplyPoint3x4(p); }
        public Vector3 MultiplyVector(Vector3 p) { return new Vector3(m00 * p.x + m01 * p.y + m02 * p.z, m10 * p.x + m11 * p.y + m12 * p.z, m20 * p.x + m21 * p.y + m22 * p.z); }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }
        public static Color white { get { return new Color(1, 1, 1, 1); } }
        public static Color black { get { return new Color(0, 0, 0, 1); } }
        public static Color clear { get { return new Color(0, 0, 0, 0); } }
        public static Color operator *(Color c, float d) { return new Color(c.r * d, c.g * d, c.b * d, c.a * d); }
        public static Color operator *(Color c, Color d) { return new Color(c.r * d.r, c.g * d.g, c.b * d.b, c.a * d.a); }
        public static Color operator +(Color c, Color d) { return new Color(c.r + d.r, c.g + d.g, c.b + d.b, c.a + d.a); }
        public static Color Lerp(Color a, Color b, float t) { t = Mathf.Clamp01(t); return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t); }
        public static implicit operator Color32(Color c)
        {
            return new Color32((byte)Math.Round(Mathf.Clamp01(c.r) * 255f), (byte)Math.Round(Mathf.Clamp01(c.g) * 255f), (byte)Math.Round(Mathf.Clamp01(c.b) * 255f), (byte)Math.Round(Mathf.Clamp01(c.a) * 255f));
        }
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color(Color32 c) { return new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f); }
        public static Color32 Lerp(Color32 a, Color32 b, float t) { t = Mathf.Clamp01(t); return new Color32((byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t), (byte)(a.b + (b.b - a.b) * t), (byte)(a.a + (b.a - a.a) * t)); }
        public override string ToString() { return "RGBA(" + r + ", " + g + ", " + b + ", " + a + ")"; }
    }

    public static class ColorUtility
    {
        public static bool TryParseHtmlString(string s, out Color c)
        {
            c = new Color(0, 0, 0, 1);
            if (string.IsNullOrEmpty(s) || s[0] != '#') return false;
            string h = s.Substring(1);
            if (h.Length != 6 && h.Length != 8) return false;
            Func<int, float> P = i => Convert.ToInt32(h.Substring(i, 2), 16) / 255f;
            c = new Color(P(0), P(2), P(4), h.Length == 8 ? P(6) : 1f);
            return true;
        }
    }

    public struct Bounds
    {
        public Vector3 center, extents;
        public Bounds(Vector3 c, Vector3 size) { center = c; extents = size * 0.5f; }
        public Vector3 size { get { return extents * 2f; } }
        public Vector3 min { get { return center - extents; } }
        public Vector3 max { get { return center + extents; } }
        public void Encapsulate(Vector3 p) { Vector3 mn = Vector3.Min(min, p), mx = Vector3.Max(max, p); center = (mn + mx) * 0.5f; extents = (mx - mn) * 0.5f; }
    }
    public struct Rect { public float x, y, width, height; public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; } }

    public class Object
    {
        public string name = "";
        public static void DestroyImmediate(Object o) { }
        public static void Destroy(Object o) { }
        public static void Destroy(Object o, float t) { }
    }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureFormat { RGBA32 = 4, RGB24 = 3, ARGB32 = 5 }
    public enum TextureWrapMode { Repeat, Clamp, Mirror, MirrorOnce }

    public class Texture2D : Object
    {
        public readonly int width, height;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        Color32[] px;
        public Texture2D(int w, int h, TextureFormat f = TextureFormat.RGBA32, bool mip = false) { width = w; height = h; px = new Color32[w * h]; }
        public void SetPixels32(Color32[] c) { Array.Copy(c, px, px.Length); }
        public Color32[] GetPixels32() { return (Color32[])px.Clone(); }
        public void SetPixel(int x, int y, Color c) { px[y * width + x] = c; }
        public Color GetPixel(int x, int y) { return px[y * width + x]; }
        public void Apply() { }
        public void Apply(bool b) { }
        // Rows are stored bottom-up like Unity's; PNG rows go top-down.
        public byte[] EncodeToPNG() { return Png.Encode(width, height, (x, y) => px[(height - 1 - y) * width + x]); }
    }

    public static class ImageConversion { public static byte[] EncodeToPNG(this Texture2D t) { return t.EncodeToPNG(); } }

    public enum IndexFormat { UInt16, UInt32 }
    public class Mesh : Object
    {
        public List<Vector3> V = new List<Vector3>(), N = new List<Vector3>();
        public List<Vector2> UV = new List<Vector2>();
        public List<int> Tris = new List<int>();
        public UnityEngine.Rendering.IndexFormat indexFormat;
        public Bounds bounds;
        public void SetVertices(List<Vector3> v) { V = new List<Vector3>(v); }
        public void SetNormals(List<Vector3> n) { N = new List<Vector3>(n); }
        public void SetUVs(int ch, List<Vector2> uv) { UV = new List<Vector2>(uv); }
        public void SetTriangles(List<int> t, int sub) { Tris = new List<int>(t); }
        public Vector3[] vertices { get { return V.ToArray(); } set { V = new List<Vector3>(value); } }
        public int vertexCount { get { return V.Count; } }
        public void RecalculateBounds()
        {
            if (V.Count == 0) { bounds = new Bounds(); return; }
            Vector3 mn = V[0], mx = V[0];
            foreach (var p in V) { mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p); }
            bounds = new Bounds((mn + mx) * 0.5f, mx - mn);
        }
    }

    public static class Debug
    {
        public static void Log(object m) { Console.WriteLine(m); }
        public static void LogWarning(object m) { Console.WriteLine("WARN " + m); }
        public static void LogError(object m) { Console.WriteLine("ERROR " + m); }
    }

    public static class Random
    {
        static System.Random r = new System.Random(1);
        public static float Range(float a, float b) { return a + (float)r.NextDouble() * (b - a); }
        public static int Range(int a, int b) { return r.Next(a, b); }
        public static float value { get { return (float)r.NextDouble(); } }
    }

    public static class Png
    {
        public static byte[] Encode(int w, int h, Func<int, int, Color32> at)
        {
            var raw = new MemoryStream();
            for (int y = 0; y < h; y++)
            {
                raw.WriteByte(0);
                for (int x = 0; x < w; x++) { var c = at(x, y); raw.WriteByte(c.r); raw.WriteByte(c.g); raw.WriteByte(c.b); raw.WriteByte(c.a); }
            }
            byte[] data = raw.ToArray();
            var o = new MemoryStream();
            o.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
            var ihdr = new MemoryStream();
            BE(ihdr, w); BE(ihdr, h); ihdr.WriteByte(8); ihdr.WriteByte(6); ihdr.WriteByte(0); ihdr.WriteByte(0); ihdr.WriteByte(0);
            Chunk(o, "IHDR", ihdr.ToArray());
            Chunk(o, "IDAT", Zlib(data));
            Chunk(o, "IEND", new byte[0]);
            return o.ToArray();
        }
        static void BE(Stream s, int v) { s.WriteByte((byte)(v >> 24)); s.WriteByte((byte)(v >> 16)); s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v); }
        static uint[] crcTable;
        static uint Crc(byte[] b)
        {
            if (crcTable == null) { crcTable = new uint[256]; for (uint n = 0; n < 256; n++) { uint c = n; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xedb88320u ^ (c >> 1) : c >> 1; crcTable[n] = c; } }
            uint crc = 0xffffffffu; foreach (byte x in b) crc = crcTable[(crc ^ x) & 0xff] ^ (crc >> 8); return crc ^ 0xffffffffu;
        }
        static void Chunk(Stream o, string tag, byte[] d)
        {
            BE(o, d.Length);
            byte[] t = System.Text.Encoding.ASCII.GetBytes(tag);
            var all = new byte[t.Length + d.Length]; Array.Copy(t, all, 4); Array.Copy(d, 0, all, 4, d.Length);
            o.Write(all, 0, all.Length); BE(o, (int)Crc(all));
        }
        static byte[] Zlib(byte[] data)
        {
            var o = new MemoryStream(); o.WriteByte(0x78); o.WriteByte(0x01);
            using (var z = new System.IO.Compression.DeflateStream(o, System.IO.Compression.CompressionMode.Compress, true)) z.Write(data, 0, data.Length);
            uint a = 1, b = 0; foreach (byte x in data) { a = (a + x) % 65521; b = (b + a) % 65521; }
            BE(o, (int)((b << 16) | a));
            return o.ToArray();
        }
    }
}
namespace UnityEngine.Rendering { public enum IndexFormat { UInt16, UInt32 } }
