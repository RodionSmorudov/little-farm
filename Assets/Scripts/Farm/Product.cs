using UnityEngine;

public class Product : MonoBehaviour
{
    [Header("Visuals")]
    public float floatSpeed = 1f;
    public float floatHeight = 0.1f;
    public float rotateSpeed = 30f;

    [Header("Economy")]
    public int price = 10;   // sell price

    private PlayerInventory inventory;
    private int myIndex;
    private Vector3 startLocalOffset;
    private float randomRotOffset;

    private void Start()
    {
        // random start for rotation
        randomRotOffset = Random.Range(0f, 360f);
    }

    public void Initialize(PlayerInventory inv, int index)
    {
        inventory = inv;
        myIndex = index;
        // Store initial local offset for floating
        startLocalOffset = transform.localPosition;
    }

    public void SetIndex(int newIndex)
    {
        myIndex = newIndex;
    }

    private void Update()
    {
        if (inventory == null) return;

        // Get target position from inventory (snake tail)
        Vector3 targetPos = inventory.GetProductPosition(myIndex);
        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);

        // Floating up/down
        float yOffset = Mathf.Sin(Time.time * floatSpeed + myIndex) * floatHeight;
        transform.position += Vector3.up * yOffset;

        // Random slow rotation
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        transform.Rotate(Vector3.right, (rotateSpeed * 0.5f) * Time.deltaTime);
    }

    // Called when sold
    public void Sell()
    {
        // The inventory will destroy this object; we just need to add money
        MoneyCounterUI.AddMoney(price);
        // Visual effects can be added here (particle, sound)
    }
}