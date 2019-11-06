using System.Collections;
using UnityEngine;
using System;
using System.Net;

namespace YawVR {

    public interface YawControllerDelegate
    {
        void ControllerStateChanged(ControllerState state);
        void DidFoundDevice(YawDevice device);
        void DidDisconnectFrom(YawDevice device);
        void YawLimitDidChange(int currentLimit);
        void TiltLimitsDidChange(int pitchFrontLimit, int pitchBackLimit, int rollLimit);
    }

    public interface YawControllerType
    {
        //Properties 
        ControllerState State { get; }
        YawDevice Device { get; }
        YawControllerDelegate ControllerDelegate { get; set; }

        bool ShouldRememberDevice { get; }

        //Motion related properties
        ReferenceMotionType ReferenceMotion { get; }
        Vector3 RotationMultiplier { get; }
        Vector2 AccelerationMultiplier { get; }
        float LateralForceMultiplier { get; }
        Vector3 ReferenceRotation { get; }
        Vector2 ReferenceVelocity { get; }
        Vector2 ReferenceAcceleration { get; }
        float ReferenceTurnAngle { get; }
        float ReferenceLateralForce { get; }
        TiltLimits TiltLimits { get; }
        float? YawLimit { get; }
        int MotionSampleSize { get; }

        //Game related setters
        void SetMotionReference(GameObject gameObject);
        void SetGameName(string gameName);
        void SetRememberDevice(bool shouldRemember);

        //Methods triggering delegate functions
        void DiscoverDevices(int onPort);
        void SetTiltLimits(int pitchFrontLimit, int pitchBackLimit, int rollLimit);
        void SetYawLimit(int yawLimit);

        //Methods with success/error action callbacks
        void ConnectToDevice(YawDevice yawDevice, Action onSuccess, Action<string> onError);
        void StartDevice(Action onSuccess, Action<string> onError);
        void StopDevice(Action onSuccess, Action<string> onError);
        void DisconnectFromDevice(Action onSuccess, Action<string> onError);

        //Setters related to motion data processing
        void SetReferenceMotionType(ReferenceMotionType motionType);
        void SetMotionSampleSize(int size);
        void SetRotationMultiplier(float yaw, float pitch, float roll);
        void SetAccelerationMultiplier(float pitch, float roll);
        void SetLateralForceMultiplier(float roll);
    }

    public class YawController : MonoBehaviour, YawControllerType, YawTCPClientDelegate, YawUDPClientDelegate
    {
        //MARK: - Serializable private fields 

        [SerializeField]
        GameObject referenceGameObject;
        [SerializeField]
        string gameName;
        [SerializeField]
        int udpClientPort;
        [SerializeField]
        private ReferenceMotionType referenceMotion = ReferenceMotionType.Rotation;
        [SerializeField]
        private Vector3 rotationMultiplier = new Vector3(1, 1, 1);
        [SerializeField]
        private Vector2 accelerationMultiplier = new Vector2(1, 1);
        [SerializeField]
        private float lateralForceMultiplier = 1;
        [SerializeField]
        private int motionSampleSize = 5;

        //MARK: - Properties 

        private static YawController instance;

        public ControllerState State { get { return state; } }
        public YawDevice Device { get { return device; } }
        public YawControllerDelegate ControllerDelegate { get; set; }
        public bool ShouldRememberDevice { get { return shouldRememberDevice; } }

        private YawTCPClient tcpCLient;
        private YawUDPClient udpClient;
        private YawDevice device = null;
        private ControllerState state = ControllerState.Initial;
        private int discoveryPort = 0;
        private CallBacks callBacks = new CallBacks();
        private CallbackTimeouts callbackTimeouts = new CallbackTimeouts();
        private bool shouldRememberDevice = false;

        //Motion related properties
        public ReferenceMotionType ReferenceMotion { get { return referenceMotion; } }
        public Vector3 RotationMultiplier { get { return rotationMultiplier; } }
        public Vector2 AccelerationMultiplier { get { return accelerationMultiplier; } }
        public float LateralForceMultiplier { get { return lateralForceMultiplier; } }
        public Vector3 ReferenceRotation { get { return referenceRotation; } }
        public Vector2 ReferenceVelocity { get { return referenceVelocity; } }
        public Vector2 ReferenceAcceleration { get { return referenceAcceleration; } }
        public float ReferenceTurnAngle { get { return referenceTurnAngle; } }
        public float ReferenceLateralForce { get { return referenceLateralForce; } }
        public TiltLimits TiltLimits { get { return tiltLimits; } }
        public float? YawLimit { get { return yawLimit; } }
        public int MotionSampleSize { get { return motionSampleSize; } }

        private Vector3 referenceRotation = new Vector3();
        private Vector2 referenceVelocity = new Vector2();
        private Vector2 referenceAcceleration = new Vector2();
        private float referenceTurnAngle = 0;
        private float referenceLateralForce = 0;
        private TiltLimits tiltLimits;
        private float? yawLimit;

        //Calculation related variables

        private Vector3? lastPosition;
        private Vector2? lastVelocityInWorldSpace = null;
        private float? lastAngle;
        private Vector2? lastVelocityInOwnSpace;
        private Rigidbody referenceRigidbody = null;
        private Vector2[] latestAccelerations;
        private Vector2[] latestVelocities;
        private float[] latestLateralForces;
        private int latestAccelerationIndex = 0;
        private int latestVelocityIndex = 0;
        private int latestLateralForceIndex = 0;

        public static YawController Instance()
        {
            if (instance == null)
            {
                throw new Exception("Please drag YawController prefab into your scene");
            }
            return instance;
        }

        //MARK: - Lifecycle methods

        void Awake()
        {
            //Creating singleton instance
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }

            //Make gameObject persitent through multiple scenes
            DontDestroyOnLoad(gameObject);

            //Initialize tcp client
            tcpCLient = new YawTCPClient();
            tcpCLient.tcpDelegate = this;

            //Initialize udp client and start listening on given listening port
            udpClient = new YawUDPClient(udpClientPort);
            udpClient.udpDelegate = this;
            udpClient.StartListening();

            //Initialize motion data arrays of given sample size
            latestAccelerations = new Vector2[motionSampleSize];
            latestVelocities = new Vector2[motionSampleSize];
            latestLateralForces = new float[motionSampleSize];

            //Loading motion preferences and connect to previously used device if exists and shouldRememberDevice is saved as true
            //We do this in Awake(), in order to when settings start it can access loaded up to date values
            Load();
        }

        void Start()
        {
            //Setting reference rigidbody if reference gameObject has one as its component
            if (referenceGameObject != null)
            {
                referenceRigidbody = referenceGameObject.GetComponent<Rigidbody>();
            }
        }

        void FixedUpdate()
        {
            //Processing motion data from reference gameObject
            ProcessMotionData();
            //If we are in game, sending rotation command to simulator  based on the latest processed motion data
            if (state == ControllerState.Started)
            {
                SendMotionData();
            }
        }


        void OnApplicationQuit()
        {
            //If our application terminates, sending Exit command to simulator if needed 
            if (state != ControllerState.Initial && state != ControllerState.Disconnecting && device != null)
            {
                DisconnectFromDevice( null, null);
            }
            //Closing tcp & udp clients
            tcpCLient.CloseConnection();
            udpClient.StopListening();
        }

        //MARK: - Game related setters

        public void SetMotionReference(GameObject gameObject)
        {
            referenceGameObject = gameObject;
            referenceRigidbody = referenceGameObject.GetComponent<Rigidbody>();
        }

        public void SetGameName(string gameName)
        {
            this.gameName = gameName;
        }

        public void SetRememberDevice(bool shouldRemember)
        {
            if (shouldRemember)
            {
                shouldRememberDevice = true;
                PlayerPrefs.SetString("REMEMBER_DEVICE", "TRUE");
            }
            else
            {
                shouldRememberDevice = false;
                PlayerPrefs.SetString("REMEMBER_DEVICE", "FALSE");
            }
            PlayerPrefs.Save();
        }

        //MARK: - Methods triggering delegate functions

        public void DiscoverDevices(int onPort)
        {
            //Save a reference to port, which will be used in creating yawDevices when discovery responses arrive
            //We have to use their listening port (this) - not from which it sends response
            discoveryPort = onPort;
            //Send the discovery broadcast
            udpClient.SendBroadcast(onPort, Commands.DEVICE_DISCOVERY);
        }

        public void SetYawLimit(int yawLimit)
        {
            //If we are connected, send a request to set yaw limit
            if (state != ControllerState.Initial && state != ControllerState.Disconnecting)
            {
                tcpCLient.BeginSend(Commands.SET_YAW_LIMIT(yawLimit));
            }
        }

        public void SetTiltLimits(int pitchForwardLimit, int pitchBackwardLimit, int rollLimit)
        {
            //If we are connected, send a request to set tilt limits
            if (state != ControllerState.Initial && state != ControllerState.Disconnecting)
            {
                tcpCLient.BeginSend(Commands.SET_TILT_LIMITS(pitchForwardLimit, pitchBackwardLimit, rollLimit));
            }
        }

        //MARK: - Methods with success/error action callbacks

        public void ConnectToDevice(YawDevice yawDevice, Action onSuccess, Action<String> onError)
        {

            if (yawDevice.Status != DeviceStatus.Available)
            {
                onError("Device is not available");
                SetState(ControllerState.Initial);
                return;
            }

            if (state == ControllerState.Initial)
            {
                SetState(ControllerState.Connecting);

                //Start tcp connection timeout
                callbackTimeouts.tcpConnectionAttemptTimeout = StartCoroutine(ResponseTimeout((error) => {
                    onError("Failed to create TCP connection");
                    SetState(ControllerState.Initial);
                    tcpCLient.StopConnecting();
                    Debug.Log("TCP client connecting timeout- initial set before");

                }));

                //Start connecting to simulator's tcp server
                tcpCLient.Initialize(yawDevice.IPAddress.ToString(),
                                     yawDevice.TCPPort,
                                     () => {
                    //Connected to tcp server
                    //Stop tcp connection timeout
                    StopCoroutine(callbackTimeouts.tcpConnectionAttemptTimeout);
                    callbackTimeouts.tcpConnectionAttemptTimeout = null;
                    //Set connected device to this device 
                    device = yawDevice;
                    //Start listening for tcp messages
                    tcpCLient.BeginRead();
                                         
                    //Start sending CHECK_IN command to connected tcp server
                    //Set CHECK_IN command callbacks and start command timeout
                    callBacks.connectingError = onError;
                    callBacks.connectingSuccess = onSuccess;
                    callbackTimeouts.connectingTimeout = StartCoroutine(ResponseTimeout((error) => {
                         onError(error);
                         SetState(ControllerState.Initial);
                     }));
                    //Send CHECK_IN command
                    tcpCLient.BeginSend(Commands.CHECK_IN(udpClientPort, gameName));

                 },
                                     (error) => {
                    //Could not connect to tcp server
                    //Stop tcp connection timeout
                    StopCoroutine(callbackTimeouts.tcpConnectionAttemptTimeout);
                    callbackTimeouts.tcpConnectionAttemptTimeout = null;
                    onError(error);
                    //Set state back to initial
                    SetState(ControllerState.Initial);
                });
            } else {
                //If we are already connected to a device, disconnect from it, then connect to new one
                DisconnectFromDevice(
                    () => {
                        ConnectToDevice(yawDevice, onSuccess, onError);
                    },
                    (error) => {
                        onError(error);
                        ConnectToDevice(yawDevice, onSuccess, onError);
                    });
            }
        }

        public void StartDevice(Action onSuccess, Action<String> onError)
        {
            if (state == ControllerState.Connected)
            {
                //Set START command callbacks and start command timeout
                callBacks.startSuccess = onSuccess;
                callBacks.startError = onError;
                callbackTimeouts.startTimeout = StartCoroutine(ResponseTimeout(onError));
                //Send START command
                tcpCLient.BeginSend(Commands.START);
                //Set state to starting
                SetState(ControllerState.Starting);
            } else {
                onError("Attempted to start device when device has not been in connected ready state");
            }
        }

        public void StopDevice(Action onSuccess, Action<String> onError)
        {
            if (state == ControllerState.Started) 
            {
                //Set STOP command callbacks and start command timeout
                callBacks.stopSuccess = onSuccess;
                callBacks.stopError = onError;
                callbackTimeouts.stopTimeout = StartCoroutine(ResponseTimeout(onError));
                //Send STOP command
                tcpCLient.BeginSend(Commands.STOP);
                //Set state to stopping
                SetState(ControllerState.Stopping);
            } else {
                onError("Attempted to stop simulator when simulator had not been in started state");
            }
        }

        public void DisconnectFromDevice(Action onSuccess, Action<String> onError)
        {
            if (state != ControllerState.Initial)
            {
                //Set EXIT command callbacks and start command timeout
                callBacks.exitSuccess = onSuccess;
                callBacks.exitError = onError;
                callbackTimeouts.exitTimeout = StartCoroutine(ResponseTimeout((error) => {
                    //If we reach timeout without server response, set state back to initial
                    //This way we reach disconnected and ready state anyway
                    SetState(ControllerState.Initial);
                    if (onError != null) {
                        onError(error);
                    }
                }));
                //Send EXIT command
                tcpCLient.BeginSend(Commands.EXIT);
                //Set state to disconnecting
                SetState(ControllerState.Disconnecting);
            }
            else
            {
                onError("Attempted to disconnect when no device was connected");
            }
        }

        // MARK: - Delegate methods

        public void DidRecieveUDPMessage(string message, IPEndPoint remoteEndPoint)
        {
            if (message.StartsWith("Y[") && message.EndsWith("]") && 
                state != ControllerState.Initial && 
                //Only accept position report from the connected simulator ip address
                (device.IPAddress.ToString() == remoteEndPoint.Address.ToString())
               )
            {
                //We recieved a position report from the connected simulator
                //Extracting rotation values from the message if it is valid
                var messageParts = message.Split('[', ']');
                if (messageParts.Length != 6) return;
                float yaw, pitch, roll;
                if (float.TryParse(messageParts[1], out yaw) &&
                    float.TryParse(messageParts[3], out pitch) &&
                    float.TryParse(messageParts[5], out roll))
                {
                    //Set device's actual position
                    var eulerAnglesVector = new Vector3(pitch, yaw, roll);
                    device.ActualPosition = eulerAnglesVector;
                }
            }
            else if (message.Contains("YAWDEVICE"))
            {
                //We recieved a device discovery answer message
                //example device discovery answer: "YAWDEVICE;MacAddrId;MyDeviceName;" + tcpServerPort + (state == DeviceState.Available ? ";OK" : ";RESERVED");
                var messageParts = message.Split(';');
                var ip = remoteEndPoint.Address;
                var udp = discoveryPort;
                int tcp;
                if (messageParts.Length == 5 && int.TryParse(messageParts[3], out tcp))
                {
                    DeviceStatus status = messageParts[4] == "AVAILABLE" ? DeviceStatus.Available : DeviceStatus.Reserved;
                    var yawDevice = new YawDevice(ip, tcp, udp, messageParts[1], messageParts[2], status);
                    //Call delegate function if we have a delegate
                    if (ControllerDelegate != null)
                    {
                        ControllerDelegate.DidFoundDevice(yawDevice);
                    }
                }
            }
        }

        public void DidRecieveTCPMessage(byte[] data)
        {
            //data.Length can't be 0 - YawTcpClient would not dispatch it
            //Read command id from the array
            byte commandId = data[0];

            switch (commandId)
            {
                case CommandIds.CHECK_IN_ANS:  
                    //Stop timeout
                    if (callbackTimeouts.connectingTimeout != null)
                    {
                        StopCoroutine(callbackTimeouts.connectingTimeout);
                        callbackTimeouts.connectingTimeout = null;
                    }
                    if (state == ControllerState.Connecting) {
                        string message = System.Text.Encoding.ASCII.GetString(data, 1, data.Length - 1);
                        if (message == "AVAILABLE")
                        {
                            //Simulator is available, we have succesfully checked in, set state to connected
                            udpClient.SetRemoteEndPoint(device.IPAddress, device.UDPPort);
                            SetState(ControllerState.Connected);
                            //Call success callback
                            if (callBacks.connectingSuccess != null)
                            {
                                callBacks.connectingSuccess();
                                callBacks.connectingSuccess = null;
                                callBacks.connectingError = null;
                            }
                            SaveDeviceData(device);
                        }
                        else
                        {
                            //Simulator is reserved, setting state back to initial
                            var messageParts = message.Split(';');
                            if (messageParts.Length != 3) return;
                            var reservingGameName = messageParts[1];
                            var reservingIp = messageParts[2];
                            SetState(ControllerState.Initial);
                            //Call error callback
                            if (callBacks.connectingError != null)
                            {
                                callBacks.connectingError("Device is in use from: " + reservingIp + " with game: " + gameName);
                                callBacks.connectingError = null;
                                callBacks.connectingSuccess = null;
                            }
                        }
                    }
                    break;

                case CommandIds.START: 
                    //Stop timeout
                    if (callbackTimeouts.startTimeout != null)
                    {
                        StopCoroutine(callbackTimeouts.startTimeout);
                        callbackTimeouts.startTimeout = null;
                    }
                    if (state == ControllerState.Starting)
                    {
                        //Set state to started
                        SetState(ControllerState.Started);
                        //Call success callback
                        if (callBacks.startSuccess != null)
                        {
                            callBacks.startSuccess();
                            callBacks.startSuccess = null;
                            callBacks.startError = null;
                        }
                    } 
                    break;

                case CommandIds.STOP:
                    //Stop timeout
                    if (callbackTimeouts.stopTimeout != null)
                    {
                        StopCoroutine(callbackTimeouts.stopTimeout);
                        callbackTimeouts.stopTimeout = null;
                    }
                    if (state != ControllerState.Initial && state != ControllerState.Disconnecting)
                    {
                        //Set state back to connected
                        SetState(ControllerState.Connected);
                        //Call success callback
                        if (callBacks.stopSuccess != null)
                        {
                            callBacks.stopSuccess();
                            callBacks.stopSuccess = null;
                            callBacks.stopError = null;
                        }
                    } 
                    break;

                case CommandIds.EXIT:
                    //Stop timeout
                    if (callbackTimeouts.exitTimeout != null)
                    {
                        StopCoroutine(callbackTimeouts.exitTimeout);
                        callbackTimeouts.exitTimeout = null;
                    }
                    //Whenever we got an exit command from connected simulator, we close connection, not only if we invoked that
                    SetState(ControllerState.Initial);
                    //Call success callback - if we have one
                    if (callBacks.exitSuccess != null)
                    {
                        callBacks.exitSuccess();
                        callBacks.exitSuccess = null;
                        callBacks.exitError = null;
                    }
                    break;

                case CommandIds.SET_YAW_LIMIT:
                    if (data.Length == 5) {
                        int limit = Commands.ByteArrayToInt(data, 1);
                        this.yawLimit = limit;
                        if (ControllerDelegate != null)
                        {
                            ControllerDelegate.YawLimitDidChange(limit);
                        }
                    }
                    break;
                case CommandIds.SET_TILT_LIMITS:
                    if (data.Length == 13)
                    {
                        int pitchForwardLimit = Commands.ByteArrayToInt(data, 1);
                        int pitchBackwardLimit = Commands.ByteArrayToInt(data, 5);
                        int rollLimit = Commands.ByteArrayToInt(data, 9);

                        if (tiltLimits == null)
                        {
                            tiltLimits = new TiltLimits(pitchForwardLimit, pitchBackwardLimit, rollLimit);
                        }
                        else
                        {
                            tiltLimits.pitchForward = pitchForwardLimit;
                            tiltLimits.pitchBackward = pitchBackwardLimit;
                            tiltLimits.roll = rollLimit;
                        }
                        if (ControllerDelegate != null) {
                            ControllerDelegate.TiltLimitsDidChange(pitchForwardLimit, pitchBackwardLimit, rollLimit);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public void DidLostServerConnection()
        {
            Debug.Log("TCP Client have disconnected");
            if (ControllerDelegate != null)
            {
                ControllerDelegate.DidDisconnectFrom(device);
            }
            SetState(ControllerState.Initial); 
        }

        //MARK: - Methods related to motion data processing

        public void SetReferenceMotionType(ReferenceMotionType motionType)
        {
            referenceMotion = motionType;
            string motionTypeString = "";
            switch (referenceMotion)
            {
                case ReferenceMotionType.Rotation:
                    motionTypeString = "ROTATION";
                    break;
                case ReferenceMotionType.Acceleration:
                    motionTypeString = "ACCELERATION";
                    break;
                case ReferenceMotionType.Mixed:
                    motionTypeString = "MIXED";
                    break;
            }
            PlayerPrefs.SetString("MOTION_TYPE", motionTypeString);
            PlayerPrefs.Save();
        }

        public void SetMotionSampleSize(int size)
        {
            if (size >= 1) {
                motionSampleSize = size;
                latestAccelerations = new Vector2[size];
                latestVelocities = new Vector2[size];
                latestLateralForces = new float[size];
                latestAccelerationIndex = 0;
                latestVelocityIndex = 0;
                latestLateralForceIndex = 0;
            }
        }

        public void SetRotationMultiplier(float yaw, float pitch, float roll)
        {
            rotationMultiplier.x = pitch;
            rotationMultiplier.y = yaw;
            rotationMultiplier.z = roll;

            PlayerPrefs.SetFloat("YAW_ROTATION_MULTIPLIER", rotationMultiplier.y);
            PlayerPrefs.SetFloat("PICTH_ROTATION_MULTIPLIER", rotationMultiplier.x);
            PlayerPrefs.SetFloat("ROLL_ROTATION_MULTIPLIER", rotationMultiplier.z);
            PlayerPrefs.Save();
        }

        public void SetAccelerationMultiplier(float pitch, float roll)
        {
            accelerationMultiplier.x = pitch;
            accelerationMultiplier.y = roll;

            PlayerPrefs.SetFloat("PITCH_ACCELERATION_MULTIPLIER", accelerationMultiplier.x);
            PlayerPrefs.SetFloat("ROLL_ACCELERATION_MULTIPLIER", accelerationMultiplier.y);
            PlayerPrefs.Save();
        }

        public void SetLateralForceMultiplier(float roll) {
            lateralForceMultiplier = roll;

            PlayerPrefs.SetFloat("LATERAL_FORCE_MULTIPLIER", lateralForceMultiplier);
            PlayerPrefs.Save();
        }

        private void ProcessMotionData()
        {
            if (referenceGameObject == null) return;

            if (referenceMotion != ReferenceMotionType.Acceleration)
            {
                //Getting reference rotation from reference gameObject
                referenceRotation = referenceGameObject.transform.eulerAngles;
            }

            if (referenceMotion != ReferenceMotionType.Rotation && referenceRigidbody != null)
            {
                if (lastVelocityInWorldSpace != null && lastVelocityInOwnSpace != null)
                {

                    //Get velocity in own space and world space
                    var localVelocity = referenceGameObject.transform.InverseTransformDirection(referenceRigidbody.velocity);
                    Vector2 velocityInOwnSpace = new Vector2(localVelocity.x, localVelocity.z);
                    Vector2 velocityInWorldSpace = new Vector2(referenceRigidbody.velocity.x, referenceRigidbody.velocity.z);

                    //Calculate turn angle from velocity vector angle change
                    referenceTurnAngle = Vector2.SignedAngle(lastVelocityInWorldSpace.Value, velocityInWorldSpace) * 1 / Time.fixedDeltaTime;

                    //Sample velocity in own space
                    latestVelocities[latestVelocityIndex] = velocityInOwnSpace;
                    latestVelocityIndex = (latestVelocityIndex + 1) % latestVelocities.Length;
                    Vector2 velSum = new Vector2();
                    for (int i = 0; i < latestVelocities.Length; i++)
                    {
                        velSum += latestVelocities[i];
                    }
                    referenceVelocity = velSum / latestVelocities.Length;

                    //Calculate and sample acceleration in own space
                    latestAccelerations[latestAccelerationIndex] = (velocityInOwnSpace - lastVelocityInOwnSpace.Value) / Time.fixedDeltaTime;
                    latestAccelerationIndex = (latestAccelerationIndex + 1) % latestAccelerations.Length;
                    Vector2 accSum = new Vector2();
                    for (int i = 0; i < latestAccelerations.Length; i++)
                    {
                        accSum += latestAccelerations[i];
                    }
                    referenceAcceleration = accSum / latestAccelerations.Length;

                    //Calculate and sample lateral force
                    latestLateralForces[latestLateralForceIndex] = referenceTurnAngle * velocityInOwnSpace.magnitude;
                    latestLateralForceIndex = (latestLateralForceIndex + 1) % latestLateralForces.Length;
                    float latSum = 0;
                    for (int i = 0; i < latestLateralForces.Length; i++)
                    {
                        latSum += latestLateralForces[i];
                    }
                    referenceLateralForce = (latSum / latestLateralForces.Length) * 0.01f;
                }

                //Save last velocity vectors
                var locVel = referenceGameObject.transform.InverseTransformDirection(referenceRigidbody.velocity);
                lastVelocityInOwnSpace = new Vector2(locVel.x, locVel.z);
                lastVelocityInWorldSpace = new Vector2(referenceRigidbody.velocity.x, referenceRigidbody.velocity.z);
            }
        }

        private void SendMotionData()
        {
            if (device == null) return;

            //Calculate rotation according to reference motion type - use signed angle form for calculations
            float x = 0, y = 0, z = 0;
            switch (referenceMotion)
            {
                case ReferenceMotionType.Rotation:
                    x = SignedForm(referenceRotation.x) * rotationMultiplier.x;
                    y = SignedForm(referenceRotation.y) * rotationMultiplier.y;
                    z = SignedForm(referenceRotation.z) * rotationMultiplier.z;
                    break;
                case ReferenceMotionType.Acceleration:
                    x = -referenceAcceleration.y * accelerationMultiplier.x;
                    y = 0;
                    z = -(referenceAcceleration.x * accelerationMultiplier.y + referenceLateralForce * lateralForceMultiplier); 
                    break;
                case ReferenceMotionType.Mixed:
                    x = SignedForm(referenceRotation.x) * rotationMultiplier.x -
                        referenceAcceleration.y * accelerationMultiplier.x; 
                    y = SignedForm(referenceRotation.y) * rotationMultiplier.y;
                    z = SignedForm(referenceRotation.z) * rotationMultiplier.z -
                                            (referenceAcceleration.x * accelerationMultiplier.y + referenceLateralForce * lateralForceMultiplier);
                    break;
            }

            //Convert back to unsigned form, and apply absolute limits to be compatible to our UnsignedForm(:float) function
            x = UnsignedForm(Mathf.Clamp(x, -90, 90));
            y = UnsignedForm(Mathf.Clamp(y, -180, 180));
            z = UnsignedForm(Mathf.Clamp(z, -90, 90));

            //Send the calculate rotations, SendRotation(:Vector3) will apply limits
            SendRotation(new Vector3(x, y, z));
        }

        //MARK: - UDP command sender functions

        private void SendRotation(Vector3 rotation)
        {
            float x, y, z;
            if (yawLimit != null)
            {
                y = UnsignedForm(Mathf.Clamp(SignedForm(rotation.y), -yawLimit.Value, yawLimit.Value));
            }
            else
            {
                y = rotation.y;
            }
            if (tiltLimits != null)
            {
                x = UnsignedForm(Mathf.Clamp(SignedForm(rotation.x), -tiltLimits.pitchBackward, tiltLimits.pitchForward));
                z = UnsignedForm(Mathf.Clamp(SignedForm(rotation.z), -tiltLimits.roll, tiltLimits.roll));
            }
            else
            {
                x = rotation.x;
                z = rotation.z;
            }
            udpClient.Send(Commands.SET_POSITION(y, x, z));
        }


        //MARK: - Methods related to saving and loadnig device data

        private void Load()
        {
            //Loads game side only parameters - motion type, and motion-rotation multipliers
            LoadMotionConfiguration();

            //If we saved that game should remember last succesfully connected device, start connecting to it
            string shouldLoad = PlayerPrefs.GetString("REMEMBER_DEVICE", "");
            if (!string.IsNullOrEmpty(shouldLoad))
            {
                shouldRememberDevice = (shouldLoad == "TRUE");
            }
            if (shouldRememberDevice)
            {
                //return value YAWDevice can be null
                YawDevice loadedDevice = LoadFromSavedDeviceData();
                if (loadedDevice != null)
                {
                    ConnectToDevice(loadedDevice,
                                    () => { },
                                    (error) => { }
                                   );
                }
            }
        }

        private void LoadMotionConfiguration()
        {
            //Load and set motion type
            //Directly setting property value, because setter would invoke a save
            string motionType = PlayerPrefs.GetString("MOTION_TYPE", "");
            switch (motionType)
            {
                case "ROTATION":
                    referenceMotion = ReferenceMotionType.Rotation;
                    break;
                case "ACCELERATION":
                    referenceMotion = ReferenceMotionType.Acceleration;
                    break;
                case "MIXED":
                    referenceMotion = ReferenceMotionType.Mixed;
                    break;
                default:
                    referenceMotion = ReferenceMotionType.Rotation;
                    break;
            }
            //Load motion multipliers
            float yawRot = PlayerPrefs.GetFloat("YAW_ROTATION_MULTIPLIER", 1);
            float pitchRot = PlayerPrefs.GetFloat("PICTH_ROTATION_MULTIPLIER", 1);
            float rollRot =  PlayerPrefs.GetFloat("ROLL_ROTATION_MULTIPLIER", 1);
            float pitchAcc =  PlayerPrefs.GetFloat("PITCH_ACCELERATION_MULTIPLIER", 1);
            float rollAcc = PlayerPrefs.GetFloat("ROLL_ACCELERATION_MULTIPLIER", 1);
            float rollLat = PlayerPrefs.GetFloat("LATERAL_FORCE_MULTIPLIER", 1);

            rotationMultiplier.x = pitchRot;
            rotationMultiplier.y = yawRot;
            rotationMultiplier.z = rollRot;
            accelerationMultiplier.x = pitchAcc;
            accelerationMultiplier.y = rollAcc;
            lateralForceMultiplier = rollLat;
        }

        private YawDevice LoadFromSavedDeviceData()
        {
            //Can return null if there is no saved device

            YawDevice yawDevice = null;
            var deviceId = PlayerPrefs.GetString("LAST_USED_DEVICE_ID", "");
            var deviceName = PlayerPrefs.GetString("LAST_USED_DEVICE_NAME", "");
            var deviceIP = PlayerPrefs.GetString("LAST_USED_IP", "");
            var udpPort = PlayerPrefs.GetInt("LAST_USED_UDP_PORT", 0);
            var tcpPort = PlayerPrefs.GetInt("LAST_USED_TCP_PORT", 0);

            IPAddress ip;
            if (!string.IsNullOrEmpty(deviceId) &&
                !string.IsNullOrEmpty(deviceName) &&
                !string.IsNullOrEmpty(deviceIP) &&
                IPAddress.TryParse(deviceIP, out ip) &&
                udpPort != 0 && tcpPort != 0)
            {
                yawDevice = new YawDevice(ip, tcpPort, udpPort, deviceId, deviceName, DeviceStatus.Available);
            }
            return yawDevice;
        }

        private void SaveDeviceData(YawDevice yawDevice)
        { 
            //Save data if needed
            if (shouldRememberDevice)
            {
                PlayerPrefs.SetInt("LAST_USED_UDP_PORT", yawDevice.UDPPort);
                PlayerPrefs.SetInt("LAST_USED_TCP_PORT", yawDevice.TCPPort);
                PlayerPrefs.SetString("LAST_USED_DEVICE_ID", yawDevice.Id);
                PlayerPrefs.SetString("LAST_USED_DEVICE_NAME", yawDevice.Name);
                PlayerPrefs.SetString("LAST_USED_IP", yawDevice.IPAddress.ToString());
                PlayerPrefs.Save();
            }
            else
            {
                //Erase previously saved device data if we should not save
                PlayerPrefs.DeleteKey("LAST_USED_UDP_PORT");
                PlayerPrefs.DeleteKey("LAST_USED_TCP_PORT");
                PlayerPrefs.DeleteKey("LAST_USED_DEVICE_ID");
                PlayerPrefs.DeleteKey("LAST_USED_DEVICE_NAME");
                PlayerPrefs.DeleteKey("LAST_USED_IP");
                PlayerPrefs.Save();
            }
        }


        //MARK: - Helper functions
        private void SetState(ControllerState newState)
        {
            state = newState;
            if (newState == ControllerState.Initial) {
                device = null;
                if (tcpCLient.Connected) {
                    tcpCLient.CloseConnection();
                }
            }
            if (ControllerDelegate != null)
            {
                ControllerDelegate.ControllerStateChanged(newState);
            }
        }

        private IEnumerator ResponseTimeout(Action<string> onError)
        {
            if (onError == null) yield break;
            yield return new WaitForSeconds(10f);
            onError("Command timeout");
        }

        private float SignedForm(float angle) {
            return angle >= 180 ? angle - 360 : angle;
        }

        private float UnsignedForm(float angle) {
            return angle < 0 ? 360 + angle : angle;
        }

        //MARK: - Helper structs

        private struct CallBacks
        {
            public Action connectingSuccess;
            public Action<string> connectingError;
            public Action startSuccess;
            public Action<string> startError;
            public Action stopSuccess;
            public Action<string> stopError;
            public Action exitSuccess;
            public Action<string> exitError;
        }
        private struct CallbackTimeouts
        {
            public Coroutine connectingTimeout;
            public Coroutine startTimeout;
            public Coroutine stopTimeout;
            public Coroutine exitTimeout;
            public Coroutine tcpConnectionAttemptTimeout;
        }
    }

}
