using System.Linq;
using NUnit.Framework;

/// <summary>Bản đồ M1–M5 đạt chuẩn LEVEL_DESIGN (MapDesignCheck): kết nối, khoảng cách, mật độ, cổng.</summary>
public class MapDesignTests
{
    private static readonly System.Collections.Generic.Dictionary<string, MapSpec> All =
        MapSpecs.All().ToDictionary(m => m.sceneName);

    [Test]
    public void AllMaps_PassDesignChecks()
    {
        var errors = All.Values.SelectMany(m => MapDesignCheck.Check(m, All)).ToList();
        Assert.IsEmpty(errors, "\n" + string.Join("\n", errors));
    }

    [Test]
    public void ThereAreFiveMaps_InTheDesignedOrder()
    {
        CollectionAssert.AreEqual(new[] { "M1_Village", "M2_Forest", "M3_Marsh", "M4_Tower", "M5_Cave" }, All.Keys.ToArray());
    }

    [Test]
    public void EveryChestItem_Exists()
    {
        foreach (var m in All.Values)
            foreach (var c in m.chests)
                Assert.IsNotNull(ItemDatabase.Get(c.itemId), $"{m.sceneName}: rương {c.id} dùng itemId không tồn tại '{c.itemId}'");
    }
}
