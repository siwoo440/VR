using System;
using System.Collections;
using System.IO;
using AtelierVerse.Core;
using AtelierVerse.Player;
using AtelierVerse.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 게임을 실제로 실행하는 테스트의 공통 바탕. 가상 키보드와 마우스, 초당 60프레임 고정, 개인 설정 되돌리기,
    /// Sandbox 씬 불러오기, 화면 그림 찍기를 맡는다.
    /// </summary>
    public abstract class PlayTestBase : InputTestFixture
    {
        protected const string SandboxScene = "Sandbox";

        private const int TestFrameRate = 60;
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;

        private int previousFrameRate;
        private float savedLook;
        private float savedFieldOfView;
        private bool savedPeopleList;
        private Action savedExitHandler;
        private RenderTexture captureTarget;
        private Camera captureCamera;

        protected Keyboard keyboard;
        protected Mouse mouse;
        protected GameUi ui;
        protected DesktopPlayerController player;

        public override void Setup()
        {
            base.Setup();

            previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = TestFrameRate;

            savedLook = GameSettings.LookSensitivity;
            savedFieldOfView = GameSettings.FieldOfView;
            savedPeopleList = GameSettings.ShowPeopleList;
            GameSettings.ResetToDefaults();

            savedExitHandler = AppExit.Handler;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            EndCapture();

            AppExit.Handler = savedExitHandler;
            GameSettings.LookSensitivity = savedLook;
            GameSettings.FieldOfView = savedFieldOfView;
            GameSettings.ShowPeopleList = savedPeopleList;
            Application.targetFrameRate = previousFrameRate;

            base.TearDown();
        }

        protected IEnumerator LoadSandbox()
        {
            yield return SceneManager.LoadSceneAsync(SandboxScene, LoadSceneMode.Single);
            yield return null;

            ui = UnityEngine.Object.FindAnyObjectByType<GameUi>();
            player = UnityEngine.Object.FindAnyObjectByType<DesktopPlayerController>();
            Assert.IsNotNull(ui, "Sandbox 씬에서 게임 화면(GameUI)을 찾을 수 없습니다.");
            Assert.IsNotNull(player, "Sandbox 씬에서 PC 캐릭터를 찾을 수 없습니다.");

            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>키나 마우스 단추를 한 번 눌렀다 뗀다. 누른 것이 한 프레임 동안 보이도록 사이에 프레임을 둔다.</summary>
        protected IEnumerator Tap(ButtonControl button)
        {
            Press(button);
            yield return null;
            yield return null;
            Release(button);
            yield return null;
            yield return null;
        }

        protected IEnumerator Scroll(float notches)
        {
            Set(mouse.scroll, new Vector2(0f, notches));
            yield return null;
            yield return null;
            Set(mouse.scroll, Vector2.zero);
            yield return null;
            yield return null;
        }

        protected static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        protected static T Find<T>(Component root, string name) where T : Component
        {
            foreach (T candidate in root.GetComponentsInChildren<T>(true))
            {
                if (candidate.gameObject.name == name) return candidate;
            }

            Assert.Fail($"{root.name} 아래에서 {name}({typeof(T).Name})을 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 화면 그림을 저장할 폴더. 명령줄에 -captureDir 인자가 없으면 테스트를 건너뛴다.
        /// </summary>
        protected static string RequireCaptureDirectory()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] != "-captureDir") continue;

                Directory.CreateDirectory(arguments[i + 1]);
                return arguments[i + 1];
            }

            Assert.Ignore("-captureDir 인자가 없어 그림을 찍지 않습니다.");
            return null;
        }

        /// <summary>
        /// 화면에 겹쳐 그리는 캔버스는 카메라 그림에 찍히지 않으므로, 찍는 동안만 캡처 카메라 앞에 붙인다.
        /// </summary>
        protected void BeginCapture()
        {
            captureTarget = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            captureCamera = new GameObject("CaptureCamera").AddComponent<Camera>();
            captureCamera.targetTexture = captureTarget;

            Canvas canvas = Find<Canvas>(ui, "Canvas");
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = captureCamera;
            canvas.planeDistance = 0.5f;
        }

        /// <summary>캐릭터의 카메라가 보는 장면을 화면 요소와 함께 PNG로 저장한다.</summary>
        protected void SaveCapture(string path)
        {
            Camera source = player.ViewCamera;
            captureCamera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            captureCamera.fieldOfView = source.fieldOfView;
            captureCamera.nearClipPlane = source.nearClipPlane;
            captureCamera.clearFlags = source.clearFlags;
            captureCamera.backgroundColor = source.backgroundColor;

            Canvas.ForceUpdateCanvases();
            captureCamera.Render();

            var picture = new Texture2D(captureTarget.width, captureTarget.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = captureTarget;
            picture.ReadPixels(new Rect(0, 0, captureTarget.width, captureTarget.height), 0, 0);
            picture.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(path, picture.EncodeToPNG());
            UnityEngine.Object.Destroy(picture);
        }

        private void EndCapture()
        {
            if (captureCamera != null)
            {
                captureCamera.targetTexture = null;
                UnityEngine.Object.Destroy(captureCamera.gameObject);
                captureCamera = null;
            }

            if (captureTarget != null)
            {
                captureTarget.Release();
                UnityEngine.Object.Destroy(captureTarget);
                captureTarget = null;
            }
        }
    }
}
