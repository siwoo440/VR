using AtelierVerse.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 메뉴의 설정 쪽. 값을 바꾸면 바로 GameSettings에 저장되고, 다른 곳에서 바뀐 값도 따라 보여 준다.
    /// 소리 크기를 바꿀 때는 그 크기를 들어 볼 수 있게 짧은 소리를 낸다.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider lookSlider;
        [SerializeField] private TMP_Text lookValue;
        [SerializeField] private Slider fieldOfViewSlider;
        [SerializeField] private TMP_Text fieldOfViewValue;
        [SerializeField] private Slider soundSlider;
        [SerializeField] private TMP_Text soundValue;
        [SerializeField] private Toggle peopleListToggle;
        [SerializeField] private Button resetButton;

        // 소리 크기를 끄는 동안 들려주는 소리의 간격. 손잡이를 끌 때 소리가 쏟아지지 않게 한다.
        private const float PreviewInterval = 0.12f;

        private float nextPreviewAt;

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
            if (lookSlider != null) lookSlider.onValueChanged.AddListener(OnLookChanged);
            if (fieldOfViewSlider != null) fieldOfViewSlider.onValueChanged.AddListener(OnFieldOfViewChanged);
            if (soundSlider != null) soundSlider.onValueChanged.AddListener(OnSoundChanged);
            if (peopleListToggle != null) peopleListToggle.onValueChanged.AddListener(OnPeopleListChanged);
            if (resetButton != null) resetButton.onClick.AddListener(GameSettings.ResetToDefaults);

            GameSettings.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameSettings.Changed -= Refresh;

            if (lookSlider != null) lookSlider.onValueChanged.RemoveListener(OnLookChanged);
            if (fieldOfViewSlider != null) fieldOfViewSlider.onValueChanged.RemoveListener(OnFieldOfViewChanged);
            if (soundSlider != null) soundSlider.onValueChanged.RemoveListener(OnSoundChanged);
            if (peopleListToggle != null) peopleListToggle.onValueChanged.RemoveListener(OnPeopleListChanged);
            if (resetButton != null) resetButton.onClick.RemoveListener(GameSettings.ResetToDefaults);
        }

        private static void OnLookChanged(float value)
        {
            GameSettings.LookSensitivity = value;
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

        private static void OnPeopleListChanged(bool value)
        {
            GameSettings.ShowPeopleList = value;
        }

        private void Refresh()
        {
            float look = GameSettings.LookSensitivity;
            float fieldOfView = GameSettings.FieldOfView;

            if (lookSlider != null) lookSlider.SetValueWithoutNotify(look);
            if (lookValue != null) lookValue.text = $"{Mathf.RoundToInt(look / GameSettings.DefaultLookSensitivity * 100f)}%";
            if (fieldOfViewSlider != null) fieldOfViewSlider.SetValueWithoutNotify(fieldOfView);
            if (fieldOfViewValue != null) fieldOfViewValue.text = $"{Mathf.RoundToInt(fieldOfView)}°";
            float sound = GameSettings.SoundVolume;
            if (soundSlider != null) soundSlider.SetValueWithoutNotify(sound);
            if (soundValue != null) soundValue.text = sound <= 0f ? "끔" : $"{Mathf.RoundToInt(sound * 100f)}%";
            if (peopleListToggle != null) peopleListToggle.SetIsOnWithoutNotify(GameSettings.ShowPeopleList);
        }
    }
}
