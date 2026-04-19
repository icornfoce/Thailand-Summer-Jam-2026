using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 30;
    private int currentHealth;
    
    [Header("Reward Settings")]
    public int healAmountOnDeath = 15; // คืนเลือด/แสงสว่างให้ศัตรูตอนตาย (Core Mechanic)

    [Header("Audio")]
    public AudioClip deathSfx;

    void Start()
    {
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

        // TODO: สั่งเล่น Effect เลือดสาดตรงนี้ในอนาคต

        if (deathSfx != null)
        {
            AudioSource.PlayClipAtPoint(deathSfx, transform.position);
        }

        // ลบศัตรูทิ้ง
        Destroy(gameObject);
    }
}
