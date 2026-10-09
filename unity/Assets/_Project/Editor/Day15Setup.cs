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
    /// 15일차 구성을 한 번에 적용한다: 돌리기·옮기기·맞추기의 안내와 상태 표시, 부품 판의 맞추기 단추가 든 게임 화면을 다시 조립한다.
    /// 입력 자산의 돌리기(Rotate)·옮기기(Grab)·맞추기(Snap) 동작은 파일에 직접 들어 있고, 여기서는 있는지만 확인한다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 14일차까지의 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/15일차 셋업 실행
    /// </summary>
    public static class Day15Setup
    {
        private const string Root = "Assets/_Project";
        private const string GameUiPrefabPath = Root + "/Prefabs/GameUI.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string CatalogPath = Root + "/Data/PartCatalog.asset";
        private const string GameUiName = "GameUI";

        // 블록 놓기가 읽는 동작. 맞추기 단계를 바꾸는 키는 PC에만 있고, VR에서는 부품 판의 단추로 바꾼다.
        private static readonly string[] RequiredActions =
        {
            "Player/Rotate", "Player/Grab", "Player/Snap",
            "XR/Rotate", "XR/Grab",
        };

        [MenuItem("Atelier Verse/15일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "15일차 셋업 완료\n\n- 놓을 블록 돌리기(R·T)와 블록 옮기기(G)\n- 맞추기 도우미(C, 끈 것이 기본)\n- VR: 부품 판의 맞추기 단추",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            InputActionAsset actions = LoadRequired<InputActionAsset>(InputPath);
            foreach (string path in RequiredActions)
            {
                if (actions.FindAction(path) == null) throw new InvalidOperationException($"입력 자산에 {path} 동작이 없습니다. AtelierInput.inputactions를 확인하세요.");
            }

            UiFactory.LoadShared();
            CreateGameUiPrefab(actions, LoadRequired<PartCatalog>(CatalogPath));
            AssetDatabase.SaveAssets();

            UpdateSandboxScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
