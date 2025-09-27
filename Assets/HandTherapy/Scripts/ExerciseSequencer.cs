using System.Collections.Generic;
using UnityEngine;
using TMPro;
using HandTherapy.Gestures;

namespace HandTherapy
{
    [System.Serializable]
    public class ExerciseStep
    {
        public string instruction;
        [Min(1)] public int repetitions = 8;
        public BaseGestureDetector detector;
    }

    public class ExerciseSequencer : MonoBehaviour
    {
        [Header("UI Connections")]
        [Tooltip("UI element for the main instruction.")]
        public TextMeshProUGUI instructionText;
        [Tooltip("UI element for progress (e.g., '5 / 8').")]
        public TextMeshProUGUI progressText;

        [Header("Exercise Routine")]
        [Tooltip("Define your sequence of exercises here.")]
        public List<ExerciseStep> exerciseSequence;

        private int currentExerciseIndex = -1;
        private int currentRep = 0;
        private BaseGestureDetector activeDetector = null;

        void Start()
        {
            StartNextExercise();
        }

        private void StartNextExercise()
        {
            // First, unsubscribe from the previous detector to stop listening to it.
            if (activeDetector != null)
            {
                activeDetector.OnGestureDetected.RemoveListener(OnRepetition);
            }

            currentExerciseIndex++;

            // Check if we have more exercises in the sequence
            if (currentExerciseIndex < exerciseSequence.Count)
            {
                // Get the data for the new step
                ExerciseStep currentStep = exerciseSequence[currentExerciseIndex];
                activeDetector = currentStep.detector;

                // Reset progress for the new step
                currentRep = 0;
                UpdateUI();

                // Subscribe to the new detector's event
                if (activeDetector != null)
                {
                    activeDetector.OnGestureDetected.AddListener(OnRepetition);
                    Debug.Log($"Starting exercise: {currentStep.instruction}");
                }
                else
                {
                    Debug.LogError($"Detector for exercise {currentExerciseIndex} is not assigned!");
                }
            }
            else
            {
                // End of the sequence
                instructionText.text = "All exercises complete!";
                progressText.text = "";
                Debug.Log("ENTIRE SEQUENCE COMPLETE!");
            }
        }

        // This method is called by the active detector's OnGestureDetected event
        private void OnRepetition()
        {
            currentRep++;
            UpdateUI();

            ExerciseStep currentStep = exerciseSequence[currentExerciseIndex];
            if (currentRep >= currentStep.repetitions)
            {
                // Current exercise is complete, advance to the next one.
                StartNextExercise();
            }
        }

        private void UpdateUI()
        {
            if (currentExerciseIndex < exerciseSequence.Count)
            {
                ExerciseStep currentStep = exerciseSequence[currentExerciseIndex];
                if (instructionText != null) instructionText.text = currentStep.instruction;
                if (progressText != null) progressText.text = $"{currentRep} / {currentStep.repetitions}";
            }
        }

        private void OnDestroy()
        {
            // Clean up the listener when the object is destroyed
            if (activeDetector != null)
            {
                activeDetector.OnGestureDetected.RemoveListener(OnRepetition);
            }
        }
    }
}