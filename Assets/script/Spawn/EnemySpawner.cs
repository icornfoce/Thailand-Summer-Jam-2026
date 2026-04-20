using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Wave Settings (Auto Procedural)")]
    [Tooltip("จำนวนเวฟทั้งหมด (0 = เกิดไปเรื่อยๆ เป็น Endless)")]
    public int totalWaves = 0;
    
    [Tooltip("จำนวนศัตรูที่จะเกิดในเวฟแรก")]
    public int initialEnemyCount = 3;
    
    [Tooltip("จำนวนศัตรูที่เพิ่มขึ้นในแต่ละรอบเวฟ")]
    public int enemyIncreasePerWave = 2;
    
    [Tooltip("ระยะเวลาดีเลย์ในการเกิดแต่ละตัวในเวฟเดียวกัน (วินาที)")]
    public float spawnInterval = 1.0f;
    
    [Tooltip("เวลาพักหน่วงก่อนจะเริ่มเวฟถัดไป (วินาที)")]
    public float timeBetweenWaves = 5f;
    
    [Tooltip("ถ้าติ๊ก ระบบจะรอให้ศัตรูในเวฟปัจจุบันตายหมดก่อน จึงจะเริ่มนับเวลาไปเวฟใหม่")]
    public bool waitToClearEnemies = true;

    [Header("Difficulty Settings")]
    [Tooltip("เลือดของศัตรูที่จะบวกเพิ่มขึ้นในแต่ละเวฟ (เช่น เวฟละ 10)")]
    public int bonusHealthPerWave = 10;

    [Header("Spawn Settings")]
    [Tooltip("ใส่ Prefab ของตัวศัตรูที่ต้องการให้เกิด")]
    public GameObject[] enemyPrefabs;

    [Tooltip("ใส่ Prefab ของ Boss (สุ่มเกิด 1 ตัวในเวฟที่เป็นบอส)")]
    public GameObject[] bossPrefabs;

    [Tooltip("บอสจะเกิดทุกๆ กี่เวฟ (ค่าเริ่มต้น 10 = เวฟ 10, 20, 30...)")]
    public int bossWaveInterval = 10;
    
    [Tooltip("จำนวนบอสที่จะเพิ่มขึ้นในแต่ละรอบบอส (เช่น รอบ 1=1 ตัว, รอบ 2=2 ตัว)")]
    public int bossIncreasePerBossWave = 1;
    
    [Tooltip("ใส่บล็อค หรือ Transform ที่ต้องการให้ศัตรูเกิดตรงนี้")]
    public Transform[] spawnPoints;
    
    [Tooltip("ความสูงที่บวกเพิ่มจากบล็อคเกิด เพื่อกันศัตรูจมพื้นหรือติดตึก")]
    public float spawnHeightOffset = 1.0f;

    [Tooltip("รัศมีการสุ่มตำแหน่งเกิดรอบๆ บล็อคที่เลือก (เพื่อไม่ให้เกิดกระจุกกันที่จุดกึ่งกลาง)")]
    public float spawnRadius = 2.0f;

    [Tooltip("จำกัดจำนวนศัตรูสูงสุดในฉากที่เกิดจาก Spawner นี้ (0 = ไม่จำกัด)")]
    public int maxEnemiesAtOnce = 5;

    [Tooltip("จำนวนที่บวกเพิ่มให้กับขีดจำกัดศัตรูสูงสุดในแต่ละเวฟ")]
    public int maxEnemiesIncreasePerWave = 1;

    [Tooltip("ให้ศัตรูเกิดแบบอัตโนมัติหรือไม่")]
    public bool spawnAutomatically = true;

    public int CurrentWave { get { return currentWaveIndex; } }
    private int currentWaveIndex = 0;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool isSpawningWave = false;
    private bool hasSpawnedFirstBoss = false;

    void Start()
    {
        if (spawnAutomatically)
        {
            StartCoroutine(SpawnManager());
        }
    }

    void Update()
    {
        // ล้างศัตรูที่ตายแล้วออกจากลิสต์ตลอดเวลา
        activeEnemies.RemoveAll(enemy => enemy == null);
    }

    /// <summary>
    /// ใช้เรียกถ้าต้องการสั่งให้เริ่มเกิดศัตรูผ่านโค้ดหรือปุ่มอื่นๆ
    /// </summary>
    public void StartSpawning()
    {
        if (!isSpawningWave)
        {
            StartCoroutine(SpawnManager());
        }
    }

    IEnumerator SpawnManager()
    {
        isSpawningWave = true;
        currentWaveIndex = 1;
        
        while (totalWaves <= 0 || currentWaveIndex <= totalWaves)
        {
            Debug.Log("เริ่มเวฟที่: " + currentWaveIndex);

            // คำนวณหาว่าเวฟปัจจุบันต้องมีศัตรูเท่าไหร่
            // สูตร: เวฟแรก = initialEnemyCount, เวฟต่อๆมาให้บวก enemyIncreasePerWave ตามรอบ
            int enemiesForThisWave = initialEnemyCount + (enemyIncreasePerWave * (currentWaveIndex - 1));
            int enemiesSpawnedInWave = 0;
            
            bool isBossWave = (bossPrefabs != null && bossPrefabs.Length > 0 && bossWaveInterval > 0 && currentWaveIndex % bossWaveInterval == 0);
            
            int bossesToSpawn = 0;
            int bossesSpawnedInWave = 0;

            if (isBossWave)
            {
                // คำนวณว่าต้องเกิดบอสกี่ตัว
                int bossWaveRound = currentWaveIndex / bossWaveInterval;
                bossesToSpawn = 1 + (bossIncreasePerBossWave * (bossWaveRound - 1));
            }

            // คำนวณขีดจำกัดศัตรูสูงสุดในฉากสำหรับเวฟนี้
            int currentMaxEnemies = maxEnemiesAtOnce;
            if (maxEnemiesAtOnce > 0)
            {
                currentMaxEnemies += (maxEnemiesIncreasePerWave * (currentWaveIndex - 1));
            }

            // วนลูปเกิดศัตรูหรือบอสจนกว่าจะครบกำหนดในเวฟนั้น
            while (enemiesSpawnedInWave < enemiesForThisWave || bossesSpawnedInWave < bossesToSpawn)
            {
                // ถ้าจำกัดจำนวนและเกิดเต็มแม็กซ์แล้ว ให้หยุดรอจนกว่าจะมีศัตรูตาย
                if (currentMaxEnemies > 0 && activeEnemies.Count >= currentMaxEnemies)
                {
                    yield return null; // รอเฟรมถัดไป
                    continue;
                }

                // สุ่มเกิดบอสก่อน ถ้าบอสยังไม่ครบ
                if (isBossWave && bossesSpawnedInWave < bossesToSpawn)
                {
                    SpawnBoss();
                    bossesSpawnedInWave++;
                }
                // ถ้าบอสครบแล้ว หรือไม่ใช่เวฟบอส ให้เกิดศัตรูแทน
                else if (enemiesSpawnedInWave < enemiesForThisWave)
                {
                    SpawnEnemy();
                    enemiesSpawnedInWave++;
                }
                
                // รอระยะเวลาก่อนเกิดตัวถัดไปในเวฟ
                yield return new WaitForSeconds(spawnInterval);
            }

            // หากเปิดระบบให้รอเคลียร์ศัตรูหมด ก็รอไปเรื่อยๆ จนกว่า List จะว่าง
            if (waitToClearEnemies)
            {
                while (activeEnemies.Count > 0)
                {
                    yield return null;
                }
            }

            // ถ้ามีกำหนดจำนวนเวฟสูงสุด และเวฟปัจจุบันเล่นจบแล้ว ให้ออกลูปทันที
            if (totalWaves > 0 && currentWaveIndex >= totalWaves)
            {
                break;
            }

            // เตรียมเข้าสู่เวฟถัดไป
            currentWaveIndex++;
            Debug.Log("รอเข้าเวฟที่ " + currentWaveIndex + "...");
            yield return new WaitForSeconds(timeBetweenWaves);
        }
        
        if (totalWaves > 0)
        {
            Debug.Log("จบครบทุกเวฟที่กำหนดใน Spawner แล้ว!");
        }
        isSpawningWave = false;
    }

    public void SpawnBoss()
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0) return;
        if (spawnPoints.Length == 0) return;

        int spawnIndex = Random.Range(0, spawnPoints.Length);
        Transform selectedSpawnPoint = spawnPoints[spawnIndex];

        int bossIndex = Random.Range(0, bossPrefabs.Length);
        GameObject selectedBossPrefab = bossPrefabs[bossIndex];

        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
        Vector3 spawnPosition = selectedSpawnPoint.position + randomOffset + Vector3.up * spawnHeightOffset;

        GameObject newBoss = Instantiate(selectedBossPrefab, spawnPosition, selectedSpawnPoint.rotation);
        
        EnemyHealth healthScript = newBoss.GetComponent<EnemyHealth>();
        if (healthScript != null && currentWaveIndex > 1)
        {
            // บวกเลือดบอสตามสเกลเดียวกับศัตรูทั่วไป หรือถ้าอยากแก้สเกลทีหลังก็แก้ตรงนี้ได้
            int bonusHealth = bonusHealthPerWave * (currentWaveIndex - 1);
            healthScript.ApplyBonusHealth(bonusHealth);
        }

        // จัดการเรื่องดรอปไอเทม (ให้ดรอปแค่ตัวแรกที่เกิดเท่านั้น)
        if (hasSpawnedFirstBoss)
        {
            BossDrop dropScript = newBoss.GetComponent<BossDrop>();
            if (dropScript != null)
            {
                dropScript.canDrop = false; // ปิดการดรอปสำหรับบอสตัวต่อๆ ไป
            }
        }
        else
        {
            hasSpawnedFirstBoss = true; // บันทึกว่าบอสตัวแรกเกิดแล้ว
        }

        activeEnemies.Add(newBoss);
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
        
        // เพิ่มเลือดให้ศัตรูตามเวฟปัจจุบัน (ลบ 1 เพื่อให้เวฟแรกสุดมีเลือดเท่าเดิม)
        EnemyHealth healthScript = newEnemy.GetComponent<EnemyHealth>();
        if (healthScript != null && currentWaveIndex > 1)
        {
            int bonusHealth = bonusHealthPerWave * (currentWaveIndex - 1);
            healthScript.ApplyBonusHealth(bonusHealth);
        }

        // เก็บศัตรูไว้ในลิสต์เพื่อติดตามจำนวนที่อยู่ในฉากปัจจุบัน
        activeEnemies.Add(newEnemy);
        
        // คอมเม้น Log ทิ้งเพื่อไม่ให้รก Console (เปิดใช้ได้เวลา Debug)
        // Debug.Log("เกิดศัตรู " + selectedEnemyPrefab.name + " ที่จุด: " + selectedSpawnPoint.name + " (กระจายแบบสุ่ม)");
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
