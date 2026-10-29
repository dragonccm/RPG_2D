using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorFollow : MonoBehaviour
{
    public GameObject NormalCursor;
    public GameObject HitCursor;
    public LayerMask enemyLayerMask;

    private void Start()
    {
        Cursor.visible = false;
        NormalCursor.SetActive(true);
        HitCursor.SetActive(false);
    }

    void Update()
    {
        Vector3 mousePosition = Input.mousePosition;
        NormalCursor.transform.position = mousePosition;
        HitCursor.transform.position = mousePosition;

        if (IsHoveringAttackableTarget())
        {
            NormalCursor.SetActive(false);
            HitCursor.SetActive(true);
        }
        else
        {
            NormalCursor.SetActive(true);
            HitCursor.SetActive(false);
        }
    }

    private bool IsHoveringAttackableTarget()
    {
        Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPosition, enemyLayerMask);
        return hitCollider != null;
    }
}
