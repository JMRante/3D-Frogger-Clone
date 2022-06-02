using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFrog : MonoBehaviour
{
    public float hopTime = 0.1f;
    private float hopTimer = 0f;

    private Vector3 lastPosition;
    private Vector3 nextPosition;

    private Quaternion lastRotation;
    private Quaternion nextRotation;

    void Start()
    {
        lastPosition = transform.position;
        nextPosition = transform.position;

        lastRotation = transform.rotation;
        nextRotation = transform.rotation;
    }

    void Update()
    {
        Vector3 hopDirection = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            hopDirection = Vector3.forward;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            hopDirection = Vector3.right;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            hopDirection = Vector3.back;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            hopDirection = Vector3.left;
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            hopDirection = transform.forward * 2f;
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            if (hopTimer == 0f)
            {
                nextRotation = transform.rotation * Quaternion.AngleAxis(-90f, Vector3.up);
                hopTimer = hopTime;
            }
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            if (hopTimer == 0f)
            {
                nextRotation = transform.rotation * Quaternion.AngleAxis(90f, Vector3.up);
                hopTimer = hopTime;
            }
        }

        if (hopDirection != Vector3.zero && hopTimer == 0f)
        {
            nextPosition = lastPosition + hopDirection;
            nextRotation = Quaternion.LookRotation(hopDirection, Vector3.up);
            hopTimer = (lastPosition - nextPosition).magnitude * hopTime;
        }

        if (hopTimer > 0f)
        {
            hopTimer -= Time.deltaTime;

            float normalizedHopTimer = 1 - (hopTimer / hopTime);
            transform.position = Vector3.Slerp(lastPosition, nextPosition, normalizedHopTimer);
            transform.rotation = Quaternion.Slerp(lastRotation, nextRotation, normalizedHopTimer);
        }

        if (hopTimer <= 0f)
        {
            hopTimer = 0f;

            transform.position = nextPosition;
            lastPosition = transform.position;

            transform.rotation = nextRotation;
            lastRotation = transform.rotation;
        }
    }
}
