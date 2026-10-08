using AtelierVerse.Core;
using AtelierVerse.UI;
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

        public static GameObject Build(InputActionAsset actions, Icons icons, Item[] items)
        {
            var root = new GameObject("GameUI");
            var ui = root.AddComponent<GameUi>();

            RectTransform canvas = CreateCanvas(root.transform);
            RectTransform hud = UiFactory.Rect("Hud", canvas);
            UiFactory.Fill(hud);

            Button menuButton = BuildMenuButton(hud, icons.Menu);
            TMP_Text roomLabel = BuildRoomChip(hud);
            PeopleListView peoplePanel = BuildPeoplePanel(hud);
            HotbarView hotbar = BuildHotbar(hud, items);
            GameObject buildHint = BuildBuildHint(hud, out TMP_Text blockCountLabel);
            BuildKeyHints(hud);
            TMP_Text viewLabel = BuildViewChip(hud);
            TMP_Text saveLabel = BuildSaveChip(hud);
            TMP_Text modeLabel = BuildModeChip(hud, out Image modeDot);
            BuildNoticeBar(hud);
            GameObject crosshair = BuildCrosshair(hud);
            GameObject focusHint = BuildFocusHint(hud);

            QuickMenuView menu = BuildQuickMenu(canvas, icons, out TMP_Text menuRoomLabel, out TMP_Text menuNameLabel, out PeopleListView menuPeople);
            CreateEventSystem(root.transform);

            var serialized = new SerializedObject(ui);
            serialized.FindProperty("actions").objectReferenceValue = actions;
            serialized.FindProperty("hotbar").objectReferenceValue = hotbar;
            serialized.FindProperty("menu").objectReferenceValue = menu;
            serialized.FindProperty("peoplePanel").objectReferenceValue = peoplePanel.gameObject;
            serialized.FindProperty("crosshair").objectReferenceValue = crosshair;
            serialized.FindProperty("focusHint").objectReferenceValue = focusHint;
            serialized.FindProperty("buildHint").objectReferenceValue = buildHint;
            serialized.FindProperty("blockCountLabel").objectReferenceValue = blockCountLabel;
            serialized.FindProperty("menuButton").objectReferenceValue = menuButton;
            serialized.FindProperty("roomLabel").objectReferenceValue = roomLabel;
            serialized.FindProperty("menuRoomLabel").objectReferenceValue = menuRoomLabel;
            serialized.FindProperty("menuNameLabel").objectReferenceValue = menuNameLabel;
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

        private static RectTransform CreateCanvas(Transform parent)
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
            return (RectTransform)gameObject.transform;
        }

        /// <summary>마우스로만 누른다. 키보드로 옮겨 다니는 선택을 끄지 않으면 Space나 WASD가 단추를 누르게 된다.</summary>
        private static void CreateEventSystem(Transform parent)
        {
            var gameObject = new GameObject("EventSystem");
            gameObject.transform.SetParent(parent, false);

            var eventSystem = gameObject.AddComponent<EventSystem>();
            eventSystem.sendNavigationEvents = false;
            gameObject.AddComponent<InputSystemUIInputModule>();
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

        private static HotbarView BuildHotbar(RectTransform hud, Item[] items)
        {
            float width = SlotCount * SlotSize + (SlotCount - 1) * SlotGap;
            RectTransform bar = UiFactory.Rect("Hotbar", hud);
            UiFactory.Place(bar, UiFactory.BottomCenter, new Vector2(0f, Margin), new Vector2(width, SlotSize));

            var view = bar.gameObject.AddComponent<HotbarView>();
            var serialized = new SerializedObject(view);
            SerializedProperty slots = serialized.FindProperty("slots");
            slots.arraySize = SlotCount;

            for (int i = 0; i < SlotCount; i++)
            {
                Image slot = UiFactory.Box($"Slot{i + 1}", bar, Paper, 14f);
                UiFactory.Place(slot.rectTransform, UiFactory.BottomLeft, new Vector2(i * (SlotSize + SlotGap), 0f), new Vector2(SlotSize, SlotSize));

                var shadow = slot.gameObject.AddComponent<Shadow>();
                shadow.effectColor = Ink;
                shadow.effectDistance = new Vector2(4f, -4f);
                Image frame = UiFactory.Outline(slot, Line);

                TMP_Text number = UiFactory.Text("Number", slot.transform, (i + 1).ToString(), 16f, Muted, true);
                UiFactory.Place(number.rectTransform, UiFactory.TopLeft, new Vector2(10f, -4f), new Vector2(20f, 22f));

                Image swatch = null;
                string itemName = string.Empty;
                if (i < items.Length)
                {
                    itemName = items[i].Name;
                    swatch = UiFactory.Box("Swatch", slot.transform, items[i].Color, 8f);
                    UiFactory.Place(swatch.rectTransform, UiFactory.Center, new Vector2(0f, -5f), new Vector2(38f, 38f));
                    UiFactory.Outline(swatch, AtelierPalette.WithAlpha(Ink, 0.55f));
                }

                SerializedProperty element = slots.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = slot.rectTransform;
                element.FindPropertyRelative("frame").objectReferenceValue = frame;
                element.FindPropertyRelative("swatch").objectReferenceValue = swatch;
                element.FindPropertyRelative("itemName").stringValue = itemName;
            }

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

        /// <summary>부품을 골랐을 때만 부품 칸 위에 보이는 안내. 놓고 지우고 칠하고 되돌리는 방법과 지금까지 놓인 블록 수를 보여 준다.</summary>
        private static GameObject BuildBuildHint(RectTransform hud, out TMP_Text countLabel)
        {
            (string key, string label)[] hints =
            {
                ("왼쪽 누르기", "놓기"),
                ("오른쪽 누르기", "지우기"),
                ("가운데·F", "칠하기"),
                ("Ctrl+Z", "되돌리기"),
                ("Ctrl+Y", "다시 실행"),
            };

            Image glass = UiFactory.Box("BuildHint", hud, DarkGlass, 22f);
            float x = 14f;

            foreach ((string key, string label) in hints)
            {
                RectTransform badge = UiFactory.Badge($"Key_{label}", glass.transform, key, AtelierPalette.WithAlpha(Paper, 0.94f), Ink);
                UiFactory.Place(badge, UiFactory.MiddleLeft, new Vector2(x, 0f), badge.sizeDelta);
                x += badge.sizeDelta.x + 8f;

                TMP_Text text = UiFactory.Text($"Label_{label}", glass.transform, label, 18f, Paper);
                float width = UiFactory.WidthOf(text);
                UiFactory.Place(text.rectTransform, UiFactory.MiddleLeft, new Vector2(x, 0f), new Vector2(width + 2f, 28f));
                x += width + 18f;
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
                ("Esc", "메뉴"),
                ("Tab", "사람들"),
                ("휠", "시점"),
                ("V", "날기"),
                ("1~9", "부품"),
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

        /// <summary>화면 위 가운데의 알림 띠(6일차). 평소에는 숨겨져 있고 Notice.Post가 오면 잠시 보인다. 너비는 실행 중에 글자에 맞춘다.</summary>
        private static NoticeBar BuildNoticeBar(RectTransform hud)
        {
            RectTransform holder = UiFactory.Rect("Notice", hud);
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

        private static QuickMenuView BuildQuickMenu(RectTransform canvas, Icons icons, out TMP_Text roomLabel, out TMP_Text nameLabel, out PeopleListView people)
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

            TMP_Text brand = UiFactory.Text("Brand", panel.transform, "Atelier | Verse", 22f, Muted, true, TextAlignmentOptions.Right);
            UiFactory.Place(brand.rectTransform, UiFactory.TopRight, new Vector2(-PanelPadding, -38f), new Vector2(300f, 32f));

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

            GameObject shortcutPage = BuildShortcutPage(pages, icons, out Button respawnTile, out Button viewTile, out TMP_Text viewTileTitle);
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
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>VRChat의 메뉴처럼 큰 타일로 자주 가는 곳을 둔다. 아직 만들지 않은 곳은 누를 수 없게 하고 "준비 중"으로 표시한다.</summary>
        private static GameObject BuildShortcutPage(RectTransform pages, Icons icons, out Button respawnTile, out Button viewTile, out TMP_Text viewTileTitle)
        {
            RectTransform page = UiFactory.Rect("ShortcutPage", pages);
            UiFactory.Fill(page);

            respawnTile = BuildTile(page, 0, "RespawnTile", "시작 위치로", "처음 선 자리로 돌아갑니다", icons.Respawn, Gold, Ink, "R", true, out _);
            viewTile = BuildTile(page, 1, "ViewTile", "3인칭으로 보기", "휠을 굴려도 바뀝니다", icons.View, AtelierPalette.Blue, Paper, null, true, out viewTileTitle);
            BuildTile(page, 2, "HomeTile", "내 작업실", "내가 만든 맵으로 갑니다", icons.Home, AtelierPalette.Clay, Paper, null, false, out _);
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

        private static GameObject BuildSettingsPage(RectTransform pages)
        {
            RectTransform page = UiFactory.Rect("SettingsPage", pages);
            UiFactory.Fill(page);

            Slider look = BuildSliderRow(page, 0, "마우스 감도", "LookSlider", out TMP_Text lookValue);
            Slider fieldOfView = BuildSliderRow(page, 1, "시야각", "FieldOfViewSlider", out TMP_Text fieldOfViewValue);

            RectTransform toggleRow = BuildRow(page, 2, "사람들 목록 보이기");
            Image toggleBox = UiFactory.Box("PeopleToggle", toggleRow, Paper, 10f);
            UiFactory.Place(toggleBox.rectTransform, UiFactory.MiddleRight, Vector2.zero, new Vector2(40f, 40f));
            toggleBox.raycastTarget = true;
            UiFactory.Outline(toggleBox, Ink);
            Image check = UiFactory.Box("Check", toggleBox.transform, Gold, 6f);
            UiFactory.Place(check.rectTransform, UiFactory.Center, Vector2.zero, new Vector2(24f, 24f));

            var toggle = toggleBox.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = toggleBox;
            toggle.graphic = check;
            UiFactory.NoNavigation(toggle);

            TMP_Text toggleHint = UiFactory.Text("Hint", toggleRow, "Tab 키로도 켜고 끕니다", 18f, Muted, false, TextAlignmentOptions.Right);
            UiFactory.Place(toggleHint.rectTransform, UiFactory.MiddleRight, new Vector2(-60f, 0f), new Vector2(360f, 30f));

            Button reset = BigButton("Reset", page, "기본값으로", null, Paper, Ink, out _);
            UiFactory.Place((RectTransform)reset.transform, UiFactory.BottomLeft, new Vector2(0f, 6f), new Vector2(220f, 52f));

            var panel = page.gameObject.AddComponent<SettingsPanel>();
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("lookSlider").objectReferenceValue = look;
            serialized.FindProperty("lookValue").objectReferenceValue = lookValue;
            serialized.FindProperty("fieldOfViewSlider").objectReferenceValue = fieldOfView;
            serialized.FindProperty("fieldOfViewValue").objectReferenceValue = fieldOfViewValue;
            serialized.FindProperty("peopleListToggle").objectReferenceValue = toggle;
            serialized.FindProperty("resetButton").objectReferenceValue = reset;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return page.gameObject;
        }

        /// <summary>설정 한 줄의 바탕과 왼쪽 이름.</summary>
        private static RectTransform BuildRow(RectTransform page, int index, string title)
        {
            RectTransform row = UiFactory.Rect($"Row{index}", page);
            UiFactory.StretchTop(row, -4f - index * 76f, 64f);

            TMP_Text label = UiFactory.Text("Label", row, title, 24f, Ink, true);
            UiFactory.Place(label.rectTransform, UiFactory.MiddleLeft, Vector2.zero, new Vector2(320f, 36f));
            return row;
        }

        private static Slider BuildSliderRow(RectTransform page, int index, string title, string sliderName, out TMP_Text value)
        {
            RectTransform row = BuildRow(page, index, title);

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
                ("1~9", "부품 고르기"),
                ("왼쪽 누르기", "블록 놓기"),
                ("오른쪽 누르기", "블록 지우기"),
                ("가운데 · F", "블록 칠하기"),
                ("Ctrl+Z", "되돌리기"),
                ("Ctrl+Y", "다시 실행"),
                ("Tab · Esc", "사람들 목록 · 메뉴"),
            };

            // 한 줄에 54px씩 여섯 줄이면 쪽 높이(360px) 안에 안내 문장까지 들어간다.
            const int rowsPerColumn = 6;
            const float rowHeight = 54f;
            for (int i = 0; i < rows.Length; i++)
            {
                float x = i / rowsPerColumn * 492f;
                float y = -4f - i % rowsPerColumn * rowHeight;

                RectTransform badge = UiFactory.Badge($"Key{i}", page, rows[i].key, Surface, Ink, 40f, 20f);
                UiFactory.Place(badge, UiFactory.TopLeft, new Vector2(x, y), badge.sizeDelta);

                TMP_Text text = UiFactory.Text($"Text{i}", page, rows[i].text, 24f, Ink);
                UiFactory.Place(text.rectTransform, UiFactory.TopLeft, new Vector2(x + 172f, y), new Vector2(310f, 40f));
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
