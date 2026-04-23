using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Stack Settings")]
    public int maxProducts = 8;
    public float productSpacing = 0.8f;
    public Vector3 tailOffset = new Vector3(0, 0, -0.5f);

    private List<Product> products = new List<Product>();
    private Transform playerTransform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        playerTransform = transform;
    }

    public bool HasSpace(int amount = 1)
    {
        return products.Count + amount <= maxProducts;
    }

    public bool AddProducts(GameObject productPrefab, int amount)
    {
        if (!HasSpace(amount))
        {
            Debug.Log("Inventory full, cannot add products.");
            return false;
        }

        for (int i = 0; i < amount; i++)
        {
            GameObject newProductObj = Instantiate(productPrefab, transform.position, Quaternion.identity);
            Product product = newProductObj.GetComponent<Product>();
            if (product == null)
            {
                Debug.LogError("Product prefab must have Product component!");
                Destroy(newProductObj);
                return false;
            }
            product.Initialize(this, products.Count);
            products.Add(product);
        }
        UpdateProductIndices();
        return true;
    }

    public int SellAllProducts()
    {
        int totalValue = 0;
        foreach (Product p in products)
        {
            totalValue += p.price;
            Destroy(p.gameObject);
        }
        products.Clear();
        return totalValue;
    }

    public int GetCurrentCount() => products.Count;

    private void UpdateProductIndices()
    {
        for (int i = 0; i < products.Count; i++)
        {
            products[i].SetIndex(i);
        }
    }

    public Vector3 GetProductPosition(int index)
    {
        if (playerTransform == null) return Vector3.zero;
        Vector3 basePos = playerTransform.position + playerTransform.TransformDirection(tailOffset);
        Vector3 direction = -playerTransform.forward;
        return basePos + direction * (index * productSpacing);
    }
}