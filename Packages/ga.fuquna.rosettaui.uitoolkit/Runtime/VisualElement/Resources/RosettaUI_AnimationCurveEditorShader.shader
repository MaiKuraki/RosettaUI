﻿Shader "Hidden/RosettaUI_AnimationCurveEditorShader"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"
            #include "SdfBezierSpline.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            StructuredBuffer<float4> _Keyframes;
            float4 _Resolution;
            float4 _OffsetZoom;
            float4 _GridParams;

            struct spline_segment
            {
                float2 startPos;
                float2 startVel;
                float2 endPos;
                float2 endVel;
            };

            StructuredBuffer<spline_segment> _Spline;
            int _SegmentCount;

            static const float LineWidth = 2.0f;
            static const float4 GridColor = float4(0.4, 0.4, 0.4, 0.5);
            static const float4 LineColor = float4(0, 1, 0, 1);

            v2f Vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 Frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                uv = uv / _OffsetZoom.zw + _OffsetZoom.xy;

                // Curve
                float2x3 CurveUvToViewUv = {
                    1, 0, -_OffsetZoom.x,
                    0, 1, -_OffsetZoom.y,
                };
                float2x2 scaleMatrix = {
                    _OffsetZoom.z, 0,
                    0, _OffsetZoom.w
                };
                CurveUvToViewUv = mul(scaleMatrix, CurveUvToViewUv);
                
                float2 ViewUvToPx = _Resolution.xy;

                float dist = INITIALLY_FAR;
                for (int idx = 0; idx < _SegmentCount; idx++)
                {
                    /* if startVel or endVel inf draw HV
                     * elif startVel or endVel -inf draw VH
                     * else draw curve
                     */
                    spline_segment seg = _Spline[idx];

                    const float2 p = ViewUvToPx * i.uv;
                    const float2 pStart = ViewUvToPx * mul(CurveUvToViewUv, float3(seg.startPos, 1));
                    const float2 vStart = ViewUvToPx * mul(CurveUvToViewUv, float3(seg.startVel, 0));
                    const float2 pEnd = ViewUvToPx * mul(CurveUvToViewUv, float3(seg.endPos, 1));
                    const float2 vEnd = ViewUvToPx * mul(CurveUvToViewUv, float3(seg.endVel, 0));

                    const bool isStartPosInf = any(isinf(seg.startVel) && seg.startVel > 0);
                    const bool isStartNegInf = any(isinf(seg.startVel) && seg.startVel < 0);
                    const bool isEndPosInf = any(isinf(seg.endVel) && seg.endVel > 0);
                    const bool isEndNegInf = any(isinf(seg.endVel) && seg.endVel < 0);

                    if (isStartPosInf || isStartNegInf || isEndPosInf || isEndNegInf)
                    {
                        const float2 mid = isStartPosInf || isEndPosInf
                                               ? float2(pEnd.x, pStart.y)
                                               : float2(pStart.x, pEnd.y);
                        dist = min(dist, SdSegment(p, pStart, mid));
                        dist = min(dist, SdSegment(p, mid, pEnd));
                    }
                    else
                    {
                        const float2 p0 = pStart;
                        const float2 p1 = pStart + vStart; // Hermite curve 1/3 factor rolled into tangent value
                        const float2 p2 = pEnd - vEnd;
                        const float2 p3 = pEnd;
                        dist = min(dist, CubicBezierSegmentSdfL2(p, p0, p1, p2, p3));
                    }
                }

                float4 col = 0;
                
                // Grid
                float2 main = _GridParams.zw;
                float2 grid = _GridParams.zw * (_GridParams.xy < 0 ? 0.1 : 1);
                float2 sub = _GridParams.zw * (_GridParams.xy < 0 ? 0.01 : 0.1);
                float2 mainGridThickness = _Resolution.zw / (main * _OffsetZoom.zw) * 2.0;
                float2 gridThickness = _Resolution.zw / (grid * _OffsetZoom.zw) * 1.0;
                float2 subThickness = _Resolution.zw / (sub * _OffsetZoom.zw) * 1.0;
                bool isMainGrid = any(abs(frac(uv / main - 0.5) - 0.5) < mainGridThickness);
                bool isGrid = any(abs(frac(uv / grid - 0.5) - 0.5) < gridThickness);
                bool2 isSubGrid = abs(frac(uv / sub - 0.5) - 0.5) < subThickness;
                
                col = isSubGrid.x ? float4(GridColor.rgb, 1.0 - frac(_GridParams.x)) : col;
                col = isSubGrid.y ? float4(GridColor.rgb, 1.0 - frac(_GridParams.y)) : col;
                col = isGrid ? GridColor : col;
                col = isMainGrid ? GridColor : col;
                
                // Line
                dist -= LineWidth * 0.5f;
                col = lerp(LineColor, col, smoothstep(-.5f, .5f, dist));

                return col;
            }
            ENDHLSL
        }
    }
}
