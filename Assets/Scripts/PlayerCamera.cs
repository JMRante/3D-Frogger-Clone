using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public PlayerFrog player;
    public Vector3 offsetFromPlayer = new Vector3(0f, 17f, -11f);

    void Start()
    {
        transform.position = player.transform.position + offsetFromPlayer;
        transform.LookAt(player.transform.position, Vector3.up);
    }

    void Update()
    {
        Vector3 targetOffsetPosition = player.transform.position + offsetFromPlayer;

        if (transform.position != targetOffsetPosition)
        {
            Vector3 directionTowardsPlayer = targetOffsetPosition - transform.position;
            directionTowardsPlayer.Normalize();

            float verticalMoveDenom = 2f;

            switch (player.state)
            {
                case PlayerState.FALLING: verticalMoveDenom = 2f; break;
                case PlayerState.HOPPING: verticalMoveDenom = 10f; break;
                case PlayerState.SUPERHOPPING: verticalMoveDenom = 10f; break;
            }

            float distanceToTargetPosition = (targetOffsetPosition - transform.position).magnitude;
            transform.position += new Vector3(
                directionTowardsPlayer.x * (distanceToTargetPosition / 0.3f),
                directionTowardsPlayer.y * (distanceToTargetPosition / verticalMoveDenom),
                directionTowardsPlayer.z * (distanceToTargetPosition / 0.3f)) * Time.deltaTime;

            if ((targetOffsetPosition - transform.position).magnitude < 0.01f)
            {
                transform.position = targetOffsetPosition;
            }
        }
    }
}
