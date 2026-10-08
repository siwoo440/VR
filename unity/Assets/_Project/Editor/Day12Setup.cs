using System;
using System.IO;
using AtelierVerse.Core;
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
    /// 12일차 구성을 한 번에 적용한다: 캐릭터에 "이 기기의 캐릭터"(LocalPlayer)와 오른손 광선을 더하고,
    /// VR에서 눈앞의 판으로 뜨는 게임 화면(손 광선으로 누르기, VR용 메뉴 안내)을 다시 조립한다.
    /// 입력 자산의 UI 묶음과 오른손이 가리키는 자세, VR의 메뉴 단추는 파일에 직접 들어 있다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 11일차까지의 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/12일차 셋업 실행
    /// </summary>
    public static class Day12Setup
    {
        private const string Root = "Assets/_Project";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player_Desktop.prefab";
        private const string GameUiPrefabPath = Root + "/Prefabs/GameUI.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string CatalogPath = Root + "/Data/PartCatalog.asset";
        private const string PointerMaterialPath = Root + "/Art/Materials/PointerRay.mat";
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";
        private const string GameUiName = "GameUI";

        // 화면 입력과 손 광선에 필요한 동작. 입력 자산에 없으면 셋업을 멈춘다.
        private static readonly string[] RequiredActions =
        {
            "UI/Point", "UI/Click", "UI/TrackedDevicePosition", "UI/TrackedDeviceOrientation",
            "XR/RightPointerPosition", "XR/RightPointerRotation",
        };

        [MenuItem("Atelier Verse/12일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "12일차 셋업 완료\n\n- 캐릭터에 이 기기의 캐릭터(LocalPlayer)와 오른손 광선 추가\n- VR에서 눈앞에 뜨는 메뉴와 알림\n- 화면 입력을 프로젝트의 입력 자산에 연결",
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

            UpdatePlayerPrefab(CreatePointerMaterial());

            UiFactory.LoadShared();
            CreateGameUiPrefab(actions, LoadRequired<PartCatalog>(CatalogPath));
            AssetDatabase.SaveAssets();

            UpdateSandboxScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>오른손 광선의 재질. 빛을 받지 않는 골드색이라 어두운 곳에서도 보인다.</summary>
        private static Material CreatePointerMaterial()
        {
            Shader shader = Shader.Find(UnlitShaderName);
            if (shader == null) throw new InvalidOperationException($"셰이더를 찾을 수 없습니다: {UnlitShaderName}");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(PointerMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, PointerMaterialPath);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", AtelierPalette.Gold);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void UpdatePlayerPrefab(Material pointerMaterial)
        {
            if (!File.Exists(PlayerPrefabPath)) throw new InvalidOperationException($"프리팹을 찾을 수 없습니다: {PlayerPrefabPath}. 2일차 셋업을 먼저 실행하세요.");

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (root.GetComponent<XrRig>() == null) throw new InvalidOperationException("캐릭터 프리팹에 VR 리그(XrRig)가 없습니다. 10일차 셋업을 먼저 실행하세요.");

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
