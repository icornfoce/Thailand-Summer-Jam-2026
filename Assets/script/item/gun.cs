using UnityEngine;
using System.Collections;

public class gun : MonoBehaviour
{
    public enum FireType { Hitscan, Projectile }
    public enum FireMode { SemiAuto, Auto, Burst }

    [Header("Gun Setup")]
    public FireType fireType = FireType.Hitscan;
    public FireMode fireMode = FireMode.Auto;
    
    [Tooltip("จุดที่ลูกปืนหรือประกายไฟจะโผล่ออกมา (ต้องมีถ้าใช้ปืนยิงจรวดหรือสเปรย์)")]
    public Transform firePoint;     
    
    [Tooltip("กล้องของผู้เล่น (ใช้สำหรับเล็งเป้าตรงกลางจอ, ถ้าไม่มีจะใช้ทิศทางของ FirePoint แทน)")]
    public Camera playerCamera;     

    [Header("Gun Stats")]
    public int damage = 10;
    [Tooltip("ระยะห่างระหว่างนัด (หน่วยเป็นวินาที, เช่น 0.1 คือรัวมาก)")]
    public float fireRate = 0.1f; 
    [Tooltip("ระยะยิงถึงสูงสุด (สำหรับ Hitscan)")]
    public float range = 100f;
    [Tooltip("เวลาที่ใช้ในการรีโหลด (วินาที)")]
    public float reloadTime = 1.5f;

    [Header("Ammo Settings")]
    public int magazineSize = 30;
    public int currentAmmo;

    [Header("Spread & Burst/Shotgun")]
    [Tooltip("จำนวนกระสุนที่ออกไปต่อการกดยิง 1 ครั้ง (ใส่น้อยกว่า 2 = ปืนปกติ, ใส่ 8+ = ลูกซอง)")]
    public int bulletsPerTap = 1;
    [Tooltip("ความกว้างของการกระจายกระสุนเป้าบาน (ยิ่งเยอะยิ่งไม่แม่น)")]
    public float spread = 0f;       
    [Tooltip("ระยะเวลาหน่วงระหว่างนัดในกรณีที่ตั้งเป็น Burst Fire เท่านั้น")]
    public float timeBetweenBurstShots = 0.05f;

    [Header("Projectile Setup (If chosen Projectile)")]
    public GameObject projectilePrefab;
    [Tooltip("แรงพุ่งของกระสุน")]
    public float projectileForce = 50f;

    [Header("Effects & Audio")]
    public GameObject muzzleFlash;
    public GameObject hitEffect; // สำหรับรอยกระสุนหรือสะเก็ดไฟกระทบกำแพง
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    
    // Internal States
    private bool isShooting;
    private bool readyToShoot = true;
    private bool isReloading = false;
    private int bulletsShotInBurst;

    void Start()
    {
        currentAmmo = magazineSize;
        readyToShoot = true;

        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // 1. เช็คปุ่มยิง
        if (fireMode == FireMode.Auto)
        {
            isShooting = Input.GetMouseButton(0); // คลิกค้าง
        }
        else
        {
            isShooting = Input.GetMouseButtonDown(0); // คลิกทีละครั้ง
        }

        // 2. เช็คปุ่มรีโหลด
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < magazineSize && !isReloading) // หรือปุ่มอื่นที่สะดวก
        {
            StartCoroutine(Reload());
        }

        // 3. จัดการตอนกดยิงแต่กระสุนหมด
        if (readyToShoot && isShooting && !isReloading && currentAmmo <= 0)
        {
            if (Input.GetMouseButtonDown(0)) // กดกริ๊กๆ ฟังเสียง
            {
                if (emptySound != null && audioSource != null) 
                    audioSource.PlayOneShot(emptySound);
            }
            return; // ป้องกันการยิงข้างล่าง
        }

        // 4. สั่งยิง (ผ่านเงื่อนไขครบ)
        if (readyToShoot && isShooting && !isReloading && currentAmmo > 0)
        {
            if (fireMode == FireMode.Burst)
            {
                StartCoroutine(ShootBurst());
            }
            else
            {
                Shoot();
            }
        }
    }

    private void Shoot()
    {
        readyToShoot = false;

        // วนลูปยิงตามจำนวน bulletsPerTap (ทำลูกซอง หรือยิงพร้อมกันหลายนัด)
        for (int i = 0; i < bulletsPerTap; i++)
        {
            // คำนวณทิศทางการยิงพร้อมเป้าบาน (Spread)
            Transform aimOrigin = (playerCamera != null) ? playerCamera.transform : (firePoint != null ? firePoint : transform);
            
            float xSpread = Random.Range(-spread, spread);
            float ySpread = Random.Range(-spread, spread);
            
            // ใส่การกระจายเข้าไป (เบี่ยงขวา-ขึ้นลง)
            Vector3 finalDirection = aimOrigin.forward;
            if (spread > 0)
            {
                finalDirection += aimOrigin.right * xSpread + aimOrigin.up * ySpread;
                finalDirection.Normalize();
            }

            if (fireType == FireType.Hitscan)
            {
                ExecuteHitscan(aimOrigin.position, finalDirection);
            }
            else if (fireType == FireType.Projectile)
            {
                ExecuteProjectile(finalDirection);
            }
        }

        // หักกระสุนทิ้ง (ยิงลูกซอง 8 เม็ด ກ็นับว่าลด 1 นัด)
        currentAmmo--;

        PlayShootEffects();

        // หน่วงเวลาสำหรับนัดถัดไป
        Invoke(nameof(ResetShot), fireRate);
    }

    private IEnumerator ShootBurst()
    {
        readyToShoot = false;
        
        // โหมด Burst ยิงรัวเป็นชุด (เช่น ชุดละ 3 นัด โดยใชั bulletsPerTap กำหนด)
        bulletsShotInBurst = bulletsPerTap;

        while (bulletsShotInBurst > 0 && currentAmmo > 0)
        {
            Transform aimOrigin = (playerCamera != null) ? playerCamera.transform : (firePoint != null ? firePoint : transform);
            
            float xSpread = Random.Range(-spread, spread);
            float ySpread = Random.Range(-spread, spread);
            Vector3 finalDirection = aimOrigin.forward + (aimOrigin.right * xSpread) + (aimOrigin.up * ySpread);
            finalDirection.Normalize();

            if (fireType == FireType.Hitscan)
            {
                ExecuteHitscan(aimOrigin.position, finalDirection);
            }
            else if (fireType == FireType.Projectile)
            {
                ExecuteProjectile(finalDirection);
            }

            currentAmmo--;
            bulletsShotInBurst--;
            PlayShootEffects();

            if (bulletsShotInBurst > 0)
            {
                yield return new WaitForSeconds(timeBetweenBurstShots);
            }
        }

        // หน่วงเวลาสำหรับพักจากการกด Burst ชุดล่าสุด
        Invoke(nameof(ResetShot), fireRate);
    }

    private void ExecuteHitscan(Vector3 origin, Vector3 direction)
    {
        // ยิงเลเซอร์ไร้รอยต่อทะลุอากาศไปเช็คเป้าหมาย
        if (Physics.Raycast(origin, direction, out RaycastHit hit, range))
        {
            // เช็คว่าชนศัตรูไหมโดยเรียกใช้คลาส EnemyHealth เหมือนตอนปะทะด้วยดาบ/เป้า
            EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
            else 
            {
                EnemyHP oldEnemyHP = hit.collider.GetComponentInParent<EnemyHP>();
                if (oldEnemyHP != null)
                {
                    oldEnemyHP.TakeDamage(damage);
                }
            }

            // เสก Hit Effect 
            if (hitEffect != null)
            {
                GameObject impactGo = Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impactGo, 2f); // เก็บกวาดกันหนักเครื่อง
            }
        }
    }

    private void ExecuteProjectile(Vector3 direction)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("⚠️ ยิง Projectile ไม่ออก เพราะคุณยังไม่ได้ลากลูกกระสุนมาใส่ช่อง 'Projectile Prefab' ในตั้งค่าปืน!");
            return;
        }
        if (firePoint == null)
        {
            Debug.LogWarning("⚠️ ยิง Projectile ไม่ออก เพราะคุณยังไม่ได้ใส่ 'Fire Point' ตรงปลายปืน!");
            return;
        }

        // เสกกระสุน
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        
        // ส่งตัวเลขดาเมจไปให้ตัวกระสุนทำงานอย่างแม่นยำ
        PlayerProjectile projScript = projectile.GetComponent<PlayerProjectile>();
        if (projScript != null)
        {
            projScript.damage = damage;
        }

        // ทำให้พุ่งไปข้างหน้าโดยเข้าถึงและอัปเดตแรงฟิสิกส์
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * projectileForce;
        }
        else
        {
            Debug.LogWarning($"⚠️ กระสุนของคุณ ({projectilePrefab.name}) ไม่มี Component 'Rigidbody' ติดอยู่ มันจึงพุ่งออกไปไม่ได้ครับ!");
        }
    }

    private void PlayShootEffects()
    {
        if (shootSound != null && audioSource != null)
        {
             audioSource.PlayOneShot(shootSound); // ใช้ PlayOneShot เสียงจะไม่โดนขัดกันเวลายิงรัวๆ
        }

        if (muzzleFlash != null && firePoint != null)
        {
            GameObject flashGo = Instantiate(muzzleFlash, firePoint.position, firePoint.rotation);
            flashGo.transform.SetParent(firePoint); 
            Destroy(flashGo, 0.5f);
        }
    }

    private void ResetShot()
    {
        readyToShoot = true;
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        
        if (reloadSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }

        // จำลองเวลาบรรจุกระสุน
        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        isReloading = false;
    }
}
