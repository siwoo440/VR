using AtelierVerse.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AtelierVerse.Player
{
    /// <summary>
    /// PC의 키보드와 마우스 조작. 입력을 읽어 몸(CharacterMotor)과 카메라(ViewRig)에 전하기만 하고, 직접 움직이지는 않는다.
    /// 시점은 화면을 눌러 마우스를 잡았을 때만 돌아가고, 휠로 1인칭과 3인칭을 오가며, V로 날기를 켜고 끈다.
    /// 날 때는 Space가 오르기, Shift가 내려오기가 된다. 메뉴가 열려 있는 동안에는 SetInputBlocked로 조작을 막는다.
    /// VR 조작은 같은 모터를 쓰는 별도의 스크립트에서 처리한다.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor), typeof(ViewRig))]
    public class DesktopPlayerController : MonoBehaviour
    {
        private const string MapName = "Player";

        [SerializeField] private InputActionAsset actions;

        private CharacterMotor motor;
        private ViewRig rig;
        private InputActionMap map;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction zoomAction;
        private InputAction flyAction;
        private float lookSensitivity;

        /// <summary>메뉴처럼 조작을 막아야 하는 화면이 열려 있는지.</summary>
        public bool InputBlocked { get; private set; }

        /// <summary>마우스를 잡고 시점을 돌리는 중인지.</summary>
        public bool LookCaptured { get; private set; }

        /// <summary>이 조작이 움직이는 몸. 다른 스크립트가 먼저 깨어나도 쓸 수 있도록 처음 쓸 때 찾는다.</summary>
        public CharacterMotor Motor => motor != null ? motor : motor = GetComponent<CharacterMotor>();

        /// <summary>이 조작이 돌리는 카메라 리그.</summary>
        public ViewRig Rig => rig != null ? rig : rig = GetComponent<ViewRig>();

        private void OnEnable()
        {
            if (actions == null)
            {
                Debug.LogError("[Atelier Verse] DesktopPlayerController에 입력 자산이 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }

            map = actions.FindActionMap(MapName, true);
            moveAction = map.FindAction("Move", true);
            lookAction = map.FindAction("Look", true);
            jumpAction = map.FindAction("Jump", true);
            sprintAction = map.FindAction("Sprint", true);
            zoomAction = map.FindAction("Zoom");
            flyAction = map.FindAction("Fly");
            map.Enable();

            GameSettings.Changed += ApplySettings;
            ApplySettings();
        }

        private void OnDisable()
        {
            GameSettings.Changed -= ApplySettings;
            map?.Disable();
            CaptureLook(false);
            if (Motor != null) Motor.Stop();
        }

        private void Update()
        {
            if (InputBlocked)
            {
                Motor.Stop();
                return;
            }

            TryCaptureLook();
            if (LookCaptured) Look();
            Zoom();
            if (flyAction != null && flyAction.WasPressedThisFrame()) Motor.SetFlying(!Motor.IsFlying);

            Vector2 input = moveAction.ReadValue<Vector2>();
            bool space = jumpAction.IsPressed();
            bool shift = sprintAction.IsPressed();

            Motor.MoveDirection = transform.right * input.x + transform.forward * input.y;
            Motor.Sprint = shift;
            Motor.Ascend = space;
            Motor.Descend = shift;
            if (jumpAction.WasPressedThisFrame()) Motor.Jump();
        }

        /// <summary>조작을 막거나 푼다. 막으면 잡고 있던 마우스도 놓고 몸을 멈춘다.</summary>
        public void SetInputBlocked(bool blocked)
        {
            InputBlocked = blocked;
            if (!blocked) return;

            CaptureLook(false);
            Motor.Stop();
        }

        /// <summary>마우스를 잡아 시점 조작을 시작하거나, 놓아서 화면의 단추를 누를 수 있게 한다.</summary>
        public void CaptureLook(bool captured)
        {
            LookCaptured = captured;
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }

        /// <summary>바라보는 방향을 정한다. yaw는 좌우 각도, lookPitch는 위아래 각도이며 아래쪽이 양수다.</summary>
        public void SetLook(float yaw, float lookPitch)
        {
            Motor.SetYaw(yaw);
            Rig.SetPitch(lookPitch);
        }

        private void ApplySettings()
        {
            lookSensitivity = GameSettings.LookSensitivity;
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
            Motor.Turn(delta.x);
            Rig.AddPitch(-delta.y);
        }

        private void Zoom()
        {
            if (zoomAction == null) return;

            float scroll = zoomAction.ReadValue<float>();
            if (Mathf.Abs(scroll) < 0.01f) return;

            Rig.Zoom(scroll > 0f);
        }
    }
}
