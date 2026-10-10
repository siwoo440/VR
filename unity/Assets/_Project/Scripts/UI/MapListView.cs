using System;
using System.Collections.Generic;
using AtelierVerse.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 맵 목록 창(17일차). 이 기기에 저장된 맵을 보여 주고, 열기·새 맵·이름 바꾸기·지우기를 요청한다.
    /// 메뉴의 "내 작업실" 타일이나 M 키로 연다. PC에서는 화면 가운데의 창으로, VR에서는 메뉴처럼 눈앞의 판에 뜬다.
    /// 파일은 직접 다루지 않는다. 무엇을 보일지는 Show로 받고, 무엇을 할지는 요청 이벤트로 알린다.
    /// 맵이 한 쪽에 다 들어가지 않으면 쪽을 넘긴다. 지우기는 잘못 누르지 않도록 두 번 눌러야 요청한다.
    /// 이름 바꾸기는 글자판이 있는 PC에서만 쓴다(단추는 VR에서 감춘다).
    /// </summary>
    public class MapListView : MonoBehaviour
    {
        private const float DeleteConfirmSeconds = 3f;

        [Serializable]
        public struct Row
        {
            public GameObject root;
            public TMP_Text nameLabel;
            public TMP_Text infoLabel;
            public GameObject currentBadge;
            public Button openButton;
            public Button renameButton;
            public Button deleteButton;
            public TMP_Text deleteLabel;
        }

        [SerializeField] private GameObject root;
        [SerializeField] private Row[] rows;
        [SerializeField] private TMP_Text pageLabel;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject renameBar;
        [SerializeField] private TMP_InputField nameField;
        [SerializeField] private Button renameConfirmButton;
        [SerializeField] private Button renameCancelButton;
        [SerializeField] private string deleteText = "지우기";
        [SerializeField] private string deleteConfirmText = "한 번 더";

        private readonly List<MapInfo> maps = new List<MapInfo>();
        private string currentId = string.Empty;
        private string renamingId;
        private string armedId;
        private float armedUntil;

        /// <summary>맵의 열기를 눌렀다. 인자는 맵의 번호표다.</summary>
        public event Action<string> OpenRequested;

        public event Action CreateRequested;

        /// <summary>이름 바꾸기를 확인했다. 인자는 맵의 번호표와 새 이름이다.</summary>
        public event Action<string, string> RenameRequested;

        /// <summary>지우기를 두 번 눌렀다. 인자는 맵의 번호표다.</summary>
        public event Action<string> DeleteRequested;

        public event Action CloseRequested;

        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>이름을 고치는 중인지. 이때는 글자를 치는 키가 게임의 키로 듣지 않아야 한다.</summary>
        public bool IsEditingName => renamingId != null;

        public int Page { get; private set; }

        public int PageCount => rows.Length == 0 ? 1 : Mathf.Max(1, (maps.Count + rows.Length - 1) / rows.Length);

        public int RowsPerPage => rows.Length;

        public int MapCount => maps.Count;

        public string PageText => pageLabel != null ? pageLabel.text : string.Empty;

        private void Awake()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                int index = i;
                if (rows[i].openButton != null) rows[i].openButton.onClick.AddListener(() => PressOpen(index));
                if (rows[i].renameButton != null) rows[i].renameButton.onClick.AddListener(() => BeginRename(IdAt(index)));
                if (rows[i].deleteButton != null) rows[i].deleteButton.onClick.AddListener(() => PressDelete(index));
            }

            if (previousButton != null) previousButton.onClick.AddListener(() => ShowPage(Page - 1));
            if (nextButton != null) nextButton.onClick.AddListener(() => ShowPage(Page + 1));
            if (createButton != null) createButton.onClick.AddListener(() => CreateRequested?.Invoke());
            if (closeButton != null) closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
            if (renameConfirmButton != null) renameConfirmButton.onClick.AddListener(ConfirmRename);
            if (renameCancelButton != null) renameCancelButton.onClick.AddListener(CancelRename);
            if (nameField != null)
            {
                nameField.characterLimit = MapLibrary.MaxNameLength;
                nameField.onSubmit.AddListener(_ => ConfirmRename());
            }

            if (renameBar != null) renameBar.SetActive(false);
        }

        private void Update()
        {
            if (armedId != null && Time.unscaledTime >= armedUntil) Disarm();
        }

        /// <summary>창을 연다. 무엇을 보일지는 이어서 Show로 알려 준다.</summary>
        public void Open()
        {
            Page = 0;
            EndRename();
            Disarm();
            if (root != null) root.SetActive(true);
        }

        public void Close()
        {
            EndRename();
            Disarm();
            if (root != null) root.SetActive(false);
        }

        /// <summary>맵 목록과 지금 열려 있는 맵을 알려 준다. 쪽은 범위 안에서 그대로 둔다.</summary>
        public void Show(IReadOnlyList<MapInfo> list, string current)
        {
            maps.Clear();
            if (list != null) maps.AddRange(list);
            currentId = current ?? string.Empty;

            if (renamingId != null && !Contains(renamingId)) EndRename();
            if (armedId != null && !Contains(armedId)) armedId = null;
            ShowPage(Page);
        }

        public void ShowPage(int page)
        {
            Page = Mathf.Clamp(page, 0, PageCount - 1);
            Refresh();
        }

        /// <summary>이 쪽의 i번째 줄에 보이는 맵의 번호표. 줄이 비어 있으면 null이다.</summary>
        public string IdAt(int rowIndex)
        {
            int index = Page * rows.Length + rowIndex;
            return rowIndex >= 0 && rowIndex < rows.Length && index < maps.Count ? maps[index].Id : null;
        }

        /// <summary>맵이 보이는 줄의 번호. 이 쪽에 없으면 -1이다.</summary>
        public int RowOf(string id)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                if (IdAt(i) == id) return i;
            }

            return -1;
        }

        public Row GetRow(int rowIndex)
        {
            return rows[rowIndex];
        }

        /// <summary>이름 고치기를 시작한다. 지금 이름을 채운 글자 칸이 보인다.</summary>
        public void BeginRename(string id)
        {
            if (id == null || !Contains(id) || nameField == null) return;

            Disarm();
            renamingId = id;
            if (renameBar != null) renameBar.SetActive(true);
            nameField.text = Find(id).Name ?? string.Empty;
            nameField.Select();
            nameField.ActivateInputField();
        }

        /// <summary>글자 칸의 이름으로 바꾸기를 요청한다. 이름이 비어 있으면 요청하지 않고 고치기를 계속한다.</summary>
        public void ConfirmRename()
        {
            if (renamingId == null || nameField == null) return;

            string cleaned = MapLibrary.CleanName(nameField.text);
            if (cleaned.Length == 0) return;

            string id = renamingId;
            EndRename();
            RenameRequested?.Invoke(id, cleaned);
        }

        public void CancelRename()
        {
            EndRename();
        }

        /// <summary>이름을 고치는 글자 칸에 글자를 넣는다. 테스트와 붙여 넣기에 쓴다.</summary>
        public void SetNameText(string text)
        {
            if (nameField != null) nameField.text = text ?? string.Empty;
        }

        public string NameText => nameField != null ? nameField.text : string.Empty;

        private void EndRename()
        {
            renamingId = null;
            if (nameField != null) nameField.DeactivateInputField();
            if (renameBar != null && renameBar.activeSelf) renameBar.SetActive(false);
        }

        private void PressOpen(int rowIndex)
        {
            string id = IdAt(rowIndex);
            if (id != null) OpenRequested?.Invoke(id);
        }

        /// <summary>실수로 지우지 않도록 같은 줄의 지우기를 두 번 눌러야 요청한다.</summary>
        private void PressDelete(int rowIndex)
        {
            string id = IdAt(rowIndex);
            if (id == null) return;

            if (armedId == id && Time.unscaledTime < armedUntil)
            {
                Disarm();
                DeleteRequested?.Invoke(id);
                return;
            }

            armedId = id;
            armedUntil = Time.unscaledTime + DeleteConfirmSeconds;
            Refresh();
        }

        private void Disarm()
        {
            if (armedId == null) return;

            armedId = null;
            Refresh();
        }

        private bool Contains(string id)
        {
            foreach (MapInfo map in maps)
            {
                if (map.Id == id) return true;
            }

            return false;
        }

        private MapInfo Find(string id)
        {
            foreach (MapInfo map in maps)
            {
                if (map.Id == id) return map;
            }

            return default;
        }

        private void Refresh()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                Row row = rows[i];
                int index = Page * rows.Length + i;
                bool has = index < maps.Count;
                if (row.root != null && row.root.activeSelf != has) row.root.SetActive(has);
                if (!has) continue;

                MapInfo map = maps[index];
                bool isCurrent = map.Id == currentId;

                if (row.nameLabel != null) row.nameLabel.text = MapLibrary.DisplayName(map.Name);
                if (row.infoLabel != null) row.infoLabel.text = Describe(map);
                if (row.currentBadge != null) row.currentBadge.SetActive(isCurrent);
                if (row.openButton != null) row.openButton.interactable = map.Readable && !isCurrent;
                if (row.renameButton != null) row.renameButton.interactable = map.Readable;
                if (row.deleteLabel != null) row.deleteLabel.text = map.Id == armedId ? deleteConfirmText : deleteText;
            }

            int pages = PageCount;
            if (pageLabel != null) pageLabel.text = $"{Page + 1} / {pages}";
            if (previousButton != null) previousButton.interactable = Page > 0;
            if (nextButton != null) nextButton.interactable = Page < pages - 1;
        }

        /// <summary>맵의 블록 수와 마지막으로 저장한 때. 때는 이 기기의 시각으로 보인다.</summary>
        public static string Describe(MapInfo map)
        {
            if (!map.Readable) return "읽을 수 없는 파일";

            string blocks = $"블록 {map.BlockCount}개";
            return map.UpdatedAt == DateTime.MinValue ? blocks : $"{blocks} · {map.UpdatedAt.ToLocalTime():yyyy-MM-dd HH:mm}";
        }
    }
}
