Shader "Shababeek/Spring Line (URP)"
{
    Properties
    {
        [Header(Color)]
        _ColorStart ("Start Color", Color) = (0.2, 0.6, 1.0, 1.0)
        _ColorEnd   ("End Color",   Color) = (0.0, 1.0, 0.8, 1.0)
        _Brightness ("Brightness", Range(0.5, 5)) = 1.5

        [Header(Edge Glow)]
        _EdgeColor     ("Edge Color", Color) = (0.3, 0.9, 1.0, 1.0)
        _EdgeWidth     ("Edge Width", Range(0.01, 0.5)) = 0.15
        _EdgeIntensity ("Edge Intensity", Range(0, 5)) = 2.0
        _CoreWidth     ("Core Width", Range(0.01, 0.5)) = 0.3

        [Header(Flow Animation)]
        _FlowSpeed    ("Flow Speed", Range(0, 10)) = 3.0
        _FlowScale    ("Flow Scale", Range(0.5, 20)) = 6.0
        _FlowIntensity("Flow Intensity", Range(0, 1)) = 0.4

        [Header(Pulse)]
        _PulseSpeed    ("Pulse Speed", Range(0, 5)) = 1.2
        _PulseIntensity("Pulse Intensity", Range(0, 1)) = 0.3

        [Header(Noise)]
        _NoiseScale    ("Noise Scale", Range(0.5, 20)) = 4.0
        _NoiseSpeed    ("Noise Speed", Range(0, 5)) = 1.0
        _NoiseIntensity("Noise Intensity", Range(0, 1)) = 0.2

        [Header(Rendering)]
        [Enum(Additive,1,Alpha,10)] _BlendDst ("Blend Mode", Float) = 1
        _Alpha ("Overall Alpha", Range(0, 1)) = 1.0
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

        LOD 100

        Pass
        {
            Name "SpringLine"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha [_BlendDst]
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
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 vertColor  : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4  _ColorStart;
                half4  _ColorEnd;
                half   _Brightness;
                half4  _EdgeColor;
                half   _EdgeWidth;
                half   _EdgeIntensity;
                half   _CoreWidth;
                half   _FlowSpeed;
                half   _FlowScale;
                half   _FlowIntensity;
                half   _PulseSpeed;
                half   _PulseIntensity;
                half   _NoiseScale;
                half   _NoiseSpeed;
                half   _NoiseIntensity;
                half   _Alpha;
            CBUFFER_END

            // ── Noise helpers ──────────────────────────────────────────────
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amp   = 0.5;
                for (int i = 0; i < 3; i++)
                {
                    value += amp * valueNoise(p);
                    p *= 2.0;
                    amp *= 0.5;
                }
                return value;
            }

            // ── Vertex ─────────────────────────────────────────────────────
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.uv         = input.uv;
                output.vertColor  = input.color;
                output.fogFactor  = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            // ── Fragment ───────────────────────────────────────────────────
            half4 frag(Varyings input) : SV_Target
            {
                float u = input.uv.x;   // along length (0 → 1)
                float v = input.uv.y;   // across width (0 → 1)
                float time = _Time.y;

                // ── Distance from center (0 at edges, 1 at center) ─────
                float distFromCenter = 1.0 - abs(v * 2.0 - 1.0);

                // ── Core shape: smooth bright core ─────────────────────
                float core = smoothstep(0.0, _CoreWidth, distFromCenter);

                // ── Edge glow: bright edges that taper inward ──────────
                float edgeMask = smoothstep(0.0, _EdgeWidth, distFromCenter)
                               * (1.0 - smoothstep(_EdgeWidth, _EdgeWidth + 0.1, distFromCenter));

                // ── Gradient color along length ────────────────────────
                half4 baseColor = lerp(_ColorStart, _ColorEnd, u);

                // ── Animated energy flow ───────────────────────────────
                float flowUV = u * _FlowScale - time * _FlowSpeed;
                float flow = sin(flowUV * 6.283) * 0.5 + 0.5;
                flow = lerp(1.0, flow, _FlowIntensity);

                // ── Pulse (global breathing) ───────────────────────────
                float pulse = 1.0 + sin(time * _PulseSpeed * 6.283) * _PulseIntensity;

                // ── Noise crackling ────────────────────────────────────
                float2 noiseUV = float2(u * _NoiseScale, v * 2.0) + time * _NoiseSpeed;
                float noiseMask = fbm(noiseUV);
                noiseMask = lerp(1.0, noiseMask, _NoiseIntensity);

                // ── Combine ────────────────────────────────────────────
                half3 coreColor = baseColor.rgb * _Brightness * core * flow * pulse * noiseMask;
                half3 edgeGlow  = _EdgeColor.rgb * _EdgeIntensity * edgeMask * pulse;
                half3 finalRGB  = coreColor + edgeGlow;

                // Alpha: strong in core, fades at edges, with overall control
                float alpha = saturate(core * 0.8 + edgeMask * 0.6) * flow * _Alpha * input.vertColor.a;

                finalRGB *= input.vertColor.rgb;
                finalRGB = MixFog(finalRGB, input.fogFactor);

                return half4(finalRGB, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vertDepth(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 fragDepth(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
