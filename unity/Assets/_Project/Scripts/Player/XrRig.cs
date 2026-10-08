using AtelierVerse.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AtelierVerse.Player
{
    /// <summary>
    /// VR 기기의 추적을 캐릭터에 옮기는 리그. 머리(카메라)와 두 손을 추적 공간의 바닥 가운데(origin)를 기준으로 놓는다.
    /// 켜지면 PC용 카메라를 가져와 머리로 쓰고 몸을 숨기며, 꺼지면 카메라를 제자리로 돌려준다.
    /// 기기의 값은 입력 자산의 XR 묶음에서 읽으므로, 실제 기기와 테스트의 가상 기기를 같은 길로 받는다.
    /// 조작 스크립트가 머리 방향을 읽기 전에 자세를 먼저 옮기도록 다른 스크립트보다 일찍 실행한다.
    /// 오른손이 가리키는 쪽으로 나가는 광선(메뉴의 단추를 가리킬 때 보임)도 이 리그가 놓는다.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class XrRig : MonoBehaviour
    {
        /// <summary>기기가 아직 머리 위치를 주지 않을 때 쓰는 선 키의 눈높이.</summary>
        public const float DefaultHeadHeight = XrSession.StandingEyeHeight;

        /// <summary>가리킨 곳에 아무것도 없을 때의 광선 길이.</summary>
        public const float DefaultPointerLength = 2.5f;

        private const string MapName = "XR";

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private Transform origin;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private AvatarView avatar;
        [SerializeField] private Transform rightPointer;
        [SerializeField] private LineRenderer pointerLine;
        [SerializeField] private Transform pointerDot;

        private InputActionMap map;
        private InputAction headPosition;
        private InputAction headRotation;
        private InputAction leftPosition;
        private InputAction leftRotation;
        private InputAction rightPosition;
        private InputAction rightRotation;
        private InputAction pointerPosition;
        private InputAction pointerRotation;
        private bool pointerRequested;
        private float pointerLength = DefaultPointerLength;
        private Transform cameraHome;
        private Vector3 cameraHomePosition;
        private Quaternion cameraHomeRotation;
        private bool hasCamera;

        /// <summary>머리(카메라)의 트랜스폼. 리그가 켜져 있을 때만 추적을 따른다.</summary>
        public Transform Head => viewCamera != null ? viewCamera.transform : transform;

        public Transform Origin => origin;

        public Transform LeftHand => leftHand;

        public Transform RightHand => rightHand;

        /// <summary>오른손이 가리키는 자세. 앞쪽(+z)이 광선이 나가는 방향이다.</summary>
        public Transform RightPointer => rightPointer;

        /// <summary>광선이 지금 보이는지. 보이라고 했고 컨트롤러가 가리키는 자세를 주고 있을 때만 보인다.</summary>
        public bool PointerShown => rightPointer != null && rightPointer.gameObject.activeSelf;

        public float PointerLength => pointerLength;

        /// <summary>머리가 보는 수평 방향. 위나 아래를 똑바로 보고 있으면 몸의 앞쪽을 쓴다.</summary>
        public Vector3 HeadForwardOnPlane
        {
            get
            {
                Vector3 forward = Head.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.01f) forward = transform.forward;
                return forward.normalized;
            }
        }

        /// <summary>머리가 몸에서 수평으로 벗어난 만큼(월드 기준). 실제로 걸어 움직이면 생긴다.</summary>
        public Vector3 HeadOffsetOnPlane
        {
            get
            {
                Vector3 offset = Head.position - transform.position;
                offset.y = 0f;
                return offset;
            }
        }

        private void OnEnable()
        {
            if (actions == null || origin == null || viewCamera == null)
            {
                Debug.LogError("[Atelier Verse] XrRig에 입력 자산, 추적 기준점, 카메라 가운데 연결되지 않은 것이 있습니다.", this);
                enabled = false;
                return;
            }

            map = actions.FindActionMap(MapName, true);
            headPosition = map.FindAction("HeadPosition", true);
            headRotation = map.FindAction("HeadRotation", true);
            leftPosition = map.FindAction("LeftHandPosition", true);
            leftRotation = map.FindAction("LeftHandRotation", true);
            rightPosition = map.FindAction("RightHandPosition", true);
            rightRotation = map.FindAction("RightHandRotation", true);
            pointerPosition = map.FindAction("RightPointerPosition", true);
            pointerRotation = map.FindAction("RightPointerRotation", true);
            map.Enable();

            // 추적 기준이 바닥이 아닌 기기에서는 추적 공간을 눈높이만큼 올려 둔다.
            origin.localPosition = new Vector3(0f, XrSession.OriginHeight, 0f);
            TakeCamera();
            if (avatar != null) avatar.SetFirstPerson(true);
            SetPointerLength(DefaultPointerLength);
            ApplyPoses();
            Application.onBeforeRender += ApplyPoses;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= ApplyPoses;
            map?.Disable();

            if (leftHand != null) leftHand.gameObject.SetActive(false);
            if (rightHand != null) rightHand.gameObject.SetActive(false);
            if (rightPointer != null) rightPointer.gameObject.SetActive(false);
            if (origin != null) origin.localPosition = Vector3.zero;
            ReturnCamera();
        }

        private void Update()
        {
            ApplyPoses();
        }

        /// <summary>추적 공간을 옮긴다. 몸이 머리 밑으로 따라온 만큼 반대로 밀어, 머리가 보는 자리는 그대로 두는 데 쓴다.</summary>
        public void ShiftOrigin(Vector3 worldDelta)
        {
            if (origin != null) origin.position += worldDelta;
        }

        /// <summary>오른손의 광선을 보이거나 감춘다. 메뉴처럼 가리켜 누를 것이 있을 때만 보인다.</summary>
        public void ShowPointer(bool show)
        {
            pointerRequested = show;
            if (!show && rightPointer != null) rightPointer.gameObject.SetActive(false);
        }

        /// <summary>광선의 길이를 정한다. 가리킨 곳까지의 거리를 넣으면 그 자리에서 끝난다.</summary>
        public void SetPointerLength(float length)
        {
            pointerLength = Mathf.Max(0.05f, length);
            if (pointerLine != null) pointerLine.SetPosition(1, new Vector3(0f, 0f, pointerLength));
            if (pointerDot != null) pointerDot.localPosition = new Vector3(0f, 0f, pointerLength);
        }

        /// <summary>오른손이 가리키는 광선. 리그가 꺼져 있거나 컨트롤러가 가리키는 자세를 주지 않으면 false다.</summary>
        public bool TryGetPointerRay(out Ray ray)
        {
            ray = default;
            if (!isActiveAndEnabled || rightPointer == null || !PointerConnected()) return false;

            ray = new Ray(rightPointer.position, rightPointer.forward);
            return true;
        }

        /// <summary>기기의 자세를 머리와 두 손에 옮긴다. 매 프레임과 그리기 직전에 부른다.</summary>
        public void ApplyPoses()
        {
            if (!hasCamera) return;

            Vector3 head = headPosition.ReadValue<Vector3>();
            if (head == Vector3.zero && XrSession.OriginHeight <= 0f) head = new Vector3(0f, DefaultHeadHeight, 0f);
            viewCamera.transform.SetLocalPositionAndRotation(head, Valid(headRotation.ReadValue<Quaternion>()));

            ApplyHand(leftHand, leftPosition, leftRotation);
            ApplyHand(rightHand, rightPosition, rightRotation);
            ApplyPointer();
        }

        /// <summary>가리키는 자세는 광선이 보이지 않을 때에도 옮겨 두어, 조준 광선을 언제든 읽을 수 있게 한다.</summary>
        private void ApplyPointer()
        {
            if (rightPointer == null) return;

            bool connected = PointerConnected();
            bool show = connected && pointerRequested;
            if (rightPointer.gameObject.activeSelf != show) rightPointer.gameObject.SetActive(show);
            if (!connected) return;

            rightPointer.SetLocalPositionAndRotation(pointerPosition.ReadValue<Vector3>(), Valid(pointerRotation.ReadValue<Quaternion>()));
        }

        /// <summary>묶음이 켜져 있을 때만 묻는다(꺼진 묶음에 연결된 기기를 물으면 입력 시스템이 연결을 새로 계산한다).</summary>
        private bool PointerConnected()
        {
            return pointerPosition != null && pointerPosition.enabled && pointerPosition.controls.Count > 0;
        }

        /// <summary>손은 기기가 연결되어 있을 때만 보인다.</summary>
        private static void ApplyHand(Transform hand, InputAction position, InputAction rotation)
        {
            if (hand == null) return;

            // 묶음이 켜져 있을 때만 묻는다. 꺼진 묶음에 연결된 기기를 물으면 입력 시스템이 연결을 새로 계산해 상태를 만들어 둔다.
            bool connected = position.enabled && position.controls.Count > 0;
            if (hand.gameObject.activeSelf != connected) hand.gameObject.SetActive(connected);
            if (!connected) return;

            hand.SetLocalPositionAndRotation(position.ReadValue<Vector3>(), Valid(rotation.ReadValue<Quaternion>()));
        }

        /// <summary>기기가 아직 방향을 주지 않으면 값이 모두 0인 회전이 온다. 그때는 돌리지 않은 것으로 본다.</summary>
        private static Quaternion Valid(Quaternion rotation)
        {
            bool empty = rotation.x == 0f && rotation.y == 0f && rotation.z == 0f && rotation.w == 0f;
            return empty ? Quaternion.identity : rotation;
        }

        private void TakeCamera()
        {
            Transform cameraTransform = viewCamera.transform;
            cameraHome = cameraTransform.parent;
            cameraHomePosition = cameraTransform.localPosition;
            cameraHomeRotation = cameraTransform.localRotation;
            cameraTransform.SetParent(origin, false);
            hasCamera = true;
        }

        private void ReturnCamera()
        {
            if (!hasCamera) return;

            hasCamera = false;
            if (viewCamera == null) return;

            Transform cameraTransform = viewCamera.transform;
            cameraTransform.SetParent(cameraHome, false);
            cameraTransform.SetLocalPositionAndRotation(cameraHomePosition, cameraHomeRotation);
        }
    }
}
