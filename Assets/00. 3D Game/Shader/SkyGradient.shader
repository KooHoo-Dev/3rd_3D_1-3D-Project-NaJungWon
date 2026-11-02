// ===== 새로 추가 : 하늘(Skybox) 셰이더 =====
// 텍스처 없이 코드로 그리는 하늘. 씬의 Directional Light(태양) 높이에 따라 낮 → 노을 → 밤으로 바뀐다
//  - 하늘 : 위 / 지평선 / 아래 3색 그라데이션 (낮, 노을, 밤 색을 따로 정한다)
//  - 태양 : 빛 방향에 원판 + 주변 번짐. 노을 때는 태양 쪽 지평선이 붉게 빛난다
//  - 구름 : 노이즈로 그린 구름이 천천히 흘러간다 (양, 크기, 속도 조절)
//  - 별 : 밤에만 보인다
// 나중에 태양을 돌리는 스크립트만 넣으면 하늘은 따로 건드리지 않아도 시간대에 맞게 바뀐다
Shader "PROJECT3D/SkyGradient"
{
    Properties
    {
        [Header(Day)]
        _DayTop ("낮 - 위", Color) = (0.24, 0.52, 0.92, 1)
        _DayHorizon ("낮 - 지평선", Color) = (0.72, 0.86, 0.98, 1)
        _DayBottom ("낮 - 아래", Color) = (0.55, 0.62, 0.68, 1)

        [Header(Sunset)]
        _SunsetTop ("노을 - 위", Color) = (0.24, 0.27, 0.52, 1)
        _SunsetHorizon ("노을 - 지평선", Color) = (1.0, 0.55, 0.28, 1)
        _SunsetBottom ("노을 - 아래", Color) = (0.35, 0.25, 0.28, 1)

        [Header(Night)]
        _NightTop ("밤 - 위", Color) = (0.02, 0.03, 0.09, 1)
        _NightHorizon ("밤 - 지평선", Color) = (0.08, 0.10, 0.22, 1)
        _NightBottom ("밤 - 아래", Color) = (0.02, 0.02, 0.04, 1)

        _HorizonExponent ("지평선 띠 두께 (작을수록 얇다)", Range(0.1, 1)) = 0.45

        [Header(Sun)]
        _SunColor ("태양 색 (낮)", Color) = (1.0, 0.96, 0.85, 1)
        _SunsetSunColor ("태양 색 (노을)", Color) = (1.0, 0.6, 0.3, 1)
        _SunSize ("태양 크기 (도)", Range(0.5, 6)) = 2.5
        _SunGlow ("태양 주변 번짐", Range(0, 2)) = 0.6
        _SunsetGlow ("노을 때 태양 쪽 지평선 밝기", Range(0, 2)) = 1
        _FallbackSunDirection ("태양이 없을 때 방향", Vector) = (0.3, 0.6, 0.4, 0)

        [Header(Clouds)]
        _CloudCoverage ("구름 양", Range(0, 1)) = 0.45
        _CloudSoftness ("구름 가장자리 부드러움", Range(0.01, 0.4)) = 0.15
        _CloudScale ("구름 크기 (작을수록 큰 구름)", Range(0.2, 5)) = 1.2
        _CloudSpeed ("구름 흐르는 속도", Range(0, 0.2)) = 0.01
        _CloudShade ("구름 두꺼운 곳 어둡기", Range(0, 1)) = 0.35
        _CloudDayColor ("구름 색 (낮)", Color) = (1, 1, 1, 1)
        _CloudSunsetColor ("구름 색 (노을)", Color) = (1.0, 0.70, 0.55, 1)
        _CloudNightColor ("구름 색 (밤)", Color) = (0.15, 0.17, 0.25, 1)

        [Header(Stars)]
        _StarDensity ("별 촘촘함", Range(50, 400)) = 180
        _StarBrightness ("별 밝기", Range(0, 3)) = 1.2
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DayTop, _DayHorizon, _DayBottom;
                half4 _SunsetTop, _SunsetHorizon, _SunsetBottom;
                half4 _NightTop, _NightHorizon, _NightBottom;
                float _HorizonExponent;
                half4 _SunColor, _SunsetSunColor;
                float _SunSize, _SunGlow, _SunsetGlow;
                float4 _FallbackSunDirection;
                float _CloudCoverage, _CloudSoftness, _CloudScale, _CloudSpeed, _CloudShade;
                half4 _CloudDayColor, _CloudSunsetColor, _CloudNightColor;
                float _StarDensity, _StarBrightness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0; // 하늘 메쉬의 정점 위치 = 바라보는 방향
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            // ---- 노이즈 (구름 / 별) ----
            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Hash3(float3 p)
            {
                return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453);
            }

            // 격자 꼭짓점의 랜덤 값을 부드럽게 이은 노이즈 (0 ~ 1)
            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash(cell);
                float b = Hash(cell + float2(1, 0));
                float c = Hash(cell + float2(0, 1));
                float d = Hash(cell + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // 크기가 다른 노이즈 4장을 겹친다 (큰 덩어리 + 작은 디테일)
            float Fbm(float2 p)
            {
                float value = 0.0;
                float amount = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += amount * ValueNoise(p);
                    p = p * 2.03 + 17.1;
                    amount *= 0.5;
                }
                return value;
            }

            // 방향에 맞는 3색 그라데이션 (y > 0 : 지평선 → 위, y < 0 : 지평선 → 아래)
            half3 Gradient(float y, half3 top, half3 horizon, half3 bottom)
            {
                if (y >= 0.0) return lerp(horizon, top, pow(saturate(y), _HorizonExponent));
                return lerp(horizon, bottom, pow(saturate(-y), 0.5));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);

                // 1. 태양 방향 (씬에 Directional Light가 없으면 대신 쓸 방향)
                float3 sunDirection = _MainLightPosition.xyz;
                if (dot(_MainLightColor.rgb, _MainLightColor.rgb) < 0.0001) sunDirection = _FallbackSunDirection.xyz;
                sunDirection = normalize(sunDirection);

                // 2. 시간대 비율 : 태양 높이로 정한다 (높으면 낮, 지평선 근처면 노을, 아래면 밤)
                float sunHeight = sunDirection.y;
                float day = smoothstep(0.0, 0.3, sunHeight);
                float night = smoothstep(0.0, -0.2, sunHeight);
                float sunset = saturate(1.0 - day - night);

                // 3. 하늘 색
                half3 sky = Gradient(direction.y, _DayTop.rgb, _DayHorizon.rgb, _DayBottom.rgb) * day;
                sky += Gradient(direction.y, _SunsetTop.rgb, _SunsetHorizon.rgb, _SunsetBottom.rgb) * sunset;
                sky += Gradient(direction.y, _NightTop.rgb, _NightHorizon.rgb, _NightBottom.rgb) * night;

                // 노을 때 태양 쪽 지평선이 더 붉게 빛난다
                float2 flatDirection = normalize(direction.xz + 0.0001);
                float2 flatSun = normalize(sunDirection.xz + 0.0001);
                float towardSun = pow(saturate(dot(flatDirection, flatSun) * 0.5 + 0.5), 4.0);
                float nearHorizon = pow(1.0 - saturate(abs(direction.y)), 4.0);
                sky += _SunsetHorizon.rgb * towardSun * nearHorizon * sunset * _SunsetGlow;

                // 4. 구름 : 하늘을 평면에 펼친 좌표로 노이즈를 읽는다 (지평선 쪽은 멀리 있는 구름이라 작고 흐리게)
                float cloud = 0.0;
                float cloudShade = 1.0;
                if (direction.y > 0.0)
                {
                    float2 uv = direction.xz / (direction.y + 0.15) * _CloudScale;
                    uv += _Time.y * _CloudSpeed * float2(1.0, 0.3);
                    float noise = Fbm(uv);

                    float threshold = lerp(0.75, 0.3, _CloudCoverage); // 구름 양이 많을수록 낮은 값부터 구름
                    cloud = smoothstep(threshold, threshold + _CloudSoftness, noise);
                    cloud *= smoothstep(0.0, 0.15, direction.y);      // 지평선 바로 위는 흐리게
                    cloudShade = 1.0 - saturate((noise - threshold) * 2.0) * _CloudShade; // 두꺼운 곳은 조금 어둡게
                }

                half3 cloudColor = _CloudDayColor.rgb * day + _CloudSunsetColor.rgb * sunset + _CloudNightColor.rgb * night;
                cloudColor *= cloudShade;

                // 5. 태양 : 원판 + 번짐 (구름에 가려지고, 지평선 아래로는 안 보인다)
                float sunDot = dot(direction, sunDirection);
                float sunRadius = radians(_SunSize);
                float disc = smoothstep(cos(sunRadius * 1.2), cos(sunRadius * 0.9), sunDot);
                float glow = pow(saturate(sunDot), 64.0) * 0.5 + pow(saturate(sunDot), 8.0) * 0.15;
                half3 sunColor = lerp(_SunsetSunColor.rgb, _SunColor.rgb, day) * (1.0 - night);
                float aboveHorizon = smoothstep(-0.02, 0.02, direction.y);

                sky += sunColor * glow * _SunGlow * aboveHorizon;
                sky = lerp(sky, cloudColor + sunColor * glow * _SunGlow * 0.5, cloud); // 구름 (태양 근처 구름은 밝게)
                sky += sunColor * disc * 3.0 * aboveHorizon * (1.0 - cloud);

                // 6. 별 : 밤에만, 구름 없는 곳에만
                float3 starCell = floor(direction * _StarDensity);
                float starRandom = Hash3(starCell);
                float3 inCell = frac(direction * _StarDensity) - 0.5;
                float star = step(0.996, starRandom) * smoothstep(0.35, 0.0, length(inCell));
                star *= 0.6 + 0.4 * sin(_Time.y * 2.0 + starRandom * 100.0); // 반짝임
                sky += star * _StarBrightness * night * aboveHorizon * (1.0 - cloud);

                return half4(sky, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
// ===== 새로 추가 끝 =====
