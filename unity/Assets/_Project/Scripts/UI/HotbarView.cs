using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 부품 칸. 화면 아래 가운데의 것은 숫자 키로 고르고, VR의 부품 판에 든 것은 칸을 가리켜 눌러 고른다.
    /// 고른 칸은 위로 올라오고 이름이 칸 위에 나타난다. 두 부품 칸은 Bind로 같은 선택 상태(HotbarModel)를 함께 쓴다.
    /// </summary>
    public class HotbarView : MonoBehaviour
    {
        [Serializable]
        public struct Slot
        {
            public RectTransform root;
            public Image frame;
            public Image swatch;
            public string itemName;
            public Button button;
        }

        [SerializeField] private Slot[] slots;
        [SerializeField] private GameObject selectedPill;
        [SerializeField] private TMP_Text selectedLabel;
        [SerializeField] private Color frameColor = Color.gray;
        [SerializeField] private Color selectedFrameColor = Color.yellow;
        [SerializeField] private float selectedLift = 12f;

        private HotbarModel model;

        public HotbarModel Model
        {
            get
            {
                if (model == null) CreateModel();
                return model;
            }
        }

        public int SlotCount => slots.Length;

        public int SelectedIndex => Model.SelectedIndex;

        /// <summary>고른 부품의 이름. 고른 칸이 없으면 빈 문자열이다.</summary>
        public string SelectedItemName => SelectedIndex == HotbarModel.None ? string.Empty : slots[SelectedIndex].itemName;

        private void Awake()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                if (slots[i].button != null) slots[i].button.onClick.AddListener(() => Select(index));
            }

            Refresh();
        }

        /// <summary>칸에 든 부품의 이름. 빈 칸이나 범위 밖이면 빈 문자열이다.</summary>
        public string GetItemName(int index)
        {
            return index >= 0 && index < slots.Length ? slots[index].itemName ?? string.Empty : string.Empty;
        }

        public void Select(int index)
        {
            Model.Select(index);
        }

        /// <summary>
        /// 다른 부품 칸의 선택 상태를 함께 쓴다. 그 뒤로는 어느 쪽에서 골라도 두 곳이 같이 바뀐다.
        /// </summary>
        public void Bind(HotbarModel shared)
        {
            if (shared == null || shared == model) return;

            if (model != null) model.SelectionChanged -= OnSelectionChanged;
            model = shared;
            model.SelectionChanged += OnSelectionChanged;
            Refresh();
        }

        private void CreateModel()
        {
            model = new HotbarModel(slots.Length);
            for (int i = 0; i < slots.Length; i++)
            {
                model.SetFilled(i, !string.IsNullOrEmpty(slots[i].itemName));
            }

            model.SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(int index)
        {
            Refresh();
        }

        private void Refresh()
        {
            int selected = Model.SelectedIndex;

            for (int i = 0; i < slots.Length; i++)
            {
                Slot slot = slots[i];
                bool isSelected = i == selected;

                if (slot.frame != null) slot.frame.color = isSelected ? selectedFrameColor : frameColor;
                if (slot.root != null)
                {
                    Vector2 position = slot.root.anchoredPosition;
                    position.y = isSelected ? selectedLift : 0f;
                    slot.root.anchoredPosition = position;
                }
            }

            if (selectedPill != null) selectedPill.SetActive(selected != HotbarModel.None);
            if (selectedLabel != null) selectedLabel.text = SelectedItemName;
        }
    }
}
