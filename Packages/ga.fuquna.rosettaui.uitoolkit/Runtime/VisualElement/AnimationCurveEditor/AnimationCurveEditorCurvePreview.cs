﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace RosettaUI.UIToolkit
{
    public class AnimationCurveEditorCurvePreview : IDisposable
    {
        private CommandBuffer _commandBuffer;
        private Material _curveDrawMaterial;
        private GraphicsBuffer _curveDataBuffer;
        private int _numActiveSegments;

        public struct CurvePreviewViewInfo
        {
            public Vector4 resolution;
            public Vector4 offsetZoom;
            public RenderTexture outputTexture;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "NotAccessedField.Local")]
        private struct SplineSegmentData
        {
            public Vector2 startPos;
            public Vector2 startVel;

            public Vector2 endPos;
            public Vector2 endVel;
        }

        public void UpdateData(CommandBuffer cmdBuf, AnimationCurve animationCurve)
        {
            var segments = animationCurve.keys.Zip(animationCurve.keys.Skip(1), ValueTuple.Create);
            var splineData = segments.Select(seg => new SplineSegmentData {
                startPos = new Vector2(seg.Item1.time, seg.Item1.value),
                endPos = new Vector2(seg.Item2.time, seg.Item2.value),
                startVel = (seg.Item2.time - seg.Item1.time) * seg.Item1.GetOutWeight() * new Vector2(1, seg.Item1.outTangent),
                endVel = (seg.Item2.time - seg.Item1.time) * seg.Item2.GetInWeight() * new Vector2(1, seg.Item2.inTangent),
            }).ToArray();

            _numActiveSegments = splineData.Length;

            if (_curveDataBuffer == null || _curveDataBuffer.count < _numActiveSegments)
            {
                _curveDataBuffer?.Dispose();
                _curveDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _numActiveSegments, Marshal.SizeOf<SplineSegmentData>());
            }

            cmdBuf.SetBufferData(_curveDataBuffer, splineData);
        }

        public void DrawCurve(CommandBuffer cmdBuf, RenderTexture outputTexture)
        {
            cmdBuf.SetGlobalBuffer("_Spline", _curveDataBuffer);
            cmdBuf.SetGlobalInt("_SegmentCount", _numActiveSegments);
            cmdBuf.Blit(null, outputTexture, _curveDrawMaterial);
        }

        public void Render(AnimationCurve animationCurve, CurvePreviewViewInfo viewInfo)
        {
            _commandBuffer.Clear();
            UpdateData(_commandBuffer, animationCurve);

            _commandBuffer.SetGlobalVector("_Resolution", viewInfo.resolution);
            _commandBuffer.SetGlobalVector("_OffsetZoom", viewInfo.offsetZoom);

            DrawCurve(_commandBuffer, viewInfo.outputTexture);

            Graphics.ExecuteCommandBuffer(_commandBuffer);
        }

        public AnimationCurveEditorCurvePreview()
        {
            _curveDrawMaterial = new Material(Resources.Load<Shader>("RosettaUI_AnimationCurveEditorShader"));
            _commandBuffer = new CommandBuffer() { name = "AnimationCurvePreview" };
        }

        public void Dispose()
        {
            if (_curveDrawMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(_curveDrawMaterial);
                _curveDrawMaterial = null;
            }

            _commandBuffer?.Dispose();
            _commandBuffer = null;

            _curveDataBuffer?.Dispose();
            _curveDataBuffer = null;
        }
    }

    internal static class KeyframeExt
    {
        public static float GetOutWeight(this Keyframe keyframe) =>
            keyframe.weightedMode is WeightedMode.Out or WeightedMode.Both ? keyframe.outWeight : 1;

        public static float GetInWeight(this Keyframe keyframe) =>
            keyframe.weightedMode is WeightedMode.In or WeightedMode.Both ? keyframe.inWeight : 1;
    }
}
