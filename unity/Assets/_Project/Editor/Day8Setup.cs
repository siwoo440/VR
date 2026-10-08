using System;
using System.IO;
using AtelierVerse.World;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 8일차 구성을 한 번에 적용한다: 플레이어 설정(창 모드, 크기, 스크립팅 백엔드)을 정하고, 메뉴에 버전이 보이는 게임 화면을 다시 조립한다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 7일차 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/8일차 셋업 실행
    /// </summary>
    public static class Day8Setup
    {
        private const string Root = "Assets/_Project";
        private const string CatalogPath = Root + "/Data/PartCatalog.asset";
        private const string GameUiPrefabPath = Root + "/Prefabs/GameUI.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string GameUiName = "GameUI";
        private const int WindowWidth = 1600;
        private const int WindowHeight = 900;

        [MenuItem("Atelier Verse/8일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "8일차 셋업 완료\n\n- 창 모드 1600×900, 크기 조절 가능, 뒤에서도 실행\n- 스크립팅 백엔드 Mono\n- 메뉴에 버전 표시",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            ApplyPlayerSettings();

            UiFactory.LoadShared();
            InputActionAsset actions = LoadRequired<InputActionAsset>(InputPath);
            PartCatalog catalog = LoadRequired<PartCatalog>(CatalogPath);
            CreateGameUiPrefab(actions, catalog);
            AssetDatabase.SaveAssets();

            UpdateSandboxScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 실행 파일의 창 설정. 전체 화면 창이 아니라 크기를 바꿀 수 있는 보통 창으로 열고,
        /// 창이 뒤로 가도 실행을 멈추지 않게 한다(자동 저장과 뒤 단계의 여러 사람 접속 때문).
        /// </summary>
        private static void ApplyPlayerSettings()
        {
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = WindowWidth;
            PlayerSettings.defaultScreenHeight = WindowHeight;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        }

        private static void CreateGameUiPrefab(InputActionAsset actions, PartCatalog catalog)
        {
            var items = new GameUiBuilder.Item[catalog.Count];
            for (int i = 0; i < items.Length; i++)
            {
                PartCatalog.Part part = catalog.Get(i);
                items[i] = new GameUiBuilder.Item(part.displayName, part.color);
            }

            GameObject root = GameUiBuilder.Build(actions, GameUiBuilder.LoadIcons(), items);
            PrefabUtility.SaveAsPrefabAsset(root, GameUiPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
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
