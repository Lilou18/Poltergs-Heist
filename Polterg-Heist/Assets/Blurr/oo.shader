Shader "Unlit/oo"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _BlurStrength ("Blur Strength", Range(0, 5)) = 1
        _Desaturation ("Desaturation", Range(0, 1)) = 1
        _GrayOverlay ("Gray Overlay (Tint)", Color) = (0.7, 0.7, 0.7, 1)
        _OverlayStrength ("Overlay Strength", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _BlurStrength;
            float _Desaturation;
            float4 _GrayOverlay;
            float _OverlayStrength;

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
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 texelSize = float2(_BlurStrength * 0.001, _BlurStrength * 0.001);

                float4 sum = float4(0, 0, 0, 0);

                // 3x3 blur kernel
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        float2 offset = float2(x, y) * texelSize;
                        sum += tex2D(_MainTex, uv + offset);
                    }
                }

                float4 blurred = sum / 9.0;

                // Désaturation
                float gray = dot(blurred.rgb, float3(0.3, 0.59, 0.11));
                float3 desaturated = lerp(blurred.rgb, gray.xxx, _Desaturation);

                // Overlay gris
                desaturated = lerp(desaturated, _GrayOverlay.rgb, _OverlayStrength);

                return float4(desaturated, blurred.a);
            }

            ENDHLSL
        }
    }
}
