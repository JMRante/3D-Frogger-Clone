using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    STANDING,
    HOPPING,
    SUPERHOPPING,
    TURNING
}

public class PlayerFrog : MonoBehaviour
{
    public float hopTime = 0.1f;
    public float superHopTime = 0.2f;
    public float turnTime = 0.05f;
    private float moveTimer = 0f;

    public float hopHeight = 0.5f;

    private PlayerState state;

    private Vector3 lastPosition;
    private Vector3 nextPosition;

    private Quaternion lastRotation;
    private Quaternion nextRotation;

    private SphereCollider sphereCollider;

    void Start()
    {
        lastPosition = transform.position;
        nextPosition = transform.position;

        lastRotation = transform.rotation;
        nextRotation = transform.rotation;

        state = PlayerState.STANDING;

        sphereCollider = GetComponent<SphereCollider>();
    }

    void Update()
    {
        Vector3 hopDirection = Vector3.zero;

        // Input and Collision Checking
        if (state == PlayerState.STANDING)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (!Physics.CheckSphere(lastPosition + Vector3.forward, sphereCollider.radius))
                {
                    hopDirection = Vector3.forward;
                    state = PlayerState.HOPPING;
                }
                else if (transform.forward != Vector3.forward)
                {
                    nextRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                    moveTimer = turnTime;
                    state = PlayerState.TURNING;
                }
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (!Physics.CheckSphere(lastPosition + Vector3.right, sphereCollider.radius))
                {
                    hopDirection = Vector3.right;
                    state = PlayerState.HOPPING;
                }
                else if (transform.forward != Vector3.right)
                {
                    nextRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
                    moveTimer = turnTime;
                    state = PlayerState.TURNING;
                }
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (!Physics.CheckSphere(lastPosition + Vector3.back, sphereCollider.radius))
                {
                    hopDirection = Vector3.back;
                    state = PlayerState.HOPPING;
                }
                else if (transform.forward != Vector3.back)
                {
                    nextRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                    moveTimer = turnTime;
                    state = PlayerState.TURNING;
                }
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (!Physics.CheckSphere(lastPosition + Vector3.left, sphereCollider.radius))
                {
                    hopDirection = Vector3.left;
                    state = PlayerState.HOPPING;
                }
                else if (transform.forward != Vector3.left)
                {
                    nextRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
                    moveTimer = turnTime;
                    state = PlayerState.TURNING;
                }
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                if (!Physics.CheckSphere(lastPosition + (transform.forward), sphereCollider.radius))
                {
                    if (!Physics.CheckSphere(lastPosition + (transform.forward * 2f), sphereCollider.radius))
                    {
                        hopDirection = transform.forward * 2f;
                        state = PlayerState.SUPERHOPPING;
                    }
                    else
                    {
                        hopDirection = transform.forward;
                        state = PlayerState.HOPPING;
                    }
                }
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                if (moveTimer == 0f)
                {
                    nextRotation = transform.rotation * Quaternion.AngleAxis(-90f, Vector3.up);
                    moveTimer = turnTime;
                    state = PlayerState.TURNING;
                }
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                if (moveTimer == 0f)
                {
                    nextRotation = transform.rotation * Quaternion.AngleAxis(90f, Vector3.up);
                    moveTimer = turnTime;
                    state = PlayerState.TURNING;
                }
            }
        }

        // Prepare Movement
        if (hopDirection != Vector3.zero && moveTimer == 0f)
        {
            nextPosition = lastPosition + hopDirection;
            nextRotation = Quaternion.LookRotation(hopDirection, Vector3.up);

            switch (state)
            {
                case PlayerState.HOPPING: moveTimer = hopTime; break;
                case PlayerState.SUPERHOPPING: moveTimer = superHopTime; break;
            }
        }

        // Movement
        if (moveTimer > 0f)
        {
            moveTimer -= Time.deltaTime;

            float normalizedMoveTimer = GetNormalizedMoveTimer();
            float hopHeightCurveY = (hopHeight * 4) * (-normalizedMoveTimer * normalizedMoveTimer + normalizedMoveTimer);

            transform.position = Vector3.Slerp(lastPosition, nextPosition, normalizedMoveTimer);
            
            if (state != PlayerState.TURNING)
            {
                transform.position += (Vector3.up * hopHeightCurveY);
            }

            transform.rotation = Quaternion.Slerp(lastRotation, nextRotation, normalizedMoveTimer);
        }

        // Stop Moving
        if (moveTimer <= 0f)
        {
            moveTimer = 0f;

            transform.position = Round(nextPosition);
            lastPosition = transform.position;

            transform.rotation = nextRotation;
            lastRotation = transform.rotation;

            state = PlayerState.STANDING;
        }
    }

    private float GetNormalizedMoveTimer()
    {
        switch (state)
        {
            case PlayerState.HOPPING: return 1 - (moveTimer / hopTime);
            case PlayerState.SUPERHOPPING: return 1 - (moveTimer / superHopTime);
            case PlayerState.TURNING: return 1 - (moveTimer / turnTime);
            default: return 0;
        }
    }

    public static bool IsSnapped(Vector3 vec)
    {
        return Mathf.Approximately(vec.x % 1f, 0f) && Mathf.Approximately(vec.y % 1f, 0f) && Mathf.Approximately(vec.z % 1f, 0f);
    }

    public static Vector3 Round(Vector3 vec)
    {
        return new Vector3(Mathf.Round(vec.x), Mathf.Round(vec.y), Mathf.Round(vec.z));
    }
}
