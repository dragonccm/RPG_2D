using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Tạo prefab kẻ địch mới từ art có sẵn bằng cách nhân bản prefab địch đã chạy được (AI, collider, thanh máu
/// giữ nguyên), đổi sprite + animation + chỉ số. Chuột Đầm: art 32 px vẽ ở 64 PPU → nhỏ ~0.5 tile, đúng
/// mật độ pixel của Tiny Swords (không phóng to sprite để khỏi lệch pixel).
/// </summary>
public static class EnemyPrefabBuilder
{
    private const string Rat = "Assets/_Project/Art/Rat/OutlinedRat/";
    private const string AnimFolder = "Assets/_Project/Art/animation/Rat";

    public static string BuildAll()
    {
        BuildRat();
        AssetDatabase.SaveAssets();
        return "Assets/_Project/Prefabs/Enemy_Rat.prefab";
    }

    private static void BuildRat()
    {
        MapArt.EnsureFolder(AnimFolder);
        var idle = MapArt.Grid(Rat + "rat-idle-outline.png", 32, 32, true);
        var run = MapArt.Grid(Rat + "rat-run-outline.png", 32, 32, true);

        var idleClip = Clip("Rat_Idle", idle, 6f);
        var runClip = Clip("Rat_Run", run, 12f);

        string ctrlPath = AnimFolder + "/Rat.controller";
        AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        var sm = ctrl.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle");
        sIdle.motion = idleClip;
        var sRun = sm.AddState("Run");
        sRun.motion = runClip;
        sm.defaultState = sIdle;
        var toRun = sIdle.AddTransition(sRun);
        toRun.hasExitTime = false; toRun.duration = 0f;
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        var toIdle = sRun.AddTransition(sIdle);
        toIdle.hasExitTime = false; toIdle.duration = 0f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        const string src = "Assets/_Project/Prefabs/Enemy_Sheep.prefab";
        const string dst = "Assets/_Project/Prefabs/Enemy_Rat.prefab";
        AssetDatabase.DeleteAsset(dst);
        AssetDatabase.CopyAsset(src, dst);

        var root = PrefabUtility.LoadPrefabContents(dst);
        root.name = "Enemy_Rat";
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.GetComponent<Canvas>() != null) continue;
            sr.sprite = idle[0];
            sr.sortingLayerName = "Characters";
        }
        foreach (var anim in root.GetComponentsInChildren<Animator>(true))
            anim.runtimeAnimatorController = ctrl;

        // Chỉ số: nhanh, mỏng máu, cắn nhẹ — đánh theo bầy
        foreach (var ch in root.GetComponents<Character>())
        {
            var so = new SerializedObject(ch);
            SetFloat(so, "maxHealth", 25f);
            SetFloat(so, "attackPower", 6f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (var enemy in root.GetComponents<Enemy>())
        {
            var so = new SerializedObject(enemy);
            SetFloat(so, "baseDamage", 6f);
            SetFloat(so, "moveSpeed", 3f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        if (root.GetComponent<YSort>() == null) root.AddComponent<YSort>();

        PrefabUtility.SaveAsPrefabAsset(root, dst);
        PrefabUtility.UnloadPrefabContents(root);
        EnemyPrefabs["Rat"] = dst;
    }

    public static readonly Dictionary<string, string> EnemyPrefabs = new Dictionary<string, string>();

    private static void SetFloat(SerializedObject so, string field, float value)
    {
        var p = so.FindProperty(field);
        if (p != null && p.propertyType == SerializedPropertyType.Float) p.floatValue = value;
        else Debug.LogWarning($"[EnemyPrefabBuilder] {so.targetObject.GetType().Name}.{field} không có");
    }

    private static AnimationClip Clip(string name, Sprite[] frames, float fps)
    {
        string path = $"{AnimFolder}/{name}.anim";
        AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip { frameRate = fps };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = frames.Select((s, i) => new ObjectReferenceKeyframe { time = i / fps, value = s }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }
}
