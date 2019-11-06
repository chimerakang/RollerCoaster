using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YawVR {
    [RequireComponent(typeof(Button))]
    public class SettingsButton : MonoBehaviour
    {

        [SerializeField]
        private GameObject settingsPanel;

        private Button button;

        void Start()
        {
            button = gameObject.GetComponent<Button>();

            button.onClick.AddListener(SettingsButtonPressed);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveAllListeners();
        }

        void SettingsButtonPressed()
        {
            if (!settingsPanel.activeInHierarchy)
            {
                settingsPanel.SetActive(true);
                if (YawController.Instance().State == ControllerState.Started) {
                    //YAWController.instance.StopDevice();
                    YawController.Instance().StopDevice(
                        () => { 
                    }, 
                        (error) => { 
                    });
                }
            }
            else
            {
                settingsPanel.SetActive(false);
                if (YawController.Instance().State == ControllerState.Connected) {
                    //YAWController.instance.StartDevice();
                    YawController.Instance().StartDevice(
                        () => {
                    },
                        (error) => {
                        });
                }

            }
        }

    }
}

