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

        private bool isFist = false; // A flag to ensure we only fire the event once

        protected override void EvaluateGesture()
        {
            Hand hand = (handToTrack == Handedness.Left)
                ? HandDataManager.Instance.LeftHand
                : HandDataManager.Instance.RightHand;

            if (hand != null)
            {
                // Check if the hand's GrabStrength is above our threshold
                if (hand.GrabStrength >= fistThreshold)
                {
                    // If this is the first frame we've detected a fist, fire the event
                    if (!isFist)
                    {
                        isFist = true;
                        Debug.Log($"FIST DETECTED for {handToTrack} hand!");
                        RaiseGestureDetected();
                    }
                }
                else
                {
                    // If the hand is no longer a fist, reset our flag
                    isFist = false;
                }
            }
            else
            {
                // If the hand is lost, reset the flag
                isFist = false;
            }
        }

        public override void ResetDetectionState()
        {
            isFist = false;
        }
    }
}