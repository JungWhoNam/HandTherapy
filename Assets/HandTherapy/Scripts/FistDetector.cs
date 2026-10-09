using Leap;
using UnityEngine;

namespace HandTherapy.Gestures
{
    /// <summary>
    /// Detects when a specified hand forms a fist using the built-in GrabStrength property.
    /// </summary>
    public class FistDetector : BaseGestureDetector
    {
        public enum Handedness { Left, Right }

        [Header("Hand Configuration")]
        [Tooltip("Which hand should this component track?")]
        public Handedness handToTrack = Handedness.Left;

        [Header("Tuning")]
        [Tooltip("How closed does the hand need to be to be considered a fist? (0=open, 1=closed)")]
        [Range(0.0f, 1.0f)]
        public float fistThreshold = 0.9f;

        [Tooltip("How open the hand must be before another fist can be counted.")]
        [Range(0.0f, 1.0f)]
        public float openThreshold = 0.1f;

        private bool isReadyToClench = false;

        protected override void EvaluateGesture()
        {
            Hand hand = handToTrack == Handedness.Left
                ? HandDataManager.Instance.LeftHand
                : HandDataManager.Instance.RightHand;

            if (hand == null)
            {
                ResetDetectionState();
                return;
            }

            if (hand.GrabStrength < openThreshold)
            {
                isReadyToClench = true;
                return;
            }

            if (hand.GrabStrength >= fistThreshold && isReadyToClench)
            {
                isReadyToClench = false;
                Debug.Log($"FIST DETECTED for {handToTrack} hand!");
                RaiseGestureDetected();
            }
        }

        public override void ResetDetectionState()
        {
            isReadyToClench = false;
        }
    }
}