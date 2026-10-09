using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// VR에서 왼손 위에 떠 있는 부품 판. 화가의 팔레트처럼 왼손에 들고 오른손 광선으로 가리켜 부품을 고른다.
    /// 부품 칸, 고른 부품의 이름, 블록 수, 걷기·날기 표시, 되돌리기와 다시 실행 단추, 맞추기 단계를 바꾸는 단추,
    /// 부품 고르는 창을 여는 단추가 있다.
    /// PC의 화면 아래에 있는 것들을 VR에서 볼 수 있게 한곳에 모은 것이며, 무엇을 보일지는 GameUi가 알려 준다.
    /// 판은 왼손 위에 떠서 늘 머리 쪽을 본다(컨트롤러마다 손의 각도가 달라도 읽을 수 있게).
    /// 이 스크립트는 늘 켜져 있는 바깥 오브젝트에 붙고, 보이고 감추는 것은 안쪽의 판이다.
    /// </summary>
    public class XrHandPalette : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private HotbarView hotbar;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text blockCountLabel;
        [SerializeField] private TMP_Text modeLabel;
        [SerializeField] private Image modeDot;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button redoButton;
        [SerializeField] private Button snapButton;
        [SerializeField] private TMP_Text snapLabel;
        [SerializeField] private Button partsButton;
        [SerializeField] private string emptyTitle = "부품을 고르세요";
        [SerializeField] private float metersPerPixel = 0.00045f;
        [SerializeField] private float lift = 0.14f;

        private Transform hand;
        private Transform head;
        private bool wanted;

        public event Action UndoRequested;
        public event Action RedoRequested;

        /// <summary>맞추기 단추를 눌렀다. 누를 때마다 다음 단계로 바꾼다.</summary>
        public event Action SnapRequested;

        /// <summary>부품 창 단추를 눌렀다. 부품 고르는 창을 연다.</summary>
        public event Action PartsRequested;

        /// <summary>판에 든 부품 칸. 화면 아래의 부품 칸과 선택 상태를 함께 쓰도록 GameUi가 잇는다.</summary>
        public HotbarView Hotbar => hotbar;

        /// <summary>판이 지금 보이는지.</summary>
        public bool IsShown => canvas != null && canvas.gameObject.activeSelf;

        /// <summary>왼손에 붙어 있는지. VR 조작일 때만 붙는다.</summary>
        public bool IsAttached => hand != null;

        public string Title => titleLabel != null ? titleLabel.text : string.Empty;

        public string BlockCountText => blockCountLabel != null ? blockCountLabel.text : string.Empty;

        public string ModeText => modeLabel != null ? modeLabel.text : string.Empty;

        /// <summary>맞추기 단추에 적힌 글자(지금 단계).</summary>
        public string SnapText => snapLabel != null ? snapLabel.text : string.Empty;

        /// <summary>왼손에서 판의 가운데까지의 높이.</summary>
        public float Lift => lift;

        private void Awake()
        {
            if (undoButton != null) undoButton.onClick.AddListener(() => UndoRequested?.Invoke());
            if (redoButton != null) redoButton.onClick.AddListener(() => RedoRequested?.Invoke());
            if (snapButton != null) snapButton.onClick.AddListener(() => SnapRequested?.Invoke());
            if (partsButton != null) partsButton.onClick.AddListener(() => PartsRequested?.Invoke());
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        /// <summary>판을 왼손에 붙인다. headCamera는 판이 바라볼 머리의 카메라이며 광선으로 누르는 계산에도 쓰인다.</summary>
        public void Attach(Transform leftHand, Camera headCamera)
        {
            hand = leftHand;
            head = headCamera != null ? headCamera.transform : null;

            if (canvas != null)
            {
                canvas.worldCamera = headCamera;
                canvas.transform.localScale = Vector3.one * metersPerPixel;
            }

            Apply();
        }

        /// <summary>판을 손에서 떼고 감춘다. PC 조작일 때의 상태다.</summary>
        public void Detach()
        {
            hand = null;
            head = null;
            if (canvas != null) canvas.worldCamera = null;
            Apply();
        }

        /// <summary>판을 보이고 싶은지 정한다. 실제로는 왼손에 붙어 있고 왼손 컨트롤러가 연결되어 있을 때만 보인다.</summary>
        public void SetWanted(bool value)
        {
            wanted = value;
            Apply();
        }

        /// <summary>고른 부품의 이름을 보인다. 고른 부품이 없으면 고르라는 안내를 보인다.</summary>
        public void ShowTitle(string partName)
        {
            if (titleLabel != null) titleLabel.text = string.IsNullOrEmpty(partName) ? emptyTitle : partName;
        }

        public void ShowBlockCount(string text)
        {
            if (blockCountLabel != null) blockCountLabel.text = text;
        }

        public void ShowMode(string text, Color dotColor)
        {
            if (modeLabel != null) modeLabel.text = text;
            if (modeDot != null) modeDot.color = dotColor;
        }

        /// <summary>맞추기 단추에 지금 단계를 적는다.</summary>
        public void ShowSnap(string text)
        {
            if (snapLabel != null) snapLabel.text = text;
        }

        /// <summary>
        /// 손 위의 판 자리. 손에서 lift만큼 위에 있고 머리 쪽을 본다(판의 앞면이 머리를 향함).
        /// 머리가 판과 같은 자리에 있으면 fallbackForward 쪽을 본다.
        /// </summary>
        public static void PoseAbove(Vector3 handPosition, Vector3 headPosition, float lift, Vector3 fallbackForward, out Vector3 position, out Quaternion rotation)
        {
            position = handPosition + Vector3.up * lift;

            Vector3 away = position - headPosition;
            if (away.sqrMagnitude < 0.0001f) away = fallbackForward.sqrMagnitude > 0.0001f ? fallbackForward : Vector3.forward;
            rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
        }

        private void Apply()
        {
            if (canvas == null) return;

            bool show = wanted && hand != null && head != null && hand.gameObject.activeInHierarchy;
            if (canvas.gameObject.activeSelf != show) canvas.gameObject.SetActive(show);
            if (!show) return;

            PoseAbove(hand.position, head.position, lift, head.forward, out Vector3 position, out Quaternion rotation);
            canvas.transform.SetPositionAndRotation(position, rotation);
        }
    }
}
