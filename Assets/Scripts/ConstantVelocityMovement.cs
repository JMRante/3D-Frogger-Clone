using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantVelocityMovement : MonoBehaviour
{
    public Vector3 velocity = Vector3.forward;

    void Update()
    {
        transform.position += velocity * Time.deltaTime;
    }
}
