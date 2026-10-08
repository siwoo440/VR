using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>
    /// 맵의 블록을 놓고 지우는 곳. 기록(BlockMap)과 화면에 보이는 블록을 함께 맞춘다.
    /// 씬에 미리 놓인 블록(자식의 PlacedBlock)은 시작할 때 기록에 올린다.
    /// 맵 문서로 내보내고(Export) 문서에서 통째로 바꿔 넣는(Import) 일도 맡는다.
    /// 이용자의 편집은 History(기록 층)를 거쳐야 되돌릴 수 있다. Place·Remove는 기록하지 않는 바탕 동작이다.
    /// </summary>
    public class BlockWorld : MonoBehaviour, IBlockStore
    {
        [SerializeField] private PartCatalog catalog;
        [SerializeField] private PlacedBlock blockPrefab;
        [SerializeField] private int maxBlocks = 500;
        [SerializeField] private Vector3Int minCell = new Vector3Int(-8, 0, -8);
        [SerializeField] private Vector3Int maxCell = new Vector3Int(7, 11, 7);

        private readonly Dictionary<Vector3Int, PlacedBlock> views = new Dictionary<Vector3Int, PlacedBlock>();
        private BlockMap map;
        private EditHistory history;

        /// <summary>블록을 놓거나 지워 개수가 바뀌면 알린다.</summary>
        public event Action Changed;

        public PartCatalog Catalog => catalog;

        /// <summary>되돌리기 기록 층. 이용자가 놓고 지우는 편집은 이곳을 거친다.</summary>
        public EditHistory History => history ??= new EditHistory(this);

        public int Count => Map.Count;

        public int MaxBlocks => Map.Capacity;

        public Vector3Int MinCell => minCell;

        public Vector3Int MaxCell => maxCell;

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

        public bool Has(Vector3Int cell)
        {
            return Map.Contains(cell);
        }

        public bool TryGetPart(Vector3Int cell, out int partIndex)
        {
            return Map.TryGet(cell, out partIndex);
        }

        /// <summary>이 칸에 놓을 수 있는지 미리 확인한다. 캐릭터나 다른 물체와 겹치는지는 보지 않는다.</summary>
        public PlaceResult CheckPlace(Vector3Int cell)
        {
            return Map.Check(cell);
        }

        public PlaceResult Place(Vector3Int cell, int partIndex)
        {
            if (catalog == null || blockPrefab == null || !catalog.IsValid(partIndex)) return PlaceResult.UnknownPart;

            PlaceResult result = Map.Place(cell, partIndex);
            if (result != PlaceResult.Ok) return result;

            CreateView(cell, partIndex);
            Changed?.Invoke();
            return result;
        }

        public bool Remove(Vector3Int cell)
        {
            if (!Map.Remove(cell)) return false;

            DestroyView(cell);
            Changed?.Invoke();
            return true;
        }

        /// <summary>있는 블록의 부품을 바꾼다(칠하기). 화면의 재질도 함께 바뀐다.</summary>
        public bool Replace(Vector3Int cell, int partIndex)
        {
            if (catalog == null || !catalog.IsValid(partIndex)) return false;
            if (!Map.Replace(cell, partIndex)) return false;

            if (views.TryGetValue(cell, out PlacedBlock block) && block != null)
            {
                block.Initialize(cell, partIndex);
                if (block.TryGetComponent(out Renderer blockRenderer)) blockRenderer.sharedMaterial = catalog.Get(partIndex).material;
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
            if (header == null) header = MapDocument.Create(string.Empty, minCell, maxCell);
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

            foreach (KeyValuePair<Vector3Int, int> entry in Map.Blocks)
            {
                CreateView(entry.Key, entry.Value);
            }

            Changed?.Invoke();
            return report;
        }

        private void CreateView(Vector3Int cell, int partIndex)
        {
            PlacedBlock block = Instantiate(blockPrefab, GridMath.CellToWorldCenter(cell), Quaternion.identity, transform);
            block.name = $"Block_{cell.x}_{cell.y}_{cell.z}";
            block.Initialize(cell, partIndex);
            if (block.TryGetComponent(out Renderer blockRenderer)) blockRenderer.sharedMaterial = catalog.Get(partIndex).material;
            views[cell] = block;
        }

        private void DestroyView(Vector3Int cell)
        {
            if (!views.TryGetValue(cell, out PlacedBlock block)) return;

            views.Remove(cell);
            if (block == null) return;

            // 없애는 것은 프레임 끝에 이루어지므로, 그 전에 꺼서 같은 프레임의 조준에 걸리지 않게 한다.
            block.gameObject.SetActive(false);
            Destroy(block.gameObject);
        }

        private void ClearAll()
        {
            History.Clear();
            var cells = new List<Vector3Int>(views.Keys);
            foreach (Vector3Int cell in cells)
            {
                DestroyView(cell);
            }

            views.Clear();
            Map.Clear();
        }

        private void RegisterSceneBlocks()
        {
            map = new BlockMap(minCell, maxCell, maxBlocks);

            foreach (PlacedBlock block in GetComponentsInChildren<PlacedBlock>())
            {
                if (map.Place(block.Cell, block.PartIndex) == PlaceResult.Ok)
                {
                    views[block.Cell] = block;
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
