using UnityEngine;

[RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
public class BombPickup : MonoBehaviour
{
    private bool collected;

    private void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true;
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnTriggerEnter(Collider other) => TryCollect(other);

    // A player who was full can collect without leaving after placing a bomb.
    private void OnTriggerStay(Collider other) => TryCollect(other);

    private void TryCollect(Collider other)
    {
        if (collected)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || !player.canMove)
            return;

        BombInventory inventory = player.GetComponent<BombInventory>();
        if (inventory == null || !inventory.TryCollect())
            return;

        collected = true;
        GetComponent<SphereCollider>().enabled = false;
        Destroy(gameObject);
    }
}
