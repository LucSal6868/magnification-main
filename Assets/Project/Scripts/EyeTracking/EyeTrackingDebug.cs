using UnityEngine;

public class EyeTrackingDebug : MonoBehaviour
{
    public GameObject GameObject;
    public GazeController GazeController;

    void Update()
    {
        if (GameObject == null || GazeController == null)
            return;

        if (!GazeController.hasGazeHit)
        {
            return;
        }

        GameObject.transform.position = GazeController.gazeHit.point;
    }
}