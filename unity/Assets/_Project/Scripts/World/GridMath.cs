using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>
    /// 길이의 표준과 모눈 계산. 모눈 한 칸(블록 한 변)이 길이의 표준 1이며 Unity의 1m다.
    /// 위치를 칸 번호로 바꾸거나 칸의 가운데 위치로 되돌린다. 블록은 칸에 맞추지 않고 자유롭게 놓이므로(14일차) 놓을 때는 쓰지 않고,
    /// 블록을 칸으로 적던 옛 판의 맵 파일을 올려 읽을 때와 뒤에 넣을 맞추기 도우미에 쓴다. 장면과 무관해 편집 모드 테스트로 검사한다.
    /// </summary>
    public static class GridMath
    {
        /// <summary>모눈 한 칸의 크기이자 표준 블록 한 변의 길이.</summary>
        public const float DefaultCellSize = 1f;
        private const float MinCellSize = 0.0001f;

        /// <summary>월드 위치가 들어 있는 칸 번호를 구한다. 음수 쪽도 같은 규칙으로 내림한다.</summary>
        public static Vector3Int WorldToCell(Vector3 world, float cellSize = DefaultCellSize)
        {
            float size = Mathf.Max(cellSize, MinCellSize);
            return new Vector3Int(
                Mathf.FloorToInt(world.x / size),
                Mathf.FloorToInt(world.y / size),
                Mathf.FloorToInt(world.z / size));
        }

        /// <summary>칸의 가운데 월드 위치를 구한다. 높이도 칸의 가운데다.</summary>
        public static Vector3 CellToWorldCenter(Vector3Int cell, float cellSize = DefaultCellSize)
        {
            float size = Mathf.Max(cellSize, MinCellSize);
            return new Vector3((cell.x + 0.5f) * size, (cell.y + 0.5f) * size, (cell.z + 0.5f) * size);
        }

        /// <summary>월드 위치를 그 위치가 들어 있는 칸의 가운데로 맞춘다.</summary>
        public static Vector3 SnapToCellCenter(Vector3 world, float cellSize = DefaultCellSize)
        {
            return CellToWorldCenter(WorldToCell(world, cellSize), cellSize);
        }
    }
}
