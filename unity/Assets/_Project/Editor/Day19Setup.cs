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
    /// 19일차 구성을 한 번에 적용한다: 소리를 내는 부품(SfxPlayer)과 소리를 고르는 부품(GameSounds),
    /// 설정의 소리 크기 줄이 든 게임 화면을 다시 조립한다. 소리는 실행 중에 코드로 만들므로 소리 파일은 만들지 않는다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 18일차까지의 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/19일차 셋업 실행
    /// </summary>
    public static class Day19Setup
    {
        private const string Root = "Assets/_Project";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player_Desktop.prefab";
        private const string GameUiPrefabPath = Root + "/Prefabs/GameUI.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string CatalogPath = Root + "/Data/PartCatalog.asset";
        private const string GameUiName = "GameUI";

        [MenuItem("Atelier Verse/19일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "19일차 셋업 완료\n\n- 기본 소리: 발소리, 블록 놓기·지우기·칠하기, 단추, 알림\n- 설정의 소리 크기",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            InputActionAsset actions = LoadRequired<InputActionAsset>(InputPath);
            RequireListener();

            UiFactory.LoadShared();
            GameObject root = GameUiBuilder.Build(actions, GameUiBuilder.LoadIcons(), LoadRequired<PartCatalog>(CatalogPath), GameUiBuilder.LoadShapeSprites());
            PrefabUtility.SaveAsPrefabAsset(root, GameUiPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            UpdateSandboxScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>소리는 캐릭터의 카메라에 붙은 "듣는 귀"(AudioListener)로 듣는다. 없으면 아무 소리도 들리지 않으므로 여기서 확인한다.</summary>
        private static void RequireListener()
        {
            GameObject player = LoadRequired<GameObject>(PlayerPrefabPath);
            AudioListener[] listeners = player.GetComponentsInChildren<AudioListener>(true);
            if (listeners.Length != 1)
            {
                throw new InvalidOperationException($"캐릭터 프리팹의 AudioListener가 {listeners.Length}개입니다. 카메라에 하나만 있어야 합니다. 2일차 셋업을 확인하세요.");
            }
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
