using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// P0-6 (GAME_MASTER_PLAN.md): test nền gameplay.
/// Test 1–4: MovementMath.ClampDiagonal — đi chéo KHÔNG nhanh hơn trục (mục 3.1).
/// Test 5: IsPlayerCharacter — player không bao giờ bị skill của chính mình nhắm (mục 2.2).
/// </summary>
public class MovementMathTests
{
    const float EPS = 0.01f;

    [Test]
    public void ClampDiagonal_DigitalDiagonal_IsNormalized()
    {
        var v = MovementMath.ClampDiagonal(new Vector2(1f, 1f));
        Assert.AreEqual(1f, v.magnitude, EPS);
        Assert.AreEqual(0.7071f, v.x, EPS);
        Assert.AreEqual(0.7071f, v.y, EPS);
    }

    [Test]
    public void ClampDiagonal_SpeedEqual_All8Directions()
    {
        // 8 hướng digital: tốc độ áp dụng (= moveSpeed * |vector|) phải bằng nhau
        var dirs = new Vector2[]
        {
            Vector2.right, Vector2.left, Vector2.up, Vector2.down,
            new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1)
        };
        foreach (var d in dirs)
        {
            var v = MovementMath.ClampDiagonal(d);
            Assert.AreEqual(1f, v.magnitude, EPS, $"hướng ({d.x},{d.y}) lệch tốc độ");
        }
    }

    [Test]
    public void ClampDiagonal_AnalogLightInput_Preserved()
    {
        var v = MovementMath.ClampDiagonal(new Vector2(0.4f, 0.4f));
        Assert.AreEqual(0.4f, v.x, EPS);
        Assert.AreEqual(0.4f, v.y, EPS);
    }

    [Test]
    public void ClampDiagonal_Zero_IsZero()
    {
        Assert.AreEqual(Vector2.zero, MovementMath.ClampDiagonal(Vector2.zero));
    }

    [Test]
    public void IsPlayerCharacter_PlayerTag_ReturnsTrue()
    {
        // Layer cuối cùng của IsPlayerCharacter: tag "Player" phải luôn chặn self-damage
        var go = new GameObject("TestWarrior");
        go.tag = "Player";
        var character = go.AddComponent<Character>();

        var exec = new ExecutorProbe(ScriptableObject.CreateInstance<SkillModule>());

        var m = typeof(SkillExecutorBase).GetMethod(
            "IsPlayerCharacter",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(m, "IsPlayerCharacter phải tồn tại trong SkillExecutorBase");

        var result = (bool)m.Invoke(exec, new object[] { character, null });
        Assert.IsTrue(result, "Nhân vật tag Player phải được nhận diện là player");
    }

    /// <summary>Probe để gọi protected IsPlayerCharacter.</summary>
    private class ExecutorProbe : SkillExecutorBase
    {
        public ExecutorProbe(SkillModule module) : base(module) { }
        public override void Execute(Character user, Vector2 targetPosition) { }
    }
}
