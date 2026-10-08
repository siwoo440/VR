using System;
using AtelierVerse.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AtelierVerse.Player
{
    /// <summary>
    /// PC에서 키보드와 마우스로 걷고 둘러보는 조작.
    /// 마우스 휠로 1인칭과 3인칭 사이를 오간다. 시점은 화면을 눌러 마우스를 잡았을 때만 돌아간다.
    /// 메뉴가 열려 있는 동안에는 SetInputBlocked로 조작을 막는다. VR 조작은 별도 리그에서 처리한다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DesktopPlayerController : MonoBehaviour
    {
        private const string MapName = "Player";
        private const float FallLimit = -10f;
        private const float FirstPersonLimit = 0.3f;
        private const float CameraRadius = 0.2f;

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private AvatarView avatar;
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float pitchLimit = 80f;
        [SerializeField] private float nearViewDistance = 2.5f;
        [SerializeField] private float farViewDistance = 8f;
        [SerializeField] private float zoomStep = 1f;
        [SerializeField] private float zoomSpeed = 14f;
        [SerializeField] private Vector2 shoulderOffset = new Vector2(0.55f, 0.3f);

        private CharacterController controller;
        private InputActionMap map;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction zoomAction;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private float lookSensitivity;
        private float pitch;
        private float verticalVelocity;
        private float targetDistance;
        private float currentDistance;
        private float lastThirdPersonDistance;

        /// <summary>1인칭과 3인칭이 바뀔 때 알린다. 값이 true이면 1인칭이다.</summary>
        public event Action<bool> ViewModeChanged;

        /// <summary>메뉴처럼 조작을 막아야 하는 화면이 열려 있는지.</summary>
        public bool InputBlocked { get; private set; }

        /// <summary>마우스를 잡고 시점을 돌리는 중인지.</summary>
        public bool LookCaptured { get; private set; }

        public bool IsFirstPerson => targetDistance <= 0f;

        /// <summary>캐릭터 머리에서 카메라까지의 목표 거리. 0이면 1인칭이다.</summary>
        public float ViewDistance => targetDistance;

        public AvatarView Avatar => avatar;

        public Camera ViewCamera => viewCamera;

        /// <summary>머리(카메라 기준점)의 위치. 손이 닿는 거리를 잴 때 쓴다.</summary>
        public Vector3 HeadPosition => cameraPivot != null ? cameraPivot.position : transform.position;

        private bool CanMoveCamera => viewCamera != null && viewCamera.transform != cameraPivot;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            startPosition = transform.position;
            startRotation = transform.rotation;
            lastThirdPersonDistance = nearViewDistance + zoomStep;
            if (viewCamera == null && cameraPivot != null) viewCamera = cameraPivot.GetComponentInChildren<Camera>();
        }

        private void OnEnable()
        {
            if (actions == null || cameraPivot == null)
            {
                Debug.LogError("[Atelier Verse] DesktopPlayerController에 입력 자산 또는 카메라 기준점이 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }

            map = actions.FindActionMap(MapName, true);
            moveAction = map.FindAction("Move", true);
            lookAction = map.FindAction("Look", true);
            jumpAction = map.FindAction("Jump", true);
            sprintAction = map.FindAction("Sprint", true);
            zoomAction = map.FindAction("Zoom");
            map.Enable();

            GameSettings.Changed += ApplySettings;
            ApplySettings();
        }

        private void OnDisable()
        {
            GameSettings.Changed -= ApplySettings;
            map?.Disable();
            CaptureLook(false);
        }

        private void Update()
        {
            if (!InputBlocked)
            {
                TryCaptureLook();
                if (LookCaptured) Look();
                Zoom();
            }

            Move();
            if (transform.position.y < FallLimit) Respawn();
        }

        private void LateUpdate()
        {
            UpdateCamera();
        }

        /// <summary>조작을 막거나 푼다. 막으면 잡고 있던 마우스도 놓는다.</summary>
        public void SetInputBlocked(bool blocked)
        {
            InputBlocked = blocked;
            if (blocked) CaptureLook(false);
        }

        /// <summary>마우스를 잡아 시점 조작을 시작하거나, 놓아서 화면의 단추를 누를 수 있게 한다.</summary>
        public void CaptureLook(bool captured)
        {
            LookCaptured = captured;
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }

        /// <summary>시작 위치로 되돌린다. 바닥 밖으로 떨어졌을 때와 메뉴의 단추가 부른다.</summary>
        public void Respawn()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(startPosition, startRotation);
            verticalVelocity = 0f;
            controller.enabled = true;
        }

        /// <summary>바라보는 방향을 정한다. yaw는 좌우 각도, lookPitch는 위아래 각도이며 아래쪽이 양수다.</summary>
        public void SetLook(float yaw, float lookPitch)
        {
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            pitch = Mathf.Clamp(lookPitch, -pitchLimit, pitchLimit);
            if (cameraPivot != null) cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        /// <summary>1인칭과 3인칭을 서로 바꾼다. 3인칭은 마지막에 쓰던 거리로 돌아간다.</summary>
        public void ToggleView()
        {
            SetViewDistance(IsFirstPerson ? lastThirdPersonDistance : 0f);
        }

        public void SetViewDistance(float distance)
        {
            if (!CanMoveCamera) return;

            bool wasFirstPerson = IsFirstPerson;
            targetDistance = distance <= 0f ? 0f : Mathf.Clamp(distance, nearViewDistance, farViewDistance);
            if (targetDistance > 0f) lastThirdPersonDistance = targetDistance;
            if (wasFirstPerson != IsFirstPerson) ViewModeChanged?.Invoke(IsFirstPerson);
        }

        /// <summary>
        /// 휠 한 칸을 굴렸을 때의 다음 시점 거리. 0은 1인칭이고, 1인칭에서 당기면 가까운 3인칭 거리로 넘어간다.
        /// </summary>
        public static float StepViewDistance(float current, bool zoomIn, float near, float far, float step)
        {
            if (zoomIn) return current <= near ? 0f : Mathf.Max(near, current - step);
            return current <= 0f ? near : Mathf.Min(far, current + step);
        }

        private void ApplySettings()
        {
            lookSensitivity = GameSettings.LookSensitivity;
            if (viewCamera != null) viewCamera.fieldOfView = GameSettings.FieldOfView;
        }

        private void TryCaptureLook()
        {
            if (LookCaptured) return;

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            CaptureLook(true);
        }

        private void Look()
        {
            Vector2 delta = lookAction.ReadValue<Vector2>() * lookSensitivity;
            transform.Rotate(0f, delta.x, 0f, Space.World);
            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        private void Zoom()
        {
            if (zoomAction == null) return;

            float scroll = zoomAction.ReadValue<float>();
            if (Mathf.Abs(scroll) < 0.01f) return;

            SetViewDistance(StepViewDistance(targetDistance, scroll > 0f, nearViewDistance, farViewDistance, zoomStep));
        }

        private void Move()
        {
            Vector2 input = InputBlocked ? Vector2.zero : moveAction.ReadValue<Vector2>();
            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            if (controller.isGrounded)
            {
                verticalVelocity = -1f;
                if (!InputBlocked && jumpAction.WasPressedThisFrame()) verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }

            verticalVelocity += gravity * Time.deltaTime;
            float speed = !InputBlocked && sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
            Vector3 planar = direction * speed;
            controller.Move((planar + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (avatar != null) avatar.Animate(planar.magnitude, Time.deltaTime);
        }

        /// <summary>카메라를 목표 거리로 옮긴다. 뒤에 벽이 있으면 벽 앞까지만 물러난다.</summary>
        private void UpdateCamera()
        {
            if (!CanMoveCamera) return;

            currentDistance = Mathf.MoveTowards(currentDistance, targetDistance, zoomSpeed * Time.deltaTime);

            float allowed = currentDistance;
            if (allowed > 0f
                && Physics.SphereCast(cameraPivot.position, CameraRadius, -cameraPivot.forward, out RaycastHit hit, allowed, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.collider != controller)
            {
                allowed = hit.distance;
            }

            // 3인칭에서는 화면 가운데의 조준점이 자기 머리에 가리지 않도록 어깨 너머로 비켜선다.
            float shoulder = nearViewDistance > 0f ? Mathf.Clamp01(allowed / nearViewDistance) : 0f;
            viewCamera.transform.localPosition = new Vector3(shoulderOffset.x * shoulder, shoulderOffset.y * shoulder, -allowed);
            if (avatar != null) avatar.SetFirstPerson(allowed < FirstPersonLimit);
        }
    }
}
