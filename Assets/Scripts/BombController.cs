using UnityEngine;

public class BombController : MonoBehaviour
{
    private bool exploded;

    public void Explode()
    {
        if (exploded)
            return;

        exploded = true;
        Debug.Log("Bomb Exploded!");

        Destroy(gameObject);
    }
}
