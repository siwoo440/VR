using System;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 아래쪽 부품 칸의 상태: 칸마다 든 부품과 지금 고른 칸. 화면과 분리해 두어 규칙만 따로 검사할 수 있다.
    /// 비어 있는 칸은 고를 수 없고, 이미 고른 칸을 다시 고르면 선택이 풀린다.
    /// 칸에 든 부품은 부품 고르는 창에서 바꾼다(16일차). 부품은 부품 목록의 번호로 적는다.
    /// </summary>
    public class HotbarModel
    {
        public const int None = -1;

        private readonly int[] parts;

        public HotbarModel(int slotCount)
        {
            parts = new int[Math.Max(1, slotCount)];
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = None;
            }
        }

        /// <summary>고른 칸이 바뀌면 알린다. 인자는 새로 고른 칸이며 고른 칸이 없으면 None이다.</summary>
        public event Action<int> SelectionChanged;

        /// <summary>칸에 든 부품이 바뀌면 알린다.</summary>
        public event Action SlotsChanged;

        public int SlotCount => parts.Length;

        public int SelectedIndex { get; private set; } = None;

        /// <summary>고른 칸에 든 부품. 고른 칸이 없으면 None이다.</summary>
        public int SelectedPart => GetPart(SelectedIndex);

        /// <summary>칸에 든 부품. 빈 칸이나 범위 밖이면 None이다.</summary>
        public int GetPart(int index)
        {
            return index >= 0 && index < parts.Length ? parts[index] : None;
        }

        public bool IsFilled(int index)
        {
            return GetPart(index) != None;
        }

        /// <summary>칸에 부품을 넣는다. part가 None(또는 음수)이면 칸을 비우며, 고른 칸을 비우면 선택도 풀린다.</summary>
        public void SetPart(int index, int part)
        {
            if (index < 0 || index >= parts.Length) return;

            if (part < 0) part = None;
            if (parts[index] == part) return;

            parts[index] = part;
            if (part == None && SelectedIndex == index) Set(None);
            SlotsChanged?.Invoke();
        }

        /// <summary>칸을 고른다. 이미 고른 칸이면 선택이 풀린다.</summary>
        public void Select(int index)
        {
            if (!IsFilled(index)) return;
            Set(index == SelectedIndex ? None : index);
        }

        /// <summary>칸을 고른다. 이미 고른 칸이어도 선택이 풀리지 않는다. 부품을 넣은 칸을 바로 쓰게 할 때 쓴다.</summary>
        public void Choose(int index)
        {
            if (!IsFilled(index)) return;
            Set(index);
        }

        public void Clear()
        {
            Set(None);
        }

        /// <summary>가장 앞의 빈 칸. 빈 칸이 없으면 None이다.</summary>
        public int FirstEmpty()
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == None) return i;
            }

            return None;
        }

        public void SelectNext()
        {
            Step(1);
        }

        public void SelectPrevious()
        {
            Step(-1);
        }

        /// <summary>부품이 든 다음 칸으로 옮긴다. 끝에 닿으면 반대쪽 끝으로 넘어간다.</summary>
        private void Step(int direction)
        {
            int start = SelectedIndex == None ? (direction > 0 ? -1 : 0) : SelectedIndex;

            for (int i = 1; i <= parts.Length; i++)
            {
                int index = ((start + direction * i) % parts.Length + parts.Length) % parts.Length;
                if (parts[index] == None) continue;

                Set(index);
                return;
            }
        }

        private void Set(int index)
        {
            if (index == SelectedIndex) return;

            SelectedIndex = index;
            SelectionChanged?.Invoke(index);
        }
    }
}
