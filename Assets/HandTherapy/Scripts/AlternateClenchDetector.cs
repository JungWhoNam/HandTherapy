using Leap;
using UnityEngine;
using static HandTherapy.Gestures.FistDetector;

namespace HandTherapy.Gestures
{
    /// <summary>
    /// Detects an alternating clench-and-open cycle between the left and right hands.
    /// </summary>
    public class AlternateClenchDetector : BaseGestureDetector
    {
        private enum DetectorState
        {
            Idle,                      // Waiting for the first hand to start its cycle
            WaitingForFirstHandOpen,   // A hand has clenched, now waiting for it to open
            WaitingForSecondHandClench // First hand's cycle is done, waiting for the second to clench (timer running)
        }

        [Header("Tuning Thresholds")]
        [Tooltip("How closed a hand must be to be considered clenched (0=open, 1=closed).")]
        [Range(0.0f, 1.0f)]
        public float clenchThreshold = 0.9f;

        [Tooltip("How open a hand must be to be considered open.")]
        [Range(0.0f, 1.0f)]
        public float openThreshold = 0.1f;

        [Header("Timing")]
        [Tooltip("The max time in seconds allowed between the first hand opening and the second hand clenching.")]
        [Range(0.1f, 2.0f)]
        public float timeWindowBetweenHands = 1.0f;

        private DetectorState currentState = DetectorState.Idle;
        private Handedness firstHandInSequence;
        private float firstHandOpenedTimestamp;

        void Update()
        {
            Hand leftHand = HandDataManager.Instance.LeftHand;
            Hand rightHand = HandDataManager.Instance.RightHand;

            // If we lose sight of either hand, reset the state machine.
            if (leftHand == null || rightHand == null)
            {
                if (currentState != DetectorState.Idle)
                {
                    ResetDetector("Hand lost");
                }
                return;
            }

            // A state machine to track the gesture sequence
            switch (currentState)
            {
                case DetectorState.Idle:
                    // Look for the start of a cycle: one hand clenching while the other is open.
                    if (leftHand.GrabStrength >= clenchThreshold && rightHand.GrabStrength < openThreshold)
                    {
                        StartSequence(Handedness.Left);
                    }
                    else if (rightHand.GrabStrength >= clenchThreshold && leftHand.GrabStrength < openThreshold)
                    {
                        StartSequence(Handedness.Right);
                    }
                    break;

                case DetectorState.WaitingForFirstHandOpen:
                    // We're waiting for the first hand to open to complete its part of the cycle.
                    Hand firstHand = (firstHandInSequence == Handedness.Left) ? leftHand : rightHand;
                    if (firstHand.GrabStrength < openThreshold)
                    {
                        // First hand's cycle is done. Start the timer for the second hand.
                        currentState = DetectorState.WaitingForSecondHandClench;
                        firstHandOpenedTimestamp = Time.time;
                    }
                    break;

                case DetectorState.WaitingForSecondHandClench:
                    // Check if the user is taking too long.
                    if (Time.time - firstHandOpenedTimestamp > timeWindowBetweenHands)
                    {
                        ResetDetector("Took too long");
                        return;
                    }

                    // Now we wait for the other hand to clench.
                    Hand secondHand = (firstHandInSequence == Handedness.Left) ? rightHand : leftHand;
                    if (secondHand.GrabStrength >= clenchThreshold)
                    {
                        // The full sequence is complete!
                        Debug.Log("ALTERNATE CLENCH CYCLE COMPLETED!");
                        OnGestureDetected.Invoke();
                        ResetDetector("Cycle complete");
                    }
                    break;
            }
        }

        private void StartSequence(Handedness startingHand)
        {
            firstHandInSequence = startingHand;
            currentState = DetectorState.WaitingForFirstHandOpen;
        }

        private void ResetDetector(string reason)
        {
            currentState = DetectorState.Idle;
        }
    }
}