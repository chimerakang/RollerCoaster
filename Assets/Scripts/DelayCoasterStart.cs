using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation.Examples;

public class DelayCoasterStart : MonoBehaviour
{
    public int delay = 10;

    private void Start()
    {
        StartCoroutine(DelayStart());

    }
    public IEnumerator DelayStart()
    {
        float speed = GetComponent<PathFollower>().speed;
        GetComponent<PathFollower>().speed = 0;
        yield return new WaitForSeconds(delay);
        GetComponent<PathFollower>().speed = speed;
    }
}
