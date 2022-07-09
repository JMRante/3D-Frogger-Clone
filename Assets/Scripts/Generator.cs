using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generator : MonoBehaviour
{
    public GameObject prefab;
    public Vector3 velocity;
    public float lifespan;

    public float generationPerSec = 5f;
    public float generationTimer = 0f;

    void Start()
    {
        Generate();
        generationTimer = generationPerSec;
    }

    void Update()
    {
        generationTimer -= Time.deltaTime;

        if (generationTimer <= 0f)
        {
            Generate();
            generationTimer = generationPerSec;
        }
    }

    private void Generate()
    {
        GameObject newPrefab = Instantiate(prefab, transform.position, transform.rotation);

        ConstantVelocityMovement cvm = newPrefab.GetComponent<ConstantVelocityMovement>();
        cvm.velocity = velocity;

        Lifespan ls = newPrefab.GetComponent<Lifespan>();
        ls.lifespan = lifespan;
    }
}
