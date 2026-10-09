using Leap;
using UnityEngine;

namespace HandTherapy.Gestures
{
    /// <summary>
    /// Detects when BOTH hands are clenched simultaneously.
    /// Fires an event once per cycle, and resets only after both hands have opened again.
    /// This version also resets if a hand is lost.
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

        // State flag to ensure the event only fires once per clench cycle.
        private bool eventFiredForThisCycle = false;

        protected override void EvaluateGesture()
        {
            Hand leftHand = HandDataManager.Instance.LeftHand;
            Hand rightHand = HandDataManager.Instance.RightHand;

            // If we lose sight of either hand, reset the detector's state.
            if (leftHand == null || rightHand == null)
            {
                // If we were waiting for an 'open' to reset, losing a hand should count as a reset.
                if (eventFiredForThisCycle)
                {
                    eventFiredForThisCycle = false;
                    Debug.Log("Hand lost, detector reset.");
                }
                return;
            }

            // If the event has already been fired, we do nothing until both hands open again.
            if (eventFiredForThisCycle)
            {
                if (leftHand.GrabStrength < openThreshold && rightHand.GrabStrength < openThreshold)
                {
                    Debug.Log("Both hands opened. Resetting detector.");
                    eventFiredForThisCycle = false;
                }
                return;
            }

            // Check if both hands are CURRENTLY clenched in this frame.
            bool areBothHandsClenched = leftHand.GrabStrength >= clenchThreshold &&
                                        rightHand.GrabStrength >= clenchThreshold;

            if (areBothHandsClenched)
            {
                Debug.Log("BOTH HANDS CLENCHED!");
                eventFiredForThisCycle = true;
                RaiseGestureDetected();
            }
        }

        public override void ResetDetectionState()
        {
            eventFiredForThisCycle = false;
        }
    }
}