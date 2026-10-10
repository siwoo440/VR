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
    /// "사본 만들기"는 같은 내용의 새 맵을 만든다(22일차). 글자 칸에 쓰던 것이 아니라 저장되어 있는 맵을 베낀다.
    /// 창은 두 갈래다(23일차): "기본 정보"와 "분위기와 크기". 둘째 갈래에서 하늘, 해의 방향과 높이, 바닥의 크기를 고르며,
    /// 고르면 바로 바뀐다. 그 갈래를 보는 동안에는 창 뒤의 어두운 막을 걷어, 창 옆으로 보이는 장면의 색이 그대로 보이게 한다.
    /// </summary>
    public class MapInfoView : MonoBehaviour
    {
        public const string DefaultSpawnText = "시작 위치 · 처음 자리";
        public const string OtherMapNote = "대표 그림과 시작 위치는 이 맵을 연 뒤에 정할 수 있습니다";
        public const string EnvironmentNote = "고르면 바로 바뀌고 저장됩니다. 창 옆으로 보이는 장면에서 확인할 수 있습니다.";
        public const string OtherMapEnvironmentNote = "분위기와 크기는 이 맵을 연 뒤에 바꿀 수 있습니다";

        /// <summary>갈래의 번호.</summary>
        public const int InfoSection = 0;
        public const int EnvironmentSection = 1;

        private const float UnavailableAlpha = 0.62f;

        [SerializeField] private GameObject root;
        [SerializeField] private Image scrim;
        [SerializeField] private ChoiceBar sectionBar;
        [SerializeField] private GameObject[] sections;
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
        [SerializeField] private Button duplicateButton;
        [SerializeField] private Button backButton;
        [SerializeField] private CanvasGroup environmentGroup;
        [SerializeField] private ChoiceBar skyBar;
        [SerializeField] private Slider sunYawSlider;
        [SerializeField] private TMP_Text sunYawValue;
        [SerializeField] private Slider sunPitchSlider;
        [SerializeField] private TMP_Text sunPitchValue;
        [SerializeField] private ChoiceBar floorBar;
        [SerializeField] private TMP_Text environmentNoteLabel;

        private Texture2D thumbnailTexture;
        private CanvasGroup saveGroup;
        private bool editable = true;
        private bool hooked;
        private bool scrimRemembered;
        private Color scrimColor;

        /// <summary>저장을 눌렀다. 인자는 맵의 번호표, 이름, 설명이다.</summary>
        public event Action<string, string, string> SaveRequested;

        /// <summary>대표 그림 찍기를 눌렀다. 지금 보는 장면을 찍는다.</summary>
        public event Action SnapshotRequested;

        /// <summary>지금 선 자리를 시작 위치로 삼기를 눌렀다.</summary>
        public event Action SetSpawnRequested;

        /// <summary>시작 위치를 처음 자리로 되돌리기를 눌렀다.</summary>
        public event Action ClearSpawnRequested;

        /// <summary>사본 만들기를 눌렀다. 인자는 맵의 번호표다.</summary>
        public event Action<string> DuplicateRequested;

        /// <summary>하늘을 골랐다. 인자는 하늘의 이름(MapSky)이다.</summary>
        public event Action<string> SkyRequested;

        /// <summary>해의 방향이나 높이를 바꿨다. 인자는 방향과 높이(도)다. 막대를 끄는 동안 이어서 온다.</summary>
        public event Action<float, float> SunRequested;

        /// <summary>바닥의 크기를 골랐다. 인자는 한 변의 칸 수다.</summary>
        public event Action<int> FloorSizeRequested;

        /// <summary>목록으로 돌아가기를 눌렀다.</summary>
        public event Action BackRequested;

        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>보이고 있는 맵의 번호표.</summary>
        public string MapId { get; private set; }

        /// <summary>보이고 있는 맵이 지금 열려 있는 맵인지.</summary>
        public bool IsCurrentMap { get; private set; }

        /// <summary>보이고 있는 갈래의 번호(InfoSection, EnvironmentSection).</summary>
        public int CurrentSection { get; private set; }

        /// <summary>글자 칸에 글자를 쓰는 중인지. 이때는 글자를 치는 키가 게임의 키로 듣지 않아야 한다.</summary>
        public bool IsEditingText => (nameField != null && nameField.isFocused) || (descriptionField != null && descriptionField.isFocused);

        public string NameText => nameField != null ? nameField.text : string.Empty;

        public string DescriptionText => descriptionField != null ? descriptionField.text : string.Empty;

        public string FactsText => factsLabel != null ? factsLabel.text : string.Empty;

        public string SpawnText => spawnLabel != null ? spawnLabel.text : string.Empty;

        /// <summary>대표 그림이 보이고 있는지.</summary>
        public bool HasThumbnail => thumbnailTexture != null;

        /// <summary>창에 보이는 해의 방향(도).</summary>
        public float ShownSunYaw => sunYawSlider != null ? sunYawSlider.value * MapSky.SunYawStep : MapSky.DefaultSunYaw;

        /// <summary>창에 보이는 해의 높이(도).</summary>
        public float ShownSunPitch => sunPitchSlider != null ? MapSky.MinSunPitch + sunPitchSlider.value * MapSky.SunPitchStep : MapSky.DefaultSunPitch;

        private void Awake()
        {
            Hook();
        }

        /// <summary>
        /// 단추와 칸, 막대를 잇는다. 창이 한 번도 켜지지 않았어도(Awake가 불리기 전에도) 값을 채울 수 있도록,
        /// 값을 처음 받을 때도 부른다. 한 번만 잇는다.
        /// </summary>
        private void Hook()
        {
            if (hooked) return;

            hooked = true;
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
            if (saveButton != null)
            {
                saveButton.onClick.AddListener(Save);
                saveGroup = saveButton.GetComponent<CanvasGroup>();
            }

            if (duplicateButton != null) duplicateButton.onClick.AddListener(Duplicate);
            if (backButton != null) backButton.onClick.AddListener(() => BackRequested?.Invoke());

            if (sectionBar != null) sectionBar.Chosen += ShowSection;
            if (skyBar != null) skyBar.Chosen += OnSkyChosen;
            if (floorBar != null) floorBar.Chosen += OnFloorChosen;

            // 막대의 한 칸이 한 단계다: 방향은 15도씩 한 바퀴, 높이는 5도씩 가장 낮은 데서 머리 위까지.
            if (sunYawSlider != null)
            {
                sunYawSlider.wholeNumbers = true;
                sunYawSlider.minValue = 0f;
                sunYawSlider.maxValue = Mathf.Round(360f / MapSky.SunYawStep) - 1f;
                sunYawSlider.onValueChanged.AddListener(_ => OnSunChanged());
            }

            if (sunPitchSlider != null)
            {
                sunPitchSlider.wholeNumbers = true;
                sunPitchSlider.minValue = 0f;
                sunPitchSlider.maxValue = Mathf.Round((MapSky.MaxSunPitch - MapSky.MinSunPitch) / MapSky.SunPitchStep);
                sunPitchSlider.onValueChanged.AddListener(_ => OnSunChanged());
            }
        }

        private void OnDestroy()
        {
            ClearThumbnail();
        }

        /// <summary>창을 연다. 기본 정보 갈래부터 보인다.</summary>
        public void Open()
        {
            Hook();
            if (root != null) root.SetActive(true);
            ShowSection(InfoSection);
        }

        public void Close()
        {
            StopEditing();
            ShowSection(InfoSection);
            if (root != null) root.SetActive(false);
            MapId = null;
            ClearThumbnail();
        }

        /// <summary>갈래 하나만 보인다. 분위기와 크기를 보는 동안에는 창 뒤의 어두운 막을 걷는다.</summary>
        public void ShowSection(int index)
        {
            if (sections == null || index < 0 || index >= sections.Length) return;

            if (index != InfoSection) StopEditing();
            CurrentSection = index;
            for (int i = 0; i < sections.Length; i++)
            {
                if (sections[i] != null && sections[i].activeSelf != (i == index)) sections[i].SetActive(i == index);
            }

            if (sectionBar != null) sectionBar.SetValueWithoutNotify(index);

            // 분위기와 크기는 고르는 대로 저장되므로, 그 갈래에서는 이름과 설명을 저장하는 단추를 감춘다.
            // 단추를 끄지 않고 보이지 않게만 한다(켜고 끄는 것은 조작 방식에 따라 다른 쪽이 정한다).
            if (saveGroup != null)
            {
                bool shown = index == InfoSection;
                saveGroup.alpha = shown ? 1f : 0f;
                saveGroup.interactable = shown;
                saveGroup.blocksRaycasts = shown;
            }

            if (scrim == null) return;

            if (!scrimRemembered)
            {
                scrimRemembered = true;
                scrimColor = scrim.color;
            }

            // 막은 걷어도 눌림은 계속 막는다(창 밖을 눌러 마우스를 잡지 않게).
            scrim.color = index == EnvironmentSection ? new Color(scrimColor.r, scrimColor.g, scrimColor.b, 0f) : scrimColor;
        }

        /// <summary>
        /// 맵의 정보를 채운다. isCurrent는 그 맵이 지금 열려 있는 맵인지이고, spawnText는 시작 위치를 설명하는 글자다.
        /// 글자 칸에 쓰던 것이 있어도 맵의 지금 값으로 덮는다. 분위기와 크기도 맵의 값으로 보이며, 지금 맵이 아니면 누를 수 없다.
        /// </summary>
        public void Show(MapInfo map, bool isCurrent, string spawnText)
        {
            Hook();
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

            ShowEnvironment(map, isCurrent);

            ClearThumbnail();
            thumbnailTexture = map.HasThumbnail ? MapThumbnail.Load(map.Id) : null;
            if (thumbnail != null)
            {
                thumbnail.texture = thumbnailTexture;
                thumbnail.enabled = thumbnailTexture != null;
            }

            if (noThumbnailLabel != null) noThumbnailLabel.SetActive(thumbnailTexture == null);
        }

        /// <summary>분위기와 크기의 칸과 막대를 맵의 값으로 맞춘다. 요청은 올리지 않는다.</summary>
        private void ShowEnvironment(MapInfo map, bool isCurrent)
        {
            if (skyBar != null) skyBar.SetValueWithoutNotify(Mathf.Max(0, MapSky.IndexOf(map.Sky)));
            if (floorBar != null) floorBar.SetValueWithoutNotify(Mathf.Max(0, MapSize.IndexOf(map.FloorSize)));

            if (sunYawSlider != null) sunYawSlider.SetValueWithoutNotify(Mathf.Round(MapSky.SnapYaw(map.SunYaw) / MapSky.SunYawStep));
            if (sunPitchSlider != null) sunPitchSlider.SetValueWithoutNotify(Mathf.Round((MapSky.SnapPitch(map.SunPitch) - MapSky.MinSunPitch) / MapSky.SunPitchStep));
            ShowSunValues();

            if (environmentNoteLabel != null) environmentNoteLabel.text = isCurrent ? EnvironmentNote : OtherMapEnvironmentNote;
            if (environmentGroup == null) return;

            environmentGroup.alpha = isCurrent ? 1f : UnavailableAlpha;
            environmentGroup.interactable = isCurrent;
            environmentGroup.blocksRaycasts = isCurrent;
        }

        private void ShowSunValues()
        {
            if (sunYawValue != null) sunYawValue.text = $"{ShownSunYaw:0}°";
            if (sunPitchValue != null) sunPitchValue.text = $"{ShownSunPitch:0}°";
        }

        private void OnSkyChosen(int index)
        {
            if (MapId == null || !IsCurrentMap || index < 0 || index >= MapSky.Presets.Length) return;
            SkyRequested?.Invoke(MapSky.Presets[index].id);
        }

        private void OnFloorChosen(int index)
        {
            if (MapId == null || !IsCurrentMap || index < 0 || index >= MapSize.Sizes.Length) return;
            FloorSizeRequested?.Invoke(MapSize.Sizes[index]);
        }

        private void OnSunChanged()
        {
            ShowSunValues();
            if (MapId == null || !IsCurrentMap) return;
            SunRequested?.Invoke(ShownSunYaw, ShownSunPitch);
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

        /// <summary>보이고 있는 맵의 사본을 만들어 달라고 요청한다.</summary>
        public void Duplicate()
        {
            if (MapId == null) return;

            StopEditing();
            DuplicateRequested?.Invoke(MapId);
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
