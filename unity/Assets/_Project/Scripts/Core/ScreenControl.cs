using UnityEngine;

namespace AtelierVerse.Core
{
    /// <summary>
    /// 게임이 그려지는 창. 실제 창(UnityScreenDevice)과 테스트의 가짜 창이 이 약속을 지킨다.
    /// </summary>
    public interface IScreenDevice
    {
        /// <summary>지금 전체 화면인지.</summary>
        bool Fullscreen { get; }

        /// <summary>지금 그려지는 너비와 높이(점).</summary>
        int Width { get; }

        int Height { get; }

        /// <summary>모니터의 너비와 높이(점). 알 수 없으면 0이다.</summary>
        int DisplayWidth { get; }

        int DisplayHeight { get; }

        /// <summary>창의 크기와 방식을 바꾼다.</summary>
        void Apply(int width, int height, bool fullscreen);
    }

    /// <summary>
    /// 화면 방식(창, 전체 화면)을 바꾼다(20일차). 지금 어느 방식인지는 따로 적어 두지 않고 창에 물어본다.
    /// 다음에 켤 때의 방식과 크기는 Unity가 스스로 기억하므로, 여기서는 켤 때 아무것도 바꾸지 않는다
    /// (명령줄로 창 크기를 준 실행을 덮어쓰지 않기 위해서이기도 하다).
    /// 전체 화면으로 갈 때 창의 크기를 적어 두었다가, 창으로 돌아올 때 그 크기로 되돌린다.
    /// </summary>
    public static class ScreenControl
    {
        public const int DefaultWindowWidth = 1600;
        public const int DefaultWindowHeight = 900;
        public const int MinWindowWidth = 960;
        public const int MinWindowHeight = 540;

        /// <summary>창은 모니터의 이만큼까지만 차지한다. 제목 줄과 작업 표시줄이 들어갈 자리를 남긴다.</summary>
        public const float WindowFit = 0.9f;

        private const string WindowWidthKey = "screen.windowWidth";
        private const string WindowHeightKey = "screen.windowHeight";

        private static IScreenDevice device;

        /// <summary>바꿀 창. 테스트에서 가짜 창으로 바꿔 끼운다. null을 넣으면 실제 창으로 돌아간다.</summary>
        public static IScreenDevice Device
        {
            get => device ??= new UnityScreenDevice();
            set => device = value;
        }

        public static bool IsFullscreen => Device.Fullscreen;

        /// <summary>창으로 돌아올 때 쓸 크기. 적어 둔 것이 없으면 처음 크기다.</summary>
        public static Vector2Int RememberedWindow
        {
            get
            {
                int width = PlayerPrefs.GetInt(WindowWidthKey, DefaultWindowWidth);
                int height = PlayerPrefs.GetInt(WindowHeightKey, DefaultWindowHeight);
                if (width < MinWindowWidth || height < MinWindowHeight) return new Vector2Int(DefaultWindowWidth, DefaultWindowHeight);
                return new Vector2Int(width, height);
            }
        }

        /// <summary>창의 크기를 적어 둔 것이 있는지.</summary>
        public static bool HasRememberedWindow => PlayerPrefs.HasKey(WindowWidthKey) && PlayerPrefs.HasKey(WindowHeightKey);

        /// <summary>화면 방식을 바꾼다. 이미 그 방식이면 아무것도 하지 않고 false를 돌려준다.</summary>
        public static bool SetFullscreen(bool fullscreen)
        {
            IScreenDevice screen = Device;
            if (screen.Fullscreen == fullscreen) return false;

            if (fullscreen)
            {
                RememberWindow(screen.Width, screen.Height);

                // 모니터의 크기를 알 수 없으면 지금 크기 그대로 전체 화면으로 간다.
                bool known = screen.DisplayWidth > 0 && screen.DisplayHeight > 0;
                screen.Apply(known ? screen.DisplayWidth : screen.Width, known ? screen.DisplayHeight : screen.Height, true);
            }
            else
            {
                Vector2Int size = FitWindow(RememberedWindow, screen.DisplayWidth, screen.DisplayHeight);
                screen.Apply(size.x, size.y, false);
            }

            return true;
        }

        /// <summary>처음 값으로 돌린다: 적어 둔 창 크기를 잊고, 전체 화면이면 처음 크기의 창으로 돌아온다. 창이면 크기는 그대로 둔다.</summary>
        public static void ResetToDefault()
        {
            ForgetWindow();
            SetFullscreen(false);
        }

        /// <summary>적어 둔 창 크기를 잊는다.</summary>
        public static void ForgetWindow()
        {
            PlayerPrefs.DeleteKey(WindowWidthKey);
            PlayerPrefs.DeleteKey(WindowHeightKey);
        }

        /// <summary>
        /// 바라는 창 크기를 모니터에 들어가게 맞춘다. 크면 가로세로 비율을 지키며 줄이고, 너무 작으면 가장 작은 크기로 올린다.
        /// 모니터의 크기를 알 수 없으면(0) 바라는 크기 그대로다.
        /// </summary>
        public static Vector2Int FitWindow(Vector2Int wanted, int displayWidth, int displayHeight)
        {
            int width = Mathf.Max(wanted.x, MinWindowWidth);
            int height = Mathf.Max(wanted.y, MinWindowHeight);
            if (displayWidth <= 0 || displayHeight <= 0) return new Vector2Int(width, height);

            float scale = Mathf.Min(1f, displayWidth * WindowFit / width, displayHeight * WindowFit / height);
            width = Mathf.Max(MinWindowWidth, Mathf.RoundToInt(width * scale));
            height = Mathf.Max(MinWindowHeight, Mathf.RoundToInt(height * scale));
            return new Vector2Int(width, height);
        }

        /// <summary>창으로 돌아올 때 쓸 크기를 적어 둔다. 너무 작은 크기는 적지 않는다.</summary>
        public static void RememberWindow(int width, int height)
        {
            if (width < MinWindowWidth || height < MinWindowHeight) return;

            PlayerPrefs.SetInt(WindowWidthKey, width);
            PlayerPrefs.SetInt(WindowHeightKey, height);
        }
    }

    /// <summary>
    /// 실제 게임 창. 방식을 바꾸는 것은 그 프레임이 끝난 뒤에 이루어지므로, 바꾼 직후 잠깐은 부탁한 방식을 지금 방식으로 알려 준다
    /// (그러지 않으면 설정 화면의 단추가 눌렀다가 되돌아갔다가 다시 바뀐다).
    /// 에디터에서는 전체 화면이 되지 않는다.
    /// </summary>
    public sealed class UnityScreenDevice : IScreenDevice
    {
        private const float SettleSeconds = 0.5f;

        private bool requested;
        private float requestedUntil = -1f;

        public bool Fullscreen => Time.unscaledTime < requestedUntil ? requested : Screen.fullScreen;

        public int Width => Screen.width;

        public int Height => Screen.height;

        public int DisplayWidth => Display.main != null ? Display.main.systemWidth : 0;

        public int DisplayHeight => Display.main != null ? Display.main.systemHeight : 0;

        public void Apply(int width, int height, bool fullscreen)
        {
            requested = fullscreen;
            requestedUntil = Time.unscaledTime + SettleSeconds;

            // 테두리 없는 전체 화면: 다른 창으로 오갈 때 화면이 깜빡이지 않는다.
            Screen.SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
    }
}
