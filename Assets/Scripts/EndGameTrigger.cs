using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndGameTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<OVRCameraRig>() != null)
        {
            FindObjectOfType<GameManager>().TriggerEndGameRoutine();
            SceneController sceneController = gameObject.GetComponent<SceneController>();
            if (sceneController != null)
            {
                sceneController.ChangeSceneFade();
            }
        }
    }
}
