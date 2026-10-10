using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AtelierVerse.Core
{
    /// <summary>
    /// Boot 씬의 진입점. 이후 로그인 확인과 저장 불러오기를 이곳에서 처리한 뒤 다음 씬으로 넘어간다.
    /// 빌드를 자동으로 확인할 때 쓰는 명령줄 인자도 읽는다: -quitAfter 초 (그 뒤 끝내기), -screenshotOut 경로 (끝내기 1초 전에 화면 저장),
    /// -screenCheck (전체 화면으로 갔다가 창으로 돌아오며 그때마다의 화면 방식과 크기를 로그에 적음. 20일차).
    /// -quitAfter로 켠 실행은 사람이 쓰는 것이 아니므로 키보드·마우스 입력을 받지 않는다(CheckRunInput).
    /// 또 끝나기 전에 초당 몇 번 그렸는지를 로그에 적는다(수직 동기화가 듣는지 보기 위한 것).
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        public const string QuitAfterArgument = "-quitAfter";
        public const string ScreenshotArgument = "-screenshotOut";
        public const string ScreenCheckArgument = "-screenCheck";
        private const float DefaultScreenshotDelay = 3f;

        [SerializeField] private string firstScene = "Sandbox";

        private void Start()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            float quitAfter = ParseSeconds(arguments, QuitAfterArgument);
            string screenshot = ReadArgument(arguments, ScreenshotArgument);

            bool screenCheck = HasFlag(arguments, ScreenCheckArgument);

            if (quitAfter > 0f)
            {
                // 확인하는 창이 떠 있는 동안 사람의 누름이 들어와 맵이나 설정이 바뀌는 일을 막는다.
                CheckRunInput.Block();
                Debug.Log("[Atelier Verse] 자동 확인 실행이라 키보드와 마우스 입력을 받지 않습니다.");
            }

            if (quitAfter > 0f || !string.IsNullOrEmpty(screenshot) || screenCheck)
            {
                // 다음 씬으로 넘어가도 남아 있어야 하므로 씬과 함께 지워지지 않게 둔다.
                DontDestroyOnLoad(gameObject);
                if (screenCheck) StartCoroutine(RunScreenCheck());
                if (quitAfter > 0f) StartCoroutine(RunFrameRateCheck(quitAfter));
                if (quitAfter > 0f || !string.IsNullOrEmpty(screenshot)) StartCoroutine(RunCheck(quitAfter, screenshot));
            }

            SceneManager.LoadScene(firstScene);
        }

        /// <summary>인자 뒤의 초를 읽는다. 없거나 숫자가 아니거나 0 이하이면 0이다.</summary>
        public static float ParseSeconds(string[] arguments, string name)
        {
            string value = ReadArgument(arguments, name);
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds) && seconds > 0f) return seconds;
            return 0f;
        }

        /// <summary>인자 바로 뒤의 값을 읽는다. 없으면 null이다.</summary>
        public static string ReadArgument(string[] arguments, string name)
        {
            if (arguments == null) return null;

            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == name) return arguments[i + 1];
            }

            return null;
        }

        /// <summary>값이 따로 없는 인자가 있는지.</summary>
        public static bool HasFlag(string[] arguments, string name)
        {
            return arguments != null && Array.IndexOf(arguments, name) >= 0;
        }

        /// <summary>
        /// 화면 방식을 설정 화면과 같은 길(ScreenControl)로 바꿔 보고 그때마다의 실제 화면을 로그에 적는다.
        /// 이 확인 때문에 적힌 창 크기는 끝나면 확인하기 전으로 되돌린다.
        /// </summary>
        private static IEnumerator RunScreenCheck()
        {
            bool hadWindow = ScreenControl.HasRememberedWindow;
            Vector2Int savedWindow = ScreenControl.RememberedWindow;

            yield return new WaitForSecondsRealtime(2f);
            Debug.Log($"[Atelier Verse] 화면 방식 확인 1/3 처음: {DescribeScreen()}");

            ScreenControl.SetFullscreen(true);
            yield return new WaitForSecondsRealtime(1.5f);
            Debug.Log($"[Atelier Verse] 화면 방식 확인 2/3 전체 화면으로: {DescribeScreen()}");

            ScreenControl.SetFullscreen(false);
            yield return new WaitForSecondsRealtime(1.5f);
            Debug.Log($"[Atelier Verse] 화면 방식 확인 3/3 창으로: {DescribeScreen()}");

            ScreenControl.ForgetWindow();
            if (hadWindow) ScreenControl.RememberWindow(savedWindow.x, savedWindow.y);
        }

        /// <summary>
        /// 씬이 뜬 뒤부터 끝나기 1초 전까지 초당 몇 번 그렸는지를 재어 로그에 적는다. 실행이 너무 짧으면 재지 않는다.
        /// </summary>
        private static IEnumerator RunFrameRateCheck(float quitAfter)
        {
            const float settle = 2f;
            float measure = quitAfter - settle - 1f;
            if (measure < 1f) yield break;

            yield return new WaitForSecondsRealtime(settle);
            int startFrame = Time.frameCount;
            float startTime = Time.realtimeSinceStartup;

            yield return new WaitForSecondsRealtime(measure);
            float seconds = Time.realtimeSinceStartup - startTime;
            double refresh = Screen.currentResolution.refreshRateRatio.value;
            Debug.Log($"[Atelier Verse] 평균 초당 {(Time.frameCount - startFrame) / seconds:0.0}번 그림(모니터 {refresh:0.#}Hz, 수직 동기화 {(QualitySettings.vSyncCount > 0 ? "켬" : "끔")})");
        }

        /// <summary>지금의 실제 화면. 창이 알려 주는 값을 그대로 적는다.</summary>
        private static string DescribeScreen()
        {
            Display display = Display.main;
            string monitor = display != null ? $"{display.systemWidth}x{display.systemHeight}" : "알 수 없음";
            return $"{(Screen.fullScreen ? "전체 화면" : "창")} {Screen.width}x{Screen.height} (모니터 {monitor})";
        }

        private IEnumerator RunCheck(float quitAfter, string screenshot)
        {
            float total = quitAfter > 0f ? quitAfter : DefaultScreenshotDelay;

            if (!string.IsNullOrEmpty(screenshot))
            {
                yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, total - 1f));
                ScreenCapture.CaptureScreenshot(screenshot);
                Debug.Log($"[Atelier Verse] 화면을 저장합니다: {screenshot}");
                yield return new WaitForSecondsRealtime(1f);
            }
            else
            {
                yield return new WaitForSecondsRealtime(total);
            }

            if (quitAfter > 0f)
            {
                Debug.Log($"[Atelier Verse] {QuitAfterArgument} {quitAfter}초가 지나 끝냅니다.");
                AppExit.Request();
            }
        }
    }
}
