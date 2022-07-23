using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    STANDING,
    HOPPING,
    SUPERHOPPING,
    TURNING,
    FALLING,
    SMOOTHSNAPPING
}

public class PlayerFrog : MonoBehaviour
{
    public float hopTime = 0.1f;
    public float superHopTime = 0.2f;
    public float turnTime = 0.05f;
    private float moveTimer = 0f;

    public float hopHeight = 0.5f;
    public float superHopHeight = 0.7f;

    public float hopMaxStepHeight = 0.6f;
    // public float superHopMaxStepHeight = 0.3f;

    public float smoothSnapTime = 0.05f;

    private float gravity = -9.8f;
    private float fallVelocity = 0f;
    private float startFallVelocity = -3f;

    public PlayerState state;

    public Transform modelTransform;

    private Vector3 lastPosition;
    private Vector3 nextPosition;

    private Quaternion lastRotation;
    private Quaternion nextRotation;

    private Quaternion lastModelRotation;
    private Quaternion nextModelRotation;

    private Vector3 lastNormal;
    private Vector3 nextNormal;

    private Transform lastParent;
    private Transform nextParent;

    private SphereCollider sphereCollider;

    private bool isDead = false;

    private int solidLayer;
    private int xzHopThroughAndSolidLayer;
    private int allSolidLayers;

    void Start()
    {
        lastPosition = transform.localPosition;
        nextPosition = transform.localPosition;

        lastRotation = transform.rotation;
        nextRotation = transform.rotation;

        lastModelRotation = modelTransform.localRotation;
        nextModelRotation = modelTransform.localRotation;

        lastNormal = Vector3.up;
        nextNormal = Vector3.up;

        lastParent = null;
        nextParent = null;

        state = PlayerState.STANDING;

        sphereCollider = GetComponent<SphereCollider>();

        solidLayer = LayerMask.GetMask("Solid");
        xzHopThroughAndSolidLayer = LayerMask.GetMask("Solid", "XZHopThrough");
        allSolidLayers = LayerMask.GetMask("Solid", "XZHopThrough", "XYZHopThrough");
    }

    void Update()
    {
        Vector3 hopDirection = Vector3.zero;

        // Input and Collision Checking
        if (state == PlayerState.STANDING)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                hopDirection = CalculateHopMovement(Vector3.forward);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                hopDirection = CalculateHopMovement(Vector3.right);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                hopDirection = CalculateHopMovement(Vector3.back);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                hopDirection = CalculateHopMovement(Vector3.left);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                Vector3 tempHopDirection = transform.forward * 2f;

                tempHopDirection += CalculateHopHeightAndParent(tempHopDirection);

                if (!Physics.CheckSphere(CalculateWorldSpaceLastPosition() + tempHopDirection + (Vector3.up * 0.5f), sphereCollider.radius, solidLayer)
                 && !Physics.CheckSphere(CalculateWorldSpaceLastPosition() + (tempHopDirection / 4f) + (Vector3.up * (0.5f + GetHopHeightYAxis(0.5f))), sphereCollider.radius, solidLayer))
                {
                    if (!Physics.CheckSphere(CalculateWorldSpaceLastPosition() + (tempHopDirection / 2f) + (Vector3.up * 0.5f), sphereCollider.radius, solidLayer)
                     && !Physics.CheckSphere(CalculateWorldSpaceLastPosition() + ((3 * tempHopDirection) / 4f) + (Vector3.up * (0.5f + GetHopHeightYAxis(0.5f))), sphereCollider.radius, solidLayer))
                    {
                        hopDirection = tempHopDirection;
                        state = PlayerState.SUPERHOPPING;
                    }
                    else
                    {
                        hopDirection = CalculateHopMovement(transform.forward);
                    }
                }
                else
                {
                    hopDirection = CalculateHopMovement(transform.forward);
                }
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                if (moveTimer == 0f)
                {
                    nextRotation = transform.rotation * Quaternion.AngleAxis(-90f, Vector3.up);

                    Vector3 nextForward = Quaternion.AngleAxis(-90f, Vector3.up) * transform.forward;
                    Vector3 nextRight = Vector3.Cross(nextForward, Vector3.up);
                    Vector3 nextModelForward = Vector3.Cross(nextNormal, nextRight);
                    nextModelRotation = Quaternion.Inverse(nextRotation) * Quaternion.LookRotation(nextModelForward, lastNormal);

                    moveTimer = turnTime;
                    state = PlayerState.TURNING;
                }
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                if (moveTimer == 0f)
                {
                    nextRotation = transform.rotation * Quaternion.AngleAxis(90f, Vector3.up);

                    Vector3 nextForward = Quaternion.AngleAxis(90f, Vector3.up) * transform.forward;
                    Vector3 nextRight = Vector3.Cross(nextForward, Vector3.up);
                    Vector3 nextModelForward = Vector3.Cross(nextNormal, nextRight);
                    nextModelRotation = Quaternion.Inverse(nextRotation) * Quaternion.LookRotation(nextModelForward, lastNormal);

                    moveTimer = turnTime;
                    state = PlayerState.TURNING;
                }
            }
        }

        // Prepare Movement
        if (hopDirection != Vector3.zero && moveTimer == 0f)
        {
            nextPosition = CalculateLocalSpaceNextPositionByDirection(hopDirection);
            nextRotation = Quaternion.Euler(0f, Quaternion.LookRotation(hopDirection, Vector3.up).eulerAngles.y, 0f);

            Vector3 nextRight = Vector3.Cross(hopDirection, Vector3.up);
            Vector3 nextForward = Vector3.Cross(nextNormal, nextRight);
            nextModelRotation = Quaternion.Inverse(nextRotation) * Quaternion.LookRotation(nextForward, nextNormal);

            switch (state)
            {
                case PlayerState.HOPPING: moveTimer = hopTime; break;
                case PlayerState.SUPERHOPPING: moveTimer = superHopTime; break;
            }
        }

        // Movement
        if (moveTimer > 0f)
        {
            if (state == PlayerState.SMOOTHSNAPPING)
            {
                moveTimer -= Time.deltaTime;

                float normalizedMoveTimer = GetNormalizedMoveTimer();

                transform.position = Vector3.Lerp(CalculateWorldSpaceLastPosition(), CalculateWorldSpaceNextPosition(), normalizedMoveTimer);
            }
            else
            {
                moveTimer -= Time.deltaTime;

                float normalizedMoveTimer = GetNormalizedMoveTimer();
                float hopHeightCurveY = GetHopHeightYAxis(normalizedMoveTimer);

                transform.position = Vector3.Lerp(CalculateWorldSpaceLastPosition(), CalculateWorldSpaceNextPosition(), normalizedMoveTimer);

                if (state != PlayerState.TURNING)
                {
                    transform.position += (Vector3.up * hopHeightCurveY);
                }

                transform.rotation = Quaternion.Slerp(lastRotation, nextRotation, normalizedMoveTimer);
                modelTransform.localRotation = Quaternion.Slerp(lastModelRotation, nextModelRotation, normalizedMoveTimer);
            }
        }

        // Stop Moving
        if (moveTimer <= 0f && state != PlayerState.FALLING)
        {
            moveTimer = 0f;

            transform.position = CalculateWorldSpaceNextPosition();

            transform.rotation = nextRotation;
            lastRotation = transform.rotation;
            nextRotation = transform.rotation;

            modelTransform.localRotation = nextModelRotation;
            lastModelRotation = modelTransform.localRotation;
            nextModelRotation = modelTransform.localRotation;

            lastNormal = nextNormal;

            lastParent = nextParent;

            lastPosition = RoundXZ(nextPosition);
            nextPosition = RoundXZ(nextPosition);
            state = PlayerState.STANDING;
        }

        // Falling
        RaycastHit hit;
        bool isFloorBelow = Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hit, 0.4f, xzHopThroughAndSolidLayer);

        if (state == PlayerState.STANDING)
        {
            if (!isFloorBelow)
            {
                fallVelocity = startFallVelocity;
                state = PlayerState.FALLING;
            }
            else
            {
                if (hit.transform.gameObject.CompareTag("Moving Platform") && lastParent == null)
                {
                    Vector3 hitPosition = new Vector3(transform.position.x, hit.point.y, transform.position.z);
                    AdjustPlayerOnMovingPlatform(hit, hitPosition);
                }
            }
        }

        if (state == PlayerState.FALLING)
        {
            if (isFloorBelow && transform.position.y - hit.point.y < 0f)
            {
                Vector3 hitPosition = new Vector3(transform.position.x, hit.point.y, transform.position.z);

                if (hit.transform.gameObject.CompareTag("Moving Platform"))
                {
                    AdjustPlayerOnMovingPlatform(hit, hitPosition);
                }
                else
                {
                    transform.position = hitPosition;

                    state = PlayerState.STANDING;

                    nextPosition = RoundXZ(hitPosition);
                    lastPosition = nextPosition;
                }
            }
            else
            {
                fallVelocity += gravity * Time.deltaTime;
                transform.position += Vector3.up * fallVelocity * Time.deltaTime;
            }
        }

        // Die
        Collider[] hitColliders = Physics.OverlapSphere(sphereCollider.center + transform.position, sphereCollider.radius, allSolidLayers);
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider != sphereCollider)
            {
                Die();
            }
        }
    }

    private Vector3 CalculateHopMovement(Vector3 inputDirection)
    {
        Vector3 tempHopDirection = inputDirection;

        tempHopDirection += CalculateHopHeightAndParent(tempHopDirection);

        if (!Physics.CheckSphere(CalculateWorldSpaceLastPosition() + tempHopDirection + (Vector3.up * 0.5f), sphereCollider.radius, solidLayer)
         && !Physics.CheckSphere(CalculateWorldSpaceLastPosition() + (tempHopDirection / 2f) + (Vector3.up * (0.5f + GetHopHeightYAxis(0.5f))), sphereCollider.radius, solidLayer))
        {
            state = PlayerState.HOPPING;
            return tempHopDirection;
        }
        else if (transform.forward != inputDirection)
        {
            nextParent = lastParent;

            nextRotation = Quaternion.LookRotation(inputDirection, Vector3.up);
            moveTimer = turnTime;
            state = PlayerState.TURNING;
            return Vector3.zero;
        }

        nextParent = lastParent;

        return Vector3.zero;
    }

    private Vector3 CalculateHopHeightAndParent(Vector3 inputDirection)
    {
        float solidHeight = CalculateWorldSpaceLastPosition().y;
        RaycastHit solidHeightHit;
        bool isSolidToJumpOn = Physics.Raycast(CalculateWorldSpaceLastPosition() + inputDirection + (Vector3.up * 1.1f), Vector3.down, out solidHeightHit, 2f, xzHopThroughAndSolidLayer);
        nextParent = null;
        nextNormal = Vector3.up;

        if (isSolidToJumpOn)
        {
            solidHeight = solidHeightHit.point.y;

            if (solidHeightHit.transform.gameObject.CompareTag("Moving Platform"))
            {
                nextParent = solidHeightHit.transform;
            }

            nextNormal = solidHeightHit.normal;
        }

        if (Mathf.Abs(solidHeight - CalculateWorldSpaceLastPosition().y) <= hopMaxStepHeight)
        {
            return Vector3.up * (solidHeight - CalculateWorldSpaceLastPosition().y);
        }

        return Vector3.zero;
    }

    private Vector3 CalculateLocalSpaceNextPositionByDirection(Vector3 hopDirection)
    {
        if (lastParent != null && nextParent != null)
        {
            return RoundXZ(nextParent.InverseTransformPoint(lastParent.TransformPoint(lastPosition) + hopDirection));
        }
        else if (lastParent == null && nextParent != null)
        {
            return RoundXZ(nextParent.InverseTransformPoint(lastPosition + hopDirection));
        }
        else if (lastParent != null && nextParent == null)
        {
            return RoundXZ(lastParent.TransformPoint(lastPosition) + hopDirection);
        }
        else
        {
            return RoundXZ(lastPosition + hopDirection);
        }
    }

    public Vector3 CalculateLocalSpaceLastPosition()
    {
        if (lastParent != null)
        {
            return lastParent.InverseTransformPoint(lastPosition);
        }
        else
        {
            return lastPosition;
        }
    }

    public Vector3 CalculateLocalSpaceNextPosition()
    {
        if (nextParent != null)
        {
            return nextParent.InverseTransformPoint(nextPosition);
        }
        else
        {
            return nextPosition;
        }
    }

    public Vector3 CalculateLocalSpaceLastPosition(Vector3 position)
    {
        if (lastParent != null)
        {
            return lastParent.InverseTransformPoint(position);
        }
        else
        {
            return lastPosition;
        }
    }

    public Vector3 CalculateLocalSpaceNextPosition(Vector3 position)
    {
        if (nextParent != null)
        {
            return nextParent.InverseTransformPoint(position);
        }
        else
        {
            return nextPosition;
        }
    }

    private Vector3 CalculateWorldSpaceLastPosition()
    {
        if (lastParent != null)
        {
            return lastParent.position + lastPosition;
        }
        else
        {
            return lastPosition;
        }
    }

    private Vector3 CalculateWorldSpaceNextPosition()
    {
        if (nextParent != null)
        {
            return nextParent.position + nextPosition;
        }
        else
        {
            return nextPosition;
        }
    }

    private void AdjustPlayerOnMovingPlatform(RaycastHit hit, Vector3 hitPosition)
    {
        lastParent = hit.transform;
        nextParent = hit.transform;

        lastPosition = CalculateLocalSpaceLastPosition(hitPosition);
        nextPosition = RoundXZ(CalculateLocalSpaceNextPosition(hitPosition));

        if (lastPosition != nextPosition)
        {
            state = PlayerState.SMOOTHSNAPPING;

            moveTimer = smoothSnapTime;
        }
        else
        {
            transform.position = hitPosition;

            state = PlayerState.STANDING;

            lastPosition = RoundXZ(nextPosition);
            nextPosition = RoundXZ(nextPosition);
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
            case PlayerState.SMOOTHSNAPPING: return 1 - (moveTimer / smoothSnapTime);
            default: return 0;
        }
    }

    private void Die()
    {
        if (!isDead)
        {
            Destroy(gameObject);
            isDead = true;
        }
    }

    private float GetHopHeightYAxis(float t)
    {
        return (GetHopHeight() * 4) * (-t * t + t);
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
