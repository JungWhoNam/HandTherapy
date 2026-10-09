using Leap;
using UnityEngine;

namespace HandTherapy.Gestures
{
    /// <summary>
    /// Detects when BOTH hands are clenched simultaneously.
    /// Requires both hands to open before each clench, including after tracking loss.
    /// </summary>
    public class BothHandsClenchDetector : BaseGestureDetector
    {
        [Header("Tuning Thresholds")]
        [Tooltip("How closed a hand must be to be considered clenched (0=open, 1=closed).")]
        [Range(0.0f, 1.0f)]
        public float clenchThreshold = 0.9f;

        [Tooltip("How open a hand must be to reset the detector for the next cycle.")]
        [Range(0.0f, 1.0f)]
        public float openThreshold = 0.1f;

        private bool isReadyToClench = false;

        protected override void EvaluateGesture()
        {
            Hand leftHand = HandDataManager.Instance.LeftHand;
            Hand rightHand = HandDataManager.Instance.RightHand;

            if (leftHand == null || rightHand == null)
            {
                ResetDetectionState();
                return;
            }

            bool bothOpen = leftHand.GrabStrength < openThreshold &&
                            rightHand.GrabStrength < openThreshold;

            if (bothOpen)
            {
                isReadyToClench = true;
                return;
            }

            bool bothClenched = leftHand.GrabStrength >= clenchThreshold &&
                                rightHand.GrabStrength >= clenchThreshold;

            if (bothClenched && isReadyToClench)
            {
                isReadyToClench = false;
                Debug.Log("BOTH HANDS CLENCHED!");
                RaiseGestureDetected();
            }
        }

        public override void ResetDetectionState()
        {
            isReadyToClench = false;
        }
    }
}