using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    STANDING,
    PREPPING,
    SUPERPREPPING,
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
    private float moveTimer = 0f;

    public float turnTime = 0.05f;
    private float turnTimer = 0f;
    private bool turnLock = false;

    public float hopHeight = 0.5f;
    public float superHopHeight = 0.7f;

    public float hopMaxStepHeight = 0.6f;
    // public float superHopMaxStepHeight = 0.3f;

    public float smoothSnapTime = 0.05f;

    private float gravity = -9.8f;
    private float fallVelocity = 0f;
    private float startFallVelocity = -3f;

    public Vector3 prepScaleDistort = new Vector3(1.1f, 0.8f, 1.1f);
    public Vector3 superPrepScaleDistort = new Vector3(1.2f, 0.6f, 1.2f);
    public float prepTime = 0.2f;
    public float superPrepTime = 0.3f;
    private float prepTimer = 0f;

    private Vector3 lastPrepDistort;
    private Quaternion lastPrepRotation;
    private Quaternion lastPrepModelRotation;

    private Vector3 lastInputDirection;
    private Vector3 lastHopDirection;

    public Vector3 hopScaleDistort = new Vector3(0.7f, 1.5f, 0.7f);

    public PlayerState state;

    public Transform modelTransform;

    private Vector3 lastPosition;
    private Vector3 nextPosition;

    private Quaternion lastRotation;
    private Quaternion nextRotation;

    private Quaternion lastModelRotation;
    private Quaternion preNextModelRotation;
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

        lastPrepDistort = Vector3.one;

        lastPrepRotation = transform.rotation;
        lastPrepModelRotation = modelTransform.localRotation;

        lastInputDirection = Vector3.zero;
        lastHopDirection = Vector3.zero;

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
            if (Input.GetKey(KeyCode.UpArrow))
            {
                hopDirection = CalculateHopMovement(Vector3.forward);
                lastInputDirection = Vector3.forward;
                turnTimer = turnTime;
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                hopDirection = CalculateHopMovement(Vector3.right);
                lastInputDirection = Vector3.right;
                turnTimer = turnTime;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                hopDirection = CalculateHopMovement(Vector3.back);
                lastInputDirection = Vector3.back;
                turnTimer = turnTime;
            }
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                hopDirection = CalculateHopMovement(Vector3.left);
                lastInputDirection = Vector3.left;
                turnTimer = turnTime;
            }
            else if (Input.GetKey(KeyCode.E))
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
                        state = PlayerState.SUPERPREPPING;
                        prepTimer = superPrepTime;
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

                lastInputDirection = transform.forward * 2f;
            }
            else if (Input.GetKey(KeyCode.Q) && !turnLock)
            {
                if (moveTimer == 0f)
                {
                    nextRotation = transform.rotation * Quaternion.AngleAxis(-90f, Vector3.up);

                    Vector3 nextForward = Quaternion.AngleAxis(-90f, Vector3.up) * transform.forward;
                    Vector3 nextRight = Vector3.Cross(nextForward, Vector3.up);
                    Vector3 nextModelForward = Vector3.Cross(nextNormal, nextRight);
                    nextModelRotation = Quaternion.Inverse(nextRotation) * Quaternion.LookRotation(nextModelForward, lastNormal);

                    moveTimer = turnTime;
                    turnTimer = turnTime;
                    state = PlayerState.TURNING;

                    turnLock = true;
                }
            }
            else if (Input.GetKey(KeyCode.W) && !turnLock)
            {
                if (moveTimer == 0f)
                {
                    nextRotation = transform.rotation * Quaternion.AngleAxis(90f, Vector3.up);

                    Vector3 nextForward = Quaternion.AngleAxis(90f, Vector3.up) * transform.forward;
                    Vector3 nextRight = Vector3.Cross(nextForward, Vector3.up);
                    Vector3 nextModelForward = Vector3.Cross(nextNormal, nextRight);
                    nextModelRotation = Quaternion.Inverse(nextRotation) * Quaternion.LookRotation(nextModelForward, lastNormal);

                    moveTimer = turnTime;
                    turnTimer = turnTime;
                    state = PlayerState.TURNING;

                    turnLock = true;
                }
            }
        }

        // Unlock turning when input is released
        if ((Input.GetKeyUp(KeyCode.Q) || Input.GetKeyUp(KeyCode.W)) && turnLock) {
            turnLock = false;
        }

        // Prepare Movement before release
        if (hopDirection != Vector3.zero && moveTimer == 0f)
        {
            nextPosition = CalculateLocalSpaceNextPositionByDirection(hopDirection);
            nextRotation = Quaternion.Euler(0f, Quaternion.LookRotation(hopDirection, Vector3.up).eulerAngles.y, 0f);

            Vector3 nextRight = Vector3.Cross(hopDirection, Vector3.up);
            Vector3 lastForward = Vector3.Cross(lastNormal, nextRight);
            Vector3 nextForward = Vector3.Cross(nextNormal, nextRight);

            preNextModelRotation = Quaternion.Inverse(nextRotation) * Quaternion.LookRotation(lastForward, lastNormal);
            nextModelRotation = Quaternion.Inverse(nextRotation) * Quaternion.LookRotation(nextForward, nextNormal);
        }

        // When prepping, distort player model. Release prepped player after input is let go of
        if (state == PlayerState.PREPPING)
        {
            if (lastHopDirection == Vector3.zero)
            {
                lastHopDirection = hopDirection;
            }

            if (prepTimer > 0f) 
            {
                prepTimer -= Time.deltaTime;
            } 
            else
            {
                prepTimer = 0f;
            }

            // Pre-turn
            if (turnTimer > 0f)
            {
                turnTimer -= Time.deltaTime;
            }
            else
            {
                turnTimer = 0f;
            }

            float normalizedPrepTimer = GetNormalizedPrepTimer();
            float normalizedTurnTimer = GetNormalizedTurnTimer();

            modelTransform.localScale = SmoothStepToAndBack(modelTransform.localScale, prepScaleDistort, normalizedPrepTimer);

            transform.rotation = Quaternion.Slerp(lastRotation, nextRotation, normalizedTurnTimer);
            modelTransform.localRotation = Quaternion.Slerp(lastModelRotation, preNextModelRotation, normalizedTurnTimer);

            if ((!Input.GetKey(KeyCode.UpArrow) && lastInputDirection == Vector3.forward) ||
                (!Input.GetKey(KeyCode.RightArrow) && lastInputDirection == Vector3.right) ||
                (!Input.GetKey(KeyCode.DownArrow) && lastInputDirection == Vector3.back) ||
                (!Input.GetKey(KeyCode.LeftArrow) && lastInputDirection == Vector3.left))
            {
                state = PlayerState.HOPPING;
                prepTimer = 0f;

                lastPrepDistort = modelTransform.localScale;
                lastPrepRotation = transform.rotation;
                lastPrepModelRotation = modelTransform.localRotation;
            }
            else if (!Input.GetKey(KeyCode.E))
            {
                if ((lastInputDirection == Vector3.forward * 2f) ||
                    (lastInputDirection == Vector3.right * 2f) ||
                    (lastInputDirection == Vector3.back * 2f) ||
                    (lastInputDirection == Vector3.left * 2f))
                {
                    state = PlayerState.HOPPING;
                    prepTimer = 0f;
                    
                    lastPrepDistort = modelTransform.localScale;
                    lastPrepRotation = transform.rotation;
                    lastPrepModelRotation = modelTransform.localRotation;
                }
            }
        }

        if (state == PlayerState.SUPERPREPPING)
        {
            if (lastHopDirection == Vector3.zero)
            {
                lastHopDirection = hopDirection;
            }

            if (prepTimer > 0f)
            {
                prepTimer -= Time.deltaTime;
            }
            else
            {
                prepTimer = 0f;
            }

            float normalizedPrepTimer = GetNormalizedPrepTimer();

            modelTransform.localScale = SmoothStepToAndBack(modelTransform.localScale, superPrepScaleDistort, normalizedPrepTimer);

            if (!Input.GetKey(KeyCode.E))
            {
                state = PlayerState.SUPERHOPPING;
                prepTimer = 0f;

                lastPrepDistort = modelTransform.localScale;
                lastPrepRotation = transform.rotation;
                lastPrepModelRotation = modelTransform.localRotation;
            }
        }

        // Prepare Movement after release
        if (lastHopDirection != Vector3.zero && moveTimer == 0f && (state == PlayerState.HOPPING || state == PlayerState.SUPERHOPPING))
        {
            switch (state)
            {
                case PlayerState.HOPPING: moveTimer = hopTime; break;
                case PlayerState.SUPERHOPPING: moveTimer = superHopTime; break;
            }

            lastHopDirection = Vector3.zero;
            lastInputDirection = Vector3.zero;
        }

        // Turn
        if (turnTimer > 0f)
        {
            turnTimer -= Time.deltaTime;
        }
        else
        {
            turnTimer = 0f;
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
                float normalizedTurnTimer = GetNormalizedTurnTimer();
                float hopHeightCurveY = GetHopHeightYAxis(normalizedMoveTimer);

                // transform.position = Vector3.Lerp(CalculateWorldSpaceLastPosition(), CalculateWorldSpaceNextPosition(), normalizedMoveTimer);
                transform.position = VectorSmoothStep(CalculateWorldSpaceLastPosition(), CalculateWorldSpaceNextPosition(), normalizedMoveTimer);

                if (state != PlayerState.TURNING)
                {
                    transform.position += (Vector3.up * hopHeightCurveY);
                    modelTransform.localScale = SmoothStepToTwice(lastPrepDistort, hopScaleDistort, Vector3.one, normalizedMoveTimer);
                }

                transform.rotation = Quaternion.Slerp(lastPrepRotation, nextRotation, normalizedMoveTimer);
                modelTransform.localRotation = Quaternion.Slerp(lastPrepModelRotation, nextModelRotation, normalizedMoveTimer);
            }
        }

        // Stop Moving
        if (moveTimer <= 0f && (state != PlayerState.STANDING && state != PlayerState.FALLING && state != PlayerState.PREPPING && state != PlayerState.SUPERPREPPING))
        {
            moveTimer = 0f;
            turnTimer = 0f;

            transform.position = CalculateWorldSpaceNextPosition();

            transform.rotation = nextRotation;
            lastRotation = transform.rotation;
            nextRotation = transform.rotation;

            modelTransform.localRotation = nextModelRotation;
            lastModelRotation = modelTransform.localRotation;
            preNextModelRotation = modelTransform.localRotation;
            nextModelRotation = modelTransform.localRotation;

            modelTransform.localScale = Vector3.one;

            lastPrepDistort = Vector3.one;
            lastPrepRotation = nextRotation;
            lastPrepModelRotation = nextModelRotation;

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

                    AdjustPlayerUpNormal(hit);
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
            state = PlayerState.PREPPING;
            prepTimer = prepTime;
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

    private Vector3 CalculateLocalSpaceLastPosition()
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

    private Vector3 CalculateLocalSpaceNextPosition()
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

    private Vector3 CalculateLocalSpaceLastPosition(Vector3 position)
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

    private Vector3 CalculateLocalSpaceNextPosition(Vector3 position)
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

        AdjustPlayerUpNormal(hit);
    }

    private void AdjustPlayerUpNormal(RaycastHit hit)
    {
        nextNormal = hit.normal;
        lastNormal = nextNormal;

        Vector3 nextRight = Vector3.Cross(transform.forward, Vector3.up);
        Vector3 nextForward = Vector3.Cross(nextNormal, nextRight);
        nextModelRotation = Quaternion.Inverse(nextRotation) * Quaternion.LookRotation(nextForward, nextNormal);
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

    private float GetNormalizedTurnTimer()
    {
        return 1 - (turnTimer / turnTime);
    }

    private float GetNormalizedPrepTimer()
    {
        switch (state)
        {
            case PlayerState.PREPPING: return 1 - (prepTimer / prepTime);
            case PlayerState.SUPERPREPPING: return 1 - (prepTimer / superPrepTime);
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

    private float SmoothStepToAndBack(float from, float to, float t) 
    {
        if (t < 0.5f) 
        {
            return Mathf.SmoothStep(from, to, t * 2);
        } 
        else
        {
            return Mathf.SmoothStep(to, from, (t * 2) - 1);
        }
    }

    private float SmoothStepToTwice(float from, float to1, float to2, float t)
    {
        if (t < 0.5f)
        {
            return Mathf.SmoothStep(from, to1, t * 2);
        }
        else
        {
            return Mathf.SmoothStep(to1, to2, (t * 2) - 1);
        }
    }

    private Vector3 SmoothStepToAndBack(Vector3 from, Vector3 to, float t)
    {
        return new Vector3(SmoothStepToAndBack(from.x, to.x, t), SmoothStepToAndBack(from.y, to.y, t), SmoothStepToAndBack(from.z, to.z, t));
    }

    private Vector3 SmoothStepToTwice(Vector3 from, Vector3 to1, Vector3 to2, float t)
    {
        return new Vector3(SmoothStepToTwice(from.x, to1.x, to2.x, t), SmoothStepToTwice(from.y, to1.y, to2.y, t), SmoothStepToTwice(from.z, to1.z, to2.z, t));
    }


    private Vector3 VectorSmoothStep(Vector3 from, Vector3 to, float t)
    {
        return new Vector3(Mathf.SmoothStep(from.x, to.x, t), Mathf.SmoothStep(from.y, to.y, t), Mathf.SmoothStep(from.z, to.z, t));
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
