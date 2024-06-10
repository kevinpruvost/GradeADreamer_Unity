using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

// Editor with button to start recording
[CustomEditor(typeof(GradeADreamer_Camera))]
public class GradeADreamer_CameraEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GradeADreamer_Camera myScript = (GradeADreamer_Camera)target;
        if (GUILayout.Button("Start Recording"))
        {
            myScript.StartRecording();
        }
    }
}

public class GradeADreamer_Camera : MonoBehaviour
{
    public Transform parent; // Parent object
    public float duration = 3f; // Duration of the rotation

    private RecorderController recorderController;

    void Start()
    {
        if (parent == null)
        {
            parent = transform.parent; // If parent is not set, use the object's parent
        }
        SetupRecorder();
    }

    public void StartRecording()
    {
        StartCoroutine(RotateAroundYAxis());
    }

    void SetupRecorder()
    {
        // Create a RecorderControllerSettings instance
        var recorderControllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();

        // Add a MovieRecorderSettings instance to it
        var movieRecorderSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieRecorderSettings.name = "MyVideo";
        movieRecorderSettings.Enabled = true;

        // Set the output file path and format
        movieRecorderSettings.OutputFile = $"{Application.dataPath}/../MyVideo.mp4"; // Save outside of Assets folder
        movieRecorderSettings.VideoBitRateMode = VideoBitrateMode.High;
        movieRecorderSettings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 3840,
            OutputHeight = 2160,
            FlipFinalOutput = false
        };

        // Set the frame rate and duration
        movieRecorderSettings.CapFrameRate = true;
        movieRecorderSettings.FrameRatePlayback = FrameRatePlayback.Variable;
        movieRecorderSettings.FrameRate = 30.0f;
        recorderControllerSettings.AddRecorderSettings(movieRecorderSettings);
        recorderControllerSettings.SetRecordModeToManual();

        // Create a RecorderController and set its settings
        recorderController = new RecorderController(recorderControllerSettings);
    }

    IEnumerator RotateAroundYAxis()
    {
        // Start recording
        recorderController.PrepareRecording();
        recorderController.StartRecording();

        Vector3 rotationAxis = Vector3.up; // Y-axis
        float childRotationSpeed = 360f / duration * 0.0f; // Child rotates at 3/4 the speed
        float parentRotationSpeed = 360f / duration * 1.00f; // Parent rotates at 1/4 the speed

        float elapsedTime = 0f;

        while (elapsedTime < duration) // Rotate for the duration of one revolution
        {
            // Rotate the child object
            float childAngle = childRotationSpeed * Time.deltaTime;
            transform.RotateAround(parent.position, rotationAxis, -childAngle);

            // Rotate the parent object
            float parentAngle = parentRotationSpeed * Time.deltaTime;
            parent.Rotate(rotationAxis, parentAngle);

            elapsedTime += Time.deltaTime;

            yield return null; // Wait for the next frame
        }

        // Stop recording after one revolution
        recorderController.StopRecording();
    }
}
