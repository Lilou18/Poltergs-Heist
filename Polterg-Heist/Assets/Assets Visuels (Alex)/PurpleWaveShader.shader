Shader "Custom/PurpleWavesSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WaveColor ("Wave Color", Color) = (0.5, 0.0, 1.0, 1.0) // Default purple
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveAmplitude ("Wave Amplitude", Float) = 0.05
        _WaveFrequency ("Wave Frequency", Float) = 5.0
        [MaterialToggle] _EnableWaves ("Enable Waves", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _WaveColor;
            float _WaveSpeed;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _EnableWaves;

            v2f vert (appdata_t v)
            {
                v2f o;
                float waveOffset = sin((_Time.y * _WaveSpeed) + (v.uv.x + v.uv.y) * _WaveFrequency) * _WaveAmplitude * _EnableWaves;
                v.vertex.y += waveOffset;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                // Apply customizable wave color tint
                fixed3 waveTint = _WaveColor.rgb; // Use customizable wave color
                col.rgb = lerp(col.rgb, waveTint * col.a, 0.5);
                col.rgb *= col.a; // Premultiply alpha
                return col;
            }
            ENDCG
        }
    }
}