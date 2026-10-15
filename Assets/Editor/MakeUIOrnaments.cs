using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Tools → UI → Tạo icon hoa thị + đường trang trí: vẽ sẵn 2 ảnh PNG (nền trong suốt, màu trắng)
/// theo họa tiết song sắt cửa sổ chung cư cũ. Màu thật chỉnh bằng ô Color của Image trong Unity.
///   Assets/UI/HoaThi.png        — icon hoa thị 4 cánh (dùng 2 bên nút đang chọn)
///   Assets/UI/DuongTrangTri.png — đường kẻ ngang có hoa thị ở giữa (dưới tiêu đề)
/// </summary>
public static class MakeUIOrnaments
{
    const string Folder = "Assets/UI";

    [MenuItem("Tools/UI/Tạo icon hoa thị + đường trang trí")]
    static void Make()
    {
        Directory.CreateDirectory(Folder);
        Save(DrawQuatrefoil(128), Folder + "/HoaThi.png");
        Save(DrawDivider(1024, 96), Folder + "/DuongTrangTri.png");
        AssetDatabase.Refresh();
        foreach (string p in new[] { Folder + "/HoaThi.png", Folder + "/DuongTrangTri.png" })
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(p);
            imp.textureType = TextureImporterType.Sprite;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        Debug.Log("[UI] Đã tạo Assets/UI/HoaThi.png và Assets/UI/DuongTrangTri.png");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(Folder + "/HoaThi.png"));
    }

    // Hoa thị: 4 vòng tròn rỗng xếp chữ thập + dấu thập ở giữa (giống song sắt)
    static Texture2D DrawQuatrefoil(int size)
    {
        var tex = Blank(size, size);
        float c = size / 2f, r = size * 0.2f, stroke = size * 0.045f;
        Vector2[] centers = { new Vector2(c, c + r), new Vector2(c, c - r), new Vector2(c + r, c), new Vector2(c - r, c) };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + .5f, y + .5f);
                float a = 0f;
                foreach (var ce in centers) a = Mathf.Max(a, Ring(p, ce, r, stroke));
                // dấu thập giữa
                float cross = Mathf.Max(Line(Mathf.Abs(p.x - c), stroke * .8f) * Inside(Mathf.Abs(p.y - c), r * 1.9f),
                                        Line(Mathf.Abs(p.y - c), stroke * .8f) * Inside(Mathf.Abs(p.x - c), r * 1.9f));
                a = Mathf.Max(a, cross);
                if (a > 0f) tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        return tex;
    }

    // Đường trang trí: 2 nét ngang mờ dần ra 2 đầu + hoa thị nhỏ ở giữa
    static Texture2D DrawDivider(int w, int h)
    {
        var tex = Blank(w, h);
        float cy = h / 2f, cx = w / 2f, stroke = 2.2f;
        Texture2D q = DrawQuatrefoil(h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx);
                float a = 0f;
                if (dx > h * 0.6f)
                {
                    float fade = 1f - Mathf.InverseLerp(h * 0.6f, w / 2f, dx); // mờ dần ra 2 đầu
                    a = Line(Mathf.Abs(y + .5f - cy), stroke) * fade;
                    // chấm nhỏ cách đều như mắt song sắt
                    if (Mathf.Repeat(dx, 48f) < 4f && Mathf.Abs(y - cy) < 4f) a = Mathf.Max(a, fade);
                }
                int qx = Mathf.RoundToInt(x - (cx - h / 2f));
                if (qx >= 0 && qx < h) a = Mathf.Max(a, q.GetPixel(qx, y).a);
                if (a > 0f) tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        Object.DestroyImmediate(q);
        return tex;
    }

    static Texture2D Blank(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var clear = new Color[w * h];
        t.SetPixels(clear);
        return t;
    }

    // độ phủ khử răng cưa cho nét vẽ
    static float Ring(Vector2 p, Vector2 c, float r, float stroke) => Line(Mathf.Abs(Vector2.Distance(p, c) - r), stroke);
    static float Line(float dist, float half) => Mathf.Clamp01(half - dist + 0.5f);
    static float Inside(float d, float limit) => Mathf.Clamp01(limit - d + 0.5f);

    static void Save(Texture2D t, string path)
    {
        File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
    }
}
