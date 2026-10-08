using System;
using System.IO;
using AtelierVerse.Core;
using AtelierVerse.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 1일차 기반 구성을 한 번에 적용한다. 여러 번 실행해도 결과가 같도록 작성했다.
    /// 메뉴: Atelier Verse/1일차 셋업 실행
    /// </summary>
    public static class Day1Setup
    {
        private const string Root = "Assets/_Project";
        private const string ScenesDir = Root + "/Scenes";
        private const string MaterialsDir = Root + "/Art/Materials";
        private const string TexturesDir = Root + "/Art/Textures";
        private const string BootScene = ScenesDir + "/Boot.unity";
        private const string SandboxScene = ScenesDir + "/Sandbox.unity";
        private const string GridTexturePath = TexturesDir + "/Grid.png";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player_Desktop.prefab";
        private const string ProjectWideActionsKey = "com.unity.input.settings.actions";
        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const float FloorSize = 16f;

        private static readonly string[] TemplateAssets =
        {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes",
            "Assets/TutorialInfo",
            "Assets/Readme.asset",
            "Assets/InputSystem_Actions.inputactions",
            "Assets/Settings/SampleSceneProfile.asset",
        };

        [MenuItem("Atelier Verse/1일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "1일차 셋업 완료\n\n- Sandbox / Boot 씬 생성\n- 모눈 바닥과 블록 재질 생성\n- PC 조작 캐릭터 프리팹 생성\n- 빌드 설정 등록과 템플릿 자산 정리",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            EnsureFolders();

            Texture2D grid = CreateGridTexture();
            Material floor = CreateMaterial("GridFloor", AtelierPalette.Ivory, grid, new Vector2(FloorSize, FloorSize));
            Material gold = CreateMaterial("Block_Gold", AtelierPalette.Gold);
            Material blue = CreateMaterial("Block_Blue", AtelierPalette.Blue);
            Material clay = CreateMaterial("Block_Clay", AtelierPalette.Clay);
            Material leaf = CreateMaterial("Block_Leaf", AtelierPalette.Leaf);
            Material ivory = CreateMaterial("Block_Ivory", AtelierPalette.Ivory);
            Material wood = CreateMaterial("Block_Wood", AtelierPalette.Wood);

            GameObject player = CreatePlayerPrefab();
            CreateSandboxScene(player, floor, gold, blue, clay, leaf, ivory, wood);
            CreateBootScene();
            ApplyBuildSettings();
            ApplyPlayerSettings();
            RemoveTemplateAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Scenes", "Scripts", "Scripts/Core", "Scripts/Player", "Scripts/World",
                "Art", "Art/Materials", "Art/Textures", "Prefabs", "Input", "Editor", "Tests", "Tests/EditMode"
            };

            foreach (string folder in folders)
            {
                string path = $"{Root}/{folder}";
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }

            AssetDatabase.Refresh();
        }

        /// <summary>한 칸에 한 번 반복되는 모눈 무늬. 가장자리에만 선을 그어 칸 경계가 선으로 보이게 한다.</summary>
        private static Texture2D CreateGridTexture()
        {
            const int size = 128;
            const int lineWidth = 2;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color32 fill = AtelierPalette.Ivory;
            Color32 line = Color.Lerp(AtelierPalette.Ivory, AtelierPalette.Blue, 0.35f);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < lineWidth || y < lineWidth || x >= size - lineWidth || y >= size - lineWidth;
                    pixels[y * size + x] = edge ? line : fill;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(GridTexturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(GridTexturePath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(GridTexturePath);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(GridTexturePath);
        }

        private static Material CreateMaterial(string name, Color color, Texture2D texture = null, Vector2? tiling = null)
        {
            Shader shader = Shader.Find(LitShaderName);
            if (shader == null) throw new InvalidOperationException($"셰이더를 찾을 수 없습니다: {LitShaderName}");

            string path = $"{MaterialsDir}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", texture != null ? Color.white : color);
            material.SetFloat("_Smoothness", 0.15f);
            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling ?? Vector2.one);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreatePlayerPrefab()
        {
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (actions == null) throw new InvalidOperationException($"입력 자산을 찾을 수 없습니다: {InputPath}");

            var root = new GameObject("Player_Desktop");
            var body = root.AddComponent<CharacterController>();
            body.height = 1.7f;
            body.radius = 0.3f;
            body.center = new Vector3(0f, 0.85f, 0f);
            body.stepOffset = 0.35f;
            // 기본값(0.001)이면 초당 프레임 수가 아주 높을 때 한 프레임 이동량이 그보다 작아 캐릭터가 움직이지 않는다.
            body.minMoveDistance = 0f;

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            pivot.tag = "MainCamera";
            var camera = pivot.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = 70f;
            pivot.AddComponent<AudioListener>();

            // 몸·카메라 리그·PC 조작을 붙이고 잇는다. 1일차에는 겉모습이 없고 카메라가 기준점에 붙어 있어 1인칭만 된다.
            PlayerWiring.Apply(root, actions, pivot.transform, camera, null);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateSandboxScene(GameObject player, Material floor, Material gold, Material blue, Material clay, Material leaf, Material ivory, Material wood)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Day1_Sandbox");

            var sun = new GameObject("Sun");
            sun.transform.SetParent(root.transform, false);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GridFloor";
            ground.transform.SetParent(root.transform, false);
            ground.transform.localScale = Vector3.one * (FloorSize / 10f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = floor;
            ground.isStatic = true;

            var blocks = new GameObject("Blocks");
            blocks.transform.SetParent(root.transform, false);

            // 골드 집: 가로 3칸, 세로 2칸, 높이 2칸에 테라코타 지붕 판
            for (int x = 2; x <= 4; x++)
            {
                for (int z = 2; z <= 3; z++)
                {
                    PlaceBlock(blocks.transform, new Vector3Int(x, 0, z), gold, "House");
                    PlaceBlock(blocks.transform, new Vector3Int(x, 1, z), gold, "House");
                    PlaceSlab(blocks.transform, new Vector3Int(x, 2, z), clay, "Roof");
                }
            }

            // 블루 단: 가로 2칸, 세로 2칸, 높이 1칸
            for (int x = -5; x <= -4; x++)
            {
                for (int z = 1; z <= 2; z++)
                {
                    PlaceBlock(blocks.transform, new Vector3Int(x, 0, z), blue, "Stage");
                }
            }

            // 계단처럼 쌓은 흰 블록
            PlaceBlock(blocks.transform, new Vector3Int(-1, 0, 4), ivory, "Step");
            PlaceBlock(blocks.transform, new Vector3Int(0, 0, 4), ivory, "Step");
            PlaceBlock(blocks.transform, new Vector3Int(0, 1, 4), ivory, "Step");

            PlaceTree(blocks.transform, new Vector3Int(-3, 0, -2), wood, leaf);
            PlaceTree(blocks.transform, new Vector3Int(6, 0, 0), wood, leaf);
            PlaceTree(blocks.transform, new Vector3Int(1, 0, 6), wood, leaf);

            var spawned = (GameObject)PrefabUtility.InstantiatePrefab(player, scene);
            spawned.transform.SetPositionAndRotation(new Vector3(0.5f, 0f, -5.5f), Quaternion.identity);

            EditorSceneManager.SaveScene(scene, SandboxScene);
        }

        private static void PlaceBlock(Transform parent, Vector3Int cell, Material material, string label)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = $"{label}_{cell.x}_{cell.y}_{cell.z}";
            block.transform.SetParent(parent, false);
            block.transform.position = GridMath.CellToWorldCenter(cell);
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
            block.isStatic = true;
        }

        /// <summary>칸 바닥에 붙는 얇은 판. 지붕처럼 쓴다.</summary>
        private static void PlaceSlab(Transform parent, Vector3Int cell, Material material, string label)
        {
            const float thickness = 0.3f;

            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = $"{label}_{cell.x}_{cell.y}_{cell.z}";
            slab.transform.SetParent(parent, false);
            Vector3 center = GridMath.CellToWorldCenter(cell);
            slab.transform.position = new Vector3(center.x, cell.y + thickness * 0.5f, center.z);
            slab.transform.localScale = new Vector3(1f, thickness, 1f);
            slab.GetComponent<MeshRenderer>().sharedMaterial = material;
            slab.isStatic = true;
        }

        private static void PlaceTree(Transform parent, Vector3Int cell, Material trunkMaterial, Material crownMaterial)
        {
            Vector3 center = GridMath.CellToWorldCenter(cell);

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trunk.name = $"Tree_Trunk_{cell.x}_{cell.z}";
            trunk.transform.SetParent(parent, false);
            trunk.transform.position = new Vector3(center.x, 0.5f, center.z);
            trunk.transform.localScale = new Vector3(0.3f, 1f, 0.3f);
            trunk.GetComponent<MeshRenderer>().sharedMaterial = trunkMaterial;
            trunk.isStatic = true;

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crown.name = $"Tree_Crown_{cell.x}_{cell.z}";
            crown.transform.SetParent(parent, false);
            crown.transform.position = new Vector3(center.x, 1.45f, center.z);
            crown.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            crown.GetComponent<MeshRenderer>().sharedMaterial = crownMaterial;
            crown.isStatic = true;
        }

        private static void CreateBootScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Bootstrap").AddComponent<Bootstrap>();
            EditorSceneManager.SaveScene(scene, BootScene);
        }

        private static void ApplyBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScene, true),
                new EditorBuildSettingsScene(SandboxScene, true),
            };
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Palettra Games";
            PlayerSettings.productName = "Atelier Verse";
        }

        /// <summary>템플릿이 넣은 예제 씬과 안내 자산을 지운다. 렌더 파이프라인 설정(Assets/Settings)은 그대로 둔다.</summary>
        private static void RemoveTemplateAssets()
        {
            EditorBuildSettings.RemoveConfigObject(ProjectWideActionsKey);

            foreach (string path in TemplateAssets)
            {
                if (File.Exists(path) || Directory.Exists(path)) AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
