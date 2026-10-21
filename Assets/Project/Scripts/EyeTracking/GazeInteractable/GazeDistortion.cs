using UnityEngine;

public class GazeDistortion : GazeInteractable
{
    private DistortionController distortion;
    private Renderer meshRenderer;

    void Awake()
    {
        distortion = GetComponent<DistortionController>();
        meshRenderer = GetComponent<Renderer>();
    }

    protected override void OnGazeHit(RaycastHit hitInfo)
    {
        if (distortion == null || meshRenderer == null)
            return;

        Vector3 localHit = transform.InverseTransformPoint(hitInfo.point);
        Bounds bounds = meshRenderer.localBounds;
        float u = Mathf.InverseLerp(
            bounds.min.x, bounds.max.x, localHit.x);
        float v = Mathf.InverseLerp(
            bounds.min.y, bounds.max.y, localHit.y);
        Vector2 gazeUV = new Vector2(u, v);

        distortion.SetWarpPosition(gazeUV);
    }
}