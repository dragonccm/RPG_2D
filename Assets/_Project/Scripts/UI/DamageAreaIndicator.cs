using UnityEngine;

public class DamageAreaIndicator : MonoBehaviour
{
    [Header("Visual Settings")]
    public LineRenderer lineRenderer;
    public SpriteRenderer areaRenderer;
    public Color rangeColor = new Color(1f, 1f, 0f, 0.5f);
    public Color areaColor = new Color(1f, 0f, 0f, 0.3f);
    
    private void Awake()
    {
        SetupComponents();
    }
    
    private void SetupComponents()
    {
        // Setup LineRenderer for range circle
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        lineRenderer.material = CreateDefaultMaterial();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        
        // Setup SpriteRenderer for area effect
        if (areaRenderer == null)
        {
            GameObject areaObj = new GameObject("AreaEffect");
            areaObj.transform.SetParent(transform);
            areaRenderer = areaObj.AddComponent<SpriteRenderer>();
        }
        
        areaRenderer.sprite = CreateCircleSprite();
        areaRenderer.color = areaColor;
        areaRenderer.sortingOrder = -1;
    }
    
    public void ShowSkillArea(SkillModule skill, Vector3 position)
    {
        if (skill == null) return;
        
        transform.position = position;
        gameObject.SetActive(true);
        
        // Show range circle
        ShowRangeCircle(skill.range);
        
        // Show area effect circle
        if (skill.areaRadius > 0)
        {
            ShowAreaCircle(skill.areaRadius);
        }
        else if (skill.skillType == SkillType.Melee)
        {
            ShowAreaCircle(skill.range * 0.5f); // Melee attacks have smaller area
        }
        
        // Auto hide after specified time
        if (skill.damageAreaDisplayTime > 0)
        {
            Invoke(nameof(HideArea), skill.damageAreaDisplayTime);
        }
    }
    
    private void ShowRangeCircle(float range)
    {
        lineRenderer.material.color = rangeColor;
        CreateCircle(lineRenderer, range, 64);
    }
    
    private void ShowAreaCircle(float radius)
    {
        areaRenderer.transform.localScale = Vector3.one * radius * 2f;
        areaRenderer.gameObject.SetActive(true);
    }
    
    private void CreateCircle(LineRenderer lr, float radius, int points)
    {
        lr.positionCount = points;
        
        for (int i = 0; i < points; i++)
        {
            float angle = i * Mathf.PI * 2f / points;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, y, 0));
        }
    }
    
    private Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2, size / 2);
        float radius = size / 2 - 2;
        
        for (int i = 0; i < colors.Length; i++)
        {
            int x = i % size;
            int y = i / size;
            
            Vector2 pos = new Vector2(x, y);
            float distance = Vector2.Distance(pos, center);
            
            if (distance <= radius)
            {
                float alpha = 1f - (distance / radius) * 0.5f;
                colors[i] = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                colors[i] = Color.clear;
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
    
    private Material CreateDefaultMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = rangeColor;
        return mat;
    }
    
    public void HideArea()
    {
        gameObject.SetActive(false);
        areaRenderer.gameObject.SetActive(false);
    }
    
    public static DamageAreaIndicator CreateIndicator()
    {
        GameObject indicatorObj = new GameObject("DamageAreaIndicator");
        return indicatorObj.AddComponent<DamageAreaIndicator>();
    }
}