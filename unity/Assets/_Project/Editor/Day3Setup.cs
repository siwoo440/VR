using System;
using System.Collections.Generic;
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
    /// 3일차 구성을 한 번에 적용한다: 부품 목록, 블록 프리팹과 미리 보기 블록, 블록 세계, 블록 놓기.
    /// 1일차에 씬에 놓은 블록 가운데 한 칸을 꽉 채우는 것은 블록 세계로 옮겨, 놓은 블록과 똑같이 지울 수 있게 한다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 1일차와 2일차 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/3일차 셋업 실행
    /// </summary>
    public static class Day3Setup
    {
        private const string Root = "Assets/_Project";
        private const string DataDir = Root + "/Data";
        private const string CatalogPath = DataDir + "/PartCatalog.asset";
        private const string MaterialsDir = Root + "/Art/Materials";
        private const string GhostMaterialPath = MaterialsDir + "/Block_Ghost.mat";
        private const string BlockPrefabPath = Root + "/Prefabs/Block.prefab";
        private const string GhostPrefabPath = Root + "/Prefabs/BlockGhost.prefab";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player_Desktop.prefab";
        private const string GameUiPrefabPath = Root + "/Prefabs/GameUI.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";
        private const string WorldName = "BlockWorld";
        private const string GameUiName = "GameUI";
        private const string SceneryRootName = "Day1_Sandbox";
        private const string SceneryBlocksName = "Blocks";
        private const int IgnoreRaycastLayer = 2;

        // 부품 칸의 순서와 같다. id는 맵을 저장할 때 쓰는 이름이므로 바꾸지 않는다.
        private static readonly (string id, string name, string material, Color color)[] Parts =
        {
            ("block.gold", "골드 블록", "Block_Gold", AtelierPalette.Gold),
            ("block.blue", "블루 블록", "Block_Blue", AtelierPalette.Blue),
            ("block.clay", "테라코타 블록", "Block_Clay", AtelierPalette.Clay),
            ("block.leaf", "잎 블록", "Block_Leaf", AtelierPalette.Leaf),
            ("block.ivory", "흰 블록", "Block_Ivory", AtelierPalette.Ivory),
            ("block.wood", "나무 블록", "Block_Wood", AtelierPalette.Wood),
        };

        // 1일차 씬의 블록 가운데 한 칸을 꽉 채우는 것들의 이름 앞부분. 지붕 판과 나무는 칸보다 작아 그대로 둔다.
        private static readonly string[] FullCellPrefixes = { "House_", "Stage_", "Step_" };

        [MenuItem("Atelier Verse/3일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "3일차 셋업 완료\n\n- 부품 목록과 블록 프리팹\n- 블록 세계와 블록 놓기\n- 놓기 안내와 블록 수 표시\n- Sandbox 씬의 블록을 블록 세계로 옮김",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);
            AssetDatabase.Refresh();

            UiFactory.LoadShared();

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (actions == null) throw new InvalidOperationException($"입력 자산을 찾을 수 없습니다: {InputPath}");

            PartCatalog catalog = CreateCatalog();
            CreateBlockPrefab(catalog);
            Renderer ghostPrefab = CreateGhostPrefab(CreateGhostMaterial());

            UpdatePlayerPrefab(actions, ghostPrefab);
            CreateGameUiPrefab(actions, catalog);
            AssetDatabase.SaveAssets();

            UpdateSandboxScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ── 부품과 프리팹 ─────────────────────────────────────────────

        private static PartCatalog CreateCatalog()
        {
            PartCatalog catalog = AssetDatabase.LoadAssetAtPath<PartCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PartCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var serialized = new SerializedObject(catalog);
            SerializedProperty parts = serialized.FindProperty("parts");
            parts.arraySize = Parts.Length;

            for (int i = 0; i < Parts.Length; i++)
            {
                string materialPath = $"{MaterialsDir}/{Parts[i].material}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null) throw new InvalidOperationException($"재질을 찾을 수 없습니다: {materialPath}. 1일차 셋업을 먼저 실행하세요.");

                SerializedProperty part = parts.GetArrayElementAtIndex(i);
                part.FindPropertyRelative("id").stringValue = Parts[i].id;
                part.FindPropertyRelative("displayName").stringValue = Parts[i].name;
                part.FindPropertyRelative("material").objectReferenceValue = material;
                part.FindPropertyRelative("color").colorValue = Parts[i].color;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>놓이는 블록 하나. 한 칸을 꽉 채우는 정육면체다.</summary>
        private static void CreateBlockPrefab(PartCatalog catalog)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "Block";
            block.GetComponent<MeshRenderer>().sharedMaterial = catalog.Get(0).material;
            block.AddComponent<PlacedBlock>();

            PrefabUtility.SaveAsPrefabAsset(block, BlockPrefabPath);
            UnityEngine.Object.DestroyImmediate(block);
        }

        /// <summary>놓일 자리를 보여 주는 반투명 재질. 색은 실행 중에 부품 색으로 바뀐다.</summary>
        private static Material CreateGhostMaterial()
        {
            Shader shader = Shader.Find(UnlitShaderName);
            if (shader == null) throw new InvalidOperationException($"셰이더를 찾을 수 없습니다: {UnlitShaderName}");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(GhostMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, GhostMaterialPath);
            }

            material.shader = shader;
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetColor("_BaseColor", AtelierPalette.WithAlpha(AtelierPalette.Gold, 0.55f));

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Renderer CreateGhostPrefab(Material material)
        {
            GameObject ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = "BlockGhost";
            UnityEngine.Object.DestroyImmediate(ghost.GetComponent<Collider>());

            var renderer = ghost.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(ghost, GhostPrefabPath);
            UnityEngine.Object.DestroyImmediate(ghost);
            return prefab.GetComponent<Renderer>();
        }

        // ── 캐릭터와 게임 화면 ───────────────────────────────────────────

        /// <summary>
        /// 캐릭터에 블록 놓기를 붙인다. 조준선이 자기 몸에 막히지 않도록 캐릭터의 층을 Ignore Raycast로 둔다.
        /// </summary>
        private static void UpdatePlayerPrefab(InputActionAsset actions, Renderer ghostPrefab)
        {
            if (!File.Exists(PlayerPrefabPath)) throw new InvalidOperationException($"프리팹을 찾을 수 없습니다: {PlayerPrefabPath}. 2일차 셋업을 먼저 실행하세요.");

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                root.layer = IgnoreRaycastLayer;

                BlockBuilder builder = root.GetComponent<BlockBuilder>();
                if (builder == null) builder = root.AddComponent<BlockBuilder>();

                var serialized = new SerializedObject(builder);
                serialized.FindProperty("actions").objectReferenceValue = actions;
                serialized.FindProperty("ghostPrefab").objectReferenceValue = ghostPrefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>부품 목록에 맞춰 게임 화면을 다시 조립한다. 부품 칸의 이름과 색이 목록에서 온다.</summary>
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

        // ── 씬 ─────────────────────────────────────────────────

        /// <summary>
        /// Sandbox 씬에 블록 세계와 새 게임 화면을 놓는다.
        /// 씬을 새로 열면 쓰이지 않던 자산이 메모리에서 내려가므로, 필요한 자산은 씬을 연 뒤에 경로로 다시 불러온다.
        /// </summary>
        private static void UpdateSandboxScene()
        {
            if (!File.Exists(SandboxScene)) throw new InvalidOperationException($"씬을 찾을 수 없습니다: {SandboxScene}. 1일차 셋업을 먼저 실행하세요.");

            Scene scene = EditorSceneManager.OpenScene(SandboxScene, OpenSceneMode.Single);
            PartCatalog catalog = LoadRequired<PartCatalog>(CatalogPath);
            PlacedBlock blockPrefab = LoadRequired<PlacedBlock>(BlockPrefabPath);
            GameObject gameUi = LoadRequired<GameObject>(GameUiPrefabPath);
            GameObject worldObject = null;
            Transform scenery = null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == GameUiName) UnityEngine.Object.DestroyImmediate(root);
                else if (root.name == WorldName) worldObject = root;
                else if (root.name == SceneryRootName) scenery = root.transform.Find(SceneryBlocksName);
            }

            if (worldObject == null)
            {
                worldObject = new GameObject(WorldName);
                SceneManager.MoveGameObjectToScene(worldObject, scene);
            }

            BlockWorld world = worldObject.GetComponent<BlockWorld>();
            if (world == null) world = worldObject.AddComponent<BlockWorld>();

            var serialized = new SerializedObject(world);
            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            serialized.FindProperty("blockPrefab").objectReferenceValue = blockPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (scenery != null) MoveFullCellBlocks(scenery, worldObject.transform, catalog, blockPrefab);

            PrefabUtility.InstantiatePrefab(gameUi, scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException($"자산을 불러오지 못했습니다: {path}");
            return asset;
        }

        /// <summary>1일차에 놓은 블록 가운데 한 칸을 꽉 채우는 것을 블록 세계의 블록으로 바꾼다. 이미 옮긴 뒤에는 할 일이 없다.</summary>
        private static void MoveFullCellBlocks(Transform scenery, Transform worldRoot, PartCatalog catalog, PlacedBlock blockPrefab)
        {
            var targets = new List<Transform>();
            foreach (Transform child in scenery)
            {
                foreach (string prefix in FullCellPrefixes)
                {
                    if (!child.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                    targets.Add(child);
                    break;
                }
            }

            foreach (Transform old in targets)
            {
                var oldRenderer = old.GetComponent<MeshRenderer>();
                int partIndex = oldRenderer != null ? catalog.IndexOf(oldRenderer.sharedMaterial) : -1;
                if (partIndex < 0) continue;

                Vector3Int cell = GridMath.WorldToCell(old.position);
                var block = (PlacedBlock)PrefabUtility.InstantiatePrefab(blockPrefab, worldRoot);
                if (block == null) throw new InvalidOperationException($"블록 프리팹을 씬에 놓지 못했습니다: {BlockPrefabPath}");
                block.name = $"Block_{cell.x}_{cell.y}_{cell.z}";
                block.transform.SetPositionAndRotation(GridMath.CellToWorldCenter(cell), Quaternion.identity);

                // 블록의 자리는 트랜스폼이 가진다. 번호는 실행할 때 블록 세계가 붙인다(14일차).
                var blockData = new SerializedObject(block);
                blockData.FindProperty("partIndex").intValue = partIndex;
                blockData.ApplyModifiedPropertiesWithoutUndo();

                var rendererData = new SerializedObject(block.GetComponent<MeshRenderer>());
                rendererData.FindProperty("m_Materials").GetArrayElementAtIndex(0).objectReferenceValue = catalog.Get(partIndex).material;
                rendererData.ApplyModifiedPropertiesWithoutUndo();

                UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
        }
    }
}
