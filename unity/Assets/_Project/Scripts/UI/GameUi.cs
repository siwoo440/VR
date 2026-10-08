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
    /// </summary>
    public class GameUi : MonoBehaviour
    {
        private const string MapName = "Game";
        private const int LocalPeopleCount = 1;

        private static readonly Key[] DigitKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private DesktopPlayerController player;
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
        [SerializeField] private TMP_Text viewLabel;
        [SerializeField] private string roomName = "시험 작업실";
        [SerializeField] private string localDisplayName = "손님";

        private InputActionMap map;
        private InputAction menuAction;
        private InputAction peopleAction;
        private InputAction slotNextAction;
        private InputAction slotPreviousAction;
        private BlockBuilder builder;
        private BlockWorld world;
        private bool viewLabelSet;
        private bool viewLabelFirstPerson;

        public bool IsMenuOpen => menu != null && menu.IsOpen;

        public bool IsPeopleListVisible => peoplePanel != null && peoplePanel.activeSelf;

        public HotbarView Hotbar => hotbar;

        public QuickMenuView Menu => menu;

        public DesktopPlayerController Player => player;

        private void Awake()
        {
            if (player == null) player = FindAnyObjectByType<DesktopPlayerController>();
            builder = FindAnyObjectByType<BlockBuilder>();
            world = FindAnyObjectByType<BlockWorld>();
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
            map.Enable();

            if (menu != null)
            {
                menu.ResumeRequested += CloseMenu;
                menu.RespawnRequested += RespawnAndClose;
                menu.ToggleViewRequested += ToggleViewAndClose;
                menu.QuitRequested += AppExit.Request;
            }

            if (menuButton != null) menuButton.onClick.AddListener(ToggleMenu);
            if (hotbar != null) hotbar.Model.SelectionChanged += ShowSelectedPart;
            if (world != null) world.Changed += ShowBlockCount;
            GameSettings.Changed += ApplySettings;
        }

        private void OnDisable()
        {
            GameSettings.Changed -= ApplySettings;
            if (world != null) world.Changed -= ShowBlockCount;
            if (hotbar != null) hotbar.Model.SelectionChanged -= ShowSelectedPart;
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
            ShowRoomInfo();
            ApplySettings();
            ShowSelectedPart(hotbar != null ? hotbar.SelectedIndex : HotbarModel.None);
            ShowBlockCount();
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

            menu.Open();
        }

        /// <summary>메뉴를 닫고 바로 시점 조작으로 돌아간다.</summary>
        public void CloseMenu()
        {
            if (menu == null || !menu.IsOpen) return;

            menu.Close();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            if (player != null)
            {
                player.SetInputBlocked(false);
                player.CaptureLook(true);
            }
        }

        private void RespawnAndClose()
        {
            if (player != null) player.Respawn();
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

        private void ApplySettings()
        {
            if (peoplePanel != null) peoplePanel.SetActive(GameSettings.ShowPeopleList);
        }

        private void RefreshHud()
        {
            bool menuOpen = IsMenuOpen;
            bool captured = player != null && player.LookCaptured;
            bool firstPerson = player == null || player.IsFirstPerson;

            bool building = hotbar != null && hotbar.SelectedIndex != HotbarModel.None;

            if (crosshair != null) crosshair.SetActive(captured && !menuOpen && (firstPerson || building));
            if (focusHint != null) focusHint.SetActive(!captured && !menuOpen);

            if (viewLabel != null && (!viewLabelSet || viewLabelFirstPerson != firstPerson))
            {
                viewLabelSet = true;
                viewLabelFirstPerson = firstPerson;
                viewLabel.text = firstPerson ? "1인칭" : "3인칭";
            }
        }
    }
}
