Shader "ScienceFlashcards/Sun"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0.28, 0.01, 1)
        _HotColor ("Hot Color", Color) = (1, 0.95, 0.28, 1)
        _DarkColor ("Dark Color", Color) = (0.5, 0.035, 0.005, 1)
        _DetailScale ("Detail Scale", Float) = 7
        _DetailSpeed ("Detail Speed", Float) = 0.08
        _GlowStrength ("Glow Strength", Range(0, 1.5)) = 0.75
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _HotColor;
                half4 _DarkColor;
                float _DetailScale;
                float _DetailSpeed;
                float _GlowStrength;
            CBUFFER_END

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            float Noise(float2 value)
            {
                float2 cell = floor(value);
                float2 blend = frac(value);
                blend = blend * blend * (3.0 - 2.0 * blend);

                return lerp(
                    lerp(Hash21(cell), Hash21(cell + float2(1, 0)), blend.x),
                    lerp(Hash21(cell + float2(0, 1)), Hash21(cell + float2(1, 1)), blend.x),
                    blend.y);
            }

            float FractalNoise(float2 value)
            {
                return Noise(value) * 0.55
                    + Noise(value * 2.07) * 0.3
                    + Noise(value * 4.13) * 0.15;
            }

            half4 SolarColor(float2 uv)
            {
                float time = _Time.y * _DetailSpeed;
                float2 surfaceUv = float2(uv.x * 2.0 + time, uv.y) * _DetailScale;
                float plasma = FractalNoise(surfaceUv);
                float filaments = FractalNoise(surfaceUv * 2.3 - float2(time * 1.7, time * 0.4));
                float energy = saturate(plasma * 0.75 + filaments * 0.45);

                half3 color = lerp(_DarkColor.rgb, _BaseColor.rgb, smoothstep(0.08, 0.72, energy));
                color = lerp(color, _HotColor.rgb, smoothstep(0.58, 1.0, energy));
                color *= 1.0 + _GlowStrength * smoothstep(0.52, 1.0, energy);
                return half4(color, 1);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return SolarColor(input.uv);
            }
            ENDHLSL
        }
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
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            fixed4 _HotColor;
            fixed4 _DarkColor;
            float _DetailScale;
            float _DetailSpeed;
            float _GlowStrength;

            struct AppData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            float Noise(float2 value)
            {
                float2 cell = floor(value);
                float2 blend = frac(value);
                blend = blend * blend * (3.0 - 2.0 * blend);

                return lerp(
                    lerp(Hash21(cell), Hash21(cell + float2(1, 0)), blend.x),
                    lerp(Hash21(cell + float2(0, 1)), Hash21(cell + float2(1, 1)), blend.x),
                    blend.y);
            }

            float FractalNoise(float2 value)
            {
                return Noise(value) * 0.55
                    + Noise(value * 2.07) * 0.3
                    + Noise(value * 4.13) * 0.15;
            }

            fixed4 SolarColor(float2 uv)
            {
                float time = _Time.y * _DetailSpeed;
                float2 surfaceUv = float2(uv.x * 2.0 + time, uv.y) * _DetailScale;
                float plasma = FractalNoise(surfaceUv);
                float filaments = FractalNoise(surfaceUv * 2.3 - float2(time * 1.7, time * 0.4));
                float energy = saturate(plasma * 0.75 + filaments * 0.45);

                fixed3 color = lerp(_DarkColor.rgb, _BaseColor.rgb, smoothstep(0.08, 0.72, energy));
                color = lerp(color, _HotColor.rgb, smoothstep(0.58, 1.0, energy));
                color *= 1.0 + _GlowStrength * smoothstep(0.52, 1.0, energy);
                return fixed4(color, 1);
            }

            Varyings Vert(AppData input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                return SolarColor(input.uv);
            }
            ENDCG
        }
    }
}
