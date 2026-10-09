using Leap;
using UnityEngine;

namespace HandTherapy.Gestures
{
    /// <summary>
    /// Detects a quick, alternating wrist bend cycle.
    /// It starts the sequence immediately upon seeing an initial pose.
    /// </summary>
    public class AlternateWristBendDetector : BaseGestureDetector
    {
        private enum Pose { None, L_Up_R_Down, L_Down_R_Up }
        private enum DetectorState { Idle, WaitingForSwitch }

        [Header("Tuning Thresholds (in Degrees)")]
        [Tooltip("The angle between the arm and palm normal must be LESS than this to be 'Up'.")]
        [Range(0.0f, 90.0f)]
        public float wristUpAngleThreshold = 70.0f;

        [Tooltip("The angle between the arm and palm normal must be GREATER than this to be 'Down'.")]
        [Range(90.0f, 180.0f)]
        public float wristDownAngleThreshold = 110.0f;

        [Header("Timing")]
        [Tooltip("How long you have to complete the switch after the initial pose is detected.")]
        [Range(0.2f, 2.0f)]
        public float switchTimeLimit = 1.5f;

        private DetectorState currentState = DetectorState.Idle;
        private Pose confirmedStartPose = Pose.None;
        private float timeSwitchStarted = 0f;

        void Update()
        {
            Hand leftHand = HandDataManager.Instance.LeftHand;
            Hand rightHand = HandDataManager.Instance.RightHand;

            if (leftHand == null || rightHand == null)
            {
                if (currentState != DetectorState.Idle) ResetDetector("Hand lost");
                return;
            }

            Pose detectedPose = GetCurrentPose(leftHand, rightHand);

            switch (currentState)
            {
                case DetectorState.Idle:
                    if (detectedPose != Pose.None)
                    {
                        // An initial pose has been detected. Immediately start waiting for the switch.
                        currentState = DetectorState.WaitingForSwitch;
                        confirmedStartPose = detectedPose;
                        timeSwitchStarted = Time.time;
                    }
                    break;

                case DetectorState.WaitingForSwitch:
                    // Check for timeout
                    if (Time.time - timeSwitchStarted > switchTimeLimit)
                    {
                        ResetDetector("Switch timed out");
                        return;
                    }

                    // Check if the inverse pose has been achieved
                    if ((detectedPose == Pose.L_Up_R_Down && confirmedStartPose == Pose.L_Down_R_Up) ||
                        (detectedPose == Pose.L_Down_R_Up && confirmedStartPose == Pose.L_Up_R_Down))
                    {
                        // The switch happened.
                        CompleteCycle();
                    }
                    break;
            }
        }

        private Pose GetCurrentPose(Hand leftHand, Hand rightHand)
        {
            float leftAngle = Vector3.Angle(leftHand.Arm.Direction, leftHand.PalmNormal);
            float rightAngle = Vector3.Angle(rightHand.Arm.Direction, rightHand.PalmNormal);

            bool isLeftWristUp = leftAngle < wristUpAngleThreshold;
            bool isLeftWristDown = leftAngle > wristDownAngleThreshold;
            bool isRightWristUp = rightAngle < wristUpAngleThreshold;
            bool isRightWristDown = rightAngle > wristDownAngleThreshold;

            if (isLeftWristUp && isRightWristDown) return Pose.L_Up_R_Down;
            if (isLeftWristDown && isRightWristUp) return Pose.L_Down_R_Up;

            return Pose.None;
        }

        private void CompleteCycle()
        {
            Debug.Log("ALTERNATE WRIST BEND CYCLE COMPLETED!");
            ResetDetector("Cycle complete");
            OnGestureDetected.Invoke();
        }

        private void ResetDetector(string reason)
        {
            currentState = DetectorState.Idle;
            confirmedStartPose = Pose.None;
        }

        public override void ResetDetectionState()
        {
            currentState = DetectorState.Idle;
            confirmedStartPose = Pose.None;
            timeSwitchStarted = 0f;
        }
    }
}