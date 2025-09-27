using HandTherapy.Gestures;
using UnityEngine;

namespace HandTherapy
{
    public class AppManager : MonoBehaviour
    {
        [Header("Connections")]
        [Tooltip("The ExerciseSequencer that runs the main routine.")]
        public ExerciseSequencer exerciseSequencer;

        private bool sequenceStarted = false;

        void Update()
        {
            // Do nothing if the sequence has already been started
            if (sequenceStarted) return;

            // When both hands are visible, tell the sequencer to start
            if (HandDataManager.Instance.LeftHand != null && HandDataManager.Instance.RightHand != null)
            {
                sequenceStarted = true;
                exerciseSequencer.StartSequence();
            }
        }
    }
}