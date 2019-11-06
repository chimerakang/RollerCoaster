using UnityEngine;
using UnityEngine.UI;

namespace YawVR {
    public class DebugPanel : MonoBehaviour
    {
        [SerializeField]
        private Text rotationText;
        [SerializeField]
        private Text velocityText;
        [SerializeField]
        private Text accelerationText;
        [SerializeField]
        private Text turnAngleText;
        [SerializeField]
        private Text lateralForceText;

        void Update()
        {
            rotationText.text = "Rotation: " + YawController.Instance().ReferenceRotation.ToString();
            velocityText.text = "Velocity: " + YawController.Instance().ReferenceVelocity.ToString();
            accelerationText.text = "Acceleration: " + YawController.Instance().ReferenceAcceleration.ToString();
            turnAngleText.text = "Turn angle(d/s): " + YawController.Instance().ReferenceTurnAngle.ToString();
            lateralForceText.text = "Lateral force: " + YawController.Instance().ReferenceLateralForce.ToString();
        }
    }
}

