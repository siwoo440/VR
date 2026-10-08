using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>
    /// 맵에 놓인 블록 하나. 자신이 어느 칸의 어떤 부품인지 기억한다.
    /// </summary>
    public class PlacedBlock : MonoBehaviour
    {
        [SerializeField] private Vector3Int cell;
        [SerializeField] private int partIndex;

        public Vector3Int Cell => cell;

        public int PartIndex => partIndex;

        public void Initialize(Vector3Int blockCell, int part)
        {
            cell = blockCell;
            partIndex = part;
        }
    }
}
