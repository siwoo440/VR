using System;
using System.IO;
using AtelierVerse.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 17일차 구성을 한 번에 적용한다: 맵 목록 창이 든 게임 화면을 다시 조립하고,
    /// Sandbox 씬의 자동 저장에 "처음부터 놓여 있는 꾸밈"(나무, 지붕)을 알려 주어 새로 만든 맵에서는 감추게 한다.
    /// 입력 자산의 맵 목록 열기(Game/Maps)는 파일에 직접 들어 있고, 여기서는 있는지만 확인한다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 16일차까지의 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/17일차 셋업 실행
    /// </summary>
    public static class Day17Setup
    {
        private const string Root = "Assets/_Project";
        private const string GameUiPrefabPath = Root + "/Prefabs/GameUI.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string CatalogPath = Root + "/Data/PartCatalog.asset";
        private const string GameUiName = "GameUI";

        // 1일차 셋업이 만든 씬의 뿌리와, 그 아래에서 블록이 아닌 꾸밈(지붕 판, 나무)이 남아 있는 묶음.
        private const string SceneryRootName = "Day1_Sandbox";
        private const string SceneryName = "Blocks";

        [MenuItem("Atelier Verse/17일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "17일차 셋업 완료\n\n- 여러 맵 다루기: 맵 목록, 새 맵, 이름 바꾸기, 지우기\n- 메뉴의 \"내 작업실\" 타일과 M 키로 엶",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            InputActionAsset actions = LoadRequired<InputActionAsset>(InputPath);
            if (actions.FindAction("Game/Maps") == null) throw new InvalidOperationException("입력 자산에 Game/Maps 동작이 없습니다. AtelierInput.inputactions를 확인하세요.");

            UiFactory.LoadShared();
            GameObject root = GameUiBuilder.Build(actions, GameUiBuilder.LoadIcons(), LoadRequired<PartCatalog>(CatalogPath), GameUiBuilder.LoadShapeSprites());
            PrefabUtility.SaveAsPrefabAsset(root, GameUiPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            UpdateSandboxScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>Sandbox 씬의 게임 화면을 새 프리팹으로 바꾸고, 자동 저장에 꾸밈 묶음을 이어 준다.</summary>
        private static void UpdateSandboxScene()
        {
            if (!File.Exists(SandboxScene)) throw new InvalidOperationException($"씬을 찾을 수 없습니다: {SandboxScene}. 1일차 셋업을 먼저 실행하세요.");

            Scene scene = EditorSceneManager.OpenScene(SandboxScene, OpenSceneMode.Single);
            GameObject gameUi = LoadRequired<GameObject>(GameUiPrefabPath);
            GameObject scenery = null;
            MapAutoSave autoSave = null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == GameUiName)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    continue;
                }

                if (root.name == SceneryRootName)
                {
                    Transform found = root.transform.Find(SceneryName);
                    if (found != null) scenery = found.gameObject;
                }

                if (autoSave == null) autoSave = root.GetComponentInChildren<MapAutoSave>(true);
            }

            if (autoSave == null) throw new InvalidOperationException("씬에 자동 저장(MapAutoSave)이 없습니다. 4일차 셋업을 먼저 실행하세요.");
            if (scenery == null) throw new InvalidOperationException($"씬에서 꾸밈 묶음({SceneryRootName}/{SceneryName})을 찾을 수 없습니다. 1일차 셋업을 먼저 실행하세요.");

            var serialized = new SerializedObject(autoSave);
            serialized.FindProperty("sampleScenery").objectReferenceValue = scenery;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(autoSave);

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
