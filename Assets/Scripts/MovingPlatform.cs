using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public GameObject startPoint;
    public GameObject endPoint;

    public float speed = 0.1f;

    private float timer = 0f;
    private GameObject target;

    void Start()
    {
        target = endPoint;
    }

    void Update()
    {
        if (target == endPoint)
        {
            timer += speed * Time.deltaTime;
            transform.position = Vector3.Lerp(startPoint.transform.position, endPoint.transform.position, timer);

            if (timer >= 1f)
            {
                target = startPoint;
                timer = 1f;
            }
        }
        else if (target == startPoint)
        {
            timer -= speed * Time.deltaTime;
            transform.position = Vector3.Lerp(startPoint.transform.position, endPoint.transform.position, timer);

            if (timer <= 0f)
            {
                target = endPoint;
                timer = 0f;
            }
        }
    }
}
