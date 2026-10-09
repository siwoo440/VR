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
    /// 맵에 놓인 블록 하나의 기록. 블록은 칸이 아니라 고유 번호(Id)로 가리키고, 자리는 가운데의 좌표다.
    /// 길이의 단위는 표준 블록 한 변(1)이며 Unity의 1m와 같다.
    /// </summary>
    public struct BlockRecord
    {
        public int Id;
        public int Part;
        public Vector3 Position;
        public Quaternion Rotation;

        public BlockRecord(int id, int part, Vector3 position, Quaternion rotation)
        {
            Id = id;
            Part = part;
            Position = position;
            Rotation = rotation;
        }

        /// <summary>자리와 방향이 같은지. 부품과 번호는 보지 않는다.</summary>
        public bool SamePose(BlockRecord other)
        {
            return (Position - other.Position).sqrMagnitude < BlockMap.Precision * BlockMap.Precision * 0.25f
                && Quaternion.Angle(Rotation, other.Rotation) < 0.01f;
        }
    }

    /// <summary>
    /// 맵에 놓인 블록의 기록. 블록마다 고유 번호와 자리(가운데의 좌표), 방향, 부품을 다루고, 화면에 보이는 블록은 BlockWorld가 맡는다.
    /// 블록은 칸에 맞추지 않고 어디에나 놓을 수 있으며 서로 겹쳐도 된다. 다만 가운데가 정확히 같은 자리에는 둘을 두지 않는다.
    /// 놓을 수 있는 범위와 개수 상한을 여기서 지킨다. 자리는 1mm 단위로 다듬어 기록한다.
    /// </summary>
    public class BlockMap : IBlockStore
    {
        /// <summary>표준 블록의 반 변. 블록 한 변이 길이의 표준(1)이다.</summary>
        public const float HalfSize = GridMath.DefaultCellSize * 0.5f;

        /// <summary>자리를 다듬는 단위(1mm).</summary>
        public const float Precision = 0.001f;

        /// <summary>번호가 없음을 뜻하는 값. 블록의 번호는 1부터 시작한다.</summary>
        public const int NoId = 0;

        private readonly Dictionary<int, BlockRecord> blocks = new Dictionary<int, BlockRecord>();
        private readonly Dictionary<Vector3Int, int> centers = new Dictionary<Vector3Int, int>();
        private readonly Vector3 min;
        private readonly Vector3 max;
        private int nextId = 1;

        /// <summary>범위는 상자의 두 모서리로 준다. 블록은 이 상자 안에 다 들어와야 한다.</summary>
        public BlockMap(Vector3 minCorner, Vector3 maxCorner, int capacity)
        {
            min = Vector3.Min(minCorner, maxCorner);
            max = Vector3.Max(minCorner, maxCorner);
            Capacity = Mathf.Max(0, capacity);
        }

        public int Count => blocks.Count;

        /// <summary>놓을 수 있는 블록 수의 상한.</summary>
        public int Capacity { get; }

        /// <summary>놓을 수 있는 범위(상자)의 가장 작은 모서리.</summary>
        public Vector3 Min => min;

        /// <summary>놓을 수 있는 범위(상자)의 가장 큰 모서리.</summary>
        public Vector3 Max => max;

        /// <summary>놓인 블록의 목록. 저장할 때 이 목록을 쓴다.</summary>
        public IReadOnlyCollection<BlockRecord> Blocks => blocks.Values;

        public bool Contains(int id)
        {
            return blocks.ContainsKey(id);
        }

        public bool TryGet(int id, out BlockRecord record)
        {
            return blocks.TryGetValue(id, out record);
        }

        /// <summary>표준 블록을 이 자리에 놓았을 때 범위 안에 다 들어오는지.</summary>
        public bool InBounds(Vector3 position)
        {
            float slack = Precision * 0.5f;
            return position.x >= min.x + HalfSize - slack && position.x <= max.x - HalfSize + slack
                && position.y >= min.y + HalfSize - slack && position.y <= max.y - HalfSize + slack
                && position.z >= min.z + HalfSize - slack && position.z <= max.z - HalfSize + slack;
        }

        /// <summary>이 자리에 놓을 수 있는지 미리 확인한다. 기록은 바뀌지 않는다.</summary>
        public PlaceResult Check(Vector3 position)
        {
            position = Quantize(position);
            if (!InBounds(position)) return PlaceResult.OutOfBounds;
            if (centers.ContainsKey(KeyOf(position))) return PlaceResult.Occupied;
            if (blocks.Count >= Capacity) return PlaceResult.Full;
            return PlaceResult.Ok;
        }

        /// <summary>
        /// 있는 블록을 이 자리로 옮길 수 있는지 미리 확인한다. 블록 수가 늘지 않으므로 상한은 보지 않고, 제자리는 막지 않는다.
        /// </summary>
        public PlaceResult CheckMove(int id, Vector3 position)
        {
            position = Quantize(position);
            if (!InBounds(position)) return PlaceResult.OutOfBounds;
            if (centers.TryGetValue(KeyOf(position), out int owner) && owner != id) return PlaceResult.Occupied;
            return PlaceResult.Ok;
        }

        /// <summary>블록을 새로 놓는다. 놓였으면 새 번호가 id에 담긴다.</summary>
        public PlaceResult Add(int partIndex, Vector3 position, Quaternion rotation, out int id)
        {
            id = NoId;
            if (partIndex < 0) return PlaceResult.UnknownPart;

            PlaceResult result = Check(position);
            if (result != PlaceResult.Ok) return result;

            id = nextId;
            Store(new BlockRecord(id, partIndex, position, rotation));
            return PlaceResult.Ok;
        }

        /// <summary>
        /// 번호가 정해진 블록을 그대로 되살린다. 지운 블록을 되돌리거나 파일에서 읽을 때 쓴다.
        /// 번호가 1보다 작거나 이미 쓰이고 있으면 Occupied다.
        /// </summary>
        public PlaceResult Restore(BlockRecord record)
        {
            if (record.Part < 0) return PlaceResult.UnknownPart;
            if (record.Id < 1 || blocks.ContainsKey(record.Id)) return PlaceResult.Occupied;

            PlaceResult result = Check(record.Position);
            if (result != PlaceResult.Ok) return result;

            Store(record);
            return PlaceResult.Ok;
        }

        public bool Remove(int id)
        {
            if (!blocks.TryGetValue(id, out BlockRecord record)) return false;

            blocks.Remove(id);
            centers.Remove(KeyOf(record.Position));
            return true;
        }

        /// <summary>
        /// 있는 블록의 부품, 자리, 방향을 바꾼다(칠하기, 옮기기, 돌리기). 번호는 그대로다.
        /// 블록이 없거나, 부품 번호가 음수이거나, 새 자리가 범위 밖이거나 다른 블록의 가운데와 같으면 false다.
        /// </summary>
        public bool Set(BlockRecord record)
        {
            if (record.Part < 0 || !blocks.TryGetValue(record.Id, out BlockRecord current)) return false;

            Vector3 position = Quantize(record.Position);
            if (!InBounds(position)) return false;

            Vector3Int key = KeyOf(position);
            if (centers.TryGetValue(key, out int owner) && owner != record.Id) return false;

            centers.Remove(KeyOf(current.Position));
            centers[key] = record.Id;
            blocks[record.Id] = new BlockRecord(record.Id, record.Part, position, Normalize(record.Rotation));
            return true;
        }

        /// <summary>position에서 radius 안에 가운데가 있는 블록 가운데 가장 가까운 것을 찾는다.</summary>
        public bool TryFindNear(Vector3 position, float radius, out BlockRecord nearest)
        {
            nearest = default;
            float best = radius * radius;
            bool found = false;

            foreach (BlockRecord record in blocks.Values)
            {
                float distance = (record.Position - position).sqrMagnitude;
                if (distance > best) continue;

                best = distance;
                nearest = record;
                found = true;
            }

            return found;
        }

        /// <summary>
        /// 가리킨 면 위에 표준 블록을 얹었을 때의 가운데 자리. 면에서 바깥쪽으로 반 변만큼 떨어진 곳이며,
        /// 면 위에서는 가리킨 바로 그 자리다(칸의 가운데로 당기지 않는다).
        /// </summary>
        public static Vector3 RestOn(Vector3 point, Vector3 normal)
        {
            Vector3 outward = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            return Quantize(point + outward * HalfSize);
        }

        /// <summary>높이만 범위 안으로 맞춘다. 블록의 옆면 아래쪽을 가리켜도 바닥에 묻히지 않고 바닥 위에 놓이게 하는 데 쓴다.</summary>
        public Vector3 ClampHeight(Vector3 position)
        {
            position.y = Mathf.Clamp(position.y, min.y + HalfSize, Mathf.Max(min.y + HalfSize, max.y - HalfSize));
            return position;
        }

        /// <summary>자리를 1mm 단위로 다듬는다.</summary>
        public static Vector3 Quantize(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x / Precision) * Precision,
                Mathf.Round(position.y / Precision) * Precision,
                Mathf.Round(position.z / Precision) * Precision);
        }

        public void Clear()
        {
            blocks.Clear();
            centers.Clear();
            nextId = 1;
        }

        private void Store(BlockRecord record)
        {
            record.Position = Quantize(record.Position);
            record.Rotation = Normalize(record.Rotation);
            blocks[record.Id] = record;
            centers[KeyOf(record.Position)] = record.Id;
            if (record.Id >= nextId) nextId = record.Id + 1;
        }

        private static Vector3Int KeyOf(Vector3 position)
        {
            return new Vector3Int(
                Mathf.RoundToInt(position.x / Precision),
                Mathf.RoundToInt(position.y / Precision),
                Mathf.RoundToInt(position.z / Precision));
        }

        /// <summary>값을 넣지 않은 방향(모두 0)은 돌리지 않은 것으로 본다.</summary>
        private static Quaternion Normalize(Quaternion rotation)
        {
            float length = Mathf.Sqrt(rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w);
            if (length < 0.0001f) return Quaternion.identity;
            return new Quaternion(rotation.x / length, rotation.y / length, rotation.z / length, rotation.w / length);
        }
    }
}
