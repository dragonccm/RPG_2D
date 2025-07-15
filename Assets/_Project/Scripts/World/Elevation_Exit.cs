using UnityEngine;

public class Elevation_Exit : MonoBehaviour
{
    public Collider2D[] mautainColliderS;
    public Collider2D[] BoundiaryColliderS;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            foreach (Collider2D collider in mautainColliderS)
            {
                collider.enabled = true;
            }
            foreach (Collider2D collider in BoundiaryColliderS)
            {
                collider.enabled = false;
            }
            other.gameObject.GetComponent<SpriteRenderer>().sortingOrder = 10;
        }
    }
}
