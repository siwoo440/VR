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
            return FromPng(MapLibrary.LoadThumbnail(id), id);
        }

        /// <summary>맵의 대표 그림을 읽는다. 휴지통에 있는 맵이면 휴지통의 그림을 읽는다.</summary>
        public static Texture2D Load(MapInfo map)
        {
            return map.InTrash ? FromPng(MapLibrary.LoadTrashThumbnail(map.TrashKey), map.TrashKey) : Load(map.Id);
        }

        private static Texture2D FromPng(byte[] png, string name)
        {
            if (png == null) return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false) { name = $"MapThumbnail_{name}" };
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
    /// 같은 창이 휴지통도 보인다(22일차). 오른쪽 위의 단추로 맵 목록과 휴지통을 오가며, 휴지통에서는 줄마다 되살리기와 아주 지우기가 있다.
    /// 아주 지우기는 되돌릴 수 없으므로 지우기처럼 두 번 눌러야 요청한다.
    /// </summary>
    public class MapListView : MonoBehaviour
    {
        public const string ListTitle = "내 작업실";
        public const string TrashTitle = "휴지통";
        public const string ListGuide = "이 기기에 저장된 맵입니다. 다른 맵을 열면 지금 맵은 저장됩니다.";
        public const string TrashGuide = "지운 맵입니다. 되살리면 맵 목록으로 돌아옵니다. 아주 지우면 되찾을 수 없습니다.";
        public const string TrashEmptyText = "휴지통이 비어 있습니다";
        public const string BackToListText = "맵 목록으로";

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
            public Button restoreButton;
            public Button purgeButton;
            public TMP_Text purgeLabel;
        }

        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text guideLabel;
        [SerializeField] private Row[] rows;
        [SerializeField] private TMP_Text emptyLabel;
        [SerializeField] private TMP_Text pageLabel;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Button trashButton;
        [SerializeField] private TMP_Text trashLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private string deleteText = "지우기";
        [SerializeField] private string purgeText = "아주 지우기";
        [SerializeField] private string deleteConfirmText = "한 번 더";

        private readonly List<MapInfo> maps = new List<MapInfo>();
        private readonly Dictionary<string, Texture2D> thumbnails = new Dictionary<string, Texture2D>();
        private string currentId = string.Empty;
        private int trashCount;
        private string armedId;
        private float armedUntil;

        /// <summary>맵의 열기를 눌렀다. 인자는 맵의 번호표다.</summary>
        public event Action<string> OpenRequested;

        public event Action CreateRequested;

        /// <summary>맵의 정보를 눌렀다. 인자는 맵의 번호표다. 맵 정보 창을 연다.</summary>
        public event Action<string> InfoRequested;

        /// <summary>지우기를 두 번 눌렀다. 인자는 맵의 번호표다.</summary>
        public event Action<string> DeleteRequested;

        /// <summary>오른쪽 위의 단추를 눌렀다. 맵 목록에서는 휴지통으로, 휴지통에서는 맵 목록으로 가자는 뜻이다.</summary>
        public event Action TrashRequested;

        /// <summary>휴지통의 되살리기를 눌렀다. 인자는 휴지통의 파일 이름(MapInfo.TrashKey)이다.</summary>
        public event Action<string> RestoreRequested;

        /// <summary>휴지통의 아주 지우기를 두 번 눌렀다. 인자는 휴지통의 파일 이름(MapInfo.TrashKey)이다.</summary>
        public event Action<string> PurgeRequested;

        public event Action CloseRequested;

        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>휴지통을 보이고 있는지. 아니면 맵 목록이다.</summary>
        public bool ShowingTrash { get; private set; }

        public int Page { get; private set; }

        public int PageCount => rows.Length == 0 ? 1 : Mathf.Max(1, (maps.Count + rows.Length - 1) / rows.Length);

        public int RowsPerPage => rows.Length;

        /// <summary>보이고 있는 목록(맵 목록이나 휴지통)의 맵 수.</summary>
        public int MapCount => maps.Count;

        public string PageText => pageLabel != null ? pageLabel.text : string.Empty;

        public string TitleText => titleLabel != null ? titleLabel.text : string.Empty;

        /// <summary>오른쪽 위 단추의 글자. 맵 목록에서는 "휴지통"에 든 수가 붙고, 휴지통에서는 "맵 목록으로"다.</summary>
        public string TrashButtonText => trashLabel != null ? trashLabel.text : string.Empty;

        /// <summary>목록이 비어 있다는 글자가 보이고 있는지.</summary>
        public bool IsEmptyShown => emptyLabel != null && emptyLabel.gameObject.activeSelf;

        private void Awake()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                int index = i;
                if (rows[i].openButton != null) rows[i].openButton.onClick.AddListener(() => PressOpen(index));
                if (rows[i].infoButton != null) rows[i].infoButton.onClick.AddListener(() => PressInfo(index));
                if (rows[i].deleteButton != null) rows[i].deleteButton.onClick.AddListener(() => PressTwice(index, false));
                if (rows[i].restoreButton != null) rows[i].restoreButton.onClick.AddListener(() => PressRestore(index));
                if (rows[i].purgeButton != null) rows[i].purgeButton.onClick.AddListener(() => PressTwice(index, true));
            }

            if (previousButton != null) previousButton.onClick.AddListener(() => ShowPage(Page - 1));
            if (nextButton != null) nextButton.onClick.AddListener(() => ShowPage(Page + 1));
            if (createButton != null) createButton.onClick.AddListener(() => CreateRequested?.Invoke());
            if (trashButton != null) trashButton.onClick.AddListener(() => TrashRequested?.Invoke());
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
            ShowingTrash = false;
            ClearThumbnails();
        }

        /// <summary>
        /// 맵 목록과 지금 열려 있는 맵을 알려 준다. 쪽은 범위 안에서 그대로 둔다. inTrash는 휴지통에 든 맵의 수이며 오른쪽 위 단추에 보인다.
        /// 휴지통을 보고 있었으면 맵 목록으로 돌아오고 첫 쪽부터 보인다.
        /// </summary>
        public void Show(IReadOnlyList<MapInfo> list, string current, int inTrash = 0)
        {
            SwitchMode(false);
            currentId = current ?? string.Empty;
            trashCount = Mathf.Max(0, inTrash);
            Fill(list);
        }

        /// <summary>휴지통의 맵을 보인다. 맵 목록을 보고 있었으면 휴지통으로 바뀌고 첫 쪽부터 보인다.</summary>
        public void ShowTrash(IReadOnlyList<MapInfo> list)
        {
            SwitchMode(true);
            Fill(list);
        }

        private void SwitchMode(bool trash)
        {
            if (ShowingTrash == trash) return;

            ShowingTrash = trash;
            Page = 0;
            armedId = null;
        }

        private void Fill(IReadOnlyList<MapInfo> list)
        {
            maps.Clear();
            if (list != null) maps.AddRange(list);

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

        /// <summary>
        /// 이 쪽의 i번째 줄에 보이는 맵을 가리키는 이름. 맵 목록에서는 번호표이고 휴지통에서는 휴지통의 파일 이름이다.
        /// 줄이 비어 있으면 null이다.
        /// </summary>
        public string IdAt(int rowIndex)
        {
            int index = Page * rows.Length + rowIndex;
            return rowIndex >= 0 && rowIndex < rows.Length && index < maps.Count ? KeyOf(maps[index]) : null;
        }

        /// <summary>맵이 보이는 줄의 번호. 이 쪽에 없으면 -1이다. 휴지통에서는 휴지통의 파일 이름으로 찾는다.</summary>
        public int RowOf(string id)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                if (IdAt(i) == id) return i;
            }

            return -1;
        }

        /// <summary>맵이 있는 쪽의 번호. 보이고 있는 목록에 없으면 -1이다. 휴지통에서는 휴지통의 파일 이름으로 찾는다.</summary>
        public int PageOf(string id)
        {
            for (int i = 0; i < maps.Count; i++)
            {
                if (KeyOf(maps[i]) == id) return rows.Length == 0 ? 0 : i / rows.Length;
            }

            return -1;
        }

        public Row GetRow(int rowIndex)
        {
            return rows[rowIndex];
        }

        private static string KeyOf(MapInfo map)
        {
            return map.InTrash ? map.TrashKey : map.Id;
        }

        private void PressOpen(int rowIndex)
        {
            string id = IdAt(rowIndex);
            if (id != null && !ShowingTrash) OpenRequested?.Invoke(id);
        }

        private void PressInfo(int rowIndex)
        {
            string id = IdAt(rowIndex);
            if (id != null && !ShowingTrash) InfoRequested?.Invoke(id);
        }

        private void PressRestore(int rowIndex)
        {
            string key = IdAt(rowIndex);
            if (key != null && ShowingTrash) RestoreRequested?.Invoke(key);
        }

        /// <summary>
        /// 실수로 지우지 않도록 같은 줄의 단추를 두 번 눌러야 요청한다. 맵 목록의 지우기와 휴지통의 아주 지우기가 함께 쓴다.
        /// purge가 보이는 목록과 맞지 않으면(다른 쪽의 단추가 눌린 것이면) 듣지 않는다.
        /// </summary>
        private void PressTwice(int rowIndex, bool purge)
        {
            string id = IdAt(rowIndex);
            if (id == null || purge != ShowingTrash) return;

            if (armedId == id && Time.unscaledTime < armedUntil)
            {
                Disarm();
                if (purge) PurgeRequested?.Invoke(id);
                else DeleteRequested?.Invoke(id);
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
                if (KeyOf(map) == id) return true;
            }

            return false;
        }

        private void Refresh()
        {
            bool trash = ShowingTrash;

            for (int i = 0; i < rows.Length; i++)
            {
                Row row = rows[i];
                int index = Page * rows.Length + i;
                bool has = index < maps.Count;
                if (row.root != null && row.root.activeSelf != has) row.root.SetActive(has);
                if (!has) continue;

                MapInfo map = maps[index];
                bool isCurrent = !trash && map.Id == currentId;
                bool armed = KeyOf(map) == armedId;

                if (row.nameLabel != null) row.nameLabel.text = MapLibrary.DisplayName(map.Name);
                if (row.infoLabel != null) row.infoLabel.text = trash ? DescribeTrash(map) : Describe(map);
                if (row.currentBadge != null) row.currentBadge.SetActive(isCurrent);

                // 맵 목록의 단추와 휴지통의 단추는 같은 자리에 겹쳐 있고, 보이는 목록의 것만 켠다.
                SetShown(row.openButton, !trash);
                SetShown(row.infoButton, !trash);
                SetShown(row.deleteButton, !trash);
                SetShown(row.restoreButton, trash);
                SetShown(row.purgeButton, trash);

                if (row.openButton != null) row.openButton.interactable = map.Readable && !isCurrent;
                if (row.infoButton != null) row.infoButton.interactable = map.Readable;
                if (row.deleteLabel != null) row.deleteLabel.text = armed ? deleteConfirmText : deleteText;
                if (row.restoreButton != null) row.restoreButton.interactable = map.Readable;
                if (row.purgeLabel != null) row.purgeLabel.text = armed ? deleteConfirmText : purgeText;
                ShowThumbnail(row.thumbnail, map);
            }

            if (titleLabel != null) titleLabel.text = trash ? TrashTitle : ListTitle;
            if (guideLabel != null) guideLabel.text = trash ? TrashGuide : ListGuide;
            if (trashLabel != null) trashLabel.text = trash ? BackToListText : (trashCount > 0 ? $"{TrashTitle} {trashCount}" : TrashTitle);
            if (createButton != null && createButton.gameObject.activeSelf == trash) createButton.gameObject.SetActive(!trash);

            if (emptyLabel != null)
            {
                bool empty = maps.Count == 0;
                if (emptyLabel.gameObject.activeSelf != empty) emptyLabel.gameObject.SetActive(empty);
                if (empty) emptyLabel.text = trash ? TrashEmptyText : string.Empty;
            }

            int pages = PageCount;
            if (pageLabel != null) pageLabel.text = $"{Page + 1} / {pages}";
            if (previousButton != null) previousButton.interactable = Page > 0;
            if (nextButton != null) nextButton.interactable = Page < pages - 1;
        }

        private static void SetShown(Button button, bool shown)
        {
            if (button != null && button.gameObject.activeSelf != shown) button.gameObject.SetActive(shown);
        }

        /// <summary>줄의 작은 그림 자리에 맵의 대표 그림을 보인다. 그림이 없으면 자리를 비워 둔다(바탕만 보인다).</summary>
        private void ShowThumbnail(RawImage image, MapInfo map)
        {
            if (image == null) return;

            string key = KeyOf(map);
            Texture2D texture = null;
            if (map.HasThumbnail && !thumbnails.TryGetValue(key, out texture))
            {
                texture = MapThumbnail.Load(map);
                thumbnails[key] = texture;
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

        /// <summary>휴지통의 맵: 블록 수와 지운 때. 때는 이 기기의 시각으로 보인다.</summary>
        public static string DescribeTrash(MapInfo map)
        {
            string text = map.Readable ? $"블록 {map.BlockCount}개" : "읽을 수 없는 파일";
            if (map.DeletedAt != DateTime.MinValue) text += $" · 지운 때 {map.DeletedAt.ToLocalTime():yyyy-MM-dd HH:mm}";
            return text;
        }
    }
}
