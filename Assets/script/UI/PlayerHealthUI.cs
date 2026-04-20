using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// แถบ HP แบบ 2 ชั้น พร้อมขอบเบลอจางๆ:
/// HP-Full (สีสัน) ซ้อนทับ HP-0 (ขาว)
/// เมื่อเลือดลด HP-Full จะจางหายจากซ้ายไปขวา แบบขอบนุ่มนวลไม่มีเส้นตัดคม
/// ใช้ Shader "UI/HPBarFade" ทำการเบลอขอบ
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("Health Bar")]
    [Tooltip("ลาก RawImage 'HP-Full' (ชั้นบน ที่จะหายไป)")]
    public RawImage hpFullImage;

    [Header("Fade Settings")]
    [Tooltip("ความกว้างของขอบเบลอ (0.01=คม, 0.2=เบลอมาก)")]
    [Range(0.01f, 0.3f)]
    public float fadeWidth = 0.08f;

    [Header("Animation")]
    [Tooltip("ความเร็วในการลดแถบ (ยิ่งมากยิ่งเร็ว)")]
    public float smoothSpeed = 8f;

    private Material fadeMaterial;
    private float targetCutoff = 0f; // 0 = เลือดเต็ม, 1 = เลือดหมด
    private float currentCutoff = 0f;

    void Start()
    {
        if (hpFullImage == null) return;

        // สร้าง Material จาก Shader "UI/HPBarFade"
        Shader fadeShader = Shader.Find("UI/HPBarFade");
        if (fadeShader == null)
        {
            Debug.LogError("หา Shader 'UI/HPBarFade' ไม่เจอ! ตรวจสอบว่าไฟล์ HPBarFade.shader อยู่ในโปรเจกต์");
            return;
        }

        fadeMaterial = new Material(fadeShader);
        fadeMaterial.SetTexture("_MainTex", hpFullImage.texture);
        fadeMaterial.SetFloat("_Cutoff", 0f);
        fadeMaterial.SetFloat("_FadeWidth", fadeWidth);

        hpFullImage.material = fadeMaterial;
    }

    void Update()
    {
        if (fadeMaterial == null) return;

        // Lerp ให้ลดลงนุ่มนวล
        currentCutoff = Mathf.Lerp(currentCutoff, targetCutoff, smoothSpeed * Time.deltaTime);
        if (Mathf.Abs(currentCutoff - targetCutoff) < 0.001f)
            currentCutoff = targetCutoff;

        fadeMaterial.SetFloat("_Cutoff", currentCutoff);
        fadeMaterial.SetFloat("_FadeWidth", fadeWidth);
    }

    // รับค่าจาก PlayerHealth.OnHealthChanged
    public void UpdateHealthText(int currentHealth, int maxHealth)
    {
        if (maxHealth > 0)
            targetCutoff = 1f - ((float)currentHealth / maxHealth);
    }

    void OnDestroy()
    {
        if (fadeMaterial != null)
            Destroy(fadeMaterial);
    }
}
