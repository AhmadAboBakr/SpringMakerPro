Shader "Shababeek/SciFi Spring Glow"
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
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        
        LOD 200
        
        // Main pass - additive blending for glow effect
        Pass
        {
            Name "GlowPass"
            Blend One One
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float3 normal : TEXCOORD3;
                float fresnel : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Alpha;
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _GlowPower;
            float _PulseSpeed;
            float _PulseIntensity;
            float _ScrollSpeed;
            float _FresnelPower;
            float _NoiseScale;
            float _NoiseIntensity;
            
            // Simple noise function
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // Smooth noise
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
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
                
                // Calculate fresnel
                o.fresnel = 1.0 - saturate(dot(normalize(o.normal), o.viewDir));
                o.fresnel = pow(o.fresnel, _FresnelPower);
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Time-based animations
                float time = _Time.y;
                
                // Pulsing effect
                float pulse = 1.0 + sin(time * _PulseSpeed) * _PulseIntensity;
                
                // UV scrolling for animated texture
                float2 scrolledUV = i.uv + float2(time * _ScrollSpeed * 0.1, 0);
                
                // Sample main texture
                fixed4 mainTex = tex2D(_MainTex, scrolledUV);
                
                // Add noise for energy effect
                float noiseValue = smoothNoise(scrolledUV * _NoiseScale + time * 0.5);
                noiseValue = lerp(1.0, noiseValue, _NoiseIntensity);
                
                // Calculate glow based on fresnel and distance from center
                float glow = i.fresnel * pulse * noiseValue;
                glow = pow(glow, _GlowPower);
                
                // Combine base color with glow
                fixed4 finalColor = _Color * mainTex;
                finalColor.rgb += _GlowColor.rgb * glow * _GlowIntensity;
                
                // Apply alpha
                finalColor.a *= _Alpha * glow;
                
                return finalColor;
            }
            ENDCG
        }
        
        // Second pass - standard rendering for depth
        Pass
        {
            Name "DepthPass"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off
            
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Alpha;
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col.a *= _Alpha * 0.3; // Subtle depth pass
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
