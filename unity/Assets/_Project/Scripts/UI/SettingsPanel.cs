using AtelierVerse.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 메뉴의 설정 쪽. 값을 바꾸면 바로 GameSettings에 저장되고, 다른 곳에서 바뀐 값도 따라 보여 준다.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider lookSlider;
        [SerializeField] private TMP_Text lookValue;
        [SerializeField] private Slider fieldOfViewSlider;
        [SerializeField] private TMP_Text fieldOfViewValue;
        [SerializeField] private Toggle peopleListToggle;
        [SerializeField] private Button resetButton;

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
        }

        private void OnEnable()
        {
            if (lookSlider != null) lookSlider.onValueChanged.AddListener(OnLookChanged);
            if (fieldOfViewSlider != null) fieldOfViewSlider.onValueChanged.AddListener(OnFieldOfViewChanged);
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
            if (peopleListToggle != null) peopleListToggle.SetIsOnWithoutNotify(GameSettings.ShowPeopleList);
        }
    }
}
