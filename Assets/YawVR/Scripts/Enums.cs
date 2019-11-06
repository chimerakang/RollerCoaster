namespace YawVR {

    public enum ReferenceMotionType
    {
        Rotation, Acceleration, Mixed
    }

    public enum DeviceStatus
    {
        Available, Reserved, Unknown
    }

    public enum Result
    {
        Success, Error
    }

    public enum ControllerState
    {
        Initial, Connecting, Connected, Starting, Started, Stopping, Disconnecting
    } 
}
