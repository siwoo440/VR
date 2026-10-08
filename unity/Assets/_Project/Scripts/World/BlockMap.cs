using System.Collections.Generic;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>블록을 놓으려 할 때의 결과.</summary>
    public enum PlaceResult
    {
        Ok,
        OutOfBounds,
        Occupied,
        Full,
        UnknownPart,
    }

    /// <summary>
    /// 맵에 놓인 블록의 기록. 칸마다 어떤 부품이 있는지만 다루고, 화면에 보이는 블록은 BlockWorld가 맡는다.
    /// 놓을 수 있는 범위와 개수 상한을 여기서 지킨다.
    /// </summary>
    public class BlockMap
    {
        private readonly Dictionary<Vector3Int, int> blocks = new Dictionary<Vector3Int, int>();
        private readonly Vector3Int min;
        private readonly Vector3Int max;

        public BlockMap(Vector3Int minCell, Vector3Int maxCell, int capacity)
        {
            min = Vector3Int.Min(minCell, maxCell);
            max = Vector3Int.Max(minCell, maxCell);
            Capacity = Mathf.Max(0, capacity);
        }

        public int Count => blocks.Count;

        /// <summary>놓을 수 있는 블록 수의 상한.</summary>
        public int Capacity { get; }

        /// <summary>칸 번호와 부품 번호의 목록. 저장할 때 이 목록을 쓴다.</summary>
        public IReadOnlyDictionary<Vector3Int, int> Blocks => blocks;

        public bool Contains(Vector3Int cell)
        {
            return blocks.ContainsKey(cell);
        }

        public bool InBounds(Vector3Int cell)
        {
            return cell.x >= min.x && cell.x <= max.x
                && cell.y >= min.y && cell.y <= max.y
                && cell.z >= min.z && cell.z <= max.z;
        }

        public bool TryGet(Vector3Int cell, out int partIndex)
        {
            return blocks.TryGetValue(cell, out partIndex);
        }

        /// <summary>이 칸에 놓을 수 있는지 미리 확인한다. 기록은 바뀌지 않는다.</summary>
        public PlaceResult Check(Vector3Int cell)
        {
            if (!InBounds(cell)) return PlaceResult.OutOfBounds;
            if (blocks.ContainsKey(cell)) return PlaceResult.Occupied;
            if (blocks.Count >= Capacity) return PlaceResult.Full;
            return PlaceResult.Ok;
        }

        public PlaceResult Place(Vector3Int cell, int partIndex)
        {
            if (partIndex < 0) return PlaceResult.UnknownPart;

            PlaceResult result = Check(cell);
            if (result == PlaceResult.Ok) blocks[cell] = partIndex;
            return result;
        }

        public bool Remove(Vector3Int cell)
        {
            return blocks.Remove(cell);
        }
    }
}
