using UnityEngine;
using System.Collections;

/// <summary>
/// MeleeFist — อาวุธหมัด
/// คลิกซ้าย = ต่อย (SphereCast จากกล้อง)
/// - โดน Enemy → ทำดาเมจ + Hitstop (slow 0.2 วิ)
/// - โดน EnemyProjectile → Parry! กระเด่งกระสุนกลับไป
/// </summary>
public class MeleeFist : MonoBehaviour
{
    [Header("=== Punch Settings ===")]
    [Tooltip("ปุ่ม Quick Melee (ต่อยแทรกได้ทุกเวลา)")]
    public KeyCode punchKey = KeyCode.F;
    [Tooltip("กล้องผู้เล่น")]
    public Camera playerCamera;

    [Header("=== Visual Settings ===")]
    [Tooltip("โมเดล/แขน ที่จะให้แสดงตอนต่อย (ถ้ามี)")]
    public GameObject meleeVisualObject;
    [Tooltip("Animator สำหรับเล่นท่าต่อย (ถ้ามี)")]
    public Animator meleeAnimator;
    [Tooltip("ชื่อ Trigger ใน Animator")]
    public string punchTriggerName = "Punch";
    [Tooltip("ระยะเวลาที่แขนจะโชว์ก่อนซ่อน (ถ้าไม่ได้ใช้ Animator)")]
    public float visualShowDuration = 0.5f;
    [Tooltip("ระยะต่อย (เมตร)")]
    public float punchRange = 3f;
    [Tooltip("รัศมี SphereCast (ยิ่งเยอะยิ่งตีง่าย)")]
    public float punchRadius = 0.5f;
    [Tooltip("ดาเมจต่อหมัด")]
    public int damage = 30;
    [Tooltip("คูลดาวน์ระหว่างหมัด (วินาที)")]
    public float punchCooldown = 0.3f;

    [Header("=== Camera Shake ===")]
    [Tooltip("ความแรงการสั่นของหน้าจอตอนต่อย")]
    public float punchShakeMagnitude = 0.15f;
    [Tooltip("ระยะเวลาการสั่นของหน้าจอตอนต่อย")]
    public float punchShakeDuration = 0.1f;

    [Header("=== Hitstop Settings ===")]
    [Tooltip("Time Scale ตอน Hitstop (0.05 = เกือบหยุด)")]
    public float hitstopTimeScale = 0.05f;
    [Tooltip("ระยะเวลา Hitstop (วินาทีจริง)")]
    public float hitstopDuration = 0.2f;

    [Header("=== Parry Settings ===")]
    [Tooltip("ความเร็วกระสุนที่เด้งกลับ")]
    public float reflectSpeed = 40f;
    [Tooltip("ระยะเวลาที่เกมจะหยุดสนิ่งตอน Parry (วินาทีจริง)")]
    public float parryFreezeDuration = 0.15f;
    [Tooltip("Time Scale ตอน Parry (0.01 = ช้ามากเท่ๆ)")]
    public float parryHitstopTimeScale = 0.05f;
    [Tooltip("ระยะเวลา Slow ตอน Parry (วินาทีจริง)")]
    public float parryHitstopDuration = 0.3f;

    [Header("=== Blood Cost ===")]
    [Tooltip("เสีย HP ต่อหมัด (0 = ฟรี)")]
    public int hpCostPerPunch = 0;

    [Header("=== VFX ===")]
    [Tooltip("เอฟเฟกต์ตอนต่อยโดน")]
    public GameObject punchHitVFX;
    [Tooltip("เอฟเฟกต์ตอน Parry")]
    public GameObject parryVFX;

    [Header("=== Audio Settings ===")]
    [Tooltip("เสียงตอนต่อยลม (วืด)")]
    public AudioClip punchSwingSFX;
    [Tooltip("เสียงตอนต่อยโดนศัตรู")]
    public AudioClip punchHitSFX;
    [Tooltip("เสียงตอน Parry สำเร็จ (ปัดกระสุน)")]
    public AudioClip parrySFX;

    // ──── Private ────
    private float nextPunchTime = 0f;
    private PlayerHealth playerHealth;
    private bool isHitstopActive = false;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        // ซ่อนโมเดลหมัดตอนเริ่มเกม (ถ้าตั้งค่าไว้)
        if (meleeVisualObject != null)
        {
            if (meleeVisualObject == this.gameObject)
            {
                Debug.LogWarning("[MeleeFist] ⚠️ คุณแปะสคริปต์นี้ไว้บน Visual Object โดยตรง! ระบบจะทำงานไม่ได้ถ้ามันถูกปิด รบกวนย้ายสคริปต์ไปไว้ที่ Main Camera แทนครับ!");
            }
            else
            {
                meleeVisualObject.SetActive(false);
            }
        }
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
        if (!Input.GetKeyDown(punchKey)) return;
        if (Time.unscaledTime < nextPunchTime) return;

        // เช็คค่าเลือด (ถ้ามี cost)
        if (hpCostPerPunch > 0 && playerHealth != null)
        {
            if (playerHealth.currentHealth < hpCostPerPunch) return;
            playerHealth.DrainHealth(hpCostPerPunch);
        }

        nextPunchTime = Time.unscaledTime + punchCooldown;
        PerformPunch();
    }

    // ─────────────────────────────────────────────────────────
    //  PUNCH — SphereCast จากกล้อง
    // ─────────────────────────────────────────────────────────
    private void PerformPunch()
    {
        // 1. แสดง Visual / Animation ของหมัด
        if (meleeVisualObject != null)
        {
            StopCoroutine(nameof(HideVisualRoutine));
            meleeVisualObject.SetActive(true);
            StartCoroutine(nameof(HideVisualRoutine));
        }

        if (meleeAnimator != null)
        {
            meleeAnimator.SetTrigger(punchTriggerName);
        }

        // 2. ลอจิกการทำดาเมจ
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // SphereCastAll เพื่อจับทุกอย่างในรัศมีหมัด
        RaycastHit[] hits = Physics.SphereCastAll(ray, punchRadius, punchRange);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        bool hitSomething = false;

        // เล่นเสียงต่อยลมไว้ก่อน ถ้าโดนเดี๋ยวเล่นเสียงโดนทับ
        PlaySFX(punchSwingSFX);

        // หน้าจอสั่นตอนต่อย
        PlayerMovement pm = GetComponentInParent<PlayerMovement>();
        if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null) pm.TriggerCameraShake(punchShakeMagnitude, punchShakeDuration);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Player")) continue;

            // ───── เช็ค EnemyProjectile ก่อน (Parry) ─────
            // ใช้ GetComponentInParent เผื่อสคริปต์อยู่คนละชั้นกับ Collider
            EnemyProjectile projectile = hit.collider.GetComponentInParent<EnemyProjectile>();
            if (projectile != null && !projectile.isParried)
            {
                ParryProjectile(projectile, hit.point);
                hitSomething = true;
                continue;
            }

            // ───── เช็ค Enemy (ทำดาเมจ + Hitstop) ─────
            EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
                Debug.Log($"[MeleeFist] 👊 ต่อยโดน {hit.collider.name} → {damage} DMG");
                SpawnVFX(punchHitVFX, hit.point, hit.normal);
                PlaySFX(punchHitSFX);
                TriggerHitstop(hitstopTimeScale, hitstopDuration);
                hitSomething = true;
                continue;
            }
            else
            {
                EnemyHP oldHP = hit.collider.GetComponentInParent<EnemyHP>();
                if (oldHP != null)
                {
                    oldHP.TakeDamage((float)damage);
                    Debug.Log($"[MeleeFist] 👊 ต่อยโดน {hit.collider.name} → {damage} DMG (EnemyHP)");
                    SpawnVFX(punchHitVFX, hit.point, hit.normal);
                    PlaySFX(punchHitSFX);
                    TriggerHitstop(hitstopTimeScale, hitstopDuration);
                    hitSomething = true;
                    continue;
                }
            }
        }

        if (!hitSomething)
            Debug.Log("[MeleeFist] 👊 ต่อยลม");
    }

    // ─────────────────────────────────────────────────────────
    //  PARRY — กระเด่ง Projectile กลับไป
    // ─────────────────────────────────────────────────────────
    private void ParryProjectile(EnemyProjectile projectile, Vector3 hitPoint)
    {
        StartCoroutine(ParryRoutine(projectile, hitPoint));
    }

    private IEnumerator ParryRoutine(EnemyProjectile projectile, Vector3 hitPoint)
    {
        Debug.Log($"[MeleeFist] ✨ PARRY! Freezing game...");

        // 1. หยุดกระสุนไว้ชั่วคราว
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true; // ล็อคตำแหน่งไว้
        }

        // เล่น VFX/SFX ทันทีที่กระทบ
        SpawnVFX(parryVFX, hitPoint, Vector3.up);
        PlaySFX(parrySFX);

        // หยุดเวลาทั้งเกม (TimeScale = 0)
        TriggerHitstop(0f, parryFreezeDuration);

        // รอจนกว่าเวลาหยุดจะหมด (ใช้ Realtime เพราะ TimeScale เป็น 0)
        yield return new WaitForSecondsRealtime(parryFreezeDuration);

        // 2. ดีดกลับ!
        if (projectile != null)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                Vector3 reflectDir = playerCamera.transform.forward;

                // ย้ายกระสุนไปข้างหน้าเล็กน้อยเพื่อไม่ให้ชนตัวผู้เล่นหรือกล้องทันที
                projectile.transform.position += reflectDir * 1.5f;
                
                // ตั้งความเร็ว
                rb.linearVelocity = reflectDir * reflectSpeed;
                projectile.transform.rotation = Quaternion.LookRotation(reflectDir);

                // เปลี่ยน Layer ให้เป็น Default (Layer 0) เพื่อให้แน่ใจว่ามันจะชนกับศัตรูได้
                // (ปกติกระสุนศัตรูอาจจะถูกตั้ง Layer ที่ไม่ให้ชนกับศัตรูด้วยกันเอง)
                projectile.gameObject.layer = 0; 
            }

            projectile.isParried = true;
            projectile.damage *= 2;

            Debug.Log($"[MeleeFist] 👊 PUNCHED BACK! (Layer set to Default, moved forward)");

            // แถม Hitstop แบบ Slow สั้นๆ หลังดีดออกไปเพื่อความสะใจ
            TriggerHitstop(parryHitstopTimeScale, parryHitstopDuration);
        }
    }

    // ─────────────────────────────────────────────────────────
    //  HITSTOP — ชะลอเวลา
    // ─────────────────────────────────────────────────────────
    private void TriggerHitstop(float timeScale, float duration)
    {
        if (!isHitstopActive)
            StartCoroutine(HitstopCoroutine(timeScale, duration));
    }

    private IEnumerator HitstopCoroutine(float targetTimeScale, float duration)
    {
        isHitstopActive = true;

        Time.timeScale = targetTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // รอตาม unscaledTime (เวลาจริง ไม่โดน timeScale)
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        isHitstopActive = false;
    }

    // ─────────────────────────────────────────────────────────
    //  VFX & SFX Helpers
    // ─────────────────────────────────────────────────────────
    private void SpawnVFX(GameObject prefab, Vector3 position, Vector3 normal)
    {
        if (prefab == null) return;
        GameObject fx = Instantiate(prefab, position, Quaternion.LookRotation(normal));
        Destroy(fx, 2f);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        // สร้างเสียงที่ตำแหน่งกล้อง จะได้ยินชัดเจน
        AudioSource.PlayClipAtPoint(clip, playerCamera.transform.position);
    }

    private IEnumerator HideVisualRoutine()
    {
        yield return new WaitForSeconds(visualShowDuration);
        if (meleeVisualObject != null)
            meleeVisualObject.SetActive(false);
    }
}
