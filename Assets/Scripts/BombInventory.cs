using System;
using System.Collections.Generic;
using UnityEngine;

public class BombInventory : MonoBehaviour
{
    public const int Capacity = 5;
    public const int StartingBombs = 2;
    public int Count { get; private set; } = StartingBombs;
    public event Action<int> CountChanged;

    private readonly List<BombController> placedBombs = new List<BombController>();

    public bool TryPlace(GameObject prefab, Vector3 position)
    {
        if (Count == 0 || prefab == null || prefab.GetComponent<BombController>() == null)
            return false;

        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        placedBombs.Add(instance.GetComponent<BombController>());
        Count--;
        CountChanged?.Invoke(Count);
        return true;
    }

    public bool TryCollect()
    {
        if (Count >= Capacity)
            return false;

        Count++;
        CountChanged?.Invoke(Count);
        return true;
    }

    public void DetonateAll()
    {
        // Clear ownership before calling explosions, which may later cause chain reactions.
        BombController[] bombs = placedBombs.ToArray();
        placedBombs.Clear();
        foreach (BombController bomb in bombs)
        {
            if (bomb != null)
                bomb.Explode();
        }
    }
}
