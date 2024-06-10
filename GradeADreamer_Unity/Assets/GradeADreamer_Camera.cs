using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GradeADreamer_Camera : MonoBehaviour
{
    private Transform parent; // Parent object
    public float duration = 3f; // Duration of the rotation

    void Start()
    {
        if (parent == null)
        {
            parent = transform.parent; // If parent is not set, use the object's parent
        }
        StartCoroutine(RotateAroundYAxis());
    }

    IEnumerator RotateAroundYAxis()
    {
        Vector3 rotationAxis = Vector3.up; // Y-axis

        while (true) // Infinite loop
        {
            float angle = (360f / duration) * Time.deltaTime; // Calculate angle to rotate this frame
            transform.RotateAround(parent.position, rotationAxis, angle); // Rotate around parent's y-axis
            yield return null; // Wait for the next frame
        }
    }
}
