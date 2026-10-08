Shader "TheGoodNightPotion/UI/Glowing Rays"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _RayColor ("Ray Color", Color) = (1,0.72,0.16,1)
        _RayCount ("Ray Count", Float) = 12
        _RotationSpeed ("Rotation Speed", Float) = -0.349066
        _MinimumPulse ("Minimum Pulse", Range(0,1)) = 0.58
        _PulseAmplitude ("Pulse Amplitude", Range(0,1)) = 0.32
        _PulseSpeed ("Pulse Speed", Float) = 2.4
        [Toggle] _UseRandomSeed ("Use Random Seed", Float) = 0
        _PulseSpeedVariation ("Pulse Speed Variation", Range(0,1)) = 0.15
        _InnerRadiusRatio ("Inner Radius Ratio", Range(0,1)) = 0.24
        _ShortRayHalfAngle ("Short Ray Half Angle", Range(0,0.5)) = 0.055
        _LongRayHalfAngle ("Long Ray Half Angle", Range(0,0.5)) = 0.17
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float seed : TEXCOORD2;
            };

            fixed4 _Color;
            fixed4 _RayColor;
            float _RayCount;
            float _RotationSpeed;
            float _MinimumPulse;
            float _PulseAmplitude;
            float _PulseSpeed;
            float _UseRandomSeed;
            float _PulseSpeedVariation;
            float _InnerRadiusRatio;
            float _ShortRayHalfAngle;
            float _LongRayHalfAngle;
            float4 _ClipRect;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);
                output.uv = input.texcoord;
                output.color = input.color * _Color;
                output.seed = input.texcoord1.x;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                const float twoPi = 6.2831853;
                float2 position = (input.uv - 0.5) * 2.0;
                float radius = length(position);
                float rayCount = max(_RayCount, 1.0);
                float angle = atan2(position.y, position.x) - _Time.y * _RotationSpeed;
                float normalizedAngle = frac(angle / twoPi);
                float rayIndex = floor(normalizedAngle * rayCount);
                float rayAngle = abs(frac(normalizedAngle * rayCount) - 0.5) * twoPi / rayCount;
                float longRay = 1.0 - fmod(rayIndex, 2.0);
                float halfAngle = lerp(_ShortRayHalfAngle, _LongRayHalfAngle, longRay);
                float angularMask = 1.0 - smoothstep(halfAngle, halfAngle + 0.02, rayAngle);
                float radialMask = step(_InnerRadiusRatio, radius) * (1.0 - smoothstep(_InnerRadiusRatio, 1.0, radius));
                float randomSeedEnabled = step(0.5, _UseRandomSeed);
                float pulseSpeedMultiplier = lerp(1.0 - _PulseSpeedVariation, 1.0 + _PulseSpeedVariation, input.seed);
                float pulseTime = _Time.y * _PulseSpeed * lerp(1.0, pulseSpeedMultiplier, randomSeedEnabled);
                float pulse = _MinimumPulse + sin(pulseTime + input.seed * twoPi * randomSeedEnabled) * _PulseAmplitude;
                fixed4 color = _RayColor * input.color;
                color.a *= angularMask * radialMask * pulse * step(radius, 1.0);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
