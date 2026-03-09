using UnityEngine;

public class Ozma : MonoBehaviour
{
    [Header("Rotation Axis")]
    public bool rotateX = false;
    public bool rotateY = true;
    public bool rotateZ = false;

    [Header("Rotation Speed")]
    public float rotationSpeed = 50f;

    void Update()
    {
        float x = rotateX ? 1f : 0f;
        float y = rotateY ? 1f : 0f;
        float z = rotateZ ? 1f : 0f;

        Vector3 rotationAxis = new Vector3(x, y, z);

        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}