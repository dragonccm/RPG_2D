using NUnit.Framework;
using UnityEngine;

/// <summary>
/// S2 (GAME_MASTER_PLAN sprint S2): test logic combo + object pool.
/// </summary>
public class PlayerCombatTests
{
    [Test]
    public void NextComboIndex_AdvancesWithinWindow()
    {
        Assert.AreEqual(1, PlayerCombat.NextComboIndex(0, true, 3));
        Assert.AreEqual(2, PlayerCombat.NextComboIndex(1, true, 3));
        // Nhịp cuối vòng về 0 (combo lặp)
        Assert.AreEqual(0, PlayerCombat.NextComboIndex(2, true, 3));
    }

    [Test]
    public void NextComboIndex_ResetsOutsideWindow()
    {
        Assert.AreEqual(0, PlayerCombat.NextComboIndex(2, false, 3));
        Assert.AreEqual(0, PlayerCombat.NextComboIndex(1, false, 3));
    }

    [Test]
    public void NextComboIndex_SingleHitCombo_AlwaysZero()
    {
        Assert.AreEqual(0, PlayerCombat.NextComboIndex(0, true, 1));
    }
}
