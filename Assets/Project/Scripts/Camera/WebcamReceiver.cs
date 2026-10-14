using UnityEngine;

public class WebcamReceiver : MonoBehaviour
{

    [SerializeField] private string cameraName = "OBS Virtual Camera";
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFPS = 30;
    private WebCamTexture videoTexture;

    void Start()
    {
        // Print all available camera devices.
        foreach (WebCamDevice device in WebCamTexture.devices)
        {
            Debug.Log("Camera found: " + device.name);
        }

        // Find the OBS virtual camera.
        foreach (WebCamDevice device in WebCamTexture.devices)
        {
            if (device.name.Contains(cameraName))
            {
                videoTexture = new WebCamTexture(
                    device.name, requestedWidth, requestedHeight, requestedFPS
                );

                videoTexture.Play();
                GetComponent<Renderer>().material.mainTexture =
                    videoTexture;

                return;
            }
        }

        Debug.LogError(
            "Webcam with name containing '" + cameraName + "' not found."
        );
    }

    void OnDestroy()
    {
        if (videoTexture != null && videoTexture.isPlaying)
        {
            videoTexture.Stop();
        }
    }
}