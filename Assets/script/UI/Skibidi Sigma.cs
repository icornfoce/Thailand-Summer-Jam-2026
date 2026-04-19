using UnityEngine;
using UnityEngine.UI;
using TMPro; // รองรับ TextMeshPro ด้วย

public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("ความเร็วในการเปลี่ยนสี RGB")]
    public float colorSpeed = 1f;

    [Header("ความหนาของขอบ (กรณีใช้ Text ปกติ)")]
    public Vector2 outlineThickness = new Vector2(2f, -2f);

    private Outline standardOutline;
    private TextMeshProUGUI tmpText;

    void Start()
    {
        // เช็คว่าใช้อะไรอยู่ ระหว่าง TextMeshPro หรือ Text ธรรมดา
        tmpText = GetComponent<TextMeshProUGUI>();

        if (tmpText == null)
        {
            // ถ้าดึง TextMeshPro ไม่เจอ จะใช้ Outline บน Text ธรรมดาแทน
            standardOutline = GetComponent<Outline>();
            
            // หากยังไม่ได้ใส่ Component Outline มาให้ โค้ดจะแอดเข้าไปเองอัตโนมัติ
            if (standardOutline == null)
            {
                standardOutline = gameObject.AddComponent<Outline>();
            }
            // ตั้งค่าความหนาของขอบ
            standardOutline.effectDistance = outlineThickness;
        }
    }

    void Update()
    {
        // คำนวณค่าสี RGB หมุนไปเรื่อยๆ
        float hue = Mathf.PingPong(Time.time * colorSpeed, 1f);
        Color rgbColor = Color.HSVToRGB(hue, 1f, 1f);
        
        // --- เปลี่ยนเป็นขอบรอบตัวอักษร ---

        if (standardOutline != null)
        {
            // เปลี่ยนสีขอบของ Text ธรรมดา
            standardOutline.effectColor = rgbColor;
        }
        else if (tmpText != null)
        {
            // เปลี่ยนสีขอบของ TextMeshPro (Material)
            tmpText.fontMaterial.EnableKeyword("OUTLINE_ON");
            tmpText.fontMaterial.SetColor("_OutlineColor", rgbColor);
            tmpText.fontMaterial.SetFloat("_OutlineWidth", 0.2f); // ตั้งค่าความหนาขอบเบื้องต้นของ TMP
        }
    }
}
