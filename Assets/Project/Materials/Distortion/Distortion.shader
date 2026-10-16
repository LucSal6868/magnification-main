Shader "Custom/Distortion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _WarpPosition ("Position", Vector) = (0.5, 0.5, 0, 0)

        _WarpSize ("Size", Float) = 0.3

        _WarpDepth ("Depth", Float) = 1.0

        _WarpCurve ("Curve", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            // Unity automatically supplies this for _MainTex.
            // x = 1 / width
            // y = 1 / height
            // z = width
            // w = height
            float4 _MainTex_TexelSize;

            sampler2D _WarpCurve;

            float4 _WarpPosition;
            float _WarpSize;
            float _WarpDepth;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // -----------------------------------------
                // Position relative to warp center
                // -----------------------------------------

                float2 offset =
                    i.uv - _WarpPosition.xy;

                // -----------------------------------------
                // Texture aspect ratio
                // -----------------------------------------

                float aspect =
                    _MainTex_TexelSize.z /
                    _MainTex_TexelSize.w;

                // Correct X so the distance calculation
                // happens in a circular coordinate system.
                float2 correctedOffset =
                    offset;

                correctedOffset.x *= aspect;

                // -----------------------------------------
                // Normalized distance from center
                //
                // 0 = center
                // 1 = edge of warp
                // -----------------------------------------

                float radius =
                    length(correctedOffset) / _WarpSize;

                // -----------------------------------------
                // Outside warp
                // -----------------------------------------

                if (radius >= 1.0)
                {
                    return tex2D(_MainTex, i.uv);
                }

                // -----------------------------------------
                // Read AnimationCurve
                //
                // X = distance from center
                // Y = curve value
                // -----------------------------------------

                float curveValue =
                    tex2D(
                        _WarpCurve,
                        float2(radius, 0.5)
                    ).r;

                // -----------------------------------------
                // Apply depth
                // -----------------------------------------

                float distortion =
                    curveValue * _WarpDepth;

                // -----------------------------------------
                // Apply radial warp
                // -----------------------------------------

                float2 distortedOffset =
                    correctedOffset *
                    (1.0 - distortion);

                // -----------------------------------------
                // Convert back to UV space
                // -----------------------------------------

                distortedOffset.x /= aspect;

                float2 distortedUV =
                    _WarpPosition.xy +
                    distortedOffset;

                // -----------------------------------------
                // Sample texture
                // -----------------------------------------

                float4 color =
                    tex2D(_MainTex, distortedUV);

                return color;
            }

            ENDHLSL
        }
    }
}
