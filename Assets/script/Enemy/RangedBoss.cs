using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RangedBoss : MonoBehaviour
{
    public enum BossState { Chasing, Charging, Firing, Cooldown }

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float rotationSpeed = 360f;
    public float beamRotationSpeed = 90f; // Slower rotation during beam
    public float stoppingDistance = 10f;
    public float retreatDistance = 5f;

    [Header("Beam Attack Settings")]
    public float chargeTime = 1.5f;
    public float firingDuration = 3f;
    public float attackCooldown = 4f;
    public float beamDamagePerSecond = 20f;
    public float beamRange = 25f;
    public float beamDragSpeed = 2f; // How fast the beam follows the player (lower = more drag)
    public float previewBeamWidth = 0.05f;
    public float firingBeamWidth = 0.5f;
    public LayerMask beamMask; // Layer(s) the beam should hit (Player, Ground, etc.)

    [Header("Projectile Attack Settings (Secondary)")]
    public int attackDamage = 10;
    public float attackRange = 15f;
    public float fireRate = 1.5f;
    public float projectileSpeed = 15f;
    public GameObject projectilePrefab;
    public GameObject attackVfx;
    public AudioClip attackSfx;

    [Header("Animation")]
    public Animator animator;
    public string chargingBool = "isCharging";
    public string firingBool = "isFiring";

    [Header("Visual Effects")]
    public LineRenderer beamRenderer;
    public GameObject beamVfxPrefab;
    public GameObject chargeVfxPrefab;
    public GameObject beamImpactVfxPrefab;
    public Transform firePoint;

    [Header("Audio")]
    public AudioClip chargeSfx;
    public AudioClip beamLoopSfx;
    private AudioSource beamAudioSource;

    private NavMeshAgent agent;
    private Transform playerTransform;
    private BossState currentState = BossState.Chasing;
    private float stateTimer = 0f;
    private Vector3 currentBeamDirection;
    private GameObject currentChargeVfx;
    private GameObject currentBeamVfx;
    private GameObject currentImpactVfx;
    private float damageAccumulator = 0f;
    private float nextFireTime = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;
        agent.updateRotation = false;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        if (beamRenderer != null) beamRenderer.enabled = false;
        // Line renderer is now used internally for logic if needed, but kept invisible as requested

        if (firePoint == null) Debug.LogError("Ranged Boss: FIRE POINT is not assigned in the Inspector!");

        // Setup AudioSource for looping beam sound
        beamAudioSource = gameObject.AddComponent<AudioSource>();
        beamAudioSource.loop = true;
        beamAudioSource.playOnAwake = false;

        if (beamLoopSfx != null)
        {
            beamAudioSource.clip = beamLoopSfx;
        }

        // Default mask to everything if the user hasn't set it in the Inspector
        if (beamMask == 0)
        {
            beamMask = ~0; // Everything
            Debug.Log("Ranged Boss: Beam Mask was empty, defaulting to 'Everything'");
        }
    }

    void Update()
    {
        // Late player detection in case the player wasn't ready at Start
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            else return; // Still no player
        }

        switch (currentState)
        {
            case BossState.Chasing:
                HandleChasing();
                break;
            case BossState.Charging:
                HandleCharging();
                break;
            case BossState.Firing:
                HandleFiring();
                break;
            case BossState.Cooldown:
                HandleCooldown();
                break;
        }

        // Periodic debug log (every 2 seconds) to see what's happening
        if (Time.frameCount % 120 == 0)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            Debug.Log($"Ranged Boss Status - State: {currentState}, Distance: {dist}, BeamRange: {beamRange}");
        }

        // Secondary attack can happen during Chasing and Cooldown
        if (currentState == BossState.Chasing || currentState == BossState.Cooldown)
        {
            HandleSecondaryAttack();
        }
    }

    private void HandleSecondaryAttack()
    {
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance <= attackRange && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    private void HandleChasing()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        RotateTowards(playerTransform.position, rotationSpeed);
        HandleMovement(distance, direction);

        if (Time.time >= stateTimer)
        {
            if (distance <= beamRange)
            {
                StartCharging();
            }
        }
    }

    private void StartCharging()
    {
        Debug.Log("Ranged Boss: Start Charging");
        currentState = BossState.Charging;
        stateTimer = Time.time + chargeTime;
        if (agent.isOnNavMesh) agent.isStopped = true;

        if (animator != null) animator.SetBool(chargingBool, true);
        
        if (chargeSfx != null) AudioSource.PlayClipAtPoint(chargeSfx, transform.position);

        if (chargeVfxPrefab != null && firePoint != null)
        {
            currentChargeVfx = Instantiate(chargeVfxPrefab, firePoint.position, firePoint.rotation);
            currentChargeVfx.transform.SetParent(firePoint);
        }

        // Line renderer remains invisible
    }

    private void HandleCharging()
    {
        // Face player during charge - rotate slower
        RotateTowards(playerTransform.position, beamRotationSpeed);

        if (agent.isOnNavMesh) agent.isStopped = true;

        if (Time.time >= stateTimer)
        {
            StartFiring();
        }
    }

    private void StartFiring()
    {
        Debug.Log("Ranged Boss: Start Firing Beam");
        currentState = BossState.Firing;
        stateTimer = Time.time + firingDuration;

        if (animator != null)
        {
            animator.SetBool(chargingBool, false);
            animator.SetBool(firingBool, true);
        }

        if (currentChargeVfx != null) Destroy(currentChargeVfx);

        // Instantiate Beam VFX
        if (beamVfxPrefab != null && firePoint != null)
        {
            currentBeamVfx = Instantiate(beamVfxPrefab, firePoint.position, firePoint.rotation);
            currentBeamVfx.transform.SetParent(firePoint);
        }

        if (beamLoopSfx != null)
        {
            beamAudioSource.clip = beamLoopSfx;
            beamAudioSource.Play();
        }

        // Initialize beam direction towards player
        currentBeamDirection = (playerTransform.position - firePoint.position).normalized;
    }

    private void HandleFiring()
    {
        // Rotate boss slower towards player during firing
        RotateTowards(playerTransform.position, beamRotationSpeed);

        if (agent.isOnNavMesh) agent.isStopped = true;

        // Aim for the center of the player (assuming pivot is at feet)
        Vector3 targetDirection = ((playerTransform.position + Vector3.up) - firePoint.position).normalized;
        
        // "Drag" effect: Slowly slerp the current beam direction towards the target player direction
        currentBeamDirection = Vector3.Slerp(currentBeamDirection, targetDirection, beamDragSpeed * Time.deltaTime);
        
        if (currentBeamDirection == Vector3.zero) currentBeamDirection = targetDirection; // Safety check

        RaycastHit hit;
        Vector3 endPoint = firePoint.position + currentBeamDirection * beamRange;

        // Update Beam VFX position and rotation
        if (currentBeamVfx != null && firePoint != null)
        {
            currentBeamVfx.transform.position = firePoint.position;
            currentBeamVfx.transform.rotation = Quaternion.LookRotation(currentBeamDirection);
        }

        // Raycasting logic
        if (firePoint != null)
        {
            // Use LayerMask to avoid hitting the boss itself
            if (Physics.Raycast(firePoint.position, currentBeamDirection, out hit, beamRange, beamMask))
            {
                endPoint = hit.point;
                Debug.Log("Beam Hit: " + hit.collider.name); // Log what we hit for debugging
                Debug.DrawRay(firePoint.position, currentBeamDirection * hit.distance, Color.red);
                
                if (hit.collider.CompareTag("Player"))
                {
                    // Use GetComponentInParent in case the script is on the parent object
                    PlayerHealth health = hit.collider.GetComponentInParent<PlayerHealth>();
                    if (health != null)
                    {
                        Debug.Log("Beam hitting Player! Accumulating damage...");
                        // Accumulate damage since PlayerHealth uses integers
                        damageAccumulator += beamDamagePerSecond * Time.deltaTime;
                        if (damageAccumulator >= 1f)
                        {
                            int damageToDeal = Mathf.FloorToInt(damageAccumulator);
                            health.TakeDamage(damageToDeal);
                            damageAccumulator -= damageToDeal;
                        }
                    }
                }

                // Impact VFX
                if (beamImpactVfxPrefab != null)
                {
                    if (currentImpactVfx == null) currentImpactVfx = Instantiate(beamImpactVfxPrefab);
                    currentImpactVfx.transform.position = hit.point;
                    currentImpactVfx.transform.rotation = Quaternion.LookRotation(hit.normal);
                }
            }
            else
            {
                Debug.Log("Beam Raycast hit NOTHING (Range too short or Mask empty)");
                if (currentImpactVfx != null) Destroy(currentImpactVfx);
            }

            // Update Beam VFX scale to reach the hit point
            if (currentBeamVfx != null)
            {
                float distance = Vector3.Distance(firePoint.position, endPoint);
                // Adjust scale based on your VFX prefab's orientation (usually Z)
                Vector3 scale = currentBeamVfx.transform.localScale;
                scale.z = distance; 
                currentBeamVfx.transform.localScale = scale;
            }
        }

        if (Time.time >= stateTimer)
        {
            EndFiring();
        }
    }

    private void EndFiring()
    {
        Debug.Log("Ranged Boss: End Firing");
        currentState = BossState.Cooldown;
        stateTimer = Time.time + attackCooldown;

        if (animator != null) animator.SetBool(firingBool, false);
        if (beamRenderer != null) beamRenderer.enabled = false;
        if (currentBeamVfx != null) Destroy(currentBeamVfx);
        if (beamAudioSource.isPlaying) beamAudioSource.Stop();
        if (currentImpactVfx != null) Destroy(currentImpactVfx);
    }

    private void HandleCooldown()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        RotateTowards(playerTransform.position, rotationSpeed);
        HandleMovement(distance, direction);

        if (Time.time >= stateTimer)
        {
            currentState = BossState.Chasing;
        }
    }

    private void HandleMovement(float distance, Vector3 direction)
    {
        if (!agent.isOnNavMesh) return;

        if (distance > stoppingDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
        }
        else if (distance < retreatDistance)
        {
            agent.isStopped = false;
            Vector3 retreatPosition = transform.position - direction * retreatDistance;
            agent.SetDestination(retreatPosition);
        }
        else
        {
            agent.isStopped = true;
        }
    }

    private void RotateTowards(Vector3 targetPos, float speed)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, speed * Time.deltaTime);
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
