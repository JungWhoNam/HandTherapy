using System;
using System.Collections.Generic;
using System.IO;
using HandTherapy.Gestures;
using UnityEngine;

namespace HandTherapy
{
    [Serializable]
    public class GestureBinding
    {
        public string gestureId;
        public BaseGestureDetector detector;
    }

    // Loads once in Awake, before the sequencer and AppManager begin in Start.
    [RequireComponent(typeof(ExerciseSequencer))]
    public class JsonRoutineLoader : MonoBehaviour
    {
        [Tooltip("Leave empty to use the Inspector routine. Relative paths start in StreamingAssets; absolute local paths are also supported.")]
        public string routineFilePath;

        [Tooltip("Connect JSON gesture IDs to existing detector components in this scene.")]
        public List<GestureBinding> gestureBindings = new List<GestureBinding>();

        private readonly List<Sprite> loadedSprites = new List<Sprite>();
        private readonly List<Texture2D> loadedTextures = new List<Texture2D>();

        [Serializable]
        private class RoutineDefinition
        {
            public string name;
            public StepDefinition[] steps;
        }

        [Serializable]
        private class StepDefinition
        {
            public string gestureId;
            public string instruction;
            public int repetitions;
            public string image;
        }

        private void Awake()
        {
            if (!enabled || string.IsNullOrWhiteSpace(routineFilePath)) return;

            ExerciseSequencer sequencer = GetComponent<ExerciseSequencer>();
            try
            {
                string path = Path.IsPathRooted(routineFilePath)
                    ? routineFilePath
                    : Path.Combine(Application.streamingAssetsPath, routineFilePath);
                path = Path.GetFullPath(path);
                RoutineDefinition definition = JsonUtility.FromJson<RoutineDefinition>(File.ReadAllText(path));
                List<ExerciseStep> routine = BuildRoutine(definition, sequencer.exerciseSequence);

                // Validate every step before loading images or replacing the default routine.
                var images = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
                string directory = Path.GetDirectoryName(path);
                for (int i = 0; i < routine.Count; i++)
                {
                    string imagePath = definition.steps[i].image;
                    if (!string.IsNullOrWhiteSpace(imagePath))
                    {
                        Sprite image = LoadImage(directory, imagePath, images);
                        if (image != null) routine[i].illustration = image;
                    }
                }

                sequencer.exerciseSequence = routine;
                Debug.Log($"Loaded JSON routine '{definition.name}': {routine.Count} steps from {path}", this);
            }
            catch (Exception exception)
            {
                ReleaseImages();
                Debug.LogWarning($"Could not load JSON routine; keeping the Inspector routine. {exception.Message}", this);
            }
        }

        private List<ExerciseStep> BuildRoutine(RoutineDefinition definition, List<ExerciseStep> defaults)
        {
            if (definition == null || definition.steps == null || definition.steps.Length == 0)
                throw new InvalidDataException("The JSON routine must contain at least one step.");

            var detectors = new Dictionary<string, BaseGestureDetector>(StringComparer.Ordinal);
            if (gestureBindings != null)
            {
                foreach (GestureBinding binding in gestureBindings)
                {
                    if (binding == null || string.IsNullOrWhiteSpace(binding.gestureId) || binding.detector == null)
                        throw new InvalidDataException("Each gesture binding needs an ID and detector.");
                    if (detectors.ContainsKey(binding.gestureId))
                        throw new InvalidDataException($"Duplicate gesture ID: {binding.gestureId}");
                    detectors.Add(binding.gestureId, binding.detector);
                }
            }

            var routine = new List<ExerciseStep>();
            for (int i = 0; i < definition.steps.Length; i++)
            {
                StepDefinition step = definition.steps[i];
                if (step == null || string.IsNullOrWhiteSpace(step.gestureId) ||
                    !detectors.TryGetValue(step.gestureId, out BaseGestureDetector detector))
                    throw new InvalidDataException($"Step {i + 1} has an unknown or missing gestureId.");
                if (step.repetitions < 1)
                    throw new InvalidDataException($"Step {i + 1} must specify repetitions of at least 1.");

                // Reuse the first Inspector step for this detector as its presentation default.
                ExerciseStep fallback = defaults?.Find(candidate => candidate != null && candidate.detector == detector);
                routine.Add(new ExerciseStep
                {
                    detector = detector,
                    repetitions = step.repetitions,
                    instruction = step.instruction ?? fallback?.instruction ?? "",
                    illustration = fallback?.illustration
                });
            }
            return routine;
        }

        private Sprite LoadImage(string directory, string imagePath, Dictionary<string, Sprite> cache)
        {
            Texture2D texture = null;
            try
            {
                string path = Path.GetFullPath(Path.Combine(directory, imagePath));
                if (cache.TryGetValue(path, out Sprite cached)) return cached;

                texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), true))
                    throw new InvalidDataException("Use a valid PNG or JPG image.");

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                loadedTextures.Add(texture);
                loadedSprites.Add(sprite);
                cache.Add(path, sprite);
                return sprite;
            }
            catch (Exception exception)
            {
                if (texture != null) Destroy(texture);
                Debug.LogWarning($"Could not load routine image '{imagePath}'; using the default illustration. {exception.Message}", this);
                return null;
            }
        }

        private void ReleaseImages()
        {
            foreach (Sprite sprite in loadedSprites) Destroy(sprite);
            foreach (Texture2D texture in loadedTextures) Destroy(texture);
            loadedSprites.Clear();
            loadedTextures.Clear();
        }

        private void OnDestroy()
        {
            ReleaseImages();
        }
    }
}
