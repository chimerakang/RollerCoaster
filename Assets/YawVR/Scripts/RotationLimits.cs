namespace YawVR {
    public class TiltLimits
    {
        public float yaw, pitchForward, pitchBackward, roll;
        public TiltLimits(float pitchForward = 30, float pitchBackward = 30, float roll = 30)
        {
            this.pitchForward = pitchForward;
            this.pitchBackward = pitchBackward;
            this.roll = roll;
        }
    }
}
