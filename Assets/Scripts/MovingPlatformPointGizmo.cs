using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatformPointGizmo : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
