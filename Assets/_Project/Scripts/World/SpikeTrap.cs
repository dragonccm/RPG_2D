using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bẫy gai (Spike Trap) 2D:
/// - Chu kỳ: Retracted (ẩn) -> Telegraph (báo hiệu nhú gai 0.5s) -> Active (bật đâm 1.0s gây sát thương) -> Cooldown (2.0s).
/// - Có thể kích hoạt tự động theo chu kỳ hoặc khi người chơi bước lên.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SpikeTrap : MonoBehaviour
{
    public enum TrapState { Retracted, Telegraph, Active, Cooldown }

    [Header("Cài đặt sát thương & chu kỳ")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float telegraphTime = 0.5f;
    [SerializeField] private float activeTime = 1.0f;
    [SerializeField] private float cooldownTime = 2.0f;
    [SerializeField] private bool autoCycle = true;

    [Header("Màu sắc báo hiệu (nếu chưa có sprite animation)")]
    [SerializeField] private Color retractedColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    [SerializeField] private Color telegraphColor = new Color(1f, 0.6f, 0.1f, 1f); // Cam cảnh báo
    [SerializeField] private Color activeColor = new Color(1f, 0.1f, 0.1f, 1f);    // Đỏ gai nhọn

    private SpriteRenderer spriteRenderer;
    private Collider2D triggerCol;
    private TrapState state = TrapState.Retracted;
    private readonly HashSet<IDamageable> victimsInside = new HashSet<IDamageable>();

    public TrapState CurrentState => state;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCol = GetComponent<Collider2D>();
        triggerCol.isTrigger = true;
        SetState(TrapState.Retracted);
    }

    private void Start()
    {
        if (autoCycle)
        {
            StartCoroutine(TrapCycleRoutine());
        }
    }

    private IEnumerator TrapCycleRoutine()
    {
        // Trễ ban đầu ngẫu nhiên để các bẫy không bật cùng 1 giây
        yield return new WaitForSeconds(Random.Range(0.2f, 1.5f));

        while (true)
        {
            SetState(TrapState.Telegraph);
            yield return new WaitForSeconds(telegraphTime);

            SetState(TrapState.Active);
            DamageAllInside();
            yield return new WaitForSeconds(activeTime);

            SetState(TrapState.Cooldown);
            yield return new WaitForSeconds(cooldownTime);

            SetState(TrapState.Retracted);
            yield return new WaitForSeconds(1.0f);
        }
    }

    public void TriggerManually()
    {
        if (state == TrapState.Retracted)
        {
            StopAllCoroutines();
            StartCoroutine(ManualTriggerRoutine());
        }
    }

    private IEnumerator ManualTriggerRoutine()
    {
        SetState(TrapState.Telegraph);
        yield return new WaitForSeconds(telegraphTime);

        SetState(TrapState.Active);
        DamageAllInside();
        yield return new WaitForSeconds(activeTime);

        SetState(TrapState.Retracted);
    }

    private void SetState(TrapState newState)
    {
        state = newState;
        if (spriteRenderer != null)
        {
            switch (state)
            {
                case TrapState.Retracted:
                case TrapState.Cooldown:
                    spriteRenderer.color = retractedColor;
                    transform.localScale = Vector3.one * 0.8f;
                    break;
                case TrapState.Telegraph:
                    spriteRenderer.color = telegraphColor;
                    transform.localScale = Vector3.one * 0.95f;
                    break;
                case TrapState.Active:
                    spriteRenderer.color = activeColor;
                    transform.localScale = Vector3.one * 1.15f;
                    break;
            }
        }
    }

    private void DamageAllInside()
    {
        victimsInside.RemoveWhere(v => v == null || (v is Object obj && obj == null));
        foreach (var victim in victimsInside)
        {
            victim.TakeDamage(damage);
            if (victim is MonoBehaviour mb)
            {
                VfxService.HitStop(ShakeLevel.Light);
                VfxService.Hit(mb.transform.position, Vector2.up, HitMaterial.Flesh);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var dmg = collision.GetComponent<IDamageable>() ?? collision.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            victimsInside.Add(dmg);
            if (state == TrapState.Active)
            {
                dmg.TakeDamage(damage);
            }
            else if (!autoCycle && state == TrapState.Retracted)
            {
                TriggerManually();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var dmg = collision.GetComponent<IDamageable>() ?? collision.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            victimsInside.Remove(dmg);
        }
    }
}
