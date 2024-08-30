﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace RosettaUI.Test
{
    // ReSharper disable once InconsistentNaming
    public class ClipboardParserTest_Deserialize
    {
        [TestCaseSource(nameof(BoolSource))]
        public void MatchUnityEditorMethod_Bool(string text) => TestMatch(text, EditorClipBoardParser.ParseBool);

        [TestCaseSource(nameof(IntSource))]
        public void MatchUnityEditorMethod_Int(string text) => TestMatch(text, EditorClipBoardParser.ParseInteger);

        [TestCaseSource(nameof(UIntSource))]
        public void MatchUnityEditorMethod_UInt(string text) => TestMatch(text, EditorClipBoardParser.ParseUint);

        [TestCaseSource(nameof(StringSource))]
        public void MatchUnityEditorMethod_String(string text) => TestMatch(text, EditorClipBoardParser.ParseString);


        [TestCaseSource(nameof(FloatSource))]
        public void MatchUnityEditorMethod_Float(string text) => TestMatch(text, EditorClipBoardParser.ParseFloat);

        [TestCaseSource(nameof(EnumSource))]
        public void MatchUnityEditorMethod_Enum(string text) =>
            TestMatch(text, EditorClipBoardParser.ParseEnum<EnumForTest>);

        [TestCaseSource(nameof(Vector2Source))]
        public void MatchUnityEditorMethod_Vector2(string text) => TestMatch(text, EditorClipBoardParser.ParseVector2);

        [TestCaseSource(nameof(Vector3Source))]
        public void MatchUnityEditorMethod_Vector3(string text) => TestMatch(text, EditorClipBoardParser.ParseVector3);

        [TestCaseSource(nameof(Vector4Source))]
        public void MatchUnityEditorMethod_Vector4(string text) => TestMatch(text, EditorClipBoardParser.ParseVector4);

        // UnityEditor.ClipboardParserはVector2Int非対応。ClipboardContextMenuでVector2からキャストしている
        // Vector2Intのシリアライズ書式はVector2と同じなのでTestCastSourceを共有する
        [TestCaseSource(nameof(Vector2Source))]
        public void MatchUnityEditorMethod_Vector2Int(string text) => TestMatch(text,
            CastParser(EditorClipBoardParser.ParseVector2, ClipboardParserUtility.ToInt));

        // UnityEditor.ClipboardParserはVector3Int非対応。ClipboardContextMenuでVector3からキャストしている
        // Vector3Intのシリアライズ書式はVector3と同じなのでTestCastSourceを共有する
        [TestCaseSource(nameof(Vector3Source))]
        public void MatchUnityEditorMethod_Vector3Int(string text) => TestMatch(text,
            CastParser(EditorClipBoardParser.ParseVector3, ClipboardParserUtility.ToInt));

        [TestCaseSource(nameof(RectSource))]
        public void MatchUnityEditorMethod_Rect(string text) => TestMatch(text, EditorClipBoardParser.ParseRect);

        // UnityEditor.ClipboardParserはRectInt非対応。ClipboardContextMenuでRectからキャストしている
        // RectIntのシリアライズ書式はRectと同じなのでTestCastSourceを共有する
        [TestCaseSource(nameof(RectSource))]
        public void MatchUnityEditorMethod_RectInt(string text) => TestMatch(text,
            CastParser(EditorClipBoardParser.ParseRect, ClipboardParserUtility.ToInt));

        [TestCaseSource(nameof(BoundsSource))]
        public void MatchUnityEditorMethod_Bounds(string text) => TestMatch(text, EditorClipBoardParser.ParseBounds);

        // UnityEditor.ClipboardParserはBoundsInt非対応。ClipboardContextMenuでBoundsからキャストしている
        // BoundsIntのシリアライズ書式はBoundsと同じなのでTestCastSourceを共有する
        [TestCaseSource(nameof(BoundsSource))]
        public void MatchUnityEditorMethod_BoundsInt(string text) => TestMatch(text,
            CastParser(EditorClipBoardParser.ParseBounds, ClipboardParserUtility.ToIntKeepValueLook));

        [TestCaseSource(nameof(QuaternionSource))]
        public void MatchUnityEditorMethod_Quaternion(string text) =>
            TestMatch(text, EditorClipBoardParser.ParseQuaternion);


        [TestCaseSource(nameof(ColorSource))]
        public void MatchUnityEditorMethod_Color(string text) => TestMatch(text, EditorClipBoardParser.ParseColor);

        [TestCaseSource(nameof(GradientSource))]
        public void MatchUnityEditorMethod_Gradient(string text) =>
            TestMatch(text, EditorClipBoardParser.ParseGradient);

        [TestCaseSource(nameof(GenericSource))]
        public void MatchUnityEditorMethod_Generic(string text) =>
            TestMatch(text, EditorClipBoardParser.ParseGeneric<ClassForTest>);


        private static Func<string, (bool, TTarget)> CastParser<TOriginal, TTarget>(
            Func<string, (bool, TOriginal)> func, Func<TOriginal, TTarget> castFunc)
        {
            return (text) =>
            {
                var (success, value) = func(text);
                return (success, success ? castFunc(value) : default);
            };
        }

        private static string[] BoolSource => new[]
        {
            "True", "False", "true", "false", "", null, "expect parse fail"
        };

        private static string[] IntSource => new[]
        {
            "0", "1", "10", "-1", "-10", int.MinValue.ToString(), int.MaxValue.ToString(), null, "expect parse fail"
        };

        private static string[] UIntSource => new[]
        {
            "0", "1", "10", "-1", "-10", uint.MinValue.ToString(), uint.MaxValue.ToString(), null, "expect parse fail"
        };

        private static string[] StringSource => new[]
        {
            "", "a", "012346789", "this is a pen.", "これはペンです。", null
        };

        private static string[] FloatSource => new[]
        {
            "0", "0.1", "1.0", "10", "-0.1", "-1", "-10",
            float.MinValue.ToString(CultureInfo.InvariantCulture),
            float.MaxValue.ToString(CultureInfo.InvariantCulture),
            null, "expect parse fail"
        };

        private static string[] EnumSource => new[]
        {
            "", "_","Enum:",
            "one", "One",
            "Enum:one", "Enum:One",
            "Enum one", "Enum;one",
            "Enum:_two", "Enum:Two",
            "Enum:three_", "Enum:Three",
            "Enum:fourthItem", "Enum:FourthItem",
            "Enum:FifthItem", "Enum:fifthItem",
            "Enum:Sixth_Item", "Enum:SixthItem",
            "Enum:SEVEN", "Enum:Seven",
            null,
            "expect parse fail"
        };

        private static IEnumerable<string> Vector2Source => FloatsSourceFill("Vector2", 2).Concat(FloatsSourceInvalid());
        private static IEnumerable<string> Vector3Source => FloatsSourceFill("Vector3", 3).Concat(FloatsSourceInvalid());
        private static IEnumerable<string> Vector4Source => FloatsSourceFill("Vector4", 4).Concat(FloatsSourceInvalid());
        private static IEnumerable<string> RectSource => FloatsSourceFill("Rect", 4).Concat(FloatsSourceInvalid());
        private static IEnumerable<string> QuaternionSource => FloatsSourceFill("Quaternion", 4).Concat(FloatsSourceInvalid());
        private static IEnumerable<string> BoundsSource => FloatsSourceFill("Bounds", 6).Concat(FloatsSourceInvalid());
        private static IEnumerable<string> ColorSource => FloatsSourceFill("Color", 4).Concat(FloatsSourceInvalid());
        
        
        private static IEnumerable<string> FloatsSourceFill(string prefix, int count)
        {
            var floatTexts = new[]
            {
                "0", "1", "0.1", "-0.1", "1e+32", "-1e+32", "Nan", "Infinity", "-Infinity",
            };

            return floatTexts.Select(text => $"{prefix}({string.Join(',', Enumerable.Repeat(text, count))})");
        }

        private static IEnumerable<string> FloatsSourceInvalid()
        {
            return new[]
            {
                "Vector2(0,0,0,0)", "Vector3(0,0,0,0)", "Vector4(0,0,0,0)",
                null, "expect parse fail"
            };
        }

        private static string[] GradientSource => new[]
            { EditorClipBoardParser.WriteGradient(new Gradient() { mode = GradientMode.Fixed }) };

        private static string[] GenericSource ()
        {
            var expectSuccessString = EditorClipBoardParser.WriteGeneric(new ClassForTest()
            {
                layerMask = 1
            });
            
            
            // 同名型違いのデータがある場合はEditorと挙動が異なる
            // RosettaUIでは型違いのデータはパースできないものとする
            // var expectFailString = Regex.Replace(expectSuccessString, "\"type\":\\d,\"val\":0", "\"type\":3,\"val\":\"\"");
            
            return new[]
            {
                expectSuccessString,
                // expectFailString,
                null,
                "expect parse fail"
            };
        }
        

        public void TestMatch<T>(string text, Func<string, (bool, T)> expectedFunc)
        {
            var (expectedSuccess, expectedValue) = expectedFunc(text);
            var (testSuccess, testValue) = ClipboardParser.Deserialize<T>(text);
            Assert.AreEqual(expectedSuccess, testSuccess);

            if (!expectedSuccess) return;
            if (typeof(T).IsValueType)
            {
                Assert.AreEqual(expectedValue, testValue);
            }
            else
            {
                Assert.AreEqual(JsonUtility.ToJson(expectedValue), JsonUtility.ToJson(testValue));
            }
        }
    }
}