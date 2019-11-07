using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomCarColor : MonoBehaviour
{
    private void Start()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.material.color = Random.ColorHSV();
        }
    }
}
