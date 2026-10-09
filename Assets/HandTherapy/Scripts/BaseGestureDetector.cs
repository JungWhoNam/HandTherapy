using UnityEngine;
using UnityEngine.Events;

namespace HandTherapy.Gestures
{
    public abstract class BaseGestureDetector : MonoBehaviour
    {
        [Header("Base Event")]
        [Tooltip("This generic event is fired when the specific gesture is detected.")]
        public UnityEvent OnGestureDetected;

        public bool IsDetecting { get; private set; }

        public void BeginDetection()
        {
            ResetDetectionState();
            IsDetecting = true;
        }

        public void EndDetection()
        {
            IsDetecting = false;
            ResetDetectionState();
        }

        // Implement EvaluateGesture in derived detectors instead of Update.
        protected void Update()
        {
            if (!IsDetecting) return;

            EvaluateGesture();
        }

        protected void RaiseGestureDetected()
        {
            if (IsDetecting)
                OnGestureDetected.Invoke();
        }

        protected abstract void EvaluateGesture();
        public abstract void ResetDetectionState();
    }
}