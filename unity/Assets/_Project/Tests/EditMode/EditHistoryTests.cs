using System.Collections.Generic;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 되돌리기 기록 층의 검사. 블록 기록(BlockMap)을 바탕으로 삼아 놓기·지우기·칠하기·옮기기를 되돌리고 다시 실행하는 규칙을 확인한다.
    /// 블록은 번호로 가리키며, 되살린 블록은 같은 번호로 돌아온다.
    /// </summary>
    public class EditHistoryTests
    {
        // 칸에 맞지 않는 자리들.
        private static readonly Vector3 A = new Vector3(0.1f, 0.5f, 0.2f);
        private static readonly Vector3 B = new Vector3(1.3f, 0.5f, -0.4f);
        private static readonly Vector3 C = new Vector3(2.2f, 0.5f, 0.7f);
        private static readonly Vector3 D = new Vector3(0.1f, 1.5f, 0.2f);

        private static BlockMap CreateMap(int capacity = 10)
        {
            return new BlockMap(new Vector3(-2f, 0f, -2f), new Vector3(3f, 4f, 3f), capacity);
        }

        [Test]
        public void 편집이_이루어지면_무엇을_했는지와_어느_블록인지_알린다()
        {
            var history = new EditHistory(CreateMap());
            var heard = new List<(EditKind kind, BlockChange change)>();
            history.Edited += (kind, change) => heard.Add((kind, change));

            int id = Place(history, A, 0);
            Assert.IsTrue(history.Replace(id, 1));
            Assert.IsTrue(history.Move(id, B, Quaternion.identity));
            Assert.IsTrue(history.Undo());
            Assert.IsTrue(history.Redo());
            Assert.IsTrue(history.Remove(id));

            var kinds = new List<EditKind>();
            foreach ((EditKind kind, BlockChange _) in heard) kinds.Add(kind);
            CollectionAssert.AreEqual(new[] { EditKind.Place, EditKind.Paint, EditKind.Move, EditKind.Undo, EditKind.Redo, EditKind.Remove }, kinds);

            // 소리를 그 자리에서 내려면 블록의 자리를 알아야 한다.
            Assert.IsTrue(heard[0].change.HasAfter);
            Assert.Less(Vector3.Distance(A, heard[0].change.After.Position), 0.001f);
            Assert.AreEqual(1, heard[1].change.After.Part);
            Assert.Less(Vector3.Distance(B, heard[2].change.After.Position), 0.001f);
            Assert.AreEqual(id, heard[3].change.Id, "되돌리기는 원래 편집의 변화를 함께 알립니다.");
            Assert.IsTrue(heard[5].change.HadBefore);
            Assert.IsFalse(heard[5].change.HasAfter);
            Assert.Less(Vector3.Distance(B, heard[5].change.Before.Position), 0.001f);
        }

        [Test]
        public void 이루어지지_않은_편집은_알리지_않는다()
        {
            var history = new EditHistory(CreateMap(1));
            int count = 0;
            history.Edited += (_, _) => count++;

            int id = Place(history, A, 0);
            Assert.AreEqual(1, count);

            Assert.AreEqual(PlaceResult.Full, history.Place(0, C, Quaternion.identity, out _));
            Assert.IsFalse(history.Replace(id, 0), "이미 같은 부품입니다.");
            Assert.IsFalse(history.Move(id, A, Quaternion.identity), "이미 그 자리입니다.");
            Assert.IsFalse(history.Remove(999));
            Assert.IsFalse(history.Redo());
            Assert.AreEqual(1, count, "이루어지지 않은 편집에 소리가 나면 안 됩니다.");

            Assert.IsTrue(history.Undo());
            Assert.IsFalse(history.Undo(), "되돌릴 것이 없습니다.");
            Assert.AreEqual(2, count);
        }

        private static int Place(EditHistory history, Vector3 position, int part)
        {
            Assert.AreEqual(PlaceResult.Ok, history.Place(part, position, Quaternion.identity, out int id));
            return id;
        }

        private static int Add(BlockMap map, Vector3 position, int part)
        {
            Assert.AreEqual(PlaceResult.Ok, map.Add(part, position, Quaternion.identity, out int id));
            return id;
        }

        [Test]
        public void 놓기를_되돌리면_사라지고_다시_실행하면_같은_번호로_돌아온다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);

            int id = Place(history, A, 2);
            Assert.IsTrue(history.CanUndo);
            Assert.IsFalse(history.CanRedo);

            Assert.IsTrue(history.Undo());
            Assert.IsFalse(map.Contains(id));
            Assert.IsFalse(history.CanUndo);
            Assert.IsTrue(history.CanRedo);

            Assert.IsTrue(history.Redo());
            Assert.IsTrue(map.TryGet(id, out BlockRecord record), "다시 실행한 블록은 같은 번호여야 합니다.");
            Assert.AreEqual(2, record.Part);
            Assert.Less(Vector3.Distance(A, record.Position), 0.0001f);
            Assert.IsFalse(history.CanRedo);
        }

        [Test]
        public void 지우기를_되돌리면_같은_부품과_자리로_돌아온다()
        {
            BlockMap map = CreateMap();
            int id = Add(map, A, 4);
            var history = new EditHistory(map);

            Assert.IsTrue(history.Remove(id));
            Assert.IsFalse(map.Contains(id));

            Assert.IsTrue(history.Undo());
            Assert.IsTrue(map.TryGet(id, out BlockRecord record));
            Assert.AreEqual(4, record.Part);
            Assert.Less(Vector3.Distance(A, record.Position), 0.0001f);

            Assert.IsTrue(history.Redo());
            Assert.IsFalse(map.Contains(id));
        }

        [Test]
        public void 새_편집을_하면_다시_실행_기록이_비워진다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            int first = Place(history, A, 0);
            history.Undo();
            Assert.IsTrue(history.CanRedo);

            Place(history, B, 1);

            Assert.IsFalse(history.CanRedo);
            Assert.IsFalse(history.Redo());
            Assert.IsFalse(map.Contains(first));
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

            Assert.AreEqual(PlaceResult.OutOfBounds, history.Place(0, new Vector3(9f, 0.5f, 0f)));
            Assert.IsFalse(history.Remove(7));
            Assert.IsFalse(history.CanUndo);
        }

        [Test]
        public void 여러_번_되돌리면_거꾸로_하나씩_돌아간다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            int a = Place(history, A, 0);
            int b = Place(history, B, 1);
            history.Remove(a);

            Assert.IsTrue(history.Undo());
            Assert.IsTrue(map.Contains(a), "지우기가 먼저 되돌아가야 합니다.");
            Assert.IsTrue(history.Undo());
            Assert.IsFalse(map.Contains(b));
            Assert.IsTrue(history.Undo());
            Assert.IsFalse(map.Contains(a));
            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void 되살린_블록을_가리키는_뒤의_기록도_계속_맞는다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            int id = Place(history, A, 0);
            history.Replace(id, 3);
            history.Remove(id);

            // 지우기, 칠하기, 놓기를 모두 되돌렸다가 처음부터 다시 실행한다.
            Assert.IsTrue(history.Undo());
            Assert.IsTrue(history.Undo());
            Assert.IsTrue(history.Undo());
            Assert.AreEqual(0, map.Count);

            Assert.IsTrue(history.Redo());
            Assert.IsTrue(history.Redo());
            Assert.IsTrue(map.TryGet(id, out BlockRecord painted), "되살린 블록의 번호가 달라지면 뒤의 칠하기가 맞지 않습니다.");
            Assert.AreEqual(3, painted.Part);
            Assert.IsTrue(history.Redo());
            Assert.IsFalse(map.Contains(id));
        }

        [Test]
        public void 기록은_상한을_넘으면_오래된_것부터_버린다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map, 3);
            int a = Place(history, A, 0);
            Place(history, B, 0);
            Place(history, C, 0);
            Place(history, D, 0);

            Assert.AreEqual(3, history.UndoCount);
            while (history.Undo())
            {
            }

            Assert.IsTrue(map.Contains(a), "가장 오래된 편집은 버려져 되돌아가지 않아야 합니다.");
            Assert.AreEqual(1, map.Count);
        }

        [Test]
        public void 상한이_가득_차_되돌리지_못하면_기록을_남긴다()
        {
            BlockMap map = CreateMap(2);
            var history = new EditHistory(map);
            int a = Place(history, A, 0);
            history.Remove(a);
            Add(map, B, 0);
            Add(map, C, 0);

            Assert.IsFalse(history.Undo(), "상한이 가득 차 블록을 되살리지 못해야 합니다.");
            Assert.AreEqual(2, history.UndoCount);
            Assert.IsFalse(history.CanRedo);
        }

        [Test]
        public void 기록을_거치지_않고_바뀐_블록이_있어도_되돌리기가_막히지_않는다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            int a = Place(history, A, 0);
            map.Remove(a);

            Assert.IsTrue(history.Undo(), "이미 없는 블록을 지우는 되돌리기는 성공으로 봐야 합니다.");
            Assert.IsFalse(map.Contains(a));

            int b = Place(history, B, 1);
            Assert.IsTrue(map.TryGet(b, out BlockRecord record));
            record.Part = 3;
            map.Set(record);
            Assert.IsTrue(history.Undo());
            Assert.IsFalse(map.Contains(b), "다른 부품으로 바뀌어 있어도 놓기를 되돌리면 지워야 합니다.");
        }

        [Test]
        public void 칠하기를_되돌리면_이전_부품으로_돌아온다()
        {
            BlockMap map = CreateMap();
            int id = Add(map, A, 0);
            var history = new EditHistory(map);

            Assert.IsTrue(history.Replace(id, 2));
            Assert.AreEqual(2, PartOf(map, id));

            Assert.IsTrue(history.Undo());
            Assert.AreEqual(0, PartOf(map, id));

            Assert.IsTrue(history.Redo());
            Assert.AreEqual(2, PartOf(map, id));
        }

        [Test]
        public void 같은_부품이나_없는_블록은_칠하기가_기록되지_않는다()
        {
            BlockMap map = CreateMap();
            int id = Add(map, A, 0);
            var history = new EditHistory(map);

            Assert.IsFalse(history.Replace(id, 0));
            Assert.IsFalse(history.Replace(id + 5, 1));
            Assert.IsFalse(history.CanUndo);
        }

        [Test]
        public void 칠한_뒤_지우기를_되돌리면_칠한_부품으로_돌아온다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            int id = Place(history, A, 0);
            history.Replace(id, 3);
            history.Remove(id);

            Assert.IsTrue(history.Undo());
            Assert.AreEqual(3, PartOf(map, id));
        }

        [Test]
        public void 옮기기를_되돌리면_원래_자리와_방향으로_돌아온다()
        {
            BlockMap map = CreateMap();
            int id = Add(map, A, 1);
            var history = new EditHistory(map);
            Quaternion turned = Quaternion.Euler(0f, 45f, 0f);

            Assert.IsTrue(history.Move(id, C, turned));
            Assert.IsTrue(map.TryGet(id, out BlockRecord moved));
            Assert.Less(Vector3.Distance(C, moved.Position), 0.0001f);
            Assert.Less(Quaternion.Angle(turned, moved.Rotation), 0.1f);
            Assert.AreEqual(1, moved.Part, "옮기기는 부품을 바꾸지 않습니다.");

            Assert.IsTrue(history.Undo());
            Assert.IsTrue(map.TryGet(id, out BlockRecord back));
            Assert.Less(Vector3.Distance(A, back.Position), 0.0001f);
            Assert.Less(Quaternion.Angle(Quaternion.identity, back.Rotation), 0.1f);

            Assert.IsTrue(history.Redo());
            Assert.IsTrue(map.TryGet(id, out BlockRecord again));
            Assert.Less(Vector3.Distance(C, again.Position), 0.0001f);
        }

        [Test]
        public void 제자리로_옮기거나_범위_밖으로_옮기면_기록되지_않는다()
        {
            BlockMap map = CreateMap();
            int id = Add(map, A, 0);
            var history = new EditHistory(map);

            Assert.IsFalse(history.Move(id, A, Quaternion.identity));
            Assert.IsFalse(history.Move(id, new Vector3(9f, 0.5f, 0f), Quaternion.identity));
            Assert.IsFalse(history.Move(id + 5, B, Quaternion.identity));
            Assert.IsFalse(history.CanUndo);
            Assert.IsTrue(map.TryGet(id, out BlockRecord record));
            Assert.Less(Vector3.Distance(A, record.Position), 0.0001f);
        }

        [Test]
        public void 비우면_되돌리기와_다시_실행이_모두_사라진다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            int a = Place(history, A, 0);
            Place(history, B, 0);
            history.Undo();

            history.Clear();

            Assert.IsFalse(history.CanUndo);
            Assert.IsFalse(history.CanRedo);
            Assert.IsTrue(map.Contains(a), "비우기는 블록을 건드리지 않습니다.");
        }

        [Test]
        public void 기록이_바뀔_때마다_알린다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            int changes = 0;
            history.Changed += () => changes++;

            Place(history, A, 0);
            history.Remove(99);
            history.Undo();
            history.Redo();
            history.Redo();
            history.Clear();
            history.Clear();

            Assert.AreEqual(4, changes);
        }

        [Test]
        public void 묶음으로_한_편집은_되돌리기_한_번에_함께_돌아온다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            int single = Place(history, D, 0);

            history.BeginGroup();
            Assert.IsTrue(history.IsGrouping);
            int a = Place(history, A, 1);
            int b = Place(history, B, 1);
            int c = Place(history, C, 1);
            Assert.AreEqual(3, history.GroupSize);
            Assert.AreEqual(1, history.UndoCount, "묶음은 닫을 때 기록 하나가 됩니다.");

            Assert.IsTrue(history.EndGroup());
            Assert.IsFalse(history.IsGrouping);
            Assert.AreEqual(0, history.GroupSize);
            Assert.AreEqual(2, history.UndoCount);
            Assert.AreEqual(4, map.Count);

            Assert.IsTrue(history.Undo());
            Assert.AreEqual(1, map.Count, "묶음의 셋이 한 번에 돌아와야 합니다.");
            Assert.IsTrue(map.Contains(single));
            Assert.AreEqual(1, history.UndoCount);
            Assert.AreEqual(1, history.RedoCount);

            Assert.IsTrue(history.Redo());
            Assert.AreEqual(4, map.Count);
            Assert.IsTrue(map.Contains(a) && map.Contains(b) && map.Contains(c), "다시 실행하면 같은 번호로 돌아와야 합니다.");
            Assert.AreEqual(2, history.UndoCount);
            Assert.AreEqual(0, history.RedoCount);
        }

        [Test]
        public void 묶음에는_놓기와_지우기와_칠하기가_섞여도_된다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            int a = Place(history, A, 0);
            int b = Place(history, B, 0);

            history.BeginGroup();
            Assert.IsTrue(history.Replace(a, 2));
            Assert.IsTrue(history.Remove(b));
            int c = Place(history, C, 1);
            Assert.IsTrue(history.EndGroup());
            Assert.AreEqual(3, history.UndoCount);

            Assert.IsTrue(history.Undo());
            Assert.AreEqual(0, PartOf(map, a));
            Assert.IsTrue(map.Contains(b), "지운 블록이 같은 번호로 돌아와야 합니다.");
            Assert.IsFalse(map.Contains(c));

            Assert.IsTrue(history.Redo());
            Assert.AreEqual(2, PartOf(map, a));
            Assert.IsFalse(map.Contains(b));
            Assert.IsTrue(map.Contains(c));
        }

        [Test]
        public void 빈_묶음은_기록을_남기지_않고_편집_하나짜리_묶음은_기록_하나다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);

            Assert.IsFalse(history.EndGroup(), "열지 않은 묶음을 닫아도 탈이 없어야 합니다.");

            history.BeginGroup();
            Assert.IsFalse(history.EndGroup());
            Assert.AreEqual(0, history.UndoCount);

            history.BeginGroup();
            history.BeginGroup();
            Place(history, A, 0);
            Assert.IsTrue(history.EndGroup());
            Assert.IsFalse(history.EndGroup(), "겹쳐 열어도 묶음은 하나입니다.");
            Assert.AreEqual(1, history.UndoCount);

            Assert.IsTrue(history.Undo());
            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void 묶음을_연_채_되돌리면_그때까지의_묶음이_돌아온다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            int single = Place(history, D, 0);

            history.BeginGroup();
            Place(history, A, 1);
            Place(history, B, 1);

            Assert.IsTrue(history.Undo());
            Assert.IsFalse(history.IsGrouping, "되돌리면 열려 있던 묶음이 닫힙니다.");
            Assert.AreEqual(1, map.Count);
            Assert.IsTrue(map.Contains(single));
            Assert.AreEqual(1, history.UndoCount);
            Assert.AreEqual(1, history.RedoCount);

            // 그 뒤의 편집은 묶음이 아니다.
            Place(history, C, 0);
            Assert.AreEqual(2, history.UndoCount);
        }

        [Test]
        public void 묶음의_첫_편집에서_다시_실행할_기록을_버린다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            Place(history, A, 0);
            Assert.IsTrue(history.Undo());
            Assert.AreEqual(1, history.RedoCount);

            history.BeginGroup();
            Assert.AreEqual(1, history.RedoCount, "묶음을 열기만 해서는 버리지 않습니다.");

            Place(history, B, 0);
            Assert.AreEqual(0, history.RedoCount);
            history.EndGroup();
        }

        [Test]
        public void 기록의_상한은_묶음을_하나로_센다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map, 2);
            int oldest = Place(history, D, 0);

            history.BeginGroup();
            Place(history, A, 0);
            Place(history, B, 0);
            history.EndGroup();

            history.BeginGroup();
            Place(history, C, 0);
            Place(history, new Vector3(-1.2f, 0.5f, -1.1f), 0);
            Place(history, new Vector3(-1.2f, 1.5f, -1.1f), 0);
            history.EndGroup();

            Assert.AreEqual(2, history.UndoCount, "기록 셋 가운데 가장 오래된 하나를 버려야 합니다.");
            Assert.IsTrue(history.Undo());
            Assert.IsTrue(history.Undo());
            Assert.IsFalse(history.Undo());
            Assert.AreEqual(1, map.Count);
            Assert.IsTrue(map.Contains(oldest), "버린 기록의 블록은 되돌릴 수 없으므로 남습니다.");
        }

        [Test]
        public void 묶음_안의_편집은_하나씩_알리고_묶음을_되돌릴_때는_한_번만_알린다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);
            var heard = new List<(EditKind kind, BlockChange change)>();
            int counts = 0;
            history.Edited += (kind, change) => heard.Add((kind, change));
            history.Changed += () => counts++;

            history.BeginGroup();
            Place(history, A, 0);
            Place(history, B, 0);
            int last = Place(history, C, 0);
            Assert.AreEqual(3, heard.Count, "소리를 내려면 블록마다 알려야 합니다.");
            Assert.AreEqual(0, counts, "기록의 수는 묶음을 닫을 때 바뀝니다.");

            history.EndGroup();
            Assert.AreEqual(1, counts);

            Assert.IsTrue(history.Undo());
            Assert.IsTrue(history.Redo());

            var kinds = new List<EditKind>();
            foreach ((EditKind kind, BlockChange _) in heard) kinds.Add(kind);
            CollectionAssert.AreEqual(new[] { EditKind.Place, EditKind.Place, EditKind.Place, EditKind.Undo, EditKind.Redo }, kinds);
            Assert.AreEqual(last, heard[3].change.Id, "묶음을 되돌릴 때의 변화는 묶음의 마지막 것입니다.");
            Assert.AreEqual(3, counts);
        }

        [Test]
        public void 묶음을_다시_실행하다_막히면_실행하기_전의_상태로_둔다()
        {
            BlockMap map = CreateMap(3);
            var history = new EditHistory(map);

            history.BeginGroup();
            Place(history, A, 0);
            Place(history, B, 0);
            history.EndGroup();
            Assert.IsTrue(history.Undo());
            Assert.AreEqual(0, map.Count);

            // 기록을 거치지 않고 두 개가 놓여, 묶음의 둘 가운데 하나만 들어갈 자리가 남았다.
            Assert.AreEqual(PlaceResult.Ok, map.Add(0, C, Quaternion.identity, out int extra));
            Assert.AreEqual(PlaceResult.Ok, map.Add(0, D, Quaternion.identity, out _));

            Assert.IsFalse(history.Redo(), "둘을 다 놓을 수 없으면 다시 실행하지 못한 것입니다.");
            Assert.AreEqual(2, map.Count, "먼저 놓인 하나가 남아 있으면 안 됩니다.");
            Assert.AreEqual(1, history.RedoCount, "기록은 그대로 남아 다시 해 볼 수 있어야 합니다.");
            Assert.AreEqual(0, history.UndoCount);

            // 자리가 나면 다시 실행된다.
            Assert.IsTrue(map.Remove(extra));
            Assert.IsTrue(history.Redo());
            Assert.AreEqual(3, map.Count);
            Assert.AreEqual(0, history.RedoCount);
        }

        [Test]
        public void 기록을_비우면_열려_있던_묶음도_버린다()
        {
            BlockMap map = CreateMap();
            var history = new EditHistory(map);

            history.BeginGroup();
            Place(history, A, 0);
            history.Clear();

            Assert.IsFalse(history.IsGrouping);
            Assert.IsFalse(history.EndGroup());
            Assert.AreEqual(0, history.UndoCount);
            Assert.AreEqual(1, map.Count, "기록만 비우고 블록은 그대로 둡니다.");
        }

        private static int PartOf(BlockMap map, int id)
        {
            Assert.IsTrue(map.TryGet(id, out BlockRecord record));
            return record.Part;
        }
    }
}
