using UnityEngine;
using System;

public class Resource : MonoBehaviour
{
    public float currentValue { get; set; }
    public float maxValue { get; set; }
    public float regenRate;
    public event Action<float, float> OnValueChanged;

    public void Initialize(float max, float regen)
    {
        maxValue = max;
        currentValue = max;
        regenRate = regen;
        OnValueChanged?.Invoke(currentValue, maxValue);
    }

    public void Decrease(float amount)
    {
        currentValue = Mathf.Max(0, currentValue - amount);
        OnValueChanged?.Invoke(currentValue, maxValue);
    }

    public void Increase(float amount)
    {
        currentValue = Mathf.Min(maxValue, currentValue + amount);
        OnValueChanged?.Invoke(currentValue, maxValue);
    }

    private void Update()
    {
        if (regenRate > 0)
        {
            Increase(regenRate * Time.deltaTime);
        }
    }
}