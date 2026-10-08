using AtelierVerse.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AtelierVerse.Player
{
    /// <summary>
    /// 고른 부품을 조준한 칸에 놓고, 놓인 블록을 지운다. 놓일 자리는 반투명 블록으로 미리 보여 준다.
    /// 부품을 고르고 마우스를 잡은 상태에서만 동작하며, 캐릭터나 다른 물체와 겹치는 칸에는 놓지 않는다.
    /// </summary>
    [RequireComponent(typeof(DesktopPlayerController))]
    public class BlockBuilder : MonoBehaviour
    {
        public const int NoPart = -1;

        private const string MapName = "Player";
        private const float OverlapMargin = 0.49f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private Renderer ghostPrefab;
        [SerializeField] private float reach = 6f;
        [SerializeField] private float ghostAlpha = 0.55f;
        [SerializeField] private Color blockedColor = new Color(0.72f, 0.27f, 0.06f);

        private DesktopPlayerController player;
        private BlockWorld world;
        private Renderer ghost;
        private MaterialPropertyBlock ghostColor;
        private InputAction placeAction;
        private InputAction removeAction;
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

        public BlockWorld World => world;

        private bool IsActive => world != null
            && world.Catalog != null
            && world.Catalog.IsValid(SelectedPart)
            && player.LookCaptured
            && !player.InputBlocked;

        private void Awake()
        {
            player = GetComponent<DesktopPlayerController>();
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
        }

        private bool PlaceAtTarget()
        {
            if (!HasTarget || !CanPlaceAtTarget) return false;
            return world.Place(TargetCell, SelectedPart) == PlaceResult.Ok;
        }

        private bool RemoveAtTarget()
        {
            return hasRemoveTarget && world.Remove(removeCell);
        }

        /// <summary>
        /// 화면 가운데가 가리키는 곳을 찾는다. 닿은 면의 바깥쪽 칸이 놓일 칸이고, 닿은 것이 놓인 블록이면 그 블록이 지울 대상이다.
        /// </summary>
        private void UpdateTarget(bool active)
        {
            HasTarget = false;
            CanPlaceAtTarget = false;
            hasRemoveTarget = false;
            if (!active || player.ViewCamera == null) return;

            Transform view = player.ViewCamera.transform;
            float distance = reach + Vector3.Distance(view.position, player.HeadPosition);
            if (!Physics.Raycast(view.position, view.forward, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;

            float cellSize = GridMath.DefaultCellSize;
            TargetCell = GridMath.WorldToCell(hit.point + hit.normal * (cellSize * 0.5f));
            HasTarget = true;
            CanPlaceAtTarget = world.CheckPlace(TargetCell) == PlaceResult.Ok
                && !Physics.CheckBox(GridMath.CellToWorldCenter(TargetCell), Vector3.one * (cellSize * OverlapMargin), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);

            if (hit.collider.TryGetComponent(out PlacedBlock block))
            {
                hasRemoveTarget = true;
                removeCell = block.Cell;
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
