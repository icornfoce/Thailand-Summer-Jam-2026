using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerProjectile : MonoBehaviour
{
    [HideInInspector]
    public int damage = 10; // รับค่าดาเมจมาจากสคริปต์ปืน
    
    [Header("Projectile Settings")]
    public float lifetime = 5f; // อายุของกระสุนก่อนจะหายไปเอง
    public GameObject hitEffect; // เอฟเฟกต์ตอนกระสุนกระทบเป้าหมาย (เช่นรอยระเบิด)

    void Start()
    {
        // ใส่เวลาทำลายกระสุนเผื่อยิงขึ้นฟ้าหรือหลุดแมพ ไม่ให้กินสเปคคอม
        Destroy(gameObject, lifetime);
    }

    // กรณีที่ตั้งกระสุนเป็นแบบชนเต็มใบ (Physics Collider)
    void OnCollisionEnter(Collision collision)
    {
        // เช็คว่าไม่ได้ยิงโดนตัวเอง (ถ้า Collider ซ้อนกัน)
        if (collision.gameObject.CompareTag("Player")) return;

        HandleHit(collision.gameObject, collision.contacts[0].point, collision.contacts[0].normal);
    }

    // กรณีที่ตั้งกระสุนเป็นแบบเดินผ่าน (Is Trigger)
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;

        HandleHit(other.gameObject, transform.position, -transform.forward);
    }

    private void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
    {
        // เช็คว่ายิงโดนศัตรูไหม แล้วทำดาเมจ
        EnemyHealth enemyHealth = hitObject.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }
        else
        {
            EnemyHP oldEnemyHP = hitObject.GetComponentInParent<EnemyHP>();
            if (oldEnemyHP != null)
            {
                oldEnemyHP.TakeDamage(damage);
            }
        }

        // เล่นเอฟเฟกต์กระสุนทะลวง (ถ้าตั้งค่าไว้)
        if (hitEffect != null)
        {
            GameObject fx = Instantiate(hitEffect, hitPoint, Quaternion.LookRotation(hitNormal));
            Destroy(fx, 2f); // ลบเอฟเฟกต์ทิ้งหลังผ่านไป 2 วิ
        }

        // ทำลายกระสุนนัดนี้ทันทีหลังชน
        Destroy(gameObject);
    }
}
