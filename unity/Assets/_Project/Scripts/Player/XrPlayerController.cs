using UnityEngine;
using UnityEngine.InputSystem;

namespace AtelierVerse.Player
{
    /// <summary>
    /// VR 컨트롤러 조작. 입력을 읽어 몸(CharacterMotor)에 전하기만 하며, PC 조작(DesktopPlayerController)과 같은 몸을 쓴다.
    /// 왼쪽 스틱은 머리가 보는 쪽으로 걷기, 오른쪽 스틱 좌우는 끊어서 돌기, 오른손의 첫째 단추는 점프, 둘째 단추는 날기다.
    /// 날 때는 오른쪽 스틱의 위아래가 오르기와 내려오기가 된다. 왼쪽 스틱을 누르면 달린다.
    /// 실제로 걸어 머리가 몸에서 벗어나면 몸을 머리 밑으로 데려온다.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public class XrPlayerController : MonoBehaviour
    {
        private const string MapName = "XR";

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private XrRig rig;
        [SerializeField] private float snapTurnAngle = 45f;
        [SerializeField] private float turnPress = 0.7f;
        [SerializeField] private float turnRelease = 0.3f;
        [SerializeField] private float moveDeadzone = 0.15f;
        [SerializeField] private float verticalPress = 0.5f;
        [SerializeField] private float followThreshold = 0.01f;

        private CharacterMotor motor;
        private InputActionMap map;
        private InputAction moveAction;
        private InputAction turnAction;
        private InputAction jumpAction;
        private InputAction flyAction;
        private InputAction sprintAction;
        private bool turnHeld;

        /// <summary>메뉴처럼 조작을 막아야 하는 화면이 열려 있는지.</summary>
        public bool InputBlocked { get; private set; }

        /// <summary>한 번에 도는 각도.</summary>
        public float SnapTurnAngle => snapTurnAngle;

        public CharacterMotor Motor => motor != null ? motor : motor = GetComponent<CharacterMotor>();

        public XrRig Rig => rig;

        private void OnEnable()
        {
            if (actions == null || rig == null)
            {
                Debug.LogError("[Atelier Verse] XrPlayerController에 입력 자산 또는 리그가 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }

            map = actions.FindActionMap(MapName, true);
            moveAction = map.FindAction("Move", true);
            turnAction = map.FindAction("Turn", true);
            jumpAction = map.FindAction("Jump", true);
            flyAction = map.FindAction("Fly", true);
            sprintAction = map.FindAction("Sprint", true);
            map.Enable();
            turnHeld = false;
        }

        private void OnDisable()
        {
            if (Motor != null) Motor.Stop();
        }

        private void Update()
        {
            FollowHead();

            if (InputBlocked)
            {
                Motor.Stop();
                return;
            }

            Vector2 move = moveAction.ReadValue<Vector2>();
            if (move.magnitude < moveDeadzone) move = Vector2.zero;
            Vector2 turn = turnAction.ReadValue<Vector2>();

            SnapTurn(turn.x);
            if (flyAction.WasPressedThisFrame()) Motor.SetFlying(!Motor.IsFlying);

            Vector3 forward = rig.HeadForwardOnPlane;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Motor.MoveDirection = right * move.x + forward * move.y;
            Motor.Sprint = sprintAction.IsPressed();
            Motor.Ascend = turn.y > verticalPress;
            Motor.Descend = turn.y < -verticalPress;
            if (jumpAction.WasPressedThisFrame()) Motor.Jump();
        }

        /// <summary>조작을 막거나 푼다. 머리와 손의 추적은 막지 않는다.</summary>
        public void SetInputBlocked(bool blocked)
        {
            InputBlocked = blocked;
            if (blocked) Motor.Stop();
        }

        /// <summary>
        /// 스틱을 끝까지 밀 때마다 한 번씩 돈다. 민 채로 있으면 더 돌지 않고, 가운데로 돌아온 뒤 다시 밀어야 돈다.
        /// 부드럽게 도는 것보다 멀미가 덜하다.
        /// </summary>
        private void SnapTurn(float x)
        {
            if (turnHeld)
            {
                if (Mathf.Abs(x) < turnRelease) turnHeld = false;
                return;
            }

            if (Mathf.Abs(x) < turnPress) return;

            turnHeld = true;
            Motor.Turn(Mathf.Sign(x) * snapTurnAngle);
        }

        /// <summary>
        /// 실제로 걸어 머리가 몸에서 벗어났으면 몸을 머리 밑으로 옮기고, 추적 공간은 옮겨진 만큼 반대로 민다.
        /// 그래서 머리가 보는 자리는 그대로이고, 벽에 막히면 몸만 멈춘다.
        /// </summary>
        private void FollowHead()
        {
            Vector3 offset = rig.HeadOffsetOnPlane;
            if (offset.magnitude < followThreshold) return;

            Vector3 moved = Motor.Shift(offset);
            rig.ShiftOrigin(-moved);
        }
    }
}
