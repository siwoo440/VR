using AtelierVerse.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 메뉴의 설정 쪽. 값을 바꾸면 바로 GameSettings에 저장되고, 다른 곳에서 바뀐 값도 따라 보여 준다.
    /// 설정은 갈래(화면, 조작, 소리)로 나뉘어 있고 위쪽의 칸으로 갈래를 고른다(20일차).
    /// 화면 방식(창, 전체 화면)은 설정에 적어 두지 않고 창에 물어보므로, 다른 길로 바뀐 것도 따라 보인다.
    /// 소리 크기를 바꿀 때는 그 크기를 들어 볼 수 있게 짧은 소리를 낸다.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private ChoiceBar sectionBar;
        [SerializeField] private GameObject[] sections;
        [SerializeField] private ChoiceBar displayBar;
        [SerializeField] private ChoiceBar qualityBar;
        [SerializeField] private Toggle vSyncToggle;
        [SerializeField] private Toggle peopleListToggle;
        [SerializeField] private Slider lookSlider;
        [SerializeField] private TMP_Text lookValue;
        [SerializeField] private Toggle invertLookToggle;
        [SerializeField] private Slider fieldOfViewSlider;
        [SerializeField] private TMP_Text fieldOfViewValue;
        [SerializeField] private Slider soundSlider;
        [SerializeField] private TMP_Text soundValue;
        [SerializeField] private Button resetButton;

        // 소리 크기를 끄는 동안 들려주는 소리의 간격. 손잡이를 끌 때 소리가 쏟아지지 않게 한다.
        private const float PreviewInterval = 0.12f;

        // 화면 방식을 고르는 칸의 차례.
        private const int WindowChoice = 0;
        private const int FullscreenChoice = 1;

        private float nextPreviewAt;
        private bool shownFullscreen;

        /// <summary>지금 보이는 갈래의 번호.</summary>
        public int CurrentSection { get; private set; }

        public int SectionCount => sections != null ? sections.Length : 0;

        private void Awake()
        {
            if (lookSlider != null)
            {
                lookSlider.minValue = GameSettings.MinLookSensitivity;
                lookSlider.maxValue = GameSettings.MaxLookSensitivity;
            }

            if (fieldOfViewSlider != null)
            {
                fieldOfViewSlider.minValue = GameSettings.MinFieldOfView;
                fieldOfViewSlider.maxValue = GameSettings.MaxFieldOfView;
                fieldOfViewSlider.wholeNumbers = true;
            }

            if (soundSlider != null)
            {
                soundSlider.minValue = 0f;
                soundSlider.maxValue = 1f;
            }
        }

        private void OnEnable()
        {
            if (sectionBar != null) sectionBar.Chosen += ShowSection;
            if (displayBar != null) displayBar.Chosen += OnDisplayChosen;
            if (qualityBar != null) qualityBar.Chosen += OnQualityChosen;
            if (vSyncToggle != null) vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            if (peopleListToggle != null) peopleListToggle.onValueChanged.AddListener(OnPeopleListChanged);
            if (lookSlider != null) lookSlider.onValueChanged.AddListener(OnLookChanged);
            if (invertLookToggle != null) invertLookToggle.onValueChanged.AddListener(OnInvertLookChanged);
            if (fieldOfViewSlider != null) fieldOfViewSlider.onValueChanged.AddListener(OnFieldOfViewChanged);
            if (soundSlider != null) soundSlider.onValueChanged.AddListener(OnSoundChanged);
            if (resetButton != null) resetButton.onClick.AddListener(ResetAll);

            GameSettings.Changed += Refresh;
            ShowSection(CurrentSection);
            Refresh();
        }

        private void OnDisable()
        {
            GameSettings.Changed -= Refresh;

            if (sectionBar != null) sectionBar.Chosen -= ShowSection;
            if (displayBar != null) displayBar.Chosen -= OnDisplayChosen;
            if (qualityBar != null) qualityBar.Chosen -= OnQualityChosen;
            if (vSyncToggle != null) vSyncToggle.onValueChanged.RemoveListener(OnVSyncChanged);
            if (peopleListToggle != null) peopleListToggle.onValueChanged.RemoveListener(OnPeopleListChanged);
            if (lookSlider != null) lookSlider.onValueChanged.RemoveListener(OnLookChanged);
            if (invertLookToggle != null) invertLookToggle.onValueChanged.RemoveListener(OnInvertLookChanged);
            if (fieldOfViewSlider != null) fieldOfViewSlider.onValueChanged.RemoveListener(OnFieldOfViewChanged);
            if (soundSlider != null) soundSlider.onValueChanged.RemoveListener(OnSoundChanged);
            if (resetButton != null) resetButton.onClick.RemoveListener(ResetAll);
        }

        private void Update()
        {
            // 화면 방식은 설정 밖에서도 바뀔 수 있고, 바꾼 것이 한 프레임 뒤에 이루어지기도 한다. 달라졌으면 따라 보인다.
            if (displayBar != null && ScreenControl.IsFullscreen != shownFullscreen) RefreshDisplay();
        }

        /// <summary>갈래 하나만 보인다. 범위 밖의 번호는 듣지 않는다.</summary>
        public void ShowSection(int index)
        {
            if (sections == null || index < 0 || index >= sections.Length) return;

            CurrentSection = index;
            for (int i = 0; i < sections.Length; i++)
            {
                if (sections[i] != null && sections[i].activeSelf != (i == index)) sections[i].SetActive(i == index);
            }

            if (sectionBar != null) sectionBar.SetValueWithoutNotify(index);
        }

        private static void OnDisplayChosen(int choice)
        {
            ScreenControl.SetFullscreen(choice == FullscreenChoice);
        }

        private static void OnQualityChosen(int choice)
        {
            GameSettings.Quality = choice;
        }

        private static void OnVSyncChanged(bool value)
        {
            GameSettings.VSync = value;
        }

        private static void OnPeopleListChanged(bool value)
        {
            GameSettings.ShowPeopleList = value;
        }

        private static void OnLookChanged(float value)
        {
            GameSettings.LookSensitivity = value;
        }

        private static void OnInvertLookChanged(bool value)
        {
            GameSettings.InvertLookY = value;
        }

        private static void OnFieldOfViewChanged(float value)
        {
            GameSettings.FieldOfView = value;
        }

        private void OnSoundChanged(float value)
        {
            GameSettings.SoundVolume = value;

            if (Time.unscaledTime < nextPreviewAt) return;

            nextPreviewAt = Time.unscaledTime + PreviewInterval;
            Sfx.Play(SfxId.Select);
        }

        /// <summary>모든 갈래의 설정을 처음 값으로 돌린다. 전체 화면이면 창으로 돌아온다.</summary>
        private void ResetAll()
        {
            GameSettings.ResetToDefaults();
            ScreenControl.ResetToDefault();
            RefreshDisplay();
        }

        private void Refresh()
        {
            float look = GameSettings.LookSensitivity;
            float fieldOfView = GameSettings.FieldOfView;
            float sound = GameSettings.SoundVolume;

            RefreshDisplay();
            if (qualityBar != null) qualityBar.SetValueWithoutNotify(GameSettings.Quality);
            if (vSyncToggle != null) vSyncToggle.SetIsOnWithoutNotify(GameSettings.VSync);
            if (peopleListToggle != null) peopleListToggle.SetIsOnWithoutNotify(GameSettings.ShowPeopleList);

            if (lookSlider != null) lookSlider.SetValueWithoutNotify(look);
            if (lookValue != null) lookValue.text = $"{Mathf.RoundToInt(look / GameSettings.DefaultLookSensitivity * 100f)}%";
            if (invertLookToggle != null) invertLookToggle.SetIsOnWithoutNotify(GameSettings.InvertLookY);
            if (fieldOfViewSlider != null) fieldOfViewSlider.SetValueWithoutNotify(fieldOfView);
            if (fieldOfViewValue != null) fieldOfViewValue.text = $"{Mathf.RoundToInt(fieldOfView)}°";

            if (soundSlider != null) soundSlider.SetValueWithoutNotify(sound);
            if (soundValue != null) soundValue.text = sound <= 0f ? "끔" : $"{Mathf.RoundToInt(sound * 100f)}%";
        }

        private void RefreshDisplay()
        {
            shownFullscreen = ScreenControl.IsFullscreen;
            if (displayBar != null) displayBar.SetValueWithoutNotify(shownFullscreen ? FullscreenChoice : WindowChoice);
        }
    }
}
