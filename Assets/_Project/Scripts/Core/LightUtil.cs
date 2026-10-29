using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Light2D chỉ chiếu các sorting layer trong danh sách target. Đèn tạo bằng code (đống lửa, đuốc)
/// hoặc đèn cũ trong scene chỉ trỏ layer Default → nền (Ground) và nhân vật (Characters) không nhận sáng.
/// Gọi 1 lần lúc tạo đèn (không dùng trong vòng lặp gameplay).
/// </summary>
public static class LightUtil
{
    private static readonly FieldInfo LayersField =
        typeof(Light2D).GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void TargetAllLayers(Light2D light)
    {
        if (light == null || LayersField == null) return;
        var layers = SortingLayer.layers;
        var ids = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++) ids[i] = layers[i].id;
        LayersField.SetValue(light, ids);
        // Light2D đăng ký theo layer lúc OnEnable — bật lại để nhận danh sách mới
        if (light.isActiveAndEnabled)
        {
            light.enabled = false;
            light.enabled = true;
        }
    }

    /// <summary>Vầng sáng mềm quanh player: giữ vùng giao tranh đọc được trong đêm.</summary>
    public static void EnsurePlayerLight(Transform player)
    {
        if (player == null || player.Find("PlayerLight") != null) return;
        var go = new GameObject("PlayerLight");
        go.transform.SetParent(player, false);
        go.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = new Color(1f, 0.93f, 0.82f);
        l.intensity = 0.55f;
        l.pointLightOuterRadius = 6f;
        l.pointLightInnerRadius = 1f;
        l.falloffIntensity = 0.7f;
        TargetAllLayers(l);
    }
}
