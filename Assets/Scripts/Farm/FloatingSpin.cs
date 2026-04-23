using UnityEngine;

public class FloatingSpin : MonoBehaviour
{
    [Header("Float Settings")]
    [SerializeField] private float floatAmplitude = 0.5f;   // how high/low it moves
    [SerializeField] private float floatSpeed = 1f;         // speed of up/down motion
    [SerializeField] private Vector3 floatDirection = Vector3.up; // direction to float (usually up)

    [Header("Spin Settings")]
    [SerializeField] private float rotationSpeed = 30f;     // degrees per second around Y axis
    [SerializeField] private Vector3 customRotationAxis = Vector3.up; // allow spin around any axis

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        // Floating: sinusoidal motion
        float offsetY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = startPosition + floatDirection * offsetY;

        // Spinning: rotate around chosen axis
        transform.Rotate(customRotationAxis * rotationSpeed * Time.deltaTime);
    }
}