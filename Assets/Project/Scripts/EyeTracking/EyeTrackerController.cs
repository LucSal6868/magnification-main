using UnityEngine;

public class EyeTrackerController : MonoBehaviour
{
    private EyeTrackerClass eyeTracker;

    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private LayerMask gazeLayers = ~0;

    public RaycastHit gazeHit { get; private set; }
    public bool hasGazeHit { get; private set; }

    void Awake()
    {
        eyeTracker = GetComponent<EyeTrackerClass>();
    }

    void Update()
    {
        hasGazeHit = false;

        if (eyeTracker == null || !eyeTracker.isGazeValid)
            return;

        Ray gazeRay = new Ray(
            eyeTracker.origin,
            eyeTracker.direction
        );

        if (Physics.Raycast(
            gazeRay,
            out RaycastHit hit,
            maxDistance,
            gazeLayers))
        {
            gazeHit = hit;
            hasGazeHit = true;

            GazeInteractable interactable =
                hit.collider.GetComponent<GazeInteractable>();

            if (interactable != null)
            {
                interactable.SendMessage(
                    "OnGazeHit",
                    hit,
                    SendMessageOptions.DontRequireReceiver
                );
            }
        }
    }
}