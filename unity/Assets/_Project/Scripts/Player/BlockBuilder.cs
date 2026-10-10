using System;
using System.Collections.Generic;
using AtelierVerse.Core;
using AtelierVerse.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AtelierVerse.Player
{
    /// <summary>블록 놓기에서 일어난 일 가운데 맵의 기록에 남지 않는 것들. 소리처럼 손에 오는 반응을 주는 데 쓴다.</summary>
    public enum BuildEvent
    {
        Rotated,
        Grabbed,
        Released,
        SnapChanged,
    }

    /// <summary>단추를 누른 채 끌어서 이어 하고 있는 일.</summary>
    public enum StrokeKind
    {
        None,
        Place,
        Remove,
        Paint,
    }

    /// <summary>놓을 수 없는 까닭. 알림 띠에 보여 준다.</summary>
    public enum BlockedReason
    {
        None,
        OutOfBounds,
        Occupied,
        Full,
        Overlap,
    }

    /// <summary>
    /// 고른 부품을 조준한 자리에 놓고, 놓인 블록을 지우거나 고른 부품으로 칠하거나 잡아서 옮긴다. 놓일 자리는 반투명 블록으로 미리 보여 준다.
    /// 블록은 칸에 맞추지 않고 가리킨 면 위의 바로 그 자리에 놓인다(14일차). 다른 블록과는 겹쳐도 된다.
    /// 놓기 전에 좌우로 15도씩 돌릴 수 있고, 맞추기 도우미를 켜면 모눈이나 가리킨 블록에 나란히 붙는 자리로 당겨진다(15일차).
    /// 부품은 모양과 크기가 다를 수 있다(16일차). 얹는 높이, 범위와 겹침 검사, 미리 보기가 부품의 크기와 모양을 따른다. 칠하기는 색만 바꾸고 모양은 그대로 둔다.
    /// 옮기기는 블록을 잡아(화면에서 잠시 감추고 미리 보기로 대신 보임) 새 자리를 가리켜 놓는 것이며, 한 번의 편집으로 기록된다.
    /// 놓기·지우기·칠하기는 단추를 누른 채 조준을 옮기면 이어서 된다(21일차). 한 번 끈 것은 기록의 한 묶음이 되어 되돌리기 한 번에 돌아온다.
    /// 끌어서 놓기는 첫 블록을 놓은 면 위에 첫 블록과 줄을 맞춰 놓는다(이미 블록이 있는 자리, 맵 밖, 캐릭터가 선 자리는 건너뜀).
    /// 끌어서 지우기는 처음 가리킨 면과 같은 면에 있는 블록만 지운다. 그러지 않으면 누르고 있는 동안 뒤의 블록까지 뚫고 들어간다.
    /// 부품을 고르고 조준하고 있을 때만(PC에서는 마우스를 잡았을 때) 동작하며, 캐릭터나 블록이 아닌 물체와 겹치는 자리에는 놓지 않는다.
    /// 놓기·지우기·칠하기·옮기기는 블록 세계의 기록 층(History)을 거쳐 되돌릴 수 있다. 놓지 못한 까닭은 알림으로 올린다.
    /// 조준 광선, 조작이 막혔는지, 어느 입력 묶음을 읽을지는 이 기기의 캐릭터(LocalPlayer)에게 물으므로 조작 방식을 직접 알지 않는다.
    /// 그래서 PC의 마우스와 VR의 컨트롤러가 같은 길로 블록을 놓는다.
    /// </summary>
    [RequireComponent(typeof(LocalPlayer))]
    public class BlockBuilder : MonoBehaviour
    {
        public const int NoPart = -1;
        public const string PaintTargetMessage = "칠할 블록을 가리키세요";
        public const string SamePartMessage = "이미 같은 색입니다";
        public const string GrabTargetMessage = "옮길 블록을 가리키세요";
        public const string GrabbedMessage = "블록을 잡았습니다 · 놓을 자리를 가리켜 놓으세요";
        public const string GrabCancelledMessage = "옮기기를 그만두었습니다";

        // 끌어서 지울 때 "같은 면"으로 보는 범위: 처음 가리킨 면에서 떨어진 거리와 면의 방향이 어긋난 정도.
        private const float StrokePlaneTolerance = 0.08f;
        private const float StrokeNormalAlign = 0.9f;

        /// <summary>
        /// 단추를 누른 뒤 이만큼(초)은 끌기로 보지 않는다. 짧게 누르면서 시점이 크게 움직여도 하나만 놓이고 하나만 지워진다.
        /// 이보다 오래 누르고 있으면 그때의 조준까지 한꺼번에 따라잡는다.
        /// </summary>
        public const float StrokeHoldDelay = 0.12f;

        private const float OverlapMargin = 0.98f;
        private const float RotatePress = 0.6f;
        private const float RotateRelease = 0.3f;
        private const float RotateRepeatDelay = 0.4f;
        private const float RotateRepeatInterval = 0.12f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Collider[] OverlapBuffer = new Collider[32];
        private static readonly List<Vector2Int> StrokePath = new List<Vector2Int>();

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private Renderer ghostPrefab;
        [SerializeField] private float reach = 6f;
        [SerializeField] private float ghostAlpha = 0.55f;
        [SerializeField] private float carriedAlpha = 0.8f;
        [SerializeField] private Color blockedColor = new Color(0.72f, 0.27f, 0.06f);

        private LocalPlayer player;
        private BlockWorld world;
        private Renderer ghost;
        private MeshFilter ghostMesh;
        private MaterialPropertyBlock ghostColor;
        private InputAction placeAction;
        private InputAction removeAction;
        private InputAction paintAction;
        private InputAction grabAction;
        private InputAction rotateAction;
        private InputAction snapAction;
        private bool wasActive;
        private bool hasRemoveTarget;
        private int targetBlockId;
        private int carriedBlockId = BlockMap.NoId;
        private int rotateHeld;
        private float nextRotateAt;

        // 조준 광선이 닿은 자리와 그 면의 방향. 끌기의 면을 정하는 데 쓴다.
        private Vector3 aimPoint;
        private Vector3 aimNormal = Vector3.up;

        // 끌기: 처음 가리킨 면(한 점과 방향)과, 끌어서 놓을 때 블록을 늘어놓는 줄(두 방향과 칸의 크기).
        private Vector3 strokeNormal;
        private Vector3 strokeFaceOrigin;
        private Vector3 strokeFirstCenter;
        private Vector3 strokeU;
        private Vector3 strokeV;
        private float strokePitchU;
        private float strokePitchV;
        private int strokePart;
        private Quaternion strokeRotation;
        private Vector2Int strokeCell;
        private bool strokeWarned;
        private float strokeStartedAt;

        /// <summary>돌린 각도, 맞추기 단계, 블록을 잡았는지가 바뀌면 알린다. 화면의 표시가 듣는다.</summary>
        public event Action StateChanged;

        /// <summary>돌렸거나, 블록을 잡았거나 내려놓았거나, 맞추기 단계를 바꿨을 때 알린다. 놓기·지우기·칠하기·옮기기는 기록 층(History)이 알린다.</summary>
        public event Action<BuildEvent> Happened;

        /// <summary>놓을 부품의 번호. 고른 부품이 없으면 NoPart다.</summary>
        public int SelectedPart { get; set; } = NoPart;

        /// <summary>놓거나 옮길 부품의 번호. 블록을 잡고 있으면 그 블록의 부품이고, 아니면 고른 부품이다.</summary>
        public int TargetPart => IsCarrying && world != null && world.TryGet(carriedBlockId, out BlockRecord carried) ? carried.Part : SelectedPart;

        /// <summary>조준한 곳에 놓일 자리가 있는지.</summary>
        public bool HasTarget { get; private set; }

        /// <summary>놓기를 누르면 블록이 놓일 자리(블록의 가운데).</summary>
        public Vector3 TargetPosition { get; private set; }

        /// <summary>놓일 블록의 방향. 좌우로만 돌린다.</summary>
        public Quaternion TargetRotation => PlacementMath.YawRotation(Yaw);

        /// <summary>놓을 블록을 좌우로 돌린 각도(도, 0 이상 360 미만). 블록을 잡으면 그 블록의 각도가 된다.</summary>
        public float Yaw { get; private set; }

        /// <summary>맞추기 도우미의 단계. 0은 끔이며 이 기기에 저장된다.</summary>
        public int SnapLevel => PlacementMath.ClampSnapLevel(GameSettings.SnapLevel);

        /// <summary>맞추는 간격. 꺼져 있으면 0이다.</summary>
        public float SnapStep => PlacementMath.SnapStepOf(SnapLevel);

        /// <summary>화면에 보이는 맞추기 단계의 이름.</summary>
        public string SnapText => PlacementMath.SnapLabelOf(SnapLevel);

        public bool CanPlaceAtTarget { get; private set; }

        /// <summary>조준한 자리에 놓을 수 없는 까닭. 놓을 수 있으면 None이다.</summary>
        public BlockedReason Blocked { get; private set; }

        /// <summary>조준한 곳에 지우거나 칠하거나 잡을 블록이 있는지.</summary>
        public bool HasBlockTarget => hasRemoveTarget;

        /// <summary>조준한 블록의 번호. HasBlockTarget일 때만 뜻이 있다.</summary>
        public int TargetBlockId => targetBlockId;

        /// <summary>옮기려고 블록을 잡고 있는지.</summary>
        public bool IsCarrying => carriedBlockId != BlockMap.NoId;

        /// <summary>잡고 있는 블록의 번호. 잡고 있지 않으면 BlockMap.NoId다.</summary>
        public int CarriedBlockId => carriedBlockId;

        /// <summary>단추를 누른 채 끌어서 이어 하고 있는 일. 끌고 있지 않으면 None이다.</summary>
        public StrokeKind Stroke { get; private set; }

        public bool IsStroking => Stroke != StrokeKind.None;

        /// <summary>이번 끌기에서 놓거나 지우거나 칠한 블록의 수.</summary>
        public int StrokeCount { get; private set; }

        /// <summary>조준 광선이 닿는 거리 안의 무엇인가에 닿았는지.</summary>
        public bool HasAimHit { get; private set; }

        /// <summary>조준 광선의 출발점에서 닿은 곳까지의 거리. HasAimHit일 때만 뜻이 있다. VR의 손 광선이 이 자리에서 끝난다.</summary>
        public float AimDistance { get; private set; }

        public BlockWorld World => world;

        private bool IsActive => world != null
            && world.Catalog != null
            && world.Catalog.IsValid(SelectedPart)
            && player.IsAiming
            && !player.InputBlocked;

        private void Awake()
        {
            player = GetComponent<LocalPlayer>();
            world = FindAnyObjectByType<BlockWorld>();
            ghostColor = new MaterialPropertyBlock();

            if (ghostPrefab != null)
            {
                ghost = Instantiate(ghostPrefab);
                ghost.name = "BlockGhost";
                ghostMesh = ghost.GetComponent<MeshFilter>();
                ghost.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (actions == null)
            {
                Debug.LogError("[Atelier Verse] BlockBuilder에 입력 자산이 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }

            player.ModeChanged += OnModeChanged;
            ResolveActions();
        }

        private void OnDisable()
        {
            if (player != null) player.ModeChanged -= OnModeChanged;

            EndStroke();
            ReleaseCarried();
            wasActive = false;
            HasAimHit = false;
            if (ghost != null) ghost.gameObject.SetActive(false);
        }

        /// <summary>
        /// 지금 조작의 입력 묶음에서 만들기 동작을 찾는다. 조작 방식이 바뀌면 다시 찾는다.
        /// 바뀐 바로 그 프레임의 누름으로 블록이 놓이지 않게 한 프레임을 다시 기다린다.
        /// 맞추기 단계를 바꾸는 키는 PC에만 있고, VR에서는 부품 판의 단추로 바꾼다.
        /// </summary>
        private void ResolveActions()
        {
            InputActionMap map = actions.FindActionMap(player.InputMapName, true);
            placeAction = map.FindAction("Place", true);
            removeAction = map.FindAction("Remove", true);
            paintAction = map.FindAction("Paint", true);
            grabAction = map.FindAction("Grab", true);
            rotateAction = map.FindAction("Rotate", true);
            snapAction = map.FindAction("Snap");
            map.Enable();
            wasActive = false;
            rotateHeld = 0;
        }

        private void OnModeChanged(ControlMode mode)
        {
            ResolveActions();
        }

        private void OnDestroy()
        {
            if (ghost != null) Destroy(ghost.gameObject);
        }

        private void Update()
        {
            // 마우스를 잡는 바로 그 누름이나 부품을 고른 바로 그 프레임에는 놓지 않도록 한 프레임을 기다린다.
            bool active = IsActive;
            bool ready = active && wasActive;
            wasActive = active;

            // 메뉴를 열거나 부품 선택을 풀면, 또는 잡은 블록이 되돌리기로 사라지면 잡은 것을 놓아 준다.
            if (IsCarrying && (!active || !world.Has(carriedBlockId))) ReleaseCarried();

            if (ready)
            {
                int steps = ReadRotateSteps();
                if (steps != 0) RotateBy(steps);
                if (snapAction != null && snapAction.WasPressedThisFrame()) CycleSnap();
            }
            else
            {
                rotateHeld = 0;
            }

            UpdateTarget(active);

            // 메뉴를 열거나 부품 선택을 풀면 끌기도 끝난다. 그때까지 한 것은 한 묶음으로 남는다.
            if (IsStroking && !ready) EndStroke();
            if (Stroke == StrokeKind.Place) UpdateStrokeTarget();

            ShowGhost();
            if (!ready) return;

            if (IsCarrying)
            {
                if (placeAction.WasPressedThisFrame()) DropCarried();
                else if (grabAction.WasPressedThisFrame() || removeAction.WasPressedThisFrame()) CancelCarry();
                return;
            }

            if (IsStroking)
            {
                ContinueStroke();
                return;
            }

            if (placeAction.WasPressedThisFrame()) BeginStroke(StrokeKind.Place);
            else if (removeAction.WasPressedThisFrame()) BeginStroke(StrokeKind.Remove);
            else if (paintAction.WasPressedThisFrame()) BeginStroke(StrokeKind.Paint);
            else if (grabAction.WasPressedThisFrame()) GrabAtTarget();
        }

        /// <summary>
        /// 단추를 누른 그 프레임의 일을 하고(하나 놓기, 지우기, 칠하기), 이어서 끌 수 있게 끌기를 시작한다.
        /// 기록의 묶음을 먼저 열어 첫 블록도 같은 묶음에 들게 한다. 놓기와 지우기는 첫 일이 이루어져야 끌기가 된다.
        /// 칠하기는 빈 곳에서 눌러도 끌기가 되어, 누른 채 블록 위로 가져가면 칠해진다.
        /// </summary>
        private void BeginStroke(StrokeKind kind)
        {
            world.History.BeginGroup();

            bool done;
            switch (kind)
            {
                case StrokeKind.Place: done = PlaceAtTarget(); break;
                case StrokeKind.Remove: done = RemoveAtTarget(); break;
                default: done = PaintAtTarget(); break;
            }

            if (!done && kind != StrokeKind.Paint)
            {
                world.History.EndGroup();
                return;
            }

            Stroke = kind;
            StrokeCount = done ? 1 : 0;
            strokeWarned = false;
            strokeStartedAt = Time.unscaledTime;
            strokeNormal = aimNormal;
            strokeFaceOrigin = aimPoint;
            if (kind == StrokeKind.Place) SetUpStrokeLattice();
            StateChanged?.Invoke();
        }

        /// <summary>
        /// 끌어서 놓을 줄을 정한다. 첫 블록이 줄의 처음(칸 0, 0)이고, 줄은 처음 가리킨 면 위에 블록의 가로세로를 따라 놓인다.
        /// 부품과 돌린 각도는 끌기를 시작할 때의 것으로 굳힌다. 끄는 도중에 부품을 바꾸거나 돌려도 줄은 그대로다.
        /// </summary>
        private void SetUpStrokeLattice()
        {
            strokePart = SelectedPart;
            strokeRotation = TargetRotation;
            strokeFirstCenter = TargetPosition;
            strokeCell = Vector2Int.zero;

            Vector3 half = world.HalfSizeOf(strokePart);
            PlacementMath.FaceAxes(strokeNormal, strokeRotation, out strokeU, out strokeV);
            strokePitchU = PlacementMath.Extent(half, strokeRotation, strokeU);
            strokePitchV = PlacementMath.Extent(half, strokeRotation, strokeV);

            // 줄의 처음은 첫 블록 바로 아래의 면 위다. 맞추기 도우미가 블록을 당겨 놓았어도 줄은 놓인 블록에 맞는다.
            strokeFaceOrigin = strokeFirstCenter - strokeNormal * Vector3.Dot(strokeFirstCenter - aimPoint, strokeNormal);
        }

        private void ContinueStroke()
        {
            InputAction held = Stroke == StrokeKind.Place ? placeAction : Stroke == StrokeKind.Remove ? removeAction : paintAction;

            // 끄는 도중에 되돌리기를 눌러 묶음이 닫혔으면 끌기를 끝낸다. 한 것이 방금 되돌려졌으므로 몇 개를 했다고 알리지 않는다.
            if (!world.History.IsGrouping)
            {
                EndStroke(false);
                return;
            }

            if (!held.IsPressed())
            {
                EndStroke();
                return;
            }

            if (Time.unscaledTime - strokeStartedAt < StrokeHoldDelay) return;

            switch (Stroke)
            {
                case StrokeKind.Place: ContinuePlace(); break;
                case StrokeKind.Remove: ContinueRemove(); break;
                case StrokeKind.Paint: ContinuePaint(); break;
            }
        }

        /// <summary>조준이 줄의 다른 칸으로 옮겨 갔으면, 지난 칸에서 그 칸까지 지나온 칸에 블록을 놓는다.</summary>
        private void ContinuePlace()
        {
            if (!TryGetStrokeCell(out Vector2Int cell) || cell == strokeCell) return;

            PlacementMath.CellsBetween(strokeCell, cell, StrokePath);
            foreach (Vector2Int step in StrokePath)
            {
                PlaceStrokeCell(step);
            }

            strokeCell = cell;
        }

        /// <summary>
        /// 줄의 한 칸에 블록을 놓는다. 이미 블록이 있는 자리(이번에 놓은 것 포함), 맵 밖, 캐릭터나 다른 물체와 겹치는 자리는 건너뛴다.
        /// 맵 밖이거나 블록 수가 다 찼을 때는 끌기 한 번에 한 번만 알린다.
        /// </summary>
        private void PlaceStrokeCell(Vector2Int cell)
        {
            Vector3 half = world.HalfSizeOf(strokePart);
            Vector3 position = StrokeCenter(cell, half);

            PlaceResult check = world.CheckPlace(strokePart, position);
            if (check == PlaceResult.OutOfBounds || check == PlaceResult.Full)
            {
                if (!strokeWarned) Notice.Post(Describe(ReasonOf(check, false)), NoticeKind.Warning);
                strokeWarned = true;
                return;
            }

            if (check != PlaceResult.Ok || OverlapsAnything(position, strokeRotation, half)) return;
            if (world.History.Place(strokePart, position, strokeRotation, out _) == PlaceResult.Ok) StrokeCount++;
        }

        /// <summary>처음 가리킨 면과 같은 면에 있는 블록을 지운다. 지운 블록 뒤에서 드러난 블록은 면이 달라 지워지지 않는다.</summary>
        private void ContinueRemove()
        {
            if (!hasRemoveTarget) return;
            if (Mathf.Abs(Vector3.Dot(aimPoint - strokeFaceOrigin, strokeNormal)) > StrokePlaneTolerance) return;
            if (Vector3.Dot(aimNormal, strokeNormal) < StrokeNormalAlign) return;

            if (world.History.Remove(targetBlockId)) StrokeCount++;
        }

        /// <summary>지나가는 블록을 고른 부품의 색으로 칠한다. 이미 그 색인 블록은 건드리지 않는다. 끄는 동안에는 알림을 올리지 않는다.</summary>
        private void ContinuePaint()
        {
            if (!hasRemoveTarget || !world.TryGet(targetBlockId, out BlockRecord current)) return;

            int painted = world.Catalog.Repaint(current.Part, SelectedPart);
            if (painted == current.Part) return;

            if (world.History.Replace(targetBlockId, painted)) StrokeCount++;
        }

        /// <summary>끌기를 끝내고 기록의 묶음을 닫는다. 둘 이상을 했으면 몇 개를 했는지와 한 번에 되돌릴 수 있음을 알린다.</summary>
        private void EndStroke(bool announce = true)
        {
            if (!IsStroking) return;

            StrokeKind ended = Stroke;
            int count = StrokeCount;
            Stroke = StrokeKind.None;
            StrokeCount = 0;
            if (world != null) world.History.EndGroup();

            if (announce && count >= 2) Notice.Post(StrokeMessage(ended, count));
            StateChanged?.Invoke();
        }

        /// <summary>끌기를 끝냈을 때의 알림 문구.</summary>
        public static string StrokeMessage(StrokeKind kind, int count)
        {
            string done = kind == StrokeKind.Place ? "놓았습니다" : kind == StrokeKind.Remove ? "지웠습니다" : "칠했습니다";
            return $"블록 {count}개를 이어 {done} · 되돌리기 한 번에 돌아옵니다";
        }

        /// <summary>
        /// 조준 광선이 끌기의 면과 만나는 곳이 줄의 몇 번째 칸인지. 면을 벗어났거나 손이 닿지 않는 거리면 false다.
        /// 사이를 가리는 블록은 보지 않는다. 방금 놓은 블록이 조준을 가려 줄이 끊기는 일이 없게 하기 위해서다.
        /// </summary>
        private bool TryGetStrokeCell(out Vector2Int cell)
        {
            cell = strokeCell;
            if (!player.TryGetAim(out Ray aim, out float extraReach)) return false;

            var plane = new Plane(strokeNormal, strokeFaceOrigin);
            if (!plane.Raycast(aim, out float distance) || distance > reach + extraReach) return false;

            cell = PlacementMath.LatticeCell(aim.GetPoint(distance), strokeFaceOrigin, strokeU, strokeV, strokePitchU, strokePitchV);
            return true;
        }

        private Vector3 StrokeCenter(Vector2Int cell, Vector3 half)
        {
            Vector3 center = PlacementMath.LatticeCenter(strokeFirstCenter, cell, strokeU, strokeV, strokePitchU, strokePitchV);
            return world.ClampHeight(BlockMap.Quantize(center), half.y);
        }

        /// <summary>끌어서 놓는 동안의 미리 보기: 조준이 있는 칸에 다음 블록이 놓일 자리를 보인다. 이미 블록이 놓인 칸에서는 감춘다.</summary>
        private void UpdateStrokeTarget()
        {
            HasTarget = false;
            CanPlaceAtTarget = false;
            Blocked = BlockedReason.None;
            hasRemoveTarget = false;
            if (!TryGetStrokeCell(out Vector2Int cell)) return;

            Vector3 half = world.HalfSizeOf(strokePart);
            Vector3 position = StrokeCenter(cell, half);
            PlaceResult check = world.CheckPlace(strokePart, position);
            if (check == PlaceResult.Occupied) return;

            TargetPosition = position;
            HasTarget = true;
            Blocked = ReasonOf(check, check == PlaceResult.Ok && OverlapsAnything(position, strokeRotation, half));
            CanPlaceAtTarget = Blocked == BlockedReason.None;
        }

        /// <summary>놓을 블록을 steps 단계(한 단계 15도)만큼 돌린다. 양수는 위에서 보아 시계 방향이다.</summary>
        public void RotateBy(int steps)
        {
            if (steps == 0) return;

            Yaw = PlacementMath.StepYaw(Yaw, steps);
            StateChanged?.Invoke();
            Happened?.Invoke(BuildEvent.Rotated);
        }

        /// <summary>맞추기 도우미를 다음 단계로 바꾸고 알린다(끔 → 1칸 → 1/2칸 → 1/4칸 → 끔).</summary>
        public void CycleSnap()
        {
            SetSnapLevel(PlacementMath.NextSnapLevel(SnapLevel));
            Notice.Post(SnapMessage(SnapLevel));
        }

        public void SetSnapLevel(int level)
        {
            level = PlacementMath.ClampSnapLevel(level);
            if (level == SnapLevel) return;

            GameSettings.SnapLevel = level;
            StateChanged?.Invoke();
            Happened?.Invoke(BuildEvent.SnapChanged);
        }

        /// <summary>맞추기 단계를 바꿨을 때의 알림 문구.</summary>
        public static string SnapMessage(int level)
        {
            return level == 0
                ? "맞추기 끔 · 가리킨 그 자리에 놓입니다"
                : $"맞추기 {PlacementMath.SnapLabelOf(level)} · 모눈과 가리킨 블록에 맞춰 놓입니다";
        }

        /// <summary>잡고 있던 블록을 제자리에 돌려놓고 알린다. 잡고 있지 않으면 아무 일도 하지 않는다.</summary>
        public void CancelCarry()
        {
            if (!IsCarrying) return;

            ReleaseCarried();
            Notice.Post(GrabCancelledMessage);
            Happened?.Invoke(BuildEvent.Released);
        }

        /// <summary>
        /// 돌리기 입력을 단계 수로 바꾼다. 누를 때 한 번, 누르고 있으면 잠시 뒤부터 이어서 돈다.
        /// VR의 단추와 스틱처럼 값이 흔들리는 입력에서도 한 번만 돌도록, 누른 값과 뗀 값을 다르게 본다.
        /// </summary>
        private int ReadRotateSteps()
        {
            float value = rotateAction.ReadValue<float>();
            float size = Mathf.Abs(value);

            if (size < RotateRelease)
            {
                rotateHeld = 0;
                return 0;
            }

            if (size < RotatePress) return 0;

            int direction = value > 0f ? 1 : -1;
            float now = Time.time;
            if (direction != rotateHeld)
            {
                rotateHeld = direction;
                nextRotateAt = now + RotateRepeatDelay;
                return direction;
            }

            if (now < nextRotateAt) return 0;

            nextRotateAt = now + RotateRepeatInterval;
            return direction;
        }

        private bool PlaceAtTarget()
        {
            if (!HasTarget) return false;

            if (!CanPlaceAtTarget)
            {
                Notice.Post(Describe(Blocked), NoticeKind.Warning);
                return false;
            }

            return world.History.Place(SelectedPart, TargetPosition, TargetRotation, out _) == PlaceResult.Ok;
        }

        private bool RemoveAtTarget()
        {
            return hasRemoveTarget && world.History.Remove(targetBlockId);
        }

        /// <summary>조준한 블록을 고른 부품의 색으로 칠한다. 모양은 그대로 둔다. 블록이 없거나 이미 같은 색이면 알림만 올린다.</summary>
        private bool PaintAtTarget()
        {
            if (!hasRemoveTarget)
            {
                Notice.Post(PaintTargetMessage);
                return false;
            }

            if (!world.TryGet(targetBlockId, out BlockRecord current)) return false;

            int painted = world.Catalog.Repaint(current.Part, SelectedPart);
            if (painted == current.Part)
            {
                Notice.Post(SamePartMessage);
                return false;
            }

            return world.History.Replace(targetBlockId, painted);
        }

        /// <summary>
        /// 조준한 블록을 잡는다. 블록은 화면에서 잠시 감춰지고 미리 보기가 그 블록의 색과 방향으로 대신 보인다.
        /// 기록은 놓을 때까지 바뀌지 않는다. 지금은 좌우로 돈 각도만 다루므로, 기울어진 블록을 옮기면 똑바로 선다.
        /// </summary>
        private bool GrabAtTarget()
        {
            if (!hasRemoveTarget || !world.TryGet(targetBlockId, out BlockRecord record))
            {
                Notice.Post(GrabTargetMessage);
                return false;
            }

            carriedBlockId = record.Id;
            Yaw = PlacementMath.YawOf(record.Rotation);
            world.SetShown(record.Id, false);
            hasRemoveTarget = false;
            Notice.Post(GrabbedMessage);
            StateChanged?.Invoke();
            Happened?.Invoke(BuildEvent.Grabbed);
            return true;
        }

        /// <summary>잡은 블록을 가리킨 자리에 놓는다. 놓을 수 없는 자리면 알리고 계속 잡고 있는다. 제자리에 그대로 놓으면 기록하지 않는다.</summary>
        private bool DropCarried()
        {
            if (!HasTarget) return false;

            if (!CanPlaceAtTarget)
            {
                Notice.Post(Describe(Blocked), NoticeKind.Warning);
                return false;
            }

            int id = carriedBlockId;
            bool moved = world.History.Move(id, TargetPosition, TargetRotation);
            ReleaseCarried();
            // 제자리에 그대로 내려놓았으면 옮긴 것이 아니므로, 내려놓았다는 것만 알린다.
            if (!moved) Happened?.Invoke(BuildEvent.Released);
            return moved;
        }

        /// <summary>잡은 것을 놓아 주고 화면의 블록을 다시 보인다. 기록은 건드리지 않는다.</summary>
        private void ReleaseCarried()
        {
            if (!IsCarrying) return;

            int id = carriedBlockId;
            carriedBlockId = BlockMap.NoId;
            if (world != null) world.SetShown(id, true);
            StateChanged?.Invoke();
        }

        private string Describe(BlockedReason reason)
        {
            switch (reason)
            {
                case BlockedReason.OutOfBounds: return "맵 바깥에는 놓을 수 없습니다";
                case BlockedReason.Occupied: return "이미 같은 자리에 블록이 있습니다";
                case BlockedReason.Full: return $"블록이 {world.MaxBlocks}개에 닿아 더 놓을 수 없습니다";
                case BlockedReason.Overlap: return "캐릭터나 다른 물체와 겹쳐 놓을 수 없습니다";
                default: return "여기에는 놓을 수 없습니다";
            }
        }

        /// <summary>
        /// 조준 광선이 가리키는 곳을 찾는다. 닿은 면 위의 그 자리에 블록을 얹은 곳이 놓일 자리이고, 닿은 것이 놓인 블록이면 그 블록이 지우거나 칠하거나 잡을 대상이다.
        /// 맞추기 도우미가 켜져 있으면 모눈이나 가리킨 블록에 맞는 자리로 당긴다. 바닥에 묻히는 자리는 바닥 위로 올린다.
        /// </summary>
        private void UpdateTarget(bool active)
        {
            HasTarget = false;
            CanPlaceAtTarget = false;
            Blocked = BlockedReason.None;
            hasRemoveTarget = false;
            HasAimHit = false;
            if (!active || !player.TryGetAim(out Ray aim, out float extraReach)) return;

            if (!Physics.Raycast(aim, out RaycastHit hit, reach + extraReach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;

            HasAimHit = true;
            AimDistance = hit.distance;
            aimPoint = hit.point;
            aimNormal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;

            bool aimedAtBlock = hit.collider.TryGetComponent(out PlacedBlock block) && block.Id != BlockMap.NoId;
            BlockRecord aimed = default;
            if (aimedAtBlock) aimedAtBlock = world.TryGet(block.Id, out aimed);

            int part = TargetPart;
            Vector3 half = world.HalfSizeOf(part);
            TargetPosition = world.ClampHeight(BlockMap.Quantize(RestPosition(hit, aimedAtBlock, aimed, half)), half.y);
            HasTarget = true;

            PlaceResult check = IsCarrying ? world.CheckMove(carriedBlockId, TargetPosition) : world.CheckPlace(part, TargetPosition);
            bool overlap = check == PlaceResult.Ok && OverlapsOther(TargetPosition, TargetRotation, half);
            Blocked = ReasonOf(check, overlap);
            CanPlaceAtTarget = Blocked == BlockedReason.None;

            if (aimedAtBlock)
            {
                hasRemoveTarget = true;
                targetBlockId = aimed.Id;
            }
        }

        /// <summary>
        /// 가리킨 면 위에 부품을 얹은 자리. half는 놓을 부품의 반 크기다. 면을 파고들지 않고 닿는 만큼 띄운다.
        /// 맞추기 도우미가 켜져 있으면 놓인 블록에는 그 블록을 기준으로, 그 밖에는 모눈에 맞춘다.
        /// </summary>
        private Vector3 RestPosition(RaycastHit hit, bool aimedAtBlock, BlockRecord aimed, Vector3 half)
        {
            float step = SnapStep;
            if (step <= 0f) return PlacementMath.RestOn(hit.point, hit.normal, half, TargetRotation);
            if (aimedAtBlock) return PlacementMath.SnapToBlock(aimed.Position, aimed.Rotation, world.HalfSizeOf(aimed.Part), hit.point, hit.normal, step, half);
            return PlacementMath.SnapToGrid(hit.point, hit.normal, step, PlacementMath.RestOffset(half, TargetRotation, hit.normal));
        }

        /// <summary>
        /// 이 자리에 부품을 놓으면 캐릭터나 블록이 아닌 물체와 겹치는지. 놓인 블록끼리는 겹쳐도 되므로 세지 않는다.
        /// 부품을 둘러싸는 상자로 본다(경사와 계단도 상자로 본다). 면이 맞닿기만 한 것은 겹침으로 보지 않도록 조금 줄여서 잰다.
        /// </summary>
        private static bool OverlapsOther(Vector3 center, Quaternion rotation, Vector3 half)
        {
            int count = Physics.OverlapBoxNonAlloc(center, half * OverlapMargin, OverlapBuffer, rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!OverlapBuffer[i].TryGetComponent(out PlacedBlock _)) return true;
            }

            return false;
        }

        /// <summary>
        /// 이 자리에 부품을 놓으면 무엇과든(캐릭터, 다른 물체, 놓인 블록) 겹치는지. 끌어서 놓을 때 쓴다.
        /// 하나씩 놓을 때는 가리킨 면 위에 얹으므로 블록끼리 겹쳐도 되지만, 끌 때는 사이를 가리는 블록을 보지 않으므로
        /// 이미 있는 블록의 속에 놓이지 않게 건너뛴다. 면이 맞닿기만 한 것은 겹침이 아니다.
        /// </summary>
        private static bool OverlapsAnything(Vector3 center, Quaternion rotation, Vector3 half)
        {
            return Physics.OverlapBoxNonAlloc(center, half * OverlapMargin, OverlapBuffer, rotation, ~0, QueryTriggerInteraction.Ignore) > 0;
        }

        private static BlockedReason ReasonOf(PlaceResult check, bool overlap)
        {
            switch (check)
            {
                case PlaceResult.OutOfBounds: return BlockedReason.OutOfBounds;
                case PlaceResult.Occupied: return BlockedReason.Occupied;
                case PlaceResult.Full: return BlockedReason.Full;
                case PlaceResult.UnknownPart: return BlockedReason.Occupied;
                default: return overlap ? BlockedReason.Overlap : BlockedReason.None;
            }
        }

        private void ShowGhost()
        {
            if (ghost == null) return;

            if (ghost.gameObject.activeSelf != HasTarget) ghost.gameObject.SetActive(HasTarget);
            if (!HasTarget) return;

            // 잡은 블록은 그 블록의 색과 모양으로, 조금 더 진하게 보여 새로 놓는 블록과 구별한다.
            // 끌어서 놓는 동안에는 끌기를 시작할 때의 부품과 방향으로 보인다.
            bool laying = Stroke == StrokeKind.Place;
            int part = laying ? strokePart : TargetPart;
            float alpha = IsCarrying ? carriedAlpha : ghostAlpha;
            PartCatalog.Part shown = world.Catalog.Get(part);
            if (ghostMesh != null && shown.mesh != null && ghostMesh.sharedMesh != shown.mesh) ghostMesh.sharedMesh = shown.mesh;

            Color color = CanPlaceAtTarget ? shown.color : blockedColor;
            color.a = alpha;
            ghostColor.SetColor(BaseColorId, color);
            ghost.SetPropertyBlock(ghostColor);
            ghost.transform.SetPositionAndRotation(TargetPosition, laying ? strokeRotation : TargetRotation);
        }
    }
}
