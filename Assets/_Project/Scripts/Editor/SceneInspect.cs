using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>In nhanh thông tin object trong scene (debug dòng lệnh): -inspectScene path -inspectName prefix</summary>
public static class SceneInspect
{
    public static void Run()
    {
        string scene = Arg("-inspectScene"), name = Arg("-inspectName") ?? "";
        EditorSceneManager.OpenScene(scene);
        foreach (var t in Object.FindObjectsByType<Transform>().Where(t => t.name.StartsWith(name)))
        {
            Debug.Log($"[Inspect] {Path(t)} pos={t.position} active={t.gameObject.activeInHierarchy}");
            foreach (var r in t.GetComponents<Renderer>())
                Debug.Log($"[Inspect]   {r.GetType().Name} enabled={r.enabled} bounds={r.bounds} layer={r.sortingLayerName}/{r.sortingOrder}"
                          + (r is SpriteRenderer sr ? $" sprite={(sr.sprite ? sr.sprite.name + " ppu=" + sr.sprite.pixelsPerUnit : "null")}" : ""));
            foreach (var c in t.GetComponents<Component>()) Debug.Log($"[Inspect]   comp {c?.GetType().Name}");
        }
        foreach (var l in Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>())
            if (l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global)
                Debug.Log($"[Inspect] GlobalLight {l.name} color={l.color} intensity={l.intensity} layers={string.Join(",", l.targetSortingLayers)}");
    }

    private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;

    private static string Arg(string n)
    {
        var a = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == n) return a[i + 1];
        return null;
    }
}
