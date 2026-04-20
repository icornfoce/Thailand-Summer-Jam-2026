using UnityEngine;

public class BossDrop : MonoBehaviour
{
    [Header("Drop Settings")]
    [Tooltip("The prefab of the item to drop when the boss dies.")]
    public GameObject itemPrefab;
    
    [Tooltip("How high above the boss's center the item should drop.")]
    public float dropHeightOffset = 0.5f;

    public void DropItem()
    {
        if (itemPrefab != null)
        {
            // Calculate drop position with offset
            Vector3 dropPos = transform.position + Vector3.up * dropHeightOffset;
            GameObject dropped = Instantiate(itemPrefab, dropPos, Quaternion.identity);

            // Automatically add the floating effect script if the prefab doesn't have it
            if (dropped.GetComponent<ItemFloat>() == null)
            {
                dropped.AddComponent<ItemFloat>();
            }
        }
        else
        {
            Debug.LogWarning("BossDrop: No item prefab assigned to drop!");
        }
    }
}
