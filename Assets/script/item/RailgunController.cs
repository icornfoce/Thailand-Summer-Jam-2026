using UnityEngine;
using System.Collections;
using TheDeveloperTrain.SciFiGuns;

/// <summary>
/// Railgun Controller — ยิงเป็นเส้นตรงทะลุทุกอย่าง
/// ใช้ RaycastAll เพื่อทำดาเมจทุกเป้าที่อยู่ในแนวยิง
/// รองรับ Blood-as-Ammo + Beam Particle Prefab
/// </summary>
[RequireComponent(typeof(Gun))]
public class RailgunController : MonoBehaviour
{
    [Header("=== Crosshair Targeting ===")]
    [Tooltip("กล้องผู้เล่น (ถ้าไม่ใส่จะหาจาก Camera.main อัตโนมัติ)")]
    public Camera playerCamera;

    [Header("=== Damage Settings ===")]
    [Tooltip("ดาเมจต่อเป้าหมายที่ทะลุผ่าน")]
    public int damage = 100;
    [Tooltip("ระยะยิงสูงสุด (เมตร)")]
    public float range = 500f;

    [Header("=== Blood Cost Settings ===")]
    [Tooltip("เสีย HP เท่าไหร่ต่อ 1 นัดที่ยิง")]
    public int hpCostPerShot = 15;
    private PlayerHealth playerHealth;

    [Header("=== Fire Cooldown ===")]
    [Tooltip("เวลาคูลดาวน์ระหว่างนัด (วินาที)")]
    public float fireCooldown = 1.5f;
    private float nextFireTime = 0f;

    [Header("=== Beam Particle ===")]
    [Tooltip("ลาก Prefab Particle Beam มาใส่ตรงนี้")]
    public GameObject beamPrefab;
    [Tooltip("ระยะเวลาที่ Beam Particle แสดงผลก่อนทำลาย (วินาที)")]
    public float beamLifetime = 2f;
    [Tooltip("ขนาด Beam (ยิ่งเลขเยอะ Beam ยิ่งยาว)")]
    public float beamScale = 5f;

    [Header("=== Hit Effects ===")]
    [Tooltip("Prefab เอฟเฟกต์ตอนกระสุนโดน")]
    public GameObject hitEffectPrefab;

    [Header("=== Muzzle ===")]
    [Tooltip("จุดปากกระบอกปืน (ถ้าไม่ใส่จะใช้ตำแหน่งของ script นี้)")]
    public Transform muzzlePoint;

    // ──── Private References ────
    private Gun sciFiGun;

    void Start()
    {
        sciFiGun = GetComponent<Gun>();

        // ปืนไม่มีรีโหลด — เติมกระสุนให้เต็มตลอด
        sciFiGun.currentBulletCount = sciFiGun.stats.magazineSize;

        // ค้นหา PlayerHealth
        playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        // ค้นหากล้อง
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Muzzle fallback
        if (muzzlePoint == null)
            muzzlePoint = transform;

        // Subscribe: เมื่อ Sci-Fi Gun ยิงกระสุนออก → ทำ Railgun Beam
        sciFiGun.onBulletShot += OnRailgunFired;
    }

    void OnDestroy()
    {
        if (sciFiGun != null)
            sciFiGun.onBulletShot -= OnRailgunFired;
    }

    void Update()
    {
        HandleInput();
    }

    // ─────────────────────────────────────────────────────────
    //  INPUT
    // ─────────────────────────────────────────────────────────
    private void HandleInput()
    {
        // Railgun = Semi-Auto เท่านั้น (คลิกทีละนัด)
        if (!Input.GetMouseButtonDown(0)) return;

        // เช็คคูลดาวน์
        if (Time.time < nextFireTime) return;

        // เช็คเลือดพอไหม
        bool hasEnoughBlood = playerHealth != null && playerHealth.currentHealth >= hpCostPerShot;
        if (!hasEnoughBlood) return;

        // เติมกระสุนให้เต็ม (ไม่ใช้ระบบ reload)
        sciFiGun.currentBulletCount = sciFiGun.stats.magazineSize;

        // ยิง — Sci-Fi Gun จะเล่น VFX/Animation แล้วเรียก onBulletShot
        sciFiGun.Shoot();
        nextFireTime = Time.time + fireCooldown;
    }

    // ─────────────────────────────────────────────────────────
    //  เมื่อกระสุนออกจากปืนจริงๆ (หลัง shootDelay)
    // ─────────────────────────────────────────────────────────
    private void OnRailgunFired()
    {
        // 1. หักเลือดแทนกระสุน
        if (playerHealth != null && hpCostPerShot > 0)
            playerHealth.DrainHealth(hpCostPerShot);

        // 2. ยิง penetrating beam
        PerformPenetratingBeam();
    }

    // ─────────────────────────────────────────────────────────
    //  PENETRATING BEAM — ทะลุทุกอย่างในแนวยิง
    // ─────────────────────────────────────────────────────────
    private void PerformPenetratingBeam()
    {
        if (playerCamera == null) return;

        // Ray จากกึ่งกลางจอ
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // ───── RaycastAll — ทะลุทุกอย่าง ─────
        RaycastHit[] hits = Physics.RaycastAll(ray, range);

        // เรียงลำดับจากใกล้ไปไกล
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // จุดสิ้นสุดของ Beam (ถ้าไม่โดนอะไรเลย ให้ไปสุดระยะ)
        Vector3 beamEndPoint = ray.origin + ray.direction * range;

        foreach (RaycastHit hit in hits)
        {
            // ข้ามตัว Player เอง
            if (hit.collider.CompareTag("Player")) continue;

            // ─ ทำดาเมจ EnemyHealth ─
            EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
                Debug.Log($"[Railgun] ⚡ ทะลุโดน {hit.collider.name} → {damage} DMG");
            }
            else
            {
                EnemyHP oldHP = hit.collider.GetComponentInParent<EnemyHP>();
                if (oldHP != null)
                {
                    oldHP.TakeDamage((float)damage);
                    Debug.Log($"[Railgun] ⚡ ทะลุโดน {hit.collider.name} → {damage} DMG (EnemyHP)");
                }
            }

            // ─ เล่น Hit Effect ─
            if (hitEffectPrefab != null)
            {
                GameObject fx = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(fx, 2f);
            }
        }

        // ──── Spawn Beam Particle จาก Muzzle ไปยังจุดสิ้นสุด ────
        SpawnBeamParticle(muzzlePoint.position, beamEndPoint);
    }

    // ─────────────────────────────────────────────────────────
    //  BEAM PARTICLE — Spawn Prefab จาก Muzzle ไปยังปลายทาง
    //  ยืด Scale X ให้ครอบคลุมระยะทางทั้งหมด
    // ─────────────────────────────────────────────────────────
    private void SpawnBeamParticle(Vector3 start, Vector3 end)
    {
        if (beamPrefab == null)
        {
            Debug.LogWarning("[Railgun] ยังไม่ได้ใส่ Beam Prefab!");
            return;
        }

        // Particle emit ตามแกน Y (ขึ้นบน) → หมุน 90° บน X เพื่อให้ Y ชี้ไปข้างหน้าแทน
        Quaternion beamRotation = playerCamera.transform.rotation * Quaternion.Euler(90f, 0f, 0f);

        // Spawn beam ที่ปากกระบอกปืน หันตามทิศกล้อง
        GameObject beam = Instantiate(beamPrefab, muzzlePoint.position, beamRotation);

        // ยืด Beam ให้ยาวขึ้น
        beam.transform.localScale = Vector3.one * beamScale;

        // ปล่อยเป็น World Space ทันที เพื่อไม่ให้ Beam ขยับตามปืนหลังยิง
        beam.transform.SetParent(null);

        // ทำลายหลังจากหมดเวลา
        Destroy(beam, beamLifetime);

        Debug.Log($"[Railgun] 🔵 Beam Spawned");
    }
}
