using AtelierVerse.Core;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 화면 방식(창, 전체 화면)을 바꾸는 규칙의 검사(20일차). 실제 창 대신 가짜 창을 끼워 무엇을 부탁하는지만 본다.
    /// 실제로 전체 화면이 되는지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class ScreenControlTests
    {
        private sealed class FakeScreen : IScreenDevice
        {
            public bool Fullscreen { get; set; }

            public int Width { get; set; } = 1280;

            public int Height { get; set; } = 720;

            public int DisplayWidth { get; set; } = 2560;

            public int DisplayHeight { get; set; } = 1440;

            public int ApplyCount { get; private set; }

            public void Apply(int width, int height, bool fullscreen)
            {
                Width = width;
                Height = height;
                Fullscreen = fullscreen;
                ApplyCount++;
            }
        }

        private IScreenDevice savedDevice;
        private bool hadWindow;
        private Vector2Int savedWindow;
        private FakeScreen screen;

        [SetUp]
        public void Setup()
        {
            savedDevice = ScreenControl.Device;
            hadWindow = ScreenControl.HasRememberedWindow;
            savedWindow = ScreenControl.RememberedWindow;
            ScreenControl.ForgetWindow();

            screen = new FakeScreen();
            ScreenControl.Device = screen;
        }

        [TearDown]
        public void TearDown()
        {
            ScreenControl.Device = savedDevice;
            ScreenControl.ForgetWindow();
            if (hadWindow) ScreenControl.RememberWindow(savedWindow.x, savedWindow.y);
        }

        [Test]
        public void 창은_모니터에_들어가는_크기로_맞춘다()
        {
            var wanted = new Vector2Int(1600, 900);

            Assert.AreEqual(new Vector2Int(1600, 900), ScreenControl.FitWindow(wanted, 2560, 1440), "들어가는 크기는 그대로 둡니다.");
            Assert.AreEqual(new Vector2Int(1600, 900), ScreenControl.FitWindow(wanted, 0, 0), "모니터의 크기를 모르면 바라는 크기 그대로입니다.");

            // 1366×768 모니터: 9할은 1229×691이고, 1600×900을 같은 비율로 줄이면 거기에 맞는다.
            Vector2Int small = ScreenControl.FitWindow(wanted, 1366, 768);
            Assert.LessOrEqual(small.x, Mathf.RoundToInt(1366 * ScreenControl.WindowFit));
            Assert.LessOrEqual(small.y, Mathf.RoundToInt(768 * ScreenControl.WindowFit));
            Assert.AreEqual(16f / 9f, small.x / (float)small.y, 0.01f, "줄일 때 가로세로 비율을 지킵니다.");

            // 1920×1080 모니터에 1920×1080 창은 제목 줄이 들어갈 자리가 없으므로 줄인다.
            Vector2Int full = ScreenControl.FitWindow(new Vector2Int(1920, 1080), 1920, 1080);
            Assert.AreEqual(new Vector2Int(1728, 972), full);
        }

        [Test]
        public void 창은_가장_작은_크기보다_작아지지_않는다()
        {
            var smallest = new Vector2Int(ScreenControl.MinWindowWidth, ScreenControl.MinWindowHeight);

            Assert.AreEqual(smallest, ScreenControl.FitWindow(new Vector2Int(320, 200), 2560, 1440));
            Assert.AreEqual(smallest, ScreenControl.FitWindow(new Vector2Int(1600, 900), 800, 600), "아주 작은 모니터에서도 가장 작은 크기는 지킵니다.");
        }

        [Test]
        public void 전체_화면으로_가면_모니터의_크기가_되고_창으로_돌아오면_전의_크기다()
        {
            Assert.IsFalse(ScreenControl.IsFullscreen);

            Assert.IsTrue(ScreenControl.SetFullscreen(true));
            Assert.IsTrue(screen.Fullscreen);
            Assert.AreEqual(2560, screen.Width);
            Assert.AreEqual(1440, screen.Height);
            Assert.AreEqual(new Vector2Int(1280, 720), ScreenControl.RememberedWindow, "창의 크기를 적어 두어야 돌아올 수 있습니다.");

            Assert.IsTrue(ScreenControl.SetFullscreen(false));
            Assert.IsFalse(screen.Fullscreen);
            Assert.AreEqual(1280, screen.Width);
            Assert.AreEqual(720, screen.Height);
            Assert.AreEqual(2, screen.ApplyCount);
        }

        [Test]
        public void 이미_그_방식이면_창을_건드리지_않는다()
        {
            Assert.IsFalse(ScreenControl.SetFullscreen(false));
            Assert.AreEqual(0, screen.ApplyCount);

            screen.Fullscreen = true;
            Assert.IsFalse(ScreenControl.SetFullscreen(true));
            Assert.AreEqual(0, screen.ApplyCount);
            Assert.IsFalse(ScreenControl.HasRememberedWindow, "바꾸지 않았는데 크기를 적었습니다.");
        }

        [Test]
        public void 적어_둔_크기가_없으면_처음_크기의_창으로_돌아온다()
        {
            // 전체 화면으로 켜진 채 시작한 경우(적어 둔 창 크기가 없다).
            screen.Fullscreen = true;
            screen.Width = 2560;
            screen.Height = 1440;

            Assert.IsTrue(ScreenControl.SetFullscreen(false));
            Assert.AreEqual(ScreenControl.DefaultWindowWidth, screen.Width);
            Assert.AreEqual(ScreenControl.DefaultWindowHeight, screen.Height);
        }

        [Test]
        public void 모니터의_크기를_모르면_지금_크기_그대로_전체_화면으로_간다()
        {
            screen.DisplayWidth = 0;
            screen.DisplayHeight = 0;

            Assert.IsTrue(ScreenControl.SetFullscreen(true));
            Assert.IsTrue(screen.Fullscreen);
            Assert.AreEqual(1280, screen.Width);
            Assert.AreEqual(720, screen.Height);
        }

        [Test]
        public void 너무_작은_창의_크기는_적어_두지_않는다()
        {
            ScreenControl.RememberWindow(200, 100);
            Assert.IsFalse(ScreenControl.HasRememberedWindow);
            Assert.AreEqual(new Vector2Int(ScreenControl.DefaultWindowWidth, ScreenControl.DefaultWindowHeight), ScreenControl.RememberedWindow);

            ScreenControl.RememberWindow(1400, 800);
            Assert.AreEqual(new Vector2Int(1400, 800), ScreenControl.RememberedWindow);
        }

        [Test]
        public void 기본값으로_돌리면_전체_화면에서_처음_크기의_창으로_돌아온다()
        {
            ScreenControl.SetFullscreen(true);
            Assert.IsTrue(ScreenControl.HasRememberedWindow);

            ScreenControl.ResetToDefault();

            Assert.IsFalse(screen.Fullscreen);
            Assert.AreEqual(ScreenControl.DefaultWindowWidth, screen.Width, "적어 둔 크기(1280)가 아니라 처음 크기로 돌아와야 합니다.");
            Assert.AreEqual(ScreenControl.DefaultWindowHeight, screen.Height);
            Assert.IsFalse(ScreenControl.HasRememberedWindow);
        }

        [Test]
        public void 창일_때_기본값으로_돌려도_창의_크기는_그대로다()
        {
            screen.Width = 1400;
            screen.Height = 800;

            ScreenControl.ResetToDefault();

            Assert.AreEqual(0, screen.ApplyCount, "끌어서 맞춘 창의 크기를 바꾸면 안 됩니다.");
            Assert.AreEqual(1400, screen.Width);
        }
    }
}
