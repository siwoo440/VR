using System;
using System.IO;
using AtelierVerse.Core;
using AtelierVerse.Player;
using AtelierVerse.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 18일차 구성을 한 번에 적용한다: 캐릭터 프리팹에 시작 위치를 다루는 부품(PlayerSpawn)을 붙이고,
    /// Sandbox 씬에 시작 위치 표식(SpawnMarker)을 놓고, 맵 정보 창이 든 게임 화면을 다시 조립한다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 17일차까지의 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/18일차 셋업 실행
    /// </summary>
    public static class Day18Setup
    {
        private const string Root = "Assets/_Project";
        private const string MaterialsDir = Root + "/Art/Materials";
        private const string MarkerBaseMaterialPath = MaterialsDir + "/SpawnMarker_Base.mat";
        private const string MarkerArrowMaterialPath = MaterialsDir + "/SpawnMarker_Arrow.mat";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player_Desktop.prefab";
        private const string GameUiPrefabPath = Root + "/Prefabs/GameUI.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string CatalogPath = Root + "/Data/PartCatalog.asset";
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";
        private const string GameUiName = "GameUI";
        private const string MarkerName = "SpawnMarker";

        // 표식은 화면 요소의 층에 둔다. 맵의 대표 그림을 찍을 때 이 층을 빼고 찍으므로 그림에 표식이 들어가지 않는다.
        private const int MarkerLayer = UiFactory.UiLayer;

        [MenuItem("Atelier Verse/18일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "18일차 셋업 완료\n\n- 맵마다 시작 위치 정하기와 바닥의 표식\n- 맵 정보 창: 이름, 설명, 대표 그림\n- 맵 파일 3판",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            InputActionAsset actions = LoadRequired<InputActionAsset>(InputPath);

            UpdatePlayerPrefab();
            Material baseMaterial = CreateMaterial(MarkerBaseMaterialPath, AtelierPalette.Ink);
            Material arrowMaterial = CreateMaterial(MarkerArrowMaterialPath, AtelierPalette.Gold);
            AssetDatabase.SaveAssets();

            UiFactory.LoadShared();
            GameObject root = GameUiBuilder.Build(actions, GameUiBuilder.LoadIcons(), LoadRequired<PartCatalog>(CatalogPath), GameUiBuilder.LoadShapeSprites());
            PrefabUtility.SaveAsPrefabAsset(root, GameUiPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            UpdateSandboxScene(baseMaterial, arrowMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>캐릭터 프리팹에 시작 위치를 다루는 부품을 붙인다.</summary>
        private static void UpdatePlayerPrefab()
        {
            if (!File.Exists(PlayerPrefabPath)) throw new InvalidOperationException($"프리팹을 찾을 수 없습니다: {PlayerPrefabPath}. 2일차 셋업을 먼저 실행하세요.");

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (root.GetComponent<CharacterMotor>() == null) throw new InvalidOperationException("캐릭터 프리팹에 CharacterMotor가 없습니다. 9일차 셋업을 먼저 실행하세요.");
                if (root.GetComponent<PlayerSpawn>() == null) root.AddComponent<PlayerSpawn>();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>빛을 받지 않는 한 가지 색의 재질. 표식은 어두운 곳에서도 같은 색으로 보인다.</summary>
        private static Material CreateMaterial(string path, Color color)
        {
            Shader shader = Shader.Find(UnlitShaderName);
            if (shader == null) throw new InvalidOperationException($"셰이더를 찾을 수 없습니다: {UnlitShaderName}");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Sandbox 씬의 게임 화면을 새 프리팹으로 바꾸고, 시작 위치 표식을 다시 만든다.</summary>
        private static void UpdateSandboxScene(Material baseMaterial, Material arrowMaterial)
        {
            if (!File.Exists(SandboxScene)) throw new InvalidOperationException($"씬을 찾을 수 없습니다: {SandboxScene}. 1일차 셋업을 먼저 실행하세요.");

            Scene scene = EditorSceneManager.OpenScene(SandboxScene, OpenSceneMode.Single);
            GameObject gameUi = LoadRequired<GameObject>(GameUiPrefabPath);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == GameUiName || root.name == MarkerName) UnityEngine.Object.DestroyImmediate(root);
            }

            CreateMarker(scene, baseMaterial, arrowMaterial);
            PrefabUtility.InstantiatePrefab(gameUi, scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// 시작 위치 표식: 바닥에 깔린 얇고 둥근 판과, 그 위에서 시작할 때 바라보는 쪽(+z)을 가리키는 화살표.
        /// 충돌체는 두지 않는다. 자리는 실행 중에 PlayerSpawn이 정한다.
        /// </summary>
        private static void CreateMarker(Scene scene, Material baseMaterial, Material arrowMaterial)
        {
            var marker = new GameObject(MarkerName);
            SceneManager.MoveGameObjectToScene(marker, scene);
            marker.AddComponent<SpawnMarker>();

            AddPiece(marker.transform, "Base", PrimitiveType.Cylinder, baseMaterial, new Vector3(0f, 0.008f, 0f), Quaternion.identity, new Vector3(0.8f, 0.006f, 0.8f));
            AddPiece(marker.transform, "ArrowShaft", PrimitiveType.Cube, arrowMaterial, new Vector3(0f, 0.02f, -0.04f), Quaternion.identity, new Vector3(0.1f, 0.012f, 0.3f));
            AddPiece(marker.transform, "ArrowHead", PrimitiveType.Cube, arrowMaterial, new Vector3(0f, 0.02f, 0.14f), Quaternion.Euler(0f, 45f, 0f), new Vector3(0.2f, 0.012f, 0.2f));

            UiFactory.SetLayer(marker, MarkerLayer);
        }

        private static void AddPiece(Transform parent, string name, PrimitiveType shape, Material material, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject piece = GameObject.CreatePrimitive(shape);
            piece.name = name;
            UnityEngine.Object.DestroyImmediate(piece.GetComponent<Collider>());
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localRotation = rotation;
            piece.transform.localScale = scale;

            var renderer = piece.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException($"자산을 불러오지 못했습니다: {path}. 앞 일차의 셋업을 먼저 실행하세요.");
            return asset;
        }
    }
}
