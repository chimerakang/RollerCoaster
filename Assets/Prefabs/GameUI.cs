using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField]
    private GameObject uiHelpersToInstantiate;
    [SerializeField]
    private Transform[] targetContentPanels;

    public LaserPointer.LaserBeamBehavior laserBeamBehavior;
    LaserPointer lp;
    LineRenderer lr;
    OVRCameraRig rig;

    [SerializeField]
    private List<GameObject> toEnable;
    [SerializeField]
    private List<GameObject> toDisable;
    private Vector3 menuOffset;
    private bool[] reEnable;


    public void Awake()
    {
        menuOffset = transform.position; // TODO: this is unpredictable/busted
        rig = FindObjectOfType<OVRCameraRig>();

        if (uiHelpersToInstantiate)
        {
            GameObject.Instantiate(uiHelpersToInstantiate);
        }

        lp = FindObjectOfType<LaserPointer>();
        if (!lp)
        {
            Debug.LogError("Debug UI requires use of a LaserPointer and will not function without it. Add one to your scene, or assign the UIHelpers prefab to the DebugUIBuilder in the inspector.");
            return;
        }
        lp.laserBeamBehavior = laserBeamBehavior;

        if (!toEnable.Contains(lp.gameObject))
        {
            toEnable.Add(lp.gameObject);
        }
        GetComponent<OVRRaycaster>().pointer = lp.gameObject;
        lp.gameObject.SetActive(false);

    }
    // Start is called before the first frame update
    void Start()
    {
        Show();
        ToggleLaserPointer(true);
    }

    public void Show()
    {
        ///Relayout();
        gameObject.SetActive(true);
        transform.position = rig.transform.TransformPoint(menuOffset);
        Vector3 newEulerRot = rig.transform.rotation.eulerAngles;
        newEulerRot.x = 0.0f;
        newEulerRot.z = 0.0f;
        transform.eulerAngles = newEulerRot;

        if (reEnable == null || reEnable.Length < toDisable.Count) reEnable = new bool[toDisable.Count];
        reEnable.Initialize();
        int len = toDisable.Count;
        for (int i = 0; i < len; ++i)
        {
            if (toDisable[i])
            {
                reEnable[i] = toDisable[i].activeSelf;
                toDisable[i].SetActive(false);
            }
        }
        len = toEnable.Count;
        for (int i = 0; i < len; ++i)
        {
            toEnable[i].SetActive(true);
        }

        int numPanels = targetContentPanels.Length;
        for (int i = 0; i < numPanels; ++i)
        {
            ///targetContentPanels[i].gameObject.SetActive(insertedElements[i].Count > 0);
        }
    }

    public void ToggleLaserPointer(bool isOn)
    {
        if (lp)
        {
            if (isOn)
            {
                lp.enabled = true;
            }
            else
            {
                lp.enabled = false;
            }
        }
    }

}
