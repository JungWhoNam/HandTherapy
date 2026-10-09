using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using HandTherapy.Gestures;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

namespace HandTherapy.Evaluation
{
    public enum LightingCondition { Normal, ScreenOnly }
    public enum RecordingStatus { Ready, Recording, Saved, SaveFailed }

    public class GestureTestRecorder : MonoBehaviour
    {
        [SerializeField] private string participant = "P01";
        [SerializeField, Min(1)] private int targetRepetitions = 8;
        [SerializeField] private LightingCondition lighting = LightingCondition.Normal;
        [SerializeField] private BaseGestureDetector detector;
        [SerializeField, Min(1)] private int trialNumber = 1;

        public RecordingStatus Status { get; private set; } = RecordingStatus.Ready;
        public bool IsRecording => Status == RecordingStatus.Recording;
        public bool HasUnsavedTrial => pendingTrial != null;
        public string StatusMessage { get; private set; } = "Ready to record.";
        public int TargetRepetitions => targetRepetitions;
        public int RecordingTargetRepetitions => activeTrial == null ? targetRepetitions : activeTrial.TargetRepetitions;
        public int LastTargetRepetitions { get; private set; }
        public int LastDetectedCount { get; private set; }
        public double LastDurationSeconds { get; private set; }
        public string LastOutcome { get; private set; }
        public string ResultsDirectory => Path.Combine(Application.persistentDataPath, "GestureEvaluation");
        public double ElapsedSeconds => timer == null ? 0 : timer.Elapsed.TotalSeconds;

        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private static readonly Encoding CsvEncoding = new UTF8Encoding(true);
        private const string SummaryHeader = "participant,lighting,gesture,trial_number,target_repetitions,detected_count,duration_seconds";
        private BaseGestureDetector activeDetector;
        private Stopwatch timer;
        private Trial activeTrial;
        private Trial pendingTrial;

        private sealed class Trial
        {
            public string Participant, Lighting, Gesture;
            public int Number, TargetRepetitions;
            public double Duration;
            public int DetectedCount;
            public bool Saved;
        }

        public bool CanStartTrial(out string reason)
        {
            if (!Application.isPlaying) reason = "Enter Play Mode to record.";
            else if (!isActiveAndEnabled) reason = "Enable this recorder and its GameObject.";
            else if (IsRecording) reason = "A trial is already recording.";
            else if (HasUnsavedTrial) reason = "Save the pending trial before starting another.";
            else if (detector == null) reason = "Assign a gesture detector.";
            else if (!detector.isActiveAndEnabled) reason = "Enable the detector and its GameObject.";
            else if (detector.IsDetecting) reason = "The detector is already running. Use a scene without AppManager or ExerciseSequencer.";
            else if (string.IsNullOrWhiteSpace(participant)) reason = "Enter a participant ID.";
            else if (targetRepetitions < 1) reason = "Target repetitions must be at least 1.";
            else if (trialNumber < 1) reason = "Trial number must be at least 1.";
            else { reason = null; return true; }
            return false;
        }

        public void StartTrial()
        {
            if (!CanStartTrial(out string reason))
            {
                Debug.LogWarning(reason, this);
                return;
            }

            // Capture settings once so later Inspector edits cannot relabel a trial.
            activeTrial = new Trial
            {
                Participant = participant.Trim(),
                Lighting = lighting.ToString(),
                Gesture = detector.GetType().Name,
                Number = trialNumber,
                TargetRepetitions = targetRepetitions
            };

            activeDetector = detector;
            if (activeDetector.OnGestureDetected == null)
                activeDetector.OnGestureDetected = new UnityEvent();

            timer = Stopwatch.StartNew();
            Status = RecordingStatus.Recording;
            StatusMessage = $"Recording. Perform {activeTrial.TargetRepetitions} repetitions, then finish manually.";
            activeDetector.OnGestureDetected.AddListener(OnGestureDetected);
            activeDetector.BeginDetection();
        }

        private void OnGestureDetected()
        {
            if (IsRecording)
                activeTrial.DetectedCount++;
        }

        // The operator confirms that the captured target number of real repetitions was performed.
        public void FinishTrial()
        {
            StopTrial("Completed");
        }

        public void CancelTrial()
        {
            StopTrial("Aborted");
        }

        private void StopTrial(string outcome)
        {
            if (!IsRecording) return;

            timer.Stop();
            Status = RecordingStatus.Ready;

            if (activeDetector != null)
            {
                activeDetector.OnGestureDetected.RemoveListener(OnGestureDetected);
                activeDetector.EndDetection();
                activeDetector = null;
            }

            activeTrial.Duration = timer.Elapsed.TotalSeconds;
            LastTargetRepetitions = activeTrial.TargetRepetitions;
            LastDetectedCount = activeTrial.DetectedCount;
            LastDurationSeconds = activeTrial.Duration;
            LastOutcome = outcome;
            if (outcome != "Completed")
            {
                activeTrial = null;
                StatusMessage = "Trial cancelled. No data was saved.";
                return;
            }

            pendingTrial = activeTrial;
            activeTrial = null;
            RetrySave();
        }

        public void RetrySave()
        {
            if (pendingTrial == null || IsRecording) return;

            try
            {
                SaveTrial(pendingTrial);
                pendingTrial = null;
                Status = RecordingStatus.Saved;
                StatusMessage = $"Completed trial saved. Actual repetitions: {LastTargetRepetitions} (operator-confirmed).";
            }
            catch (Exception exception)
            {
                Status = RecordingStatus.SaveFailed;
                StatusMessage = "Save failed. Trial remains in memory; retry before leaving Play Mode.";
                Debug.LogError($"{StatusMessage}\n{exception.Message}", this);
            }
        }

        private void SaveTrial(Trial trial)
        {
            if (trial.Saved) return;

            Directory.CreateDirectory(ResultsDirectory);
            string summaryPath = Path.Combine(ResultsDirectory, "trials.csv");
            bool hasSummary = File.Exists(summaryPath) && new FileInfo(summaryPath).Length > 0;
            string existingCsv = "";

            if (hasSummary)
            {
                existingCsv = File.ReadAllText(summaryPath, CsvEncoding);
                using (var reader = new StringReader(existingCsv))
                {
                    if (reader.ReadLine() != SummaryHeader)
                        throw new IOException("Existing trials.csv has a different header. Move it before recording.");
                }
            }

            string[] fields =
            {
                trial.Participant, trial.Lighting, trial.Gesture,
                trial.Number.ToString(Invariant), trial.TargetRepetitions.ToString(Invariant),
                trial.DetectedCount.ToString(Invariant), trial.Duration.ToString("F6", Invariant)
            };
            for (int i = 0; i < fields.Length; i++) fields[i] = CsvField(fields[i]);

            var csv = new StringBuilder();
            if (hasSummary)
            {
                csv.Append(existingCsv);
                if (!existingCsv.EndsWith("\n", StringComparison.Ordinal)) csv.AppendLine();
            }
            else
            {
                csv.AppendLine(SummaryHeader);
            }
            csv.AppendLine(string.Join(",", fields));

            // Replace the file only after writing succeeds, so a failed save can be retried.
            string temporaryPath = Path.Combine(ResultsDirectory, Path.GetRandomFileName());
            try
            {
                File.WriteAllText(temporaryPath, csv.ToString(), CsvEncoding);
                if (File.Exists(summaryPath)) File.Replace(temporaryPath, summaryPath, null);
                else File.Move(temporaryPath, summaryPath);
                trial.Saved = true;
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        private static string CsvField(string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private void OnDisable()
        {
            if (IsRecording) CancelTrial();
        }
    }
}
