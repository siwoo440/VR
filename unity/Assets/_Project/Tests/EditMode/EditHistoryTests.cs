using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 되돌리기 기록 층의 검사. 블록 기록(BlockMap)을 바탕으로 삼아 놓기·지우기를 되돌리고 다시 실행하는 규칙을 확인한다.
    /// </summary>
    public class EditHistoryTests
    {
        private static readonly Vector3Int A = new Vector3Int(0, 0, 0);
        private static readonly Vector3Int B = new Vector3Int(1, 0, 0);
        private static readonly Vector3Int C = new Vector3Int(2, 0, 0);
        private static readonly Vector3Int D = new Vector3Int(0, 1, 0);

        private static BlockMap CreateMap(int capacity = 10)
        {
            return new BlockMap(new Vector3Int(-2, 0, -2), new Vector3Int(2, 3, 2), capacity);
        }

        [Test]
        public void 놓기를_되돌리면_사라지고_다시_실행하면_돌아온다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);

            Assert.AreEqual(PlaceResult.Ok, history.Place(A, 2));
            Assert.IsTrue(history.CanUndo);
            Assert.IsFalse(history.CanRedo);

            Assert.IsTrue(history.Undo());
            Assert.IsFalse(map.Contains(A));
            Assert.IsFalse(history.CanUndo);
            Assert.IsTrue(history.CanRedo);

            Assert.IsTrue(history.Redo());
            Assert.IsTrue(map.TryGet(A, out int part));
            Assert.AreEqual(2, part);
            Assert.IsFalse(history.CanRedo);
        }

        [Test]
        public void 지우기를_되돌리면_같은_부품으로_돌아온다()
        {
            BlockMap map = CreateMap();
            map.Place(A, 4);
            var history = new EditHistory(map);

            Assert.IsTrue(history.Remove(A));
            Assert.IsFalse(map.Contains(A));

            Assert.IsTrue(history.Undo());
            Assert.IsTrue(map.TryGet(A, out int part));
            Assert.AreEqual(4, part);

            Assert.IsTrue(history.Redo());
            Assert.IsFalse(map.Contains(A));
        }

        [Test]
        public void 새_편집을_하면_다시_실행_기록이_비워진다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            history.Place(A, 0);
            history.Undo();
            Assert.IsTrue(history.CanRedo);

            history.Place(B, 1);

            Assert.IsFalse(history.CanRedo);
            Assert.IsFalse(history.Redo());
            Assert.IsFalse(map.Contains(A));
        }

        [Test]
        public void 되돌릴_것이_없으면_아무_일도_하지_않는다()
        {
            var history = new EditHistory(CreateMap());

            Assert.IsFalse(history.Undo());
            Assert.IsFalse(history.Redo());
        }

        [Test]
        public void 놓지_못한_편집과_없는_블록_지우기는_기록되지_않는다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);

            Assert.AreEqual(PlaceResult.OutOfBounds, history.Place(new Vector3Int(9, 0, 0), 0));
            Assert.IsFalse(history.Remove(B));
            Assert.IsFalse(history.CanUndo);
        }

        [Test]
        public void 여러_번_되돌리면_거꾸로_하나씩_돌아간다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            history.Place(A, 0);
            history.Place(B, 1);
            history.Remove(A);

            Assert.IsTrue(history.Undo());
            Assert.IsTrue(map.Contains(A), "지우기가 먼저 되돌아가야 합니다.");
            Assert.IsTrue(history.Undo());
            Assert.IsFalse(map.Contains(B));
            Assert.IsTrue(history.Undo());
            Assert.IsFalse(map.Contains(A));
            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void 기록은_상한을_넘으면_오래된_것부터_버린다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map, 3);
            history.Place(A, 0);
            history.Place(B, 0);
            history.Place(C, 0);
            history.Place(D, 0);

            Assert.AreEqual(3, history.UndoCount);
            while (history.Undo())
            {
            }

            Assert.IsTrue(map.Contains(A), "가장 오래된 편집은 버려져 되돌아가지 않아야 합니다.");
            Assert.AreEqual(1, map.Count);
        }

        [Test]
        public void 상한이_가득_차_되돌리지_못하면_기록을_남긴다()
        {
            BlockMap map = CreateMap(2);
            var history = new EditHistory(map);
            history.Place(A, 0);
            history.Remove(A);
            map.Place(B, 0);
            map.Place(C, 0);

            Assert.IsFalse(history.Undo(), "상한이 가득 차 블록을 되살리지 못해야 합니다.");
            Assert.AreEqual(2, history.UndoCount);
            Assert.IsFalse(history.CanRedo);
        }

        [Test]
        public void 기록을_거치지_않고_바뀐_칸이_있어도_되돌리기가_막히지_않는다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            history.Place(A, 0);
            map.Remove(A);

            Assert.IsTrue(history.Undo(), "이미 없는 블록을 지우는 되돌리기는 성공으로 봐야 합니다.");
            Assert.IsFalse(map.Contains(A));

            history.Place(B, 1);
            map.Remove(B);
            map.Place(B, 3);
            Assert.IsTrue(history.Undo());
            Assert.IsFalse(map.Contains(B), "다른 부품이 있어도 놓기를 되돌리면 비워야 합니다.");
        }

        [Test]
        public void 비우면_되돌리기와_다시_실행이_모두_사라진다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            history.Place(A, 0);
            history.Place(B, 0);
            history.Undo();

            history.Clear();

            Assert.IsFalse(history.CanUndo);
            Assert.IsFalse(history.CanRedo);
            Assert.IsTrue(map.Contains(A), "비우기는 블록을 건드리지 않습니다.");
        }

        [Test]
        public void 기록이_바뀔_때마다_알린다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            int changes = 0;
            history.Changed += () => changes++;

            history.Place(A, 0);
            history.Remove(B);
            history.Undo();
            history.Redo();
            history.Redo();
            history.Clear();
            history.Clear();

            Assert.AreEqual(4, changes);
        }
    }
}
