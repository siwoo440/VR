using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>
    /// 맵의 블록을 놓고 지우는 곳. 기록(BlockMap)과 화면에 보이는 블록을 함께 맞춘다.
    /// 씬에 미리 놓인 블록(자식의 PlacedBlock)은 시작할 때 기록에 올린다.
    /// </summary>
    public class BlockWorld : MonoBehaviour
    {
        [SerializeField] private PartCatalog catalog;
        [SerializeField] private PlacedBlock blockPrefab;
        [SerializeField] private int maxBlocks = 500;
        [SerializeField] private Vector3Int minCell = new Vector3Int(-8, 0, -8);
        [SerializeField] private Vector3Int maxCell = new Vector3Int(7, 11, 7);

        private readonly Dictionary<Vector3Int, PlacedBlock> views = new Dictionary<Vector3Int, PlacedBlock>();
        private BlockMap map;

        /// <summary>블록을 놓거나 지워 개수가 바뀌면 알린다.</summary>
        public event Action Changed;

        public PartCatalog Catalog => catalog;

        public int Count => Map.Count;

        public int MaxBlocks => Map.Capacity;

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

            PlacedBlock block = Instantiate(blockPrefab, GridMath.CellToWorldCenter(cell), Quaternion.identity, transform);
            block.name = $"Block_{cell.x}_{cell.y}_{cell.z}";
            block.Initialize(cell, partIndex);
            if (block.TryGetComponent(out Renderer blockRenderer)) blockRenderer.sharedMaterial = catalog.Get(partIndex).material;

            views[cell] = block;
            Changed?.Invoke();
            return result;
        }

        public bool Remove(Vector3Int cell)
        {
            if (!Map.Remove(cell)) return false;

            if (views.TryGetValue(cell, out PlacedBlock block))
            {
                views.Remove(cell);
                if (block != null)
                {
                    // 없애는 것은 프레임 끝에 이루어지므로, 그 전에 꺼서 같은 프레임의 조준에 걸리지 않게 한다.
                    block.gameObject.SetActive(false);
                    Destroy(block.gameObject);
                }
            }

            Changed?.Invoke();
            return true;
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
