using System;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 아래쪽 부품 칸의 선택 상태. 화면과 분리해 두어 규칙만 따로 검사할 수 있다.
    /// 비어 있는 칸은 고를 수 없고, 이미 고른 칸을 다시 고르면 선택이 풀린다.
    /// </summary>
    public class HotbarModel
    {
        public const int None = -1;

        private readonly bool[] filled;

        public HotbarModel(int slotCount)
        {
            filled = new bool[Math.Max(1, slotCount)];
        }

        public event Action<int> SelectionChanged;

        public int SlotCount => filled.Length;

        public int SelectedIndex { get; private set; } = None;

        public bool IsFilled(int index)
        {
            return index >= 0 && index < filled.Length && filled[index];
        }

        /// <summary>칸에 부품이 들어 있는지 정한다. 고른 칸을 비우면 선택도 풀린다.</summary>
        public void SetFilled(int index, bool value)
        {
            if (index < 0 || index >= filled.Length) return;

            filled[index] = value;
            if (!value && SelectedIndex == index) Set(None);
        }

        public void Select(int index)
        {
            if (!IsFilled(index)) return;
            Set(index == SelectedIndex ? None : index);
        }

        public void Clear()
        {
            Set(None);
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

            for (int i = 1; i <= filled.Length; i++)
            {
                int index = ((start + direction * i) % filled.Length + filled.Length) % filled.Length;
                if (!filled[index]) continue;

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
