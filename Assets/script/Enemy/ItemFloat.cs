using UnityEngine;

public class ItemFloat : MonoBehaviour
{
    [Header("Floating Settings")]
    public float floatAmplitude = 0.25f; // How high/low it floats from the center
    public float floatSpeed = 2f;        // How fast it floats

    private Vector3 startPos;

    void Start()
    {
        // Remember the starting position when the item is spawned
        startPos = transform.position;
    }

    void Update()
    {
        // Float up and down using Sine wave based on time
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
