using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lifespan : MonoBehaviour
{
    public float lifespan = 30f;

    void Update()
    {
        lifespan -= Time.deltaTime;

        if (lifespan <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
