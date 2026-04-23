using UnityEngine;
using UnityEngine.UI;

public class MoneyCounterUI : MonoBehaviour
{
    public static MoneyCounterUI Instance { get; private set; }

    public Text moneyText;   // or TMP_Text

    private static int currentMoney = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        UpdateUI();
    }

    public static void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateUI();
    }

    public static int GetMoney() => currentMoney;

    public static bool HasMoney(int amount) => currentMoney >= amount;

    public static bool SpendMoney(int amount)
    {
        if (HasMoney(amount))
        {
            currentMoney -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    private static void UpdateUI()
    {
        if (Instance != null && Instance.moneyText != null)
            Instance.moneyText.text = "Gold: " + currentMoney;
    }
}