using System;
using System.Collections.Generic;
using System.IO;
using AtelierVerse.Core;
using AtelierVerse.Player;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 2일차 구성을 한 번에 적용한다: 한글 글꼴, 화면용 그림, 블록 캐릭터가 붙은 PC 캐릭터, 게임 화면(GameUI).
    /// 여러 번 실행해도 결과가 같도록 작성했다. 1일차 셋업이 먼저 적용되어 있어야 한다.
    /// 메뉴: Atelier Verse/2일차 셋업 실행
    /// </summary>
    public static class Day2Setup
    {
        public const string FontAssetPath = UiFactory.FontAssetPath;

        private const string Root = "Assets/_Project";
        private const string FontPath = Root + "/Art/Fonts/NanumGothic.ttf";
        private const string UiArtDir = UiFactory.ArtDir;
        private const string MaterialsDir = Root + "/Art/Materials";
        private const string SandboxScene = Root + "/Scenes/Sandbox.unity";
        private const string InputPath = Root + "/Input/AtelierInput.inputactions";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player_Desktop.prefab";
        private const string GameUiPrefabPath = Root + "/Prefabs/GameUI.prefab";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string PlayerName = "Player_Desktop";
        private const string GameUiName = "GameUI";

        private static readonly Vector3 SpawnPosition = new Vector3(0.5f, 0f, -5.5f);

        // 12×12 블록 그림. '#'이 칠해지는 칸이다.
        private static readonly string[] MenuIcon =
        {
            "............",
            "............",
            ".##########.",
            ".##########.",
            "............",
            ".##########.",
            ".##########.",
            "............",
            ".##########.",
            ".##########.",
            "............",
            "............",
        };

        private static readonly string[] RespawnIcon =
        {
            "............",
            "..##........",
            "..#########.",
            "..#########.",
            "..#########.",
            "..#########.",
            "..##........",
            "..##........",
            "..##........",
            "..##........",
            ".####.......",
            "............",
        };

        private static readonly string[] ViewIcon =
        {
            "............",
            "............",
            "...######...",
            ".##......##.",
            "##..####..##",
            "#..######..#",
            "#..######..#",
            "##..####..##",
            ".##......##.",
            "...######...",
            "............",
            "............",
        };

        private static readonly string[] HomeIcon =
        {
            "............",
            ".....##.....",
            "....####....",
            "...######...",
            "..########..",
            ".##########.",
            "..########..",
            "..###..###..",
            "..###..###..",
            "..###..###..",
            "............",
            "............",
        };

        private static readonly string[] MapIcon =
        {
            "............",
            ".####..####.",
            ".####..####.",
            ".####..####.",
            ".####..####.",
            "............",
            "............",
            ".####..####.",
            ".####..####.",
            ".####..####.",
            ".####..####.",
            "............",
        };

        private static readonly string[] AvatarIcon =
        {
            "............",
            "....####....",
            "....####....",
            "....####....",
            "............",
            "..########..",
            "..########..",
            "..########..",
            "....####....",
            "....#..#....",
            "....#..#....",
            "............",
        };

        private static readonly string[] ShieldIcon =
        {
            "............",
            ".##########.",
            ".##########.",
            ".##########.",
            ".##########.",
            ".##########.",
            "..########..",
            "..########..",
            "...######...",
            "....####....",
            ".....##.....",
            "............",
        };

        [MenuItem("Atelier Verse/2일차 셋업 실행")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Apply();
            EditorSceneManager.OpenScene(SandboxScene);
            EditorUtility.DisplayDialog("Atelier Verse",
                "2일차 셋업 완료\n\n- 한글 글꼴과 화면용 그림 생성\n- 블록 캐릭터와 1인칭·3인칭 카메라\n- 게임 화면(GameUI) 프리팹\n- Sandbox 씬에 배치",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            if (!File.Exists(TmpSettingsPath))
            {
                throw new InvalidOperationException("TextMesh Pro 기본 자산이 없습니다. Window > TextMeshPro > Import TMP Essential Resources를 실행한 뒤 다시 시도하세요.");
            }

            foreach (string folder in new[] { UiArtDir, Root + "/Scripts/UI", Root + "/Tests/PlayMode" })
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            }

            AssetDatabase.Refresh();

            UiFactory.Font = CreateFontAsset();
            UiFactory.Rounded = CreateSprite($"{UiArtDir}/Rounded.png", DrawRounded(64, UiFactory.SpriteCorner, 0f), 28f, FilterMode.Bilinear);
            UiFactory.RoundedLine = CreateSprite($"{UiArtDir}/RoundedLine.png", DrawRounded(64, UiFactory.SpriteCorner, 4f), 28f, FilterMode.Bilinear);

            var icons = new GameUiBuilder.Icons
            {
                Menu = CreateIcon("Menu", MenuIcon),
                Respawn = CreateIcon("Respawn", RespawnIcon),
                View = CreateIcon("View", ViewIcon),
                Home = CreateIcon("Home", HomeIcon),
                Map = CreateIcon("Map", MapIcon),
                Avatar = CreateIcon("Avatar", AvatarIcon),
                Shield = CreateIcon("Shield", ShieldIcon),
            };

            // 부품 칸에 넣는 것은 1일차에 만든 블록 재질과 같은 여섯 가지 색이다.
            GameUiBuilder.Item[] items =
            {
                new GameUiBuilder.Item("골드 블록", AtelierPalette.Gold),
                new GameUiBuilder.Item("블루 블록", AtelierPalette.Blue),
                new GameUiBuilder.Item("테라코타 블록", AtelierPalette.Clay),
                new GameUiBuilder.Item("잎 블록", AtelierPalette.Leaf),
                new GameUiBuilder.Item("흰 블록", AtelierPalette.Ivory),
                new GameUiBuilder.Item("나무 블록", AtelierPalette.Wood),
            };

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (actions == null) throw new InvalidOperationException($"입력 자산을 찾을 수 없습니다: {InputPath}");

            GameObject player = CreatePlayerPrefab(actions);
            GameObject gameUi = CreateGameUiPrefab(actions, icons, items);
            UpdateSandboxScene(player, gameUi);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ── 글꼴과 그림 ───────────────────────────────────────────────

        /// <summary>
        /// 한글을 쓸 수 있는 글꼴 자산. 필요한 글자를 쓸 때마다 채워 넣는 방식(Dynamic)이라 이용자가 입력한 이름도 표시된다.
        /// </summary>
        private static TMP_FontAsset CreateFontAsset()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                Font source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
                if (source == null) throw new InvalidOperationException($"글꼴 파일을 찾을 수 없습니다: {FontPath}");

                fontAsset = TMP_FontAsset.CreateFontAsset(source, 64, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                if (fontAsset == null) throw new InvalidOperationException("글꼴 자산을 만들지 못했습니다.");

                AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

                Texture2D atlas = fontAsset.atlasTextures[0];
                atlas.name = "NanumGothic SDF Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);

                fontAsset.material.name = "NanumGothic SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            var serialized = new SerializedObject(fontAsset);
            SerializedProperty clearOnBuild = serialized.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearOnBuild != null)
            {
                clearOnBuild.boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(fontAsset);

            // 새로 만드는 글자 요소도 한글 글꼴을 쓰도록 기본 글꼴로 지정한다.
            if (TMP_Settings.instance != null)
            {
                var settings = new SerializedObject(TMP_Settings.instance);
                SerializedProperty defaultFont = settings.FindProperty("m_defaultFontAsset");
                if (defaultFont != null && defaultFont.objectReferenceValue != fontAsset)
                {
                    defaultFont.objectReferenceValue = fontAsset;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            return fontAsset;
        }

        /// <summary>둥근 사각형. ring이 0보다 크면 그 두께의 테두리 선만 남긴다.</summary>
        private static Texture2D DrawRounded(int size, float radius, float ring)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 둥근 사각형의 가장자리까지 거리. 안쪽이 음수다.
                    float dx = Mathf.Abs(x + 0.5f - half) - (half - radius);
                    float dy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    float distance = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;

                    float alpha = Mathf.Clamp01(0.5f - distance);
                    if (ring > 0f) alpha *= Mathf.Clamp01(0.5f + distance + ring);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static Sprite CreateIcon(string name, string[] rows)
        {
            int size = rows.Length;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                string row = rows[size - 1 - y];
                for (int x = 0; x < size; x++)
                {
                    bool filled = x < row.Length && row[x] == '#';
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(filled ? 255 : 0));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return CreateSprite($"{UiArtDir}/Icon_{name}.png", texture, 0f, FilterMode.Point);
        }

        /// <summary>그림을 PNG로 저장하고 화면용 그림(Sprite)으로 불러온다. border는 늘려도 모양이 유지되는 가장자리 폭이다.</summary>
        private static Sprite CreateSprite(string path, Texture2D texture, float border, FilterMode filterMode)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.mipmapEnabled = false;
            importer.filterMode = filterMode;
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

        private static Material BlockMaterial(string name, Color? createWith = null)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            if (createWith == null) throw new InvalidOperationException($"재질을 찾을 수 없습니다: {path}. 1일차 셋업을 먼저 실행하세요.");

            Shader shader = Shader.Find(LitShaderName);
            if (shader == null) throw new InvalidOperationException($"셰이더를 찾을 수 없습니다: {LitShaderName}");

            material = new Material(shader);
            material.SetColor("_BaseColor", createWith.Value);
            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ── 캐릭터 ────────────────────────────────────────────────

        /// <summary>
        /// PC 캐릭터를 다시 만든다. 1일차와 달리 카메라가 기준점의 자식이라 뒤로 물러날 수 있고, 블록 몸과 이름표가 붙는다.
        /// </summary>
        private static GameObject CreatePlayerPrefab(InputActionAsset actions)
        {
            var root = new GameObject(PlayerName);

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

            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(pivot.transform, false);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = GameSettings.DefaultFieldOfView;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.Lerp(AtelierPalette.Ivory, AtelierPalette.Blue, 0.2f);
            cameraObject.AddComponent<AudioListener>();

            GameObject nameplate = BuildNameplate(root.transform);
            AvatarView avatar = BuildAvatar(root.transform, nameplate);

            var controller = root.AddComponent<DesktopPlayerController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("actions").objectReferenceValue = actions;
            serialized.FindProperty("cameraPivot").objectReferenceValue = pivot.transform;
            serialized.FindProperty("viewCamera").objectReferenceValue = camera;
            serialized.FindProperty("avatar").objectReferenceValue = avatar;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>정육면체로 만든 블록 캐릭터. 발바닥이 y 0이고 앞은 +z 쪽이다.</summary>
        private static AvatarView BuildAvatar(Transform parent, GameObject nameplate)
        {
            Material skin = BlockMaterial("Block_Ivory");
            Material top = BlockMaterial("Block_Blue");
            Material accent = BlockMaterial("Block_Gold");
            Material ink = BlockMaterial("Block_Ink", AtelierPalette.Ink);

            var root = new GameObject("Avatar");
            root.transform.SetParent(parent, false);
            var renderers = new List<Renderer>();

            Part("Torso", root.transform, new Vector3(0f, 1f, 0f), new Vector3(0.52f, 0.6f, 0.28f), top, renderers);
            Part("Head", root.transform, new Vector3(0f, 1.53f, 0f), new Vector3(0.42f, 0.42f, 0.42f), skin, renderers);
            Part("Cap", root.transform, new Vector3(0f, 1.78f, 0f), new Vector3(0.46f, 0.08f, 0.46f), accent, renderers);
            Part("CapBrim", root.transform, new Vector3(0f, 1.76f, 0.29f), new Vector3(0.3f, 0.04f, 0.14f), accent, renderers);
            Part("EyeLeft", root.transform, new Vector3(-0.09f, 1.56f, 0.211f), new Vector3(0.06f, 0.08f, 0.02f), ink, renderers);
            Part("EyeRight", root.transform, new Vector3(0.09f, 1.56f, 0.211f), new Vector3(0.06f, 0.08f, 0.02f), ink, renderers);

            Transform leftArm = Limb("ArmLeft", root.transform, new Vector3(-0.35f, 1.28f, 0f), new Vector3(0.16f, 0.58f, 0.2f), top, renderers);
            Transform rightArm = Limb("ArmRight", root.transform, new Vector3(0.35f, 1.28f, 0f), new Vector3(0.16f, 0.58f, 0.2f), top, renderers);
            Transform leftLeg = Limb("LegLeft", root.transform, new Vector3(-0.13f, 0.7f, 0f), new Vector3(0.22f, 0.7f, 0.24f), ink, renderers);
            Transform rightLeg = Limb("LegRight", root.transform, new Vector3(0.13f, 0.7f, 0f), new Vector3(0.22f, 0.7f, 0.24f), ink, renderers);

            var view = root.AddComponent<AvatarView>();
            var serialized = new SerializedObject(view);
            SerializedProperty bodies = serialized.FindProperty("bodyRenderers");
            bodies.arraySize = renderers.Count;
            for (int i = 0; i < renderers.Count; i++)
            {
                bodies.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }

            serialized.FindProperty("leftArm").objectReferenceValue = leftArm;
            serialized.FindProperty("rightArm").objectReferenceValue = rightArm;
            serialized.FindProperty("leftLeg").objectReferenceValue = leftLeg;
            serialized.FindProperty("rightLeg").objectReferenceValue = rightLeg;
            serialized.FindProperty("nameplate").objectReferenceValue = nameplate;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void Part(string name, Transform parent, Vector3 position, Vector3 size, Material material, List<Renderer> renderers)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());

            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderers.Add(renderer);
        }

        /// <summary>어깨나 엉덩이를 축으로 흔들리는 팔다리. 축 아래로 몸통이 매달린다.</summary>
        private static Transform Limb(string name, Transform parent, Vector3 joint, Vector3 size, Material material, List<Renderer> renderers)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = joint;
            Part("Mesh", pivot.transform, new Vector3(0f, -size.y * 0.5f, 0f), size, material, renderers);
            return pivot.transform;
        }

        /// <summary>VRChat처럼 머리 위에 뜨는 이름표. 3인칭일 때만 보인다.</summary>
        private static GameObject BuildNameplate(Transform parent)
        {
            var gameObject = new GameObject("Nameplate", typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

            // 크기는 캔버스를 월드 공간으로 바꾼 뒤에 정해야 화면 크기 값으로 덮이지 않는다. 가로 0.84m, 세로 0.22m쯤 된다.
            rect.anchorMin = UiFactory.Center;
            rect.anchorMax = UiFactory.Center;
            rect.pivot = UiFactory.Center;
            rect.sizeDelta = new Vector2(240f, 64f);
            rect.localScale = Vector3.one * 0.0035f;
            rect.localPosition = new Vector3(0f, 2.06f, 0f);

            Image pill = UiFactory.Box("Pill", rect, AtelierPalette.WithAlpha(AtelierPalette.Ink, 0.88f), 32f);
            UiFactory.Fill(pill.rectTransform);

            Image status = UiFactory.Box("Status", pill.transform, new Color32(0x7B, 0xD3, 0xA0, 0xFF), 9f);
            UiFactory.Place(status.rectTransform, UiFactory.MiddleLeft, new Vector2(24f, 0f), new Vector2(18f, 18f));

            TMP_Text label = UiFactory.Text("Label", pill.transform, "손님", 34f, AtelierPalette.Ivory, true, TextAlignmentOptions.Center);
            UiFactory.Fill(label.rectTransform, 46f, 0f, 22f, 0f);

            UiFactory.SetLayer(gameObject, 0);

            var nameplate = gameObject.AddComponent<Nameplate>();
            var serialized = new SerializedObject(nameplate);
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return gameObject;
        }

        // ── 게임 화면과 씬 ────────────────────────────────────────────

        private static GameObject CreateGameUiPrefab(InputActionAsset actions, GameUiBuilder.Icons icons, GameUiBuilder.Item[] items)
        {
            GameObject root = GameUiBuilder.Build(actions, icons, items);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GameUiPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Sandbox 씬의 캐릭터를 새 프리팹으로 바꾸고 게임 화면을 놓는다. 블록과 바닥은 그대로 둔다.</summary>
        private static void UpdateSandboxScene(GameObject player, GameObject gameUi)
        {
            if (!File.Exists(SandboxScene)) throw new InvalidOperationException($"씬을 찾을 수 없습니다: {SandboxScene}. 1일차 셋업을 먼저 실행하세요.");

            Scene scene = EditorSceneManager.OpenScene(SandboxScene, OpenSceneMode.Single);
            foreach (GameObject existing in scene.GetRootGameObjects())
            {
                if (existing.name == PlayerName || existing.name == GameUiName) UnityEngine.Object.DestroyImmediate(existing);
            }

            var spawned = (GameObject)PrefabUtility.InstantiatePrefab(player, scene);
            spawned.transform.SetPositionAndRotation(SpawnPosition, Quaternion.identity);
            PrefabUtility.InstantiatePrefab(gameUi, scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
