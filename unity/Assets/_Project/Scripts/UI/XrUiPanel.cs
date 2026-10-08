using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// VR에서 게임 화면을 눈앞에 떠 있는 판으로 바꾼다. PC에서는 화면에 겹쳐 그리는 원래 방식 그대로 둔다.
    /// 판은 두 가지로 놓인다. 메뉴가 열리면 그때의 눈앞에 놓여 그 자리에 머물고(가리켜 누르기 쉽게),
    /// 메뉴가 닫혀 있으면 머리를 따라와 알림이 늘 시야 위쪽에 보이게 한다.
    /// 판의 크기는 거리에 비례하므로, 벽에 막혀 가까이 놓여도 눈에 보이는 크기(각도)는 같다.
    /// 오른손 광선으로 누르는 일은 입력 시스템의 추적 기기용 광선(TrackedDeviceRaycaster)이 맡고, 이곳은 켜고 끄기만 한다.
    /// </summary>
    public class XrUiPanel : MonoBehaviour
    {
        /// <summary>화면을 만들 때의 기준 크기. 판의 크기도 이 값이다.</summary>
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;

        [SerializeField] private Canvas canvas;
        [SerializeField] private TrackedDeviceRaycaster trackedRaycaster;
        [SerializeField] private InputSystemUIInputModule inputModule;
        [SerializeField] private GameObject[] screenOnly;
        [SerializeField] private float metersPerPixel = 0.0013f;
        [SerializeField] private float menuDistance = 1.6f;
        [SerializeField] private float minMenuDistance = 0.7f;
        [SerializeField] private float wallMargin = 0.15f;
        [SerializeField] private float menuDrop = 0.12f;
        [SerializeField] private float followDistance = 1f;
        [SerializeField] private float followDrop = 0.4f;
        [SerializeField] private float followSpeed = 5f;

        private Transform head;
        private bool follow = true;

        /// <summary>눈앞의 판으로 떠 있는지. PC에서는 false다.</summary>
        public bool IsWorldSpace { get; private set; }

        /// <summary>판이 지금 놓인 거리(머리에서 판의 가운데까지의 수평 거리).</summary>
        public float Distance { get; private set; }

        public Canvas Canvas => canvas;

        /// <summary>메뉴를 놓는 기본 거리. 이 거리에서 화면의 1픽셀이 MetersPerPixel 미터가 된다.</summary>
        public float MenuDistance => menuDistance;

        public float FollowDistance => followDistance;

        public float MetersPerPixel => metersPerPixel;

        /// <summary>판이 머리를 따라올지. 켜는 순간 바로 눈앞으로 옮긴다.</summary>
        public bool Follow
        {
            get => follow;
            set
            {
                follow = value;
                if (follow) SnapToFollow();
            }
        }

        private void LateUpdate()
        {
            if (!IsWorldSpace || !follow || head == null) return;

            FollowTarget(out Vector3 position, out Quaternion rotation);
            float blend = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, position, blend),
                Quaternion.Slerp(transform.rotation, rotation, blend));
        }

        /// <summary>화면을 눈앞의 판으로 바꾼다. headCamera는 머리의 카메라, trackingOrigin은 컨트롤러의 자세가 기준으로 삼는 추적 공간이다.</summary>
        public void ShowInWorld(Camera headCamera, Transform trackingOrigin)
        {
            if (canvas == null || headCamera == null) return;

            head = headCamera.transform;
            IsWorldSpace = true;

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = headCamera;

            var rect = (RectTransform)canvas.transform;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);

            if (trackedRaycaster != null) trackedRaycaster.enabled = true;
            if (inputModule != null) inputModule.xrTrackingOrigin = trackingOrigin;
            SetScreenOnly(false);

            if (follow) SnapToFollow();
            else PlaceForMenu();
        }

        /// <summary>화면에 겹쳐 그리는 방식으로 돌아간다.</summary>
        public void ShowOnScreen()
        {
            head = null;
            IsWorldSpace = false;

            if (trackedRaycaster != null) trackedRaycaster.enabled = false;
            if (inputModule != null) inputModule.xrTrackingOrigin = null;

            if (canvas != null)
            {
                canvas.worldCamera = null;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            SetScreenOnly(true);
        }

        /// <summary>
        /// 메뉴를 열 때 판을 지금의 눈앞에 놓는다. 앞에 벽이 가까우면 벽 앞으로 당겨 놓는다.
        /// </summary>
        public void PlaceForMenu()
        {
            if (!IsWorldSpace || head == null) return;

            Vector3 forward = ForwardOnPlane(head.forward, transform.forward);
            float distance = menuDistance;
            if (Physics.Raycast(head.position, forward, out RaycastHit hit, menuDistance + wallMargin, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                distance = Mathf.Clamp(hit.distance - wallMargin, minMenuDistance, menuDistance);
            }

            PoseInFront(head.position, forward, forward, distance, DropAt(menuDrop, distance), out Vector3 position, out Quaternion rotation);
            Apply(position, rotation, distance);
        }

        /// <summary>오른손 광선이 화면의 무엇인가에 닿아 있으면 광선의 출발점에서 그곳까지의 거리를 준다.</summary>
        public bool TryGetPointerHit(out float distance)
        {
            distance = 0f;
            if (!IsWorldSpace || inputModule == null || inputModule.trackedDevicePosition == null) return false;

            // 꺼진 동작에 연결된 기기를 물으면 입력 시스템이 연결을 새로 계산하므로 켜져 있을 때만 묻는다.
            InputAction action = inputModule.trackedDevicePosition.action;
            if (action == null || !action.enabled || action.controls.Count == 0) return false;

            RaycastResult hit = inputModule.GetLastRaycastResult(action.controls[0].device.deviceId);
            if (!hit.isValid) return false;

            distance = hit.distance;
            return true;
        }

        /// <summary>
        /// 머리 앞의 판 자리. 머리가 보는 수평 방향으로 distance만큼 앞, 눈높이에서 drop만큼 아래이며 판은 머리 쪽을 본다.
        /// 머리가 위나 아래를 똑바로 보고 있으면 fallbackForward의 수평 방향을 쓴다.
        /// </summary>
        public static void PoseInFront(Vector3 headPosition, Vector3 headForward, Vector3 fallbackForward, float distance, float drop, out Vector3 position, out Quaternion rotation)
        {
            Vector3 forward = ForwardOnPlane(headForward, fallbackForward);
            position = headPosition + forward * distance + Vector3.down * drop;
            rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        /// <summary>수평 방향만 남긴다. 거의 수직이면 fallback의 수평 방향, 그것도 수직이면 앞(+z)이다.</summary>
        public static Vector3 ForwardOnPlane(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude >= 0.01f) return direction.normalized;

            fallback.y = 0f;
            return fallback.sqrMagnitude >= 0.01f ? fallback.normalized : Vector3.forward;
        }

        /// <summary>거리에 맞는 판의 배율. 기준 거리에서 1픽셀이 metersPerPixel 미터이고, 거리에 비례해 커지므로 보이는 크기가 같다.</summary>
        public static float ScaleAt(float metersPerPixel, float distance, float referenceDistance)
        {
            return referenceDistance > 0f ? metersPerPixel * distance / referenceDistance : metersPerPixel;
        }

        private void SnapToFollow()
        {
            if (!IsWorldSpace || head == null) return;

            FollowTarget(out Vector3 position, out Quaternion rotation);
            Apply(position, rotation, followDistance);
        }

        private void FollowTarget(out Vector3 position, out Quaternion rotation)
        {
            PoseInFront(head.position, head.forward, transform.forward, followDistance, DropAt(followDrop, followDistance), out position, out rotation);
        }

        /// <summary>내려 두는 높이도 판의 크기와 같은 비율로 줄인다.</summary>
        private float DropAt(float drop, float distance)
        {
            return menuDistance > 0f ? drop * distance / menuDistance : drop;
        }

        private void Apply(Vector3 position, Quaternion rotation, float distance)
        {
            Distance = distance;
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = Vector3.one * ScaleAt(metersPerPixel, distance, menuDistance);
        }

        private void SetScreenOnly(bool visible)
        {
            if (screenOnly == null) return;

            foreach (GameObject item in screenOnly)
            {
                if (item != null && item.activeSelf != visible) item.SetActive(visible);
            }
        }
    }
}
