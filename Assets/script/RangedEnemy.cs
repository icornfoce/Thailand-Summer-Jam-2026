using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RangedEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    public float stoppingDistance = 8f; // The distance at which the enemy will stop moving towards the player
    public float retreatDistance = 5f; // The distance at which the enemy will start backing away
    
    [Header("Combat")]
    public int attackDamage = 10; // Damage the projectile will deal
    public float attackRange = 10f; // Maximum distance to shoot at the player
    public float fireRate = 1.5f; // Time in seconds between shots
    public GameObject projectilePrefab; // The sphere prefab to shoot
    public Transform firePoint; // Where the projectile spawns
    public float projectileSpeed = 10f; // Speed of the projectile
    
    private float nextFireTime = 0f;
    private Transform playerTransform;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Freeze rotation so the physics engine doesn't make the enemy tip over
        rb.freezeRotation = true;

        // Find the player object using its tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("Player not found! Make sure the player object has the 'Player' tag.");
        }
    }

    void FixedUpdate()
    {
        // If the player exists, track them
        if (playerTransform != null)
        {
            // Calculate direction to player
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            
            // Ignore the Y-axis so the enemy doesn't try to fly up or burrow into the ground
            direction.y = 0;

            // Make the enemy face the player (even when stopped)
            if (direction != Vector3.zero)
            {
                transform.forward = direction;
            }

            // Calculate distance to player (ignoring Y axis height difference)
            Vector3 flatPlayerPos = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
            Vector3 flatEnemyPos = new Vector3(transform.position.x, 0, transform.position.z);
            float distanceToPlayer = Vector3.Distance(flatEnemyPos, flatPlayerPos);

            // Move towards player if too far
            if (distanceToPlayer > stoppingDistance)
            {
                Vector3 targetPosition = transform.position + direction * speed * Time.fixedDeltaTime;
                rb.MovePosition(targetPosition);
            }
            // Move away from player if too close
            else if (distanceToPlayer < retreatDistance)
            {
                Vector3 targetPosition = transform.position - direction * speed * Time.fixedDeltaTime;
                rb.MovePosition(targetPosition);
            }
            // Stop moving if in the sweet spot between retreatDistance and stoppingDistance
            else
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); 
            }
            
            // Combat logic: Shoot if within range and cooldown is ready
            if (distanceToPlayer <= attackRange && Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("Projectile Prefab or Fire Point is not assigned on " + gameObject.name);
            return;
        }

        // Spawn the projectile at the fire point
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        
        // Try to get a Rigidbody on the projectile to move it
        Rigidbody projRb = projectile.GetComponent<Rigidbody>();
        if (projRb != null)
        {
            // Calculate a precise aim direction towards the player's center
            Vector3 aimDirection = (playerTransform.position - firePoint.position).normalized;
            
            // Apply velocity to the projectile using linearVelocity
            projRb.linearVelocity = aimDirection * projectileSpeed;
        }
        else
        {
            Debug.LogWarning("The projectile prefab is missing a Rigidbody component!");
        }

        // Pass the damage value to the projectile's script
        EnemyProjectile projScript = projectile.GetComponent<EnemyProjectile>();
        if (projScript != null)
        {
            projScript.damage = attackDamage;
        }
        else
        {
            Debug.LogWarning("The projectile prefab is missing the EnemyProjectile script!");
        }
    }
}
