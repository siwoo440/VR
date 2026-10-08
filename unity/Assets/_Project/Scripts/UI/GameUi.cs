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
    /// VR에서는 화면을 눈앞의 판(XrUiPanel)으로 띄우고, 메뉴가 열려 있는 동안 오른손 광선을 보인다.
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

        private const string MapName = "Game";
        private const int LocalPeopleCount = 1;

        private static readonly Key[] DigitKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private LocalPlayer player;
        [SerializeField] private XrUiPanel xrPanel;
        [SerializeField] private HotbarView hotbar;
        [SerializeField] private QuickMenuView menu;
        [SerializeField] private PeopleListView[] peopleLists;
        [SerializeField] private GameObject peoplePanel;
        [SerializeField] private GameObject crosshair;
        [SerializeField] private GameObject focusHint;
        [SerializeField] private GameObject buildHint;
        [SerializeField] private TMP_Text blockCountLabel;
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
        private BlockBuilder builder;
        private BlockWorld world;
        private MapAutoSave autoSave;
        private bool viewLabelSet;
        private bool viewLabelFirstPerson;

        public bool IsMenuOpen => menu != null && menu.IsOpen;

        public bool IsPeopleListVisible => peoplePanel != null && peoplePanel.activeSelf;

        public HotbarView Hotbar => hotbar;

        public QuickMenuView Menu => menu;

        /// <summary>이 기기의 캐릭터.</summary>
        public LocalPlayer Player => player;

        public XrUiPanel XrPanel => xrPanel;

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

            if (hotbar != null) hotbar.Model.SelectionChanged += ShowSelectedPart;
            if (world != null) world.Changed += ShowBlockCount;
            if (autoSave != null) autoSave.Changed += ShowSaveState;
            GameSettings.Changed += ApplySettings;
        }

        private void OnDisable()
        {
            GameSettings.Changed -= ApplySettings;
            if (autoSave != null) autoSave.Changed -= ShowSaveState;
            if (world != null) world.Changed -= ShowBlockCount;
            if (hotbar != null) hotbar.Model.SelectionChanged -= ShowSelectedPart;
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
            ApplyControlMode();
            ShowRoomInfo();
            ApplySettings();
            ShowSelectedPart(hotbar != null ? hotbar.SelectedIndex : HotbarModel.None);
            ShowBlockCount();
            ShowSaveState();
            ShowFlyMode(player != null && player.Motor.IsFlying);
            RefreshHud();
        }

        private void Update()
        {
            if (menuAction.WasPressedThisFrame())
            {
                ToggleMenu();
            }
            else if (IsMenuOpen)
            {
                ReadMenuKeys();
            }
            else
            {
                ReadPlayKeys();
            }

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

        /// <summary>
        /// VR에서 메뉴를 열면 판을 지금의 눈앞에 놓아 그 자리에 두고 오른손 광선을 보인다.
        /// 닫으면 판이 다시 머리를 따라오고 광선을 감춘다. PC에서는 아무 일도 하지 않는다.
        /// </summary>
        private void ShowVrMenu(bool open)
        {
            if (!InVr) return;

            if (xrPanel != null)
            {
                xrPanel.Follow = !open;
                if (open) xrPanel.PlaceForMenu();
            }

            XrRig rig = player.XrRig;
            if (rig != null) rig.ShowPointer(open);
        }

        /// <summary>조작 방식에 맞게 화면을 놓는다: PC는 화면에 겹쳐서, VR은 눈앞의 판으로. 시작할 때와 조작 방식이 바뀔 때 부른다.</summary>
        private void ApplyControlMode()
        {
            bool vr = InVr;
            bool menuOpen = IsMenuOpen;
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

            if (rig != null) rig.ShowPointer(vr && menuOpen);
        }

        private void OnControlModeChanged(ControlMode mode)
        {
            ApplyControlMode();
        }

        /// <summary>VR에서 메뉴가 열려 있는 동안, 광선이 화면에 닿았으면 닿은 자리에서 끝나게 한다.</summary>
        private void UpdatePointer()
        {
            if (!InVr || !IsMenuOpen || xrPanel == null) return;

            XrRig rig = player.XrRig;
            if (rig == null) return;

            rig.SetPointerLength(xrPanel.TryGetPointerHit(out float distance) ? distance : XrRig.DefaultPointerLength);
        }

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

        private void ReadPlayKeys()
        {
            if (peopleAction.WasPressedThisFrame()) GameSettings.ShowPeopleList = !GameSettings.ShowPeopleList;

            // 되돌리기는 부품을 고르지 않았거나 마우스를 잡지 않았어도 되지만, 메뉴가 열려 있으면 이 메서드까지 오지 않는다.
            if (world != null)
            {
                if (undoAction.WasPressedThisFrame() && !world.History.Undo()) Notice.Post(NothingToUndoMessage);
                else if (redoAction.WasPressedThisFrame() && !world.History.Redo()) Notice.Post(NothingToRedoMessage);
            }

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
            if (builder != null) builder.SelectedPart = index;
            if (buildHint != null) buildHint.SetActive(index != HotbarModel.None);
        }

        private void ShowBlockCount()
        {
            if (blockCountLabel != null && world != null) blockCountLabel.text = $"블록 {world.Count}/{world.MaxBlocks}";
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
            if (modeLabel != null) modeLabel.text = flying ? FlyLabel : WalkLabel;
            if (modeDot != null) modeDot.color = flying ? flyDotColor : walkDotColor;
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
        }

        private void RefreshHud()
        {
            bool menuOpen = IsMenuOpen;
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
