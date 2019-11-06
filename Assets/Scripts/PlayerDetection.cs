using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDetection : MonoBehaviour
{
    public float requiredDetectionTime = 1.5f;
    public GameObject focusedObject = null;

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
        Debug.DrawRay(transform.position, transform.forward * 100, Color.red, 1);
        CastRay(); 
    }

    private void CastRay()
    {
        RaycastHit hit;

        focusedObject = null;

        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity))
        {
            focusedObject = hit.collider.gameObject;

            if(hit.collider.tag == "Coin" && !tracking)
            {
                StartCoroutine(FocusTracker(hit.collider.gameObject));
                return;
            }
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

            if (focusTimeCounter > requiredDetectionTime && !focusedObjectCollected)
            {
                if(objectToTrack.GetComponent<I_Interactable>() != null)
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
}
