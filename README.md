# HandTherapy

A Unity application for contactless hand exercises using Ultraleap tracking. It guides users through a configurable routine with instructions, illustrations, repetition counts, and progress feedback.

Gesture detection is separate from routine progression, allowing developers to configure existing exercises or add new detectors.

## Features

- Four exercises: fingertip clapping, simultaneous hand clenching, alternating hand clenching, and alternating wrist bending.
- Inspector settings for detection thresholds and exercise routines.
- JSON configuration for exercise order, repetitions, instructions, and local images.
- Automatic start and reset for unattended operation.
- An evaluation scene for recording detection counts to CSV.

## Getting started

1. Clone the repository and open it in Unity **6.2 (`6000.2.2f1`)**.
2. Allow Unity to restore packages, including the **Ultraleap Unity package 7.2.0**. Install the separate Ultraleap tracking runtime and connect a compatible sensor.
3. Open `Assets/HandTherapy/Demo.unity`.
4. On the `Scripts` GameObject, leave **Json Routine Loader → Routine File Path** empty for the Inspector routine, or set it to `Routines_Example/routine.json` for the included example.
5. Enter Play Mode and place both hands over the sensor to start.

Place the sensor on the desk in front of the monitor and keep both hands visible to tracking. The internal evaluation used movements mainly about 25 cm above the sensor.

**Note:** The Demo scene currently points to `Routines/routine.json`; change this to the example path above or clear it.

## Inspector routines

In `ExerciseSequencer → Exercise Sequence`, arrange steps and set their **Instruction**, **Illustration**, **Repetitions**, and **Detector**. Leave the JSON path empty to use this routine.

Tune each detector's thresholds and timing in its Inspector. JSON settings configure routines; they do not change detection thresholds.

For alternating exercises, a detected transfer to the opposite side counts as one repetition, rather than a round trip. Fingertip clapping uses proximity and orientation as detection cues, rather than directly measuring contact.

## JSON routines

The included four-exercise routine is `Assets/StreamingAssets/Routines_Example/routine.json`. The following is a separate minimal example with an optional external image:

```json
{
  "name": "ShortRoutine",
  "steps": [
    {
      "gestureId": "fingertip_clap",
      "instruction": "양손 손끝으로 박수를 치세요.",
      "repetitions": 8,
      "image": "images/fingertip-clap.png"
    }
  ]
}
```

The `image` field is optional. If omitted, the routine uses the illustration assigned in the Unity Inspector to the first exercise step with the same detector. If that step has no illustration, no image is shown. To use an external image, supply your own PNG/JPG file at the specified path; the example image above is not included in the repository.

Connect IDs to detector components in `JsonRoutineLoader → Gesture Bindings`. The Demo scene defines:

| Gesture ID | Detector |
| --- | --- |
| `fingertip_clap` | `FingertipClapDetector` |
| `both_hands_clench` | `BothHandsClenchDetector` |
| `alternate_clench` | `AlternateClenchDetector` |
| `alternate_wrist_bend` | `AlternateWristBendDetector` |

- IDs are case-sensitive and unique. Every step needs a bound ID and a positive repetition count.
- Relative JSON paths start at `Assets/StreamingAssets` in the Editor. Absolute local paths are also supported.
- Relative image paths start at the JSON file's directory. Use local PNG/JPG files.
- Omitted instructions and illustrations use defaults from the first Inspector step with the same detector. An explicit empty instruction displays no text.
- An invalid routine retains the Inspector routine; an image loading failure retains the default illustration.

JSON is loaded once at startup. Restart the scene or application after editing it. Desktop routines can be changed between launches without rebuilding, provided their detectors and bindings already exist.

## Add a gesture detector

1. Derive a component from `HandTherapy.Gestures.BaseGestureDetector`.
2. Override `protected EvaluateGesture()` using `HandDataManager.Instance.LeftHand` and `RightHand`, handling missing hands. Use this method instead of defining a separate `Update()` loop.
3. Call `RaiseGestureDetected()` once per recognized repetition, with a rearming condition to avoid repeated events while holding a pose.
4. Override `public ResetDetectionState()` to clear state and timers.
5. Assign the component to a routine step or JSON binding.

The base class runs evaluation only during active detection and resets state when detection begins or ends. Keep counting, UI updates, and step transitions in `ExerciseSequencer`.

## Evaluation recording

Open `Assets/HandTherapy/Evaluation.unity` and select `GestureTestRecorder` in Play Mode. Run it independently of `AppManager` and `ExerciseSequencer`.

1. Set **Participant**, **Lighting**, **Detector**, **Trial Number**, and **Target Repetitions** (normally 8).
2. Click **Start Recording** and perform the target number of actual movements. The detection count is hidden during recording.
3. Click **Finish** after completing the movements; recording does not stop at the target detection count.
4. Click **Open Results Folder** to access the CSV.

Lighting selections are labels; set room lighting manually. **Cancel Trial** discards the recording. If saving fails, use **Retry Save** before leaving Play Mode.

Output: `Application.persistentDataPath/GestureEvaluation/trials.csv`.

```text
participant,lighting,gesture,trial_number,target_repetitions,detected_count,duration_seconds
```

`gesture` is the detector class name. Actual repetitions are confirmed by the operator; duration is the recording interval. Final counts alone do not separate missed and duplicate detections.

## Main components

| Component | Responsibility |
| --- | --- |
| `HandDataManager` | Hand tracking input |
| `BaseGestureDetector` and derived detectors | Gesture recognition and detection events |
| `ExerciseSequencer` | Exercise guidance, counting, and step progression |
| `AppManager` | Automatic start and reset |
| `JsonRoutineLoader` | External routine and image loading |
| `GestureTestRecorder` | Trial recording and CSV output |

Scripts are under `Assets/HandTherapy/Scripts`.
