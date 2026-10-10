using System;
using UnityEngine;

namespace AtelierVerse.Core
{
    /// <summary>
    /// 개인 설정의 화면 품질과 수직 동기화를 실제 그리기에 적용한다(20일차). 켜질 때 한 번, 그 뒤로는 설정이 바뀔 때마다 적용한다.
    /// 빌드를 확인할 때는 명령줄의 -quality low|normal|high(또는 0|1|2)로 이번 실행의 품질만 정할 수 있다.
    /// 그 값은 저장하지 않으며, 설정에서 품질을 바꾸면 그때부터는 설정을 따른다.
    /// 에디터에서는 마지막 적용 부품이 꺼질 때 바꾸기 전의 상태로 돌려놓는다(실행 중에 바꾼 것이 프로젝트 설정 파일에 남기 때문이다).
    /// </summary>
    public class GraphicsApplier : MonoBehaviour
    {
        public const string QualityArgument = "-quality";

        private static int active;

        private int forcedQuality = -1;
        private int settingAtStart;
        private bool logged;

        /// <summary>명령줄로 이번 실행의 품질을 정했는지.</summary>
        public bool IsForced => forcedQuality >= 0;

        private void Awake()
        {
            forcedQuality = ParseQuality(Environment.GetCommandLineArgs());
            settingAtStart = GameSettings.Quality;
        }

        private void OnEnable()
        {
            active++;
            GameSettings.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            GameSettings.Changed -= Apply;

            // 씬이 바뀔 때는 새 씬의 부품이 먼저 켜질 수도 있으므로, 켜져 있는 부품이 하나도 없을 때만 돌려놓는다.
            active = Mathf.Max(0, active - 1);
            if (active == 0 && Application.isEditor) GraphicsQuality.Restore();
        }

        /// <summary>명령줄에서 품질을 읽는다. 없거나 알 수 없는 값이면 -1이다.</summary>
        public static int ParseQuality(string[] arguments)
        {
            string value = Bootstrap.ReadArgument(arguments, QualityArgument);
            if (string.IsNullOrEmpty(value)) return -1;

            switch (value.Trim().ToLowerInvariant())
            {
                case "0":
                case "low": return GameSettings.QualityLow;
                case "1":
                case "normal": return GameSettings.QualityNormal;
                case "2":
                case "high": return GameSettings.QualityHigh;
                default: return -1;
            }
        }

        private void Apply()
        {
            // 설정에서 품질을 바꿨으면 명령줄의 값은 그만 따른다.
            if (IsForced && GameSettings.Quality != settingAtStart) forcedQuality = -1;

            int quality = IsForced ? forcedQuality : GameSettings.Quality;
            bool vSync = GameSettings.VSync;
            bool changed = GraphicsQuality.Apply(quality, vSync);
            if (!changed && logged) return;

            logged = true;
            int applied = GraphicsQuality.Current;
            string label = applied >= 0 ? GraphicsQuality.Labels[applied] : "알 수 없음";
            Debug.Log($"[Atelier Verse] 화면 품질 {label}({GraphicsQuality.CurrentLevelName}), 수직 동기화 {(QualitySettings.vSyncCount > 0 ? "켬" : "끔")}");
        }
    }
}
