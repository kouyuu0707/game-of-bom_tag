using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(BombInventory))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public bool canMove = true;
    public GameObject bombPrefab;
    private BombInventory inventory;
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        inventory = GetComponent<BombInventory>();
    }

    void Update()
    {
        if (!canMove)
        {
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(horizontal, 0, vertical);

        controller.Move(move * moveSpeed * Time.deltaTime);


        if (Input.GetKeyDown(KeyCode.B))
        {
            Vector3 bombPosition =
                transform.position + new Vector3(0, -1.0f, 0);

            inventory.TryPlace(bombPrefab, bombPosition);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            inventory.DetonateAll();
        }
    }
}
