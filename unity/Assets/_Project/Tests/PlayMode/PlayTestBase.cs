using System;
using System.Collections;
using System.IO;
using AtelierVerse.Core;
using AtelierVerse.Player;
using AtelierVerse.UI;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 게임을 실제로 실행하는 테스트의 공통 바탕. 가상 키보드와 마우스, 초당 60프레임 고정, 개인 설정 되돌리기,
    /// 가짜 창(화면 방식을 바꿔도 실제 창은 건드리지 않음)과 화면 품질 되돌리기,
    /// Sandbox 씬 불러오기, 화면 그림 찍기를 맡는다.
    /// 맵 파일은 테스트마다 새 임시 폴더에 두어 이 기기의 실제 저장 파일을 건드리지 않는다.
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
        private int savedSnapLevel;
        private string savedHotbarParts;
        private float savedSoundVolume;
        private bool savedInvertLook;
        private int savedQuality;
        private bool savedVSync;
        private IScreenDevice savedScreen;
        private bool hadWindow;
        private Vector2Int savedWindow;
        private Action savedExitHandler;
        private RenderTexture captureTarget;
        private Camera captureCamera;

        protected Keyboard keyboard;
        protected Mouse mouse;
        protected GameUi ui;
        protected DesktopPlayerController player;

        /// <summary>테스트 동안 실제 창 대신 쓰이는 가짜 창. 화면 방식을 바꾸면 여기에 적힌다.</summary>
        protected FakeScreen screen;

        protected sealed class FakeScreen : IScreenDevice
        {
            public bool Fullscreen { get; set; }

            public int Width { get; set; } = 1600;

            public int Height { get; set; } = 900;

            public int DisplayWidth { get; set; } = 2560;

            public int DisplayHeight { get; set; } = 1440;

            /// <summary>창의 크기나 방식을 바꾼 횟수.</summary>
            public int ApplyCount { get; private set; }

            public void Apply(int width, int height, bool fullscreen)
            {
                Width = width;
                Height = height;
                Fullscreen = fullscreen;
                ApplyCount++;
            }
        }

        private static readonly string TestMapRoot = Path.Combine(Path.GetTempPath(), "atelier-verse-tests");

        /// <summary>
        /// 처음 선 자리(0.5, 0, -5.5)에서 45도 아래를 보고 놓은 블록이 놓이는 자리쯤. 눈높이만큼 앞의 바닥이며 칸의 가운데가 아니다.
        /// 블록은 가리킨 그 자리에 놓이므로, 테스트는 이 근처(BlockNear의 반경)에 블록이 있는지를 본다.
        /// </summary>
        protected static readonly Vector3 AimedFloorSpot = new Vector3(0.5f, 0.5f, -3.95f);

        /// <summary>이 테스트의 맵 파일 폴더. 첫 LoadSandbox에서 저장 위치가 되고, 테스트가 끝나면 지운다.</summary>
        protected string mapDirectory;

        public override void Setup()
        {
            base.Setup();

            // 저장 위치는 여기서 바꾸지 않는다. 앞 테스트의 씬이 아직 떠 있어, 바꾸면 그 씬의 남은 변경이 이 테스트의 폴더에 쓰인다.
            mapDirectory = Path.Combine(TestMapRoot, Path.GetRandomFileName());

            // 조작 방식은 테스트마다 기본(키보드·마우스)으로 돌려 둔다. VR 테스트만 씬을 열기 전에 바꾼다.
            PlayerModeSwitch.Forced = null;

            previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = TestFrameRate;

            savedLook = GameSettings.LookSensitivity;
            savedFieldOfView = GameSettings.FieldOfView;
            savedPeopleList = GameSettings.ShowPeopleList;
            savedSnapLevel = GameSettings.SnapLevel;
            savedHotbarParts = GameSettings.HotbarParts;
            savedSoundVolume = GameSettings.SoundVolume;
            savedInvertLook = GameSettings.InvertLookY;
            savedQuality = GameSettings.Quality;
            savedVSync = GameSettings.VSync;
            GameSettings.ResetToDefaults();

            // 테스트는 초당 60프레임으로 고정해 돌린다. 수직 동기화가 켜져 있으면 그 고정이 듣지 않을 수 있어 끄고 시작한다.
            GameSettings.VSync = false;

            // 화면 방식을 바꾸는 테스트가 실제 창을 건드리지 않게 가짜 창을 끼운다.
            hadWindow = ScreenControl.HasRememberedWindow;
            savedWindow = ScreenControl.RememberedWindow;
            ScreenControl.ForgetWindow();
            savedScreen = ScreenControl.Device;
            screen = new FakeScreen();
            ScreenControl.Device = screen;

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
            GameSettings.SnapLevel = savedSnapLevel;
            GameSettings.HotbarParts = savedHotbarParts;
            GameSettings.SoundVolume = savedSoundVolume;
            GameSettings.InvertLookY = savedInvertLook;
            GameSettings.Quality = savedQuality;
            GameSettings.VSync = savedVSync;

            // 에디터에서는 실행 중에 바꾼 품질 단계와 수직 동기화가 프로젝트 설정 파일에 남는다. 테스트마다 바꾸기 전으로 돌려놓는다.
            GraphicsQuality.Restore();

            ScreenControl.Device = savedScreen;
            ScreenControl.ForgetWindow();
            if (hadWindow) ScreenControl.RememberWindow(savedWindow.x, savedWindow.y);

            Application.targetFrameRate = previousFrameRate;

            // 아직 떠 있는 씬의 남은 변경을 지금 저장해 두어야, 다음 테스트가 씬을 바꿀 때 그 변경이 다음 테스트의 폴더로 새지 않는다.
            MapAutoSave autoSave = UnityEngine.Object.FindAnyObjectByType<MapAutoSave>();
            if (autoSave != null && autoSave.HasPendingChanges) autoSave.SaveNow();
            if (Directory.Exists(mapDirectory)) Directory.Delete(mapDirectory, true);
            PlayerModeSwitch.Forced = null;

            base.TearDown();
        }

        protected IEnumerator LoadSandbox()
        {
            PrepareMapDirectory();
            yield return SceneManager.LoadSceneAsync(SandboxScene, LoadSceneMode.Single);
            yield return null;

            ui = UnityEngine.Object.FindAnyObjectByType<GameUi>();
            player = UnityEngine.Object.FindAnyObjectByType<DesktopPlayerController>();
            Assert.IsNotNull(ui, "Sandbox 씬에서 게임 화면(GameUI)을 찾을 수 없습니다.");
            Assert.IsNotNull(player, "Sandbox 씬에서 PC 캐릭터를 찾을 수 없습니다.");

            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>
        /// 이 테스트의 맵 폴더로 저장 위치를 바꾼다. 앞 테스트의 씬에 남은 변경은 바꾸기 전에 앞 테스트의 폴더로 저장해,
        /// 씬이 내려갈 때의 저장이 이 테스트의 파일이 되지 않게 한다. 같은 테스트 안에서 두 번 부르면 아무것도 하지 않는다.
        /// 씬을 열기 전에 맵 파일을 미리 써 두는 테스트는 파일을 쓰기 전에 직접 부른다.
        /// </summary>
        protected void PrepareMapDirectory()
        {
            if (MapStorage.Directory == mapDirectory) return;

            MapAutoSave previous = UnityEngine.Object.FindAnyObjectByType<MapAutoSave>();
            if (previous != null && previous.HasPendingChanges) previous.SaveNow();

            string old = MapStorage.Directory;
            MapStorage.Directory = mapDirectory;
            if (old.StartsWith(TestMapRoot, StringComparison.Ordinal) && Directory.Exists(old)) Directory.Delete(old, true);
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

        /// <summary>마우스를 잡고, 아래로 pitch도를 보고, 숫자 키로 부품을 고른 뒤 조준이 잡힐 때까지 기다린다.</summary>
        protected IEnumerator AimWithPart(float pitch, KeyControl partKey)
        {
            player.CaptureLook(true);
            player.SetLook(0f, pitch);
            yield return Tap(partKey);
            yield return Frames(2);
        }

        /// <summary>Ctrl을 누른 채 키를 한 번 눌렀다 뗀다.</summary>
        protected IEnumerator TapWithCtrl(KeyControl key)
        {
            Press(keyboard.leftCtrlKey);
            yield return null;
            yield return Tap(key);
            Release(keyboard.leftCtrlKey);
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

        /// <summary>모눈 칸의 가운데 자리. 테스트에서 블록을 놓을 자리를 칸 번호로 적을 때 쓴다(블록 한 변이 1이다).</summary>
        protected static Vector3 CellCenter(int x, int y, int z)
        {
            return GridMath.CellToWorldCenter(new Vector3Int(x, y, z));
        }

        /// <summary>position 근처에 가운데가 있는 블록의 기록을 찾는다. 옆 블록(1 떨어짐)과 헷갈리지 않게 반경은 반 변보다 작게 둔다.</summary>
        protected static bool BlockNear(BlockWorld world, Vector3 position, out BlockRecord record, float radius = 0.3f)
        {
            return world.TryFindNear(position, radius, out record);
        }

        protected static bool HasBlockNear(BlockWorld world, Vector3 position, float radius = 0.3f)
        {
            return world.TryFindNear(position, radius, out _);
        }

        protected static bool TryGetPartNear(BlockWorld world, Vector3 position, out int part)
        {
            bool found = world.TryFindNear(position, 0.3f, out BlockRecord record);
            part = found ? record.Part : -1;
            return found;
        }

        /// <summary>position 근처의 블록을 화면에서(오브젝트로) 찾는다. 기록과 화면이 함께 맞는지 볼 때 쓴다.</summary>
        protected static PlacedBlock FindViewNear(BlockWorld world, Vector3 position, float radius = 0.3f)
        {
            PlacedBlock nearest = null;
            float best = radius;
            foreach (PlacedBlock block in world.GetComponentsInChildren<PlacedBlock>())
            {
                float distance = Vector3.Distance(block.transform.position, position);
                if (distance > best) continue;

                best = distance;
                nearest = block;
            }

            return nearest;
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
        /// VR에서처럼 화면이 눈앞의 판으로 떠 있으면 그대로 찍힌다.
        /// </summary>
        protected void BeginCapture()
        {
            captureTarget = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            captureCamera = new GameObject("CaptureCamera").AddComponent<Camera>();
            captureCamera.targetTexture = captureTarget;

            Canvas canvas = Find<Canvas>(ui, "Canvas");
            if (canvas.renderMode == RenderMode.WorldSpace) return;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = captureCamera;
            canvas.planeDistance = 0.5f;
        }

        /// <summary>캐릭터의 카메라가 보는 장면을 화면 요소와 함께 PNG로 저장한다.</summary>
        protected void SaveCapture(string path)
        {
            Camera source = player.Rig.ViewCamera;
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
