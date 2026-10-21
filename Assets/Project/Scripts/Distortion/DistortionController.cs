
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class DistortionController : MonoBehaviour
{
    [Header("Distortion Settings")]
    [SerializeField] private DistortionData distortionData;

    private Vector2 warpPosition = new Vector2(0.5f, 0.5f);

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Texture2D bakedCurve;

    private static readonly int WarpCurveProperty =
        Shader.PropertyToID("_WarpCurve");

    private static readonly int WarpPositionProperty =
        Shader.PropertyToID("_WarpPosition");

    private static readonly int WarpSizeProperty =
        Shader.PropertyToID("_WarpSize");

    private static readonly int WarpMagnitudeProperty =
        Shader.PropertyToID("_WarpMagnitude");

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();

        UpdateDistortion();
    }

    private void Update()
    {
        UpdateDistortion();
    }

    private void UpdateDistortion()
    {
        if (distortionData == null)
            return;

        if (bakedCurve == null)
        {
            bakedCurve = distortionData.BakeCurve();
        }

        meshRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetTexture(
            WarpCurveProperty,
            bakedCurve
        );

        propertyBlock.SetVector(
            WarpPositionProperty,
            new Vector4(
                warpPosition.x,
                warpPosition.y,
                0f,
                0f
            )
        );

        propertyBlock.SetFloat(
            WarpSizeProperty,
            distortionData.size
        );

        propertyBlock.SetFloat(
            WarpMagnitudeProperty,
            distortionData.magnitude
        );

        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    // Set the distortion center in UV coordinates (0-1).
    public void SetWarpPosition(Vector2 position)
    {
        warpPosition = position;
        UpdateDistortion();
    }

    private void OnDestroy()
    {
        if (bakedCurve != null)
        {
            Destroy(bakedCurve);
        }
    }
}
