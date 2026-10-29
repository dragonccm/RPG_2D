using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Đổi không khí map theo story flag (LEVEL_DESIGN 3.1: làng đỏ dần khi q3 bắt đầu). Khi flag bật,
/// Global Light 2D chuyển dần sang màu/độ sáng mới; nạp lại save đã có flag thì áp ngay, không chuyển cảnh.
/// </summary>
public class FlagAmbience : MonoBehaviour
{
    public string flag = "";
    public Color color = new Color(0.55f, 0.25f, 0.3f);
    public float intensity = 0.65f;
    public float seconds = 4f;

    private Light2D globalLight;
    private Coroutine fade;

    private void Awake() { WorldFlags.Changed += OnChanged; }
    private void OnDestroy() { WorldFlags.Changed -= OnChanged; }

    private void Start()
    {
        FindLight();
        if (WorldFlags.Has(flag)) Apply(false);
    }

    private void OnChanged(string f)
    {
        if (f == null || f == flag) Apply(f != null);
    }

    private void FindLight()
    {
        foreach (var l in FindObjectsByType<Light2D>())
            if (l.lightType == Light2D.LightType.Global) { globalLight = l; return; }
    }

    private void Apply(bool animate)
    {
        if (!WorldFlags.Has(flag)) return;
        if (globalLight == null) FindLight();
        if (globalLight == null) return;
        if (fade != null) StopCoroutine(fade);
        if (!animate || !isActiveAndEnabled)
        {
            globalLight.color = color;
            globalLight.intensity = intensity;
        }
        else fade = StartCoroutine(Fade());
    }

    private IEnumerator Fade()
    {
        Color c0 = globalLight.color;
        float i0 = globalLight.intensity, t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / seconds);
            globalLight.color = Color.Lerp(c0, color, k);
            globalLight.intensity = Mathf.Lerp(i0, intensity, k);
            yield return null;
        }
        fade = null;
    }
}
