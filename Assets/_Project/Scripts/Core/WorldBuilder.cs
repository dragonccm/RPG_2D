using UnityEngine;

/// <summary>
/// S3 (GAME_MASTER_PLAN 4): builder dựng nội dung map bằng code —
/// dùng bởi editor script / test / bootstrap runtime. Giữ đây để mỗi eval CLI ngắn.
/// Visual placeholder procedural (sprite tròn) — thay art thật ở S4+.
/// </summary>
public static class WorldBuilder
{
    /// <summary>Tạo GameObject sprite tròn procedural (đỉnh mềm, alpha fade mép).</summary>
    public static GameObject CreateCircleSprite(string name, Vector3 pos, Color color, float radius = 0.5f)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        const int S = 48;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float dx = (x - 23.5f) / 23.5f, dy = (y - 23.5f) / 23.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            px[y * S + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(1f - d) * 255f));
        }
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f / (radius * 2f));
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.color = color;
        return go;
    }

    public static GameObject CreateCampfire(string checkpointId, Vector3 pos)
    {
        var go = CreateCircleSprite("Campfire_" + checkpointId, pos, new Color(1f, 0.5f, 0.1f, 0.9f), 0.5f);
        go.AddComponent<Campfire>().checkpointId = checkpointId;
        return go;
    }

    public static GameObject CreateNPC(string name, Vector3 pos, Color color)
    {
        var go = CreateCircleSprite(name, pos, color, 0.45f);
        var tm = go.AddComponent<TextMesh>();
        tm.text = name.Replace("NPC_", "");
        tm.characterSize = 0.25f;
        tm.anchor = TextAnchor.UpperCenter;
        tm.color = Color.white;
        return go;
    }

    /// <summary>Cổng 2 chiều: from → to. Trả về GO cổng tại from.</summary>
    public static GameObject CreatePortal(string portalName, Vector3 from, Transform destination)
    {
        var go = CreateCircleSprite("Portal_" + portalName, from, new Color(0.4f, 0.8f, 1f, 0.5f), 0.7f);
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.7f;
        var portal = go.AddComponent<ScenePortal>();
        portal.portalName = portalName;
        portal.destinationRef = destination;
        return go;
    }

    public static Transform CreateAnchor(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        return go.transform;
    }
}
