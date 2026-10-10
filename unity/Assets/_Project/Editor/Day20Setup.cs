using System;
using System.IO;
using AtelierVerse.Core;
using AtelierVerse.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 20일차 구성을 한 번에 적용한다: 화면 품질 세 단계(낮음·보통·높음)의 그리기 설정과 품질 단계를 만들고,
    /// 갈래로 나눈 설정 쪽(화면·조작·소리)이 든 게임 화면을 다시 조립한다.
    /// "보통"은 19일차까지 쓰던 단계(PC)를 그대로 쓰므로 화면의 모습이 바뀌지 않는다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 19일차까지의 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/20일차 셋업 실행
    /// </summary>
    public static class Day20Setup
    {
        private const string Root = "Assets/_Project";
        private const string GameUiPrefabPath = Root + "/Prefabs/GameUI.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string CatalogPath = Root + "/Data/PartCatalog.asset";
        private const string GameUiName = "GameUI";

        private const string QualitySettingsPath = "ProjectSettings/QualitySettings.asset";

        /// <summary>보통 품질의 그리기 설정. 19일차까지 쓰던 것이며 여기서는 고치지 않는다.</summary>
        public const string NormalPipelinePath = "Assets/Settings/PC_RPAsset.asset";
        public const string LowPipelinePath = "Assets/Settings/PC_Low_RPAsset.asset";
        public const string HighPipelinePath = "Assets/Settings/PC_High_RPAsset.asset";

        [MenuItem("Atelier Verse/20일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "20일차 셋업 완료\n\n- 화면 품질 세 단계(낮음·보통·높음)\n- 설정의 갈래: 화면, 조작, 소리\n- 화면 방식, 수직 동기화, 위아래 시점 반대로",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            InputActionAsset actions = LoadRequired<InputActionAsset>(InputPath);

            UnityEngine.Object low = EnsurePipeline(LowPipelinePath, 0.75f, 1, 1024, 30f, 1, false);
            UnityEngine.Object high = EnsurePipeline(HighPipelinePath, 1f, 4, 4096, 70f, 4, true);
            AssetDatabase.SaveAssets();
            ApplyQualityLevels(low, high);

            UiFactory.LoadShared();
            GameObject root = GameUiBuilder.Build(actions, GameUiBuilder.LoadIcons(), LoadRequired<PartCatalog>(CatalogPath), GameUiBuilder.LoadShapeSprites());
            PrefabUtility.SaveAsPrefabAsset(root, GameUiPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            UpdateSandboxScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 품질 하나의 그리기 설정을 만든다. 보통 품질의 것을 베껴서 품질마다 다른 값만 고친다.
        /// 이미 있으면 새로 베끼지 않고 값만 다시 넣는다(자산의 번호가 바뀌면 품질 단계의 연결이 끊긴다).
        /// </summary>
        private static UnityEngine.Object EnsurePipeline(string path, float renderScale, int msaa, int shadowResolution, float shadowDistance, int cascades, bool softShadows)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(NormalPipelinePath) == null)
                {
                    throw new InvalidOperationException($"보통 품질의 그리기 설정을 찾을 수 없습니다: {NormalPipelinePath}");
                }

                if (!AssetDatabase.CopyAsset(NormalPipelinePath, path)) throw new InvalidOperationException($"그리기 설정을 베끼지 못했습니다: {path}");
            }

            UnityEngine.Object pipeline = AssetDatabase.LoadMainAssetAtPath(path);
            var serialized = new SerializedObject(pipeline);
            Require(serialized, "m_RenderScale").floatValue = renderScale;
            Require(serialized, "m_MSAA").intValue = msaa;
            Require(serialized, "m_MainLightShadowmapResolution").intValue = shadowResolution;
            Require(serialized, "m_ShadowDistance").floatValue = shadowDistance;
            Require(serialized, "m_ShadowCascadeCount").intValue = cascades;
            Require(serialized, "m_SoftShadowsSupported").boolValue = softShadows;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            return pipeline;
        }

        /// <summary>
        /// 품질 단계 "PC Low"와 "PC High"를 만들고 그리기 설정을 잇는다. 새 단계는 "PC"를 베껴 목록의 끝에 더하므로
        /// 앞 단계의 번호(기기마다의 기본 단계가 가리키는 번호)는 바뀌지 않는다.
        /// 세 단계 모두 수직 동기화를 켠 것으로 적어 둔다. 설정의 처음 값(켬)과 같게 해 두는 것이다.
        /// </summary>
        private static void ApplyQualityLevels(UnityEngine.Object low, UnityEngine.Object high)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(QualitySettingsPath);
            if (assets == null || assets.Length == 0) throw new InvalidOperationException($"품질 설정을 불러오지 못했습니다: {QualitySettingsPath}");

            var serialized = new SerializedObject(assets[0]);
            SerializedProperty levels = Require(serialized, "m_QualitySettings");

            string normalName = GraphicsQuality.LevelNames[GameSettings.QualityNormal];
            if (FindLevel(levels, normalName) < 0) throw new InvalidOperationException($"품질 단계 '{normalName}'을 찾을 수 없습니다.");

            EnsureLevel(levels, normalName, GraphicsQuality.LevelNames[GameSettings.QualityLow], low);
            EnsureLevel(levels, normalName, GraphicsQuality.LevelNames[GameSettings.QualityHigh], high);

            foreach (string name in GraphicsQuality.LevelNames)
            {
                levels.GetArrayElementAtIndex(FindLevel(levels, name)).FindPropertyRelative("vSyncCount").intValue = 1;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureLevel(SerializedProperty levels, string sourceName, string name, UnityEngine.Object pipeline)
        {
            int index = FindLevel(levels, name);
            if (index < 0)
            {
                // 베낄 단계의 바로 뒤에 같은 것이 하나 더 생긴다. 그것을 끝으로 옮겨 앞 단계의 번호를 지킨다.
                int source = FindLevel(levels, sourceName);
                levels.InsertArrayElementAtIndex(source);
                levels.MoveArrayElement(source + 1, levels.arraySize - 1);
                index = levels.arraySize - 1;
            }

            SerializedProperty level = levels.GetArrayElementAtIndex(index);
            level.FindPropertyRelative("name").stringValue = name;
            level.FindPropertyRelative("customRenderPipeline").objectReferenceValue = pipeline;
        }

        private static int FindLevel(SerializedProperty levels, string name)
        {
            for (int i = 0; i < levels.arraySize; i++)
            {
                if (levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name) return i;
            }

            return -1;
        }

        private static SerializedProperty Require(SerializedObject serialized, string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new InvalidOperationException($"{serialized.targetObject.name}에 {name} 항목이 없습니다. Unity나 URP의 판이 바뀌었는지 확인하세요.");
            return property;
        }

        /// <summary>Sandbox 씬의 게임 화면을 새 프리팹으로 바꾼다.</summary>
        private static void UpdateSandboxScene()
        {
            if (!File.Exists(SandboxScene)) throw new InvalidOperationException($"씬을 찾을 수 없습니다: {SandboxScene}. 1일차 셋업을 먼저 실행하세요.");

            Scene scene = EditorSceneManager.OpenScene(SandboxScene, OpenSceneMode.Single);
            GameObject gameUi = LoadRequired<GameObject>(GameUiPrefabPath);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == GameUiName) UnityEngine.Object.DestroyImmediate(root);
            }

            PrefabUtility.InstantiatePrefab(gameUi, scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException($"자산을 불러오지 못했습니다: {path}. 앞 일차의 셋업을 먼저 실행하세요.");
            return asset;
        }
    }
}
