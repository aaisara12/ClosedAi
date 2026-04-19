Shader "Custom/Explosion"
{
    Properties
    {
        [Header(Timing)]
        _Age          ("Age (seconds since spawn)",   Float) = 0.0
        _Duration     ("Total Duration (seconds)",    Float) = 1.2

        [Header(Shape)]
        _FresnelPower ("Fresnel Edge Power",          Float) = 1.5
        _EdgeSoftness ("Edge Softness",               Float) = 0.25

        [Header(Colour)]
        _CoreColor    ("Core Color",  Color) = (1.0, 1.0, 0.85, 1.0)
        _MidColor     ("Mid Color",   Color) = (1.0, 0.35, 0.02, 1.0)
        _OuterColor   ("Outer Color", Color) = (0.15, 0.05, 0.0,  1.0)
        _EmissiveBoost("Emissive Boost", Float) = 3.0

        [Header(Noise)]
        _NoiseTiling  ("Noise Tiling",               Float) = 3.0
        _NoiseStrength("Noise Colour Variation",     Float) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent+1"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ExplosionForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One   // Additive — bright areas light up the scene
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── Properties ──────────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float  _Age;
                float  _Duration;
                float  _FresnelPower;
                float  _EdgeSoftness;
                float4 _CoreColor;
                float4 _MidColor;
                float4 _OuterColor;
                float  _EmissiveBoost;
                float  _NoiseTiling;
                float  _NoiseStrength;
            CBUFFER_END

            // ── Vertex I/O ───────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalOS    : TEXCOORD2;
            };

            // ── Hash-based FBM noise (no texture required) ───────────────────────
            float2 _Hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            float _ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = _Hash2(i              ).x;
                float b = _Hash2(i + float2(1,0)).x;
                float c = _Hash2(i + float2(0,1)).x;
                float d = _Hash2(i + float2(1,1)).x;

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // 3-octave FBM
            float _Fbm(float2 p)
            {
                float v = 0.0, amp = 0.5;
                UNITY_UNROLL
                for (int i = 0; i < 3; i++)
                {
                    v   += amp * _ValueNoise(p);
                    p   *= 2.1;
                    amp *= 0.5;
                }
                return v;
            }

            // ── Vertex shader ────────────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.normalOS    = IN.normalOS;
                return OUT;
            }

            // ── Fragment shader ──────────────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                // Normalised lifetime [0,1]
                float t = saturate(_Age / max(_Duration, 0.0001));

                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // ── Fresnel: camera-facing areas are brighter, rim is softer ─────
                float nDotV    = saturate(dot(normalWS, viewDirWS));
                float fresnel  = pow(nDotV, _FresnelPower);
                float edgeAlpha = smoothstep(0.0, _EdgeSoftness + 0.001, fresnel);

                // ── Noise on sphere surface using object-space normal ─────────────
                // Blend XZ and XY projections by |normalOS.y| to avoid pole pinching
                float2 noiseUV_XZ = IN.normalOS.xz * _NoiseTiling + float2(_Age * 0.5, _Age * 0.3);
                float2 noiseUV_XY = IN.normalOS.xy * _NoiseTiling + float2(_Age * 0.3, _Age * 0.6);
                float  blendY     = abs(IN.normalOS.y);
                float  noise      = lerp(_Fbm(noiseUV_XZ), _Fbm(noiseUV_XY), blendY);

                // ── Lifetime fade: dissolves out over the last ~40% of duration ───
                float lifeAlpha = 1.0 - smoothstep(0.6, 1.0, t);
                float alpha     = edgeAlpha * lifeAlpha;

                // ── Colour: noise + age drive the hot-to-cool gradient ────────────
                float colourT = saturate(noise * _NoiseStrength + t * 0.55);
                float3 colour = lerp(_CoreColor.rgb, _MidColor.rgb,  saturate(colourT * 2.0));
                colour        = lerp(colour,         _OuterColor.rgb, saturate(colourT * 2.0 - 1.0));

                // Camera-facing centre stays hottest, cools with age
                colour = lerp(colour, _CoreColor.rgb, fresnel * (1.0 - t) * 0.4);

                // Emissive boost dims as the explosion cools
                colour *= _EmissiveBoost * (1.0 - t * 0.75);

                return half4(colour * alpha, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}

