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
        public const float DefaultSoundVolume = 0.7f;

        // 화면 품질. 번호의 뜻(어느 품질 단계인지)은 GraphicsQuality가 안다.
        public const int QualityLow = 0;
        public const int QualityNormal = 1;
        public const int QualityHigh = 2;
        public const int DefaultQuality = QualityNormal;

        private const string LookKey = "settings.lookSensitivity";
        private const string FieldOfViewKey = "settings.fieldOfView";
        private const string PeopleListKey = "settings.showPeopleList";
        private const string SnapLevelKey = "settings.snapLevel";
        private const string HotbarPartsKey = "settings.hotbarParts";
        private const string SoundVolumeKey = "settings.soundVolume";
        private const string InvertLookKey = "settings.invertLookY";
        private const string QualityKey = "settings.quality";
        private const string VSyncKey = "settings.vSync";

        public static event Action Changed;

        /// <summary>마우스를 움직인 거리 1에 시점이 도는 각도.</summary>
        public static float LookSensitivity
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(LookKey, DefaultLookSensitivity), MinLookSensitivity, MaxLookSensitivity);
            set => SetFloat(LookKey, Mathf.Clamp(value, MinLookSensitivity, MaxLookSensitivity));
        }

        /// <summary>마우스를 위로 밀면 아래를 보게 할지. 끈 것이 기본이다.</summary>
        public static bool InvertLookY
        {
            get => PlayerPrefs.GetInt(InvertLookKey, 0) != 0;
            set
            {
                if (InvertLookY == value) return;
                PlayerPrefs.SetInt(InvertLookKey, value ? 1 : 0);
                Changed?.Invoke();
            }
        }

        /// <summary>화면에 보이는 세로 시야각(도).</summary>
        public static float FieldOfView
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(FieldOfViewKey, DefaultFieldOfView), MinFieldOfView, MaxFieldOfView);
            set => SetFloat(FieldOfViewKey, Mathf.Clamp(value, MinFieldOfView, MaxFieldOfView));
        }

        /// <summary>전체 소리 크기(0~1). 0이면 소리가 나지 않는다.</summary>
        public static float SoundVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(SoundVolumeKey, DefaultSoundVolume));
            set => SetFloat(SoundVolumeKey, Mathf.Clamp01(value));
        }

        /// <summary>화면 품질(QualityLow~QualityHigh). 범위를 벗어난 값은 범위 안으로 맞춘다.</summary>
        public static int Quality
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, DefaultQuality), QualityLow, QualityHigh);
            set
            {
                value = Mathf.Clamp(value, QualityLow, QualityHigh);
                if (Quality == value) return;
                PlayerPrefs.SetInt(QualityKey, value);
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// 수직 동기화. 켜면 모니터가 보여 줄 수 있는 만큼만 그려, 화면이 찢어져 보이지 않고 그래픽 카드가 쉬지 않고 도는 일이 없다.
        /// 켠 것이 기본이다.
        /// </summary>
        public static bool VSync
        {
            get => PlayerPrefs.GetInt(VSyncKey, 1) != 0;
            set
            {
                if (VSync == value) return;
                PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0);
                Changed?.Invoke();
            }
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
            PlayerPrefs.DeleteKey(SoundVolumeKey);
            PlayerPrefs.DeleteKey(InvertLookKey);
            PlayerPrefs.DeleteKey(QualityKey);
            PlayerPrefs.DeleteKey(VSyncKey);
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
