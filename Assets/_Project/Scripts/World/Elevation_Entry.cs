using UnityEngine;

public class Elevation_Entry : MonoBehaviour
{
    public Collider2D[] mautainColliderS;
    public Collider2D[] BoundiaryColliderS;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            foreach (Collider2D collider in mautainColliderS)
            {
                collider.enabled = false;
            }
            foreach (Collider2D collider in BoundiaryColliderS)
            {
                collider.enabled = true;
            }
            other.gameObject.GetComponent<SpriteRenderer>().sortingOrder = 15;
        }
    }
}
