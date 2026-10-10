using System;
using UnityEngine;

namespace AtelierVerse.Player
{
    /// <summary>
    /// 캐릭터의 몸을 움직이는 곳: 걷기, 달리기, 점프, 중력, 날기, 시작 위치로 돌아가기.
    /// 키보드나 마우스를 읽지 않고, 조작 스크립트가 정해 준 의도(가려는 방향, 달리기, 오르내리기)대로만 움직인다.
    /// PC 조작(DesktopPlayerController)뿐 아니라 VR 조작과 다른 사람의 캐릭터도 이 모터를 쓴다.
    /// 조작 스크립트가 같은 프레임에 의도를 먼저 정할 수 있도록 다른 스크립트보다 늦게 실행한다.
    /// 시작 위치는 씬에 놓인 처음 자리이며, 맵마다 따로 정한 자리로 바꿀 수 있다(18일차). 그 자리가 블록에 막혀 있으면 위쪽의 빈 자리에 선다.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMotor : MonoBehaviour
    {
        private const float FallLimit = -10f;

        // 시작 위치가 막혀 있을 때 위로 올려 보는 간격과 횟수(가장 높이 16까지), 바닥에 닿은 것을 막힌 것으로 보지 않게 띄우는 높이.
        private const float LiftStep = 0.25f;
        private const int MaxLiftSteps = 64;
        private const float FloorSkin = 0.06f;

        [SerializeField] private AvatarView avatar;
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float flySpeed = 7f;

        private CharacterController controller;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float verticalVelocity;
        private bool jumpRequested;

        // 자신의 마지막 걸음에서 바닥에 닿았는지. Shift처럼 다른 이동이 끼어도 점프와 중력의 판단이 흔들리지 않게 따로 기억한다.
        private bool grounded;

        /// <summary>날기를 켜거나 끌 때 알린다. 값이 true이면 날고 있다.</summary>
        public event Action<bool> FlyModeChanged;

        /// <summary>날고 있는지(만들기 시점). 시작할 때와 시작 위치로 돌아갈 때는 걷기다.</summary>
        public bool IsFlying { get; private set; }

        public bool IsGrounded => grounded;

        public float WalkSpeed => walkSpeed;

        public float SprintSpeed => sprintSpeed;

        /// <summary>날 때의 속도(m/s).</summary>
        public float FlySpeed => flySpeed;

        public AvatarView Avatar => avatar;

        /// <summary>시작 위치(발이 닿는 자리). 시작 위치로 돌아갈 때 이 자리에 선다.</summary>
        public Vector3 SpawnPosition => spawnPosition;

        /// <summary>시작할 때 바라보는 좌우 각도.</summary>
        public float SpawnYaw => spawnRotation.eulerAngles.y;

        /// <summary>시작 위치를 따로 정했는지. 정하지 않았으면 씬에 놓인 처음 자리다.</summary>
        public bool HasCustomSpawn { get; private set; }

        /// <summary>가려는 방향(월드 기준의 수평 방향, 길이 1 이하). 바꿀 때까지 그 방향으로 계속 간다.</summary>
        public Vector3 MoveDirection { get; set; }

        /// <summary>걸을 때 달리기 속도로 갈지.</summary>
        public bool Sprint { get; set; }

        /// <summary>날 때 위로 오를지.</summary>
        public bool Ascend { get; set; }

        /// <summary>날 때 아래로 내려올지.</summary>
        public bool Descend { get; set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            startPosition = transform.position;
            startRotation = transform.rotation;
            spawnPosition = startPosition;
            spawnRotation = startRotation;
        }

        private void Update()
        {
            if (IsFlying) Fly(Time.deltaTime);
            else Walk(Time.deltaTime);

            if (transform.position.y < FallLimit) Respawn();
        }

        /// <summary>다음 걸음에 뛰어오른다. 바닥에 닿아 있지 않거나 날고 있으면 아무 일도 하지 않는다.</summary>
        public void Jump()
        {
            jumpRequested = true;
        }

        /// <summary>의도를 모두 지운다. 걷고 있었으면 제자리에 서고, 날고 있었으면 그 자리에 머문다.</summary>
        public void Stop()
        {
            MoveDirection = Vector3.zero;
            Sprint = false;
            Ascend = false;
            Descend = false;
            jumpRequested = false;
        }

        /// <summary>몸이 바라보는 좌우 방향을 정한다.</summary>
        public void SetYaw(float yaw)
        {
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>몸을 좌우로 돌린다. 오른쪽이 양수다.</summary>
        public void Turn(float degrees)
        {
            transform.Rotate(0f, degrees, 0f, Space.World);
        }

        /// <summary>시작 위치를 정한다. position은 발이 닿는 자리, yaw는 바라보는 좌우 각도다. 몸은 다음에 시작 위치로 돌아갈 때 옮겨진다.</summary>
        public void SetSpawn(Vector3 position, float yaw)
        {
            spawnPosition = position;
            spawnRotation = Quaternion.Euler(0f, yaw, 0f);
            HasCustomSpawn = true;
        }

        /// <summary>시작 위치를 씬에 놓인 처음 자리로 되돌린다.</summary>
        public void ResetSpawn()
        {
            spawnPosition = startPosition;
            spawnRotation = startRotation;
            HasCustomSpawn = false;
        }

        /// <summary>
        /// 시작 위치로 되돌린다. 바닥 밖으로 떨어졌을 때, 메뉴의 단추, 다른 맵을 열었을 때 부른다. 날고 있었으면 걷기로 돌아온다.
        /// 시작 위치가 블록 같은 것에 막혀 있으면 그 위쪽의 빈 자리에 선다.
        /// </summary>
        public void Respawn()
        {
            if (controller == null) controller = GetComponent<CharacterController>();

            controller.enabled = false;
            transform.SetPositionAndRotation(FreeSpotAbove(spawnPosition), spawnRotation);
            verticalVelocity = 0f;
            grounded = false;
            controller.enabled = true;
            SetFlying(false);
        }

        /// <summary>
        /// feet에 섰을 때 몸이 무엇인가에 겹치면, 겹치지 않을 때까지 조금씩 위로 올린 자리를 돌려준다. 끝까지 막혀 있으면 feet 그대로다.
        /// 방금 놓인 블록도 보이도록 물리의 자리를 먼저 맞춘다. 자기 몸은 세지 않는다.
        /// </summary>
        private Vector3 FreeSpotAbove(Vector3 feet)
        {
            Physics.SyncTransforms();

            float radius = controller.radius * 0.95f;
            float half = Mathf.Max(0f, controller.height * 0.5f - controller.radius);
            int others = ~(1 << gameObject.layer);

            for (int i = 0; i <= MaxLiftSteps; i++)
            {
                Vector3 candidate = feet + Vector3.up * (LiftStep * i);
                Vector3 center = candidate + controller.center;
                Vector3 bottom = center - Vector3.up * half + Vector3.up * FloorSkin;
                Vector3 top = center + Vector3.up * half;
                if (!Physics.CheckCapsule(bottom, top, radius, others, QueryTriggerInteraction.Ignore)) return candidate;
            }

            return feet;
        }

        /// <summary>
        /// 의도와 상관없이 몸을 수평으로 조금 옮긴다. VR에서 실제로 걸어 머리가 몸에서 벗어났을 때 몸을 머리 밑으로 데려오는 데 쓴다.
        /// 벽이나 블록에 막히면 덜 옮겨지며, 실제로 옮겨진 만큼을 돌려준다.
        /// </summary>
        public Vector3 Shift(Vector3 worldDelta)
        {
            if (controller == null || !controller.enabled) return Vector3.zero;

            Vector3 before = transform.position;
            controller.Move(new Vector3(worldDelta.x, 0f, worldDelta.z));
            return transform.position - before;
        }

        /// <summary>날기를 켜거나 끈다. 끄면 그 자리에서 떨어지기 시작한다.</summary>
        public void SetFlying(bool flying)
        {
            if (IsFlying == flying) return;

            IsFlying = flying;
            verticalVelocity = 0f;
            FlyModeChanged?.Invoke(flying);
        }

        /// <summary>
        /// 날 때의 속도 벡터. planar는 바라보는 좌우 방향의 이동(길이 1 이하), up·down은 오르기와 내려오기다.
        /// 위아래와 앞뒤를 함께 누르면 대각선이 더 빠르지 않도록 길이를 1로 맞춘다.
        /// </summary>
        public static Vector3 ComposeFlyMove(Vector3 planar, bool up, bool down, float speed)
        {
            Vector3 direction = new Vector3(planar.x, (up ? 1f : 0f) - (down ? 1f : 0f), planar.z);
            if (direction.sqrMagnitude > 1f) direction.Normalize();
            return direction * speed;
        }

        private Vector3 PlanarDirection()
        {
            Vector3 direction = new Vector3(MoveDirection.x, 0f, MoveDirection.z);
            if (direction.sqrMagnitude > 1f) direction.Normalize();
            return direction;
        }

        private void Walk(float deltaTime)
        {
            bool jump = jumpRequested;
            jumpRequested = false;

            if (grounded)
            {
                verticalVelocity = -1f;
                if (jump) verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }

            verticalVelocity += gravity * deltaTime;
            Vector3 planar = PlanarDirection() * (Sprint ? sprintSpeed : walkSpeed);
            controller.Move((planar + Vector3.up * verticalVelocity) * deltaTime);
            grounded = controller.isGrounded;

            if (avatar != null) avatar.Animate(planar.magnitude, deltaTime);
        }

        /// <summary>날기. 중력 없이 움직이고 팔다리는 흔들지 않는다.</summary>
        private void Fly(float deltaTime)
        {
            jumpRequested = false;
            controller.Move(ComposeFlyMove(PlanarDirection(), Ascend, Descend, flySpeed) * deltaTime);
            grounded = controller.isGrounded;

            if (avatar != null) avatar.Animate(0f, deltaTime);
        }
    }
}
