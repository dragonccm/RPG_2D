using UnityEngine;
using UnityEngine.TextCore.Text;

public interface ISkill
{
    void Activate(Character user, Vector2 targetPosition);
    float GetCooldown();
    float GetManaCost();
}