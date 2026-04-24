using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RepairableObject : MonoBehaviour
{
    [Header("Repair Settings")]
    public GameObject brokenPrefab;
    public GameObject repairedPrefab;
    public int requiredMoney = 100;
    public float interactionRange = 2f;

    [Header("Audio")]
    public AudioClip successSound;
    public AudioClip failSound;

    [Header("UI Hints")]
    public Vector3 eHintOffset = new Vector3(0, 1.5f, 0);
    public Vector3 priceHintOffset = new Vector3(0, 1.0f, 0);
    public Color priceTextColor = Color.yellow;

    private Transform playerTransform;
    private AudioSource audioSource;
    private GameObject dynamicHint;   // "Press E"
    private GameObject priceHint;     // price display
    private GameObject currentModel;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
        else Debug.LogError("RepairableObject: No GameObject with tag 'Player' found.");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;

        if (brokenPrefab != null)
        {
            currentModel = Instantiate(brokenPrefab, transform);
            currentModel.transform.localPosition = Vector3.zero;
            currentModel.transform.localRotation = Quaternion.identity;
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool canInteract = dist <= interactionRange;

        // Press E hint
        if (canInteract && dynamicHint == null)
            ShowDynamicHint(true);
        else if (!canInteract && dynamicHint != null)
            ShowDynamicHint(false);

        // Price hint
        if (canInteract && priceHint == null)
            ShowPriceHint(true);
        else if (!canInteract && priceHint != null)
            ShowPriceHint(false);

        // Interaction – only if this is the nearest interactable object
        if (canInteract && Input.GetKeyDown(KeyCode.E))
        {
            TryRepair();
            //if (IsNearestInteractable())
            //    TryRepair();
        }
    }

    private void TryRepair()
    {
        if (MoneyCounterUI.SpendMoney(requiredMoney))
        {
            if (successSound != null) audioSource.PlayOneShot(successSound);
            if (repairedPrefab != null)
                Instantiate(repairedPrefab, transform.position, transform.rotation);
            Destroy(gameObject, 1.5f); // small delay for sound
        }
        else
        {
            if (failSound != null) audioSource.PlayOneShot(failSound);
            Debug.Log($"Not enough money! Need {requiredMoney}, have {MoneyCounterUI.GetMoney()}");
        }
    }

    // ---------- Nearest Interactable (avoids overlap with crops) ----------
    //private bool IsNearestInteractable()
    //{
    //    List<Component> allInteractables = new List<Component>();
    //    allInteractables.AddRange(FindObjectsOfType<Crop>());
    //    allInteractables.AddRange(FindObjectsOfType<RepairableObject>());

    //    Component nearest = null;
    //    float minDist = float.MaxValue;

    //    foreach (Component comp in allInteractables)
    //    {
    //        float range = 0f;
    //        bool canBeInteracted = false;

    //        if (comp is Crop crop)
    //        {
    //            if (crop.IsInteractable())
    //            {
    //                range = crop.interactionRange;
    //                canBeInteracted = true;
    //            }
    //        }
    //        else if (comp is RepairableObject rep)
    //        {
    //            range = rep.interactionRange;
    //            canBeInteracted = true;
    //        }

    //        if (!canBeInteracted) continue;

    //        float d = Vector3.Distance(comp.transform.position, playerTransform.position);
    //        if (d <= range && d < minDist)
    //        {
    //            minDist = d;
    //            nearest = comp;
    //        }
    //    }
    //    return nearest == this;
    //}

    // ---------- "Press E" Hint (white circle with bold E) ----------
    private void ShowDynamicHint(bool show)
    {
        if (show && dynamicHint == null)
        {
            dynamicHint = CreateEHint();
            dynamicHint.transform.SetParent(transform);
            dynamicHint.transform.localPosition = eHintOffset;
        }
        else if (!show && dynamicHint != null)
        {
            Destroy(dynamicHint);
            dynamicHint = null;
        }
    }

    private GameObject CreateEHint()
    {
        GameObject hintRoot = new GameObject("PressEHint");
        Canvas canvas = hintRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.scaleFactor = 0.01f;

        CanvasScaler scaler = hintRoot.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;
        hintRoot.AddComponent<GraphicRaycaster>();

        // White circle background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(hintRoot.transform);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.sprite = CreateCircleSprite(32, Color.white);
        bgImage.color = Color.white;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(50, 50);
        bgRect.anchoredPosition = Vector2.zero;

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(hintRoot.transform);
        Text text = textObj.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);
        text.font = font;
        text.text = "E";
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = 60;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(50, 50);
        textRect.anchoredPosition = Vector2.zero;
        textRect.localScale = new Vector2(8, 8);

        // Canvas scale
        RectTransform canvasRect = hintRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(50, 50);
        canvasRect.localScale = Vector3.one * 0.01f;

        Billboard billboard = hintRoot.AddComponent<Billboard>();
        billboard.cameraToFace = Camera.main;

        return hintRoot;
    }

    // ---------- Price Hint (dark panel + gold amount) ----------
    private void ShowPriceHint(bool show)
    {
        if (show && priceHint == null)
        {
            priceHint = CreatePriceHint();
            priceHint.transform.SetParent(transform);
            priceHint.transform.localPosition = priceHintOffset;
        }
        else if (!show && priceHint != null)
        {
            Destroy(priceHint);
            priceHint = null;
        }
    }

    private GameObject CreatePriceHint()
    {
        GameObject hintRoot = new GameObject("PriceHint");
        Canvas canvas = hintRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.scaleFactor = 0.01f;

        CanvasScaler scaler = hintRoot.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;
        hintRoot.AddComponent<GraphicRaycaster>();

        // Dark semi‑transparent background
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(hintRoot.transform);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(256, 96);
        panelRect.anchoredPosition = Vector2.zero;

        // Text: "Repair: X G"
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(hintRoot.transform);
        Text text = textObj.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);
        text.font = font;
        text.text = $"Repair: {requiredMoney} G";
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = priceTextColor;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = 28;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(100, 40);
        textRect.anchoredPosition = Vector2.zero;
        textRect.localScale = new Vector2(8, 8);

        // Scale canvas (world size ~ 1 meter wide)
        RectTransform canvasRect = hintRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100, 40);
        canvasRect.localScale = Vector3.one * 0.01f;

        Billboard billboard = hintRoot.AddComponent<Billboard>();
        billboard.cameraToFace = Camera.main;

        return hintRoot;
    }

    private Sprite CreateCircleSprite(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        int center = size / 2;
        int radius = size / 2;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x - center;
                int dy = y - center;
                if (dx * dx + dy * dy <= radius * radius)
                    colors[y * size + x] = color;
                else
                    colors[y * size + x] = Color.clear;
            }
        }
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}