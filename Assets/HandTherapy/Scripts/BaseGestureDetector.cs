using UnityEngine;
using UnityEngine.Events;

namespace HandTherapy.Gestures
{
    public abstract class BaseGestureDetector : MonoBehaviour
    {
        [Header("Base Event")]
        [Tooltip("This generic event is fired when the specific gesture is detected.")]
        public UnityEvent OnGestureDetected;

        public abstract void ResetDetectionState();
    }
}