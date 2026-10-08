using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>
    /// 맵의 블록을 놓고 지우고 고치는 곳. 기록(BlockMap)과 화면에 보이는 블록을 함께 맞춘다.
    /// 블록은 번호로 가리키며 칸에 맞추지 않고 어디에나 놓인다. 길이의 표준은 블록 한 변(1)이다.
    /// 씬에 미리 놓인 블록(자식의 PlacedBlock)은 시작할 때 그 자리 그대로 기록에 올린다.
    /// 맵 문서로 내보내고(Export) 문서에서 통째로 바꿔 넣는(Import) 일도 맡는다.
    /// 이용자의 편집은 History(기록 층)를 거쳐야 되돌릴 수 있다. Add·Remove·Set은 기록하지 않는 바탕 동작이다.
    /// </summary>
    public class BlockWorld : MonoBehaviour, IBlockStore
    {
        [SerializeField] private PartCatalog catalog;
        [SerializeField] private PlacedBlock blockPrefab;
        [SerializeField] private int maxBlocks = 500;
        [SerializeField] private Vector3Int minCell = new Vector3Int(-8, 0, -8);
        [SerializeField] private Vector3Int maxCell = new Vector3Int(7, 11, 7);

        private readonly Dictionary<int, PlacedBlock> views = new Dictionary<int, PlacedBlock>();
        private BlockMap map;
        private EditHistory history;

        /// <summary>블록을 놓거나 지우거나 고치면 알린다.</summary>
        public event Action Changed;

        public PartCatalog Catalog => catalog;

        /// <summary>되돌리기 기록 층. 이용자가 놓고 지우는 편집은 이곳을 거친다.</summary>
        public EditHistory History => history ??= new EditHistory(this);

        public int Count => Map.Count;

        public int MaxBlocks => Map.Capacity;

        /// <summary>
        /// 놓을 수 있는 범위(상자)의 가장 작은 모서리. 씬에는 모눈의 칸 번호로 적혀 있고, 가장 작은 칸의 아래 모서리가 된다.
        /// </summary>
        public Vector3 BoundsMin => (Vector3)Vector3Int.Min(minCell, maxCell) * GridMath.DefaultCellSize;

        /// <summary>놓을 수 있는 범위(상자)의 가장 큰 모서리. 가장 큰 칸의 위 모서리가 된다.</summary>
        public Vector3 BoundsMax => (Vector3)(Vector3Int.Max(minCell, maxCell) + Vector3Int.one) * GridMath.DefaultCellSize;

        /// <summary>놓인 블록의 목록.</summary>
        public IReadOnlyCollection<BlockRecord> Blocks => Map.Blocks;

        private BlockMap Map
        {
            get
            {
                if (map == null) RegisterSceneBlocks();
                return map;
            }
        }

        private void Awake()
        {
            if (map == null) RegisterSceneBlocks();
        }

        public bool Has(int id)
        {
            return Map.Contains(id);
        }

        public bool TryGet(int id, out BlockRecord record)
        {
            return Map.TryGet(id, out record);
        }

        /// <summary>position에서 radius 안에 가운데가 있는 블록 가운데 가장 가까운 것을 찾는다.</summary>
        public bool TryFindNear(Vector3 position, float radius, out BlockRecord nearest)
        {
            return Map.TryFindNear(position, radius, out nearest);
        }

        /// <summary>이 자리에 놓을 수 있는지 미리 확인한다. 캐릭터나 다른 물체와 겹치는지는 보지 않는다.</summary>
        public PlaceResult CheckPlace(Vector3 position)
        {
            return Map.Check(position);
        }

        /// <summary>높이만 범위 안으로 맞춘다. 바닥에 묻히는 자리를 바닥 위로 올리는 데 쓴다.</summary>
        public Vector3 ClampHeight(Vector3 position)
        {
            return Map.ClampHeight(position);
        }

        public PlaceResult Add(int partIndex, Vector3 position, Quaternion rotation, out int id)
        {
            id = BlockMap.NoId;
            if (catalog == null || blockPrefab == null || !catalog.IsValid(partIndex)) return PlaceResult.UnknownPart;

            PlaceResult result = Map.Add(partIndex, position, rotation, out id);
            if (result != PlaceResult.Ok) return result;

            CreateView(id);
            Changed?.Invoke();
            return result;
        }

        /// <summary>블록을 똑바로 선 채로 놓는다.</summary>
        public PlaceResult Add(int partIndex, Vector3 position)
        {
            return Add(partIndex, position, Quaternion.identity, out _);
        }

        public PlaceResult Restore(BlockRecord record)
        {
            if (catalog == null || blockPrefab == null || !catalog.IsValid(record.Part)) return PlaceResult.UnknownPart;

            PlaceResult result = Map.Restore(record);
            if (result != PlaceResult.Ok) return result;

            CreateView(record.Id);
            Changed?.Invoke();
            return result;
        }

        public bool Remove(int id)
        {
            if (!Map.Remove(id)) return false;

            DestroyView(id);
            Changed?.Invoke();
            return true;
        }

        /// <summary>있는 블록의 부품, 자리, 방향을 바꾼다(칠하기, 옮기기, 돌리기). 화면의 블록도 함께 바뀐다.</summary>
        public bool Set(BlockRecord record)
        {
            if (catalog == null || !catalog.IsValid(record.Part)) return false;
            if (!Map.Set(record)) return false;

            if (Map.TryGet(record.Id, out BlockRecord stored) && views.TryGetValue(record.Id, out PlacedBlock block) && block != null)
            {
                ApplyToView(block, stored);
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>블록을 모두 지운다.</summary>
        public void Clear()
        {
            ClearAll();
            Changed?.Invoke();
        }

        /// <summary>지금 놓인 블록을 맵 문서로 만든다. header의 이름과 만든 시각을 이어받고 저장 시각은 지금이 된다.</summary>
        public MapDocument Export(MapDocument header)
        {
            if (header == null) header = MapDocument.Create(string.Empty, BoundsMin, BoundsMax);
            return MapDocument.FromBlocks(header, Map.Blocks, index => catalog != null && catalog.IsValid(index) ? catalog.Get(index).id : null);
        }

        /// <summary>
        /// 지금 블록을 모두 지우고 문서의 블록으로 바꿔 넣는다. 읽지 못한 블록의 수는 결과에 담긴다.
        /// 문서의 범위는 참고용이며 실제 범위는 이 블록 세계의 값이다.
        /// </summary>
        public MapLoadReport Import(MapDocument document)
        {
            ClearAll();
            MapLoadReport report = MapDocument.Apply(document, Map, id => catalog != null ? catalog.IndexOf(id) : -1);

            foreach (BlockRecord record in Map.Blocks)
            {
                CreateView(record.Id);
            }

            Changed?.Invoke();
            return report;
        }

        private void CreateView(int id)
        {
            if (!Map.TryGet(id, out BlockRecord record)) return;

            PlacedBlock block = Instantiate(blockPrefab, record.Position, record.Rotation, transform);
            block.name = $"Block_{id}";
            ApplyToView(block, record);
            views[id] = block;
        }

        private void ApplyToView(PlacedBlock block, BlockRecord record)
        {
            block.transform.SetPositionAndRotation(record.Position, record.Rotation);
            block.Initialize(record.Id, record.Part);
            if (block.TryGetComponent(out Renderer blockRenderer)) blockRenderer.sharedMaterial = catalog.Get(record.Part).material;
        }

        private void DestroyView(int id)
        {
            if (!views.TryGetValue(id, out PlacedBlock block)) return;

            views.Remove(id);
            if (block == null) return;

            // 없애는 것은 프레임 끝에 이루어지므로, 그 전에 꺼서 같은 프레임의 조준에 걸리지 않게 한다.
            block.gameObject.SetActive(false);
            Destroy(block.gameObject);
        }

        private void ClearAll()
        {
            History.Clear();
            var ids = new List<int>(views.Keys);
            foreach (int id in ids)
            {
                DestroyView(id);
            }

            views.Clear();
            Map.Clear();
        }

        /// <summary>씬에 미리 놓인 블록을 그 자리와 방향 그대로 기록에 올리고 번호를 붙인다.</summary>
        private void RegisterSceneBlocks()
        {
            map = new BlockMap(BoundsMin, BoundsMax, maxBlocks);

            foreach (PlacedBlock block in GetComponentsInChildren<PlacedBlock>())
            {
                Transform blockTransform = block.transform;
                if (map.Add(block.PartIndex, blockTransform.position, blockTransform.rotation, out int id) == PlaceResult.Ok)
                {
                    block.Initialize(id, block.PartIndex);
                    views[id] = block;
                }
                else
                {
                    Debug.LogWarning($"[Atelier Verse] 씬의 블록 {block.name}을(를) 기록에 올리지 못해 지웁니다.", this);
                    Destroy(block.gameObject);
                }
            }
        }
    }
}
