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
    /// 10일차 구성을 한 번에 적용한다: 캐릭터 프리팹에 VR 리그(머리와 두 손의 추적), VR 조작, 조작 방식 고르기를 더한다.
    /// VR 쪽은 꺼 둔 채로 저장하므로 평소에는 키보드·마우스 조작으로 시작한다. 입력 자산의 XR 묶음은 파일에 직접 들어 있다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 9일차 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/10일차 셋업 실행
    /// </summary>
    public static class Day10Setup
    {
        private const string Root = "Assets/_Project";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player_Desktop.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string HandMaterialPath = Root + "/Art/Materials/Block_Ivory.mat";

        [MenuItem("Atelier Verse/10일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "10일차 셋업 완료\n\n- 캐릭터에 VR 리그(머리·두 손)와 VR 조작 추가\n- 평소에는 키보드·마우스 조작으로 시작",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (actions == null) throw new InvalidOperationException($"입력 자산을 찾을 수 없습니다: {InputPath}");
            if (actions.FindActionMap("XR") == null) throw new InvalidOperationException("입력 자산에 XR 묶음이 없습니다. AtelierInput.inputactions를 확인하세요.");
            if (!File.Exists(PlayerPrefabPath)) throw new InvalidOperationException($"프리팹을 찾을 수 없습니다: {PlayerPrefabPath}. 2일차 셋업을 먼저 실행하세요.");

            Material handMaterial = AssetDatabase.LoadAssetAtPath<Material>(HandMaterialPath);
            if (handMaterial == null) throw new InvalidOperationException($"재질을 찾을 수 없습니다: {HandMaterialPath}. 1일차 셋업을 먼저 실행하세요.");

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (root.GetComponent<CharacterMotor>() == null) throw new InvalidOperationException("캐릭터 프리팹에 몸(CharacterMotor)이 없습니다. 9일차 셋업을 먼저 실행하세요.");

                Camera camera = root.GetComponentInChildren<Camera>(true);
                AvatarView avatar = root.GetComponentInChildren<AvatarView>(true);
                PlayerWiring.ApplyXr(root, actions, camera, avatar, handMaterial);

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
