using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class DistortionController : MonoBehaviour
{
    [SerializeField] private DistortionData distortionData;

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Texture2D bakedCurve;

    private static readonly int WarpCurveProperty =
        Shader.PropertyToID("_WarpCurve");

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
        bakedCurve = distortionData.BakeCurve();
        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetTexture(
            WarpCurveProperty,
            bakedCurve
        );
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDestroy()
    {
        if (bakedCurve != null)
        {
            Destroy(bakedCurve);
        }
    }
}
