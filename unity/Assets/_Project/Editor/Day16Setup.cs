using System;
using System.Collections.Generic;
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
    /// 16일차 구성을 한 번에 적용한다: 모양이 다른 부품(판, 기둥, 경사, 계단)의 메시와 화면용 그림을 만들고,
    /// 부품 목록에 모양마다 여섯 색을 더한 뒤, 부품 고르는 창이 든 게임 화면을 다시 조립한다.
    /// 캐릭터가 경사 부품(45도)을 걸어 오를 수 있게 캐릭터 프리팹의 오를 수 있는 기울기도 넓힌다.
    /// 부품 목록의 앞 여섯(블록 여섯 색)은 순서와 저장용 이름을 그대로 두고 그 뒤에 더한다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. 15일차까지의 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/16일차 셋업 실행
    /// </summary>
    public static class Day16Setup
    {
        private const string Root = "Assets/_Project";
        private const string MeshDir = Root + "/Art/Meshes";
        private const string GameUiPrefabPath = Root + "/Prefabs/GameUI.prefab";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player_Desktop.prefab";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string CatalogPath = Root + "/Data/PartCatalog.asset";
        private const string GameUiName = "GameUI";
        private const string BlockIdPrefix = "block.";
        private const string BlockNameSuffix = " 블록";
        private const int SpriteSize = 96;
        private const int SpriteSamples = 4;
        private const float SpriteEdge = 0.06f;

        // 경사 부품은 45도다. Unity의 기본값(45도)으로는 꼭 45도인 면을 오르지 못하므로 조금 넉넉하게 둔다.
        private const float WalkableSlope = 50f;

        // 저장용 이름의 앞부분. 한 번 정하면 바꾸지 않는다. 순서는 PartShape와 같다.
        private static readonly string[] ShapeIds = { "block", "slab", "pillar", "wedge", "stairs" };

        // 화면용 그림의 윤곽(0~1의 네모 안, y는 위쪽이 큰 값). 순서는 PartShape와 같다.
        private static readonly Vector2[][] Outlines =
        {
            new[] { new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.14f), new Vector2(0.86f, 0.86f), new Vector2(0.14f, 0.86f) },
            new[] { new Vector2(0.08f, 0.33f), new Vector2(0.92f, 0.33f), new Vector2(0.92f, 0.67f), new Vector2(0.08f, 0.67f) },
            new[] { new Vector2(0.33f, 0.08f), new Vector2(0.67f, 0.08f), new Vector2(0.67f, 0.92f), new Vector2(0.33f, 0.92f) },
            new[] { new Vector2(0.1f, 0.16f), new Vector2(0.9f, 0.16f), new Vector2(0.9f, 0.84f) },
            new[]
            {
                new Vector2(0.1f, 0.16f), new Vector2(0.9f, 0.16f), new Vector2(0.9f, 0.84f), new Vector2(0.633f, 0.84f),
                new Vector2(0.633f, 0.613f), new Vector2(0.367f, 0.613f), new Vector2(0.367f, 0.387f), new Vector2(0.1f, 0.387f),
            },
        };

        [MenuItem("Atelier Verse/16일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "16일차 셋업 완료\n\n- 모양이 다른 부품: 판, 기둥, 경사, 계단(색마다)\n- 부품 고르는 창(B)\n- VR: 부품 판의 부품 창 단추",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            InputActionAsset actions = LoadRequired<InputActionAsset>(InputPath);
            if (actions.FindAction("Game/Parts") == null) throw new InvalidOperationException("입력 자산에 Game/Parts 동작이 없습니다. AtelierInput.inputactions를 확인하세요.");

            Mesh[] meshes = CreateMeshes();
            PartCatalog catalog = UpdateCatalog(meshes);
            Sprite[] sprites = CreateShapeSprites();
            UpdatePlayerPrefab();
            AssetDatabase.SaveAssets();

            UiFactory.LoadShared();
            GameObject root = GameUiBuilder.Build(actions, GameUiBuilder.LoadIcons(), catalog, sprites);
            PrefabUtility.SaveAsPrefabAsset(root, GameUiPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            UpdateSandboxScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>캐릭터가 걸어 오를 수 있는 기울기를 넓힌다. 경사 부품을 걸어 오르기 위한 것이다.</summary>
        private static void UpdatePlayerPrefab()
        {
            if (!File.Exists(PlayerPrefabPath)) throw new InvalidOperationException($"프리팹을 찾을 수 없습니다: {PlayerPrefabPath}. 2일차 셋업을 먼저 실행하세요.");

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var body = root.GetComponent<CharacterController>();
                if (body == null) throw new InvalidOperationException("캐릭터 프리팹에 CharacterController가 없습니다.");

                body.slopeLimit = WalkableSlope;
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>모양마다 메시 자산을 하나씩 만든다. 이미 있으면 내용만 바꿔, 부품 목록과 씬이 가리키는 자산이 그대로 남게 한다.</summary>
        private static Mesh[] CreateMeshes()
        {
            if (!AssetDatabase.IsValidFolder(MeshDir)) AssetDatabase.CreateFolder(Root + "/Art", "Meshes");

            var meshes = new Mesh[PartMeshes.ShapeCount];
            for (int i = 0; i < meshes.Length; i++)
            {
                var shape = (PartShape)i;
                string path = $"{MeshDir}/Part_{shape}.asset";
                Mesh built = PartMeshes.Build(shape);
                Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

                if (existing == null)
                {
                    AssetDatabase.CreateAsset(built, path);
                    meshes[i] = built;
                    continue;
                }

                EditorUtility.CopySerialized(built, existing);
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(built);
                meshes[i] = existing;
            }

            return meshes;
        }

        /// <summary>
        /// 부품 목록을 다시 채운다. 앞에는 지금 있는 블록들(색의 순서 그대로)을 두고, 그 뒤에 모양마다 같은 색들을 더한다.
        /// 색의 이름과 재질은 블록 부품에서 따온다("골드 블록" → "골드 판", block.gold → slab.gold).
        /// </summary>
        private static PartCatalog UpdateCatalog(Mesh[] meshes)
        {
            PartCatalog catalog = LoadRequired<PartCatalog>(CatalogPath);

            var colors = new List<PartCatalog.Part>();
            for (int i = 0; i < catalog.Count; i++)
            {
                PartCatalog.Part part = catalog.Get(i);
                if (part.id != null && part.id.StartsWith(BlockIdPrefix, StringComparison.Ordinal)) colors.Add(part);
            }

            if (colors.Count == 0) throw new InvalidOperationException("부품 목록에 블록 부품이 없습니다. 3일차 셋업을 먼저 실행하세요.");

            var serialized = new SerializedObject(catalog);
            SerializedProperty parts = serialized.FindProperty("parts");
            parts.arraySize = colors.Count * PartMeshes.ShapeCount;

            int index = 0;
            for (int shapeIndex = 0; shapeIndex < PartMeshes.ShapeCount; shapeIndex++)
            {
                var shape = (PartShape)shapeIndex;
                foreach (PartCatalog.Part color in colors)
                {
                    string suffix = color.id.Substring(BlockIdPrefix.Length);
                    string colorName = color.displayName.EndsWith(BlockNameSuffix, StringComparison.Ordinal)
                        ? color.displayName.Substring(0, color.displayName.Length - BlockNameSuffix.Length)
                        : color.displayName;

                    SerializedProperty part = parts.GetArrayElementAtIndex(index++);
                    part.FindPropertyRelative("id").stringValue = $"{ShapeIds[shapeIndex]}.{suffix}";
                    part.FindPropertyRelative("displayName").stringValue = $"{colorName} {PartMeshes.NameOf(shape)}";
                    part.FindPropertyRelative("material").objectReferenceValue = color.material;
                    part.FindPropertyRelative("color").colorValue = color.color;
                    part.FindPropertyRelative("shape").enumValueIndex = shapeIndex;
                    part.FindPropertyRelative("mesh").objectReferenceValue = meshes[shapeIndex];
                    part.FindPropertyRelative("size").vector3Value = PartMeshes.SizeOf(shape);
                    part.FindPropertyRelative("boxCollider").boolValue = PartMeshes.IsBox(shape);
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>
        /// 부품 칸과 부품 고르는 창에 쓰는 모양 그림을 그린다. 흰 바탕에 회색 테두리이며, 화면에서 부품의 색을 입히면
        /// 테두리가 그 색의 어두운 빛이 된다.
        /// </summary>
        private static Sprite[] CreateShapeSprites()
        {
            var sprites = new Sprite[PartMeshes.ShapeCount];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = SaveSprite($"{UiFactory.ArtDir}/Shape_{(PartShape)i}.png", DrawOutline(Outlines[i]));
            }

            return sprites;
        }

        private static Texture2D DrawOutline(Vector2[] polygon)
        {
            var texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
            var pixels = new Color32[SpriteSize * SpriteSize];
            float total = SpriteSamples * SpriteSamples;

            for (int y = 0; y < SpriteSize; y++)
            {
                for (int x = 0; x < SpriteSize; x++)
                {
                    int inside = 0;
                    int edge = 0;

                    for (int sy = 0; sy < SpriteSamples; sy++)
                    {
                        for (int sx = 0; sx < SpriteSamples; sx++)
                        {
                            var point = new Vector2((x + (sx + 0.5f) / SpriteSamples) / SpriteSize, (y + (sy + 0.5f) / SpriteSamples) / SpriteSize);
                            if (!Contains(polygon, point)) continue;

                            inside++;
                            if (DistanceToEdge(polygon, point) < SpriteEdge) edge++;
                        }
                    }

                    if (inside == 0)
                    {
                        pixels[y * SpriteSize + x] = new Color32(255, 255, 255, 0);
                        continue;
                    }

                    float edgeShare = edge / (float)inside;
                    byte shade = (byte)Mathf.RoundToInt(Mathf.Lerp(255f, 92f, edgeShare));
                    pixels[y * SpriteSize + x] = new Color32(shade, shade, shade, (byte)Mathf.RoundToInt(255f * inside / total));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static bool Contains(Vector2[] polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }

            return inside;
        }

        private static float DistanceToEdge(Vector2[] polygon, Vector2 point)
        {
            float best = float.MaxValue;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[j];
                Vector2 b = polygon[i];
                Vector2 along = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, along) / along.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(point, a + along * t));
            }

            return best;
        }

        /// <summary>그림을 PNG로 저장하고 화면용 그림(Sprite)으로 불러온다.</summary>
        private static Sprite SaveSprite(string path, Texture2D texture)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException($"화면용 그림을 불러오지 못했습니다: {path}");
            return sprite;
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
