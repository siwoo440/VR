using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>블록 기록이 되돌리기에 내어 주는 최소한의 동작. BlockMap과 BlockWorld가 구현한다. 블록은 번호로 가리킨다.</summary>
    public interface IBlockStore
    {
        bool TryGet(int id, out BlockRecord record);

        /// <summary>블록을 새로 놓고 새 번호를 준다.</summary>
        PlaceResult Add(int partIndex, Vector3 position, Quaternion rotation, out int id);

        /// <summary>번호가 정해진 블록을 그대로 되살린다.</summary>
        PlaceResult Restore(BlockRecord record);

        bool Remove(int id);

        /// <summary>있는 블록의 부품, 자리, 방향을 바꾼다. 블록이 없거나 바꿀 수 없으면 false다.</summary>
        bool Set(BlockRecord record);
    }

    /// <summary>편집의 종류. 편집이 이루어졌을 때 무엇이 일어났는지 알리는 데 쓴다(소리 등).</summary>
    public enum EditKind
    {
        Place,
        Remove,
        Paint,
        Move,
        Undo,
        Redo,
    }

    /// <summary>블록 하나의 변화. 앞이나 뒤에 블록이 없었으면 HadBefore·HasAfter가 false다.</summary>
    public struct BlockChange
    {
        public int Id;
        public bool HadBefore;
        public BlockRecord Before;
        public bool HasAfter;
        public BlockRecord After;

        public static BlockChange Placed(BlockRecord after)
        {
            return new BlockChange { Id = after.Id, HasAfter = true, After = after };
        }

        public static BlockChange Removed(BlockRecord before)
        {
            return new BlockChange { Id = before.Id, HadBefore = true, Before = before };
        }

        public static BlockChange Changed(BlockRecord before, BlockRecord after)
        {
            return new BlockChange { Id = before.Id, HadBefore = true, Before = before, HasAfter = true, After = after };
        }
    }

    /// <summary>
    /// 블록 세계 위의 기록 층. 놓기·지우기·칠하기·옮기기를 하나씩 기록해 되돌리고(Undo) 다시 실행한다(Redo).
    /// 블록을 번호로 가리키므로, 지운 블록을 되돌리면 같은 번호로 돌아와 그 뒤의 기록이 계속 맞는다.
    /// 화면과 무관해서 편집 모드 테스트로 검사한다.
    /// </summary>
    public class EditHistory
    {
        public const int DefaultCapacity = 100;

        private readonly IBlockStore store;
        private readonly List<BlockChange> undoList = new List<BlockChange>();
        private readonly Stack<BlockChange> redoStack = new Stack<BlockChange>();

        public EditHistory(IBlockStore blockStore, int capacity = DefaultCapacity)
        {
            store = blockStore ?? throw new ArgumentNullException(nameof(blockStore));
            Capacity = Mathf.Max(1, capacity);
        }

        /// <summary>되돌릴 수 있는 기록이나 다시 실행할 기록의 수가 바뀌면 알린다.</summary>
        public event Action Changed;

        /// <summary>
        /// 편집이 이루어지면 알린다: 무엇을 했는지와 어느 블록이 어떻게 바뀌었는지. 이루어지지 않은 편집은 알리지 않는다.
        /// 되돌리기와 다시 실행의 변화는 원래 편집의 것(앞 → 뒤)이다.
        /// </summary>
        public event Action<EditKind, BlockChange> Edited;

        /// <summary>기억하는 기록의 상한. 넘으면 오래된 것부터 버린다.</summary>
        public int Capacity { get; }

        public int UndoCount => undoList.Count;

        public int RedoCount => redoStack.Count;

        public bool CanUndo => undoList.Count > 0;

        public bool CanRedo => redoStack.Count > 0;

        /// <summary>블록을 놓고 기록한다. 놓지 못하면 기록하지 않는다. 놓였으면 새 번호가 id에 담긴다.</summary>
        public PlaceResult Place(int partIndex, Vector3 position, Quaternion rotation, out int id)
        {
            PlaceResult result = store.Add(partIndex, position, rotation, out id);
            if (result == PlaceResult.Ok && store.TryGet(id, out BlockRecord placed)) Push(BlockChange.Placed(placed), EditKind.Place);
            return result;
        }

        /// <summary>블록을 똑바로 선 채로 놓고 기록한다.</summary>
        public PlaceResult Place(int partIndex, Vector3 position)
        {
            return Place(partIndex, position, Quaternion.identity, out _);
        }

        /// <summary>블록을 지우고 기록한다. 지울 블록이 없으면 기록하지 않는다.</summary>
        public bool Remove(int id)
        {
            if (!store.TryGet(id, out BlockRecord before)) return false;
            if (!store.Remove(id)) return false;

            Push(BlockChange.Removed(before), EditKind.Remove);
            return true;
        }

        /// <summary>있는 블록을 다른 부품으로 바꾸고(칠하기) 기록한다. 블록이 없거나 이미 같은 부품이면 기록하지 않는다.</summary>
        public bool Replace(int id, int partIndex)
        {
            if (!store.TryGet(id, out BlockRecord before)) return false;
            if (before.Part == partIndex) return false;

            BlockRecord after = before;
            after.Part = partIndex;
            return Change(before, after, EditKind.Paint);
        }

        /// <summary>있는 블록의 자리와 방향을 바꾸고(옮기기, 돌리기) 기록한다. 블록이 없거나 이미 그 자리와 방향이면 기록하지 않는다.</summary>
        public bool Move(int id, Vector3 position, Quaternion rotation)
        {
            if (!store.TryGet(id, out BlockRecord before)) return false;

            BlockRecord after = before;
            after.Position = BlockMap.Quantize(position);
            after.Rotation = rotation;
            if (before.SamePose(after)) return false;

            return Change(before, after, EditKind.Move);
        }

        /// <summary>마지막 편집을 되돌린다. 되돌릴 것이 없거나 되돌리지 못하면 false이고 기록은 그대로 남는다.</summary>
        public bool Undo()
        {
            if (undoList.Count == 0) return false;

            BlockChange change = undoList[undoList.Count - 1];
            if (!Apply(change.Id, change.HadBefore, change.Before)) return false;

            undoList.RemoveAt(undoList.Count - 1);
            redoStack.Push(change);
            Changed?.Invoke();
            Edited?.Invoke(EditKind.Undo, change);
            return true;
        }

        /// <summary>되돌린 편집을 다시 실행한다.</summary>
        public bool Redo()
        {
            if (redoStack.Count == 0) return false;

            BlockChange change = redoStack.Peek();
            if (!Apply(change.Id, change.HasAfter, change.After)) return false;

            redoStack.Pop();
            undoList.Add(change);
            Changed?.Invoke();
            Edited?.Invoke(EditKind.Redo, change);
            return true;
        }

        /// <summary>기록을 모두 비운다. 맵을 통째로 바꿔 넣을 때 쓴다.</summary>
        public void Clear()
        {
            if (undoList.Count == 0 && redoStack.Count == 0) return;

            undoList.Clear();
            redoStack.Clear();
            Changed?.Invoke();
        }

        private bool Change(BlockRecord before, BlockRecord after, EditKind kind)
        {
            if (!store.Set(after)) return false;

            // 기록에는 다듬어진 값(자리의 단위, 방향의 정규화)을 남긴다.
            if (store.TryGet(after.Id, out BlockRecord stored)) after = stored;
            Push(BlockChange.Changed(before, after), kind);
            return true;
        }

        private void Push(BlockChange change, EditKind kind)
        {
            redoStack.Clear();
            undoList.Add(change);
            if (undoList.Count > Capacity) undoList.RemoveAt(0);
            Changed?.Invoke();
            Edited?.Invoke(kind, change);
        }

        /// <summary>
        /// 번호의 블록을 target 상태로 맞춘다(has가 false이면 없는 상태). 기록을 거치지 않고 바뀐 블록이 있어도 막히지 않도록,
        /// 이미 그 상태이면 성공으로 보고, 있으면 고쳐 놓고, 없으면 같은 번호로 되살린다.
        /// </summary>
        private bool Apply(int id, bool has, BlockRecord target)
        {
            bool exists = store.TryGet(id, out BlockRecord current);

            if (!has)
            {
                return !exists || store.Remove(id);
            }

            if (exists)
            {
                return (current.Part == target.Part && current.SamePose(target)) || store.Set(target);
            }

            return store.Restore(target) == PlaceResult.Ok;
        }
    }
}
