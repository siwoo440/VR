using AtelierVerse.Core;
using AtelierVerse.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AtelierVerse.Player
{
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
    /// 고른 부품을 조준한 칸에 놓고, 놓인 블록을 지우거나 고른 부품으로 칠한다. 놓일 자리는 반투명 블록으로 미리 보여 준다.
    /// 부품을 고르고 조준하고 있을 때만(PC에서는 마우스를 잡았을 때) 동작하며, 캐릭터나 다른 물체와 겹치는 칸에는 놓지 않는다.
    /// 놓기·지우기·칠하기는 블록 세계의 기록 층(History)을 거쳐 되돌릴 수 있다. 놓지 못한 까닭은 알림으로 올린다.
    /// 조준 광선과 조작이 막혔는지는 이 기기의 캐릭터(LocalPlayer)에게 물으므로 조작 방식을 직접 알지 않는다.
    /// </summary>
    [RequireComponent(typeof(LocalPlayer))]
    public class BlockBuilder : MonoBehaviour
    {
        public const int NoPart = -1;
        public const string PaintTargetMessage = "칠할 블록을 가리키세요";
        public const string SamePartMessage = "이미 같은 부품입니다";

        private const string MapName = "Player";
        private const float OverlapMargin = 0.49f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private Renderer ghostPrefab;
        [SerializeField] private float reach = 6f;
        [SerializeField] private float ghostAlpha = 0.55f;
        [SerializeField] private Color blockedColor = new Color(0.72f, 0.27f, 0.06f);

        private LocalPlayer player;
        private BlockWorld world;
        private Renderer ghost;
        private MaterialPropertyBlock ghostColor;
        private InputAction placeAction;
        private InputAction removeAction;
        private InputAction paintAction;
        private bool wasActive;
        private bool hasRemoveTarget;
        private Vector3Int removeCell;

        /// <summary>놓을 부품의 번호. 고른 부품이 없으면 NoPart다.</summary>
        public int SelectedPart { get; set; } = NoPart;

        /// <summary>조준한 곳에 놓일 칸이 있는지.</summary>
        public bool HasTarget { get; private set; }

        /// <summary>왼쪽을 누르면 블록이 놓일 칸.</summary>
        public Vector3Int TargetCell { get; private set; }

        public bool CanPlaceAtTarget { get; private set; }

        /// <summary>조준한 칸에 놓을 수 없는 까닭. 놓을 수 있으면 None이다.</summary>
        public BlockedReason Blocked { get; private set; }

        /// <summary>조준한 곳에 지우거나 칠할 블록이 있는지.</summary>
        public bool HasBlockTarget => hasRemoveTarget;

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

            InputActionMap map = actions.FindActionMap(MapName, true);
            placeAction = map.FindAction("Place", true);
            removeAction = map.FindAction("Remove", true);
            paintAction = map.FindAction("Paint", true);
            map.Enable();
        }

        private void OnDisable()
        {
            wasActive = false;
            if (ghost != null) ghost.gameObject.SetActive(false);
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

            UpdateTarget(active);
            ShowGhost();
            if (!ready) return;

            if (placeAction.WasPressedThisFrame()) PlaceAtTarget();
            else if (removeAction.WasPressedThisFrame()) RemoveAtTarget();
            else if (paintAction.WasPressedThisFrame()) PaintAtTarget();
        }

        private bool PlaceAtTarget()
        {
            if (!HasTarget) return false;

            if (!CanPlaceAtTarget)
            {
                Notice.Post(Describe(Blocked), NoticeKind.Warning);
                return false;
            }

            return world.History.Place(TargetCell, SelectedPart) == PlaceResult.Ok;
        }

        private bool RemoveAtTarget()
        {
            return hasRemoveTarget && world.History.Remove(removeCell);
        }

        /// <summary>조준한 블록을 고른 부품으로 바꾼다. 블록이 없거나 이미 같은 부품이면 알림만 올린다.</summary>
        private bool PaintAtTarget()
        {
            if (!hasRemoveTarget)
            {
                Notice.Post(PaintTargetMessage);
                return false;
            }

            if (world.TryGetPart(removeCell, out int current) && current == SelectedPart)
            {
                Notice.Post(SamePartMessage);
                return false;
            }

            return world.History.Replace(removeCell, SelectedPart);
        }

        private string Describe(BlockedReason reason)
        {
            switch (reason)
            {
                case BlockedReason.OutOfBounds: return "맵 바깥에는 놓을 수 없습니다";
                case BlockedReason.Occupied: return "이미 블록이 있는 칸입니다";
                case BlockedReason.Full: return $"블록이 {world.MaxBlocks}개에 닿아 더 놓을 수 없습니다";
                case BlockedReason.Overlap: return "캐릭터나 다른 물체와 겹쳐 놓을 수 없습니다";
                default: return "여기에는 놓을 수 없습니다";
            }
        }

        /// <summary>
        /// 조준 광선이 가리키는 곳을 찾는다. 닿은 면의 바깥쪽 칸이 놓일 칸이고, 닿은 것이 놓인 블록이면 그 블록이 지울 대상이다.
        /// </summary>
        private void UpdateTarget(bool active)
        {
            HasTarget = false;
            CanPlaceAtTarget = false;
            Blocked = BlockedReason.None;
            hasRemoveTarget = false;
            if (!active || !player.TryGetAim(out Ray aim, out float extraReach)) return;

            if (!Physics.Raycast(aim, out RaycastHit hit, reach + extraReach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;

            float cellSize = GridMath.DefaultCellSize;
            TargetCell = GridMath.WorldToCell(hit.point + hit.normal * (cellSize * 0.5f));
            HasTarget = true;

            PlaceResult check = world.CheckPlace(TargetCell);
            bool overlap = check == PlaceResult.Ok
                && Physics.CheckBox(GridMath.CellToWorldCenter(TargetCell), Vector3.one * (cellSize * OverlapMargin), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            Blocked = ReasonOf(check, overlap);
            CanPlaceAtTarget = Blocked == BlockedReason.None;

            if (hit.collider.TryGetComponent(out PlacedBlock block))
            {
                hasRemoveTarget = true;
                removeCell = block.Cell;
            }
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

            Color color = CanPlaceAtTarget ? world.Catalog.Get(SelectedPart).color : blockedColor;
            color.a = ghostAlpha;
            ghostColor.SetColor(BaseColorId, color);
            ghost.SetPropertyBlock(ghostColor);
            ghost.transform.SetPositionAndRotation(GridMath.CellToWorldCenter(TargetCell), Quaternion.identity);
        }
    }
}
