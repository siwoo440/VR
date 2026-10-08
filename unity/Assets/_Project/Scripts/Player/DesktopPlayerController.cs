using UnityEngine;
using UnityEngine.InputSystem;

namespace AtelierVerse.Player
{
    /// <summary>
    /// PC에서 키보드와 마우스로 걷고 둘러보는 1인칭 조작.
    /// 화면을 누르면 마우스가 잠기고 Esc로 풀린다. VR 조작은 별도 리그에서 처리한다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DesktopPlayerController : MonoBehaviour
    {
        private const string MapName = "Player";
        private const float FallLimit = -10f;

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float lookSensitivity = 0.12f;
        [SerializeField] private float pitchLimit = 80f;

        private CharacterController controller;
        private InputActionMap map;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private Vector3 startPosition;
        private float pitch;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            startPosition = transform.position;
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
            map.Enable();
        }

        private void OnDisable()
        {
            map?.Disable();
            SetCursorLocked(false);
        }

        private void Update()
        {
            UpdateCursorLock();
            if (Cursor.lockState == CursorLockMode.Locked) Look();
            Move();
            if (transform.position.y < FallLimit) ReturnToStart();
        }

        private void UpdateCursorLock()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorLocked(true);
            }
        }

        private void Look()
        {
            Vector2 delta = lookAction.ReadValue<Vector2>() * lookSensitivity;
            transform.Rotate(0f, delta.x, 0f, Space.World);
            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        private void Move()
        {
            Vector2 input = moveAction.ReadValue<Vector2>();
            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            if (controller.isGrounded)
            {
                verticalVelocity = -1f;
                if (jumpAction.WasPressedThisFrame()) verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }

            verticalVelocity += gravity * Time.deltaTime;
            float speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
            controller.Move((direction * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        /// <summary>바닥 밖으로 떨어지면 시작 위치로 되돌린다.</summary>
        private void ReturnToStart()
        {
            controller.enabled = false;
            transform.position = startPosition;
            verticalVelocity = 0f;
            controller.enabled = true;
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
