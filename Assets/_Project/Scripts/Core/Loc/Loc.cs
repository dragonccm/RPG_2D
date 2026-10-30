using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bảng chuỗi hiển thị (CORE_PROJECT_GUIDELINES 6.10). UI/thông báo gọi Loc.T("khoá") thay vì viết thẳng tiếng Việt trong code.
/// Dữ liệu: Resources/Loc/vi.json (+ en.json …) dạng {"entries":[{"key":"...","text":"..."}]}. Thiếu khoá → trả về chính khoá
/// (dễ phát hiện), thiếu ngôn ngữ → rơi về "vi". Tham số theo kiểu string.Format: Loc.T("inv.slots", 3, 16).
/// Hội thoại (DialogueDatabase) vẫn là dữ liệu trong code — chuyển sang file khi cần thêm ngôn ngữ.
/// </summary>
public static class Loc
{
    [Serializable] private class Entry { public string key; public string text; }
    [Serializable] private class Table { public Entry[] entries; }

    public const string Fallback = "vi";
    private static readonly Dictionary<string, string> current = new Dictionary<string, string>();
    private static readonly Dictionary<string, string> fallback = new Dictionary<string, string>();
    private static string language = "";

    public static string Language
    {
        get { Ensure(); return language; }
        set { language = value; Reload(); PlayerPrefs.SetString("language", value); }
    }

    public static string T(string key)
    {
        Ensure();
        if (current.TryGetValue(key, out var s)) return s;
        if (fallback.TryGetValue(key, out s)) return s;
        return key;
    }

    public static string T(string key, params object[] args)
    {
        string fmt = T(key);
        try { return string.Format(fmt, args); }
        catch (FormatException) { return fmt; }
    }

    /// <summary>Có bản dịch thì dùng, không thì giữ chuỗi gốc trong dữ liệu (hội thoại, tên vật phẩm...).</summary>
    public static string TOr(string key, string original) => Has(key) ? T(key) : original;

    public static bool Has(string key) { Ensure(); return current.ContainsKey(key) || fallback.ContainsKey(key); }

    /// <summary>Nạp từ JSON (test hoặc ngôn ngữ tải ngoài).</summary>
    public static void LoadJson(string json, Dictionary<string, string> into)
    {
        into.Clear();
        if (string.IsNullOrEmpty(json)) return;
        var table = JsonUtility.FromJson<Table>(json);
        if (table?.entries == null) return;
        foreach (var e in table.entries)
            if (e != null && !string.IsNullOrEmpty(e.key)) into[e.key] = e.text ?? "";
    }

    public static IEnumerable<string> Keys { get { Ensure(); return fallback.Keys; } }

    private static void Ensure()
    {
        if (!string.IsNullOrEmpty(language)) return;
        language = PlayerPrefs.GetString("language", Fallback);
        Reload();
    }

    private static void Reload()
    {
        LoadJson(Resources.Load<TextAsset>("Loc/" + Fallback)?.text, fallback);
        if (language == Fallback) current.Clear();
        else LoadJson(Resources.Load<TextAsset>("Loc/" + language)?.text, current);
    }

    public static void ResetForTest() { language = ""; current.Clear(); fallback.Clear(); }
}
