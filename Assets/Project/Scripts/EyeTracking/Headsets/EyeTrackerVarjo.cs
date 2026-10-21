using Unity.XR.CoreUtils;
using UnityEngine;
using Varjo.XR;

public class EyeTrackerVarjo : EyeTrackerClass
{
    [SerializeField] private Camera xrCamera;
    [SerializeField] private bool requestCalibration = true;
    [SerializeField] private VarjoEyeTracking.GazeCalibrationMode calibrationMode = VarjoEyeTracking.GazeCalibrationMode.Fast;

    void Start()
    {
        if (requestCalibration)
        {
            VarjoEyeTracking.RequestGazeCalibration(calibrationMode);
        }
        Debug.Log(VarjoEyeTracking.GetGazeCalibrationQuality());
    }

    void Update()
    {
        isGazeValid = false;
        var gazeData = VarjoEyeTracking.GetGaze();

        if ((int)gazeData.status != 2)
        {
            // 0 - Data unavailable: User is not wearing the headset or eyes cannot be located
            // 1 - User is wearing the headset, but gaze tracking is being calibrated
            // 2 - Data is valid
            return;
        }

        if (gazeData.leftStatus != VarjoEyeTracking.GazeEyeStatus.Tracked || 
            gazeData.rightStatus != VarjoEyeTracking.GazeEyeStatus.Tracked)
        {
            // 0 - Eye is not tracked and not visible (e.g., the eye is shut)
            // 1 - Eye is visible but not reliably tracked (e.g., during a saccade or blink)
            // 2 - Eye is tracked but quality is compromised (e.g., the headset has moved after calibration)
            // 3 - Eye is tracked
            //VarjoEyeTracking.RequestGazeCalibration(VarjoEyeTracking.GazeCalibrationMode.Fast);    
            return;
        }

        
        origin = xrCamera.transform.TransformPoint(
            new Vector3(
                gazeData.gaze.origin.x,
                gazeData.gaze.origin.y,
                gazeData.gaze.origin.z
            )
        );

        direction = xrCamera.transform.TransformDirection(
            new Vector3(
                gazeData.gaze.forward.x,
                gazeData.gaze.forward.y,
                gazeData.gaze.forward.z
            )
        ).normalized;

        isGazeValid = true;
    }
}
