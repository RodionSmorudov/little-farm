using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Crop : MonoBehaviour
{
    [System.Serializable]
    public class StageData
    {
        public GameObject modelPrefab;
        public float timeToGrow = 0f;
        public float timeToDie = 0f;
    }

    [Header("Stages")]
    public StageData[] stages = new StageData[5];

    [Header("Interaction")]
    public float interactionRange = 2f;
    public KeyCode interactKey = KeyCode.E;

    [Header("UI Icons")]
    public GameObject wateringIconPrefab;
    public GameObject harvestIconPrefab;
    public GameObject deadIconPrefab;
    public Vector3 statusIconOffset = new Vector3(0, 1.5f, 0);
    public Vector3 hintOffset = new Vector3(0, 2.0f, 0);

    [Header("Product")]
    public GameObject productPrefab;
    public int productAmount = 1;

    [Header("Audio")]
    public AudioClip waterSound;
    public AudioClip harvestSound;
    public AudioClip shovelSound;
    public AudioClip inventoryFullSound;   // new!

    private int currentStage = 0;
    private GameObject currentModel = null;
    private float growthTimer = 0f;
    private float deathTimer = 0f;
    private bool isWaitingForAction = false;
    private GameObject activeIcon = null;
    private GameObject dynamicHint = null;
    private Transform playerTransform = null;
    private AudioSource audioSource;

    private static List<Crop> allCrops = new List<Crop>();

    private void Awake() => allCrops.Add(this);
    private void OnDestroy() => allCrops.Remove(this);

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
        else Debug.LogError("Crop: No GameObject with tag 'Player' found.");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;

        SetStage(0);
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Timers for growing stages (0-2)
        if (!isWaitingForAction && currentStage < 3)
        {
            growthTimer += Time.deltaTime;
            if (growthTimer >= stages[currentStage].timeToGrow)
            {
                growthTimer = 0f;
                isWaitingForAction = true;
                deathTimer = 0f;
                ShowCorrectStatusIcon(true);
            }
        }
        else if (isWaitingForAction && currentStage < 3 && stages[currentStage].timeToDie > 0)
        {
            deathTimer += Time.deltaTime;
            if (deathTimer >= stages[currentStage].timeToDie)
            {
                isWaitingForAction = false;
                SetStage(4);
                isWaitingForAction = true;
                ShowCorrectStatusIcon(true);
            }
        }

        // Stage 3 death timer
        if (isWaitingForAction && currentStage == 3 && stages[3].timeToDie > 0)
        {
            deathTimer += Time.deltaTime;
            if (deathTimer >= stages[3].timeToDie)
            {
                isWaitingForAction = false;
                SetStage(4);
                isWaitingForAction = true;
                ShowCorrectStatusIcon(true);
            }
        }

        // "Press E" hint
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool canInteract = (dist <= interactionRange) && (isWaitingForAction || currentStage == 4);
        if (canInteract && dynamicHint == null)
            ShowDynamicHint(true);
        else if (!canInteract && dynamicHint != null)
            ShowDynamicHint(false);

        // Interaction (nearest crop)
        if (Input.GetKeyDown(interactKey))
        {
            Crop nearest = GetNearestCrop();
            if (nearest == this && canInteract)
                Interact();
        }
    }

    private void Interact()
    {
        if (currentStage == 4) // dead – shovel
        {
            if (shovelSound != null) audioSource.PlayOneShot(shovelSound);
            ResetCrop();
        }
        else if (isWaitingForAction)
        {
            if (currentStage < 3) // watering
            {
                if (waterSound != null) audioSource.PlayOneShot(waterSound);
                AdvanceToNextStage();
            }
            else if (currentStage == 3) // harvest
            {
                if (PlayerInventory.Instance != null && productPrefab != null)
                {
                    // Check for space first
                    if (!PlayerInventory.Instance.HasSpace(productAmount))
                    {
                        // Inventory full – play dedicated sound and exit (no harvest sound)
                        if (inventoryFullSound != null) audioSource.PlayOneShot(inventoryFullSound);
                        Debug.Log("Inventory full, cannot harvest.");
                        return; // crop remains harvestable
                    }

                    // Space available – play harvest sound and proceed
                    if (harvestSound != null) audioSource.PlayOneShot(harvestSound);
                    bool success = PlayerInventory.Instance.AddProducts(productPrefab, productAmount);
                    if (success)
                    {
                        ResetCrop();
                    }
                    else
                    {
                        // Fallback (should not happen after HasSpace check)
                        if (inventoryFullSound != null) audioSource.PlayOneShot(inventoryFullSound);
                    }
                }
                else
                {
                    // No inventory reference or missing product prefab – old fallback (play harvest sound and reset)
                    if (harvestSound != null) audioSource.PlayOneShot(harvestSound);
                    ResetCrop();
                }
            }
        }
    }

    private void AdvanceToNextStage()
    {
        if (currentStage < 2)
        {
            SetStage(currentStage + 1);
            isWaitingForAction = false;
            growthTimer = 0f;
            deathTimer = 0f;
            ShowCorrectStatusIcon(false);
        }
        else if (currentStage == 2)
        {
            SetStage(3);
            isWaitingForAction = true;
            growthTimer = 0f;
            deathTimer = 0f;
            ShowCorrectStatusIcon(true);
        }
    }

    private void ResetCrop()
    {
        SetStage(0);
        isWaitingForAction = false;
        growthTimer = 0f;
        deathTimer = 0f;
        ShowCorrectStatusIcon(false);
    }

    private void SetStage(int newStage)
    {
        currentStage = newStage;
        if (currentModel != null) Destroy(currentModel);
        if (stages[currentStage].modelPrefab != null)
        {
            currentModel = Instantiate(stages[currentStage].modelPrefab, transform);
            currentModel.transform.localPosition = Vector3.zero;
            currentModel.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning($"Crop: No model prefab for stage {currentStage}");
        }
    }

    private void ShowCorrectStatusIcon(bool show)
    {
        if (activeIcon != null)
        {
            Destroy(activeIcon);
            activeIcon = null;
        }
        if (!show) return;

        GameObject iconPrefab = null;
        if (currentStage == 4)
            iconPrefab = deadIconPrefab;
        else if (currentStage == 3)
            iconPrefab = harvestIconPrefab;
        else if (currentStage >= 0 && currentStage <= 2 && isWaitingForAction)
            iconPrefab = wateringIconPrefab;

        if (iconPrefab != null)
        {
            activeIcon = Instantiate(iconPrefab, transform);
            activeIcon.transform.localPosition = statusIconOffset;
        }
    }

    // ========== DYNAMIC "PRESS E" HINT ==========
    private void ShowDynamicHint(bool show)
    {
        if (show && dynamicHint == null)
        {
            dynamicHint = CreateDynamicHint();
            dynamicHint.transform.SetParent(transform);
            dynamicHint.transform.localPosition = hintOffset;
        }
        else if (!show && dynamicHint != null)
        {
            Destroy(dynamicHint);
            dynamicHint = null;
        }
    }

    private GameObject CreateDynamicHint()
    {
        GameObject hintRoot = new GameObject("PressEHint");
        Canvas canvas = hintRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.scaleFactor = 0.01f;

        CanvasScaler scaler = hintRoot.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;
        hintRoot.AddComponent<GraphicRaycaster>();

        // Background
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
        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont("Arial", 32);
        }
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

    private Crop GetNearestCrop()
    {
        if (playerTransform == null) return null;
        Crop nearest = null;
        float minDist = float.MaxValue;
        foreach (Crop c in allCrops)
        {
            float d = Vector3.Distance(c.transform.position, playerTransform.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = c;
            }
        }
        return nearest;
    }
}

// Billboard script remains the same
public class Billboard : MonoBehaviour
{
    public Camera cameraToFace;
    void LateUpdate()
    {
        if (cameraToFace == null) cameraToFace = Camera.main;
        if (cameraToFace != null)
            transform.LookAt(transform.position + cameraToFace.transform.rotation * Vector3.forward,
                            cameraToFace.transform.rotation * Vector3.up);
    }
}