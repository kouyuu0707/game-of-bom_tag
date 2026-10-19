using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent agent;
    public GameObject gameOverText;
    public GameObject retryButton;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        agent.SetDestination(player.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController playerController =
                other.GetComponent<PlayerController>();

            playerController.canMove = false;

            gameOverText.SetActive(true);
            retryButton.SetActive(true);

            Debug.Log("Game Over");
        }
    }
}