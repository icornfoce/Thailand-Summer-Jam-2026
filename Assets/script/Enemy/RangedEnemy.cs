using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RangedEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    public float rotationSpeed = 720f;
    public float stoppingDistance = 8f; // The distance at which the enemy will stop moving towards the player
    public float retreatDistance = 5f; // The distance at which the enemy will start backing away
    
    [Header("Combat")]
    public int attackDamage = 10; // Damage the projectile will deal
    public float attackRange = 10f; // Maximum distance to shoot at the player
    public float fireRate = 1.5f; // Time in seconds between shots
    public GameObject projectilePrefab; // The sphere prefab to shoot
    public Transform firePoint; // Where the projectile spawns
    public float projectileSpeed = 10f; // Speed of the projectile
    public float predictionIntensity = 1f; // How much to lead the shot (0 = no prediction, 1 = full prediction)
    
    [Header("Audio & Visuals")]
    public AudioClip attackSfx;
    public GameObject attackVfx;
    
    [Header("Animation")]
    public Animator animator;
    public string runAnimationBool = "isRunning";
    public string attackTrigger = "Attack";
    
    private float nextFireTime = 0f;
    private Transform playerTransform;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.updateRotation = false; // We handle rotation manually

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

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        // If the player exists, track them
        if (playerTransform != null)
        {
            // Calculate distance to player (ignoring Y axis height difference)
            Vector3 flatPlayerPos = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
            Vector3 flatEnemyPos = new Vector3(transform.position.x, 0, transform.position.z);
            float distanceToPlayer = Vector3.Distance(flatEnemyPos, flatPlayerPos);

            // --- Predictive Tracking ---
            // Target the Main Camera (head height) instead of the pivot (feet height)
            Vector3 targetPosition = (Camera.main != null) ? Camera.main.transform.position : playerTransform.position;
            
            // Get player velocity to predict where they will be
            CharacterController playerCc = playerTransform.GetComponent<CharacterController>();
            if (playerCc != null && predictionIntensity > 0)
            {
                // Simple prediction: Target = CurrentPos + (Velocity * TimeToReach)
                float travelTime = distanceToPlayer / projectileSpeed;
                targetPosition += playerCc.velocity * travelTime * predictionIntensity;
            }

            // Calculate direction to the (possibly predicted) position
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0;

            // Smoothly rotate towards the target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Move towards player if too far
            if (distanceToPlayer > stoppingDistance)
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(playerTransform.position);
                }
            }
            // Move away from player if too close
            else if (distanceToPlayer < retreatDistance)
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    // Move in the opposite direction of the player
                    Vector3 retreatPosition = transform.position - direction * retreatDistance;
                    agent.SetDestination(retreatPosition);
                }
            }
            // Stop moving if in the sweet spot between retreatDistance and stoppingDistance
            else
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
            }
            
            // Combat logic: Shoot if within range and cooldown is ready
            if (distanceToPlayer <= attackRange && Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }

            // Update Animation
            if (animator != null)
            {
                bool isMoving = agent.velocity.magnitude > 0.1f && !agent.isStopped;
                animator.SetBool(runAnimationBool, isMoving);
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

        if (attackSfx != null)
        {
            AudioSource.PlayClipAtPoint(attackSfx, transform.position);
        }

        if (attackVfx != null && firePoint != null)
        {
            Instantiate(attackVfx, firePoint.position, firePoint.rotation);
        }

        if (animator != null)
        {
            animator.SetTrigger(attackTrigger);
        }

        // Spawn the projectile at the fire point
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        
        // Try to get a Rigidbody on the projectile to move it
        Rigidbody projRb = projectile.GetComponent<Rigidbody>();
        if (projRb != null)
        {
            // Use firePoint's forward which is already aiming at the (predicted) player position
            projRb.linearVelocity = firePoint.forward * projectileSpeed;
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
