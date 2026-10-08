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
    /// 4일차 구성을 한 번에 적용한다: 블록 세계에 자동 저장을 붙이고, 저장 표시가 든 게임 화면을 다시 조립한다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 3일차 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/4일차 셋업 실행
    /// </summary>
    public static class Day4Setup
    {
        private const string Root = "Assets/_Project";
        private const string CatalogPath = Root + "/Data/PartCatalog.asset";
        private const string GameUiPrefabPath = Root + "/Prefabs/GameUI.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string WorldName = "BlockWorld";
        private const string GameUiName = "GameUI";
        private const string MapName = "시험 작업실";

        [MenuItem("Atelier Verse/4일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "4일차 셋업 완료\n\n- 블록 세계에 자동 저장 추가\n- 게임 화면에 저장 표시 추가",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            UiFactory.LoadShared();

            InputActionAsset actions = LoadRequired<InputActionAsset>(InputPath);
            PartCatalog catalog = LoadRequired<PartCatalog>(CatalogPath);

            CreateGameUiPrefab(actions, catalog);
            AssetDatabase.SaveAssets();

            UpdateSandboxScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>저장 표시가 더해진 게임 화면을 다시 조립한다. 부품 칸의 이름과 색은 3일차와 같이 부품 목록에서 온다.</summary>
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

        /// <summary>Sandbox 씬의 블록 세계에 자동 저장을 붙이고, 게임 화면을 새 프리팹으로 바꾼다.</summary>
        private static void UpdateSandboxScene()
        {
            if (!File.Exists(SandboxScene)) throw new InvalidOperationException($"씬을 찾을 수 없습니다: {SandboxScene}. 1일차 셋업을 먼저 실행하세요.");

            Scene scene = EditorSceneManager.OpenScene(SandboxScene, OpenSceneMode.Single);
            GameObject gameUi = LoadRequired<GameObject>(GameUiPrefabPath);
            GameObject worldObject = null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == GameUiName) UnityEngine.Object.DestroyImmediate(root);
                else if (root.name == WorldName) worldObject = root;
            }

            if (worldObject == null) throw new InvalidOperationException("씬에 블록 세계(BlockWorld)가 없습니다. 3일차 셋업을 먼저 실행하세요.");

            BlockWorld world = worldObject.GetComponent<BlockWorld>();
            MapAutoSave autoSave = worldObject.GetComponent<MapAutoSave>();
            if (autoSave == null) autoSave = worldObject.AddComponent<MapAutoSave>();

            var serialized = new SerializedObject(autoSave);
            serialized.FindProperty("world").objectReferenceValue = world;
            serialized.FindProperty("mapName").stringValue = MapName;
            serialized.ApplyModifiedPropertiesWithoutUndo();

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
