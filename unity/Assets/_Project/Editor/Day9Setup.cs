using System;
using System.IO;
using AtelierVerse.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 9일차 구성을 한 번에 적용한다: PC 캐릭터 프리팹에 몸(CharacterMotor)과 카메라 리그(ViewRig)를 붙이고 PC 조작과 잇는다.
    /// 캐릭터의 모양과 Sandbox 씬은 바꾸지 않는다. 씬의 캐릭터는 프리팹을 따라 함께 바뀐다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 3일차 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/9일차 셋업 실행
    /// </summary>
    public static class Day9Setup
    {
        private const string Root = "Assets/_Project";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player_Desktop.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";

        [MenuItem("Atelier Verse/9일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "9일차 셋업 완료\n\n- PC 캐릭터를 몸(모터)·카메라 리그·PC 조작으로 나눔\n- 게임에서의 동작은 그대로",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (actions == null) throw new InvalidOperationException($"입력 자산을 찾을 수 없습니다: {InputPath}");
            if (!File.Exists(PlayerPrefabPath)) throw new InvalidOperationException($"프리팹을 찾을 수 없습니다: {PlayerPrefabPath}. 2일차 셋업을 먼저 실행하세요.");

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform pivot = root.transform.Find(PlayerWiring.PivotName);
                if (pivot == null) throw new InvalidOperationException($"캐릭터 프리팹에 {PlayerWiring.PivotName}가 없습니다. 2일차 셋업을 먼저 실행하세요.");

                Camera camera = pivot.GetComponentInChildren<Camera>(true);
                AvatarView avatar = root.GetComponentInChildren<AvatarView>(true);
                PlayerWiring.Apply(root, actions, pivot, camera, avatar);

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
