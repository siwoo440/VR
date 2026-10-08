using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 블록 기록의 규칙 검사. 범위, 겹침, 개수 상한을 확인한다.
    /// </summary>
    public class BlockMapTests
    {
        private static BlockMap Create(int capacity = 10)
        {
            return new BlockMap(new Vector3Int(-2, 0, -2), new Vector3Int(2, 3, 2), capacity);
        }

        [Test]
        public void 빈_칸에_놓으면_기록된다()
        {
            BlockMap map = Create();

            Assert.AreEqual(PlaceResult.Ok, map.Place(new Vector3Int(1, 0, -1), 3));
            Assert.AreEqual(1, map.Count);
            Assert.IsTrue(map.TryGet(new Vector3Int(1, 0, -1), out int part));
            Assert.AreEqual(3, part);
        }

        [Test]
        public void 이미_블록이_있는_칸에는_놓을_수_없다()
        {
            BlockMap map = Create();
            map.Place(Vector3Int.zero, 0);

            Assert.AreEqual(PlaceResult.Occupied, map.Place(Vector3Int.zero, 1));
            Assert.IsTrue(map.TryGet(Vector3Int.zero, out int part));
            Assert.AreEqual(0, part, "겹쳐 놓으려던 부품으로 바뀌면 안 됩니다.");
        }

        [Test]
        public void 범위_밖에는_놓을_수_없다()
        {
            BlockMap map = Create();

            Assert.AreEqual(PlaceResult.OutOfBounds, map.Place(new Vector3Int(3, 0, 0), 0));
            Assert.AreEqual(PlaceResult.OutOfBounds, map.Place(new Vector3Int(0, -1, 0), 0));
            Assert.AreEqual(PlaceResult.OutOfBounds, map.Place(new Vector3Int(0, 4, 0), 0));
            Assert.AreEqual(PlaceResult.Ok, map.Place(new Vector3Int(2, 3, -2), 0), "범위의 가장자리 칸에는 놓을 수 있어야 합니다.");
        }

        [Test]
        public void 상한에_닿으면_더_놓을_수_없고_지우면_다시_놓을_수_있다()
        {
            BlockMap map = Create(2);
            map.Place(new Vector3Int(0, 0, 0), 0);
            map.Place(new Vector3Int(1, 0, 0), 0);

            Assert.AreEqual(PlaceResult.Full, map.Place(new Vector3Int(2, 0, 0), 0));
            Assert.IsTrue(map.Remove(new Vector3Int(0, 0, 0)));
            Assert.AreEqual(PlaceResult.Ok, map.Place(new Vector3Int(2, 0, 0), 0));
        }

        [Test]
        public void 없는_블록은_지울_수_없다()
        {
            BlockMap map = Create();

            Assert.IsFalse(map.Remove(new Vector3Int(1, 1, 1)));
            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void 미리_확인하는_것만으로는_기록이_바뀌지_않는다()
        {
            BlockMap map = Create();

            Assert.AreEqual(PlaceResult.Ok, map.Check(Vector3Int.zero));
            Assert.AreEqual(0, map.Count);
            Assert.IsFalse(map.Contains(Vector3Int.zero));
        }

        [Test]
        public void 부품_번호가_음수이면_놓을_수_없다()
        {
            BlockMap map = Create();

            Assert.AreEqual(PlaceResult.UnknownPart, map.Place(Vector3Int.zero, -1));
            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void 범위의_두_모서리를_거꾸로_주어도_같은_범위가_된다()
        {
            var map = new BlockMap(new Vector3Int(2, 3, 2), new Vector3Int(-2, 0, -2), 10);

            Assert.IsTrue(map.InBounds(new Vector3Int(-2, 0, -2)));
            Assert.IsTrue(map.InBounds(new Vector3Int(2, 3, 2)));
            Assert.IsFalse(map.InBounds(new Vector3Int(3, 0, 0)));
        }
    }
}
