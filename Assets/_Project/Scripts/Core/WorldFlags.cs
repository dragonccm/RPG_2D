using System;
using System.Collections.Generic;

/// <summary>
/// Story/world flags (CORE_PROJECT_GUIDELINES 6.7): nguồn sự thật duy nhất cho thay đổi thế giới —
/// rương đã mở, công tắc đã bật, tường đã phá, boss đã hạ, cổng đã mở...
/// Lưu trong SaveData.questFlags. Quy ước tên: "loại:id" (vd "chest:village_dash", "boss:tho_san_bong").
/// </summary>
public static class WorldFlags
{
    private static readonly HashSet<string> flags = new HashSet<string>();

    /// <summary>Gọi khi 1 flag được bật (tham số = tên flag) hoặc khi import save (tham số = null).</summary>
    public static event Action<string> Changed;

    public static bool Has(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);

    public static void Set(string flag)
    {
        if (string.IsNullOrEmpty(flag) || !flags.Add(flag)) return;
        Changed?.Invoke(flag);
    }

    public static List<string> Export() => new List<string>(flags);

    public static void Import(IEnumerable<string> saved)
    {
        flags.Clear();
        if (saved != null)
            foreach (var f in saved)
                if (!string.IsNullOrEmpty(f)) flags.Add(f);
        Changed?.Invoke(null);
    }

    public static void ResetForTest()
    {
        flags.Clear();
        Changed = null;
    }
}
