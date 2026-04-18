using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Events")]
    public UnityEvent<int, int> OnHealthChanged; // ส่งค่า (เลือดปัจจุบัน, เลือดสูงสุด) ไปอัปเดต UI ได้
    public UnityEvent OnTakeDamage;
    public UnityEvent OnPlayerDeath;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth); // ส่งให้ UI ตอนเริ่มเกม
    }

    // ฟังก์ชันสำหรับรับความเสียหาย (เอาไปผูกกับศัตรู/กับดัก)
    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return; // ถ้าตายแล้ว ไม่ต้องทำอะไร

        currentHealth -= amount;
        OnTakeDamage?.Invoke(); // สั่งให้เกิดเอฟเฟกต์/เสียงตอนโดนตี

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"Player took {amount} damage! Current Health: {currentHealth}");
    }

    // ฟังก์ชันสำหรับเพิ่มเลือด (เอาไปผูกกับเวลาฆ่าศัตรูตายตามคอนเซปต์เกม)
    public void Heal(int amount)
    {
        if (currentHealth <= 0) return; // ตายแล้วเพิ่มเลือดไม่ได้เว้นแต่จะใช้ระบบชุบ

        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"Player healed for {amount}! Current Health: {currentHealth}");
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        OnPlayerDeath?.Invoke(); // ใช้เรียกหน้าต่าง Game Over
    }
}
