using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 블록 기록의 규칙 검사. 블록은 칸에 맞추지 않고 어디에나 놓이며 번호로 가리킨다. 범위, 개수 상한, 번호, 자리 다듬기를 확인한다.
    /// </summary>
    public class BlockMapTests
    {
        // 범위는 x·z로 -2~3, 높이 0~4인 상자다. 표준 블록(한 변 1)의 가운데는 -1.5~2.5, 높이 0.5~3.5까지 놓일 수 있다.
        private static BlockMap Create(int capacity = 10)
        {
            return new BlockMap(new Vector3(-2f, 0f, -2f), new Vector3(3f, 4f, 3f), capacity);
        }

        private static int Add(BlockMap map, Vector3 position, int part = 0)
        {
            Assert.AreEqual(PlaceResult.Ok, map.Add(part, position, Quaternion.identity, out int id));
            return id;
        }

        [Test]
        public void 칸에_맞지_않는_자리에도_놓이고_번호가_붙는다()
        {
            BlockMap map = Create();

            Assert.AreEqual(PlaceResult.Ok, map.Add(3, new Vector3(1.37f, 0.5f, -0.82f), Quaternion.identity, out int id));
            Assert.AreEqual(1, id, "번호는 1부터 붙습니다.");
            Assert.AreEqual(1, map.Count);
            Assert.IsTrue(map.TryGet(id, out BlockRecord record));
            Assert.AreEqual(3, record.Part);
            Assert.Less(Vector3.Distance(new Vector3(1.37f, 0.5f, -0.82f), record.Position), 0.0001f, "놓은 자리가 칸의 가운데로 당겨지면 안 됩니다.");

            Assert.AreEqual(2, Add(map, new Vector3(0f, 0.5f, 0f)));
        }

        [Test]
        public void 블록끼리_겹쳐도_놓이지만_가운데가_같은_자리에는_놓을_수_없다()
        {
            BlockMap map = Create();
            int first = Add(map, new Vector3(0.5f, 0.5f, 0.5f));

            Assert.AreEqual(PlaceResult.Ok, map.Add(1, new Vector3(0.8f, 0.5f, 0.5f), Quaternion.identity, out _), "겹치는 자리에도 놓을 수 있어야 합니다.");
            Assert.AreEqual(PlaceResult.Occupied, map.Add(1, new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, out int rejected));
            Assert.AreEqual(BlockMap.NoId, rejected);
            Assert.AreEqual(PlaceResult.Occupied, map.Add(1, new Vector3(0.5002f, 0.5f, 0.5f), Quaternion.identity, out _), "1mm보다 가까우면 같은 자리로 봅니다.");

            Assert.IsTrue(map.TryGet(first, out BlockRecord record));
            Assert.AreEqual(0, record.Part, "놓지 못한 부품으로 바뀌면 안 됩니다.");
        }

        [Test]
        public void 범위_밖에는_놓을_수_없다()
        {
            BlockMap map = Create();

            Assert.AreEqual(PlaceResult.OutOfBounds, map.Check(new Vector3(2.6f, 0.5f, 0f)), "블록의 한쪽이 범위 밖으로 나가면 안 됩니다.");
            Assert.AreEqual(PlaceResult.OutOfBounds, map.Check(new Vector3(0f, 0.4f, 0f)), "바닥 아래로 묻히면 안 됩니다.");
            Assert.AreEqual(PlaceResult.OutOfBounds, map.Check(new Vector3(0f, 3.6f, 0f)));
            Assert.AreEqual(PlaceResult.Ok, map.Check(new Vector3(2.5f, 3.5f, -1.5f)), "범위에 꼭 맞게 닿는 자리에는 놓을 수 있어야 합니다.");
        }

        [Test]
        public void 상한에_닿으면_더_놓을_수_없고_지우면_다시_놓을_수_있다()
        {
            BlockMap map = Create(2);
            int first = Add(map, new Vector3(0f, 0.5f, 0f));
            Add(map, new Vector3(1f, 0.5f, 0f));

            Assert.AreEqual(PlaceResult.Full, map.Add(0, new Vector3(2f, 0.5f, 0f), Quaternion.identity, out _));
            Assert.IsTrue(map.Remove(first));
            Assert.AreEqual(PlaceResult.Ok, map.Add(0, new Vector3(2f, 0.5f, 0f), Quaternion.identity, out int id));
            Assert.AreEqual(3, id, "지운 블록의 번호를 다시 쓰지 않습니다.");
        }

        [Test]
        public void 없는_블록은_지울_수_없다()
        {
            BlockMap map = Create();

            Assert.IsFalse(map.Remove(1));
            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void 미리_확인하는_것만으로는_기록이_바뀌지_않는다()
        {
            BlockMap map = Create();

            Assert.AreEqual(PlaceResult.Ok, map.Check(new Vector3(0f, 0.5f, 0f)));
            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void 부품_번호가_음수이면_놓을_수_없다()
        {
            BlockMap map = Create();

            Assert.AreEqual(PlaceResult.UnknownPart, map.Add(-1, new Vector3(0f, 0.5f, 0f), Quaternion.identity, out _));
            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void 있는_블록의_부품과_자리와_방향을_고칠_수_있다()
        {
            BlockMap map = Create();
            Assert.IsFalse(map.Set(new BlockRecord(1, 1, new Vector3(0f, 0.5f, 0f), Quaternion.identity)), "없는 블록은 고칠 수 없어야 합니다.");

            int id = Add(map, new Vector3(0f, 0.5f, 0f));
            int other = Add(map, new Vector3(1f, 0.5f, 0f));

            Assert.IsTrue(map.Set(new BlockRecord(id, 2, new Vector3(-0.25f, 1.5f, 0.75f), Quaternion.Euler(0f, 30f, 0f))));
            Assert.IsTrue(map.TryGet(id, out BlockRecord record));
            Assert.AreEqual(2, record.Part);
            Assert.Less(Vector3.Distance(new Vector3(-0.25f, 1.5f, 0.75f), record.Position), 0.0001f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 30f, 0f), record.Rotation), 0.1f);
            Assert.AreEqual(2, map.Count, "고치기는 블록 수를 바꾸지 않습니다.");

            Assert.IsFalse(map.Set(new BlockRecord(id, -1, record.Position, record.Rotation)));
            Assert.IsFalse(map.Set(new BlockRecord(id, 2, new Vector3(9f, 0.5f, 0f), Quaternion.identity)), "범위 밖으로는 옮길 수 없어야 합니다.");
            Assert.IsFalse(map.Set(new BlockRecord(id, 2, new Vector3(1f, 0.5f, 0f), Quaternion.identity)), "다른 블록의 가운데와 같은 자리로는 옮길 수 없어야 합니다.");
            Assert.AreEqual(PlaceResult.Ok, map.Check(new Vector3(0f, 0.5f, 0f)), "옮기고 난 원래 자리는 비어 있어야 합니다.");
            Assert.IsTrue(map.Contains(other));
        }

        [Test]
        public void 지운_블록은_같은_번호로_되살릴_수_있다()
        {
            BlockMap map = Create();
            int id = Add(map, new Vector3(0.3f, 0.5f, 0.3f), 4);
            Assert.IsTrue(map.TryGet(id, out BlockRecord record));
            map.Remove(id);

            Assert.AreEqual(PlaceResult.Ok, map.Restore(record));
            Assert.IsTrue(map.TryGet(id, out BlockRecord restored));
            Assert.AreEqual(4, restored.Part);

            Assert.AreEqual(PlaceResult.Occupied, map.Restore(record), "이미 있는 번호로는 되살릴 수 없습니다.");
            Assert.AreEqual(PlaceResult.Occupied, map.Restore(new BlockRecord(0, 0, new Vector3(1f, 0.5f, 1f), Quaternion.identity)), "번호는 1부터입니다.");

            Assert.AreEqual(PlaceResult.Ok, map.Restore(new BlockRecord(40, 0, new Vector3(1f, 0.5f, 1f), Quaternion.identity)));
            Assert.AreEqual(41, Add(map, new Vector3(2f, 0.5f, 2f)), "되살린 번호보다 큰 번호부터 새로 붙여야 합니다.");
        }

        [Test]
        public void 자리는_1mm_단위로_다듬어_기록한다()
        {
            BlockMap map = Create();
            int id = Add(map, new Vector3(0.12345f, 0.5f, -1.00049f));

            Assert.IsTrue(map.TryGet(id, out BlockRecord record));
            Assert.AreEqual(0.123f, record.Position.x, 0.00001f);
            Assert.AreEqual(-1f, record.Position.z, 0.00001f);
        }

        [Test]
        public void 가리킨_면_위의_그_자리에_반_변만큼_띄워_얹는다()
        {
            Vector3 onFloor = BlockMap.RestOn(new Vector3(1.37f, 0f, -0.82f), Vector3.up);
            Assert.Less(Vector3.Distance(new Vector3(1.37f, 0.5f, -0.82f), onFloor), 0.0001f, "바닥을 가리키면 가리킨 자리 바로 위에 놓여야 합니다.");

            Vector3 onSide = BlockMap.RestOn(new Vector3(2f, 0.7f, 0.31f), Vector3.right);
            Assert.Less(Vector3.Distance(new Vector3(2.5f, 0.7f, 0.31f), onSide), 0.0001f, "옆면을 가리키면 그 면에 붙어 놓여야 합니다.");
        }

        [Test]
        public void 바닥에_묻히는_자리는_높이만_바닥_위로_올린다()
        {
            BlockMap map = Create();

            Vector3 raised = map.ClampHeight(new Vector3(1.2f, 0.2f, -0.4f));
            Assert.AreEqual(0.5f, raised.y, 0.0001f);
            Assert.AreEqual(1.2f, raised.x, 0.0001f, "좌우 자리는 바꾸지 않습니다.");
            Assert.AreEqual(3.5f, map.ClampHeight(new Vector3(0f, 9f, 0f)).y, 0.0001f);
            Assert.AreEqual(1.7f, map.ClampHeight(new Vector3(0f, 1.7f, 0f)).y, 0.0001f);
        }

        [Test]
        public void 가까운_블록을_자리로_찾을_수_있다()
        {
            BlockMap map = Create();
            Add(map, new Vector3(0f, 0.5f, 0f), 1);
            int near = Add(map, new Vector3(1f, 0.5f, 0f), 2);

            Assert.IsTrue(map.TryFindNear(new Vector3(0.9f, 0.5f, 0f), 0.3f, out BlockRecord found));
            Assert.AreEqual(near, found.Id);
            Assert.IsFalse(map.TryFindNear(new Vector3(0.5f, 2.5f, 0f), 0.3f, out _));
        }

        [Test]
        public void 범위의_두_모서리를_거꾸로_주어도_같은_범위가_된다()
        {
            var map = new BlockMap(new Vector3(3f, 4f, 3f), new Vector3(-2f, 0f, -2f), 10);

            Assert.IsTrue(map.InBounds(new Vector3(-1.5f, 0.5f, -1.5f)));
            Assert.IsTrue(map.InBounds(new Vector3(2.5f, 3.5f, 2.5f)));
            Assert.IsFalse(map.InBounds(new Vector3(3f, 0.5f, 0f)));
        }

        [Test]
        public void 옮길_수_있는지는_제자리를_막지_않고_상한도_보지_않는다()
        {
            BlockMap map = Create(2);
            int first = Add(map, new Vector3(0f, 0.5f, 0f));
            int second = Add(map, new Vector3(1f, 0.5f, 0f));

            Assert.AreEqual(PlaceResult.Full, map.Check(new Vector3(2f, 0.5f, 0f)), "상한에 닿았으면 새로 놓을 수는 없습니다.");
            Assert.AreEqual(PlaceResult.Ok, map.CheckMove(first, new Vector3(2f, 0.5f, 0f)), "옮기기는 블록 수가 늘지 않으므로 상한에 걸리지 않습니다.");
            Assert.AreEqual(PlaceResult.Ok, map.CheckMove(first, new Vector3(0f, 0.5f, 0f)), "제자리는 막지 않습니다.");
            Assert.AreEqual(PlaceResult.Occupied, map.CheckMove(first, new Vector3(1f, 0.5f, 0f)), "다른 블록의 가운데와 같은 자리로는 옮길 수 없습니다.");
            Assert.AreEqual(PlaceResult.OutOfBounds, map.CheckMove(second, new Vector3(9f, 0.5f, 0f)));
            Assert.AreEqual(2, map.Count, "확인만 하고 기록은 바꾸지 않습니다.");
        }

        [Test]
        public void 모두_지우면_번호도_처음부터_다시_붙는다()
        {
            BlockMap map = Create();
            Add(map, new Vector3(0f, 0.5f, 0f));
            Add(map, new Vector3(1f, 0.5f, 0f));

            map.Clear();

            Assert.AreEqual(0, map.Count);
            Assert.AreEqual(1, Add(map, new Vector3(0f, 0.5f, 0f)));
        }
    }
}
