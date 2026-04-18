using UnityEngine;
using UnityEngine.Events;

public class Sword : MonoBehaviour
{
    [Header("Sword Stats")]
    public int baseDamage = 15;
    public float swingCooldown = 0.4f;

    [Header("Hitbox Settings")]
    public Transform attackPoint; // จุดศูนย์กลางของการฟัน (เช่น ปลายดาบ หรือหน้ากล้อง)
    public float attackRadius = 1.5f; // ความกว้างของการฟัน
    public LayerMask enemyLayer; // กำหนดว่าเลเยอร์ไหนคือศัตรู

    [Header("SFX / VFX Events")]
    public UnityEvent OnSwing; // ใส่เสียงฟันลม
    public UnityEvent OnHitEnemy; // ใส่เสียงฟันโดนเนื้อ / เลือดสาด

    private float nextAttackTime = 0f;
    
    // ระบบนับ Combo
    private int comboStep = 0;
    private float comboResetTime = 1.5f; // ถ้าหยุดฟันเกิน 1.5 วิ คอมโบจะรีเซ็ต
    private float lastSwingTime = 0f;

    void Update()
    {
        // ถ้าระยะเวลาห่างจากการฟันครั้งสุดท้ายมากเกินไป ให้รีเซ็ตคอมโบ
        if (Time.time - lastSwingTime > comboResetTime)
        {
            comboStep = 0;
        }

        // กดเมาส์ซ้ายเพื่อฟัน
        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time >= nextAttackTime)
            {
                SwingSword();
            }
        }
    }

    void SwingSword()
    {
        // 1. จัดการเรื่องเวลาและ Cooldown
        nextAttackTime = Time.time + swingCooldown;
        lastSwingTime = Time.time;
        
        // 2. นับคอมโบ (1 -> 2 -> 3 แล้ววนกลับ)
        comboStep++;
        if (comboStep > 3) comboStep = 1; 

        Debug.Log("Swinging Sword! Combo Hit: " + comboStep);
        OnSwing?.Invoke(); // สั่งให้ Unity เล่นเสียงฟันลมจาก Inspector

        // 3. ตรวจสอบระยะฟัน (Hit Detection)
        if (attackPoint == null)
        {
            attackPoint = transform; // สำรองไว้ถ้าลืมใส่
        }

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRadius, enemyLayer);
        bool hitSomething = false;

        // 4. ทำดาเมจใส่ศัตรูทุกคนที่อยู่ในระยะฟัน
        foreach (Collider enemyCollider in hitEnemies)
        {
            EnemyHealth enemyHealth = enemyCollider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                int currentDamage = baseDamage;
                
                // ถ้านี่คือการฟันคอมโบฮิตที่ 3 ให้แรงขึ้น 50%
                if (comboStep == 3)
                {
                    currentDamage = Mathf.RoundToInt(baseDamage * 1.5f);
                }

                enemyHealth.TakeDamage(currentDamage);
                hitSomething = true;
            }
        }

        if (hitSomething)
        {
            OnHitEnemy?.Invoke(); // สั่งให้ Unity เล่นเสียงฉัวะ! หรือสเปรย์เลือด
        }
    }

    // ฟังก์ชันนี้ช่วยวาดลูกโลกสีแดงในฉาก (ตอนอยู่ในโปรแกรม Unity) ให้เราเห็นระยะฟันของดาบ
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
