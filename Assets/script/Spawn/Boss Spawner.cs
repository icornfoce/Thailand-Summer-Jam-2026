using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [Header("Boss Settings")]
    public GameObject bossPrefab;
    public Transform spawnPoint;

    [Header("Wave Settings")]
    [Tooltip("กำหนดว่าจะให้บอสเกิดทุกๆ กี่เวฟ (เช่น ถ้าตั้ง 5 บอสจะเกิดในเวฟที่ 5, 10, 15...)")]
    public int spawnEveryNWaves = 5;

    // ฟังก์ชันนี้ให้เรียกใช้เมื่อเริ่มต้นเวฟใหม่ (จาก EnemySpawner หรือ WaveManager) โดยส่งเลขเวฟปัจจุบันเข้ามา
    public void OnWaveStarted(int currentWave)
    {
        // ตรวจสอบว่าเวฟปัจจุบันหารด้วย spawnEveryNWaves ลงตัวหรือไม่ (และต้องไม่ใช่เวฟ 0)
        if (currentWave > 0 && currentWave % spawnEveryNWaves == 0)
        {
            SpawnBoss();
        }
    }

    private void SpawnBoss()
    {
        if (bossPrefab != null)
        {
            Transform targetSpawnPoint = spawnPoint != null ? spawnPoint : transform;
            Instantiate(bossPrefab, targetSpawnPoint.position, targetSpawnPoint.rotation);
            Debug.Log("Boss Spawned!");
        }
        else
        {
            Debug.LogWarning("ยังไม่ได้ใส่ Boss Prefab ใน BossSpawner");
        }
    }
}
