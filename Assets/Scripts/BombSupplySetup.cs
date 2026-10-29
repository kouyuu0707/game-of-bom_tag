using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Scene-specific supplies and HUD. Inventory and pickup rules live in their own components.
public class BombSupplySetup : MonoBehaviour
{
    [SerializeField] private BombInventory playerInventory;
    [SerializeField] private GameObject pickupPrefab;
    [SerializeField] private Vector3[] pickupPositions;
    private TextMeshProUGUI counter;
    private PlayerHealth health;

    private void Start()
    {
        if (playerInventory == null || pickupPrefab == null)
        {
            Debug.LogError("Bomb supplies require a player inventory and pickup prefab.", this);
            return;
        }

        foreach (Vector3 position in pickupPositions)
            Instantiate(pickupPrefab, position, Quaternion.identity);

        GameObject hud = new GameObject("Bomb HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        hud.transform.SetParent(transform, false);
        Canvas canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        GameObject label = new GameObject("Bomb Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(hud.transform, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(24, -24);
        rect.sizeDelta = new Vector2(600, 140);
        counter = label.GetComponent<TextMeshProUGUI>();
        counter.fontSize = 28;
        counter.color = Color.white;
        counter.outlineWidth = 0.2f;
        counter.raycastTarget = false;
        health = playerInventory.GetComponent<PlayerHealth>();
        if (health != null)
            health.Changed += RefreshHealth;
        playerInventory.CountChanged += Refresh;
        Refresh(playerInventory.Count);
    }

    private void Refresh(int count)
    {
        counter.text = $"HP: {(health != null ? health.Current : 0)}/{PlayerHealth.MaxHealth}\nBombs: {count}/{BombInventory.Capacity}\nB: Place    E: Detonate all yours";
    }

    private void RefreshHealth(int current) => Refresh(playerInventory.Count);

    private void OnDestroy()
    {
        if (playerInventory != null)
            playerInventory.CountChanged -= Refresh;
        if (health != null)
            health.Changed -= RefreshHealth;
    }
}
