using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DetectionRing : MonoBehaviour
{
    public Image detectionImage;

    public void ToggleRingEnabled()
    {
        detectionImage.enabled = !detectionImage.enabled;
    }

    public void UpdateRingFill(float fillAmount)
    {
        detectionImage.fillAmount = fillAmount;
    }
}
