using UnityEngine;

[CreateAssetMenu(fileName = "DistortionData", menuName = "Custom/DistortionData")]
public class DistortionData : ScriptableObject
{
    public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
    public Texture2D BakeCurve(int resolution = 512)
    {
        Texture2D texture = new Texture2D(
            resolution,
            1,
            TextureFormat.RFloat,
            false,
            true
        );

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < resolution; x++)
        {
            float t = 1f - x / (float)(resolution - 1);
            float value = curve.Evaluate(t);

            texture.SetPixel(x, 0, new Color(value, 0, 0, 1));
        }

        texture.Apply();

        return texture;
    }
}
