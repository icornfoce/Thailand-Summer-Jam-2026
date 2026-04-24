using UnityEngine;
using TMPro; // สำหรับจัดการ TextMeshPro

public class wave : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("ลาก TextMeshPro ที่ต้องการแสดงเลขเวฟมาใส่ตรงนี้")]
    public TextMeshProUGUI waveText;
    
    [Header("Spawner Reference")]
    [Tooltip("ลาก Game Object ที่มีสคริปต์ EnemySpawner มาใส่ตรงนี้")]
    public EnemySpawner enemySpawner;

    void Update()
    {
        // ตรวจสอบว่ามีการตั้งค่าทั้ง waveText และ enemySpawner แล้วหรือไม่
        if (waveText != null && enemySpawner != null)
        {
            // ดึงค่าเวฟปัจจุบันจาก EnemySpawner แล้วนำมาแสดงผล
            waveText.text = "Wave: " + enemySpawner.CurrentWave;
        }
    }
}
