using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AtelierVerse.Core
{
    /// <summary>
    /// Boot 씬의 진입점. 이후 로그인 확인과 저장 불러오기를 이곳에서 처리한 뒤 다음 씬으로 넘어간다.
    /// 빌드를 자동으로 확인할 때 쓰는 명령줄 인자도 읽는다: -quitAfter 초 (그 뒤 끝내기), -screenshotOut 경로 (끝내기 1초 전에 화면 저장).
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        public const string QuitAfterArgument = "-quitAfter";
        public const string ScreenshotArgument = "-screenshotOut";
        private const float DefaultScreenshotDelay = 3f;

        [SerializeField] private string firstScene = "Sandbox";

        private void Start()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            float quitAfter = ParseSeconds(arguments, QuitAfterArgument);
            string screenshot = ReadArgument(arguments, ScreenshotArgument);

            if (quitAfter > 0f || !string.IsNullOrEmpty(screenshot))
            {
                // 다음 씬으로 넘어가도 남아 있어야 하므로 씬과 함께 지워지지 않게 둔다.
                DontDestroyOnLoad(gameObject);
                StartCoroutine(RunCheck(quitAfter, screenshot));
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
