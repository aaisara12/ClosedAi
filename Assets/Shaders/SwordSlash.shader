Shader "Custom/SwordSlash"
{
    Properties
    {
        [Header(Timing)]
        _Age          ("Age (seconds since spawn)",   Float) = 0.0
        _Duration     ("Total Duration (seconds)",    Float) = 0.35

        [Header(Sweep)]
        _SlashAngle   ("Slash Angle (degrees)",       Float) = -45.0
        _SweepSharpness("Sweep Edge Sharpness",       Float) = 6.0

        [Header(Colour)]
        [HDR] _CoreColor  ("Core Color",  Color) = (1.0, 1.0, 1.0,  1.0)
        [HDR] _MidColor   ("Mid Color",   Color) = (0.6, 0.85, 1.0, 1.0)
        [HDR] _OuterColor ("Outer Color", Color) = (0.1, 0.3,  0.9, 1.0)
        _EmissiveBoost    ("Emissive Boost", Float) = 4.0

        [Header(Glow)]
        _GlowWidth    ("Glow Edge Width",             Float) = 0.25
        _GlowFalloff  ("Glow Falloff",                Float) = 2.5
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
            Name "SwordSlashForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One       // Additive — bloom-friendly
            ZWrite Off
            Cull Off            // Visible from both sides

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── Properties ──────────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float  _Age;
                float  _Duration;
                float  _SlashAngle;
                float  _SweepSharpness;
                float4 _CoreColor;
                float4 _MidColor;
                float4 _OuterColor;
                float  _EmissiveBoost;
                float  _GlowWidth;
                float  _GlowFalloff;
            CBUFFER_END

            // ── Vertex I/O ───────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            // ── Vertex shader ────────────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // ── Fragment shader ──────────────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                // Normalised lifetime [0,1]
                float t = saturate(_Age / max(_Duration, 0.0001));

                // ── Rotate UV by _SlashAngle to set sweep direction ───────────────
                float2 centeredUV = IN.uv - 0.5;
                float  rad   = _SlashAngle * (3.14159265 / 180.0);
                float  cosA  = cos(rad);
                float  sinA  = sin(rad);
                float2 rotUV = float2(
                    centeredUV.x * cosA - centeredUV.y * sinA,
                    centeredUV.x * sinA + centeredUV.y * cosA
                ) + 0.5;

                // ── Sweep wipe: leading edge travels 0→1 along rotated U axis ────
                // The front edge is a sharp step; behind it the slash is visible.
                float sweepFront = t;                                   // leading edge position
                float sweepTrail = t - 0.35;                            // trailing fade starts here
                float frontEdge  = smoothstep(sweepFront, sweepFront - 0.08 / max(_SweepSharpness, 0.001), rotUV.x);
                float trailEdge  = smoothstep(sweepTrail, sweepTrail + 0.25, rotUV.x);
                float sweepMask  = saturate(frontEdge * trailEdge);

                // ── Arc V-axis: V=0.5 is the arc spine, fade toward 0 and 1 ──────
                float vDist   = abs(IN.uv.y - 0.5) * 2.0;             // 0 at spine, 1 at edges
                float arcMask = pow(saturate(1.0 - vDist), _GlowFalloff * (1.0 - _GlowWidth + 0.001));

                // Brighten the spine glow
                float glowSpine = pow(saturate(1.0 - vDist), _GlowFalloff * 3.5);

                // ── Lifetime fade: quick burst then fade out ──────────────────────
                float lifeAlpha = 1.0 - smoothstep(0.55, 1.0, t);
                // Brief intensity spike at the start
                float spike     = 1.0 + 1.5 * exp(-t * 12.0);

                // ── Tri-colour: V maps core→mid→outer across arc width ────────────
                float colT   = vDist;  // 0 = core (spine), 1 = outer (edge)
                float3 colour = lerp(_CoreColor.rgb,  _MidColor.rgb,   saturate(colT * 2.0));
                colour        = lerp(colour,           _OuterColor.rgb, saturate(colT * 2.0 - 1.0));

                // Spine glow brightens toward core color
                colour = lerp(colour, _CoreColor.rgb, glowSpine * 0.6);

                // Apply emissive, spike, and age dimming
                colour *= _EmissiveBoost * spike * (1.0 - t * 0.5);

                float alpha = sweepMask * arcMask * lifeAlpha;

                return half4(colour * alpha, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}

