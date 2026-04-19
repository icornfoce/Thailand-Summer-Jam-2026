using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("ใส่ Prefab ของตัวศัตรูที่ต้องการให้เกิด")]
    public GameObject[] enemyPrefabs;
    
    [Tooltip("ใส่บล็อค หรือ Transform ที่ต้องการให้ศัตรูเกิดตรงนี้")]
    public Transform[] spawnPoints;
    
    [Tooltip("ระยะเวลาดีเลย์ในการเกิดแต่ละตัว (วินาที)")]
    public float spawnInterval = 3f;
    
    [Tooltip("ความสูงที่บวกเพิ่มจากบล็อคเกิด เพื่อกันศัตรูจมพื้นหรือติดตึก")]
    public float spawnHeightOffset = 1.0f;

    [Tooltip("รัศมีการสุ่มตำแหน่งเกิดรอบๆ บล็อคที่เลือก (เพื่อไม่ให้เกิดกระจุกกันที่จุดกึ่งกลาง)")]
    public float spawnRadius = 2.0f;
    
    [Tooltip("จำกัดจำนวนศัตรูสูงสุดในฉากที่เกิดจาก Spawner นี้ (0 = ไม่จำกัด)")]
    public int maxEnemiesAtOnce = 5;

    [Tooltip("ให้ศัตรูเกิดแบบอัตโนมัติหรือไม่")]
    public bool spawnAutomatically = true;

    private float nextSpawnTime;
    private List<GameObject> activeEnemies = new List<GameObject>();

    void Start()
    {
        // เริ่มนับเวลาเพื่อเกิดตัวแรก
        nextSpawnTime = Time.time + spawnInterval;
    }

    void Update()
    {
        if (!spawnAutomatically) return;

        // ล้างลิสต์เผื่อศัตรูตายหรือถูกทำลายไปแล้ว ออกจากลิสต์
        activeEnemies.RemoveAll(enemy => enemy == null);

        // เช็คว่าถึงเวลาเกิดและจำนวนศัตรูยังไม่เกินที่กำหนด (ถ้ากำหนดไว้)
        if (Time.time >= nextSpawnTime && (maxEnemiesAtOnce <= 0 || activeEnemies.Count < maxEnemiesAtOnce))
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval; // รีเซ็ตเวลาสำหรับตัวต่อไป
        }
    }

    public void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("ยังไม่ได้ตั้งค่า Enemy Prefab ใน EnemySpawner!");
            return;
        }

        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("ยังไม่ได้ตั้งค่า Spawn Points ใน EnemySpawner!");
            return;
        }

        // สุ่มจุดเกิดจากบล็อคที่กำหนดเอาไว้
        int spawnIndex = Random.Range(0, spawnPoints.Length);
        Transform selectedSpawnPoint = spawnPoints[spawnIndex];

        // สุ่มศัตรูที่จะเกิด (ถ้าระบุหลายตัว)
        int enemyIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject selectedEnemyPrefab = enemyPrefabs[enemyIndex];

        // สุ่มตำแหน่งกระจายรอบๆ จุดเกิดบนระนาบ 2D (แนวราบ x, z)
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);

        // กำหนดตำแหน่งเกิด โดยบวกตำแหน่งสุ่ม และบวกความสูง (Y-axis) ขึ้นไป
        Vector3 spawnPosition = selectedSpawnPoint.position + randomOffset + Vector3.up * spawnHeightOffset;

        // สร้างศัตรูในฉาก
        GameObject newEnemy = Instantiate(selectedEnemyPrefab, spawnPosition, selectedSpawnPoint.rotation);
        
        // เก็บศัตรูไว้ในลิสต์เพื่อติดตามจำนวนที่อยู่ในฉากปัจจุบัน
        activeEnemies.Add(newEnemy);
        
        Debug.Log("เกิดศัตรู " + selectedEnemyPrefab.name + " ที่จุด: " + selectedSpawnPoint.name + " (กระจายแบบสุ่ม)");
    }
    
    // วาดจุดเกิดให้เห็นใน Scene view ตอนที่เอา Spawner ไปใช้ เพื่อให้กะระยะได้ง่ายขึ้น
    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                Vector3 actualSpawnPos = point.position + Vector3.up * spawnHeightOffset;
                
                // วาดจุดศูนย์กลางของการเกิด
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(actualSpawnPos, 0.5f);
                Gizmos.DrawLine(point.position, actualSpawnPos);

                // วาดรัศมีการสุ่มให้เห็นเป็นวงกว้าง
                Gizmos.color = new Color(0, 1, 0, 0.3f); // วงสีเขียวโปร่งแสง
#if UNITY_EDITOR
                UnityEditor.Handles.color = new Color(0, 1, 0, 0.1f);
                UnityEditor.Handles.DrawSolidDisc(actualSpawnPos, Vector3.up, spawnRadius);
#endif
            }
        }
    }
}
