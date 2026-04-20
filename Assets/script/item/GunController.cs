using UnityEngine;
using System.Collections;
using TheDeveloperTrain.SciFiGuns;

/// <summary>
/// สคริปต์หลักสำหรับควบคุมปืน Sci-Fi
/// ทำงานคู่กับ Gun component ของ The Developer Train
/// รับ Input > หักเลือดแทนกระสุน > ยิง Raycast ไปที่ Crosshair เพื่อทำดาเมจ
/// </summary>
[RequireComponent(typeof(Gun))]
public class GunController : MonoBehaviour
{
    [Header("=== Crosshair Targeting ===")]
    [Tooltip("กล้องผู้เล่น (ถ้าไม่ใส่จะหาจาก Camera.main อัตโนมัติ)")]
    public Camera playerCamera;

    [Header("=== Damage Settings ===")]
    [Tooltip("ดาเมจที่จะทำเมื่อกระสุน Hitscan โดนเป้าหมาย")]
    public int damage = 15;
    [Tooltip("ระยะยิงสูงสุดของ Hitscan (หน่วย: เมตร)")]
    public float range = 150f;

    [Header("=== Blood Cost Settings ===")]
    [Tooltip("เสีย HP เท่าไหร่ต่อ 1 นัดที่ยิง (ใช้เลือดแทนกระสุน)")]
    public int hpCostPerShot = 2;
    private PlayerHealth playerHealth;

    [Header("=== Fire Mode ===")]
    [Tooltip("Auto = คลิกค้าง | Semi = คลิกทีละครั้ง")]
    public bool isAutoFire = false;

    [Header("=== Hit Effects ===")]
    [Tooltip("Prefab เอฟเฟกต์ตอนกระสุนโดนผนัง/ศัตรู")]
    public GameObject hitEffectPrefab;

    // ──── Private References ────
    private Gun sciFiGun;          // The Developer Train Gun component
    private bool isTriggerHeld;

    // ──── State ────
    private bool isWaitingForNextShot = false;
    private float shotTimer = 0f;

    void Start()
    {
        sciFiGun = GetComponent<Gun>();

        // ปืนไม่มีรีโหลด — เติมกระสุนให้เต็มตลอด (จำกัดด้วยเลือดอย่างเดียว)
        sciFiGun.currentBulletCount = sciFiGun.stats.magazineSize;

        // ค้นหา PlayerHealth
        playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        // ค้นหากล้อง
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Subscribe: ทุกครั้งที่ Sci-Fi Gun ยิงกระสุนออกมา เราจะทำ Raycast ดาเมจเอง
        // (กระสุน Sci-Fi จะทำหน้าที่เป็น Visual Tracer เท่านั้น)
        sciFiGun.onBulletShot += OnSciGunBulletShot;
    }

    void OnDestroy()
    {
        if (sciFiGun != null)
            sciFiGun.onBulletShot -= OnSciGunBulletShot;
    }

    void Update()
    {
        HandleInput();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  INPUT
    // ─────────────────────────────────────────────────────────────────────────
    private void HandleInput()
    {
        // เช็คปุ่มยิงตาม Fire Mode
        if (isAutoFire)
            isTriggerHeld = Input.GetMouseButton(0);
        else
            isTriggerHeld = Input.GetMouseButtonDown(0);

        // ──── เช็คเลือดพอไหม ────
        bool hasEnoughBlood = playerHealth != null && playerHealth.currentHealth >= hpCostPerShot;

        if (!hasEnoughBlood)
        {
            // เลือดไม่พอ ยิงไม่ได้
            return;
        }

        if (isTriggerHeld)
        {
            // เติมกระสุนให้เต็มก่อนยิงทุกครั้ง เพื่อไม่ให้ระบบ reload ของ Sci-Fi Gun ทำงาน
            sciFiGun.currentBulletCount = sciFiGun.stats.magazineSize;

            // ส่งสัญญาณให้ Sci-Fi Gun ยิง (มันจะจัดการ cooldown, burst, particle ในตัวเอง)
            sciFiGun.Shoot();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CALLED EVERY TIME A BULLET ACTUALLY LEAVES THE GUN
    //  (หลัง shootDelay ผ่านไป — ตรงกับ Animation ปืนยิงสคริปต์จริงๆ)
    // ─────────────────────────────────────────────────────────────────────────
    private void OnSciGunBulletShot()
    {
        // 1. หักเลือดแทนกระสุน
        if (playerHealth != null && hpCostPerShot > 0)
        {
            playerHealth.DrainHealth(hpCostPerShot);
        }

        // 2. Raycast จากกึ่งกลาง Crosshair ไปหาเป้าหมาย
        PerformCrosshairHitscan();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CROSSHAIR HITSCAN
    //  ยิง Ray จากกล้อง (กึ่งกลางจอ) ไปข้างหน้า แล้วตรวจสอบว่าโดนอะไร
    // ─────────────────────────────────────────────────────────────────────────
    private void PerformCrosshairHitscan()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)); // กึ่งกลางจอพอดี

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            // ─ ลองโดน EnemyHealth ก่อน (สคริปต์ใหม่) ─
            EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
            else
            {
                // ─ ลอง EnemyHP สคริปต์เก่า (fallback) ─
                EnemyHP oldHP = hit.collider.GetComponentInParent<EnemyHP>();
                if (oldHP != null)
                {
                    oldHP.TakeDamage(damage);
                }
            }

            // ─ เล่น Hit Effect ตรงจุดกระทบ ─
            if (hitEffectPrefab != null)
            {
                GameObject fx = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(fx, 2f);
            }
        }
    }
}
