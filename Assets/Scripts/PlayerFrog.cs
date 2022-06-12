using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    STANDING,
    HOPPING,
    SUPERHOPPING,
    TURNING,
    FALLING
}

public class PlayerFrog : MonoBehaviour
{
    public float hopTime = 0.1f;
    public float superHopTime = 0.2f;
    public float turnTime = 0.05f;
    private float moveTimer = 0f;

    public float hopHeight = 0.5f;
    public float superHopHeight = 0.7f;

    private float gravity = -9.8f;
    private float fallVelocity = 0f;
    private float startFallVelocity = -3f;

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
                if (!Physics.CheckSphere(lastPosition + Vector3.forward + (Vector3.up * 0.5f), sphereCollider.radius))
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
                if (!Physics.CheckSphere(lastPosition + Vector3.right + (Vector3.up * 0.5f), sphereCollider.radius))
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
                if (!Physics.CheckSphere(lastPosition + Vector3.back + (Vector3.up * 0.5f), sphereCollider.radius))
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
                if (!Physics.CheckSphere(lastPosition + Vector3.left + (Vector3.up * 0.5f), sphereCollider.radius))
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
                if (!Physics.CheckSphere(lastPosition + (transform.forward) + (Vector3.up * 0.5f), sphereCollider.radius))
                {
                    if (!Physics.CheckSphere(lastPosition + (transform.forward * 2f) + (Vector3.up * 0.5f), sphereCollider.radius))
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
            float hopHeightCurveY = (GetHopHeight() * 4) * (-normalizedMoveTimer * normalizedMoveTimer + normalizedMoveTimer);

            transform.position = Vector3.Slerp(lastPosition, nextPosition, normalizedMoveTimer);
            
            if (state != PlayerState.TURNING)
            {
                transform.position += (Vector3.up * hopHeightCurveY);
            }

            transform.rotation = Quaternion.Slerp(lastRotation, nextRotation, normalizedMoveTimer);
        }

        // Stop Moving
        if (moveTimer <= 0f && state != PlayerState.FALLING)
        {
            moveTimer = 0f;

            transform.position = RoundXZ(nextPosition);
            lastPosition = transform.position;

            transform.rotation = nextRotation;
            lastRotation = transform.rotation;

            state = PlayerState.STANDING;
        }

        // Falling
        RaycastHit hit;
        bool isFloorBelow = Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hit, 0.4f);

        if (state == PlayerState.STANDING && !isFloorBelow)
        {
            fallVelocity = startFallVelocity;
            state = PlayerState.FALLING;
        }

        if (state == PlayerState.FALLING)
        {
            if (isFloorBelow && transform.position.y - hit.point.y < 0f)
            {
                // transform.position = new Vector3(transform.position.x, 0.5f - (transform.position.y - hit.point.y), transform.position.z);
                // Debug.Log(transform.position.y + "," + hit.point.y + "," + (0.5f - (transform.position.y - hit.point.y)));
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);

                state = PlayerState.STANDING;

                lastPosition = transform.position;
                nextPosition = transform.position;
            }
            else
            {
                fallVelocity += gravity * Time.deltaTime;
                transform.position += Vector3.up * fallVelocity * Time.deltaTime;
            }
        }
    }

    private float GetHopHeight()
    {
        switch (state)
        {
            case PlayerState.HOPPING: return hopHeight;
            case PlayerState.SUPERHOPPING: return superHopHeight;
            default: return hopHeight;
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

    public static Vector3 RoundXZ(Vector3 vec)
    {
        return new Vector3(Mathf.Round(vec.x), vec.y, Mathf.Round(vec.z));
    }
}
