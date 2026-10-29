using UnityEngine;

/// <summary>
/// Đạn của AttackDef (shape = Projectile): bay thẳng, trúng mục tiêu hợp lệ hoặc vật cản thì nổ.
/// Quét bằng OverlapCircle theo bước (không phụ thuộc Rigidbody/trigger của prefab), không cấp phát mỗi frame.
/// </summary>
public class AttackProjectile : MonoBehaviour
{
    private AttackDef def;
    private Character owner;
    private Vector2 dir;
    private float travelled;
    private SpriteRenderer sr;

    private static readonly Collider2D[] buffer = new Collider2D[8];

    public static AttackProjectile Spawn(AttackDef def, Character owner, Vector2 origin, Vector2 dir, GameObject prefab)
    {
        GameObject go;
        if (prefab != null) go = Instantiate(prefab, origin, Quaternion.identity);
        else go = new GameObject("AttackProjectile_" + def.attackId);
        go.transform.position = origin;
        // Prefab cũ có thể mang script đạn riêng + collider/Rigidbody → vô hiệu để không gây sát thương 2 lần
        foreach (var rb in go.GetComponentsInChildren<Rigidbody2D>()) rb.simulated = false;
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>()) if (mb != null) mb.enabled = false;

        var p = go.AddComponent<AttackProjectile>();
        p.def = def;
        p.owner = owner;
        p.dir = dir.normalized;
        p.sr = go.GetComponentInChildren<SpriteRenderer>();
        if (p.sr == null) p.BuildDefaultVisual(go);
        go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p.dir.y, p.dir.x) * Mathf.Rad2Deg);
        return p;
    }

    private void BuildDefaultVisual(GameObject go)
    {
        sr = go.AddComponent<SpriteRenderer>();
        bool dart = def.projectileStyle == ProjectileStyle.Dart;
        sr.sprite = dart ? PixelSprites.Dart : PixelSprites.Orb;
        sr.color = dart ? new Color(0.9f, 0.85f, 0.7f) : def.projectileColor;
        sr.sortingLayerName = "Effects";
        sr.sortingOrder = 4600;
        RuntimeMaterials.MakeUnlit(sr);
        if (!dart)
        {
            // lõi sáng + đèn nhỏ: đạn phép đọc rõ trong đêm
            var core = new GameObject("Core").AddComponent<SpriteRenderer>();
            core.transform.SetParent(go.transform, false);
            core.sprite = PixelSprites.Orb;
            core.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            core.color = Color.Lerp(def.projectileColor, Color.white, 0.7f);
            core.sortingLayerName = "Effects";
            core.sortingOrder = 4601;
            RuntimeMaterials.MakeUnlit(core);
            var l = new GameObject("Glow").AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            l.transform.SetParent(go.transform, false);
            l.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            l.color = def.projectileColor;
            l.intensity = 0.8f;
            l.pointLightOuterRadius = 2.2f;
            LightUtil.TargetAllLayers(l);
        }
    }

    private void Update()
    {
        float step = def.projectileSpeed * Time.deltaTime;
        Vector2 next = (Vector2)transform.position + dir * step;
        transform.position = next;
        travelled += step;

        int n = Physics2D.OverlapCircle(next, def.radius, new ContactFilter2D { useTriggers = false }, buffer);
        for (int i = 0; i < n; i++)
        {
            var col = buffer[i];
            if (col == null) continue;
            var target = col.GetComponentInParent<Character>();
            if (target != null)
            {
                if (target == owner || target.IsDead || !AttackRunner.IsValidTarget(def.team, target)) continue;
                float dmg = AttackRunner.DealDamage(owner, def, target, dir, out bool crit);
                owner?.GetComponent<Enemy>()?.RaiseDealDamage(target.gameObject, dmg);
                Explode(HitMaterial.Magic);
                return;
            }
            if (owner != null && col.transform.IsChildOf(owner.transform)) continue;
            Explode(HitMaterial.Stone); // vật cản
            return;
        }
        if (travelled >= def.projectileRange) Explode(HitMaterial.Magic);
    }

    private void Explode(HitMaterial material)
    {
        VfxService.Hit(transform.position, -dir, material);
        VfxService.Impact(transform.position, def.projectileColor, 0.6f);
        Destroy(gameObject);
    }
}
