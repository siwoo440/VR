using System;
using System.IO;
using AtelierVerse.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 14일차 구성을 한 번에 적용한다: 블록이 칸 대신 자리(트랜스폼)로 기록되게 바뀌었으므로,
    /// 블록 프리팹과 Sandbox 씬의 블록을 다시 저장해 더는 쓰지 않는 칸 번호를 지운다. 블록의 자리와 부품은 그대로다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 3일차 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/14일차 셋업 실행
    /// </summary>
    public static class Day14Setup
    {
        private const string Root = "Assets/_Project";
        private const string BlockPrefabPath = Root + "/Prefabs/Block.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";

        [MenuItem("Atelier Verse/14일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "14일차 셋업 완료\n\n- 블록을 칸이 아니라 자리로 기록\n- 블록 프리팹과 씬의 블록에서 칸 번호 지우기",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            if (!File.Exists(BlockPrefabPath)) throw new InvalidOperationException($"프리팹을 찾을 수 없습니다: {BlockPrefabPath}. 3일차 셋업을 먼저 실행하세요.");
            if (!File.Exists(SandboxScene)) throw new InvalidOperationException($"씬을 찾을 수 없습니다: {SandboxScene}. 1일차 셋업을 먼저 실행하세요.");

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(BlockPrefabPath);
            try
            {
                foreach (PlacedBlock block in prefabRoot.GetComponentsInChildren<PlacedBlock>(true))
                {
                    EditorUtility.SetDirty(block);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, BlockPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            Scene scene = EditorSceneManager.OpenScene(SandboxScene, OpenSceneMode.Single);
            int count = 0;
            var instances = new System.Collections.Generic.List<GameObject>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (PlacedBlock block in root.GetComponentsInChildren<PlacedBlock>(true))
                {
                    EditorUtility.SetDirty(block);
                    if (PrefabUtility.IsPartOfPrefabInstance(block)) instances.Add(block.gameObject);
                    count++;
                }
            }

            // 씬의 블록은 블록 프리팹의 사본이라, 없어진 칸 번호가 "고친 값"으로 남는다. 쓰이지 않는 고친 값을 지운다.
            if (instances.Count > 0) PrefabUtility.RemoveUnusedOverrides(instances.ToArray(), InteractionMode.AutomatedAction);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Atelier Verse] 씬의 블록 {count}개를 자리 기준으로 다시 저장했습니다.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
