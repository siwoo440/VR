using AtelierVerse.Core;
using AtelierVerse.Player;
using AtelierVerse.World;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 게임 화면 전체를 묶는 곳. 늘 보이는 화면(위쪽 띠, 사람들 목록, 부품 칸)과 Esc 메뉴를 잇고,
    /// 메뉴가 열려 있는 동안 캐릭터 조작을 막는다. 부품 칸에서 고른 부품을 블록 놓기(BlockBuilder)에 알려 준다.
    /// 캐릭터는 이 기기의 캐릭터(LocalPlayer)로만 알고, 키보드·마우스인지 VR인지는 그쪽에 묻는다.
    /// VR에서는 화면을 눈앞의 판(XrUiPanel)으로 띄우고, 부품 칸과 상태 표시는 왼손 위의 부품 판(XrHandPalette)에 보인다.
    /// 부품 고르는 창(PartPickerView)을 열고 닫으며, 창에서 고른 부품을 부품 칸에 넣고 그 배치를 이 기기에 저장한다.
    /// 오른손 광선은 가리킨 화면이나 블록을 놓을 자리에서 끝나게 한다.
    /// </summary>
    public class GameUi : MonoBehaviour
    {
        public const string NothingToUndoMessage = "되돌릴 것이 없습니다";
        public const string NothingToRedoMessage = "다시 실행할 것이 없습니다";
        public const string FlyOnMessage = "날기 · Space로 오르고 Shift로 내려옵니다";
        public const string FlyOnVrMessage = "날기 · 오른쪽 스틱을 위아래로 밀어 오르내립니다";
        public const string FlyOffMessage = "걷기로 돌아왔습니다";
        public const string WalkLabel = "걷기";
        public const string FlyLabel = "날기";
        public const string BrandName = "Atelier | Verse";
        public const string GrabLabel = "옮기기";
        public const string CarryingLabel = "옮기는 중";

        private const string MapName = "Game";
        private const int LocalPeopleCount = 1;

        private static readonly Key[] DigitKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private LocalPlayer player;
        [SerializeField] private XrUiPanel xrPanel;
        [SerializeField] private XrHandPalette palette;
        [SerializeField] private HotbarView hotbar;
        [SerializeField] private QuickMenuView menu;
        [SerializeField] private PartPickerView picker;
        [SerializeField] private PeopleListView[] peopleLists;
        [SerializeField] private GameObject peoplePanel;
        [SerializeField] private GameObject crosshair;
        [SerializeField] private GameObject focusHint;
        [SerializeField] private GameObject buildHint;
        [SerializeField] private TMP_Text blockCountLabel;
        [SerializeField] private TMP_Text rotateLabel;
        [SerializeField] private TMP_Text grabLabel;
        [SerializeField] private TMP_Text snapLabel;
        [SerializeField] private Color hintTextColor = Color.white;
        [SerializeField] private Color hintActiveColor = Color.yellow;
        [SerializeField] private Button menuButton;
        [SerializeField] private TMP_Text roomLabel;
        [SerializeField] private TMP_Text menuRoomLabel;
        [SerializeField] private TMP_Text menuNameLabel;
        [SerializeField] private TMP_Text brandLabel;
        [SerializeField] private TMP_Text viewLabel;
        [SerializeField] private TMP_Text saveLabel;
        [SerializeField] private Color saveTextColor = Color.white;
        [SerializeField] private Color saveFailedColor = Color.yellow;
        [SerializeField] private TMP_Text modeLabel;
        [SerializeField] private Image modeDot;
        [SerializeField] private Color walkDotColor = Color.white;
        [SerializeField] private Color flyDotColor = Color.yellow;
        [SerializeField] private string roomName = "시험 작업실";
        [SerializeField] private string localDisplayName = "손님";

        private InputActionMap map;
        private InputAction menuAction;
        private InputAction peopleAction;
        private InputAction slotNextAction;
        private InputAction slotPreviousAction;
        private InputAction undoAction;
        private InputAction redoAction;
        private InputAction partsAction;
        private BlockBuilder builder;
        private BlockWorld world;
        private MapAutoSave autoSave;
        private bool viewLabelSet;
        private bool viewLabelFirstPerson;

        public bool IsMenuOpen => menu != null && menu.IsOpen;

        /// <summary>부품 고르는 창이 열려 있는지.</summary>
        public bool IsPickerOpen => picker != null && picker.IsOpen;

        /// <summary>메뉴나 부품 고르는 창처럼 조작을 막는 창이 열려 있는지.</summary>
        public bool IsModalOpen => IsMenuOpen || IsPickerOpen;

        public bool IsPeopleListVisible => peoplePanel != null && peoplePanel.activeSelf;

        public HotbarView Hotbar => hotbar;

        public QuickMenuView Menu => menu;

        public PartPickerView Picker => picker;

        /// <summary>이 기기의 캐릭터.</summary>
        public LocalPlayer Player => player;

        public XrUiPanel XrPanel => xrPanel;

        /// <summary>VR에서 왼손 위에 뜨는 부품 판.</summary>
        public XrHandPalette Palette => palette;

        private bool InVr => player != null && player.IsVr;

        private void Awake()
        {
            if (player == null) player = FindAnyObjectByType<LocalPlayer>();
            builder = FindAnyObjectByType<BlockBuilder>();
            world = FindAnyObjectByType<BlockWorld>();
            autoSave = FindAnyObjectByType<MapAutoSave>();
        }

        private void OnEnable()
        {
            if (actions == null)
            {
                Debug.LogError("[Atelier Verse] GameUi에 입력 자산이 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }

            map = actions.FindActionMap(MapName, true);
            menuAction = map.FindAction("Menu", true);
            peopleAction = map.FindAction("People", true);
            slotNextAction = map.FindAction("SlotNext", true);
            slotPreviousAction = map.FindAction("SlotPrevious", true);
            undoAction = map.FindAction("Undo", true);
            redoAction = map.FindAction("Redo", true);
            partsAction = map.FindAction("Parts", true);
            map.Enable();

            if (menu != null)
            {
                menu.ResumeRequested += CloseMenu;
                menu.RespawnRequested += RespawnAndClose;
                menu.ToggleViewRequested += ToggleViewAndClose;
                menu.QuitRequested += AppExit.Request;
            }

            if (menuButton != null) menuButton.onClick.AddListener(ToggleMenu);
            if (player != null)
            {
                player.Motor.FlyModeChanged += OnFlyModeChanged;
                player.ModeChanged += OnControlModeChanged;
            }

            if (hotbar != null)
            {
                hotbar.Model.SelectionChanged += ShowSelectedPart;
                hotbar.Model.SlotsChanged += OnSlotsChanged;
            }

            if (picker != null)
            {
                if (hotbar != null) picker.Bind(hotbar.Model);
                picker.PartChosen += PutPart;
                picker.ClearRequested += ClearSlot;
                picker.CloseRequested += ClosePicker;
            }

            if (palette != null)
            {
                // 부품 판의 부품 칸은 화면 아래의 부품 칸과 같은 선택 상태를 쓴다.
                if (hotbar != null && palette.Hotbar != null) palette.Hotbar.Bind(hotbar.Model);
                palette.UndoRequested += Undo;
                palette.RedoRequested += Redo;
                palette.SnapRequested += CycleSnap;
                palette.PartsRequested += OpenPicker;
                if (palette.Hotbar != null) palette.Hotbar.EmptySlotPressed += OpenPickerFor;
            }

            if (builder != null) builder.StateChanged += ShowBuildState;

            if (world != null) world.Changed += ShowBlockCount;
            if (autoSave != null) autoSave.Changed += ShowSaveState;
            GameSettings.Changed += ApplySettings;
        }

        private void OnDisable()
        {
            GameSettings.Changed -= ApplySettings;
            if (autoSave != null) autoSave.Changed -= ShowSaveState;
            if (world != null) world.Changed -= ShowBlockCount;
            if (builder != null) builder.StateChanged -= ShowBuildState;
            if (palette != null)
            {
                if (palette.Hotbar != null) palette.Hotbar.EmptySlotPressed -= OpenPickerFor;
                palette.PartsRequested -= OpenPicker;
                palette.SnapRequested -= CycleSnap;
                palette.RedoRequested -= Redo;
                palette.UndoRequested -= Undo;
            }

            if (picker != null)
            {
                picker.CloseRequested -= ClosePicker;
                picker.ClearRequested -= ClearSlot;
                picker.PartChosen -= PutPart;
            }

            if (hotbar != null)
            {
                hotbar.Model.SlotsChanged -= OnSlotsChanged;
                hotbar.Model.SelectionChanged -= ShowSelectedPart;
            }
            if (player != null)
            {
                player.ModeChanged -= OnControlModeChanged;
                player.Motor.FlyModeChanged -= OnFlyModeChanged;
            }

            if (menuButton != null) menuButton.onClick.RemoveListener(ToggleMenu);

            if (menu != null)
            {
                menu.ResumeRequested -= CloseMenu;
                menu.RespawnRequested -= RespawnAndClose;
                menu.ToggleViewRequested -= ToggleViewAndClose;
                menu.QuitRequested -= AppExit.Request;
            }

            map?.Disable();
        }

        private void Start()
        {
            LoadHotbar();
            ApplyControlMode();
            ShowRoomInfo();
            ApplySettings();
            ShowSelectedPart(hotbar != null ? hotbar.SelectedIndex : HotbarModel.None);
            ShowBlockCount();
            ShowBuildState();
            ShowSaveState();
            ShowFlyMode(player != null && player.Motor.IsFlying);
            RefreshHud();
        }

        private void Update()
        {
            if (menuAction.WasPressedThisFrame())
            {
                // 부품 고르는 창이 열려 있으면 메뉴 키는 그 창을 닫는다.
                if (IsPickerOpen) ClosePicker();
                else ToggleMenu();
            }
            else if (IsMenuOpen)
            {
                ReadMenuKeys();
            }
            else if (IsPickerOpen)
            {
                ReadPickerKeys();
            }
            else
            {
                ReadPlayKeys();
            }

            if (palette != null) palette.SetWanted(InVr && !IsModalOpen);
            UpdatePointer();
            RefreshHud();
        }

        public void ToggleMenu()
        {
            if (IsMenuOpen) CloseMenu();
            else OpenMenu();
        }

        public void OpenMenu()
        {
            if (menu == null || menu.IsOpen) return;

            if (IsPickerOpen) picker.Close();

            if (player != null)
            {
                player.SetInputBlocked(true);
                menu.ShowViewMode(player.IsFirstPerson);
            }

            ShowVrMenu(true);
            menu.Open();
        }

        /// <summary>메뉴를 닫고 바로 조작으로 돌아간다. PC에서는 마우스를 다시 잡는다.</summary>
        public void CloseMenu()
        {
            if (menu == null || !menu.IsOpen) return;

            menu.Close();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            if (player != null) player.ResumeControl();
            ShowVrMenu(false);
        }

        /// <summary>부품 고르는 창을 연다. 넣을 칸은 고른 칸, 없으면 가장 앞의 빈 칸, 그것도 없으면 첫 칸이다.</summary>
        public void OpenPicker()
        {
            if (hotbar == null)
            {
                OpenPickerFor(0);
                return;
            }

            int slot = hotbar.SelectedIndex;
            if (slot == HotbarModel.None) slot = hotbar.Model.FirstEmpty();
            OpenPickerFor(slot == HotbarModel.None ? 0 : slot);
        }

        /// <summary>부품 고르는 창을 열고 slot 칸에 넣게 한다. 메뉴처럼 창이 열려 있는 동안 캐릭터 조작을 막는다.</summary>
        public void OpenPickerFor(int slot)
        {
            if (picker == null || IsMenuOpen) return;

            if (!picker.IsOpen)
            {
                if (player != null) player.SetInputBlocked(true);
                ShowVrMenu(true);
            }

            picker.Open(slot);
        }

        /// <summary>부품 고르는 창을 닫고 바로 조작으로 돌아간다.</summary>
        public void ClosePicker()
        {
            if (picker == null || !picker.IsOpen) return;

            picker.Close();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            if (player != null) player.ResumeControl();
            ShowVrMenu(false);
        }

        public void TogglePicker()
        {
            if (IsPickerOpen) ClosePicker();
            else OpenPicker();
        }

        /// <summary>부품을 칸에 넣고 그 칸을 고른 뒤 창을 닫는다. 바로 놓을 수 있는 상태가 된다.</summary>
        private void PutPart(int slot, int part)
        {
            if (hotbar == null || hotbar.Catalog == null || !hotbar.Catalog.IsValid(part)) return;

            hotbar.Model.SetPart(slot, part);
            hotbar.Model.Choose(slot);
            ClosePicker();
        }

        private void ClearSlot(int slot)
        {
            if (hotbar != null) hotbar.Model.SetPart(slot, HotbarModel.None);
        }

        /// <summary>칸에 든 부품이 바뀌면 고른 부품을 다시 알리고 배치를 저장한다.</summary>
        private void OnSlotsChanged()
        {
            ShowSelectedPart(hotbar.SelectedIndex);
            SaveHotbar();
        }

        /// <summary>이 기기에 저장해 둔 부품 칸의 배치를 불러온다. 저장한 것이 없으면 처음의 배치 그대로다. 목록에 없는 이름의 칸은 비운다.</summary>
        private void LoadHotbar()
        {
            if (hotbar == null || hotbar.Catalog == null) return;

            string saved = GameSettings.HotbarParts;
            if (string.IsNullOrEmpty(saved)) return;

            string[] ids = saved.Split(',');
            for (int i = 0; i < hotbar.SlotCount; i++)
            {
                int part = i < ids.Length && ids[i].Length > 0 ? hotbar.Catalog.IndexOf(ids[i]) : HotbarModel.None;
                hotbar.Model.SetPart(i, part);
            }
        }

        private void SaveHotbar()
        {
            if (hotbar == null || hotbar.Catalog == null) return;

            var ids = new string[hotbar.SlotCount];
            for (int i = 0; i < ids.Length; i++)
            {
                int part = hotbar.Model.GetPart(i);
                ids[i] = hotbar.Catalog.IsValid(part) ? hotbar.Catalog.Get(part).id : string.Empty;
            }

            GameSettings.HotbarParts = string.Join(",", ids);
        }

        /// <summary>
        /// VR에서 메뉴를 열면 판을 지금의 눈앞에 놓아 그 자리에 둔다. 닫으면 판이 다시 머리를 따라온다.
        /// PC에서는 아무 일도 하지 않는다.
        /// </summary>
        private void ShowVrMenu(bool open)
        {
            if (!InVr || xrPanel == null) return;

            xrPanel.Follow = !open;
            if (open) xrPanel.PlaceForMenu();
        }

        /// <summary>조작 방식에 맞게 화면을 놓는다: PC는 화면에 겹쳐서, VR은 눈앞의 판으로. 시작할 때와 조작 방식이 바뀔 때 부른다.</summary>
        private void ApplyControlMode()
        {
            bool vr = InVr;
            bool menuOpen = IsModalOpen;
            XrRig rig = player != null ? player.XrRig : null;

            if (menu != null) menu.ShowControlMode(vr);

            if (xrPanel != null)
            {
                if (vr)
                {
                    xrPanel.Follow = !menuOpen;
                    xrPanel.ShowInWorld(player.ViewCamera, rig != null ? rig.Origin : null);
                }
                else
                {
                    xrPanel.ShowOnScreen();
                }
            }

            if (palette != null)
            {
                if (vr && rig != null) palette.Attach(rig.LeftHand, player.ViewCamera);
                else palette.Detach();
            }

            // VR의 광선은 UpdatePointer가 매 프레임 고친다. PC로 돌아오면 여기서 감춘다.
            if (rig != null && !vr) rig.ShowPointer(false);
        }

        private void OnControlModeChanged(ControlMode mode)
        {
            ApplyControlMode();
        }

        /// <summary>
        /// VR의 오른손 광선을 고친다. 화면(메뉴, 부품 판)에 닿았으면 닿은 자리에서, 블록을 놓을 자리를 가리키면 그 자리에서 끝난다.
        /// 메뉴가 닫혀 있고 부품도 고르지 않아 가리켜 할 일이 없을 때는, 가리키는 방향만 알 수 있게 짧게 둔다.
        /// </summary>
        private void UpdatePointer()
        {
            if (!InVr) return;

            XrRig rig = player.XrRig;
            if (rig == null) return;

            bool building = hotbar != null && hotbar.SelectedIndex != HotbarModel.None;
            float length;
            if (xrPanel != null && xrPanel.TryGetPointerHit(out float uiDistance)) length = uiDistance;
            else if (IsModalOpen) length = XrRig.DefaultPointerLength;
            else if (builder != null && builder.HasAimHit) length = builder.AimDistance;
            else length = building ? XrRig.DefaultPointerLength : XrRig.IdlePointerLength;

            rig.ShowPointer(true);
            rig.SetPointerLength(length);
        }

        /// <summary>마지막 편집을 되돌린다. 되돌릴 것이 없으면 알린다. Ctrl+Z와 부품 판의 단추가 부른다.</summary>
        public void Undo()
        {
            if (world != null && !world.History.Undo()) Notice.Post(NothingToUndoMessage);
        }

        /// <summary>되돌린 편집을 다시 실행한다. 다시 실행할 것이 없으면 알린다. Ctrl+Y와 부품 판의 단추가 부른다.</summary>
        public void Redo()
        {
            if (world != null && !world.History.Redo()) Notice.Post(NothingToRedoMessage);
        }

        /// <summary>맞추기 도우미를 다음 단계로 바꾼다. 부품 판의 단추가 부른다(PC에서는 블록 놓기가 키를 직접 읽는다).</summary>
        public void CycleSnap()
        {
            if (builder != null) builder.CycleSnap();
        }

        /// <summary>돌리기·옮기기·맞추기 안내에 적힌 글자. 차례로 돌린 각도, 잡고 있는지, 맞추기 단계다.</summary>
        public string RotateText => rotateLabel != null ? rotateLabel.text : string.Empty;

        public string GrabText => grabLabel != null ? grabLabel.text : string.Empty;

        public string SnapText => snapLabel != null ? snapLabel.text : string.Empty;

        private void RespawnAndClose()
        {
            if (player != null) player.Motor.Respawn();
            CloseMenu();
        }

        private void ToggleViewAndClose()
        {
            if (player != null) player.ToggleView();
            CloseMenu();
        }

        private void ReadMenuKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) RespawnAndClose();
        }

        /// <summary>부품 고르는 창이 열려 있을 때의 키: 부품 창 키로 닫고, 숫자 키로 넣을 칸을 고른다.</summary>
        private void ReadPickerKeys()
        {
            if (partsAction.WasPressedThisFrame())
            {
                ClosePicker();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || hotbar == null) return;

            int count = Mathf.Min(DigitKeys.Length, hotbar.SlotCount);
            for (int i = 0; i < count; i++)
            {
                if (keyboard[DigitKeys[i]].wasPressedThisFrame) picker.SetTarget(i);
            }
        }

        private void ReadPlayKeys()
        {
            if (partsAction.WasPressedThisFrame())
            {
                OpenPicker();
                return;
            }

            if (peopleAction.WasPressedThisFrame()) GameSettings.ShowPeopleList = !GameSettings.ShowPeopleList;

            // 되돌리기는 부품을 고르지 않았거나 마우스를 잡지 않았어도 되지만, 메뉴가 열려 있으면 이 메서드까지 오지 않는다.
            if (undoAction.WasPressedThisFrame()) Undo();
            else if (redoAction.WasPressedThisFrame()) Redo();

            if (hotbar == null) return;

            if (slotNextAction.WasPressedThisFrame()) hotbar.Model.SelectNext();
            if (slotPreviousAction.WasPressedThisFrame()) hotbar.Model.SelectPrevious();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            int count = Mathf.Min(DigitKeys.Length, hotbar.SlotCount);
            for (int i = 0; i < count; i++)
            {
                KeyControl key = keyboard[DigitKeys[i]];
                if (key.wasPressedThisFrame) hotbar.Select(i);
            }
        }

        private void ShowRoomInfo()
        {
            string muted = ColorUtility.ToHtmlStringRGB(AtelierPalette.Muted);
            if (roomLabel != null) roomLabel.text = $"{roomName}  <color=#{muted}>{LocalPeopleCount}/{RoomRules.MaxPeople}</color>";
            if (menuRoomLabel != null) menuRoomLabel.text = $"{roomName} · {LocalPeopleCount}/{RoomRules.MaxPeople}";
            if (menuNameLabel != null) menuNameLabel.text = localDisplayName;
            // 어느 판에서 생긴 문제인지 알 수 있도록 메뉴에 버전을 적는다. 값은 프로젝트 설정의 bundleVersion이다.
            if (brandLabel != null) brandLabel.text = $"{BrandName}  v{Application.version}";

            foreach (PeopleListView list in peopleLists)
            {
                if (list != null) list.Show(localDisplayName, LocalPeopleCount, RoomRules.MaxPeople);
            }

            if (player != null)
            {
                Nameplate nameplate = player.GetComponentInChildren<Nameplate>(true);
                if (nameplate != null) nameplate.SetName(localDisplayName);
            }
        }

        /// <summary>고른 부품을 블록 놓기에 알리고, 부품을 골랐을 때만 놓기 안내를 보인다.</summary>
        private void ShowSelectedPart(int index)
        {
            // 칸의 번호가 아니라 그 칸에 든 부품의 번호를 알린다. 칸에 든 부품은 부품 고르는 창에서 바뀔 수 있다.
            if (builder != null) builder.SelectedPart = hotbar != null ? hotbar.SelectedPart : BlockBuilder.NoPart;
            if (buildHint != null) buildHint.SetActive(index != HotbarModel.None);
            if (palette != null) palette.ShowTitle(hotbar != null ? hotbar.SelectedItemName : string.Empty);
        }

        private void ShowBlockCount()
        {
            if (world == null) return;

            string text = $"블록 {world.Count}/{world.MaxBlocks}";
            if (blockCountLabel != null) blockCountLabel.text = text;
            if (palette != null) palette.ShowBlockCount(text);
        }

        /// <summary>
        /// 놓기 안내의 돌리기·옮기기·맞추기 자리에 지금 상태를 적는다: 돌린 각도, 블록을 잡고 있는지, 맞추기 단계.
        /// VR에서는 맞추기 단계를 부품 판의 단추에 적는다.
        /// </summary>
        private void ShowBuildState()
        {
            if (builder == null) return;

            if (rotateLabel != null) rotateLabel.text = $"돌리기 {builder.Yaw:0}°";
            if (grabLabel != null)
            {
                grabLabel.text = builder.IsCarrying ? CarryingLabel : GrabLabel;
                grabLabel.color = builder.IsCarrying ? hintActiveColor : hintTextColor;
            }

            string snap = $"맞추기 {builder.SnapText}";
            if (snapLabel != null)
            {
                snapLabel.text = snap;
                snapLabel.color = builder.SnapLevel != 0 ? hintActiveColor : hintTextColor;
            }

            if (palette != null) palette.ShowSnap(snap);
        }

        /// <summary>날기를 켜고 끌 때 표시를 바꾸고 조작법을 알린다.</summary>
        private void OnFlyModeChanged(bool flying)
        {
            ShowFlyMode(flying);
            Notice.Post(flying ? (InVr ? FlyOnVrMessage : FlyOnMessage) : FlyOffMessage);
        }

        /// <summary>오른쪽 아래의 걷기·날기 표시.</summary>
        private void ShowFlyMode(bool flying)
        {
            string text = flying ? FlyLabel : WalkLabel;
            Color color = flying ? flyDotColor : walkDotColor;
            if (modeLabel != null) modeLabel.text = text;
            if (modeDot != null) modeDot.color = color;
            if (palette != null) palette.ShowMode(text, color);
        }

        /// <summary>오른쪽 아래의 저장 표시. 자동 저장이 없으면 표시를 감춘다.</summary>
        private void ShowSaveState()
        {
            if (saveLabel == null) return;

            if (autoSave == null)
            {
                saveLabel.transform.parent.gameObject.SetActive(false);
                return;
            }

            saveLabel.text = autoSave.Message;
            saveLabel.color = autoSave.State == SaveState.Failed ? saveFailedColor : saveTextColor;
        }

        private void ApplySettings()
        {
            if (peoplePanel != null) peoplePanel.SetActive(GameSettings.ShowPeopleList);
            // 맞추기 단계도 개인 설정에 저장되므로, 설정이 바뀌면 표시를 다시 맞춘다.
            ShowBuildState();
        }

        private void RefreshHud()
        {
            bool menuOpen = IsModalOpen;
            bool aiming = player != null && player.IsAiming;
            bool firstPerson = player == null || player.IsFirstPerson;

            bool building = hotbar != null && hotbar.SelectedIndex != HotbarModel.None;

            if (crosshair != null) crosshair.SetActive(aiming && !menuOpen && (firstPerson || building));
            // "화면을 누르면 둘러볼 수 있습니다"는 마우스를 잡아야 하는 PC 조작에서만 뜻이 있다.
            bool desktopControl = player != null && !player.IsVr;
            if (focusHint != null) focusHint.SetActive(desktopControl && !aiming && !menuOpen);

            if (viewLabel != null && (!viewLabelSet || viewLabelFirstPerson != firstPerson))
            {
                viewLabelSet = true;
                viewLabelFirstPerson = firstPerson;
                viewLabel.text = firstPerson ? "1인칭" : "3인칭";
            }
        }
    }
}
