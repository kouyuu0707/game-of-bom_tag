using UnityEngine;

public class BombController : MonoBehaviour
{
    public void Explode()
    {
        Debug.Log("Bomb Exploded!");

        Destroy(gameObject);
    }
}