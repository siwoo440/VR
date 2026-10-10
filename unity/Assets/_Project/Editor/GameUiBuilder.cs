using System.Collections.Generic;
using AtelierVerse.Core;
using AtelierVerse.UI;
using AtelierVerse.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 게임 화면(GameUI 프리팹)을 조립한다. 일차가 지나며 화면에 요소가 늘면 이곳에 더하고, 그 일차의 셋업이 다시 조립한다.
    /// 늘 보이는 화면은 로블록스처럼 위쪽 띠·사람들 목록·아래쪽 부품 칸으로 두고,
    /// Esc 메뉴는 로블록스의 탭과 아래 단추에 VRChat의 큰 타일을 섞었다.
    /// 기준 화면 크기는 1920×1080이고 위치와 크기는 그 기준의 값이다.
    /// VR에서는 같은 화면을 눈앞의 판으로 띄운다(XrUiPanel). 그때 늘 보이는 화면(Hud)과 메뉴 뒤의 어두운 막은 감추고,
    /// 알림 띠와 메뉴만 남긴다. 메뉴 안에서 한쪽 조작에만 맞는 안내는 DesktopOnly·VrOnly로 모아 QuickMenuView가 켜고 끈다.
    /// VR에서 만들 때 필요한 부품 칸과 상태 표시는 왼손 위에 뜨는 작은 판(HandPalette)에 따로 둔다.
    /// 부품 칸에 넣을 부품을 고르는 창(PartPicker)은 메뉴처럼 화면 가운데에 뜨고, VR에서는 눈앞의 판에 뜬다(16일차).
    /// 맵 목록 창(MapList)도 같은 방식이며, 메뉴의 "내 작업실" 타일로 연다(17일차).
    /// 맵 목록의 줄에서 맵 정보 창(MapInfo)으로 넘어가 이름·설명·대표 그림·시작 위치를 고친다(18일차).
    /// </summary>
    internal static class GameUiBuilder
    {
        public struct Icons
        {
            public Sprite Menu;
            public Sprite Respawn;
            public Sprite View;
            public Sprite Home;
            public Sprite Map;
            public Sprite Avatar;
            public Sprite Shield;
        }

        public struct Item
        {
            public string Name;
            public Color Color;

            public Item(string name, Color color)
            {
                Name = name;
                Color = color;
            }
        }

        /// <summary>2일차 셋업이 그려 둔 블록 그림을 불러온다. 뒤 일차의 셋업이 화면을 다시 조립할 때 쓴다.</summary>
        public static Icons LoadIcons()
        {
            return new Icons
            {
                Menu = UiFactory.LoadSprite("Icon_Menu"),
                Respawn = UiFactory.LoadSprite("Icon_Respawn"),
                View = UiFactory.LoadSprite("Icon_View"),
                Home = UiFactory.LoadSprite("Icon_Home"),
                Map = UiFactory.LoadSprite("Icon_Map"),
                Avatar = UiFactory.LoadSprite("Icon_Avatar"),
                Shield = UiFactory.LoadSprite("Icon_Shield"),
            };
        }

        private const float Margin = 28f;
        private const int SlotCount = 9;
        private const float SlotSize = 72f;
        private const float SlotGap = 8f;
        private const int DefaultFilledSlots = 6;
        private const string CatalogPath = "Assets/_Project/Data/PartCatalog.asset";
        private const float PanelWidth = 1040f;
        private const float PanelHeight = 664f;
        private const float PanelPadding = 36f;

        private static readonly Color Paper = AtelierPalette.Ivory;
        private static readonly Color Surface = AtelierPalette.Surface;
        private static readonly Color Ink = AtelierPalette.Ink;
        private static readonly Color Muted = AtelierPalette.Muted;
        private static readonly Color Line = AtelierPalette.Line;
        private static readonly Color Gold = AtelierPalette.Gold;
        private static readonly Color DarkGlass = AtelierPalette.WithAlpha(AtelierPalette.Ink, 0.86f);

        // 메뉴를 조립하는 동안 모아 두는 것: 키보드·마우스에서만 보일 것과 VR에서만 보일 것.
        private static readonly List<Object> DesktopOnly = new List<Object>();
        private static readonly List<Object> VrOnly = new List<Object>();

        // 키보드·마우스에만 해당하는 설정의 줄. VR에서 메뉴가 흐리게 하고 누를 수 없게 한다(20일차).
        private static readonly List<Object> DesktopOnlyGroups = new List<Object>();

        /// <summary>
        /// 16일차의 셋업이 그려 둔 모양 그림(블록, 판, 기둥, 경사, 계단)을 불러온다. 하나라도 없으면 null이다.
        /// 그림이 없으면 부품 칸은 둥근 네모로 그린다(앞 일차의 셋업만 적용된 상태).
        /// </summary>
        public static Sprite[] LoadShapeSprites()
        {
            var sprites = new Sprite[PartMeshes.ShapeCount];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiFactory.ArtDir}/Shape_{(PartShape)i}.png");
                if (sprites[i] == null) return null;
            }

            return sprites;
        }

        /// <summary>
        /// 앞 일차의 셋업이 부르는 것. 부품의 이름과 색은 이제 부품 목록에서 읽으므로 items는 쓰지 않는다.
        /// </summary>
        public static GameObject Build(InputActionAsset actions, Icons icons, Item[] items)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PartCatalog>(CatalogPath);
            if (catalog == null) throw new System.InvalidOperationException($"부품 목록을 찾을 수 없습니다: {CatalogPath}. 3일차 셋업을 먼저 실행하세요.");
            return Build(actions, icons, catalog, LoadShapeSprites());
        }

        public static GameObject Build(InputActionAsset actions, Icons icons, PartCatalog catalog, Sprite[] shapeSprites)
        {
            DesktopOnly.Clear();
            VrOnly.Clear();
            DesktopOnlyGroups.Clear();

            var root = new GameObject("GameUI");
            var ui = root.AddComponent<GameUi>();

            // 소리: 무슨 일이 일어났는지 듣고 소리를 고르는 부품과, 실제로 소리를 내는 부품(19일차).
            root.AddComponent<SfxPlayer>();
            root.AddComponent<GameSounds>();

            // 화면: 설정의 화면 품질과 수직 동기화를 실제 그리기에 적용하는 부품(20일차).
            root.AddComponent<GraphicsApplier>();

            RectTransform canvas = CreateCanvas(root.transform, out TrackedDeviceRaycaster trackedRaycaster);
            RectTransform hud = UiFactory.Rect("Hud", canvas);
            UiFactory.Fill(hud);

            Button menuButton = BuildMenuButton(hud, icons.Menu);
            TMP_Text roomLabel = BuildRoomChip(hud);
            PeopleListView peoplePanel = BuildPeoplePanel(hud);
            HotbarView hotbar = BuildHotbar(hud, catalog, shapeSprites);
            GameObject buildHint = BuildBuildHint(hud, out TMP_Text blockCountLabel, out TMP_Text rotateLabel, out TMP_Text grabLabel, out TMP_Text snapLabel);
            BuildKeyHints(hud);
            TMP_Text viewLabel = BuildViewChip(hud);
            TMP_Text saveLabel = BuildSaveChip(hud);
            TMP_Text modeLabel = BuildModeChip(hud, out Image modeDot);
            GameObject crosshair = BuildCrosshair(hud);
            GameObject focusHint = BuildFocusHint(hud);

            // 알림 띠는 VR에서도 보여야 하므로 늘 보이는 화면(Hud) 밖에 둔다. 메뉴보다 먼저 만들어 메뉴 아래에 그려지게 한다.
            BuildNoticeBar(canvas);

            // 부품 고르는 창의 키 딱지도 메뉴가 조작 방식에 맞춰 켜고 끄므로 메뉴보다 먼저 만들고, 그리는 순서는 메뉴 위로 옮긴다.
            PartPickerView picker = BuildPartPicker(canvas, catalog, shapeSprites, out GameObject pickerScrim);
            MapListView mapList = BuildMapList(canvas, out GameObject mapListScrim);
            MapInfoView mapInfo = BuildMapInfo(canvas, out GameObject mapInfoScrim);
            QuickMenuView menu = BuildQuickMenu(canvas, icons, out TMP_Text menuRoomLabel, out TMP_Text menuNameLabel, out TMP_Text brandLabel, out PeopleListView menuPeople, out GameObject scrim);
            picker.transform.SetAsLastSibling();
            mapList.transform.SetAsLastSibling();
            mapInfo.transform.SetAsLastSibling();
            InputSystemUIInputModule inputModule = CreateEventSystem(root.transform, actions);
            XrUiPanel xrPanel = AddXrPanel(canvas, trackedRaycaster, inputModule, hud.gameObject, scrim, pickerScrim, mapListScrim, mapInfoScrim);
            XrHandPalette palette = BuildHandPalette(root.transform, catalog, shapeSprites);

            var serialized = new SerializedObject(ui);
            serialized.FindProperty("actions").objectReferenceValue = actions;
            serialized.FindProperty("xrPanel").objectReferenceValue = xrPanel;
            serialized.FindProperty("palette").objectReferenceValue = palette;
            serialized.FindProperty("hotbar").objectReferenceValue = hotbar;
            serialized.FindProperty("menu").objectReferenceValue = menu;
            serialized.FindProperty("picker").objectReferenceValue = picker;
            serialized.FindProperty("mapList").objectReferenceValue = mapList;
            serialized.FindProperty("mapInfo").objectReferenceValue = mapInfo;
            serialized.FindProperty("peoplePanel").objectReferenceValue = peoplePanel.gameObject;
            serialized.FindProperty("crosshair").objectReferenceValue = crosshair;
            serialized.FindProperty("focusHint").objectReferenceValue = focusHint;
            serialized.FindProperty("buildHint").objectReferenceValue = buildHint;
            serialized.FindProperty("blockCountLabel").objectReferenceValue = blockCountLabel;
            serialized.FindProperty("rotateLabel").objectReferenceValue = rotateLabel;
            serialized.FindProperty("grabLabel").objectReferenceValue = grabLabel;
            serialized.FindProperty("snapLabel").objectReferenceValue = snapLabel;
            serialized.FindProperty("hintTextColor").colorValue = Paper;
            serialized.FindProperty("hintActiveColor").colorValue = Gold;
            serialized.FindProperty("menuButton").objectReferenceValue = menuButton;
            serialized.FindProperty("roomLabel").objectReferenceValue = roomLabel;
            serialized.FindProperty("menuRoomLabel").objectReferenceValue = menuRoomLabel;
            serialized.FindProperty("menuNameLabel").objectReferenceValue = menuNameLabel;
            serialized.FindProperty("brandLabel").objectReferenceValue = brandLabel;
            serialized.FindProperty("viewLabel").objectReferenceValue = viewLabel;
            serialized.FindProperty("saveLabel").objectReferenceValue = saveLabel;
            serialized.FindProperty("saveTextColor").colorValue = Paper;
            serialized.FindProperty("saveFailedColor").colorValue = Gold;
            serialized.FindProperty("modeLabel").objectReferenceValue = modeLabel;
            serialized.FindProperty("modeDot").objectReferenceValue = modeDot;
            serialized.FindProperty("walkDotColor").colorValue = AtelierPalette.Leaf;
            serialized.FindProperty("flyDotColor").colorValue = Gold;
            SetObjects(serialized.FindProperty("peopleLists"), peoplePanel, menuPeople);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static RectTransform CreateCanvas(Transform parent, out TrackedDeviceRaycaster trackedRaycaster)
        {
            var gameObject = new GameObject("Canvas", typeof(RectTransform));
            gameObject.layer = UiFactory.UiLayer;
            gameObject.transform.SetParent(parent, false);

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            // VR 컨트롤러의 광선으로 누르기 위한 부품. PC에서는 꺼 두고 XrUiPanel이 VR일 때만 켠다.
            trackedRaycaster = gameObject.AddComponent<TrackedDeviceRaycaster>();
            trackedRaycaster.enabled = false;
            return (RectTransform)gameObject.transform;
        }

        /// <summary>
        /// 마우스와 VR 컨트롤러의 광선으로만 누른다. 키보드로 옮겨 다니는 선택을 끄지 않으면 Space나 WASD가 단추를 누르게 된다.
        /// 누르는 입력은 이 프로젝트의 입력 자산(UI 묶음)에 잇는다. 마우스 쪽은 입력 시스템의 기본 정의와 같고, 추적 기기는 오른손만 쓴다.
        /// </summary>
        private static InputSystemUIInputModule CreateEventSystem(Transform parent, InputActionAsset actions)
        {
            var gameObject = new GameObject("EventSystem");
            gameObject.transform.SetParent(parent, false);

            var eventSystem = gameObject.AddComponent<EventSystem>();
            eventSystem.sendNavigationEvents = false;

            var module = gameObject.AddComponent<InputSystemUIInputModule>();
            var serialized = new SerializedObject(module);
            serialized.FindProperty("m_ActionsAsset").objectReferenceValue = actions;
            SetAction(serialized, "m_PointAction", actions, "UI/Point");
            SetAction(serialized, "m_LeftClickAction", actions, "UI/Click");
            SetAction(serialized, "m_RightClickAction", actions, "UI/RightClick");
            SetAction(serialized, "m_MiddleClickAction", actions, "UI/MiddleClick");
            SetAction(serialized, "m_ScrollWheelAction", actions, "UI/ScrollWheel");
            SetAction(serialized, "m_TrackedDevicePositionAction", actions, "UI/TrackedDevicePosition");
            SetAction(serialized, "m_TrackedDeviceOrientationAction", actions, "UI/TrackedDeviceOrientation");
            serialized.FindProperty("m_MoveAction").objectReferenceValue = null;
            serialized.FindProperty("m_SubmitAction").objectReferenceValue = null;
            serialized.FindProperty("m_CancelAction").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return module;
        }

        /// <summary>입력 자산이 가져올 때 만들어 둔 동작 참조를 찾아 넣는다. 프리팹에 저장되려면 자산 안의 참조여야 한다.</summary>
        private static void SetAction(SerializedObject serialized, string property, InputActionAsset actions, string actionPath)
        {
            InputAction action = actions.FindAction(actionPath);
            if (action == null) throw new System.InvalidOperationException($"입력 자산에 {actionPath} 동작이 없습니다. AtelierInput.inputactions를 확인하세요.");

            InputActionReference reference = null;
            foreach (Object item in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(actions)))
            {
                if (item is InputActionReference candidate && candidate.action != null && candidate.action.id == action.id)
                {
                    reference = candidate;
                    break;
                }
            }

            if (reference == null) throw new System.InvalidOperationException($"입력 자산에서 {actionPath} 동작의 참조를 찾지 못했습니다. 입력 자산을 다시 가져오세요.");
            serialized.FindProperty(property).objectReferenceValue = reference;
        }

        /// <summary>VR에서 화면을 눈앞의 판으로 띄우는 스크립트. 판으로 띄울 때 감출 것(늘 보이는 화면, 메뉴 뒤의 막)을 알려 둔다.</summary>
        private static XrUiPanel AddXrPanel(RectTransform canvas, TrackedDeviceRaycaster trackedRaycaster, InputSystemUIInputModule inputModule, params GameObject[] screenOnly)
        {
            var panel = canvas.gameObject.AddComponent<XrUiPanel>();
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("canvas").objectReferenceValue = canvas.GetComponent<Canvas>();
            serialized.FindProperty("trackedRaycaster").objectReferenceValue = trackedRaycaster;
            serialized.FindProperty("inputModule").objectReferenceValue = inputModule;
            SetObjects(serialized.FindProperty("screenOnly"), screenOnly);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        // ── 늘 보이는 화면 ─────────────────────────────────────────────

        private static Button BuildMenuButton(RectTransform hud, Sprite icon)
        {
            Image box = UiFactory.Card("MenuButton", hud, Paper, Ink, 18f);
            UiFactory.Place(box.rectTransform, UiFactory.TopLeft, new Vector2(Margin, -Margin), new Vector2(64f, 64f));
            UiFactory.Icon("Icon", box.transform, icon, Ink, 36f);
            return UiFactory.Clickable(box);
        }

        private static TMP_Text BuildRoomChip(RectTransform hud)
        {
            Image chip = UiFactory.Card("RoomChip", hud, Paper, Ink, 28f);

            Image status = UiFactory.Box("Status", chip.transform, AtelierPalette.Leaf, 8f);
            UiFactory.Place(status.rectTransform, UiFactory.MiddleLeft, new Vector2(22f, 0f), new Vector2(16f, 16f));

            TMP_Text label = UiFactory.Text("Label", chip.transform, "시험 작업실  1/8", 24f, Ink, true);
            UiFactory.Fill(label.rectTransform, 50f, 0f, 20f, 0f);
            // 맵의 이름이 길면 줄여 보인다. 칸의 너비는 실행 중에 이름에 맞춘다(GameUi.FitRoomChip).
            label.overflowMode = TextOverflowModes.Ellipsis;

            float width = 50f + UiFactory.WidthOf(label) + 30f;
            UiFactory.Place(chip.rectTransform, UiFactory.TopLeft, new Vector2(Margin + 64f + 16f, -Margin - 4f), new Vector2(width, 56f));
            return label;
        }

        private static PeopleListView BuildPeoplePanel(RectTransform hud)
        {
            Image card = UiFactory.Card("People", hud, Paper, Ink, 20f);
            UiFactory.Place(card.rectTransform, UiFactory.TopRight, new Vector2(-Margin, -Margin), new Vector2(300f, 126f));

            TMP_Text header = UiFactory.Text("Header", card.transform, "사람들 1/8", 22f, Ink, true);
            UiFactory.Place(header.rectTransform, UiFactory.TopLeft, new Vector2(22f, -12f), new Vector2(200f, 34f));

            RectTransform key = UiFactory.Badge("Key", card.transform, "Tab", Surface, Muted);
            UiFactory.Place(key, UiFactory.TopRight, new Vector2(-18f, -15f), key.sizeDelta);

            UiFactory.Divider(card.transform, -56f, 18f, Line);
            TMP_Text localName = BuildPersonRow(card.transform, 22f, -72f, 36f);

            var view = card.gameObject.AddComponent<PeopleListView>();
            SetPeopleList(view, header, localName);
            return view;
        }

        /// <summary>사람 한 줄: 캐릭터 색 네모, 이름, 자기 자신 표시.</summary>
        private static TMP_Text BuildPersonRow(Transform parent, float x, float y, float size)
        {
            Image swatch = UiFactory.Box("Swatch", parent, AtelierPalette.Blue, 8f);
            UiFactory.Place(swatch.rectTransform, UiFactory.TopLeft, new Vector2(x, y), new Vector2(size, size));

            TMP_Text localName = UiFactory.Text("Name", parent, "손님", 22f, Ink);
            UiFactory.Place(localName.rectTransform, UiFactory.TopLeft, new Vector2(x + size + 12f, y), new Vector2(170f, size));

            RectTransform me = UiFactory.Badge("Me", parent, "나", Gold, Ink);
            UiFactory.Place(me, UiFactory.TopRight, new Vector2(-18f, y - (size - me.sizeDelta.y) * 0.5f), me.sizeDelta);
            return localName;
        }

        private static HotbarView BuildHotbar(RectTransform hud, PartCatalog catalog, Sprite[] shapeSprites)
        {
            float width = SlotCount * SlotSize + (SlotCount - 1) * SlotGap;
            RectTransform bar = UiFactory.Rect("Hotbar", hud);
            UiFactory.Place(bar, UiFactory.BottomCenter, new Vector2(0f, Margin), new Vector2(width, SlotSize));

            var view = bar.gameObject.AddComponent<HotbarView>();
            var serialized = new SerializedObject(view);
            AddSlots(serialized, bar, catalog, shapeSprites, SlotSize, false);

            Image pill = UiFactory.Box("SelectedPill", hud, Ink, 20f);
            UiFactory.Place(pill.rectTransform, UiFactory.BottomCenter, new Vector2(0f, Margin + SlotSize + 26f), new Vector2(240f, 40f));
            TMP_Text label = UiFactory.Text("Label", pill.transform, string.Empty, 20f, Paper, true, TextAlignmentOptions.Center);
            UiFactory.Fill(label.rectTransform);
            pill.gameObject.SetActive(false);

            serialized.FindProperty("selectedPill").objectReferenceValue = pill.gameObject;
            serialized.FindProperty("selectedLabel").objectReferenceValue = label;
            serialized.FindProperty("frameColor").colorValue = Line;
            serialized.FindProperty("selectedFrameColor").colorValue = Gold;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>
        /// 처음에 부품 칸에 넣어 두는 부품. 앞의 여섯 칸에 블록 여섯 색을 넣고 나머지는 비워 둔다.
        /// 다른 모양은 부품 고르는 창에서 넣는다.
        /// </summary>
        private static int[] DefaultParts(PartCatalog catalog)
        {
            var parts = new int[SlotCount];
            int next = 0;
            for (int i = 0; i < parts.Length; i++) parts[i] = HotbarModel.None;

            for (int part = 0; part < catalog.Count && next < DefaultFilledSlots; part++)
            {
                if (catalog.Get(part).shape == PartShape.Block) parts[next++] = part;
            }

            return parts;
        }

        /// <summary>
        /// 부품의 모양과 색을 보이는 작은 그림. 모양 그림(16일차 셋업이 그린 것)이 있으면 그것에 색을 입히고,
        /// 없으면 둥근 네모로 그린다. 부품이 없으면 꺼 둔 채로 만든다(실행 중에 PartSwatch가 켜고 바꾼다).
        /// </summary>
        private static Image Swatch(Transform parent, PartCatalog catalog, int part, Sprite[] shapeSprites, float size, Vector2 offset)
        {
            bool has = catalog != null && catalog.IsValid(part);
            Color color = has ? catalog.Get(part).color : Paper;
            Image image;

            if (shapeSprites != null && shapeSprites.Length > 0)
            {
                int shape = has ? Mathf.Clamp((int)catalog.Get(part).shape, 0, shapeSprites.Length - 1) : 0;
                image = UiFactory.Icon("Swatch", parent, shapeSprites[shape], color, size);
                image.rectTransform.anchoredPosition = offset;
            }
            else
            {
                image = UiFactory.Box("Swatch", parent, color, 8f);
                UiFactory.Place(image.rectTransform, UiFactory.Center, offset, new Vector2(size * 0.86f, size * 0.86f));
                UiFactory.Outline(image, AtelierPalette.WithAlpha(Ink, 0.55f));
            }

            image.gameObject.SetActive(has);
            return image;
        }

        /// <summary>
        /// 부품 칸의 칸 아홉 개를 만든다. 화면 아래의 부품 칸과 VR의 부품 판이 함께 쓴다.
        /// size는 칸 한 변의 길이이며 안의 숫자와 부품 그림은 그 크기에 맞춘다. clickable이면 칸을 눌러 고를 수 있다(VR의 부품 판).
        /// 칸에 든 부품은 실행 중에 바뀌므로 모든 칸에 그림 자리를 두고, 무엇을 그릴지는 HotbarView가 정한다.
        /// </summary>
        private static void AddSlots(SerializedObject serialized, RectTransform bar, PartCatalog catalog, Sprite[] shapeSprites, float size, bool clickable)
        {
            float scale = size / SlotSize;
            int[] defaults = DefaultParts(catalog);
            SerializedProperty slots = serialized.FindProperty("slots");
            slots.arraySize = SlotCount;

            for (int i = 0; i < SlotCount; i++)
            {
                Image slot = UiFactory.Box($"Slot{i + 1}", bar, Paper, 14f);
                UiFactory.Place(slot.rectTransform, UiFactory.BottomLeft, new Vector2(i * (size + SlotGap), 0f), new Vector2(size, size));

                var shadow = slot.gameObject.AddComponent<Shadow>();
                shadow.effectColor = Ink;
                shadow.effectDistance = new Vector2(4f, -4f);
                Image frame = UiFactory.Outline(slot, Line);

                TMP_Text number = UiFactory.Text("Number", slot.transform, (i + 1).ToString(), 16f, Muted, true);
                UiFactory.Place(number.rectTransform, UiFactory.TopLeft, new Vector2(10f * scale, -4f * scale), new Vector2(20f, 22f));

                Image swatch = Swatch(slot.transform, catalog, defaults[i], shapeSprites, 44f * scale, new Vector2(0f, -5f * scale));

                // 빈 칸도 누를 수 있다. 누르면 부품 고르는 창이 그 칸에 넣도록 열린다.
                Button button = clickable ? UiFactory.Clickable(slot) : null;

                SerializedProperty element = slots.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = slot.rectTransform;
                element.FindPropertyRelative("frame").objectReferenceValue = frame;
                element.FindPropertyRelative("swatch").objectReferenceValue = swatch;
                element.FindPropertyRelative("button").objectReferenceValue = button;
            }

            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            SetObjects(serialized.FindProperty("shapeSprites"), shapeSprites ?? new Sprite[0]);
            SerializedProperty defaultParts = serialized.FindProperty("defaultParts");
            defaultParts.arraySize = defaults.Length;
            for (int i = 0; i < defaults.Length; i++)
            {
                defaultParts.GetArrayElementAtIndex(i).intValue = defaults[i];
            }
        }

        /// <summary>
        /// VR에서 왼손 위에 뜨는 부품 판(13일차). 화가의 팔레트처럼 왼손에 들고 오른손 광선으로 가리켜 누른다.
        /// 위에는 고른 부품의 이름과 블록 수와 걷기·날기 표시, 가운데에는 부품 칸,
        /// 아래에는 되돌리기·다시 실행·맞추기·부품 창 단추, 맨 아래 두 줄에는 놓기·지우기·칠하기와 돌리기·옮기기의 단추 안내가 있다.
        /// PC에서는 보이지 않는다. 판은 월드 공간의 작은 캔버스이며 XrHandPalette가 크기와 자리를 정한다. 꺼 둔 채로 저장한다.
        /// </summary>
        private static XrHandPalette BuildHandPalette(Transform parent, PartCatalog catalog, Sprite[] shapeSprites)
        {
            const float slotSize = 64f;
            const float pad = 40f;
            const float width = pad * 2f + SlotCount * slotSize + (SlotCount - 1) * SlotGap;
            const float height = 290f;
            const float slotsTop = 70f;
            const float buttonsTop = slotsTop + slotSize + 18f;
            const float hintTop = buttonsTop + 46f + 14f;
            const float modeWidth = 124f;

            var holder = new GameObject("HandPalette");
            holder.transform.SetParent(parent, false);
            var palette = holder.AddComponent<XrHandPalette>();

            var canvasObject = new GameObject("PaletteCanvas", typeof(RectTransform));
            canvasObject.layer = UiFactory.UiLayer;
            canvasObject.transform.SetParent(holder.transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            canvasObject.AddComponent<TrackedDeviceRaycaster>();

            var rect = (RectTransform)canvasObject.transform;
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one * 0.00045f;

            Image card = UiFactory.Card("PaletteCard", rect, Paper, Ink, 24f, 6f);
            UiFactory.Fill(card.rectTransform);

            TMP_Text title = UiFactory.Text("PaletteTitle", card.transform, "부품을 고르세요", 26f, Ink, true);
            UiFactory.Place(title.rectTransform, UiFactory.TopLeft, new Vector2(pad, -18f), new Vector2(300f, 38f));

            // 위쪽 오른쪽: 블록 수와 걷기·날기 표시
            Image modeChip = UiFactory.Box("PaletteMode", card.transform, DarkGlass, 20f);
            UiFactory.Place(modeChip.rectTransform, UiFactory.TopRight, new Vector2(-pad, -16f), new Vector2(modeWidth, 40f));
            Image modeDot = UiFactory.Box("Dot", modeChip.transform, AtelierPalette.Leaf, 6f);
            UiFactory.Place(modeDot.rectTransform, UiFactory.MiddleLeft, new Vector2(18f, 0f), new Vector2(12f, 12f));
            TMP_Text modeLabel = UiFactory.Text("Label", modeChip.transform, "걷기", 20f, Paper, true, TextAlignmentOptions.Center);
            UiFactory.Fill(modeLabel.rectTransform, 32f, 0f, 12f, 0f);

            TMP_Text blockCount = UiFactory.Text("PaletteBlockCount", card.transform, "블록 0/500", 20f, Muted, true, TextAlignmentOptions.Right);
            UiFactory.Place(blockCount.rectTransform, UiFactory.TopRight, new Vector2(-pad - modeWidth - 14f, -22f), new Vector2(190f, 30f));

            RectTransform bar = UiFactory.Rect("PaletteHotbar", card.transform);
            UiFactory.Place(bar, UiFactory.TopLeft, new Vector2(pad, -slotsTop), new Vector2(width - pad * 2f, slotSize));

            var hotbar = bar.gameObject.AddComponent<HotbarView>();
            var hotbarSerialized = new SerializedObject(hotbar);
            AddSlots(hotbarSerialized, bar, catalog, shapeSprites, slotSize, true);
            hotbarSerialized.FindProperty("frameColor").colorValue = Line;
            hotbarSerialized.FindProperty("selectedFrameColor").colorValue = Gold;
            hotbarSerialized.FindProperty("selectedLift").floatValue = 8f;
            hotbarSerialized.ApplyModifiedPropertiesWithoutUndo();

            // 단추 줄: 되돌리기, 다시 실행, 맞추기(누를 때마다 다음 단계), 부품 창(부품 고르는 창 열기).
            const float historyWidth = 142f;
            const float snapWidth = 184f;
            const float partsWidth = 136f;
            const float buttonGap = 12f;

            Button undo = BigButton("Undo", card.transform, "되돌리기", null, Paper, Ink, out TMP_Text undoLabel);
            UiFactory.Place((RectTransform)undo.transform, UiFactory.TopLeft, new Vector2(pad, -buttonsTop), new Vector2(historyWidth, 46f));
            undoLabel.fontSize = 22f;

            Button redo = BigButton("Redo", card.transform, "다시 실행", null, Paper, Ink, out TMP_Text redoLabel);
            UiFactory.Place((RectTransform)redo.transform, UiFactory.TopLeft, new Vector2(pad + historyWidth + buttonGap, -buttonsTop), new Vector2(historyWidth, 46f));
            redoLabel.fontSize = 22f;

            Button snap = BigButton("Snap", card.transform, "맞추기 끔", null, Paper, Ink, out TMP_Text snapLabel);
            UiFactory.Place((RectTransform)snap.transform, UiFactory.TopLeft, new Vector2(pad + (historyWidth + buttonGap) * 2f, -buttonsTop), new Vector2(snapWidth, 46f));
            snapLabel.fontSize = 22f;

            Button parts = BigButton("Parts", card.transform, "부품 창", null, Gold, Ink, out TMP_Text partsLabel);
            UiFactory.Place((RectTransform)parts.transform, UiFactory.TopRight, new Vector2(-pad, -buttonsTop), new Vector2(partsWidth, 46f));
            partsLabel.fontSize = 22f;

            TMP_Text hint = UiFactory.Text("PaletteHint", card.transform, "오른손 방아쇠 놓기 · 옆 단추 지우기 · 왼손 방아쇠 칠하기", 18f, Muted);
            UiFactory.Place(hint.rectTransform, UiFactory.TopLeft, new Vector2(pad, -hintTop), new Vector2(width - pad * 2f, 26f));

            TMP_Text hint2 = UiFactory.Text("PaletteHint2", card.transform, "왼손 첫째 단추 돌리기 · 오른쪽 스틱 누르기 반대로 · 왼손 옆 단추 옮기기", 18f, Muted);
            UiFactory.Place(hint2.rectTransform, UiFactory.TopLeft, new Vector2(pad, -hintTop - 28f), new Vector2(width - pad * 2f, 26f));

            canvasObject.SetActive(false);

            var serialized = new SerializedObject(palette);
            serialized.FindProperty("canvas").objectReferenceValue = canvas;
            serialized.FindProperty("hotbar").objectReferenceValue = hotbar;
            serialized.FindProperty("titleLabel").objectReferenceValue = title;
            serialized.FindProperty("blockCountLabel").objectReferenceValue = blockCount;
            serialized.FindProperty("modeLabel").objectReferenceValue = modeLabel;
            serialized.FindProperty("modeDot").objectReferenceValue = modeDot;
            serialized.FindProperty("undoButton").objectReferenceValue = undo;
            serialized.FindProperty("redoButton").objectReferenceValue = redo;
            serialized.FindProperty("snapButton").objectReferenceValue = snap;
            serialized.FindProperty("snapLabel").objectReferenceValue = snapLabel;
            serialized.FindProperty("partsButton").objectReferenceValue = parts;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return palette;
        }

        /// <summary>
        /// 부품 고르는 창(16일차). 위에는 넣을 칸을 고르는 줄, 아래에는 모양마다 한 줄씩 부품이 있다.
        /// 부품을 누르면 넣을 칸에 들어간다. PC에서는 화면 가운데의 창으로, VR에서는 메뉴처럼 눈앞의 판에 뜬다.
        /// 뒤의 어두운 막은 VR에서 감추도록 따로 돌려준다.
        /// </summary>
        private static PartPickerView BuildPartPicker(RectTransform canvas, PartCatalog catalog, Sprite[] shapeSprites, out GameObject scrimObject)
        {
            const float targetTop = 108f;
            const float targetSize = 60f;
            const float gridLeft = PanelPadding + 110f;
            const float gridTop = targetTop + targetSize + 28f;
            const float cellSize = 58f;
            const float cellGap = 10f;
            const float rowHeight = cellSize + 8f;

            RectTransform holder = UiFactory.Rect("PartPicker", canvas);
            UiFactory.Fill(holder);
            var view = holder.gameObject.AddComponent<PartPickerView>();

            RectTransform root = UiFactory.Rect("Root", holder);
            UiFactory.Fill(root);

            RectTransform scrimRect = UiFactory.Rect("Scrim", root);
            UiFactory.Fill(scrimRect);
            var scrim = scrimRect.gameObject.AddComponent<Image>();
            scrim.color = AtelierPalette.Scrim;
            scrim.raycastTarget = true;
            scrimObject = scrimRect.gameObject;

            Image panel = UiFactory.Card("Panel", root, Paper, Ink, 28f, 8f);
            UiFactory.Place(panel.rectTransform, UiFactory.Center, Vector2.zero, new Vector2(PanelWidth, PanelHeight));

            TMP_Text title = UiFactory.Text("Title", panel.transform, "부품 고르기", 30f, Ink, true);
            UiFactory.Place(title.rectTransform, UiFactory.TopLeft, new Vector2(PanelPadding, -24f), new Vector2(400f, 40f));

            TMP_Text guide = UiFactory.Text("Guide", panel.transform, "넣을 칸을 고른 뒤 부품을 누르면 그 칸에 들어갑니다. 크기는 블록 한 변을 1로 적었습니다.", 20f, Muted);
            UiFactory.Place(guide.rectTransform, UiFactory.TopLeft, new Vector2(PanelPadding, -66f), new Vector2(PanelWidth - PanelPadding * 2f, 28f));

            // 넣을 칸: 아래쪽 부품 칸과 같은 아홉 칸
            TMP_Text targetTitle = UiFactory.Text("TargetTitle", panel.transform, "넣을 칸", 22f, Ink, true);
            UiFactory.Place(targetTitle.rectTransform, UiFactory.TopLeft, new Vector2(PanelPadding, -targetTop - 15f), new Vector2(100f, 30f));

            var targetButtons = new Button[SlotCount];
            var targetFrames = new Image[SlotCount];
            var targetSwatches = new Image[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                Image slot = UiFactory.Box($"Target{i + 1}", panel.transform, Paper, 12f);
                UiFactory.Place(slot.rectTransform, UiFactory.TopLeft, new Vector2(gridLeft + i * (targetSize + 8f), -targetTop), new Vector2(targetSize, targetSize));
                targetFrames[i] = UiFactory.Outline(slot, Line);

                TMP_Text number = UiFactory.Text("Number", slot.transform, (i + 1).ToString(), 14f, Muted, true);
                UiFactory.Place(number.rectTransform, UiFactory.TopLeft, new Vector2(8f, -3f), new Vector2(18f, 20f));

                targetSwatches[i] = Swatch(slot.transform, catalog, HotbarModel.None, shapeSprites, 36f, new Vector2(0f, -4f));
                targetButtons[i] = UiFactory.Clickable(slot);
            }

            TMP_Text targetLabel = UiFactory.Text("TargetLabel", panel.transform, "1번 칸 · 비어 있음", 22f, Ink, true, TextAlignmentOptions.Right);
            UiFactory.Place(targetLabel.rectTransform, UiFactory.TopRight, new Vector2(-PanelPadding, -targetTop - 15f), new Vector2(240f, 30f));

            UiFactory.Divider(panel.transform, -targetTop - targetSize - 14f, PanelPadding, Line);

            // 부품: 모양마다 한 줄, 줄 안에서는 부품 목록의 순서(색의 순서)대로
            var cellButtons = new List<Button>();
            var cellParts = new List<int>();
            for (int shapeIndex = 0; shapeIndex < PartMeshes.ShapeCount; shapeIndex++)
            {
                var shape = (PartShape)shapeIndex;
                float y = gridTop + shapeIndex * rowHeight;
                int column = 0;

                for (int part = 0; part < catalog.Count; part++)
                {
                    PartCatalog.Part entry = catalog.Get(part);
                    if (entry.shape != shape) continue;

                    Image cell = UiFactory.Box($"Part_{entry.id}", panel.transform, Surface, 12f);
                    UiFactory.Place(cell.rectTransform, UiFactory.TopLeft, new Vector2(gridLeft + column * (cellSize + cellGap), -y), new Vector2(cellSize, cellSize));
                    UiFactory.Outline(cell, Line);
                    Swatch(cell.transform, catalog, part, shapeSprites, 42f, Vector2.zero);

                    cellButtons.Add(UiFactory.Clickable(cell));
                    cellParts.Add(part);
                    column++;
                }

                if (column == 0) continue;

                TMP_Text rowLabel = UiFactory.Text($"Row{shapeIndex}", panel.transform, PartMeshes.NameOf(shape), 24f, Ink, true);
                UiFactory.Place(rowLabel.rectTransform, UiFactory.TopLeft, new Vector2(PanelPadding, -y - 13f), new Vector2(100f, 32f));

                TMP_Text sizeLabel = UiFactory.Text($"Size{shapeIndex}", panel.transform, SizeText(shape), 20f, Muted);
                UiFactory.Place(sizeLabel.rectTransform, UiFactory.TopLeft, new Vector2(gridLeft + column * (cellSize + cellGap) + 14f, -y - 15f), new Vector2(430f, 28f));
            }

            Button clear = BigButton("ClearSlot", panel.transform, "칸 비우기", null, Paper, Ink, out _);
            UiFactory.Place((RectTransform)clear.transform, UiFactory.BottomLeft, new Vector2(PanelPadding, 34f), new Vector2(300f, 64f));

            Button close = BigButton("ClosePicker", panel.transform, "돌아가기", "B", Gold, Ink, out _);
            UiFactory.Place((RectTransform)close.transform, UiFactory.BottomRight, new Vector2(-PanelPadding, 34f), new Vector2(300f, 64f));

            root.gameObject.SetActive(false);

            var serialized = new SerializedObject(view);
            serialized.FindProperty("root").objectReferenceValue = root.gameObject;
            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            SetObjects(serialized.FindProperty("shapeSprites"), shapeSprites ?? new Sprite[0]);
            serialized.FindProperty("targetLabel").objectReferenceValue = targetLabel;
            serialized.FindProperty("clearButton").objectReferenceValue = clear;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("frameColor").colorValue = Line;
            serialized.FindProperty("targetFrameColor").colorValue = Gold;

            SerializedProperty cells = serialized.FindProperty("cells");
            cells.arraySize = cellButtons.Count;
            for (int i = 0; i < cellButtons.Count; i++)
            {
                SerializedProperty cell = cells.GetArrayElementAtIndex(i);
                cell.FindPropertyRelative("button").objectReferenceValue = cellButtons[i];
                cell.FindPropertyRelative("part").intValue = cellParts[i];
            }

            SerializedProperty targets = serialized.FindProperty("targets");
            targets.arraySize = SlotCount;
            for (int i = 0; i < SlotCount; i++)
            {
                SerializedProperty target = targets.GetArrayElementAtIndex(i);
                target.FindPropertyRelative("button").objectReferenceValue = targetButtons[i];
                target.FindPropertyRelative("frame").objectReferenceValue = targetFrames[i];
                target.FindPropertyRelative("swatch").objectReferenceValue = targetSwatches[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>
        /// 맵 목록 창(17일차). 이 기기에 저장된 맵을 한 줄씩 보이고, 줄마다 작은 대표 그림과 열기·정보·지우기 단추가 있다.
        /// 아래에는 새 맵, 쪽 넘기기, 돌아가기가 있다. 이름과 설명은 "정보"로 여는 맵 정보 창에서 고친다(18일차).
        /// PC에서는 화면 가운데의 창으로, VR에서는 메뉴처럼 눈앞의 판에 뜬다.
        /// 뒤의 어두운 막은 VR에서 감추도록 따로 돌려준다.
        /// </summary>
        private static MapListView BuildMapList(RectTransform canvas, out GameObject scrimObject)
        {
            const int rowCount = 6;
            const float rowsTop = 108f;
            const float rowHeight = 62f;
            const float rowGap = 8f;
            const float buttonHeight = 44f;
            const float textLeft = 116f;

            RectTransform holder = UiFactory.Rect("MapList", canvas);
            UiFactory.Fill(holder);
            var view = holder.gameObject.AddComponent<MapListView>();

            RectTransform root = UiFactory.Rect("Root", holder);
            UiFactory.Fill(root);

            RectTransform scrimRect = UiFactory.Rect("Scrim", root);
            UiFactory.Fill(scrimRect);
            var scrim = scrimRect.gameObject.AddComponent<Image>();
            scrim.color = AtelierPalette.Scrim;
            scrim.raycastTarget = true;
            scrimObject = scrimRect.gameObject;

            Image panel = UiFactory.Card("Panel", root, Paper, Ink, 28f, 8f);
            UiFactory.Place(panel.rectTransform, UiFactory.Center, Vector2.zero, new Vector2(PanelWidth, PanelHeight));

            TMP_Text title = UiFactory.Text("Title", panel.transform, "내 작업실", 30f, Ink, true);
            UiFactory.Place(title.rectTransform, UiFactory.TopLeft, new Vector2(PanelPadding, -24f), new Vector2(360f, 40f));

            TMP_Text guide = UiFactory.Text("Guide", panel.transform, "이 기기에 저장된 맵입니다. 다른 맵을 열면 지금 맵은 저장됩니다.", 20f, Muted);
            UiFactory.Place(guide.rectTransform, UiFactory.TopLeft, new Vector2(PanelPadding, -66f), new Vector2(PanelWidth - PanelPadding * 2f, 28f));

            // 맵 한 줄: 작은 대표 그림, 이름, 블록 수·저장한 때·설명, 오른쪽에 열기·정보·지우기
            var rowRoots = new GameObject[rowCount];
            var rowThumbnails = new RawImage[rowCount];
            var rowNames = new TMP_Text[rowCount];
            var rowInfos = new TMP_Text[rowCount];
            var rowBadges = new GameObject[rowCount];
            var rowOpens = new Button[rowCount];
            var rowInfoButtons = new Button[rowCount];
            var rowDeletes = new Button[rowCount];
            var rowDeleteLabels = new TMP_Text[rowCount];

            for (int i = 0; i < rowCount; i++)
            {
                Image row = UiFactory.Box($"Row{i + 1}", panel.transform, Surface, 16f);
                UiFactory.StretchTop(row.rectTransform, -rowsTop - i * (rowHeight + rowGap), rowHeight, PanelPadding);
                UiFactory.Outline(row, Line);

                rowThumbnails[i] = Thumbnail("Thumbnail", row.transform, new Vector2(96f, 54f), 10f, out _);
                UiFactory.Place((RectTransform)rowThumbnails[i].transform.parent, UiFactory.MiddleLeft, new Vector2(8f, 0f), new Vector2(96f, 54f));

                rowNames[i] = UiFactory.Text("Name", row.transform, "맵", 24f, Ink, true);
                UiFactory.Place(rowNames[i].rectTransform, UiFactory.TopLeft, new Vector2(textLeft, -5f), new Vector2(340f, 30f));
                rowNames[i].overflowMode = TextOverflowModes.Ellipsis;

                rowInfos[i] = UiFactory.Text("Info", row.transform, "블록 0개", 18f, Muted);
                UiFactory.Place(rowInfos[i].rectTransform, UiFactory.TopLeft, new Vector2(textLeft, -34f), new Vector2(514f, 24f));
                rowInfos[i].overflowMode = TextOverflowModes.Ellipsis;

                RectTransform badge = UiFactory.Badge("Current", row.transform, "지금 맵", Gold, Ink, 26f);
                UiFactory.Place(badge, UiFactory.TopLeft, new Vector2(textLeft + 348f, -6f), badge.sizeDelta);
                rowBadges[i] = badge.gameObject;

                rowDeletes[i] = BigButton("Delete", row.transform, "지우기", null, Paper, AtelierPalette.Clay, out rowDeleteLabels[i]);
                UiFactory.Place((RectTransform)rowDeletes[i].transform, UiFactory.MiddleRight, new Vector2(-14f, 0f), new Vector2(112f, buttonHeight));
                rowDeleteLabels[i].fontSize = 20f;

                rowInfoButtons[i] = BigButton("Info", row.transform, "정보", null, Paper, Ink, out TMP_Text infoLabel);
                UiFactory.Place((RectTransform)rowInfoButtons[i].transform, UiFactory.MiddleRight, new Vector2(-14f - 112f - 8f, 0f), new Vector2(92f, buttonHeight));
                infoLabel.fontSize = 20f;

                rowOpens[i] = BigButton("Open", row.transform, "열기", null, Gold, Ink, out TMP_Text openLabel);
                UiFactory.Place((RectTransform)rowOpens[i].transform, UiFactory.MiddleRight, new Vector2(-14f - 112f - 8f - 92f - 8f, 0f), new Vector2(92f, buttonHeight));
                openLabel.fontSize = 20f;

                rowRoots[i] = row.gameObject;
            }

            // 아래: 새 맵, 쪽 넘기기, 돌아가기
            Button create = BigButton("CreateMap", panel.transform, "새 맵", null, Paper, Ink, out _);
            UiFactory.Place((RectTransform)create.transform, UiFactory.BottomLeft, new Vector2(PanelPadding, 34f), new Vector2(300f, 64f));

            Button previous = BigButton("PreviousPage", panel.transform, "이전", null, Paper, Ink, out TMP_Text previousLabel);
            UiFactory.Place((RectTransform)previous.transform, UiFactory.BottomCenter, new Vector2(-110f, 40f), new Vector2(92f, 52f));
            previousLabel.fontSize = 20f;

            TMP_Text pageLabel = UiFactory.Text("PageLabel", panel.transform, "1 / 1", 22f, Ink, true, TextAlignmentOptions.Center);
            UiFactory.Place(pageLabel.rectTransform, UiFactory.BottomCenter, new Vector2(0f, 50f), new Vector2(110f, 32f));

            Button next = BigButton("NextPage", panel.transform, "다음", null, Paper, Ink, out TMP_Text nextLabel);
            UiFactory.Place((RectTransform)next.transform, UiFactory.BottomCenter, new Vector2(110f, 40f), new Vector2(92f, 52f));
            nextLabel.fontSize = 20f;

            Button close = BigButton("CloseMapList", panel.transform, "돌아가기", "M", Gold, Ink, out _);
            UiFactory.Place((RectTransform)close.transform, UiFactory.BottomRight, new Vector2(-PanelPadding, 34f), new Vector2(300f, 64f));

            root.gameObject.SetActive(false);

            var serialized = new SerializedObject(view);
            serialized.FindProperty("root").objectReferenceValue = root.gameObject;
            serialized.FindProperty("pageLabel").objectReferenceValue = pageLabel;
            serialized.FindProperty("previousButton").objectReferenceValue = previous;
            serialized.FindProperty("nextButton").objectReferenceValue = next;
            serialized.FindProperty("createButton").objectReferenceValue = create;
            serialized.FindProperty("closeButton").objectReferenceValue = close;

            SerializedProperty rows = serialized.FindProperty("rows");
            rows.arraySize = rowCount;
            for (int i = 0; i < rowCount; i++)
            {
                SerializedProperty row = rows.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("root").objectReferenceValue = rowRoots[i];
                row.FindPropertyRelative("thumbnail").objectReferenceValue = rowThumbnails[i];
                row.FindPropertyRelative("nameLabel").objectReferenceValue = rowNames[i];
                row.FindPropertyRelative("infoLabel").objectReferenceValue = rowInfos[i];
                row.FindPropertyRelative("currentBadge").objectReferenceValue = rowBadges[i];
                row.FindPropertyRelative("openButton").objectReferenceValue = rowOpens[i];
                row.FindPropertyRelative("infoButton").objectReferenceValue = rowInfoButtons[i];
                row.FindPropertyRelative("deleteButton").objectReferenceValue = rowDeletes[i];
                row.FindPropertyRelative("deleteLabel").objectReferenceValue = rowDeleteLabels[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>
        /// 맵 정보 창(18일차). 왼쪽에는 대표 그림과 그림 찍기, 오른쪽에는 이름·설명의 글자 칸과 블록 수·날짜, 시작 위치와 그것을 정하는 단추가 있다.
        /// 아래에는 목록으로 돌아가기와 저장이 있다. 맵 목록 창의 줄에 있는 "정보"로 열며, 열려 있는 동안 맵 목록 창은 닫혀 있다.
        /// VR에는 글자판이 없어 저장 단추를 감추고 글자 칸은 MapInfoView가 잠근다.
        /// 뒤의 어두운 막은 VR에서 감추도록 따로 돌려준다.
        /// </summary>
        private static MapInfoView BuildMapInfo(RectTransform canvas, out GameObject scrimObject)
        {
            const float top = 108f;
            const float leftWidth = 400f;
            const float rightLeft = PanelPadding + leftWidth + 32f;
            const float rightWidth = PanelWidth - PanelPadding - rightLeft;
            const float fieldHeight = 46f;

            RectTransform holder = UiFactory.Rect("MapInfo", canvas);
            UiFactory.Fill(holder);
            var view = holder.gameObject.AddComponent<MapInfoView>();

            RectTransform root = UiFactory.Rect("Root", holder);
            UiFactory.Fill(root);

            RectTransform scrimRect = UiFactory.Rect("Scrim", root);
            UiFactory.Fill(scrimRect);
            var scrim = scrimRect.gameObject.AddComponent<Image>();
            scrim.color = AtelierPalette.Scrim;
            scrim.raycastTarget = true;
            scrimObject = scrimRect.gameObject;

            Image panel = UiFactory.Card("Panel", root, Paper, Ink, 28f, 8f);
            UiFactory.Place(panel.rectTransform, UiFactory.Center, Vector2.zero, new Vector2(PanelWidth, PanelHeight));

            TMP_Text title = UiFactory.Text("Title", panel.transform, "맵 정보", 30f, Ink, true);
            UiFactory.Place(title.rectTransform, UiFactory.TopLeft, new Vector2(PanelPadding, -24f), new Vector2(360f, 40f));

            TMP_Text guide = UiFactory.Text("Guide", panel.transform, "이름과 설명은 저장을 눌러야 바뀝니다. 그림 찍기와 시작 위치는 누르면 바로 바뀝니다.", 20f, Muted);
            UiFactory.Place(guide.rectTransform, UiFactory.TopLeft, new Vector2(PanelPadding, -66f), new Vector2(PanelWidth - PanelPadding * 2f, 28f));
            DesktopOnly.Add(guide.gameObject);

            // 왼쪽: 대표 그림과 그림 찍기
            RawImage thumbnail = Thumbnail("Thumbnail", panel.transform, new Vector2(leftWidth, leftWidth * 9f / 16f), 16f, out GameObject noThumbnail);
            UiFactory.Place((RectTransform)thumbnail.transform.parent, UiFactory.TopLeft, new Vector2(PanelPadding, -top), new Vector2(leftWidth, leftWidth * 9f / 16f));

            Button snapshot = BigButton("Snapshot", panel.transform, "지금 보는 장면을 대표 그림으로", null, Paper, Ink, out TMP_Text snapshotLabel);
            UiFactory.Place((RectTransform)snapshot.transform, UiFactory.TopLeft, new Vector2(PanelPadding, -top - leftWidth * 9f / 16f - 14f), new Vector2(leftWidth, 52f));
            snapshotLabel.fontSize = 22f;

            TMP_Text snapshotNote = UiFactory.Text("SnapshotNote", panel.transform, "화면의 단추와 글자는 찍히지 않습니다.", 18f, Muted);
            UiFactory.Place(snapshotNote.rectTransform, UiFactory.TopLeft, new Vector2(PanelPadding, -top - leftWidth * 9f / 16f - 76f), new Vector2(leftWidth, 26f));

            // 오른쪽: 이름, 설명, 블록 수와 날짜, 시작 위치
            TMP_Text nameTitle = UiFactory.Text("NameTitle", panel.transform, "이름", 20f, Ink, true);
            UiFactory.Place(nameTitle.rectTransform, UiFactory.TopLeft, new Vector2(rightLeft, -top + 2f), new Vector2(200f, 26f));

            TMP_InputField nameField = InputField("NameField", panel.transform, "맵의 이름");
            UiFactory.Place((RectTransform)nameField.transform, UiFactory.TopLeft, new Vector2(rightLeft, -top - 28f), new Vector2(rightWidth, fieldHeight));

            TMP_Text descriptionTitle = UiFactory.Text("DescriptionTitle", panel.transform, "설명", 20f, Ink, true);
            UiFactory.Place(descriptionTitle.rectTransform, UiFactory.TopLeft, new Vector2(rightLeft, -top - 88f), new Vector2(200f, 26f));

            TMP_InputField descriptionField = InputField("DescriptionField", panel.transform, "어떤 맵인지 한 줄로");
            UiFactory.Place((RectTransform)descriptionField.transform, UiFactory.TopLeft, new Vector2(rightLeft, -top - 118f), new Vector2(rightWidth, fieldHeight));

            TMP_Text facts = UiFactory.Text("Facts", panel.transform, "블록 0개", 18f, Muted);
            UiFactory.Place(facts.rectTransform, UiFactory.TopLeft, new Vector2(rightLeft, -top - 178f), new Vector2(rightWidth, 26f));
            facts.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform divider = UiFactory.Rect("Divider", panel.transform);
            UiFactory.Place(divider, UiFactory.TopLeft, new Vector2(rightLeft, -top - 216f), new Vector2(rightWidth, 2f));
            var dividerLine = divider.gameObject.AddComponent<Image>();
            dividerLine.color = Line;
            dividerLine.raycastTarget = false;

            TMP_Text spawnLabel = UiFactory.Text("SpawnLabel", panel.transform, MapInfoView.DefaultSpawnText, 20f, Ink, true);
            UiFactory.Place(spawnLabel.rectTransform, UiFactory.TopLeft, new Vector2(rightLeft, -top - 232f), new Vector2(rightWidth, 28f));
            spawnLabel.overflowMode = TextOverflowModes.Ellipsis;

            Button setSpawn = BigButton("SetSpawn", panel.transform, "지금 선 자리를 시작 위치로", null, Paper, Ink, out TMP_Text setSpawnLabel);
            UiFactory.Place((RectTransform)setSpawn.transform, UiFactory.TopLeft, new Vector2(rightLeft, -top - 270f), new Vector2(318f, 52f));
            setSpawnLabel.fontSize = 20f;

            Button clearSpawn = BigButton("ClearSpawn", panel.transform, "처음 자리로", null, Paper, Ink, out TMP_Text clearSpawnLabel);
            UiFactory.Place((RectTransform)clearSpawn.transform, UiFactory.TopLeft, new Vector2(rightLeft + 318f + 12f, -top - 270f), new Vector2(rightWidth - 318f - 12f, 52f));
            clearSpawnLabel.fontSize = 20f;

            TMP_Text note = UiFactory.Text("Note", panel.transform, string.Empty, 18f, AtelierPalette.Clay);
            UiFactory.Place(note.rectTransform, UiFactory.TopLeft, new Vector2(rightLeft, -top - 336f), new Vector2(rightWidth, 26f));

            // 아래: 목록으로, 저장
            Button back = BigButton("BackToList", panel.transform, "목록으로", "Esc", Paper, Ink, out _);
            UiFactory.Place((RectTransform)back.transform, UiFactory.BottomLeft, new Vector2(PanelPadding, 34f), new Vector2(300f, 64f));

            Button save = BigButton("SaveInfo", panel.transform, "저장", null, Gold, Ink, out _);
            UiFactory.Place((RectTransform)save.transform, UiFactory.BottomRight, new Vector2(-PanelPadding, 34f), new Vector2(300f, 64f));
            DesktopOnly.Add(save.gameObject);

            root.gameObject.SetActive(false);

            var serialized = new SerializedObject(view);
            serialized.FindProperty("root").objectReferenceValue = root.gameObject;
            serialized.FindProperty("thumbnail").objectReferenceValue = thumbnail;
            serialized.FindProperty("noThumbnailLabel").objectReferenceValue = noThumbnail;
            serialized.FindProperty("nameField").objectReferenceValue = nameField;
            serialized.FindProperty("descriptionField").objectReferenceValue = descriptionField;
            serialized.FindProperty("factsLabel").objectReferenceValue = facts;
            serialized.FindProperty("spawnLabel").objectReferenceValue = spawnLabel;
            serialized.FindProperty("noteLabel").objectReferenceValue = note;
            serialized.FindProperty("snapshotButton").objectReferenceValue = snapshot;
            serialized.FindProperty("setSpawnButton").objectReferenceValue = setSpawn;
            serialized.FindProperty("clearSpawnButton").objectReferenceValue = clearSpawn;
            serialized.FindProperty("saveButton").objectReferenceValue = save;
            serialized.FindProperty("backButton").objectReferenceValue = back;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>
        /// 맵의 대표 그림을 보이는 자리. 테두리가 있는 바탕 상자 안에 그림이 가득 차며, 그림이 없을 때는 바탕과 "그림 없음" 글자가 보인다.
        /// 돌려주는 것은 안쪽의 그림이고, 자리를 잡을 때는 그 부모(바탕 상자)를 놓는다. 그림은 꺼 둔 채로 만든다.
        /// </summary>
        private static RawImage Thumbnail(string name, Transform parent, Vector2 size, float corner, out GameObject emptyLabel)
        {
            Image back = UiFactory.Box(name + "Back", parent, Line, corner);
            back.rectTransform.sizeDelta = size;

            // 글자를 먼저 만들어 그림 아래에 깔리게 한다. 그림이 있으면 그림에 가려진다.
            TMP_Text label = UiFactory.Text("Empty", back.transform, "그림 없음", size.y > 100f ? 20f : 13f, Muted, false, TextAlignmentOptions.Center);
            UiFactory.Fill(label.rectTransform);
            emptyLabel = label.gameObject;

            RectTransform rect = UiFactory.Rect(name, back.transform);
            UiFactory.Fill(rect, 3f, 3f, 3f, 3f);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.enabled = false;

            UiFactory.Outline(back, AtelierPalette.WithAlpha(Ink, 0.4f));
            return image;
        }

        /// <summary>글자를 한 줄 써넣는 칸. 테두리가 있는 종이색 상자이며, 비어 있을 때는 흐린 안내 글자가 보인다.</summary>
        private static TMP_InputField InputField(string name, Transform parent, string placeholderText)
        {
            Image box = UiFactory.Box(name, parent, Paper, 12f);
            UiFactory.Outline(box, Ink);
            box.raycastTarget = true;

            RectTransform area = UiFactory.Rect("TextArea", box.transform);
            UiFactory.Fill(area, 14f, 6f, 14f, 6f);
            area.gameObject.AddComponent<RectMask2D>();

            TMP_Text placeholder = UiFactory.Text("Placeholder", area, placeholderText, 22f, Muted, false, TextAlignmentOptions.MidlineLeft);
            UiFactory.Fill(placeholder.rectTransform);

            TMP_Text text = UiFactory.Text("Text", area, string.Empty, 22f, Ink, false, TextAlignmentOptions.MidlineLeft);
            UiFactory.Fill(text.rectTransform);

            var field = box.gameObject.AddComponent<TMP_InputField>();
            field.targetGraphic = box;
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.fontAsset = UiFactory.Font;
            field.pointSize = 22f;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.caretColor = Ink;
            field.customCaretColor = true;
            field.selectionColor = AtelierPalette.WithAlpha(Gold, 0.45f);
            UiFactory.NoNavigation(field);
            return field;
        }

        /// <summary>부품 고르는 창의 줄 끝에 적는 크기와 한마디. 크기는 가로 × 높이 × 세로이며 블록 한 변이 1이다.</summary>
        private static string SizeText(PartShape shape)
        {
            Vector3 size = PartMeshes.SizeOf(shape);
            string text = $"{size.x:0.##} × {size.y:0.##} × {size.z:0.##}";
            switch (shape)
            {
                case PartShape.Block: return text + " · 길이의 표준";
                case PartShape.Slab: return text + " · 반 높이";
                case PartShape.Pillar: return text + " · 반 굵기";
                case PartShape.Wedge: return text + " · 45도로 오름";
                case PartShape.Stairs: return text + $" · {PartMeshes.StairSteps}단";
                default: return text;
            }
        }

        /// <summary>부품을 골랐을 때만 부품 칸 위에 보이는 안내. 놓고 지우고 칠하고 되돌리는 방법과 지금까지 놓인 블록 수를 보여 준다.</summary>
        private static GameObject BuildBuildHint(RectTransform hud, out TMP_Text countLabel, out TMP_Text rotateLabel, out TMP_Text grabLabel, out TMP_Text snapLabel)
        {
            // 돌리기·옮기기·맞추기의 글자는 실행 중에 지금 상태로 바뀐다(15일차). 자리는 가장 긴 글자(widest)에 맞춰 잡는다.
            (string key, string name, string label, string widest)[] hints =
            {
                ("왼쪽 누르기", "놓기", "놓기", null),
                ("오른쪽 누르기", "지우기", "지우기", null),
                ("가운데·F", "칠하기", "칠하기", null),
                ("R·T", "돌리기", "돌리기 0°", "돌리기 345°"),
                ("G", "옮기기", "옮기기", "옮기는 중"),
                ("C", "맞추기", "맞추기 끔", "맞추기 1/4칸"),
                ("Ctrl+Z", "되돌리기", "되돌리기", null),
                ("Ctrl+Y", "다시 실행", "다시 실행", null),
            };

            Image glass = UiFactory.Box("BuildHint", hud, DarkGlass, 22f);
            float x = 14f;
            rotateLabel = null;
            grabLabel = null;
            snapLabel = null;

            foreach ((string key, string name, string label, string widest) in hints)
            {
                RectTransform badge = UiFactory.Badge($"Key_{name}", glass.transform, key, AtelierPalette.WithAlpha(Paper, 0.94f), Ink);
                UiFactory.Place(badge, UiFactory.MiddleLeft, new Vector2(x, 0f), badge.sizeDelta);
                x += badge.sizeDelta.x + 8f;

                TMP_Text text = UiFactory.Text($"Label_{name}", glass.transform, widest ?? label, 18f, Paper);
                float width = UiFactory.WidthOf(text);
                text.text = label;
                UiFactory.Place(text.rectTransform, UiFactory.MiddleLeft, new Vector2(x, 0f), new Vector2(width + 2f, 28f));
                x += width + 18f;

                if (name == "돌리기") rotateLabel = text;
                else if (name == "옮기기") grabLabel = text;
                else if (name == "맞추기") snapLabel = text;
            }

            const float countWidth = 150f;
            countLabel = UiFactory.Text("BlockCount", glass.transform, "블록 0/500", 18f, Gold, true, TextAlignmentOptions.Right);
            UiFactory.Place(countLabel.rectTransform, UiFactory.MiddleLeft, new Vector2(x, 0f), new Vector2(countWidth, 28f));
            x += countWidth + 18f;

            UiFactory.Place(glass.rectTransform, UiFactory.BottomCenter, new Vector2(0f, Margin + SlotSize + 26f + 40f + 10f), new Vector2(x, 44f));
            glass.gameObject.SetActive(false);
            return glass.gameObject;
        }

        private static void BuildKeyHints(RectTransform hud)
        {
            (string key, string label)[] hints =
            {
                // 사람들 목록의 Tab은 그 목록의 머리말에 딱지로 적혀 있어 여기서는 뺀다. 줄이 부품 칸에 닿지 않게 하기 위해서다.
                ("Esc", "메뉴"),
                ("휠", "시점"),
                ("V", "날기"),
                ("1~9", "부품"),
                ("B", "부품 창"),
            };

            Image glass = UiFactory.Box("KeyHints", hud, DarkGlass, 22f);
            float x = 14f;

            foreach ((string key, string label) in hints)
            {
                RectTransform badge = UiFactory.Badge($"Key_{key}", glass.transform, key, AtelierPalette.WithAlpha(Paper, 0.94f), Ink);
                UiFactory.Place(badge, UiFactory.MiddleLeft, new Vector2(x, 0f), badge.sizeDelta);
                x += badge.sizeDelta.x + 8f;

                TMP_Text text = UiFactory.Text($"Label_{key}", glass.transform, label, 18f, Paper);
                float width = UiFactory.WidthOf(text);
                UiFactory.Place(text.rectTransform, UiFactory.MiddleLeft, new Vector2(x, 0f), new Vector2(width + 2f, 28f));
                x += width + 18f;
            }

            UiFactory.Place(glass.rectTransform, UiFactory.BottomLeft, new Vector2(Margin, Margin), new Vector2(x - 2f, 44f));
        }

        private static TMP_Text BuildViewChip(RectTransform hud)
        {
            Image glass = UiFactory.Box("ViewChip", hud, DarkGlass, 22f);
            UiFactory.Place(glass.rectTransform, UiFactory.BottomRight, new Vector2(-Margin, Margin), new Vector2(110f, 44f));

            TMP_Text label = UiFactory.Text("Label", glass.transform, "1인칭", 18f, Paper, true, TextAlignmentOptions.Center);
            UiFactory.Fill(label.rectTransform);
            return label;
        }

        /// <summary>시점 표시 왼쪽의 저장 표시. 자동 저장의 안내 문구("저장했습니다" 등)가 그대로 들어간다(4일차).</summary>
        private static TMP_Text BuildSaveChip(RectTransform hud)
        {
            const float viewChipWidth = 110f;
            const float width = 280f;

            Image glass = UiFactory.Box("SaveChip", hud, DarkGlass, 22f);
            UiFactory.Place(glass.rectTransform, UiFactory.BottomRight, new Vector2(-Margin - viewChipWidth - 10f, Margin), new Vector2(width, 44f));

            Image dot = UiFactory.Box("Dot", glass.transform, Gold, 6f);
            UiFactory.Place(dot.rectTransform, UiFactory.MiddleLeft, new Vector2(16f, 0f), new Vector2(12f, 12f));

            TMP_Text label = UiFactory.Text("Label", glass.transform, "저장 준비 중", 18f, Paper, true, TextAlignmentOptions.Center);
            UiFactory.Fill(label.rectTransform, 34f, 0f, 12f, 0f);
            return label;
        }

        /// <summary>저장 표시 왼쪽의 걷기·날기 표시(7일차). 점은 걷기일 때 잎색, 날 때 골드다.</summary>
        private static TMP_Text BuildModeChip(RectTransform hud, out Image dot)
        {
            const float viewChipWidth = 110f;
            const float saveChipWidth = 280f;
            const float width = 110f;

            Image glass = UiFactory.Box("ModeChip", hud, DarkGlass, 22f);
            UiFactory.Place(glass.rectTransform, UiFactory.BottomRight, new Vector2(-Margin - viewChipWidth - 10f - saveChipWidth - 10f, Margin), new Vector2(width, 44f));

            dot = UiFactory.Box("Dot", glass.transform, AtelierPalette.Leaf, 6f);
            UiFactory.Place(dot.rectTransform, UiFactory.MiddleLeft, new Vector2(16f, 0f), new Vector2(12f, 12f));

            TMP_Text label = UiFactory.Text("Label", glass.transform, "걷기", 18f, Paper, true, TextAlignmentOptions.Center);
            UiFactory.Fill(label.rectTransform, 30f, 0f, 12f, 0f);
            return label;
        }

        /// <summary>
        /// 화면 위 가운데의 알림 띠(6일차). 평소에는 숨겨져 있고 Notice.Post가 오면 잠시 보인다. 너비는 실행 중에 글자에 맞춘다.
        /// VR에서도 보이도록 캔버스 바로 아래에 둔다(12일차).
        /// </summary>
        private static NoticeBar BuildNoticeBar(RectTransform canvas)
        {
            RectTransform holder = UiFactory.Rect("Notice", canvas);
            UiFactory.Place(holder, UiFactory.TopLeft, Vector2.zero, Vector2.zero);
            holder.anchorMin = new Vector2(0f, 1f);
            holder.anchorMax = new Vector2(1f, 1f);
            holder.pivot = new Vector2(0.5f, 1f);
            holder.anchoredPosition = Vector2.zero;
            holder.sizeDelta = new Vector2(0f, 0f);

            Image glass = UiFactory.Box("Bar", holder, DarkGlass, 24f);
            UiFactory.Place(glass.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -Margin - 6f), new Vector2(480f, 48f));

            Image dot = UiFactory.Box("Dot", glass.transform, AtelierPalette.Leaf, 6f);
            UiFactory.Place(dot.rectTransform, UiFactory.MiddleLeft, new Vector2(16f, 0f), new Vector2(12f, 12f));

            TMP_Text label = UiFactory.Text("Label", glass.transform, string.Empty, 20f, Paper, true, TextAlignmentOptions.Center);
            UiFactory.Fill(label.rectTransform, 34f, 0f, 34f, 0f);

            glass.gameObject.SetActive(false);

            var bar = holder.gameObject.AddComponent<NoticeBar>();
            var serialized = new SerializedObject(bar);
            serialized.FindProperty("bar").objectReferenceValue = glass.gameObject;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.FindProperty("dot").objectReferenceValue = dot;
            serialized.FindProperty("infoColor").colorValue = AtelierPalette.Leaf;
            serialized.FindProperty("warningColor").colorValue = Gold;
            serialized.FindProperty("errorColor").colorValue = AtelierPalette.Clay;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return bar;
        }

        private static GameObject BuildCrosshair(RectTransform hud)
        {
            Image outer = UiFactory.Box("Crosshair", hud, AtelierPalette.WithAlpha(Ink, 0.85f), 7f);
            UiFactory.Place(outer.rectTransform, UiFactory.Center, Vector2.zero, new Vector2(14f, 14f));

            Image dot = UiFactory.Box("Dot", outer.transform, Paper, 4f);
            UiFactory.Place(dot.rectTransform, UiFactory.Center, Vector2.zero, new Vector2(8f, 8f));

            outer.gameObject.SetActive(false);
            return outer.gameObject;
        }

        private static GameObject BuildFocusHint(RectTransform hud)
        {
            Image glass = UiFactory.Box("FocusHint", hud, DarkGlass, 26f);
            TMP_Text label = UiFactory.Text("Label", glass.transform, "화면을 누르면 둘러볼 수 있습니다", 24f, Paper, false, TextAlignmentOptions.Center);
            UiFactory.Fill(label.rectTransform);
            UiFactory.Place(glass.rectTransform, UiFactory.Center, new Vector2(0f, -190f), new Vector2(UiFactory.WidthOf(label) + 64f, 52f));
            return glass.gameObject;
        }

        // ── Esc 메뉴 ────────────────────────────────────────────────

        private static QuickMenuView BuildQuickMenu(RectTransform canvas, Icons icons, out TMP_Text roomLabel, out TMP_Text nameLabel, out TMP_Text brandLabel, out PeopleListView people, out GameObject scrimObject)
        {
            RectTransform holder = UiFactory.Rect("QuickMenu", canvas);
            UiFactory.Fill(holder);
            var view = holder.gameObject.AddComponent<QuickMenuView>();

            RectTransform root = UiFactory.Rect("Root", holder);
            UiFactory.Fill(root);

            RectTransform scrimRect = UiFactory.Rect("Scrim", root);
            UiFactory.Fill(scrimRect);
            var scrim = scrimRect.gameObject.AddComponent<Image>();
            scrim.color = AtelierPalette.Scrim;
            scrim.raycastTarget = true;
            scrimObject = scrimRect.gameObject;

            Image panel = UiFactory.Card("Panel", root, Paper, Ink, 28f, 8f);
            UiFactory.Place(panel.rectTransform, UiFactory.Center, Vector2.zero, new Vector2(PanelWidth, PanelHeight));

            // 머리말: 누구인지와 어느 방인지
            Image avatar = UiFactory.Box("Avatar", panel.transform, AtelierPalette.Blue, 14f);
            UiFactory.Place(avatar.rectTransform, UiFactory.TopLeft, new Vector2(PanelPadding, -26f), new Vector2(56f, 56f));
            UiFactory.Outline(avatar, Ink);

            nameLabel = UiFactory.Text("Name", panel.transform, "손님", 30f, Ink, true);
            UiFactory.Place(nameLabel.rectTransform, UiFactory.TopLeft, new Vector2(PanelPadding + 72f, -22f), new Vector2(480f, 38f));

            roomLabel = UiFactory.Text("Room", panel.transform, "시험 작업실 · 1/8", 20f, Muted);
            UiFactory.Place(roomLabel.rectTransform, UiFactory.TopLeft, new Vector2(PanelPadding + 72f, -58f), new Vector2(480f, 28f));

            // 서비스 이름과 버전(실행 중에 GameUi가 채움)
            brandLabel = UiFactory.Text("Brand", panel.transform, "Atelier | Verse", 22f, Muted, true, TextAlignmentOptions.Right);
            UiFactory.Place(brandLabel.rectTransform, UiFactory.TopRight, new Vector2(-PanelPadding, -38f), new Vector2(360f, 32f));

            // 탭: 고른 탭 아래에 형광펜 선이 깔린다
            string[] tabNames = { "바로가기", "사람들", "설정", "도움말" };
            const float tabTop = -104f;
            const float tabWidth = 150f;
            const float tabHeight = 52f;

            var tabButtons = new Button[tabNames.Length];
            var tabMarkers = new GameObject[tabNames.Length];
            for (int i = 0; i < tabNames.Length; i++)
            {
                RectTransform tab = UiFactory.Rect($"Tab{i}", panel.transform);
                UiFactory.Place(tab, UiFactory.TopLeft, new Vector2(PanelPadding + i * (tabWidth + 8f), tabTop), new Vector2(tabWidth, tabHeight));

                var hit = tab.gameObject.AddComponent<Image>();
                hit.color = Color.clear;

                Image marker = UiFactory.Box("Marker", tab, AtelierPalette.WithAlpha(Gold, 0.62f), 6f);
                UiFactory.Place(marker.rectTransform, UiFactory.BottomCenter, new Vector2(0f, 8f), new Vector2(tabWidth - 30f, 16f));

                TMP_Text label = UiFactory.Text("Label", tab, tabNames[i], 24f, Ink, true, TextAlignmentOptions.Center);
                UiFactory.Fill(label.rectTransform);

                tabButtons[i] = UiFactory.Clickable(hit);
                tabMarkers[i] = marker.gameObject;
            }

            UiFactory.Divider(panel.transform, tabTop - tabHeight - 6f, PanelPadding, Line);

            RectTransform pages = UiFactory.Rect("Pages", panel.transform);
            UiFactory.Fill(pages, PanelPadding, 124f, PanelPadding, 180f);

            GameObject shortcutPage = BuildShortcutPage(pages, icons, out Button respawnTile, out Button viewTile, out TMP_Text viewTileTitle, out CanvasGroup viewTileGroup, out Button mapsTile);
            GameObject peoplePage = BuildPeoplePage(pages, out people);
            GameObject settingsPage = BuildSettingsPage(pages);
            GameObject helpPage = BuildHelpPage(pages);

            // 아래 단추: 로블록스 메뉴처럼 가장 자주 쓰는 세 가지를 크게 둔다
            Button quit = BigButton("Quit", panel.transform, "게임 끝내기", null, Paper, AtelierPalette.Clay, out TMP_Text quitLabel);
            UiFactory.Place((RectTransform)quit.transform, UiFactory.BottomLeft, new Vector2(PanelPadding, 34f), new Vector2(300f, 64f));

            Button respawn = BigButton("Respawn", panel.transform, "시작 위치로", "R", Paper, Ink, out _);
            UiFactory.Place((RectTransform)respawn.transform, UiFactory.BottomCenter, new Vector2(0f, 34f), new Vector2(300f, 64f));

            Button resume = BigButton("Resume", panel.transform, "돌아가기", "Esc", Gold, Ink, out _);
            UiFactory.Place((RectTransform)resume.transform, UiFactory.BottomRight, new Vector2(-PanelPadding, 34f), new Vector2(300f, 64f));

            root.gameObject.SetActive(false);

            var serialized = new SerializedObject(view);
            serialized.FindProperty("root").objectReferenceValue = root.gameObject;
            SetObjects(serialized.FindProperty("tabButtons"), tabButtons);
            SetObjects(serialized.FindProperty("tabPages"), shortcutPage, peoplePage, settingsPage, helpPage);
            SetObjects(serialized.FindProperty("tabMarkers"), tabMarkers);
            serialized.FindProperty("resumeButton").objectReferenceValue = resume;
            serialized.FindProperty("respawnButton").objectReferenceValue = respawn;
            serialized.FindProperty("quitButton").objectReferenceValue = quit;
            serialized.FindProperty("quitLabel").objectReferenceValue = quitLabel;
            serialized.FindProperty("respawnTile").objectReferenceValue = respawnTile;
            serialized.FindProperty("viewTile").objectReferenceValue = viewTile;
            serialized.FindProperty("viewTileTitle").objectReferenceValue = viewTileTitle;
            serialized.FindProperty("viewTileGroup").objectReferenceValue = viewTileGroup;
            serialized.FindProperty("mapsTile").objectReferenceValue = mapsTile;
            SetObjects(serialized.FindProperty("desktopOnly"), DesktopOnly.ToArray());
            SetObjects(serialized.FindProperty("vrOnly"), VrOnly.ToArray());
            SetObjects(serialized.FindProperty("desktopOnlyGroups"), DesktopOnlyGroups.ToArray());
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>VRChat의 메뉴처럼 큰 타일로 자주 가는 곳을 둔다. 아직 만들지 않은 곳은 누를 수 없게 하고 "준비 중"으로 표시한다.</summary>
        private static GameObject BuildShortcutPage(RectTransform pages, Icons icons, out Button respawnTile, out Button viewTile, out TMP_Text viewTileTitle, out CanvasGroup viewTileGroup, out Button mapsTile)
        {
            RectTransform page = UiFactory.Rect("ShortcutPage", pages);
            UiFactory.Fill(page);

            respawnTile = BuildTile(page, 0, "RespawnTile", "시작 위치로", "처음 선 자리로 돌아갑니다", icons.Respawn, Gold, Ink, "R", true, out _);
            viewTile = BuildTile(page, 1, "ViewTile", "3인칭으로 보기", "휠을 굴려도 바뀝니다", icons.View, AtelierPalette.Blue, Paper, null, true, out viewTileTitle);

            // VR은 늘 1인칭이라 이 타일을 누를 수 없다. QuickMenuView가 VR에서 흐리게 하고 "PC 전용" 딱지를 보인다.
            viewTileGroup = viewTile.gameObject.AddComponent<CanvasGroup>();
            RectTransform pcOnly = UiFactory.Badge("PcOnly", viewTile.transform, "PC 전용", Paper, Muted, 30f);
            UiFactory.Place(pcOnly, UiFactory.TopRight, new Vector2(-18f, -20f), pcOnly.sizeDelta);
            VrOnly.Add(pcOnly.gameObject);
            // 내 작업실: 이 기기에 저장된 맵의 목록을 연다(17일차). 계정에 저장한 맵은 계정을 붙일 때 함께 보인다.
            mapsTile = BuildTile(page, 2, "HomeTile", "내 작업실", "이 기기에 만든 맵을 엽니다", icons.Home, AtelierPalette.Clay, Paper, "M", true, out _);
            BuildTile(page, 3, "MapTile", "맵 둘러보기", "다른 사람의 맵을 찾습니다", icons.Map, AtelierPalette.Leaf, Paper, null, false, out _);
            BuildTile(page, 4, "AvatarTile", "캐릭터", "모습을 고릅니다", icons.Avatar, AtelierPalette.Blue, Paper, null, false, out _);
            BuildTile(page, 5, "SafetyTile", "안전·신고", "차단하고 신고합니다", icons.Shield, AtelierPalette.Clay, Paper, null, false, out _);
            return page.gameObject;
        }

        private static Button BuildTile(RectTransform page, int index, string name, string title, string subtitle, Sprite icon, Color blockColor, Color iconColor, string key, bool ready, out TMP_Text titleText)
        {
            const float width = 312f;
            const float height = 170f;
            const float gap = 16f;

            int column = index % 3;
            int row = index / 3;

            Image tile = UiFactory.Card(name, page, Surface, ready ? Ink : Line, 20f, ready ? 5f : 0f);
            UiFactory.Place(tile.rectTransform, UiFactory.TopLeft, new Vector2(column * (width + gap), -row * (height + gap)), new Vector2(width, height));

            Image block = UiFactory.Box("Block", tile.transform, blockColor, 14f);
            UiFactory.Place(block.rectTransform, UiFactory.TopLeft, new Vector2(22f, -20f), new Vector2(64f, 64f));
            UiFactory.Icon("Icon", block.transform, icon, iconColor, 40f);

            titleText = UiFactory.Text("Title", tile.transform, title, 26f, Ink, true);
            UiFactory.Place(titleText.rectTransform, UiFactory.TopLeft, new Vector2(22f, -96f), new Vector2(270f, 34f));

            TMP_Text sub = UiFactory.Text("Subtitle", tile.transform, subtitle, 18f, Muted);
            UiFactory.Place(sub.rectTransform, UiFactory.TopLeft, new Vector2(22f, -130f), new Vector2(270f, 26f));

            if (!ready)
            {
                RectTransform soon = UiFactory.Badge("Soon", tile.transform, "준비 중", Paper, Muted, 30f);
                UiFactory.Place(soon, UiFactory.TopRight, new Vector2(-18f, -20f), soon.sizeDelta);
                tile.gameObject.AddComponent<CanvasGroup>().alpha = 0.62f;
                return null;
            }

            if (key != null)
            {
                RectTransform badge = UiFactory.Badge("Key", tile.transform, key, Paper, Ink, 30f);
                UiFactory.Place(badge, UiFactory.TopRight, new Vector2(-18f, -20f), badge.sizeDelta);
                DesktopOnly.Add(badge.gameObject);
            }

            return UiFactory.Clickable(tile);
        }

        private static GameObject BuildPeoplePage(RectTransform pages, out PeopleListView view)
        {
            RectTransform page = UiFactory.Rect("PeoplePage", pages);
            UiFactory.Fill(page);

            TMP_Text header = UiFactory.Text("Header", page, "사람들 1/8", 26f, Ink, true);
            UiFactory.Place(header.rectTransform, UiFactory.TopLeft, new Vector2(0f, -2f), new Vector2(400f, 36f));

            Image row = UiFactory.Box("Row", page, Surface, 16f);
            UiFactory.StretchTop(row.rectTransform, -54f, 72f);
            UiFactory.Outline(row, Line);
            TMP_Text localName = BuildPersonRow(row.transform, 16f, -14f, 44f);

            TMP_Text note = UiFactory.Text("Note", page, "여러 사람이 같은 방에 들어오는 기능은 다음 단계에서 연결됩니다.", 20f, Muted);
            UiFactory.Place(note.rectTransform, UiFactory.TopLeft, new Vector2(0f, -146f), new Vector2(960f, 30f));

            view = page.gameObject.AddComponent<PeopleListView>();
            SetPeopleList(view, header, localName);
            return page.gameObject;
        }

        /// <summary>
        /// 설정 쪽. 위에 갈래(화면·조작·소리)를 고르는 칸과 기본값 단추가 있고, 그 아래에 고른 갈래의 줄이 보인다(20일차).
        /// 키보드·마우스에만 해당하는 줄은 VR에서 흐리게 하고 "PC 전용" 딱지를 보인다.
        /// </summary>
        private static GameObject BuildSettingsPage(RectTransform pages)
        {
            const float barHeight = 48f;

            RectTransform page = UiFactory.Rect("SettingsPage", pages);
            UiFactory.Fill(page);

            ChoiceBar sectionBar = BuildChoiceBar("Sections", page, "Section", new[] { "화면", "조작", "소리" }, 132f, barHeight);
            var sectionRect = (RectTransform)sectionBar.transform;
            UiFactory.Place(sectionRect, UiFactory.TopLeft, new Vector2(0f, -2f), sectionRect.sizeDelta);

            Button reset = BigButton("Reset", page, "모두 기본값으로", null, Paper, Ink, out _);
            UiFactory.Place((RectTransform)reset.transform, UiFactory.TopRight, new Vector2(-6f, -2f), new Vector2(236f, barHeight));

            // 화면
            RectTransform screen = BuildSection(page, "ScreenSection", barHeight);

            RectTransform displayRow = BuildRow(screen, 0, "화면 방식", true);
            ChoiceBar display = BuildChoiceBar("DisplayBar", displayRow, "Display", new[] { "창", "전체 화면" }, ChoiceWidth, ChoiceHeight);
            PlaceRight(display);

            RectTransform qualityRow = BuildRow(screen, 1, "화면 품질", false);
            ChoiceBar quality = BuildChoiceBar("QualityBar", qualityRow, "Quality", GraphicsQuality.Labels, ChoiceWidth, ChoiceHeight);
            PlaceRight(quality);

            Toggle vSync = BuildToggleRow(screen, 2, "수직 동기화", "VSyncToggle", "모니터에 맞춰 그려 화면이 찢어져 보이지 않습니다", false, true);
            Toggle people = BuildToggleRow(screen, 3, "사람들 목록 보이기", "PeopleToggle", "Tab 키로도 켜고 끕니다", true, false);

            // 조작
            RectTransform controls = BuildSection(page, "ControlSection", barHeight);
            Slider look = BuildSliderRow(controls, 0, "마우스 감도", "LookSlider", true, out TMP_Text lookValue);
            Toggle invertLook = BuildToggleRow(controls, 1, "위아래 시점 반대로", "InvertLookToggle", "마우스를 위로 밀면 아래를 봅니다", false, true);
            Slider fieldOfView = BuildSliderRow(controls, 2, "시야각", "FieldOfViewSlider", true, out TMP_Text fieldOfViewValue);

            // 소리
            RectTransform sound = BuildSection(page, "SoundSection", barHeight);
            Slider soundSlider = BuildSliderRow(sound, 0, "소리 크기", "SoundSlider", false, out TMP_Text soundValue);

            controls.gameObject.SetActive(false);
            sound.gameObject.SetActive(false);

            var panel = page.gameObject.AddComponent<SettingsPanel>();
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("sectionBar").objectReferenceValue = sectionBar;
            SetObjects(serialized.FindProperty("sections"), screen.gameObject, controls.gameObject, sound.gameObject);
            serialized.FindProperty("displayBar").objectReferenceValue = display;
            serialized.FindProperty("qualityBar").objectReferenceValue = quality;
            serialized.FindProperty("vSyncToggle").objectReferenceValue = vSync;
            serialized.FindProperty("peopleListToggle").objectReferenceValue = people;
            serialized.FindProperty("lookSlider").objectReferenceValue = look;
            serialized.FindProperty("lookValue").objectReferenceValue = lookValue;
            serialized.FindProperty("invertLookToggle").objectReferenceValue = invertLook;
            serialized.FindProperty("fieldOfViewSlider").objectReferenceValue = fieldOfView;
            serialized.FindProperty("fieldOfViewValue").objectReferenceValue = fieldOfViewValue;
            serialized.FindProperty("soundSlider").objectReferenceValue = soundSlider;
            serialized.FindProperty("soundValue").objectReferenceValue = soundValue;
            serialized.FindProperty("resetButton").objectReferenceValue = reset;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return page.gameObject;
        }

        private const float ChoiceWidth = 150f;
        private const float ChoiceHeight = 46f;
        private const float ChoiceGap = 8f;
        private const float RowPitch = 74f;
        private const float RowHeight = 62f;

        /// <summary>설정의 갈래 하나가 들어가는 자리. 위쪽의 갈래 칸 아래를 가득 채운다.</summary>
        private static RectTransform BuildSection(RectTransform page, string name, float barHeight)
        {
            RectTransform section = UiFactory.Rect(name, page);
            UiFactory.Fill(section, 0f, 0f, 0f, barHeight + 16f);
            return section;
        }

        /// <summary>
        /// 설정 한 줄의 바탕과 왼쪽 이름. desktopOnly이면 키보드·마우스에만 해당하는 줄이다:
        /// VR에서 메뉴가 이 줄을 흐리게 하고 누를 수 없게 하며, 이름 옆에 "PC 전용" 딱지가 보인다.
        /// </summary>
        private static RectTransform BuildRow(RectTransform section, int index, string title, bool desktopOnly)
        {
            RectTransform row = UiFactory.Rect($"Row{index}", section);
            UiFactory.StretchTop(row, -index * RowPitch, RowHeight);

            TMP_Text label = UiFactory.Text("Label", row, title, 24f, Ink, true);
            UiFactory.Place(label.rectTransform, UiFactory.MiddleLeft, Vector2.zero, new Vector2(320f, 36f));

            if (desktopOnly)
            {
                DesktopOnlyGroups.Add(row.gameObject.AddComponent<CanvasGroup>());

                RectTransform pcOnly = UiFactory.Badge("PcOnly", row, "PC 전용", Paper, Muted, 30f);
                UiFactory.Place(pcOnly, UiFactory.MiddleLeft, new Vector2(UiFactory.WidthOf(label) + 14f, 0f), pcOnly.sizeDelta);
                VrOnly.Add(pcOnly.gameObject);

                // 딱지는 줄과 함께 흐려지지 않게 한다. 왜 누를 수 없는지 알리는 글자이기 때문이다.
                var badgeGroup = pcOnly.gameObject.AddComponent<CanvasGroup>();
                badgeGroup.ignoreParentGroups = true;
                badgeGroup.blocksRaycasts = false;
            }

            return row;
        }

        private static Slider BuildSliderRow(RectTransform section, int index, string title, string sliderName, bool desktopOnly, out TMP_Text value)
        {
            RectTransform row = BuildRow(section, index, title, desktopOnly);

            GameObject sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderObject.name = sliderName;
            sliderObject.transform.SetParent(row, false);
            UiFactory.SetLayer(sliderObject, UiFactory.UiLayer);
            UiFactory.Place((RectTransform)sliderObject.transform, UiFactory.MiddleRight, new Vector2(-110f, 0f), new Vector2(460f, 28f));

            UiFactory.Style(sliderObject.transform.Find("Background").GetComponent<Image>(), UiFactory.Rounded, Line, 7f);
            UiFactory.Style(sliderObject.transform.Find("Fill Area/Fill").GetComponent<Image>(), UiFactory.Rounded, Gold, 7f);

            Image handle = sliderObject.transform.Find("Handle Slide Area/Handle").GetComponent<Image>();
            UiFactory.Style(handle, UiFactory.Rounded, Ink, 10f);
            handle.raycastTarget = true;

            var slider = sliderObject.GetComponent<Slider>();
            UiFactory.NoNavigation(slider);

            value = UiFactory.Text("Value", row, "100%", 22f, Muted, true, TextAlignmentOptions.Right);
            UiFactory.Place(value.rectTransform, UiFactory.MiddleRight, Vector2.zero, new Vector2(96f, 36f));
            return slider;
        }

        /// <summary>켜고 끄는 칸이 오른쪽 끝에 있는 줄. hint는 칸 왼쪽의 작은 설명이며, hintDesktopOnly이면 키보드·마우스에서만 보인다.</summary>
        private static Toggle BuildToggleRow(RectTransform section, int index, string title, string toggleName, string hint, bool hintDesktopOnly, bool desktopOnly)
        {
            RectTransform row = BuildRow(section, index, title, desktopOnly);

            Image box = UiFactory.Box(toggleName, row, Paper, 10f);
            UiFactory.Place(box.rectTransform, UiFactory.MiddleRight, Vector2.zero, new Vector2(40f, 40f));
            box.raycastTarget = true;
            UiFactory.Outline(box, Ink);
            Image check = UiFactory.Box("Check", box.transform, Gold, 6f);
            UiFactory.Place(check.rectTransform, UiFactory.Center, Vector2.zero, new Vector2(24f, 24f));

            var toggle = box.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            UiFactory.NoNavigation(toggle);

            if (!string.IsNullOrEmpty(hint))
            {
                TMP_Text hintText = UiFactory.Text("Hint", row, hint, 18f, Muted, false, TextAlignmentOptions.Right);
                UiFactory.Place(hintText.rectTransform, UiFactory.MiddleRight, new Vector2(-60f, 0f), new Vector2(460f, 30f));
                if (hintDesktopOnly) DesktopOnly.Add(hintText.gameObject);
            }

            return toggle;
        }

        /// <summary>나란히 놓인 칸 가운데 하나를 고르는 줄. 칸의 이름은 prefix에 번호를 붙인 것이다. 크기는 칸의 수에 맞춰지며 자리는 부르는 쪽이 정한다.</summary>
        private static ChoiceBar BuildChoiceBar(string name, Transform parent, string prefix, string[] options, float width, float height)
        {
            RectTransform bar = UiFactory.Rect(name, parent);
            bar.sizeDelta = new Vector2(options.Length * width + (options.Length - 1) * ChoiceGap, height);

            var buttons = new Button[options.Length];
            var fills = new Image[options.Length];
            var labels = new TMP_Text[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                Image box = UiFactory.Box($"{prefix}{i}", bar, Paper, 12f);
                UiFactory.Place(box.rectTransform, UiFactory.MiddleLeft, new Vector2(i * (width + ChoiceGap), 0f), new Vector2(width, height));
                UiFactory.Outline(box, Ink);

                labels[i] = UiFactory.Text("Label", box.transform, options[i], 22f, Ink, true, TextAlignmentOptions.Center);
                UiFactory.Fill(labels[i].rectTransform);

                fills[i] = box;
                buttons[i] = UiFactory.Clickable(box);
            }

            var choice = bar.gameObject.AddComponent<ChoiceBar>();
            var serialized = new SerializedObject(choice);
            SetObjects(serialized.FindProperty("buttons"), buttons);
            SetObjects(serialized.FindProperty("fills"), fills);
            SetObjects(serialized.FindProperty("labels"), labels);
            serialized.FindProperty("selectedFill").colorValue = Gold;
            serialized.FindProperty("normalFill").colorValue = Paper;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return choice;
        }

        /// <summary>고르는 줄을 설정 한 줄의 오른쪽 끝에 붙인다.</summary>
        private static void PlaceRight(ChoiceBar bar)
        {
            var rect = (RectTransform)bar.transform;
            UiFactory.Place(rect, UiFactory.MiddleRight, Vector2.zero, rect.sizeDelta);
        }

        /// <summary>로블록스 메뉴의 도움말처럼 조작 키를 한눈에 보여 준다.</summary>
        private static GameObject BuildHelpPage(RectTransform pages)
        {
            RectTransform page = UiFactory.Rect("HelpPage", pages);
            UiFactory.Fill(page);

            (string key, string text)[] rows =
            {
                ("W A S D", "걷기"),
                ("마우스", "둘러보기"),
                ("Space · Shift", "점프 · 달리기 (날 때 위 · 아래)"),
                ("V", "날기 켜고 끄기"),
                ("휠", "1인칭·3인칭 바꾸기"),
                ("1~9 · B", "부품 고르기 · 부품 창"),
                ("왼쪽 · 오른쪽", "블록 놓기 · 지우기 (누르기)"),
                ("가운데 · F", "블록 칠하기"),
                ("R · T", "놓을 블록 돌리기 (15도씩)"),
                ("G", "블록 잡아서 옮기기"),
                ("C", "맞추기 도우미 단계 바꾸기"),
                ("Ctrl+Z · Y", "되돌리기 · 다시 실행"),
                ("M", "내 작업실 (맵 목록)"),
                ("Tab · Esc", "사람들 목록 · 메뉴"),
            };

            // VR 컨트롤러의 조작. 컨트롤러마다 단추 이름이 달라 "첫째·둘째 단추"로 적는다(Quest의 오른손은 A·B).
            (string key, string text)[] vrRows =
            {
                ("왼쪽 스틱", "걷기"),
                ("왼쪽 스틱 누르기", "달리기"),
                ("오른쪽 스틱 좌우", "45도씩 돌기"),
                ("오른쪽 스틱 위아래", "날 때 위 · 아래"),
                ("오른손 첫째 단추", "점프"),
                ("오른손 둘째 단추", "날기 켜고 끄기"),
                ("왼손 메뉴 단추", "메뉴 (내 작업실도 여기)"),
                ("왼손 위의 부품 판", "부품·되돌리기·맞추기"),
                ("오른손 방아쇠", "누르기 · 블록 놓기"),
                ("오른손 옆 단추", "블록 지우기"),
                ("왼손 방아쇠", "블록 칠하기"),
                ("왼손 첫째 단추", "블록 돌리기"),
                ("오른쪽 스틱 누르기", "반대로 돌리기"),
                ("왼손 옆 단추", "블록 옮기기"),
            };

            // 한 줄에 46px씩 일곱 줄이면 쪽 높이(360px) 안에 안내 문장까지 들어간다.
            const int rowsPerColumn = 7;
            const float rowHeight = 46f;

            RectTransform desktopKeys = UiFactory.Rect("KeysDesktop", page);
            UiFactory.Fill(desktopKeys);
            DesktopOnly.Add(desktopKeys.gameObject);
            for (int i = 0; i < rows.Length; i++)
            {
                float x = i / rowsPerColumn * 492f;
                float y = -4f - i % rowsPerColumn * rowHeight;

                RectTransform badge = UiFactory.Badge($"Key{i}", desktopKeys, rows[i].key, Surface, Ink, 40f, 20f);
                UiFactory.Place(badge, UiFactory.TopLeft, new Vector2(x, y), badge.sizeDelta);

                TMP_Text text = UiFactory.Text($"Text{i}", desktopKeys, rows[i].text, 24f, Ink);
                UiFactory.Place(text.rectTransform, UiFactory.TopLeft, new Vector2(x + 172f, y), new Vector2(310f, 40f));
            }

            RectTransform vrKeys = UiFactory.Rect("KeysVr", page);
            UiFactory.Fill(vrKeys);
            VrOnly.Add(vrKeys.gameObject);
            for (int i = 0; i < vrRows.Length; i++)
            {
                float x = i / rowsPerColumn * 492f;
                float y = -4f - i % rowsPerColumn * rowHeight;

                RectTransform badge = UiFactory.Badge($"VrKey{i}", vrKeys, vrRows[i].key, Surface, Ink, 40f, 20f);
                UiFactory.Place(badge, UiFactory.TopLeft, new Vector2(x, y), badge.sizeDelta);

                TMP_Text text = UiFactory.Text($"VrText{i}", vrKeys, vrRows[i].text, 24f, Ink);
                UiFactory.Place(text.rectTransform, UiFactory.TopLeft, new Vector2(x + 232f, y), new Vector2(250f, 40f));
            }

            TMP_Text note = UiFactory.Text("Note", page, "대화는 여러 사람이 함께 들어오는 기능과 같이 연결됩니다.", 20f, Muted);
            UiFactory.Place(note.rectTransform, UiFactory.TopLeft, new Vector2(0f, -4f - rowsPerColumn * rowHeight), new Vector2(960f, 30f));
            return page.gameObject;
        }

        /// <summary>글자가 가운데 있고 왼쪽에 키 딱지가 붙는 큰 단추.</summary>
        private static Button BigButton(string name, Transform parent, string text, string key, Color fill, Color textColor, out TMP_Text label)
        {
            Image box = UiFactory.Card(name, parent, fill, Ink, 18f);

            label = UiFactory.Text("Label", box.transform, text, 24f, textColor, true, TextAlignmentOptions.Center);
            UiFactory.Fill(label.rectTransform);

            if (key != null)
            {
                RectTransform badge = UiFactory.Badge("Key", box.transform, key, AtelierPalette.WithAlpha(Ink, 0.12f), Ink);
                UiFactory.Place(badge, UiFactory.MiddleLeft, new Vector2(16f, 0f), badge.sizeDelta);
                DesktopOnly.Add(badge.gameObject);
            }

            return UiFactory.Clickable(box);
        }

        private static void SetPeopleList(PeopleListView view, TMP_Text header, TMP_Text localName)
        {
            var serialized = new SerializedObject(view);
            serialized.FindProperty("header").objectReferenceValue = header;
            serialized.FindProperty("localName").objectReferenceValue = localName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjects(SerializedProperty array, params Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
