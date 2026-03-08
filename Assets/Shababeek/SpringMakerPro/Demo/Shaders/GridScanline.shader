Shader "Shababeek/Demo/Grid Scanline"
{
    Properties
    {
        [Header(Grid)]
        _GridColor ("Grid Color", Color) = (0.3, 0.6, 1.0, 0.15)
        _GridSize ("Grid Cell Size", Float) = 1.0
        _GridThickness ("Grid Line Thickness", Range(0.001, 0.05)) = 0.015
        _SubGridAlpha ("Sub-Grid Alpha", Range(0, 1)) = 0.3
        _SubGridDivisions ("Sub-Grid Divisions", Float) = 4.0

        [Header(Radial Scanlines)]
        _ScanlineColor ("Scanline Color", Color) = (0.2, 0.8, 1.0, 0.08)
        _ScanlineSpacing ("Scanline Spacing", Range(0.5, 20)) = 2.0
        _ScanlineThickness ("Scanline Thickness", Range(0.001, 0.5)) = 0.08
        _ScanlineSpeed ("Scanline Expand Speed", Range(0, 5)) = 0.8

        [Header(Surface)]
        _BaseColor ("Base Color", Color) = (0.02, 0.02, 0.04, 1.0)
        _FadeRadius ("Edge Fade Radius", Float) = 15.0
        _FadeSoftness ("Edge Fade Softness", Range(0.1, 10)) = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "GridScanlinePass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
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
                float3 positionWS : TEXCOORD0;
                float  fogFactor  : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4  _GridColor;
                float  _GridSize;
                float  _GridThickness;
                float  _SubGridAlpha;
                float  _SubGridDivisions;
                half4  _ScanlineColor;
                float  _ScanlineSpacing;
                float  _ScanlineThickness;
                float  _ScanlineSpeed;
                half4  _BaseColor;
                float  _FadeRadius;
                float  _FadeSoftness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.fogFactor  = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            // Anti-aliased grid line using screen-space derivatives
            float gridLine(float coord, float thickness)
            {
                float d = fwidth(coord);
                float aa = d * 1.5;
                float half_t = thickness * 0.5;
                float dist = abs(frac(coord + 0.5) - 0.5);
                return 1.0 - smoothstep(half_t - aa, half_t + aa, dist);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 worldXZ = input.positionWS.xz;

                // ── Main grid ──
                float gx = gridLine(worldXZ.x / _GridSize, _GridThickness);
                float gz = gridLine(worldXZ.y / _GridSize, _GridThickness);
                float mainGrid = saturate(gx + gz);

                // ── Sub-grid ──
                float subSize = _GridSize / _SubGridDivisions;
                float sx = gridLine(worldXZ.x / subSize, _GridThickness * 0.6);
                float sz = gridLine(worldXZ.y / subSize, _GridThickness * 0.6);
                float subGrid = saturate(sx + sz) * _SubGridAlpha;

                float grid = saturate(mainGrid + subGrid);

                // ── Radial scanlines (expanding rings from origin) ──
                float radialDist = length(worldXZ);
                float scanCoord = radialDist / _ScanlineSpacing - _Time.y * _ScanlineSpeed;
                float scanDist = abs(frac(scanCoord + 0.5) - 0.5);
                float scanD = fwidth(scanCoord);
                float scanLine = 1.0 - smoothstep(_ScanlineThickness * 0.5 - scanD,
                                                   _ScanlineThickness * 0.5 + scanD,
                                                   scanDist);

                // ── Radial fade from origin ──
                float dist = length(worldXZ);
                float fade = 1.0 - smoothstep(_FadeRadius - _FadeSoftness, _FadeRadius, dist);

                // ── Composite ──
                half3 color = _BaseColor.rgb;
                color += _GridColor.rgb * grid * _GridColor.a * fade;
                color += _ScanlineColor.rgb * scanLine * _ScanlineColor.a * fade;

                color = MixFog(color, input.fogFactor);

                return half4(color, 1.0);
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
    Fallback "Universal Render Pipeline/Unlit"
}
