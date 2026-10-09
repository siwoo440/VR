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
        private const string SnapLevelKey = "settings.snapLevel";
        private const string HotbarPartsKey = "settings.hotbarParts";

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

        /// <summary>
        /// 블록을 놓을 때의 맞추기 도우미 단계(0은 끔). 단계의 뜻은 PlacementMath가 정하며 여기서는 번호만 기억한다.
        /// </summary>
        public static int SnapLevel
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(SnapLevelKey, 0));
            set
            {
                value = Mathf.Max(0, value);
                if (SnapLevel == value) return;
                PlayerPrefs.SetInt(SnapLevelKey, value);
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// 부품 칸에 넣어 둔 부품들. 부품의 저장용 이름을 칸의 순서대로 쉼표로 이어 적고, 빈 칸은 빈 글자다.
        /// 값이 없으면(빈 문자열) 처음의 배치를 쓴다. 이름의 뜻은 부품 목록이 알고 여기서는 글자만 기억한다.
        /// </summary>
        public static string HotbarParts
        {
            get => PlayerPrefs.GetString(HotbarPartsKey, string.Empty);
            set
            {
                value ??= string.Empty;
                if (HotbarParts == value) return;
                PlayerPrefs.SetString(HotbarPartsKey, value);
                Changed?.Invoke();
            }
        }

        public static void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(LookKey);
            PlayerPrefs.DeleteKey(FieldOfViewKey);
            PlayerPrefs.DeleteKey(PeopleListKey);
            PlayerPrefs.DeleteKey(SnapLevelKey);
            PlayerPrefs.DeleteKey(HotbarPartsKey);
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
