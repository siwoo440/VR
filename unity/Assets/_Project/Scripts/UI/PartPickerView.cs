using System;
using AtelierVerse.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 부품 고르는 창(16일차). 로블록스의 가방처럼, 부품 목록의 부품을 아래쪽 부품 칸(아홉 칸)에 넣고 뺀다.
    /// 위에는 넣을 칸을 고르는 줄이 있고, 아래에는 모양별로 줄을 지은 부품들이 있다. 부품을 누르면 넣을 칸에 들어간다.
    /// 무엇을 할지는 직접 처리하지 않고 요청 이벤트로 알리며, 칸의 상태는 부품 칸과 같은 것(HotbarModel)을 본다.
    /// PC에서는 화면 가운데의 창으로, VR에서는 메뉴처럼 눈앞의 판에 뜬다.
    /// </summary>
    public class PartPickerView : MonoBehaviour
    {
        [Serializable]
        public struct Cell
        {
            public Button button;
            public int part;
        }

        [Serializable]
        public struct Target
        {
            public Button button;
            public Image frame;
            public Image swatch;
        }

        [SerializeField] private GameObject root;
        [SerializeField] private Cell[] cells;
        [SerializeField] private Target[] targets;
        [SerializeField] private PartCatalog catalog;
        [SerializeField] private Sprite[] shapeSprites;
        [SerializeField] private TMP_Text targetLabel;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Color frameColor = Color.gray;
        [SerializeField] private Color targetFrameColor = Color.yellow;

        private HotbarModel model;

        /// <summary>부품을 눌렀다. 인자는 넣을 칸과 부품의 번호다.</summary>
        public event Action<int, int> PartChosen;

        /// <summary>칸 비우기를 눌렀다. 인자는 비울 칸이다.</summary>
        public event Action<int> ClearRequested;

        public event Action CloseRequested;

        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>부품을 누르면 들어갈 칸.</summary>
        public int TargetSlot { get; private set; }

        public int CellCount => cells.Length;

        public string TargetText => targetLabel != null ? targetLabel.text : string.Empty;

        private void Awake()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                int part = cells[i].part;
                if (cells[i].button != null) cells[i].button.onClick.AddListener(() => PartChosen?.Invoke(TargetSlot, part));
            }

            for (int i = 0; i < targets.Length; i++)
            {
                int slot = i;
                if (targets[i].button != null) targets[i].button.onClick.AddListener(() => SetTarget(slot));
            }

            if (clearButton != null) clearButton.onClick.AddListener(() => ClearRequested?.Invoke(TargetSlot));
            if (closeButton != null) closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
        }

        private void OnDestroy()
        {
            if (model != null) model.SlotsChanged -= Refresh;
        }

        /// <summary>부품 칸의 상태를 잇는다. 칸에 든 부품이 바뀌면 넣을 칸의 줄도 함께 바뀐다.</summary>
        public void Bind(HotbarModel shared)
        {
            if (shared == model) return;

            if (model != null) model.SlotsChanged -= Refresh;
            model = shared;
            if (model != null) model.SlotsChanged += Refresh;
            Refresh();
        }

        /// <summary>창을 연다. slot은 부품을 넣을 칸이다.</summary>
        public void Open(int slot)
        {
            if (root != null) root.SetActive(true);
            SetTarget(slot);
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
        }

        /// <summary>부품을 넣을 칸을 바꾼다. 범위 밖이면 첫 칸이다.</summary>
        public void SetTarget(int slot)
        {
            TargetSlot = slot >= 0 && slot < targets.Length ? slot : 0;
            Refresh();
        }

        /// <summary>창에 놓인 순서로 i번째 부품의 번호.</summary>
        public int GetCellPart(int index)
        {
            return index >= 0 && index < cells.Length ? cells[index].part : HotbarModel.None;
        }

        /// <summary>부품의 번호로 그 부품의 단추를 찾는다. 없으면 null이다.</summary>
        public Button FindCell(int part)
        {
            foreach (Cell cell in cells)
            {
                if (cell.part == part) return cell.button;
            }

            return null;
        }

        /// <summary>넣을 칸의 줄에서 i번째 칸의 단추.</summary>
        public Button GetTargetButton(int slot)
        {
            return slot >= 0 && slot < targets.Length ? targets[slot].button : null;
        }

        private void Refresh()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                int part = model != null ? model.GetPart(i) : HotbarModel.None;
                PartSwatch.Show(targets[i].swatch, catalog, part, shapeSprites);
                if (targets[i].frame != null) targets[i].frame.color = i == TargetSlot ? targetFrameColor : frameColor;
            }

            if (targetLabel == null) return;

            int current = model != null ? model.GetPart(TargetSlot) : HotbarModel.None;
            string name = catalog != null && catalog.IsValid(current) ? catalog.Get(current).displayName : "비어 있음";
            targetLabel.text = $"{TargetSlot + 1}번 칸 · {name}";
        }
    }
}
