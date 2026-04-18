Shader "Custom/ForceField"
{
    Properties
    {
        // Shared texture for all three layers
        _MainTex ("Main Texture", 2D) = "white" {}

        // Layer 1
        [Header(Layer 1)]
        _Layer1Tiling  ("Layer 1 Tiling",   Float) = 1.0
        _Layer1ScrollX ("Layer 1 Scroll X", Float) = 0.1
        _Layer1ScrollY ("Layer 1 Scroll Y", Float) = 0.2
        _Layer1Color   ("Layer 1 Color",    Color) = (0.2, 0.6, 1.0, 1.0)

        // Layer 2
        [Header(Layer 2)]
        _Layer2Tiling  ("Layer 2 Tiling",   Float) = 1.5
        _Layer2ScrollX ("Layer 2 Scroll X", Float) = -0.15
        _Layer2ScrollY ("Layer 2 Scroll Y", Float) = 0.1
        _Layer2Color   ("Layer 2 Color",    Color) = (0.1, 0.4, 0.9, 1.0)

        // Layer 3
        [Header(Layer 3)]
        _Layer3Tiling  ("Layer 3 Tiling",   Float) = 2.5
        _Layer3ScrollX ("Layer 3 Scroll X", Float) = 0.05
        _Layer3ScrollY ("Layer 3 Scroll Y", Float) = -0.25
        _Layer3Color   ("Layer 3 Color",    Color) = (0.5, 0.8, 1.0, 1.0)

        // Fresnel
        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Float) = 2.0
        _FresnelColor ("Fresnel Color", Color) = (0.3, 0.7, 1.0, 1.0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForceFieldForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One       // Additive
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // -------------------------------------------------------
            // Textures & Samplers
            // -------------------------------------------------------
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // -------------------------------------------------------
            // Constant Buffer
            // -------------------------------------------------------
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;

                float  _Layer1Tiling;
                float  _Layer1ScrollX;
                float  _Layer1ScrollY;
                float4 _Layer1Color;

                float  _Layer2Tiling;
                float  _Layer2ScrollX;
                float  _Layer2ScrollY;
                float4 _Layer2Color;

                float  _Layer3Tiling;
                float  _Layer3ScrollX;
                float  _Layer3ScrollY;
                float4 _Layer3Color;

                float  _FresnelPower;
                float4 _FresnelColor;
            CBUFFER_END

            // -------------------------------------------------------
            // Structs
            // -------------------------------------------------------
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
            };

            // -------------------------------------------------------
            // Vertex
            // -------------------------------------------------------
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs    = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.normalWS   = normalInputs.normalWS;
                OUT.viewDirWS  = GetWorldSpaceViewDir(posInputs.positionWS);

                return OUT;
            }

            // -------------------------------------------------------
            // Fragment
            // -------------------------------------------------------
            half4 frag(Varyings IN) : SV_Target
            {
                float2 baseUV = IN.uv;

                // --- Layer 1 ---
                float2 uv1    = baseUV * _Layer1Tiling + _Time.y * float2(_Layer1ScrollX, _Layer1ScrollY);
                half4  samp1  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv1);
                half4  layer1 = samp1 * _Layer1Color;

                // --- Layer 2 ---
                float2 uv2    = baseUV * _Layer2Tiling + _Time.y * float2(_Layer2ScrollX, _Layer2ScrollY);
                half4  samp2  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv2);
                half4  layer2 = samp2 * _Layer2Color;

                // --- Layer 3 ---
                float2 uv3    = baseUV * _Layer3Tiling + _Time.y * float2(_Layer3ScrollX, _Layer3ScrollY);
                half4  samp3  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv3);
                half4  layer3 = samp3 * _Layer3Color;

                // --- Composite layers ---
                half4 composite = layer1 + layer2 + layer3;

                // --- Fresnel ---
                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);
                float  fresnel   = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                composite.rgb   += fresnel * _FresnelColor.rgb;

                // Alpha drives additive brightness; keep it consistent with rgb luminance
                composite.a = saturate(composite.r + composite.g + composite.b);

                return composite;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

