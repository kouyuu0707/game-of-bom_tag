using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public bool canMove = true;
    public GameObject bombPrefab;
    private GameObject placedBomb;
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
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

            placedBomb = Instantiate(bombPrefab, bombPosition, Quaternion.identity);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (placedBomb != null)
            {
                BombController bombController =
                    placedBomb.GetComponent<BombController>();

                bombController.Explode();

                placedBomb = null;
            }
        }
    }
}