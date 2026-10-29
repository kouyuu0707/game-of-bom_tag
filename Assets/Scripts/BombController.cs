using System.Collections.Generic;
using UnityEngine;

public class BombController : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float blastRadius = 3f;
    private bool exploded;

    public void Explode()
    {
        if (exploded)
            return;

        exploded = true;
        // Include trigger colliders: the current enemy uses a trigger capsule.
        Physics.SyncTransforms();
        Collider[] hits = Physics.OverlapSphere(transform.position, blastRadius,
            Physics.AllLayers, QueryTriggerInteraction.Collide);
        var players = new HashSet<PlayerHealth>();
        var enemies = new HashSet<EnemyController>();
        foreach (Collider hit in hits)
        {
            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();
            if (player != null)
                players.Add(player);
            EnemyController enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy != null)
                enemies.Add(enemy);
        }

        foreach (PlayerHealth player in players)
            player.TakeDamage(1);
        foreach (EnemyController enemy in enemies)
            enemy.TakeDamage(1);

        ShowBlastRadius();
        Destroy(gameObject);
    }

    private void ShowBlastRadius()
    {
        // A short-lived ring shows the horizontal blast radius without blocking the view.
        GameObject effect = new GameObject("Blast Radius");
        LineRenderer ring = effect.AddComponent<LineRenderer>();
        MeshRenderer mesh = GetComponent<MeshRenderer>();
        if (mesh != null)
            ring.sharedMaterial = mesh.sharedMaterial;
        ring.loop = true;
        ring.useWorldSpace = true;
        ring.widthMultiplier = 0.12f;
        ring.positionCount = 64;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2 / ring.positionCount;
            ring.SetPosition(i, transform.position + new Vector3(
                Mathf.Cos(angle) * blastRadius, 0.1f, Mathf.Sin(angle) * blastRadius));
        }
        Destroy(effect, 0.35f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, blastRadius);
    }
}
