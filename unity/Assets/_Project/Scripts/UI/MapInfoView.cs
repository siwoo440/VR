using System;
using AtelierVerse.World;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 맵 정보 창(18일차). 맵 하나의 대표 그림, 이름, 설명, 블록 수와 만든 날·고친 날, 시작 위치를 보이고 고친다.
    /// 맵 목록 창의 줄에 있는 "정보"로 연다. 이름과 설명은 글자 칸에 쓰고 "저장"을 눌러야 바뀐다.
    /// 대표 그림 찍기와 시작 위치 정하기는 그 맵 안에 있어야 할 수 있으므로, 지금 열려 있는 맵에서만 누를 수 있다.
    /// 무엇을 보일지는 Show로 받고, 무엇을 할지는 요청 이벤트로 알린다. 대표 그림만은 여기서 읽는다.
    /// VR에는 글자판이 없어 이름과 설명을 고칠 수 없다(글자 칸을 잠그고 저장 단추를 감춘다).
    /// </summary>
    public class MapInfoView : MonoBehaviour
    {
        public const string DefaultSpawnText = "시작 위치 · 처음 자리";
        public const string OtherMapNote = "대표 그림과 시작 위치는 이 맵을 연 뒤에 정할 수 있습니다";

        [SerializeField] private GameObject root;
        [SerializeField] private RawImage thumbnail;
        [SerializeField] private GameObject noThumbnailLabel;
        [SerializeField] private TMP_InputField nameField;
        [SerializeField] private TMP_InputField descriptionField;
        [SerializeField] private TMP_Text factsLabel;
        [SerializeField] private TMP_Text spawnLabel;
        [SerializeField] private TMP_Text noteLabel;
        [SerializeField] private Button snapshotButton;
        [SerializeField] private Button setSpawnButton;
        [SerializeField] private Button clearSpawnButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button backButton;

        private Texture2D thumbnailTexture;
        private bool editable = true;

        /// <summary>저장을 눌렀다. 인자는 맵의 번호표, 이름, 설명이다.</summary>
        public event Action<string, string, string> SaveRequested;

        /// <summary>대표 그림 찍기를 눌렀다. 지금 보는 장면을 찍는다.</summary>
        public event Action SnapshotRequested;

        /// <summary>지금 선 자리를 시작 위치로 삼기를 눌렀다.</summary>
        public event Action SetSpawnRequested;

        /// <summary>시작 위치를 처음 자리로 되돌리기를 눌렀다.</summary>
        public event Action ClearSpawnRequested;

        /// <summary>목록으로 돌아가기를 눌렀다.</summary>
        public event Action BackRequested;

        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>보이고 있는 맵의 번호표.</summary>
        public string MapId { get; private set; }

        /// <summary>보이고 있는 맵이 지금 열려 있는 맵인지.</summary>
        public bool IsCurrentMap { get; private set; }

        /// <summary>글자 칸에 글자를 쓰는 중인지. 이때는 글자를 치는 키가 게임의 키로 듣지 않아야 한다.</summary>
        public bool IsEditingText => (nameField != null && nameField.isFocused) || (descriptionField != null && descriptionField.isFocused);

        public string NameText => nameField != null ? nameField.text : string.Empty;

        public string DescriptionText => descriptionField != null ? descriptionField.text : string.Empty;

        public string FactsText => factsLabel != null ? factsLabel.text : string.Empty;

        public string SpawnText => spawnLabel != null ? spawnLabel.text : string.Empty;

        /// <summary>대표 그림이 보이고 있는지.</summary>
        public bool HasThumbnail => thumbnailTexture != null;

        private void Awake()
        {
            if (nameField != null)
            {
                nameField.characterLimit = MapLibrary.MaxNameLength;
                nameField.onSubmit.AddListener(_ => Save());
            }

            if (descriptionField != null)
            {
                descriptionField.characterLimit = MapDocument.MaxDescriptionLength;
                descriptionField.onSubmit.AddListener(_ => Save());
            }

            if (snapshotButton != null) snapshotButton.onClick.AddListener(() => SnapshotRequested?.Invoke());
            if (setSpawnButton != null) setSpawnButton.onClick.AddListener(() => SetSpawnRequested?.Invoke());
            if (clearSpawnButton != null) clearSpawnButton.onClick.AddListener(() => ClearSpawnRequested?.Invoke());
            if (saveButton != null) saveButton.onClick.AddListener(Save);
            if (backButton != null) backButton.onClick.AddListener(() => BackRequested?.Invoke());
        }

        private void OnDestroy()
        {
            ClearThumbnail();
        }

        public void Open()
        {
            if (root != null) root.SetActive(true);
        }

        public void Close()
        {
            StopEditing();
            if (root != null) root.SetActive(false);
            MapId = null;
            ClearThumbnail();
        }

        /// <summary>
        /// 맵의 정보를 채운다. isCurrent는 그 맵이 지금 열려 있는 맵인지이고, spawnText는 시작 위치를 설명하는 글자다.
        /// 글자 칸에 쓰던 것이 있어도 맵의 지금 값으로 덮는다.
        /// </summary>
        public void Show(MapInfo map, bool isCurrent, string spawnText)
        {
            MapId = map.Id;
            IsCurrentMap = isCurrent;

            if (nameField != null) nameField.SetTextWithoutNotify(map.Name ?? string.Empty);
            if (descriptionField != null) descriptionField.SetTextWithoutNotify(map.Description ?? string.Empty);
            if (factsLabel != null) factsLabel.text = Facts(map);
            if (spawnLabel != null) spawnLabel.text = spawnText ?? string.Empty;
            if (noteLabel != null) noteLabel.text = isCurrent ? string.Empty : OtherMapNote;

            if (snapshotButton != null) snapshotButton.interactable = isCurrent;
            if (setSpawnButton != null) setSpawnButton.interactable = isCurrent;
            if (clearSpawnButton != null) clearSpawnButton.interactable = isCurrent && map.HasSpawn;

            ClearThumbnail();
            thumbnailTexture = map.HasThumbnail ? MapThumbnail.Load(map.Id) : null;
            if (thumbnail != null)
            {
                thumbnail.texture = thumbnailTexture;
                thumbnail.enabled = thumbnailTexture != null;
            }

            if (noThumbnailLabel != null) noThumbnailLabel.SetActive(thumbnailTexture == null);
        }

        /// <summary>이름과 설명을 고칠 수 있는지 정한다. 글자판이 없는 VR에서는 고칠 수 없다.</summary>
        public void SetEditable(bool value)
        {
            editable = value;
            if (nameField != null) nameField.interactable = value;
            if (descriptionField != null) descriptionField.interactable = value;
            if (!value) StopEditing();
        }

        /// <summary>글자 칸의 이름과 설명으로 저장을 요청한다. 이름이 비어 있으면 요청하지 않는다.</summary>
        public void Save()
        {
            if (!editable || MapId == null || nameField == null) return;

            string name = MapLibrary.CleanName(nameField.text);
            if (name.Length == 0) return;

            string description = descriptionField != null ? MapLibrary.CleanDescription(descriptionField.text) : string.Empty;
            StopEditing();
            SaveRequested?.Invoke(MapId, name, description);
        }

        /// <summary>글자 쓰기를 그만둔다. 쓰던 글자는 칸에 남는다.</summary>
        public void StopEditing()
        {
            if (nameField != null) nameField.DeactivateInputField();
            if (descriptionField != null) descriptionField.DeactivateInputField();

            EventSystem system = EventSystem.current;
            if (system == null) return;

            GameObject selected = system.currentSelectedGameObject;
            bool onField = (nameField != null && selected == nameField.gameObject) || (descriptionField != null && selected == descriptionField.gameObject);
            if (onField) system.SetSelectedGameObject(null);
        }

        /// <summary>이름 칸에 글자를 쓰기 시작한다.</summary>
        public void FocusName()
        {
            if (nameField == null || !editable) return;

            nameField.Select();
            nameField.ActivateInputField();
        }

        /// <summary>글자 칸에 글자를 넣는다. 테스트와 붙여 넣기에 쓴다.</summary>
        public void SetNameText(string text)
        {
            if (nameField != null) nameField.text = text ?? string.Empty;
        }

        public void SetDescriptionText(string text)
        {
            if (descriptionField != null) descriptionField.text = text ?? string.Empty;
        }

        private void ClearThumbnail()
        {
            if (thumbnail != null)
            {
                thumbnail.texture = null;
                thumbnail.enabled = false;
            }

            if (thumbnailTexture != null) Destroy(thumbnailTexture);
            thumbnailTexture = null;
        }

        /// <summary>맵의 블록 수와 만든 날, 고친 때. 날과 때는 이 기기의 시각으로 보인다.</summary>
        public static string Facts(MapInfo map)
        {
            string text = $"블록 {map.BlockCount}개";
            if (map.CreatedAt != DateTime.MinValue) text += $" · 만든 날 {map.CreatedAt.ToLocalTime():yyyy-MM-dd}";
            if (map.UpdatedAt != DateTime.MinValue) text += $" · 고친 때 {map.UpdatedAt.ToLocalTime():yyyy-MM-dd HH:mm}";
            return text;
        }

        /// <summary>시작 위치를 설명하는 글자. 정하지 않았으면 처음 자리라고 적고, 정했으면 자리와 방향을 적는다.</summary>
        public static string DescribeSpawn(bool custom, Vector3 position, float yaw)
        {
            return custom
                ? $"시작 위치 · 정한 자리 ({position.x:0.#}, {position.y:0.#}, {position.z:0.#}) · 방향 {Mathf.Repeat(yaw, 360f):0}°"
                : DefaultSpawnText;
        }
    }
}
