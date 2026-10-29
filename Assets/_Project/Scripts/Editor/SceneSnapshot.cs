using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Chụp ảnh scene từ dòng lệnh (kiểm tra map không cần mở Editor):
/// Unity -batchmode -executeMethod SceneSnapshot.Run -snapScene &lt;path&gt; -snapOut &lt;png&gt;
///   [-snapCenter x,y] [-snapSize orthoSize] [-snapRes 1280x720]
/// Không truyền center → chụp toàn bộ bounds của các Tilemap.
/// </summary>
public static class SceneSnapshot
{
    public static void Run()
    {
        string scene = Arg("-snapScene", "Assets/_Project/Scenes/World.unity");
        string output = Arg("-snapOut", "snapshot.png");
        string res = Arg("-snapRes", "1600x900");
        var parts = res.Split('x');
        int w = int.Parse(parts[0]), h = int.Parse(parts[1]);

        EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);

        Vector2 center;
        float size;
        string c = Arg("-snapCenter", null);
        if (c != null)
        {
            var xy = c.Split(',');
            center = new Vector2(float.Parse(xy[0], System.Globalization.CultureInfo.InvariantCulture),
                                 float.Parse(xy[1], System.Globalization.CultureInfo.InvariantCulture));
            size = float.Parse(Arg("-snapSize", "5"), System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            var b = new Bounds();
            bool any = false;
            foreach (var r in Object.FindObjectsByType<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            center = b.center;
            size = Mathf.Max(b.extents.y, b.extents.x * h / w) * 1.05f;
        }

        var go = new GameObject("SnapshotCamera");
        var cam = go.AddComponent<Camera>();
        var main = Camera.main;
        if (main != null)
        {
            cam.CopyFrom(main);
        }
        cam.orthographic = true;
        cam.orthographicSize = size;
        cam.transform.position = new Vector3(center.x, center.y, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);

        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(output, tex.EncodeToPNG());
        Debug.Log($"[SceneSnapshot] {scene} → {output} center={center} size={size}");
    }

    private static string Arg(string name, string fallback)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return fallback;
    }
}
