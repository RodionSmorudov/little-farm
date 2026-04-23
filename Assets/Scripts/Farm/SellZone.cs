using UnityEngine;

public class SellZone : MonoBehaviour
{
    [Header("Settings")]
    public AudioClip sellSound;
    public GameObject floatingTotalPrefab;   // optional: prefab with Text or TMPro

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (PlayerInventory.Instance == null) return;

        int productCount = PlayerInventory.Instance.GetCurrentCount();
        if (productCount == 0) return;

        // Sell all at once and get total price
        int totalGold = PlayerInventory.Instance.SellAllProducts();

        // Play sound once
        if (sellSound != null && audioSource != null)
            audioSource.PlayOneShot(sellSound);

        // Add money
        MoneyCounterUI.AddMoney(totalGold);

        // Show floating total text
        if (floatingTotalPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 1.5f;
            GameObject floatObj = Instantiate(floatingTotalPrefab, spawnPos, Quaternion.identity);
            // Try to set text to "+totalGold"
            var textComp = floatObj.GetComponent<UnityEngine.UI.Text>();
            if (textComp != null) textComp.text = "+" + totalGold;
            else
            {
                var tmp = floatObj.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmp != null) tmp.text = "+" + totalGold;
            }
            Destroy(floatObj, 1.5f);
        }

        Debug.Log($"Sold {productCount} products for {totalGold} gold.");
    }
}