using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    STANDING,
    HOPPING,
    SUPERHOPPING,
    TURNINGRIGHT,
    TURNINGLEFT
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

    void Start()
    {
        lastPosition = transform.position;
        nextPosition = transform.position;

        lastRotation = transform.rotation;
        nextRotation = transform.rotation;

        state = PlayerState.STANDING;
    }

    void Update()
    {
        Vector3 hopDirection = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            hopDirection = Vector3.forward;
            state = PlayerState.HOPPING;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            hopDirection = Vector3.right;
            state = PlayerState.HOPPING;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            hopDirection = Vector3.back;
            state = PlayerState.HOPPING;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            hopDirection = Vector3.left;
            state = PlayerState.HOPPING;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            hopDirection = transform.forward * 2f;
            state = PlayerState.SUPERHOPPING;
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            if (moveTimer == 0f)
            {
                nextRotation = transform.rotation * Quaternion.AngleAxis(-90f, Vector3.up);
                moveTimer = turnTime;
                state = PlayerState.TURNINGLEFT;
            }
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            if (moveTimer == 0f)
            {
                nextRotation = transform.rotation * Quaternion.AngleAxis(90f, Vector3.up);
                moveTimer = turnTime;
                state = PlayerState.TURNINGRIGHT;
            }
        }

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

        if (moveTimer > 0f)
        {
            moveTimer -= Time.deltaTime;

            float normalizedMoveTimer = GetNormalizedMoveTimer();
            float hopHeightCurveY = (hopHeight * 4) * (-normalizedMoveTimer * normalizedMoveTimer + normalizedMoveTimer);

            transform.position = Vector3.Slerp(lastPosition, nextPosition, normalizedMoveTimer);
            
            if (state != PlayerState.TURNINGLEFT && state != PlayerState.TURNINGRIGHT)
            {
                transform.position += (Vector3.up * hopHeightCurveY);
            }

            transform.rotation = Quaternion.Slerp(lastRotation, nextRotation, normalizedMoveTimer);
        }

        if (moveTimer <= 0f)
        {
            moveTimer = 0f;

            transform.position = nextPosition;
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
            case PlayerState.TURNINGLEFT: return 1 - (moveTimer / turnTime);
            case PlayerState.TURNINGRIGHT: return 1 - (moveTimer / turnTime);
            default: return 0;
        }
    }
}
