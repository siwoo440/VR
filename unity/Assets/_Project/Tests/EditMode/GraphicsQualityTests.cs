using System.Collections.Generic;
using AtelierVerse.Core;
using AtelierVerse.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 화면 품질의 검사(20일차): 품질을 품질 단계의 이름으로 찾는 규칙, 프로젝트에 세 단계와 저마다의 그리기 설정이 있는지,
    /// 품질과 수직 동기화를 적용하는지. 품질마다 실제로 어떻게 보이는지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class GraphicsQualityTests
    {
        private const string QualitySettingsPath = "ProjectSettings/QualitySettings.asset";

        [SetUp]
        public void Setup()
        {
            GraphicsQuality.Restore();
        }

        // 에디터에서는 바꾼 품질 단계와 수직 동기화가 프로젝트 설정 파일에 남으므로, 테스트마다 바꾸기 전으로 돌려놓는다.
        [TearDown]
        public void TearDown()
        {
            GraphicsQuality.Restore();
        }

        [Test]
        public void 품질은_단계의_이름으로_찾는다()
        {
            string[] names = { "Mobile", "PC", "PC Low", "PC High" };
            Assert.AreEqual(2, GraphicsQuality.FindLevel(names, GameSettings.QualityLow));
            Assert.AreEqual(1, GraphicsQuality.FindLevel(names, GameSettings.QualityNormal));
            Assert.AreEqual(3, GraphicsQuality.FindLevel(names, GameSettings.QualityHigh));

            // 차례가 달라도 이름으로 찾는다.
            string[] shuffled = { "PC High", "PC Low", "PC" };
            Assert.AreEqual(1, GraphicsQuality.FindLevel(shuffled, GameSettings.QualityLow));
            Assert.AreEqual(0, GraphicsQuality.FindLevel(shuffled, GameSettings.QualityHigh));
        }

        [Test]
        public void 없는_단계와_범위_밖의_품질은_찾지_못한다()
        {
            // Quest처럼 PC의 단계가 없는 기기.
            string[] mobileOnly = { "Mobile" };
            Assert.AreEqual(-1, GraphicsQuality.FindLevel(mobileOnly, GameSettings.QualityNormal));
            Assert.AreEqual(-1, GraphicsQuality.FindLevel(null, GameSettings.QualityNormal));
            Assert.AreEqual(-1, GraphicsQuality.FindLevel(new[] { "PC" }, -1));
            Assert.AreEqual(-1, GraphicsQuality.FindLevel(new[] { "PC" }, 3));

            Assert.AreEqual(GameSettings.QualityHigh, GraphicsQuality.QualityOf("PC High"));
            Assert.AreEqual(-1, GraphicsQuality.QualityOf("Mobile"));
            Assert.AreEqual(GraphicsQuality.Count, GraphicsQuality.Labels.Length);
        }

        [Test]
        public void 프로젝트에_세_단계가_있고_저마다_다른_그리기_설정을_쓴다()
        {
            string[] names = QualitySettings.names;
            var pipelines = new HashSet<Object>();

            for (int quality = 0; quality < GraphicsQuality.Count; quality++)
            {
                int level = GraphicsQuality.FindLevel(names, quality);
                Assert.GreaterOrEqual(level, 0, $"품질 단계 '{GraphicsQuality.LevelNames[quality]}'이 없습니다. 20일차 셋업을 실행하세요.");

                Object pipeline = QualitySettings.GetRenderPipelineAssetAt(level);
                Assert.IsNotNull(pipeline, $"'{GraphicsQuality.LevelNames[quality]}' 단계에 그리기 설정이 이어져 있지 않습니다.");
                Assert.IsTrue(pipelines.Add(pipeline), "두 단계가 같은 그리기 설정을 씁니다.");
            }

            Assert.AreEqual(Day20Setup.NormalPipelinePath, AssetDatabase.GetAssetPath(QualitySettings.GetRenderPipelineAssetAt(GraphicsQuality.FindLevel(names, GameSettings.QualityNormal))),
                "보통은 19일차까지 쓰던 그리기 설정이어야 화면의 모습이 바뀌지 않습니다.");
        }

        [Test]
        public void 품질이_높을수록_더_또렷하게_그린다()
        {
            SerializedObject low = Pipeline(Day20Setup.LowPipelinePath);
            SerializedObject normal = Pipeline(Day20Setup.NormalPipelinePath);
            SerializedObject high = Pipeline(Day20Setup.HighPipelinePath);

            Assert.Less(low.FindProperty("m_RenderScale").floatValue, normal.FindProperty("m_RenderScale").floatValue, "낮음은 더 작게 그려 늘려 보입니다.");
            Assert.AreEqual(1f, normal.FindProperty("m_RenderScale").floatValue, 0.001f);
            Assert.AreEqual(1f, high.FindProperty("m_RenderScale").floatValue, 0.001f);

            Assert.Less(low.FindProperty("m_MainLightShadowmapResolution").intValue, normal.FindProperty("m_MainLightShadowmapResolution").intValue);
            Assert.Less(normal.FindProperty("m_MainLightShadowmapResolution").intValue, high.FindProperty("m_MainLightShadowmapResolution").intValue);

            Assert.Greater(high.FindProperty("m_MSAA").intValue, normal.FindProperty("m_MSAA").intValue, "높음은 가장자리를 매끄럽게 합니다.");
            Assert.IsFalse(low.FindProperty("m_SoftShadowsSupported").boolValue);
            Assert.IsTrue(high.FindProperty("m_SoftShadowsSupported").boolValue);
        }

        [Test]
        public void 세_단계_모두_수직_동기화를_켠_것으로_적혀_있다()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(QualitySettingsPath)[0]);
            SerializedProperty levels = settings.FindProperty("m_QualitySettings");
            int found = 0;

            for (int i = 0; i < levels.arraySize; i++)
            {
                SerializedProperty level = levels.GetArrayElementAtIndex(i);
                if (GraphicsQuality.QualityOf(level.FindPropertyRelative("name").stringValue) < 0) continue;

                found++;
                Assert.AreEqual(1, level.FindPropertyRelative("vSyncCount").intValue, "설정의 처음 값(켬)과 같아야 합니다.");
            }

            Assert.AreEqual(GraphicsQuality.Count, found);
        }

        [Test]
        public void 품질과_수직_동기화를_적용하고_이미_그_값이면_건드리지_않는다()
        {
            GraphicsQuality.Apply(GameSettings.QualityNormal, true);

            Assert.IsTrue(GraphicsQuality.Apply(GameSettings.QualityLow, false));
            Assert.AreEqual(GameSettings.QualityLow, GraphicsQuality.Current);
            Assert.AreEqual("PC Low", GraphicsQuality.CurrentLevelName);
            Assert.AreEqual(0, QualitySettings.vSyncCount);
            Assert.IsFalse(GraphicsQuality.Apply(GameSettings.QualityLow, false), "같은 값을 다시 적용했는데 바뀌었다고 했습니다.");

            // 단계를 바꾸면 그 단계에 적힌 값(켬)으로 돌아가므로, 끈 것을 다시 맞춰야 한다.
            Assert.IsTrue(GraphicsQuality.Apply(GameSettings.QualityHigh, false));
            Assert.AreEqual(GameSettings.QualityHigh, GraphicsQuality.Current);
            Assert.AreEqual(0, QualitySettings.vSyncCount, "품질을 바꾼 뒤에 수직 동기화가 다시 켜졌습니다.");

            Assert.IsTrue(GraphicsQuality.Apply(GameSettings.QualityHigh, true));
            Assert.AreEqual(1, QualitySettings.vSyncCount);
        }

        [Test]
        public void 돌려놓으면_단계와_단계마다의_수직_동기화가_바꾸기_전과_같다()
        {
            int startLevel = QualitySettings.GetQualityLevel();
            int[] before = VSyncOfEachLevel();

            GraphicsQuality.Apply(GameSettings.QualityLow, false);
            GraphicsQuality.Apply(GameSettings.QualityHigh, false);
            GraphicsQuality.Apply(GameSettings.QualityNormal, false);
            GraphicsQuality.Apply(GameSettings.QualityHigh, false);
            GraphicsQuality.Restore();

            Assert.AreEqual(startLevel, QualitySettings.GetQualityLevel(), "처음의 단계로 돌아오지 않았습니다.");
            CollectionAssert.AreEqual(before, VSyncOfEachLevel(), "지나간 단계에 끈 수직 동기화가 남았습니다. 에디터에서는 이 값이 프로젝트 설정 파일에 적힙니다.");
        }

        [Test]
        public void 명령줄의_품질을_읽는다()
        {
            Assert.AreEqual(GameSettings.QualityLow, GraphicsApplier.ParseQuality(new[] { "game.exe", "-quality", "low" }));
            Assert.AreEqual(GameSettings.QualityNormal, GraphicsApplier.ParseQuality(new[] { "game.exe", "-quality", "1" }));
            Assert.AreEqual(GameSettings.QualityHigh, GraphicsApplier.ParseQuality(new[] { "-quitAfter", "5", "-quality", "HIGH" }));

            Assert.AreEqual(-1, GraphicsApplier.ParseQuality(new[] { "game.exe" }));
            Assert.AreEqual(-1, GraphicsApplier.ParseQuality(new[] { "game.exe", "-quality", "ultra" }));
            Assert.AreEqual(-1, GraphicsApplier.ParseQuality(new[] { "game.exe", "-quality" }), "값이 없으면 읽지 않습니다.");
            Assert.AreEqual(-1, GraphicsApplier.ParseQuality(null));
        }

        /// <summary>품질 설정에 단계마다 적힌 수직 동기화 값. 실행 중에 바꾼 값도 여기에 보인다.</summary>
        private static int[] VSyncOfEachLevel()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(QualitySettingsPath)[0]);
            SerializedProperty levels = settings.FindProperty("m_QualitySettings");
            var values = new int[levels.arraySize];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = levels.GetArrayElementAtIndex(i).FindPropertyRelative("vSyncCount").intValue;
            }

            return values;
        }

        private static SerializedObject Pipeline(string path)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            Assert.IsNotNull(asset, $"그리기 설정이 없습니다: {path}. 20일차 셋업을 실행하세요.");
            return new SerializedObject(asset);
        }
    }
}
