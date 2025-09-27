using Leap;
using UnityEngine;
using UnityEngine.Events;

namespace HandTherapy.Gestures
{
    /// <summary>
    /// Detects a clap made by touching the fingertips of both hands together,
    /// ensuring that the palms are facing each other.
    /// </summary>
    public class FingertipClapDetector : MonoBehaviour
    {
        [Header("Tuning Thresholds")]
        [Tooltip("How close the index fingertips must be to register a clap (in meters).")]
        [Range(0.0f, 0.1f)]
        public float clapDistanceThreshold = 0.02f;

        [Tooltip("How far apart the index fingertips must be to allow the next clap to be detected.")]
        [Range(0.0f, 0.2f)]
        public float resetDistanceThreshold = 0.05f;

        [Header("Orientation")]
        [Tooltip("How opposite the palms must be facing. (-1 = perfectly opposite, 0 = perpendicular).")]
        [Range(-1.0f, 0.0f)]
        public float orientationThreshold = -0.7f;

        [Header("Event")]
        [Tooltip("Fired once per detected fingertip clap.")]
        public UnityEvent OnFingertipClap;

        private bool isReadyToClap = true;

        void Update()
        {
            Hand leftHand = HandDataManager.Instance.LeftHand;
            Hand rightHand = HandDataManager.Instance.RightHand;

            if (leftHand == null || rightHand == null)
            {
                isReadyToClap = true;
                return;
            }

            // --- Proximity Check ---
            Vector3 leftIndexTip = leftHand.Index.TipPosition;
            Vector3 rightIndexTip = rightHand.Index.TipPosition;
            float distance = Vector3.Distance(leftIndexTip, rightIndexTip);

            // --- Orientation Check ---
            Vector3 leftPalmNormal = leftHand.PalmNormal;
            Vector3 rightPalmNormal = rightHand.PalmNormal;
            float palmOrientationDotProduct = Vector3.Dot(leftPalmNormal, rightPalmNormal);

            // --- Detection Logic ---

            // A clap requires the hands to be close AND have the correct orientation.
            bool isClapConditionMet = distance < clapDistanceThreshold &&
                                      palmOrientationDotProduct < orientationThreshold;

            if (isClapConditionMet && isReadyToClap)
            {
                Debug.Log("FINGERTIP CLAP DETECTED!");
                OnFingertipClap.Invoke();
                isReadyToClap = false;
            }
            else if (distance > resetDistanceThreshold)
            {
                isReadyToClap = true;
            }
        }
    }
}