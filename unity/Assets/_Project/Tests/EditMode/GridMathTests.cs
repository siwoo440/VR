using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 모눈 계산 검사. 부품을 칸에 맞춰 놓는 기능의 바탕이므로 음수 쪽과 칸 경계를 함께 확인한다.
    /// </summary>
    public class GridMathTests
    {
        [Test]
        public void WorldToCell_양수_위치는_내림한_칸_번호가_된다()
        {
            Assert.AreEqual(new Vector3Int(2, 0, 3), GridMath.WorldToCell(new Vector3(2.4f, 0.9f, 3.99f)));
        }

        [Test]
        public void WorldToCell_음수_위치도_같은_규칙으로_내림한다()
        {
            Assert.AreEqual(new Vector3Int(-1, 0, -3), GridMath.WorldToCell(new Vector3(-0.1f, 0f, -2.5f)));
        }

        [Test]
        public void WorldToCell_칸_경계는_다음_칸에_속한다()
        {
            Assert.AreEqual(new Vector3Int(1, 0, 0), GridMath.WorldToCell(new Vector3(1f, 0f, 0f)));
        }

        [Test]
        public void CellToWorldCenter_칸의_가운데_위치를_돌려준다()
        {
            Assert.AreEqual(new Vector3(2.5f, 0.5f, -0.5f), GridMath.CellToWorldCenter(new Vector3Int(2, 0, -1)));
        }

        [Test]
        public void SnapToCellCenter_같은_칸_안의_위치는_같은_가운데로_맞춰진다()
        {
            Vector3 first = GridMath.SnapToCellCenter(new Vector3(4.1f, 0.2f, -1.9f));
            Vector3 second = GridMath.SnapToCellCenter(new Vector3(4.9f, 0.8f, -1.1f));
            Assert.AreEqual(first, second);
            Assert.AreEqual(new Vector3(4.5f, 0.5f, -1.5f), first);
        }

        [Test]
        public void 칸_크기를_바꾸면_그_크기에_맞춰_계산한다()
        {
            Assert.AreEqual(new Vector3Int(1, 0, 2), GridMath.WorldToCell(new Vector3(0.6f, 0.1f, 1.2f), 0.5f));
            Assert.AreEqual(new Vector3(0.75f, 0.25f, 1.25f), GridMath.CellToWorldCenter(new Vector3Int(1, 0, 2), 0.5f));
        }

        [Test]
        public void 칸_크기가_0_이하여도_오류_없이_계산한다()
        {
            Assert.DoesNotThrow(() => GridMath.WorldToCell(new Vector3(1f, 1f, 1f), 0f));
            Assert.DoesNotThrow(() => GridMath.CellToWorldCenter(Vector3Int.one, -1f));
        }
    }
}
