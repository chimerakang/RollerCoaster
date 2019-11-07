using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class FadePanel : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Image>().DOFade(0, 4.5f);
    }
}
