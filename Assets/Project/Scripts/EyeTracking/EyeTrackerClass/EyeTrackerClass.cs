using UnityEngine;

public abstract class EyeTrackerClass : MonoBehaviour
{
    [HideInInspector] 
    public Vector3 origin; 
    public Vector3 direction; 
    public bool isGazeValid;
}
