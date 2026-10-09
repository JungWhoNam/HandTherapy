using System.Collections.Generic;
using UnityEngine;
using TMPro;
using HandTherapy.Gestures;
using UnityEngine.Events;

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
        private enum AppState { Welcome, InProgress, Complete }

        [Header("UI Screens")]
        public GameObject welcomeScreenUI;
        public GameObject exerciseUI;

        [Header("UI Connections")]
        [Tooltip("UI element for the main instruction.")]
        public TextMeshProUGUI instructionText;
        [Tooltip("UI element for progress (e.g., '5 / 8').")]
        public TextMeshProUGUI progressText;
        [Tooltip("UI text for the top-right corner, e.g., '운동 1 / 3'.")]
        public TextMeshProUGUI progressIndicatorText;

        [Header("Exercise Routine")]
        [Tooltip("Define your sequence of exercises here.")]
        public List<ExerciseStep> exerciseSequence;

        [Header("Events")]
        [Tooltip("Fired when the entire exercise sequence is completed.")]
        public UnityEvent OnSequenceCompleted;

        private AppState currentState = AppState.Welcome;
        private int currentExerciseIndex = -1;
        private int currentRep = 0;
        private BaseGestureDetector activeDetector = null;

        void Start()
        {
            // Start by showing the welcome screen
            currentState = AppState.Welcome;
            welcomeScreenUI.SetActive(true);
            exerciseUI.SetActive(false);
            if (progressIndicatorText != null) progressIndicatorText.text = "";

        }

        // This is called by the AppManager when hands are detected
        public void StartSequence()
        {
            if (currentState != AppState.Welcome) return;

            currentState = AppState.InProgress;
            welcomeScreenUI.SetActive(false);
            exerciseUI.SetActive(true);

            StartNextExercise();
        }

        private void StartNextExercise()
        {
            ReleaseActiveDetector();

            currentExerciseIndex++;
            UpdateProgressIndicator();

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
                    activeDetector.BeginDetection();
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
                currentState = AppState.Complete;
                instructionText.text = "모든 운동을 완료했습니다!";
                progressText.text = "수고하셨습니다.\n5초 후 시작 화면으로 돌아갑니다.";
                Debug.Log("ENTIRE SEQUENCE COMPLETE!");

                OnSequenceCompleted.Invoke();
            }
        }

        private void ReleaseActiveDetector()
        {
            if (activeDetector == null) return;

            activeDetector.OnGestureDetected.RemoveListener(OnRepetition);
            activeDetector.EndDetection();
            activeDetector = null;
        }

        // This method is called by the active detector's OnGestureDetected event
        private void OnRepetition()
        {
            if (currentState != AppState.InProgress ||
                activeDetector == null ||
                !activeDetector.IsDetecting ||
                currentExerciseIndex < 0 ||
                currentExerciseIndex >= exerciseSequence.Count)
            {
                return;
            }

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
            if (currentState == AppState.InProgress && currentExerciseIndex < exerciseSequence.Count)
            {
                ExerciseStep currentStep = exerciseSequence[currentExerciseIndex];
                if (instructionText != null) instructionText.text = currentStep.instruction;
                if (progressText != null) progressText.text = $"{currentRep} / {currentStep.repetitions}";
            }
        }

        private void UpdateProgressIndicator()
        {
            if (progressIndicatorText == null) return;

            if (currentState == AppState.InProgress && currentExerciseIndex < exerciseSequence.Count)
            {
                // Add 1 to the index to show "1 / 3" instead of "0 / 3"
                progressIndicatorText.text = $"운동 {currentExerciseIndex + 1} / {exerciseSequence.Count}";
            }
            else
            {
                // Clear the indicator text when the sequence is over or not started
                progressIndicatorText.text = "";
            }
        }

        public void ResetSequence()
        {
            ReleaseActiveDetector();

            // Reset all state variables
            currentState = AppState.Welcome; // Or a new 'Idle' state if you prefer
            currentExerciseIndex = -1;
            currentRep = 0;
            activeDetector = null;

            // Clear the UI
            instructionText.text = "";
            progressText.text = "";
            if (progressIndicatorText != null) progressIndicatorText.text = "";

            Debug.Log("Exercise sequence has been reset.");
        }

        private void OnDestroy()
        {
            ReleaseActiveDetector();
        }
    }
}