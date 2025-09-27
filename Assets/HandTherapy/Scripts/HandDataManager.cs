using Leap;
using UnityEngine;

namespace HandTherapy.Gestures
{
    public class HandDataManager : MonoBehaviour
    {
        public static HandDataManager Instance { get; private set; }

        public LeapProvider leapProvider;

        public Hand LeftHand { get; private set; }
        public Hand RightHand { get; private set; }

        private void Awake()
        {
            // Set up the singleton instance
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }

        private void OnEnable()
        {
            if (leapProvider == null)
            {
                leapProvider = FindFirstObjectByType<LeapProvider>();
            }
            leapProvider.OnUpdateFrame += OnUpdateFrame;
        }

        private void OnDisable()
        {
            leapProvider.OnUpdateFrame -= OnUpdateFrame;
        }

        void OnUpdateFrame(Frame frame)
        {
            LeftHand = frame.GetHand(Chirality.Left);
            RightHand = frame.GetHand(Chirality.Right);
        }
    }
}