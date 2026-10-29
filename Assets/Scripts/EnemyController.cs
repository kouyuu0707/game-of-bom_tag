using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent agent;
    // Retained for compatibility with the existing scene references.
    public GameObject gameOverText;
    public GameObject retryButton;
    public bool IsDead { get; private set; }
    private GameManager game;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        game = FindFirstObjectByType<GameManager>();
    }

    private void Update()
    {
        if (IsDead || (game != null && game.IsFinished))
        {
            if (agent.isOnNavMesh)
                agent.isStopped = true;
            return;
        }

        if (player != null && agent.isOnNavMesh)
            agent.SetDestination(player.position);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead)
            return;

        IsDead = true;
        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other) => DamagePlayer(other);
    private void OnTriggerStay(Collider other) => DamagePlayer(other);

    private void DamagePlayer(Collider other)
    {
        if (IsDead || (game != null && game.IsFinished))
            return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
            health.TakeDamage(1);
    }
}
