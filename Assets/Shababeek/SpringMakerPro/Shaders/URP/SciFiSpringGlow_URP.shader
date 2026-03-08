Shader "Shababeek/SciFi Spring Glow (URP)"
{
    Properties
    {
        [Header(Base Properties)]
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (0.2, 0.8, 1.0, 1.0)
        _Alpha ("Alpha", Range(0, 1)) = 1.0

        [Header(Glow Properties)]
        _GlowColor ("Glow Color", Color) = (0.0, 1.0, 1.0, 1.0)
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 2.0
        _GlowPower ("Glow Power", Range(0.1, 5)) = 1.5

        [Header(Animation)]
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.0
        _PulseIntensity ("Pulse Intensity", Range(0, 2)) = 0.5
        _ScrollSpeed ("Scroll Speed", Range(0, 10)) = 2.0

        [Header(Advanced)]
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 2.0
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 1.0
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        LOD 200

        // ─── Glow Pass (Additive) ───────────────────────────────────────
        Pass
        {
            Name "GlowPass"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                float3 normalWS   : TEXCOORD3;
                float  fresnel    : TEXCOORD4;
                float  fogFactor  : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half   _Alpha;
                half4  _GlowColor;
                half   _GlowIntensity;
                half   _GlowPower;
                half   _PulseSpeed;
                half   _PulseIntensity;
                half   _ScrollSpeed;
                half   _FresnelPower;
                half   _NoiseScale;
                half   _NoiseIntensity;
            CBUFFER_END

            // Simple hash noise
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Smooth value noise
            float smoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv         = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS  = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                output.fogFactor  = ComputeFogFactor(posInputs.positionCS.z);

                // Fresnel
                output.fresnel = 1.0 - saturate(dot(normalize(output.normalWS), output.viewDirWS));
                output.fresnel = pow(output.fresnel, _FresnelPower);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;

                // Pulsing effect
                float pulse = 1.0 + sin(time * _PulseSpeed) * _PulseIntensity;

                // Scrolling UVs
                float2 scrolledUV = input.uv + float2(time * _ScrollSpeed * 0.1, 0);

                // Main texture sample
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scrolledUV);

                // Noise for energy crackling
                float noiseValue = smoothNoise(scrolledUV * _NoiseScale + time * 0.5);
                noiseValue = lerp(1.0, noiseValue, _NoiseIntensity);

                // Glow from fresnel + pulse + noise
                float glow = input.fresnel * pulse * noiseValue;
                glow = pow(glow, _GlowPower);

                // Combine
                half4 finalColor = _Color * mainTex;
                finalColor.rgb += _GlowColor.rgb * glow * _GlowIntensity;
                finalColor.a *= _Alpha * glow;

                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);

                return finalColor;
            }
            ENDHLSL
        }

        // ─── Depth Pass (Subtle Alpha) ──────────────────────────────────
        Pass
        {
            Name "DepthPass"
            Tags { "LightMode" = "DepthOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half   _Alpha;
                half4  _GlowColor;
                half   _GlowIntensity;
                half   _GlowPower;
                half   _PulseSpeed;
                half   _PulseIntensity;
                half   _ScrollSpeed;
                half   _FresnelPower;
                half   _NoiseScale;
                half   _NoiseIntensity;
            CBUFFER_END

            Varyings vertDepth(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 fragDepth(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                col.a *= _Alpha * 0.3;
                return col;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
