using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public const int MaxHealth = 3;
    [SerializeField, Min(0)] private float invulnerabilitySeconds = 1f;
    public int Current { get; private set; } = MaxHealth;
    public bool IsDead => Current == 0;
    public event Action<int> Changed;
    private float nextDamageTime;

    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead || Time.time < nextDamageTime)
            return false;

        Current = Mathf.Max(0, Current - amount);
        nextDamageTime = Time.time + invulnerabilitySeconds;
        if (IsDead && TryGetComponent(out PlayerController player))
            player.canMove = false;
        Changed?.Invoke(Current);
        return true;
    }
}
