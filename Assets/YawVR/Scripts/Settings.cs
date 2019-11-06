using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace YawVR
{

    public class Settings : MonoBehaviour, YawControllerDelegate
    {

        //YAW device settings ui elements
        [SerializeField]
        private Text setupTitleLabel;
        [SerializeField]
        private GameObject deviceListScrollViewContent;
        [SerializeField]
        private GameObject DeviceListItemPrefab;
        [SerializeField]
        private Button connectButton;
        [SerializeField]
        private InputField ipAddressInputField;
        [SerializeField]
        private InputField udpPortInputField;
        [SerializeField]
        private InputField tcpPortInputField;
        [SerializeField]
        private Button disconnectButton;
        [SerializeField]
        private Toggle rememberDeviceToggle;
        [SerializeField]
        private Text errorText;

        //Source of motion settings ui elements
        [SerializeField]
        private Dropdown sourceOfMotionDropdown;
        [SerializeField]
        private InputField yawRotationMultiplierInputField;
        [SerializeField]
        private InputField pitchRotationMultiplierInputField;
        [SerializeField]
        private InputField rollRotationMultiplierInputField;
        [SerializeField]
        private InputField pitchAccelerationMultiplierInputField;
        [SerializeField]
        private InputField rollAccelerationMultiplierInputField;
        [SerializeField]
        private InputField lateralForceMultiplierInputField;

        //Rotation limits settings ui elements
        [SerializeField]
        private InputField yawLimitInputField;
        [SerializeField]
        private InputField pitchForwardLimitInputField;
        [SerializeField]
        private InputField pitchBackwardLimitInputField;
        [SerializeField]
        private InputField rollLimitInputField;

        //DeviceDiscovery deviceDiscovery = new DeviceDiscovery();
        private int? udpPort = 50010;
        private int? tcpPort;

        private IPAddress ipAddress;
        private YawDevice selectedDevice;
        private List<YawDevice> availableDevices = new List<YawDevice>();
        private List<GameObject> deviceButtons = new List<GameObject>();

        private bool firstEnable  = true;

        void Start()
        {
            //Setting ui elements' initial state and methods
            setupTitleLabel.text = "Set target YAW device";

            //Yaw device settings ui elements
            connectButton.onClick.AddListener(ConnectButtonPressed);
            connectButton.interactable = false;
            disconnectButton.onClick.AddListener(DisconnectButtonPressed);
            udpPortInputField.text = udpPort.ToString();
            udpPortInputField.onValueChanged.AddListener(delegate { UDPPortInputFieldTextDidChange(udpPortInputField); });
            tcpPortInputField.text = tcpPort.ToString();
            tcpPortInputField.onValueChanged.AddListener(delegate { TCPPortInputFieldTextDidChange(tcpPortInputField); });
            ipAddressInputField.onValueChanged.AddListener(delegate { IPAddressInputFieldTextDidChange(ipAddressInputField); });
            rememberDeviceToggle.onValueChanged.AddListener(delegate { RememberDeviceToggleValueDidChange(rememberDeviceToggle); });

            //Source of motion settings ui elements
            sourceOfMotionDropdown.onValueChanged.AddListener(delegate { SourceOfMotionDropDownValueChanged(sourceOfMotionDropdown); });
            yawRotationMultiplierInputField.onEndEdit.AddListener(delegate { RotationMultiplierInputFieldTextDidChange(yawRotationMultiplierInputField); });
            pitchRotationMultiplierInputField.onEndEdit.AddListener(delegate { RotationMultiplierInputFieldTextDidChange(pitchRotationMultiplierInputField); });
            rollRotationMultiplierInputField.onEndEdit.AddListener(delegate { RotationMultiplierInputFieldTextDidChange(rollRotationMultiplierInputField); });

            pitchAccelerationMultiplierInputField.onEndEdit.AddListener(delegate { AccelerationMultiplierInputFieldTextDidChange(pitchAccelerationMultiplierInputField); });
            rollAccelerationMultiplierInputField.onEndEdit.AddListener(delegate { AccelerationMultiplierInputFieldTextDidChange(rollAccelerationMultiplierInputField); });
            lateralForceMultiplierInputField.onEndEdit.AddListener(delegate { LateralForceMultiplierInputFieldTextDidChange(lateralForceMultiplierInputField); });

            //Rotation limits settings ui elements
            yawLimitInputField.onEndEdit.AddListener(delegate { RotationLimitInputFieldTextDidChange(yawLimitInputField); });
            pitchForwardLimitInputField.onEndEdit.AddListener(delegate { RotationLimitInputFieldTextDidChange(pitchForwardLimitInputField); });
            pitchBackwardLimitInputField.onEndEdit.AddListener(delegate { RotationLimitInputFieldTextDidChange(pitchBackwardLimitInputField); });
            rollLimitInputField.onEndEdit.AddListener(delegate { RotationLimitInputFieldTextDidChange(rollLimitInputField); });

            //Set self to delegate, to recieve DidFoundDevice(:) method calls from YAWController
            YawController.Instance().ControllerDelegate = this;

            //YAWController load these properties during Awake()
            rememberDeviceToggle.isOn = YawController.Instance().ShouldRememberDevice;
            yawRotationMultiplierInputField.text = YawController.Instance().RotationMultiplier.y.ToString();
            pitchRotationMultiplierInputField.text = YawController.Instance().RotationMultiplier.x.ToString();
            rollRotationMultiplierInputField.text = YawController.Instance().RotationMultiplier.z.ToString();
            pitchAccelerationMultiplierInputField.text = YawController.Instance().AccelerationMultiplier.x.ToString();
            rollAccelerationMultiplierInputField.text = YawController.Instance().AccelerationMultiplier.y.ToString();
            lateralForceMultiplierInputField.text = YawController.Instance().LateralForceMultiplier.ToString();

            switch (YawController.Instance().ReferenceMotion)
            {
                case ReferenceMotionType.Rotation:
                    sourceOfMotionDropdown.value = 0;
                    break;
                case ReferenceMotionType.Acceleration:
                    sourceOfMotionDropdown.value = 1;
                    break;
                case ReferenceMotionType.Mixed:
                    sourceOfMotionDropdown.value = 2;
                    break;
            }

            //Initially set YAWController related ui elements according to YAWController's state
            RefreshLayout(YawController.Instance().State);
           
            //Start seacrhing for devices
            StartCoroutine(SearchForDevices());
        }

        private void OnDisable()
        {
            StopCoroutine(SearchForDevices());
        }

        private void OnEnable()
        {
            if (firstEnable) { // First OnEnable can be called before YawControllers Awake() - so we wont have access to YawController as a singleton
                firstEnable = false;
                return;
            }
            //Refresh device list
            availableDevices.Clear();
            foreach (GameObject deviceButton in deviceButtons)
            {
                Destroy(deviceButton);
            }
            deviceButtons.Clear();
            StartCoroutine(SearchForDevices());
        }

        private void OnDestroy()
        {
            StopCoroutine(SearchForDevices());

            //Remove all listeners
            connectButton.onClick.RemoveAllListeners();
            disconnectButton.onClick.RemoveAllListeners();
            udpPortInputField.onValueChanged.RemoveAllListeners();
            tcpPortInputField.onValueChanged.RemoveAllListeners();
            ipAddressInputField.onValueChanged.RemoveAllListeners();
            rememberDeviceToggle.onValueChanged.RemoveAllListeners();
            sourceOfMotionDropdown.onValueChanged.RemoveAllListeners();
            yawRotationMultiplierInputField.onEndEdit.RemoveAllListeners();
            pitchRotationMultiplierInputField.onEndEdit.RemoveAllListeners();
            rollRotationMultiplierInputField.onEndEdit.RemoveAllListeners();
            pitchAccelerationMultiplierInputField.onEndEdit.RemoveAllListeners();
            rollAccelerationMultiplierInputField.onEndEdit.RemoveAllListeners();
            lateralForceMultiplierInputField.onEndEdit.RemoveAllListeners();
            yawLimitInputField.onEndEdit.RemoveAllListeners();
            pitchForwardLimitInputField.onEndEdit.RemoveAllListeners();
            pitchBackwardLimitInputField.onEndEdit.RemoveAllListeners();
            rollLimitInputField.onEndEdit.RemoveAllListeners();
        }

        private IEnumerator SearchForDevices()
        {
            while (true)
            {
                if (udpPort != null && udpPort > 1024)
                {
                    YawController.Instance().DiscoverDevices(udpPort.Value);
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        public void LayoutDeviceButtons(List<YawDevice> devices)
        {
            foreach (GameObject deviceButton in deviceButtons)
            {
                Destroy(deviceButton);
            }
            deviceButtons.Clear();

            var scrollViewContentRectTransform = deviceListScrollViewContent.GetComponent<RectTransform>();

            var yOffset = 0f;
            for (var i = 0; i < devices.Count; i++)
            {
                YawDevice device = devices[i];
                if (i == 0)
                {
                    yOffset = scrollViewContentRectTransform.rect.height / 2 - 20f;
                }
                else
                {
                    yOffset -= 50f;
                }
                string textToShow = device.Name;
                if (device.Status != DeviceStatus.Available)
                {
                    textToShow += " - Reserved";
                }
                var deviceButton = (GameObject)Instantiate(DeviceListItemPrefab,
                  Vector2.zero, Quaternion.identity);
                var deviceButtonRectTransform = deviceButton
                    .GetComponent<RectTransform>();
                deviceButtonRectTransform.SetParent(scrollViewContentRectTransform, true);
                deviceButtonRectTransform.anchoredPosition =
                                           new Vector2(0f, yOffset);

                var deviceButtonText = deviceButton.GetComponentInChildren<Text>();
                deviceButtonText.text = textToShow;
                deviceButtonText.fontSize = 10;
                deviceButton.GetComponent<Button>().onClick.AddListener(delegate { DeviceListItemPressed(device); });
                deviceButtons.Add(deviceButton);
                scrollViewContentRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, i * 50 + 100);
            }
        }

        void UDPPortInputFieldTextDidChange(InputField inputField)
        {
            availableDevices.Clear();
            LayoutDeviceButtons(availableDevices);
            int portNumber;
            if (int.TryParse(inputField.text, out portNumber))
            {
                //TODO: - Error - not a port number
                udpPort = portNumber;
            }
            else
            {
                selectedDevice = null;
                udpPort = null;
            }
            SetDeviceFromPortAndIp();
        }

        void TCPPortInputFieldTextDidChange(InputField inputField)
        {
            int portNumber;
            if (int.TryParse(inputField.text, out portNumber))
            {
                tcpPort = portNumber;
            }
            else
            {
                //TODO: - Error - not a port number
                selectedDevice = null;
                tcpPort = null;
            }
            SetDeviceFromPortAndIp();
        }

        void IPAddressInputFieldTextDidChange(InputField inputField)
        {
            IPAddress ipFromString;
            if (IPAddress.TryParse(inputField.text, out ipFromString))
            {
                this.ipAddress = ipFromString;
            }
            else
            {
                //TODO: - Error - not an ip address
                selectedDevice = null;
                ipAddress = null;
            }
            SetDeviceFromPortAndIp();
        }

        void SetDeviceFromPortAndIp()
        {
            if (ipAddress != null && udpPort != null && tcpPort != null)
            {
                //string hostName = Dns.GetHostEntry(this.ipAddress).HostName;
                selectedDevice = new YawDevice(ipAddress, tcpPort.Value, udpPort.Value, "Manually set device", "Manually set device", DeviceStatus.Unknown); //TODO: - status
                connectButton.interactable = true;
            }
            else
            {
                connectButton.interactable = false;
            }
        }

        void ConnectButtonPressed()
        {
            if (selectedDevice != null)
            {
                if (YawController.Instance().Device != null && SameDevice(YawController.Instance().Device, selectedDevice)) return;

                YawController.Instance().ConnectToDevice(
                    selectedDevice,
                    null,
                   (error) =>
                   {
                       ShowError(error);
                   });
            }
        }

        void DisconnectButtonPressed()
        {
            if (YawController.Instance().State != ControllerState.Initial)
            {
                YawController.Instance().DisconnectFromDevice(
                    null,
                    (error) =>
                {
                    ShowError(error);
                });

            }
        }

        void DeviceListItemPressed(YawDevice device)
        {
            if (device.Status != DeviceStatus.Available || YawController.Instance().State != ControllerState.Initial) return;
            ipAddressInputField.text = device.IPAddress.ToString();
            udpPortInputField.text = device.UDPPort.ToString();
            tcpPortInputField.text = device.TCPPort.ToString();
            selectedDevice = device;
            connectButton.interactable = true;
        }

        void SourceOfMotionDropDownValueChanged(Dropdown dropdown)
        {
            switch (dropdown.value) {
                case 0:
                    YawController.Instance().SetReferenceMotionType(ReferenceMotionType.Rotation);
                    break;
                case 1:
                    YawController.Instance().SetReferenceMotionType(ReferenceMotionType.Acceleration);
                    break;
                case 2:
                    YawController.Instance().SetReferenceMotionType(ReferenceMotionType.Mixed);
                    break;
                default:
                    break;
            }
        }

        void RotationMultiplierInputFieldTextDidChange(InputField inputField)
        {
            float yaw;
            float pitch;
            float roll;
            if (float.TryParse(yawRotationMultiplierInputField.text, out yaw) &&
                float.TryParse(pitchRotationMultiplierInputField.text, out pitch) &&
                float.TryParse(rollRotationMultiplierInputField.text, out roll)
               ) {
                YawController.Instance().SetRotationMultiplier(yaw, pitch, roll);
            } else {
                ShowError("Invalid value in rotation multiplier field");
            }
        }

        void AccelerationMultiplierInputFieldTextDidChange(InputField inputField)
        {
            float pitch;
            float roll;
            if (float.TryParse(pitchAccelerationMultiplierInputField.text, out pitch) &&
                float.TryParse(rollAccelerationMultiplierInputField.text, out roll)
               )
            {
                YawController.Instance().SetAccelerationMultiplier(pitch, roll);
            }
            else
            {
                ShowError("Invalid value in acceleration multiplier field");
            }
        }

        void LateralForceMultiplierInputFieldTextDidChange(InputField inputField)
        {
            float roll;
            if (float.TryParse(lateralForceMultiplierInputField.text, out roll)
               )
            {
                YawController.Instance().SetLateralForceMultiplier(roll);
            }
            else
            {
                ShowError("Invalid value in lateral force multiplier field");
            }
        }


        void RotationLimitInputFieldTextDidChange(InputField inputField)
        {
            if (inputField == yawLimitInputField) {
                int yawLimit;
                if (int.TryParse(yawLimitInputField.text, out yawLimit))
                {
                    YawController.Instance().SetYawLimit(yawLimit);
                }
            } else {
                int pitchForwardLimit, pitchBackwardLimit, rollLimit;
                if (int.TryParse(pitchForwardLimitInputField.text, out pitchForwardLimit) &&
                    int.TryParse(pitchBackwardLimitInputField.text, out pitchBackwardLimit) &&
                    int.TryParse(rollLimitInputField.text, out rollLimit))
                {
                    YawController.Instance().SetTiltLimits(pitchForwardLimit, pitchBackwardLimit, rollLimit);
                }
            }
        }

        public void DidFoundDevice(YawDevice device)
        {
            bool contains = false;
            bool containsWithDifferentState = false;
            int index = 0;

            foreach (YawDevice availableDevice in availableDevices)
            {
                if (device.Id == availableDevice.Id)
                {
                    contains = true;
                    if (device.Status != availableDevice.Status || device.TCPPort != availableDevice.TCPPort)
                    {
                        containsWithDifferentState = true;
                        index = availableDevices.IndexOf(availableDevice);
                    }
                }
            }
            if (!contains)
            {
                availableDevices.Add(device);
                LayoutDeviceButtons(availableDevices);
            }
            else if (containsWithDifferentState)
            {
                availableDevices.RemoveAt(index);
                availableDevices.Add(device);
                LayoutDeviceButtons(availableDevices);
            }
        }

        private bool SameDevice(YawDevice device, YawDevice toDevice)
        {
            if (device.Id == toDevice.Id && device.TCPPort == toDevice.TCPPort && device.UDPPort == toDevice.UDPPort) return true;
            return false;
        }

        public void YawLimitDidChange(int currentLimit)
        {
            yawLimitInputField.text = currentLimit.ToString();
        }

        public void TiltLimitsDidChange(int pitchFrontLimit, int pitchBackLimit, int rollLimit)
        {
            pitchForwardLimitInputField.text = pitchFrontLimit.ToString();
            pitchBackwardLimitInputField.text = pitchBackLimit.ToString();
            rollLimitInputField.text = rollLimit.ToString();
        }

        private void RememberDeviceToggleValueDidChange(Toggle toggle)
        {
            YawController.Instance().SetRememberDevice(toggle.isOn);
        }

        public void DidDisconnectFrom(YawDevice device)
        {
            ShowError("Device disconnected");
        }

        public void ControllerStateChanged(ControllerState state)
        {
            RefreshLayout(state);
        }

        private void RefreshLayout(ControllerState state) {
            switch (state)
            {
                case ControllerState.Initial:
                    connectButton.interactable = false;
                    connectButton.GetComponentInChildren<Text>().text = "Connect";
                    setupTitleLabel.text = "Set target YAW device";
                    disconnectButton.gameObject.SetActive(false);
                    ipAddress = null;
                    ipAddressInputField.text = "";
                    tcpPort = null;
                    tcpPortInputField.text = "";

                    availableDevices.Clear();
                    foreach (GameObject deviceButton in deviceButtons)
                    {
                        Destroy(deviceButton);
                    }
                    deviceButtons.Clear();

                    break;
                case ControllerState.Connecting:
                    connectButton.interactable = false;
                    connectButton.GetComponentInChildren<Text>().text = "Connecting...";
                    break;
                case ControllerState.Connected:
                    connectButton.interactable = false;
                    disconnectButton.GetComponentInChildren<Text>().text = "Disconnect";
                    setupTitleLabel.text = "Active device: " + YawController.Instance().Device.Name;
                    disconnectButton.gameObject.SetActive(true);
                    disconnectButton.interactable = true;
                    connectButton.GetComponentInChildren<Text>().text = "Connect";
                    break;
                case ControllerState.Starting:
                    break;
                case ControllerState.Started:
                    break;
                case ControllerState.Stopping:
                    break;
                case ControllerState.Disconnecting:
                    connectButton.interactable = false;
                    disconnectButton.interactable = false;
                    disconnectButton.GetComponentInChildren<Text>().text = "Disconnecting...";
                    break;
            }
        }

        private void ShowError(string error, int duration = 10)
        {
            if (errorText.text != "")
            {
                StopCoroutine(ClearError(duration));
            }
            errorText.text = error;
            StartCoroutine(ClearError(duration));
        }

        private IEnumerator ClearError(int duration)
        {
            yield return new WaitForSeconds(duration);
            errorText.text = "";
        }

    }
}
