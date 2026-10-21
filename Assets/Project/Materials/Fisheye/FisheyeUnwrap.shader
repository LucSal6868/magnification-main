
Shader "Custom/FisheyeUnwrap"
{
    Properties
    {
        _MainTex ("180 Fisheye Image", 2D) = "white" {}
        _CenterX ("Fisheye Center X", Range(0, 1)) = 0.5
        _CenterY ("Fisheye Center Y", Range(0, 1)) = 0.5
        _Radius ("Fisheye Radius", Range(0.01, 0.5)) = 0.5
        _AngleOffset ("Angle Offset", Range(-180, 180)) = 0
        _FlipY ("Flip Source Y", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        ZWrite On
        ZTest LEqual

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _CenterX;
            float _CenterY;
            float _Radius;
            float _AngleOffset;
            float _FlipY;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            
        fixed4 frag(v2f i) : SV_Target
        {
            // Output X maps to angle: -90 to +90 degrees.
            float angle = (i.uv.x - 0.5) * UNITY_PI;

            // Output Y maps from fisheye center to rim.
            // Flip this if the image is upside down.
            float r = 1.0 - i.uv.y;

            // Polar to Cartesian.
            float2 dir = float2(sin(angle), cos(angle));

            // Convert circular-image coordinates to texture UVs.
            // _Radius is the radius in normalized texture UV units.
            float2 fishUV = float2(_CenterX, _CenterY);

            fishUV.x += dir.x * r * _Radius * 2.0;
            fishUV.y += dir.y * r * _Radius * 2.0;

            if (_FlipY > 0.5)
                fishUV.y = 1.0 - fishUV.y;

            // Prevent sampling outside the texture.
            if (fishUV.x < 0.0 || fishUV.x > 1.0 ||
                fishUV.y < 0.0 || fishUV.y > 1.0)
                return fixed4(0, 0, 0, 1);

            return tex2D(_MainTex, fishUV);
        }


            ENDCG
        }
    }
}
