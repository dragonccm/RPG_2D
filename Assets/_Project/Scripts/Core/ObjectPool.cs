using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S2 (GAME_MASTER_PLAN 12.2-1): object pool tối giản cho GameObject (damage numbers,
/// impact effects, projectiles). Mục tiêu: 0 GC alloc/frame trong gameplay.
/// Dùng: var obj = ObjectPool.Get(prefabOrFactory); ...; ObjectPool.Release(obj);
/// </summary>
public class ObjectPool
{
    private readonly System.Func<GameObject> factory;
    private readonly Stack<GameObject> free = new Stack<GameObject>();
    private readonly Transform parent;

    public int FreeCount => free.Count;
    public int TotalCreated { get; private set; }

    public ObjectPool(System.Func<GameObject> factory, Transform parent = null)
    {
        this.factory = factory;
        this.parent = parent;
    }

    public GameObject Get(Vector3 position)
    {
        GameObject go = null;
        while (free.Count > 0)
        {
            go = free.Pop();
            if (go != null) break;
            go = null; // entry đã bị Destroy — bỏ
        }
        if (go == null)
        {
            go = factory();
            TotalCreated++;
        }
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.SetActive(true);
        return go;
    }

    public void Release(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(parent, false);
        free.Push(go);
    }
}
