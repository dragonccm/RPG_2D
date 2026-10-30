using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Menu chính tối giản (scene MainMenu): Chơi mới / Tiếp tục (nếu có save) / Thoát.
/// Tự dựng UI lúc chạy — scene chỉ cần 1 object gắn component này.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    
    private Font font;

    private void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGo = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        var bg = MakeRect("Background", canvasGo.transform, Vector2.zero, Vector2.one);
        bg.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.05f, 0.09f);

        Label(canvasGo.transform, Loc.T("menu.title"), 72, new Color(0.85f, 0.2f, 0.22f), 0.72f);
        Label(canvasGo.transform, Loc.T("menu.subtitle"), 22, new Color(0.8f, 0.78f, 0.72f), 0.62f);

        float y = 0.46f;
        Button(canvasGo.transform, Loc.T("menu.new"), y, GameFlow.NewGame);
        if (SaveSystem.HasSave) Button(canvasGo.transform, Loc.T("menu.continue"), y -= 0.1f, GameFlow.Continue);
        Button(canvasGo.transform, Loc.T("menu.quit"), y - 0.1f, Application.Quit);

        Slider(canvasGo.transform, Loc.T("menu.sfx"), 0.2f, AudioService.SfxVolume, v => { AudioService.SfxVolume = v; AudioService.Play(SfxId.SwordHit); });
        Slider(canvasGo.transform, Loc.T("menu.music"), 0.12f, AudioService.MusicVolume, v => AudioService.MusicVolume = v);
    }

    private RectTransform MakeRect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private void Label(Transform parent, string text, int size, Color color, float y)
    {
        var rt = MakeRect("Label", parent, new Vector2(0.1f, y - 0.05f), new Vector2(0.9f, y + 0.05f));
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
    }

    private void Slider(Transform parent, string text, float y, float value, UnityEngine.Events.UnityAction<float> onChange)
    {
        Label(parent, text, 18, new Color(0.8f, 0.78f, 0.72f), y + 0.035f);
        var rt = MakeRect("Slider_" + text, parent, new Vector2(0.4f, y - 0.015f), new Vector2(0.6f, y + 0.015f));
        var bg = rt.gameObject.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.18f, 0.14f);
        var fillArea = MakeRect("Fill", rt, Vector2.zero, Vector2.one);
        var fill = fillArea.gameObject.AddComponent<Image>();
        fill.color = new Color(0.75f, 0.35f, 0.2f);
        var s = rt.gameObject.AddComponent<UnityEngine.UI.Slider>();
        s.fillRect = fillArea;
        s.targetGraphic = bg;
        s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        s.minValue = 0f; s.maxValue = 1f; s.value = value;
        s.onValueChanged.AddListener(onChange);
    }

    private void Button(Transform parent, string text, float y, UnityEngine.Events.UnityAction onClick)
    {
        var rt = MakeRect("Button_" + text, parent, new Vector2(0.4f, y - 0.035f), new Vector2(0.6f, y + 0.035f));
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0.25f, 0.18f, 0.14f);
        var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
        b.targetGraphic = img;
        var colors = b.colors;
        colors.highlightedColor = new Color(0.55f, 0.3f, 0.2f);
        b.colors = colors;
        b.onClick.AddListener(onClick);
        Label(rt, text, 26, new Color(0.95f, 0.9f, 0.8f), 0.5f);
    }
}
