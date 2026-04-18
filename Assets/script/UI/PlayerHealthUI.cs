using UnityEngine;
using TMPro; // ตัวจัดการ TextMeshPro

public class PlayerHealthUI : MonoBehaviour
{
    public TextMeshProUGUI healthText;

    // ฟังก์ชันนี้จะรับเลข 2 ตัวมาจาก PlayerHealth แล้วแปลงเป็นข้อความโชว์บนหน้าจอ
    public void UpdateHealthText(int currentHealth, int maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = "HP: " + currentHealth + " / " + maxHealth;
        }
    }
}
