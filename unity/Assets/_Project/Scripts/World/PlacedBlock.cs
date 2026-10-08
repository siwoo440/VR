using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>
    /// 맵에 놓인 블록 하나의 화면 쪽 모습. 자신이 기록의 몇 번 블록이고 어떤 부품인지 기억한다.
    /// 자리와 방향은 트랜스폼이 가진다. 번호는 실행 중에 블록 세계가 정해 주며 씬에는 저장하지 않는다.
    /// </summary>
    public class PlacedBlock : MonoBehaviour
    {
        [SerializeField] private int partIndex;

        private int id;

        /// <summary>블록 기록에서의 번호. 아직 기록에 오르지 않았으면 0이다.</summary>
        public int Id => id;

        public int PartIndex => partIndex;

        public void Initialize(int blockId, int part)
        {
            id = blockId;
            partIndex = part;
        }
    }
}
