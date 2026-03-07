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

        [Header(Scanlines)]
        _ScanlineColor ("Scanline Color", Color) = (0.2, 0.8, 1.0, 0.08)
        _ScanlineSpacing ("Scanline Spacing", Range(0.5, 20)) = 4.0
        _ScanlineThickness ("Scanline Thickness", Range(0.001, 0.5)) = 0.08
        _ScanlineSpeed ("Scanline Scroll Speed", Range(0, 5)) = 0.3

        [Header(Surface)]
        _BaseColor ("Base Color", Color) = (0.02, 0.02, 0.04, 1.0)
        _FadeRadius ("Edge Fade Radius", Float) = 15.0
        _FadeSoftness ("Edge Fade Softness", Range(0.1, 10)) = 3.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "GridScanlinePass"
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _GridColor;
            float _GridSize;
            float _GridThickness;
            float _SubGridAlpha;
            float _SubGridDivisions;

            fixed4 _ScanlineColor;
            float _ScanlineSpacing;
            float _ScanlineThickness;
            float _ScanlineSpeed;

            fixed4 _BaseColor;
            float _FadeRadius;
            float _FadeSoftness;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
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

            fixed4 frag(v2f i) : SV_Target
            {
                float2 worldXZ = i.worldPos.xz;

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

                // ── Scanlines (world-space Z, scrolling) ──
                float scanCoord = worldXZ.y / _ScanlineSpacing + _Time.y * _ScanlineSpeed;
                float scanDist = abs(frac(scanCoord + 0.5) - 0.5);
                float scanD = fwidth(scanCoord);
                float scanLine = 1.0 - smoothstep(_ScanlineThickness * 0.5 - scanD,
                                                   _ScanlineThickness * 0.5 + scanD,
                                                   scanDist);

                // ── Radial fade from origin ──
                float dist = length(worldXZ);
                float fade = 1.0 - smoothstep(_FadeRadius - _FadeSoftness, _FadeRadius, dist);

                // ── Composite ──
                fixed3 color = _BaseColor.rgb;
                color += _GridColor.rgb * grid * _GridColor.a * fade;
                color += _ScanlineColor.rgb * scanLine * _ScanlineColor.a * fade;

                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
