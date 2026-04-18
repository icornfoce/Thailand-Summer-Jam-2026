using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [HideInInspector]
    public int damage = 10; // This is set by the RangedEnemy script
    
    public float lifetime = 5f; // How long before the bullet destroys itself if it misses

    void Start()
    {
        // Destroy the bullet after 'lifetime' seconds to prevent them floating forever
        Destroy(gameObject, lifetime);
    }

    // This handles physical collisions (if the bullet has a non-trigger collider)
    void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    // This handles trigger overlaps (if you checked "Is Trigger" on the bullet's collider)
    void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        // Check if we hit the player
        if (hitObject.CompareTag("Player"))
        {
            Debug.Log("Bullet hit the Player for " + damage + " damage!");
            
            PlayerHealth health = hitObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }

        // Destroy the bullet as soon as it hits something
        Destroy(gameObject);
    }
}
