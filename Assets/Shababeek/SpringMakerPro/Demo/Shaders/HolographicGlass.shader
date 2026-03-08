Shader "Shababeek/Demo/Holographic Glass"
{
    Properties
    {
        [Header(Base)]
        _BaseColor ("Base Tint", Color) = (0.08, 0.12, 0.35, 0.15)
        _Alpha ("Base Alpha", Range(0, 1)) = 0.12

        [Header(Iridescence)]
        _IriColor1 ("Iri Color A (Rim)", Color) = (0.3, 0.45, 1.0, 1.0)
        _IriColor2 ("Iri Color B (Graze)", Color) = (0.7, 0.3, 1.0, 1.0)
        _IriColor3 ("Iri Color C (Accent)", Color) = (0.1, 0.9, 0.95, 1.0)
        _IriFresnelPower ("Iri Fresnel Power", Range(0.5, 8)) = 2.5
        _IriShift ("Iri Normal Shift", Range(0, 2)) = 0.6
        _IriIntensity ("Iri Intensity", Range(0, 5)) = 1.8

        [Header(Rim Glow)]
        _RimColor ("Rim Glow Color", Color) = (0.2, 0.5, 1.0, 1.0)
        _RimPower ("Rim Fresnel Power", Range(0.5, 8)) = 3.0
        _RimIntensity ("Rim Glow Intensity", Range(0, 5)) = 2.2

        [Header(Specular)]
        _SpecColor2 ("Specular Color", Color) = (0.8, 0.85, 1.0, 1.0)
        _SpecPower ("Specular Power", Range(4, 256)) = 80
        _SpecIntensity ("Specular Intensity", Range(0, 5)) = 1.5

        [Header(Reflection Fake)]
        _ReflColor ("Environment Tint", Color) = (0.15, 0.25, 0.6, 1.0)
        _ReflIntensity ("Reflection Intensity", Range(0, 3)) = 0.6
        _ReflFresnelPower ("Reflection Fresnel", Range(0.5, 6)) = 2.0

        [Header(Inner Glow)]
        _InnerColor ("Inner Glow Color", Color) = (0.1, 0.2, 0.8, 1.0)
        _InnerIntensity ("Inner Glow Intensity", Range(0, 3)) = 0.4
        _InnerFalloff ("Inner Glow Falloff", Range(0.5, 5)) = 1.5

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
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
        Cull [_Cull]

        // ─── Back faces pass (darker inner shell) ───────────────────
        Pass
        {
            Name "BackFaces"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vertBack
            #pragma fragment fragBack
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                half   _Alpha;
                half4  _IriColor1;
                half4  _IriColor2;
                half4  _IriColor3;
                half   _IriFresnelPower;
                half   _IriShift;
                half   _IriIntensity;
                half4  _RimColor;
                half   _RimPower;
                half   _RimIntensity;
                half4  _SpecColor2;
                half   _SpecPower;
                half   _SpecIntensity;
                half4  _ReflColor;
                half   _ReflIntensity;
                half   _ReflFresnelPower;
                half4  _InnerColor;
                half   _InnerIntensity;
                half   _InnerFalloff;
            CBUFFER_END

            Varyings vertBack(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                // Flip normal for back faces
                output.normalWS   = -TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS  = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                return output;
            }

            half4 fragBack(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                half3 v = normalize(input.viewDirWS);
                half ndotv = saturate(dot(n, v));

                // Inner glow — brighter toward edges
                half inner = pow(1.0 - ndotv, _InnerFalloff) * _InnerIntensity;
                half3 color = _InnerColor.rgb * inner;

                return half4(color, _Alpha * 0.5 + inner * 0.3);
            }
            ENDHLSL
        }

        // ─── Front faces pass (main holographic glass) ──────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                float2 uv          : TEXCOORD3;
                float  fogFactor   : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                half   _Alpha;
                half4  _IriColor1;
                half4  _IriColor2;
                half4  _IriColor3;
                half   _IriFresnelPower;
                half   _IriShift;
                half   _IriIntensity;
                half4  _RimColor;
                half   _RimPower;
                half   _RimIntensity;
                half4  _SpecColor2;
                half   _SpecPower;
                half   _SpecIntensity;
                half4  _ReflColor;
                half   _ReflIntensity;
                half   _ReflFresnelPower;
                half4  _InnerColor;
                half   _InnerIntensity;
                half   _InnerFalloff;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS  = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                output.uv         = input.uv;
                output.fogFactor  = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            // Three-color iridescence based on fresnel + normal direction
            half3 Iridescence(half ndotv, half3 normal)
            {
                half fresnel = pow(1.0h - ndotv, _IriFresnelPower);

                // Shift based on normal direction to vary color across the surface
                half shift = normal.y * _IriShift + normal.x * _IriShift * 0.5h;
                shift = frac(shift * 0.5h + 0.5h);

                // Blend three colors based on shift
                half3 color;
                if (shift < 0.33h)
                {
                    half t = shift / 0.33h;
                    color = lerp(_IriColor1.rgb, _IriColor2.rgb, t);
                }
                else if (shift < 0.66h)
                {
                    half t = (shift - 0.33h) / 0.33h;
                    color = lerp(_IriColor2.rgb, _IriColor3.rgb, t);
                }
                else
                {
                    half t = (shift - 0.66h) / 0.34h;
                    color = lerp(_IriColor3.rgb, _IriColor1.rgb, t);
                }

                return color * fresnel * _IriIntensity;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                half3 v = normalize(input.viewDirWS);
                half ndotv = saturate(dot(n, v));

                // ── Base glass tint ──
                half3 color = _BaseColor.rgb;

                // ── Iridescence ──
                color += Iridescence(ndotv, n);

                // ── Rim glow ──
                half rim = pow(1.0h - ndotv, _RimPower);
                color += _RimColor.rgb * rim * _RimIntensity;

                // ── Specular (Blinn-Phong from main light) ──
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half3 halfDir  = normalize(v + lightDir);
                half  ndoth    = saturate(dot(n, halfDir));
                half  spec     = pow(ndoth, _SpecPower) * _SpecIntensity;
                color += _SpecColor2.rgb * spec * mainLight.color;

                // Secondary specular from a fake overhead fill light
                half3 fillDir  = normalize(half3(0.3h, 1.0h, -0.2h));
                half3 halfFill = normalize(v + fillDir);
                half  specFill = pow(saturate(dot(n, halfFill)), _SpecPower * 0.5h) * _SpecIntensity * 0.4h;
                color += _SpecColor2.rgb * specFill;

                // ── Fake environment reflection ──
                half3 reflDir = reflect(-v, n);
                half  reflUp  = reflDir.y * 0.5h + 0.5h;
                half3 fakeEnv = lerp(_ReflColor.rgb * 0.3h, _ReflColor.rgb, reflUp);
                half  reflFresnel = pow(1.0h - ndotv, _ReflFresnelPower);
                color += fakeEnv * reflFresnel * _ReflIntensity;

                // ── Alpha: more transparent when facing, more visible at edges ──
                half alpha = _Alpha + rim * 0.6h + spec * 0.3h;
                alpha = saturate(alpha);

                color = MixFog(color, input.fogFactor);

                return half4(color, alpha);
            }
            ENDHLSL
        }

        // ─── Additive glow overlay pass ─────────────────────────────
        Pass
        {
            Name "GlowOverlay"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Back
            Blend One One
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vertGlow
            #pragma fragment fragGlow
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                half   _Alpha;
                half4  _IriColor1;
                half4  _IriColor2;
                half4  _IriColor3;
                half   _IriFresnelPower;
                half   _IriShift;
                half   _IriIntensity;
                half4  _RimColor;
                half   _RimPower;
                half   _RimIntensity;
                half4  _SpecColor2;
                half   _SpecPower;
                half   _SpecIntensity;
                half4  _ReflColor;
                half   _ReflIntensity;
                half   _ReflFresnelPower;
                half4  _InnerColor;
                half   _InnerIntensity;
                half   _InnerFalloff;
            CBUFFER_END

            Varyings vertGlow(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS  = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                return output;
            }

            half4 fragGlow(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                half3 v = normalize(input.viewDirWS);
                half ndotv = saturate(dot(n, v));

                // Soft additive rim glow for bloom-like halo
                half rim = pow(1.0h - ndotv, _RimPower + 1.0h);
                half3 glow = _RimColor.rgb * rim * _RimIntensity * 0.3h;

                // Subtle accent shimmer
                half shimmer = pow(1.0h - ndotv, _IriFresnelPower + 2.0h);
                glow += _IriColor3.rgb * shimmer * 0.15h;

                return half4(glow, 1.0);
            }
            ENDHLSL
        }

        // ─── Depth Only ─────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

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

            half4 fragDepth(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
