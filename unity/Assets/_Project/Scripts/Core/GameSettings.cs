using System;
using UnityEngine;

namespace AtelierVerse.Core
{
    /// <summary>
    /// 이 기기에 저장하는 개인 설정. 범위를 벗어난 값은 범위 안으로 맞추고, 값이 바뀌면 Changed로 알린다.
    /// </summary>
    public static class GameSettings
    {
        public const float MinLookSensitivity = 0.02f;
        public const float MaxLookSensitivity = 0.4f;
        public const float DefaultLookSensitivity = 0.12f;
        public const float MinFieldOfView = 50f;
        public const float MaxFieldOfView = 100f;
        public const float DefaultFieldOfView = 70f;

        private const string LookKey = "settings.lookSensitivity";
        private const string FieldOfViewKey = "settings.fieldOfView";
        private const string PeopleListKey = "settings.showPeopleList";

        public static event Action Changed;

        /// <summary>마우스를 움직인 거리 1에 시점이 도는 각도.</summary>
        public static float LookSensitivity
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(LookKey, DefaultLookSensitivity), MinLookSensitivity, MaxLookSensitivity);
            set => SetFloat(LookKey, Mathf.Clamp(value, MinLookSensitivity, MaxLookSensitivity));
        }

        /// <summary>화면에 보이는 세로 시야각(도).</summary>
        public static float FieldOfView
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(FieldOfViewKey, DefaultFieldOfView), MinFieldOfView, MaxFieldOfView);
            set => SetFloat(FieldOfViewKey, Mathf.Clamp(value, MinFieldOfView, MaxFieldOfView));
        }

        /// <summary>화면 오른쪽 위의 사람들 목록을 보일지 여부.</summary>
        public static bool ShowPeopleList
        {
            get => PlayerPrefs.GetInt(PeopleListKey, 1) != 0;
            set
            {
                if (ShowPeopleList == value) return;
                PlayerPrefs.SetInt(PeopleListKey, value ? 1 : 0);
                Changed?.Invoke();
            }
        }

        public static void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(LookKey);
            PlayerPrefs.DeleteKey(FieldOfViewKey);
            PlayerPrefs.DeleteKey(PeopleListKey);
            Changed?.Invoke();
        }

        private static void SetFloat(string key, float value)
        {
            if (PlayerPrefs.HasKey(key) && Mathf.Approximately(PlayerPrefs.GetFloat(key), value)) return;
            PlayerPrefs.SetFloat(key, value);
            Changed?.Invoke();
        }
    }
}
