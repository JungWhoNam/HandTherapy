using HandTherapy.Gestures;
using UnityEngine;

namespace HandTherapy
{
    public class AppManager : MonoBehaviour
    {
        [Header("Connections")]
        [Tooltip("The ExerciseSequencer that runs the main routine.")]
        public ExerciseSequencer exerciseSequencer;

        [Header("Settings")]
        [Tooltip("Time in seconds with no hands visible during an exercise before resetting.")]
        public float inactivityResetTime = 5.0f;
        [Tooltip("Time in seconds after completion before automatically resetting to the welcome screen.")]
        public float completionResetTime = 5.0f;

        private enum AppState { Welcome, InProgress, Complete }
        private AppState currentState = AppState.Welcome;

        private float inactivityTimer = 0f;
        private float completionTimer = 0f;

        void Start()
        {
            currentState = AppState.Welcome;
            exerciseSequencer.welcomeScreenUI.SetActive(true);
            exerciseSequencer.exerciseUI.SetActive(false);

            exerciseSequencer.OnSequenceCompleted.AddListener(OnSequenceComplete);
        }

        void Update()
        {
            switch (currentState)
            {
                case AppState.Welcome:
                    if (HandDataManager.Instance.LeftHand != null && HandDataManager.Instance.RightHand != null)
                    {
                        StartExperience();
                    }
                    break;

                case AppState.InProgress:
                    bool handsVisible = HandDataManager.Instance.LeftHand != null || HandDataManager.Instance.RightHand != null;

                    if (handsVisible)
                    {
                        inactivityTimer = 0f;
                    }
                    else
                    {
                        inactivityTimer += Time.deltaTime;
                        if (inactivityTimer >= inactivityResetTime)
                        {
                            ResetToWelcomeScreen("Inactivity detected");
                        }
                    }
                    break;

                case AppState.Complete:
                    completionTimer += Time.deltaTime;
                    if (completionTimer >= completionResetTime)
                    {
                        ResetToWelcomeScreen("Completion timeout");
                    }
                    break;
            }
        }

        private void StartExperience()
        {
            currentState = AppState.InProgress;
            inactivityTimer = 0f;
            exerciseSequencer.StartSequence();
        }

        private void OnSequenceComplete()
        {
            currentState = AppState.Complete;
            completionTimer = 0f;
        }

        private void ResetToWelcomeScreen(string reason)
        {
            if (currentState == AppState.Welcome) return;

            Debug.Log($"Resetting to welcome screen: {reason}");

            exerciseSequencer.ResetSequence();
            currentState = AppState.Welcome;
            inactivityTimer = 0f;
            completionTimer = 0f;

            exerciseSequencer.welcomeScreenUI.SetActive(true);
            exerciseSequencer.exerciseUI.SetActive(false);
        }

        private void OnDestroy()
        {
            if (exerciseSequencer != null)
            {
                exerciseSequencer.OnSequenceCompleted.RemoveListener(OnSequenceComplete);
            }
        }
    }
}