using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>블록 기록이 되돌리기에 내어 주는 최소한의 동작. BlockMap과 BlockWorld가 구현한다.</summary>
    public interface IBlockStore
    {
        bool TryGetPart(Vector3Int cell, out int partIndex);

        PlaceResult Place(Vector3Int cell, int partIndex);

        bool Remove(Vector3Int cell);

        /// <summary>있는 블록의 부품을 바꾼다. 블록이 없거나 부품을 모르면 false다.</summary>
        bool Replace(Vector3Int cell, int partIndex);
    }

    /// <summary>칸 하나의 변화. 부품이 없으면 EditHistory.NoPart다.</summary>
    public struct BlockChange
    {
        public Vector3Int Cell;
        public int Before;
        public int After;

        public BlockChange(Vector3Int cell, int before, int after)
        {
            Cell = cell;
            Before = before;
            After = after;
        }
    }

    /// <summary>
    /// 블록 세계 위의 기록 층. 놓기·지우기·칠하기를 하나씩 기록해 되돌리고(Undo) 다시 실행한다(Redo).
    /// 뒤에 생기는 도구(옮기기)도 이 층을 거치면 같은 방식으로 되돌릴 수 있다.
    /// 화면과 무관해서 편집 모드 테스트로 검사한다.
    /// </summary>
    public class EditHistory
    {
        public const int NoPart = -1;
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

        /// <summary>기억하는 기록의 상한. 넘으면 오래된 것부터 버린다.</summary>
        public int Capacity { get; }

        public int UndoCount => undoList.Count;

        public int RedoCount => redoStack.Count;

        public bool CanUndo => undoList.Count > 0;

        public bool CanRedo => redoStack.Count > 0;

        /// <summary>블록을 놓고 기록한다. 놓지 못하면 기록하지 않는다.</summary>
        public PlaceResult Place(Vector3Int cell, int partIndex)
        {
            PlaceResult result = store.Place(cell, partIndex);
            if (result == PlaceResult.Ok) Push(new BlockChange(cell, NoPart, partIndex));
            return result;
        }

        /// <summary>블록을 지우고 기록한다. 지울 블록이 없으면 기록하지 않는다.</summary>
        public bool Remove(Vector3Int cell)
        {
            if (!store.TryGetPart(cell, out int before)) return false;
            if (!store.Remove(cell)) return false;

            Push(new BlockChange(cell, before, NoPart));
            return true;
        }

        /// <summary>있는 블록을 다른 부품으로 바꾸고(칠하기) 기록한다. 블록이 없거나 이미 같은 부품이면 기록하지 않는다.</summary>
        public bool Replace(Vector3Int cell, int partIndex)
        {
            if (!store.TryGetPart(cell, out int before)) return false;
            if (before == partIndex) return false;
            if (!store.Replace(cell, partIndex)) return false;

            Push(new BlockChange(cell, before, partIndex));
            return true;
        }

        /// <summary>마지막 편집을 되돌린다. 되돌릴 것이 없거나 되돌리지 못하면 false이고 기록은 그대로 남는다.</summary>
        public bool Undo()
        {
            if (undoList.Count == 0) return false;

            BlockChange change = undoList[undoList.Count - 1];
            if (!Apply(change.Cell, change.Before)) return false;

            undoList.RemoveAt(undoList.Count - 1);
            redoStack.Push(change);
            Changed?.Invoke();
            return true;
        }

        /// <summary>되돌린 편집을 다시 실행한다.</summary>
        public bool Redo()
        {
            if (redoStack.Count == 0) return false;

            BlockChange change = redoStack.Peek();
            if (!Apply(change.Cell, change.After)) return false;

            redoStack.Pop();
            undoList.Add(change);
            Changed?.Invoke();
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

        private void Push(BlockChange change)
        {
            redoStack.Clear();
            undoList.Add(change);
            if (undoList.Count > Capacity) undoList.RemoveAt(0);
            Changed?.Invoke();
        }

        /// <summary>
        /// 칸을 target 상태로 맞춘다. 기록을 거치지 않고 바뀐 칸이 있어도 막히지 않도록,
        /// 이미 그 상태이면 성공으로 보고 다른 부품이 있으면 바꿔 넣는다.
        /// </summary>
        private bool Apply(Vector3Int cell, int target)
        {
            bool exists = store.TryGetPart(cell, out int current);

            if (target == NoPart)
            {
                return !exists || store.Remove(cell);
            }

            if (exists)
            {
                return current == target || store.Replace(cell, target);
            }

            return store.Place(cell, target) == PlaceResult.Ok;
        }
    }
}
