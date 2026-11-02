// 월드 좌표로 텍스처를 입히는 지형 셰이더 (박스 크기와 상관없이 텍스처 크기가 일정하다)
//  - 평지 : 풀 (동쪽 숲 구역은 숲 바닥), 큰 얼룩으로 흙 패치
//  - 경사 : 흙
//  - 절벽 : 바위 (세 방향에서 투영해서 옆면도 늘어나지 않음)
//  - 높은 곳(산 정상) : 바위/흙이 섞임
Shader "PROJECT3D/WorldTerrain"
{
    Properties
    {
        _GrassTex ("Grass (평지)", 2D) = "white" {}
        _ForestTex ("Forest Ground (숲 바닥)", 2D) = "white" {}
        _DirtTex ("Dirt (경사)", 2D) = "white" {}
        _RockTex ("Rock (절벽/산)", 2D) = "white" {}
        _MacroTex ("Macro Noise (큰 얼룩)", 2D) = "gray" {}

        _Brightness ("전체 밝기", Range(0.5, 2)) = 1.15
        _TileSize ("텍스처 한 장 크기 (m)", Float) = 6
        _MacroSize ("큰 얼룩 크기 (m)", Float) = 60
        _MacroStrength ("큰 얼룩 밝기 차이", Range(0, 1)) = 0.2

        _DirtSlope ("흙 시작 기울기 (normal.y)", Range(0, 1)) = 0.95
        _RockSlope ("바위 시작 기울기 (normal.y)", Range(0, 1)) = 0.7
        _SlopeBlend ("기울기 섞이는 폭", Range(0.01, 0.3)) = 0.15
        _SlopeDirtAmount ("경사의 흙 최대량", Range(0, 1)) = 0.45
        _FlatDirtAmount ("평지의 흙 줄무늬 양", Range(0, 1)) = 0.12
        _SlopeFadeHeight ("경사 아래쪽 풀→흙 전환 높이 (m)", Float) = 4

        _ForestStartX ("숲 시작 X", Float) = 110
        _ForestEndX ("숲 완전 X", Float) = 135
        _MountainHeight ("산 정상 시작 높이", Float) = 14
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_GrassTex);  SAMPLER(sampler_GrassTex); // 모든 텍스처가 이 샘플러(반복)를 같이 쓴다
            TEXTURE2D(_ForestTex);
            TEXTURE2D(_DirtTex);
            TEXTURE2D(_RockTex);
            TEXTURE2D(_MacroTex);

            CBUFFER_START(UnityPerMaterial)
                float _Brightness;
                float _TileSize;
                float _MacroSize;
                float _MacroStrength;
                float _DirtSlope;
                float _RockSlope;
                float _SlopeBlend;
                float _SlopeDirtAmount;
                float _FlatDirtAmount;
                float _SlopeFadeHeight;
                float _ForestStartX;
                float _ForestEndX;
                float _MountainHeight;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            // 위에서 내려다본 방향(xz)으로 입힌다 (평지, 경사용)
            half3 SampleTop(TEXTURE2D_PARAM(tex, samp), float3 positionWS, float scale)
            {
                return SAMPLE_TEXTURE2D(tex, samp, positionWS.xz * scale).rgb;
            }

            // 세 방향(옆, 위, 앞)에서 입히고 면 방향에 따라 섞는다 (절벽 옆면도 늘어나지 않음)
            half3 SampleTriplanar(TEXTURE2D_PARAM(tex, samp), float3 positionWS, float3 weights, float scale)
            {
                half3 sideX = SAMPLE_TEXTURE2D(tex, samp, positionWS.zy * scale).rgb;
                half3 top = SAMPLE_TEXTURE2D(tex, samp, positionWS.xz * scale).rgb;
                half3 sideZ = SAMPLE_TEXTURE2D(tex, samp, positionWS.xy * scale).rgb;
                return sideX * weights.x + top * weights.y + sideZ * weights.z;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 positionWS = input.positionWS;
                float scale = 1.0 / _TileSize;

                // 세 방향 섞는 비율 (면이 향한 방향일수록 크게)
                float3 weights = pow(abs(normalWS), 4.0);
                weights /= (weights.x + weights.y + weights.z);

                // 큰 얼룩 (0~1). 반복 무늬가 티 나지 않게 하고, 흙 패치/숲 경계를 흔든다
                half macro = SAMPLE_TEXTURE2D(_MacroTex, sampler_GrassTex, positionWS.xz / _MacroSize).r;

                // 1. 평지 색 : 풀 ↔ 숲 바닥 (x 기준, 경계는 얼룩으로 흔들기)
                half3 grass = SampleTop(TEXTURE2D_ARGS(_GrassTex, sampler_GrassTex), positionWS, scale);
                half3 grassFar = SampleTop(TEXTURE2D_ARGS(_GrassTex, sampler_GrassTex), positionWS + 17.0, scale * 0.31);
                grass = lerp(grass, grassFar, 0.35); // 다른 크기로 한 번 더 섞어서 반복 무늬 줄이기

                half3 forest = SampleTop(TEXTURE2D_ARGS(_ForestTex, sampler_GrassTex), positionWS, scale);
                half3 dirt = SampleTop(TEXTURE2D_ARGS(_DirtTex, sampler_GrassTex), positionWS, scale);
                half3 rock = SampleTriplanar(TEXTURE2D_ARGS(_RockTex, sampler_GrassTex), positionWS, weights, scale * 0.8);

                float forestMask = smoothstep(_ForestStartX, _ForestEndX, positionWS.x + (macro - 0.5) * 30.0);
                half3 flatColor = lerp(grass, forest, forestMask);

                float dirtPatch = smoothstep(0.62, 0.78, macro); // 군데군데 흙 바닥 (가장자리를 넓게 섞기)
                flatColor = lerp(flatColor, dirt, dirtPatch * 0.6);

                float mountain = smoothstep(_MountainHeight, _MountainHeight + 6.0, positionWS.y); // 산 정상은 조금 거칠게
                flatColor = lerp(flatColor, lerp(dirt, rock, 0.4), mountain * 0.35);

                // 2. 흙 줄무늬 : 평지와 경사가 같은 무늬를 쓰고 양만 다르게 → 경계에서 무늬가 끊기지 않는다
                half dirtDetail = dot(dirt, half3(0.333, 0.333, 0.333));      // 흙 텍스처 밝기를 줄무늬 모양으로 사용
                float streak = saturate((macro * 0.6 + dirtDetail * 0.8) - 0.35) * 2.0;

                // 3. 기울기 : 평지 → (풀+흙) 경사 → 바위
                float up = normalWS.y;
                float slopeFactor = 1.0 - smoothstep(_DirtSlope - _SlopeBlend, _DirtSlope, up);
                float slopeFade = smoothstep(0.0, _SlopeFadeHeight, positionWS.y); // 경사 아래쪽은 바닥과 같은 풀에서 시작
                float dirtAmount = lerp(_FlatDirtAmount, _SlopeDirtAmount * slopeFade, slopeFactor) * streak;
                float toRock = 1.0 - smoothstep(_RockSlope - _SlopeBlend, _RockSlope, up);

                half3 albedo = lerp(flatColor, dirt, saturate(dirtAmount));
                albedo = lerp(albedo, rock, toRock);
                albedo *= lerp(1.0 - _MacroStrength * 0.5, 1.0 + _MacroStrength * 0.5, macro);
                albedo *= _Brightness;

                // 4. 조명 : 주 광원(그림자 포함) + 주변광
                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 lightColor = mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half3 diffuse = LightingLambert(lightColor, mainLight.direction, normalWS);
                half3 ambient = SampleSHPixel(half3(0, 0, 0), normalWS);

                half3 color = albedo * (diffuse + ambient);
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // 그림자 / 깊이는 URP 기본 Lit 셰이더의 것을 그대로 쓴다
        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"
        UsePass "Universal Render Pipeline/Lit/DEPTHONLY"
        UsePass "Universal Render Pipeline/Lit/DEPTHNORMALS"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
