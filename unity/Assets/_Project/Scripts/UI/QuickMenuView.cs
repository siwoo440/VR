using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// Esc로 여는 메뉴. 위쪽 탭과 아래쪽 큰 단추는 로블록스의 메뉴를, 바로가기의 큰 타일은 VRChat의 메뉴를 따랐다.
    /// 무엇을 할지는 직접 처리하지 않고 요청 이벤트로 알린다.
    /// 조작 방식(키보드·마우스, VR)에 따라 그 조작에 맞는 안내만 보인다.
    /// </summary>
    public class QuickMenuView : MonoBehaviour
    {
        private const float QuitConfirmSeconds = 3f;
        private const float UnavailableAlpha = 0.62f;

        [SerializeField] private GameObject root;
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private GameObject[] tabPages;
        [SerializeField] private GameObject[] tabMarkers;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button respawnButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text quitLabel;
        [SerializeField] private Button respawnTile;
        [SerializeField] private Button viewTile;
        [SerializeField] private TMP_Text viewTileTitle;
        [SerializeField] private CanvasGroup viewTileGroup;
        [SerializeField] private Button mapsTile;
        [SerializeField] private GameObject[] desktopOnly;
        [SerializeField] private GameObject[] vrOnly;
        [SerializeField] private string quitText = "게임 끝내기";
        [SerializeField] private string quitConfirmText = "한 번 더 누르면 끝납니다";

        private float quitArmedUntil;

        public event Action ResumeRequested;
        public event Action RespawnRequested;
        public event Action ToggleViewRequested;
        public event Action QuitRequested;

        /// <summary>"내 작업실" 타일을 눌렀다. 맵 목록 창을 연다.</summary>
        public event Action MapsRequested;

        public bool IsOpen => root != null && root.activeSelf;

        public int CurrentTab { get; private set; }

        public int TabCount => tabPages.Length;

        /// <summary>VR 조작에 맞춘 안내를 보이는 중인지.</summary>
        public bool IsVrMode { get; private set; }

        /// <summary>끝내기 단추를 한 번 눌러 확인을 기다리는 중인지.</summary>
        public bool IsQuitArmed => Time.unscaledTime < quitArmedUntil;

        private void Awake()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int index = i;
                if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => ShowTab(index));
            }

            if (resumeButton != null) resumeButton.onClick.AddListener(() => ResumeRequested?.Invoke());
            if (respawnButton != null) respawnButton.onClick.AddListener(() => RespawnRequested?.Invoke());
            if (respawnTile != null) respawnTile.onClick.AddListener(() => RespawnRequested?.Invoke());
            if (viewTile != null) viewTile.onClick.AddListener(() => ToggleViewRequested?.Invoke());
            if (mapsTile != null) mapsTile.onClick.AddListener(() => MapsRequested?.Invoke());
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitPressed);

            ShowTab(0);
            DisarmQuit();
        }

        private void Update()
        {
            if (quitArmedUntil > 0f && !IsQuitArmed) DisarmQuit();
        }

        public void Open()
        {
            ShowTab(0);
            DisarmQuit();
            if (root != null) root.SetActive(true);
        }

        public void Close()
        {
            DisarmQuit();
            if (root != null) root.SetActive(false);
        }

        public void ShowTab(int index)
        {
            if (index < 0 || index >= tabPages.Length) return;

            CurrentTab = index;
            for (int i = 0; i < tabPages.Length; i++)
            {
                if (tabPages[i] != null) tabPages[i].SetActive(i == index);
                if (i < tabMarkers.Length && tabMarkers[i] != null) tabMarkers[i].SetActive(i == index);
            }
        }

        /// <summary>바로가기 타일에 지금 누르면 바뀔 시점을 적는다.</summary>
        public void ShowViewMode(bool firstPerson)
        {
            if (viewTileTitle != null) viewTileTitle.text = firstPerson ? "3인칭으로 보기" : "1인칭으로 보기";
        }

        /// <summary>
        /// 조작 방식에 맞는 안내만 보인다. VR에서는 키 딱지와 키보드·마우스 안내를 감추고 VR 조작 안내를 보이며,
        /// 1인칭·3인칭을 바꾸는 타일은 누를 수 없게 한다(VR은 늘 1인칭이다).
        /// </summary>
        public void ShowControlMode(bool vr)
        {
            IsVrMode = vr;
            SetActive(desktopOnly, !vr);
            SetActive(vrOnly, vr);

            if (viewTile != null) viewTile.interactable = !vr;
            if (viewTileGroup != null) viewTileGroup.alpha = vr ? UnavailableAlpha : 1f;
        }

        private static void SetActive(GameObject[] items, bool active)
        {
            if (items == null) return;

            foreach (GameObject item in items)
            {
                if (item != null && item.activeSelf != active) item.SetActive(active);
            }
        }

        /// <summary>실수로 끝내지 않도록 두 번 눌러야 끝내기를 요청한다.</summary>
        private void OnQuitPressed()
        {
            if (IsQuitArmed)
            {
                DisarmQuit();
                QuitRequested?.Invoke();
                return;
            }

            quitArmedUntil = Time.unscaledTime + QuitConfirmSeconds;
            if (quitLabel != null) quitLabel.text = quitConfirmText;
        }

        private void DisarmQuit()
        {
            quitArmedUntil = 0f;
            if (quitLabel != null) quitLabel.text = quitText;
        }
    }
}
