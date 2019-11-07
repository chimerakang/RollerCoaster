using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    public float moveSpeed = 1.0f;
    public Transform[] points;
    private Transform currentDestination;
    private int currentDestIndex = 0;

    private void Start()
    {
        currentDestination = points[0];    
    }

    private void Update()
    {
        float dist = Vector3.Distance(transform.position, currentDestination.transform.position);
        print(dist);

        if (dist > 10f)
        {
            transform.position = Vector3.Lerp(transform.position, currentDestination.position, moveSpeed * Time.deltaTime);
        }
        else
        {
            if (currentDestIndex + 1 > points.Length)
                currentDestIndex = 0;
            else
                currentDestIndex++;

            currentDestination = points[currentDestIndex];
            transform.eulerAngles = currentDestination.localEulerAngles;
        }
    }
}

