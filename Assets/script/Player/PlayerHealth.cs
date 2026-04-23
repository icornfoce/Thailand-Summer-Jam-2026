using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Blood Decay Settings")]
    public bool enableHpDecay = true;
    [Tooltip("ลดเลือด 1 หน่วย ทุกๆ กี่วินาที (0.5 คือลด 2 หน่วยต่อวิ)")]
    public float timePerHpDrop = 1f;
    private float decayTimer;

    [Header("UI Visual Settings (คุมแถบเลือด)")]
    public RawImage hpFullImage;
    [Tooltip("ความเร็วในการไหลลดลงของแถบเลือด (เลื่อยๆ)")]
    [Range(0.1f, 5f)]
    public float smoothHPSpeed = 1f;
    [Tooltip("ความกว้างของการจางหาย (0=คม, 1=จางมาก)")]
    [Range(0.01f, 1f)]
    public float hpFadeWidth = 0.4f;

    [Header("Gradient Colors")]
    public Color colorLeft = Color.white;
    public Color colorRight = Color.red;

    private Material fadeMaterial;
    private float targetCutoff = 0f; // 0 = เต็ม, 1 = หมด
    private float currentCutoff = 0f;

    [Header("Events")]
    public UnityEvent<int, int> OnHealthChanged; 
    public UnityEvent OnTakeDamage;
    public UnityEvent OnPlayerDeath;

    [Header("Death UI")]
    [Tooltip("ใส่ UI Screen ที่จะให้แสดงตอนตาย")]
    public GameObject deathUI;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth); 

        if (deathUI != null) deathUI.SetActive(false);

        // --- ตั้งค่า Shader (ไม่ต้องตั้ง Read/Write Enabled แล้ว) ---
        if (hpFullImage != null)
        {
            Shader fadeShader = Shader.Find("UI/HPBarFade");
            if (fadeShader != null)
            {
                // สร้าง Material ใหม่จาก Shader
                fadeMaterial = new Material(fadeShader);
                fadeMaterial.SetTexture("_MainTex", hpFullImage.texture);
                hpFullImage.material = fadeMaterial;
                
                UpdateMaterialProperties();
            }
            else
            {
                Debug.LogError("หา Shader 'UI/HPBarFade' ไม่เจอ! ตรวจสอบว่าไฟล์ HPBarFade.shader ยังอยู่ดีไหมครับ");
            }
        }
    }

    void Update()
    {
        // 1. ระบบ HP Decay
        if (enableHpDecay && currentHealth > 0)
        {
            decayTimer += Time.deltaTime;
            if (decayTimer >= timePerHpDrop)
            {
                decayTimer = 0f;
                DrainHealth(1);
            }
        }

        // 2. ระบบ UI อัปเดตแถบเลือดผ่าน Shader (เลื่อยๆ)
        if (fadeMaterial != null)
        {
            currentCutoff = Mathf.MoveTowards(currentCutoff, targetCutoff, smoothHPSpeed * Time.deltaTime);
            UpdateMaterialProperties();
        }
    }

    private void UpdateMaterialProperties()
    {
        if (fadeMaterial == null) return;

        // ส่งค่าไปยัง Shader
        fadeMaterial.SetFloat("_Cutoff", currentCutoff);
        fadeMaterial.SetFloat("_FadeWidth", hpFadeWidth);
        fadeMaterial.SetColor("_LeftColor", colorLeft);
        fadeMaterial.SetColor("_RightColor", colorRight);
    }

    public void DrainHealth(int amount)
    {
        if (currentHealth <= 0) return;
        currentHealth -= amount;
        if (currentHealth <= 0) { currentHealth = 0; Die(); }
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateTargetCutoff();
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return; 
        currentHealth -= amount;
        OnTakeDamage?.Invoke(); 
        if (currentHealth <= 0) { currentHealth = 0; Die(); }
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateTargetCutoff();
    }

    public void Heal(int amount)
    {
        if (currentHealth <= 0) return; 
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateTargetCutoff();
    }

    private void UpdateTargetCutoff()
    {
        if (maxHealth > 0)
            targetCutoff = 1f - ((float)currentHealth / maxHealth);
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        if (deathUI != null) deathUI.SetActive(true);
        OnPlayerDeath?.Invoke(); 
    }

    void OnDestroy()
    {
        if (fadeMaterial != null) Destroy(fadeMaterial);
    }
}
