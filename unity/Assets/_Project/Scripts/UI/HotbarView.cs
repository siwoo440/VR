using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 화면 아래 가운데의 부품 칸. 숫자 키로 고르며, 고른 칸은 위로 올라오고 이름이 칸 위에 나타난다.
    /// 지금은 고르기만 되고, 고른 부품을 놓는 동작은 뒤 일차에서 연결한다.
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
            Refresh();
        }

        public void Select(int index)
        {
            Model.Select(index);
        }

        private void CreateModel()
        {
            model = new HotbarModel(slots.Length);
            for (int i = 0; i < slots.Length; i++)
            {
                model.SetFilled(i, !string.IsNullOrEmpty(slots[i].itemName));
            }

            model.SelectionChanged += _ => Refresh();
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
