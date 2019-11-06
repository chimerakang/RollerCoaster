using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float xRot, yRot, zRot;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(xRot, yRot, zRot);
    }
}
