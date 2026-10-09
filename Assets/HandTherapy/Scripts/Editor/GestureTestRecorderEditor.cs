using System.IO;
using HandTherapy.Evaluation;
using UnityEditor;
using UnityEngine;

namespace HandTherapy.EditorTools
{
    [CustomEditor(typeof(GestureTestRecorder))]
    public class GestureTestRecorderEditor : Editor
    {
        private SerializedProperty participant, targetRepetitions, lighting, detector, trialNumber;

        private void OnEnable()
        {
            participant = serializedObject.FindProperty("participant");
            targetRepetitions = serializedObject.FindProperty("targetRepetitions");
            lighting = serializedObject.FindProperty("lighting");
            detector = serializedObject.FindProperty("detector");
            trialNumber = serializedObject.FindProperty("trialNumber");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var recorder = (GestureTestRecorder)target;

            EditorGUILayout.LabelField("Trial Settings", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(recorder.IsRecording || recorder.HasUnsavedTrial))
            {
                EditorGUILayout.PropertyField(participant);
                EditorGUILayout.PropertyField(lighting);
                EditorGUILayout.PropertyField(detector);
                EditorGUILayout.PropertyField(trialNumber);
                EditorGUILayout.PropertyField(targetRepetitions);
            }
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Play Mode only. Use a scene without AppManager or ExerciseSequencer. " +
                $"Finish only after {recorder.RecordingTargetRepetitions} actual repetitions; recording does not stop at the target detection count.",
                MessageType.Info);

            bool canStart = recorder.CanStartTrial(out string reason);
            using (new EditorGUI.DisabledScope(!canStart))
            {
                if (GUILayout.Button("Start Recording")) recorder.StartTrial();
            }
            if (!canStart && !recorder.IsRecording && !recorder.HasUnsavedTrial)
                EditorGUILayout.HelpBox(reason, MessageType.Info);

            using (new EditorGUI.DisabledScope(!recorder.IsRecording))
            {
                if (GUILayout.Button($"Finish - {recorder.RecordingTargetRepetitions} Actual Repetitions Completed")) recorder.FinishTrial();
                if (GUILayout.Button("Cancel Trial")) recorder.CancelTrial();
            }

            if (recorder.HasUnsavedTrial && GUILayout.Button("Retry Save")) recorder.RetrySave();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", recorder.Status.ToString());
            EditorGUILayout.HelpBox(recorder.StatusMessage,
                recorder.HasUnsavedTrial ? MessageType.Error : MessageType.Info);

            if (recorder.IsRecording)
            {
                EditorGUILayout.LabelField("Elapsed Seconds", recorder.ElapsedSeconds.ToString("F1"));
                EditorGUILayout.LabelField("Detected Count", "Hidden during recording");
                Repaint();
            }
            else if (!string.IsNullOrEmpty(recorder.LastOutcome))
            {
                EditorGUILayout.LabelField("Last Outcome", recorder.LastOutcome);
                EditorGUILayout.LabelField("Last Target Repetitions", recorder.LastTargetRepetitions.ToString());
                EditorGUILayout.LabelField("Last Detected Count", recorder.LastDetectedCount.ToString());
                EditorGUILayout.LabelField("Last Duration (s)", recorder.LastDurationSeconds.ToString("F2"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Results Folder");
            EditorGUILayout.SelectableLabel(recorder.ResultsDirectory, EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            using (new EditorGUI.DisabledScope(!Directory.Exists(recorder.ResultsDirectory)))
            {
                if (GUILayout.Button("Open Results Folder"))
                    EditorUtility.RevealInFinder(recorder.ResultsDirectory);
            }
        }
    }
}
