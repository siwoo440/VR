using System;
using System.Collections.Generic;
using AtelierVerse.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 맵의 대표 그림을 화면에 올릴 수 있는 그림으로 읽는다. 맵 목록 창과 맵 정보 창이 함께 쓴다.
    /// 만든 그림은 쓰는 쪽이 다 쓴 뒤에 없애야 한다.
    /// </summary>
    public static class MapThumbnail
    {
        /// <summary>맵의 대표 그림을 읽는다. 그림이 없거나 읽지 못하면 null이다.</summary>
        public static Texture2D Load(string id)
        {
            byte[] png = MapLibrary.LoadThumbnail(id);
            if (png == null) return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false) { name = $"MapThumbnail_{id}" };
            if (texture.LoadImage(png, true)) return texture;

            UnityEngine.Object.Destroy(texture);
            return null;
        }
    }

    /// <summary>
    /// 맵 목록 창(17일차). 이 기기에 저장된 맵을 보여 주고, 열기·새 맵·정보·지우기를 요청한다.
    /// 메뉴의 "내 작업실" 타일이나 M 키로 연다. PC에서는 화면 가운데의 창으로, VR에서는 메뉴처럼 눈앞의 판에 뜬다.
    /// 무엇을 보일지는 Show로 받고, 무엇을 할지는 요청 이벤트로 알린다. 맵의 대표 그림만은 여기서 읽어 줄마다 작게 보인다(18일차).
    /// 맵이 한 쪽에 다 들어가지 않으면 쪽을 넘긴다. 지우기는 잘못 누르지 않도록 두 번 눌러야 요청한다.
    /// 이름과 설명을 고치는 일은 줄의 "정보"로 여는 맵 정보 창(MapInfoView)에서 한다.
    /// </summary>
    public class MapListView : MonoBehaviour
    {
        private const float DeleteConfirmSeconds = 3f;

        [Serializable]
        public struct Row
        {
            public GameObject root;
            public RawImage thumbnail;
            public TMP_Text nameLabel;
            public TMP_Text infoLabel;
            public GameObject currentBadge;
            public Button openButton;
            public Button infoButton;
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
        [SerializeField] private string deleteText = "지우기";
        [SerializeField] private string deleteConfirmText = "한 번 더";

        private readonly List<MapInfo> maps = new List<MapInfo>();
        private readonly Dictionary<string, Texture2D> thumbnails = new Dictionary<string, Texture2D>();
        private string currentId = string.Empty;
        private string armedId;
        private float armedUntil;

        /// <summary>맵의 열기를 눌렀다. 인자는 맵의 번호표다.</summary>
        public event Action<string> OpenRequested;

        public event Action CreateRequested;

        /// <summary>맵의 정보를 눌렀다. 인자는 맵의 번호표다. 맵 정보 창을 연다.</summary>
        public event Action<string> InfoRequested;

        /// <summary>지우기를 두 번 눌렀다. 인자는 맵의 번호표다.</summary>
        public event Action<string> DeleteRequested;

        public event Action CloseRequested;

        public bool IsOpen => root != null && root.activeSelf;

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
                if (rows[i].infoButton != null) rows[i].infoButton.onClick.AddListener(() => PressInfo(index));
                if (rows[i].deleteButton != null) rows[i].deleteButton.onClick.AddListener(() => PressDelete(index));
            }

            if (previousButton != null) previousButton.onClick.AddListener(() => ShowPage(Page - 1));
            if (nextButton != null) nextButton.onClick.AddListener(() => ShowPage(Page + 1));
            if (createButton != null) createButton.onClick.AddListener(() => CreateRequested?.Invoke());
            if (closeButton != null) closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
        }

        private void Update()
        {
            if (armedId != null && Time.unscaledTime >= armedUntil) Disarm();
        }

        private void OnDestroy()
        {
            ClearThumbnails();
        }

        /// <summary>창을 연다. 무엇을 보일지는 이어서 Show로 알려 준다. 보던 쪽은 그대로 둔다.</summary>
        public void Open()
        {
            Disarm();
            if (root != null) root.SetActive(true);
        }

        public void Close()
        {
            Disarm();
            if (root != null) root.SetActive(false);
            Page = 0;
            ClearThumbnails();
        }

        /// <summary>맵 목록과 지금 열려 있는 맵을 알려 준다. 쪽은 범위 안에서 그대로 둔다.</summary>
        public void Show(IReadOnlyList<MapInfo> list, string current)
        {
            maps.Clear();
            if (list != null) maps.AddRange(list);
            currentId = current ?? string.Empty;

            // 대표 그림은 그 사이에 다시 찍혔을 수 있으므로 읽어 둔 것을 버리고 보이는 줄의 것만 다시 읽는다.
            ClearThumbnails();
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

        private void PressOpen(int rowIndex)
        {
            string id = IdAt(rowIndex);
            if (id != null) OpenRequested?.Invoke(id);
        }

        private void PressInfo(int rowIndex)
        {
            string id = IdAt(rowIndex);
            if (id != null) InfoRequested?.Invoke(id);
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
                if (row.infoButton != null) row.infoButton.interactable = map.Readable;
                if (row.deleteLabel != null) row.deleteLabel.text = map.Id == armedId ? deleteConfirmText : deleteText;
                ShowThumbnail(row.thumbnail, map);
            }

            int pages = PageCount;
            if (pageLabel != null) pageLabel.text = $"{Page + 1} / {pages}";
            if (previousButton != null) previousButton.interactable = Page > 0;
            if (nextButton != null) nextButton.interactable = Page < pages - 1;
        }

        /// <summary>줄의 작은 그림 자리에 맵의 대표 그림을 보인다. 그림이 없으면 자리를 비워 둔다(바탕만 보인다).</summary>
        private void ShowThumbnail(RawImage image, MapInfo map)
        {
            if (image == null) return;

            Texture2D texture = null;
            if (map.HasThumbnail && !thumbnails.TryGetValue(map.Id, out texture))
            {
                texture = MapThumbnail.Load(map.Id);
                thumbnails[map.Id] = texture;
            }

            image.texture = texture;
            image.enabled = texture != null;
        }

        private void ClearThumbnails()
        {
            foreach (Row row in rows)
            {
                if (row.thumbnail == null) continue;

                row.thumbnail.texture = null;
                row.thumbnail.enabled = false;
            }

            foreach (Texture2D texture in thumbnails.Values)
            {
                if (texture != null) Destroy(texture);
            }

            thumbnails.Clear();
        }

        /// <summary>맵의 블록 수와 마지막으로 저장한 때, 설명이 있으면 설명. 때는 이 기기의 시각으로 보인다.</summary>
        public static string Describe(MapInfo map)
        {
            if (!map.Readable) return "읽을 수 없는 파일";

            string text = $"블록 {map.BlockCount}개";
            if (map.UpdatedAt != DateTime.MinValue) text += $" · {map.UpdatedAt.ToLocalTime():yyyy-MM-dd HH:mm}";
            if (!string.IsNullOrEmpty(map.Description)) text += $" · {map.Description}";
            return text;
        }
    }
}
