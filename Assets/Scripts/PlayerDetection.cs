using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDetection : MonoBehaviour
{
    public float requiredDetectionTime = .5f;
    public GameObject focusedObject = null;
    public Transform rayTransform;
    public LineRenderer rayLine;

    private DetectionRing detectionRing;
    private float startTime;
    private bool tracking;
    private float focusTimeCounter;
    private bool focusedObjectCollected;

    private void Start()
    {
        startTime = Time.time;
        detectionRing = GetComponent<DetectionRing>();
    }

    private void Update()
    {
        if (rayTransform != null)
        {
            transform.position = rayTransform.position;
            transform.forward = rayTransform.forward;
            ///Debug.DrawRay(transform.position, transform.forward * 100, Color.red, 1);
            showLaser();
            CastRay();
        }
    }

    private void CastRay()
    {
        RaycastHit hit;

        focusedObject = null;

        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity))
        {
            focusedObject = hit.collider.gameObject;

            if (hit.collider.tag == "Coin" /*&& !tracking*/)
            {
                StartCoroutine(FocusTracker2(hit.collider.gameObject));
                return;
            }
        }
    }

    private IEnumerator FocusTracker2(GameObject objectToTrack)
    {
        if (objectToTrack.GetComponent<I_Interactable>() != null)
        {
            objectToTrack.GetComponent<I_Interactable>().OnInteract();
            yield return true;
        }
        else
        {
            yield return null;
        }
    }

    private IEnumerator FocusTracker(GameObject objectToTrack)
    {
        detectionRing.ToggleRingEnabled();
        tracking = true;
        while (focusedObject == objectToTrack && !focusedObjectCollected)
        {
            focusTimeCounter += Time.deltaTime;

            detectionRing.UpdateRingFill(focusTimeCounter / requiredDetectionTime);

            if (focusTimeCounter > requiredDetectionTime /* && !focusedObjectCollected*/)
            {
                if (objectToTrack.GetComponent<I_Interactable>() != null)
                    objectToTrack.GetComponent<I_Interactable>().OnInteract();

                focusedObjectCollected = true;
            }
            yield return null;
        }
        detectionRing.UpdateRingFill(0);
        detectionRing.ToggleRingEnabled();
        focusedObjectCollected = false;
        focusTimeCounter = 0;
        tracking = false;
    }

    private float GetTimeSinceGameStart()
    {
        return Time.time - startTime;
    }

    private void showLaser()
    {
        if (rayLine != null)
        {
            rayLine.enabled = true;
            rayLine.SetPosition(0, transform.position);
            rayLine.SetPosition(1, transform.position + transform.forward * 100.0f);
        }
    }
}
