using System;
using System.IO;
using AtelierVerse.Player;
using AtelierVerse.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 13일차 구성을 한 번에 적용한다: VR에서 왼손 위에 뜨는 부품 판과 VR의 만들기 안내가 든 게임 화면을 다시 조립하고,
    /// 캐릭터 프리팹을 다시 이어 조작 방식 고르기가 더는 블록 놓기를 끄지 않게 한다.
    /// 입력 자산의 XR 묶음에 든 놓기·지우기·칠하기는 파일에 직접 들어 있다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 12일차까지의 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/13일차 셋업 실행
    /// </summary>
    public static class Day13Setup
    {
        private const string Root = "Assets/_Project";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player_Desktop.prefab";
        private const string GameUiPrefabPath = Root + "/Prefabs/GameUI.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string CatalogPath = Root + "/Data/PartCatalog.asset";
        private const string HandMaterialPath = Root + "/Art/Materials/Block_Ivory.mat";
        private const string PointerMaterialPath = Root + "/Art/Materials/PointerRay.mat";
        private const string GameUiName = "GameUI";

        // VR에서 만들 때 읽는 동작. PC의 Player 묶음에 있는 것과 같은 이름으로 XR 묶음에도 있어야 한다.
        private static readonly string[] RequiredActions =
        {
            "XR/Place", "XR/Remove", "XR/Paint",
            "Player/Place", "Player/Remove", "Player/Paint",
        };

        [MenuItem("Atelier Verse/13일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "13일차 셋업 완료\n\n- VR에서 오른손으로 가리켜 블록 놓기·지우기·칠하기\n- 왼손 위의 부품 판(부품 고르기, 되돌리기, 상태 표시)",
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

            UpdatePlayerPrefab(actions);

            UiFactory.LoadShared();
            CreateGameUiPrefab(actions, LoadRequired<PartCatalog>(CatalogPath));
            AssetDatabase.SaveAssets();

            UpdateSandboxScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>캐릭터 프리팹의 VR 쪽을 다시 잇는다. 조작 방식 고르기에서 빠진 항목이 프리팹에서도 지워진다.</summary>
        private static void UpdatePlayerPrefab(InputActionAsset actions)
        {
            if (!File.Exists(PlayerPrefabPath)) throw new InvalidOperationException($"프리팹을 찾을 수 없습니다: {PlayerPrefabPath}. 2일차 셋업을 먼저 실행하세요.");

            Material handMaterial = LoadRequired<Material>(HandMaterialPath);
            Material pointerMaterial = LoadRequired<Material>(PointerMaterialPath);

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (root.GetComponent<LocalPlayer>() == null) throw new InvalidOperationException("캐릭터 프리팹에 LocalPlayer가 없습니다. 12일차 셋업을 먼저 실행하세요.");

                Camera camera = root.GetComponentInChildren<Camera>(true);
                AvatarView avatar = root.GetComponentInChildren<AvatarView>(true);
                PlayerWiring.ApplyXr(root, actions, camera, avatar, handMaterial);
                PlayerWiring.ApplyLocal(root, pointerMaterial);

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
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
