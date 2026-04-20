using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 30;
    private int currentHealth;
    
    [Header("Reward Settings")]
    public int healAmountOnDeath = 15; // คืนเลือด/แสงสว่างให้ศัตรูตอนตาย (Core Mechanic)

    [Header("Audio & Visuals")]
    public AudioClip deathSfx;
    [Tooltip("List of VFX prefabs to spawn randomly on death.")]
    public GameObject[] deathVfxPrefabs;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // ฟังก์ชันนี้ใช้สำหรับเพิ่มเลือดสูงสุดให้ศัตรู (มักถูกเรียกใช้จาก EnemySpawner)
    public void ApplyBonusHealth(int bonus)
    {
        maxHealth += bonus;
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + " โดนเข้าไป " + damage + " ดาเมจ! เลือดเหลือ: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " ตายแล้ว! เลือดสาดกระจาย!");

        // เมื่อศัตรูตาย จะฮีลเลือดให้ผู้เล่นตามคอนเซปต์ "ต้องฆ่าถึงจะอยู่รอด (สว่างขึ้น)"
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.Heal(healAmountOnDeath);
        }

        // เช็คว่ามีสคริปต์ BossDrop หรือไม่ ถ้ามีให้ดรอปไอเทม
        BossDrop bossDrop = GetComponent<BossDrop>();
        if (bossDrop != null)
        {
            bossDrop.DropItem();
        }

        // สุ่มเล่น Effect เลือดสาด/ตาย
        if (deathVfxPrefabs != null && deathVfxPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, deathVfxPrefabs.Length);
            GameObject selectedVfx = deathVfxPrefabs[randomIndex];
            if (selectedVfx != null)
            {
                Instantiate(selectedVfx, transform.position, Quaternion.identity);
            }
        }

        if (deathSfx != null)
        {
            AudioSource.PlayClipAtPoint(deathSfx, transform.position);
        }

        // ลบศัตรูทิ้ง
        Destroy(gameObject);
    }
}
