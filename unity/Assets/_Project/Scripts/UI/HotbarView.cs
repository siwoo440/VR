using System;
using AtelierVerse.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 부품의 모양과 색을 작은 그림으로 보인다. 부품 칸과 부품 고르는 창이 함께 쓴다.
    /// 모양마다 그림이 따로 있고(셋업이 그려 둔 것), 색은 부품의 색을 입힌다.
    /// </summary>
    public static class PartSwatch
    {
        /// <summary>그림을 부품의 모양과 색으로 맞춘다. 부품이 없으면 그림을 감춘다.</summary>
        public static void Show(Image swatch, PartCatalog catalog, int part, Sprite[] shapeSprites)
        {
            if (swatch == null) return;

            bool has = catalog != null && catalog.IsValid(part);
            if (swatch.gameObject.activeSelf != has) swatch.gameObject.SetActive(has);
            if (!has) return;

            PartCatalog.Part shown = catalog.Get(part);
            swatch.color = shown.color;

            int shape = (int)shown.shape;
            if (shapeSprites != null && shape >= 0 && shape < shapeSprites.Length && shapeSprites[shape] != null)
            {
                swatch.sprite = shapeSprites[shape];
            }
        }
    }

    /// <summary>
    /// 부품 칸. 화면 아래 가운데의 것은 숫자 키로 고르고, VR의 부품 판에 든 것은 칸을 가리켜 눌러 고른다.
    /// 고른 칸은 위로 올라오고 이름이 칸 위에 나타난다. 두 부품 칸은 Bind로 같은 상태(HotbarModel)를 함께 쓴다.
    /// 칸에 든 부품은 부품 고르는 창에서 바꿀 수 있고(16일차), 칸의 그림은 부품 목록에서 그때그때 그린다.
    /// </summary>
    public class HotbarView : MonoBehaviour
    {
        [Serializable]
        public struct Slot
        {
            public RectTransform root;
            public Image frame;
            public Image swatch;
            public Button button;
        }

        [SerializeField] private Slot[] slots;
        [SerializeField] private PartCatalog catalog;
        [SerializeField] private Sprite[] shapeSprites;
        [SerializeField] private int[] defaultParts;
        [SerializeField] private GameObject selectedPill;
        [SerializeField] private TMP_Text selectedLabel;
        [SerializeField] private Color frameColor = Color.gray;
        [SerializeField] private Color selectedFrameColor = Color.yellow;
        [SerializeField] private float selectedLift = 12f;

        private HotbarModel model;

        /// <summary>빈 칸을 눌렀다. 인자는 칸의 번호다. 부품 고르는 창을 그 칸에 넣도록 여는 데 쓴다.</summary>
        public event Action<int> EmptySlotPressed;

        public HotbarModel Model
        {
            get
            {
                if (model == null) CreateModel();
                return model;
            }
        }

        public PartCatalog Catalog => catalog;

        public int SlotCount => slots.Length;

        public int SelectedIndex => Model.SelectedIndex;

        /// <summary>고른 칸에 든 부품의 번호. 고른 칸이 없으면 HotbarModel.None이다.</summary>
        public int SelectedPart => Model.SelectedPart;

        /// <summary>고른 부품의 이름. 고른 칸이 없으면 빈 문자열이다.</summary>
        public string SelectedItemName => GetItemName(SelectedIndex);

        private void Awake()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                if (slots[i].button != null) slots[i].button.onClick.AddListener(() => Press(index));
            }

            Refresh();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>칸에 든 부품의 이름. 빈 칸이나 범위 밖이면 빈 문자열이다.</summary>
        public string GetItemName(int index)
        {
            int part = Model.GetPart(index);
            return catalog != null && catalog.IsValid(part) ? catalog.Get(part).displayName ?? string.Empty : string.Empty;
        }

        public void Select(int index)
        {
            Model.Select(index);
        }

        /// <summary>
        /// 다른 부품 칸의 상태를 함께 쓴다. 그 뒤로는 어느 쪽에서 고르거나 부품을 바꿔도 두 곳이 같이 바뀐다.
        /// </summary>
        public void Bind(HotbarModel shared)
        {
            if (shared == null || shared == model) return;

            Unsubscribe();
            model = shared;
            Subscribe();
            Refresh();
        }

        /// <summary>처음의 부품 배치: 셋업이 정해 둔 부품을 앞 칸부터 넣는다.</summary>
        private void CreateModel()
        {
            model = new HotbarModel(slots.Length);
            if (defaultParts != null && catalog != null)
            {
                for (int i = 0; i < slots.Length && i < defaultParts.Length; i++)
                {
                    if (catalog.IsValid(defaultParts[i])) model.SetPart(i, defaultParts[i]);
                }
            }

            Subscribe();
        }

        private void Subscribe()
        {
            model.SelectionChanged += OnSelectionChanged;
            model.SlotsChanged += Refresh;
        }

        private void Unsubscribe()
        {
            if (model == null) return;

            model.SelectionChanged -= OnSelectionChanged;
            model.SlotsChanged -= Refresh;
        }

        /// <summary>칸을 눌렀다. 부품이 든 칸이면 고르고, 빈 칸이면 알린다.</summary>
        private void Press(int index)
        {
            if (Model.IsFilled(index)) Select(index);
            else EmptySlotPressed?.Invoke(index);
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

                PartSwatch.Show(slot.swatch, catalog, Model.GetPart(i), shapeSprites);
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
