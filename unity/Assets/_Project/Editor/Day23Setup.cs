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
    /// 23일차 구성을 한 번에 적용한다: 씬에 맵의 분위기와 바닥 크기를 보이는 부품(WorldEnvironment)을 두어 해·바닥·블록 세계에 잇고,
    /// 맵 정보 창에 "분위기와 크기" 갈래가 든 게임 화면을 다시 조립한다.
    /// 바닥은 실행 중에 넓이가 바뀌므로 움직이지 않는 물체라는 표시(static)를 뗀다. 그 표시가 있으면 빌드에서 넓이가 바뀌지 않을 수 있다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 22일차까지의 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/23일차 셋업 실행
    /// </summary>
    public static class Day23Setup
    {
        private const string Root = "Assets/_Project";
        private const string GameUiPrefabPath = Root + "/Prefabs/GameUI.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string CatalogPath = Root + "/Data/PartCatalog.asset";
        private const string GameUiName = "GameUI";
        private const string SunName = "Sun";
        private const string FloorName = "GridFloor";

        [MenuItem("Atelier Verse/23일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "23일차 셋업 완료\n\n- 맵의 분위기: 하늘, 해의 방향과 높이\n- 맵의 크기: 바닥 16·24·32\n- 맵 정보 창의 \"분위기와 크기\" 갈래",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            InputActionAsset actions = LoadRequired<InputActionAsset>(InputPath);

            UiFactory.LoadShared();
            GameObject root = GameUiBuilder.Build(actions, GameUiBuilder.LoadIcons(), LoadRequired<PartCatalog>(CatalogPath), GameUiBuilder.LoadShapeSprites());
            PrefabUtility.SaveAsPrefabAsset(root, GameUiPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            UpdateSandboxScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>Sandbox 씬에 분위기 부품을 두어 잇고, 게임 화면을 새 프리팹으로 바꾼다.</summary>
        private static void UpdateSandboxScene()
        {
            if (!File.Exists(SandboxScene)) throw new InvalidOperationException($"씬을 찾을 수 없습니다: {SandboxScene}. 1일차 셋업을 먼저 실행하세요.");

            Scene scene = EditorSceneManager.OpenScene(SandboxScene, OpenSceneMode.Single);
            WireEnvironment();

            GameObject gameUi = LoadRequired<GameObject>(GameUiPrefabPath);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == GameUiName) UnityEngine.Object.DestroyImmediate(root);
            }

            PrefabUtility.InstantiatePrefab(gameUi, scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// 분위기 부품을 블록 세계가 있는 물체에 두고 해·바닥·블록 세계에 잇는다. 자동 저장도 이 부품을 알게 한다.
        /// </summary>
        private static void WireEnvironment()
        {
            BlockWorld world = UnityEngine.Object.FindAnyObjectByType<BlockWorld>();
            MapAutoSave autoSave = UnityEngine.Object.FindAnyObjectByType<MapAutoSave>();
            if (world == null || autoSave == null) throw new InvalidOperationException("씬에 블록 세계나 자동 저장이 없습니다. 4일차 셋업을 먼저 실행하세요.");

            Light sun = FindNamed<Light>(SunName);
            Renderer floor = FindNamed<Renderer>(FloorName);
            if (sun == null || floor == null) throw new InvalidOperationException($"씬에서 해({SunName})나 바닥({FloorName})을 찾을 수 없습니다. 1일차 셋업을 먼저 실행하세요.");

            // 바닥의 넓이를 실행 중에 바꾸므로 움직이지 않는 물체라는 표시를 뗀다.
            GameObjectUtility.SetStaticEditorFlags(floor.gameObject, 0);

            WorldEnvironment environment = world.GetComponent<WorldEnvironment>();
            if (environment == null) environment = world.gameObject.AddComponent<WorldEnvironment>();

            var serialized = new SerializedObject(environment);
            serialized.FindProperty("world").objectReferenceValue = world;
            serialized.FindProperty("sun").objectReferenceValue = sun;
            serialized.FindProperty("floor").objectReferenceValue = floor;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var save = new SerializedObject(autoSave);
            save.FindProperty("environment").objectReferenceValue = environment;
            save.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T FindNamed<T>(string name) where T : Component
        {
            foreach (T candidate in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == name) return candidate;
            }

            return null;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException($"자산을 불러오지 못했습니다: {path}. 앞 일차의 셋업을 먼저 실행하세요.");
            return asset;
        }
    }
}
