using NUnit.Framework;
using UnityEngine;

/// <summary>
/// S2 (GAME_MASTER_PLAN 12.2-1): test object pool — tái sử dụng instance, không phình.
/// </summary>
public class ObjectPoolTests
{
    private int created;

    private GameObject Factory()
    {
        created++;
        return new GameObject("Pooled_" + created);
    }

    [Test]
    public void Get_ReusesReleasedInstance()
    {
        var pool = new ObjectPool(Factory);
        var a = pool.Get(Vector3.zero);
        pool.Release(a);
        var b = pool.Get(Vector3.right);

        Assert.AreSame(a, b, "Instance đã release phải được tái sử dụng");
        Assert.AreEqual(1, pool.TotalCreated, "Không được tạo mới khi pool còn free");
        Assert.IsTrue(b.activeSelf);
    }

    [Test]
    public void Get_CreatesNewWhenPoolEmpty()
    {
        var pool = new ObjectPool(Factory);
        var a = pool.Get(Vector3.zero);
        var b = pool.Get(Vector3.zero);
        Assert.AreNotSame(a, b);
        Assert.AreEqual(2, pool.TotalCreated);
    }

    [Test]
    public void Release_DeactivatesAndParks()
    {
        var pool = new ObjectPool(Factory);
        var a = pool.Get(Vector3.zero);
        pool.Release(a);
        Assert.IsFalse(a.activeSelf, "Object trong pool phải inactive");
        Assert.AreEqual(1, pool.FreeCount);
    }

    [Test]
    public void Release_DestroyedObject_IsSafe()
    {
        var pool = new ObjectPool(Factory);
        var a = pool.Get(Vector3.zero);
        Object.DestroyImmediate(a);
        Assert.DoesNotThrow(() => pool.Release(a));
        // Pool tự bỏ entry hỏng và tạo instance mới
        var b = pool.Get(Vector3.zero);
        Assert.IsNotNull(b);
        Assert.AreEqual(2, pool.TotalCreated);
    }
}
