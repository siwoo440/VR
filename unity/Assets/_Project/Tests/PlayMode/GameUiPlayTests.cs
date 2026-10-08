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
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 게임 화면을 실제로 실행해 확인한다: 메뉴 열고 닫기, 부품 칸, 사람들 목록, 1인칭·3인칭, 설정.
    /// 키와 마우스는 InputTestFixture의 가상 장치로 넣고, 화면의 단추는 onClick을 직접 불러 누른다.
    /// </summary>
    public class GameUiPlayTests : InputTestFixture
    {
        private const string SandboxScene = "Sandbox";
        private const int TestFrameRate = 60;
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;

        private int previousFrameRate;
        private float savedLook;
        private float savedFieldOfView;
        private bool savedPeopleList;
        private Action savedExitHandler;
        private Keyboard keyboard;
        private Mouse mouse;
        private GameUi ui;
        private DesktopPlayerController player;

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
            AppExit.Handler = savedExitHandler;
            GameSettings.LookSensitivity = savedLook;
            GameSettings.FieldOfView = savedFieldOfView;
            GameSettings.ShowPeopleList = savedPeopleList;
            Application.targetFrameRate = previousFrameRate;

            base.TearDown();
        }

        [UnityTest]
        public IEnumerator 처음에는_메뉴가_닫혀_있고_화면_요소가_준비되어_있다()
        {
            yield return LoadSandbox();

            Assert.IsFalse(ui.IsMenuOpen);
            Assert.IsTrue(ui.IsPeopleListVisible);
            Assert.AreEqual(9, ui.Hotbar.SlotCount);
            Assert.AreEqual(HotbarModel.None, ui.Hotbar.SelectedIndex);
            Assert.IsTrue(player.IsFirstPerson);
            Assert.IsFalse(player.LookCaptured);

            PeopleListView people = ui.GetComponentInChildren<PeopleListView>();
            Assert.AreEqual($"사람들 1/{RoomRules.MaxPeople}", people.HeaderText);

            Nameplate nameplate = player.GetComponentInChildren<Nameplate>(true);
            Assert.AreEqual("손님", nameplate.DisplayName);
        }

        [UnityTest]
        public IEnumerator Esc_키로_메뉴를_열면_조작이_막히고_다시_누르면_돌아간다()
        {
            yield return LoadSandbox();

            yield return Tap(keyboard.escapeKey);
            Assert.IsTrue(ui.IsMenuOpen);
            Assert.IsTrue(player.InputBlocked);
            Assert.IsFalse(player.LookCaptured);

            Vector3 before = player.transform.position;
            Press(keyboard.wKey);
            yield return new WaitForSeconds(0.4f);
            Release(keyboard.wKey);
            yield return null;

            Vector3 moved = player.transform.position - before;
            moved.y = 0f;
            Assert.Less(moved.magnitude, 0.05f, "메뉴가 열려 있는데 캐릭터가 걸었습니다.");

            yield return Tap(keyboard.escapeKey);
            Assert.IsFalse(ui.IsMenuOpen);
            Assert.IsFalse(player.InputBlocked);
            Assert.IsTrue(player.LookCaptured, "메뉴를 닫으면 바로 시점 조작으로 돌아가야 합니다.");
        }

        [UnityTest]
        public IEnumerator 숫자_키로_부품_칸을_고르고_같은_키로_푼다()
        {
            yield return LoadSandbox();

            yield return Tap(keyboard.digit2Key);
            Assert.AreEqual(1, ui.Hotbar.SelectedIndex);
            Assert.AreEqual("블루 블록", ui.Hotbar.SelectedItemName);

            yield return Tap(keyboard.digit4Key);
            Assert.AreEqual(3, ui.Hotbar.SelectedIndex);

            yield return Tap(keyboard.digit4Key);
            Assert.AreEqual(HotbarModel.None, ui.Hotbar.SelectedIndex);

            yield return Tap(keyboard.digit9Key);
            Assert.AreEqual(HotbarModel.None, ui.Hotbar.SelectedIndex, "빈 칸은 고를 수 없어야 합니다.");
        }

        [UnityTest]
        public IEnumerator 메뉴가_열려_있으면_숫자_키가_부품_칸을_바꾸지_않는다()
        {
            yield return LoadSandbox();

            ui.OpenMenu();
            yield return null;
            yield return Tap(keyboard.digit1Key);

            Assert.AreEqual(HotbarModel.None, ui.Hotbar.SelectedIndex);
        }

        [UnityTest]
        public IEnumerator 휠을_당기면_3인칭이_되어_몸이_보이고_밀면_1인칭으로_돌아온다()
        {
            yield return LoadSandbox();
            Assert.IsTrue(player.Avatar.IsFirstPerson);

            yield return Scroll(-1f);
            Assert.IsFalse(player.IsFirstPerson);
            Assert.AreEqual(2.5f, player.ViewDistance, 0.01f, "휠 한 칸에 한 단계만 바뀌어야 합니다.");

            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(-2.5f, player.ViewCamera.transform.localPosition.z, 0.1f);
            Assert.IsFalse(player.Avatar.IsFirstPerson, "3인칭에서는 몸이 보여야 합니다.");

            yield return Scroll(1f);
            Assert.IsTrue(player.IsFirstPerson);

            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(0f, player.ViewCamera.transform.localPosition.z, 0.01f);
            Assert.IsTrue(player.Avatar.IsFirstPerson);
        }

        [UnityTest]
        public IEnumerator 화면을_눌러_마우스를_잡은_뒤에만_시점이_돈다()
        {
            yield return LoadSandbox();

            float startYaw = player.transform.eulerAngles.y;
            Set(mouse.delta, new Vector2(120f, 0f));
            yield return null;
            yield return null;
            Assert.AreEqual(0f, Mathf.DeltaAngle(startYaw, player.transform.eulerAngles.y), 0.01f, "마우스를 잡기 전에는 시점이 돌지 않아야 합니다.");

            Set(mouse.delta, Vector2.zero);
            yield return Tap(mouse.leftButton);
            Assert.IsTrue(player.LookCaptured);

            Set(mouse.delta, new Vector2(120f, 0f));
            yield return null;
            yield return null;
            Set(mouse.delta, Vector2.zero);
            yield return null;

            Assert.Greater(Mathf.DeltaAngle(startYaw, player.transform.eulerAngles.y), 5f);
        }

        [UnityTest]
        public IEnumerator 메뉴의_시작_위치로_단추는_캐릭터를_되돌리고_메뉴를_닫는다()
        {
            yield return LoadSandbox();
            Vector3 start = player.transform.position;

            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.position = start + new Vector3(3f, 0f, 4f);
            body.enabled = true;
            yield return null;

            ui.OpenMenu();
            yield return null;
            Find<Button>(ui.Menu, "Respawn").onClick.Invoke();
            yield return null;

            Assert.IsFalse(ui.IsMenuOpen);
            Assert.Less((player.transform.position - start).magnitude, 0.3f);
        }

        [UnityTest]
        public IEnumerator 메뉴의_시점_타일은_3인칭으로_바꾸고_메뉴를_닫는다()
        {
            yield return LoadSandbox();

            ui.OpenMenu();
            yield return null;
            Find<Button>(ui.Menu, "ViewTile").onClick.Invoke();
            yield return null;

            Assert.IsFalse(ui.IsMenuOpen);
            Assert.IsFalse(player.IsFirstPerson);
        }

        [UnityTest]
        public IEnumerator 메뉴의_탭을_누르면_그_쪽만_보인다()
        {
            yield return LoadSandbox();

            ui.OpenMenu();
            yield return null;
            Assert.AreEqual(0, ui.Menu.CurrentTab);

            Find<Button>(ui.Menu, "Tab2").onClick.Invoke();
            yield return null;

            Assert.AreEqual(2, ui.Menu.CurrentTab);
            Assert.IsTrue(Find<Transform>(ui.Menu, "SettingsPage").gameObject.activeInHierarchy);
            Assert.IsFalse(Find<Transform>(ui.Menu, "ShortcutPage").gameObject.activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator Tab_키로_사람들_목록을_끄고_켠다()
        {
            yield return LoadSandbox();

            yield return Tap(keyboard.tabKey);
            Assert.IsFalse(ui.IsPeopleListVisible);
            Assert.IsFalse(GameSettings.ShowPeopleList);

            yield return Tap(keyboard.tabKey);
            Assert.IsTrue(ui.IsPeopleListVisible);
        }

        [UnityTest]
        public IEnumerator 설정에서_바꾼_값은_저장되고_바로_적용된다()
        {
            yield return LoadSandbox();

            ui.OpenMenu();
            ui.Menu.ShowTab(2);
            yield return null;

            Find<Slider>(ui.Menu, "FieldOfViewSlider").value = 90f;
            Find<Slider>(ui.Menu, "LookSlider").value = GameSettings.MaxLookSensitivity;
            Find<Toggle>(ui.Menu, "PeopleToggle").isOn = false;
            yield return null;

            Assert.AreEqual(90f, GameSettings.FieldOfView, 0.01f);
            Assert.AreEqual(90f, player.ViewCamera.fieldOfView, 0.01f);
            Assert.AreEqual(GameSettings.MaxLookSensitivity, GameSettings.LookSensitivity, 0.0001f);
            Assert.IsFalse(ui.IsPeopleListVisible);

            Find<Button>(ui.Menu, "Reset").onClick.Invoke();
            yield return null;

            Assert.AreEqual(GameSettings.DefaultFieldOfView, player.ViewCamera.fieldOfView, 0.01f);
            Assert.IsTrue(ui.IsPeopleListVisible);
        }

        [UnityTest]
        public IEnumerator 게임_끝내기는_두_번_눌러야_요청된다()
        {
            int exits = 0;
            AppExit.Handler = () => exits++;
            yield return LoadSandbox();

            ui.OpenMenu();
            yield return null;
            Button quit = Find<Button>(ui.Menu, "Quit");

            quit.onClick.Invoke();
            yield return null;
            Assert.AreEqual(0, exits);
            Assert.IsTrue(ui.Menu.IsQuitArmed);

            quit.onClick.Invoke();
            yield return null;
            Assert.AreEqual(1, exits);
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = ReadArgument("-captureDir");
            if (string.IsNullOrEmpty(directory)) Assert.Ignore("-captureDir 인자가 없어 그림을 찍지 않습니다.");

            yield return LoadSandbox();
            Directory.CreateDirectory(directory);

            // 화면에 겹쳐 그리는 캔버스는 카메라 그림에 찍히지 않으므로, 찍는 동안만 캡처 카메라 앞에 붙인다.
            var target = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var holder = new GameObject("CaptureCamera");
            var capture = holder.AddComponent<Camera>();
            capture.targetTexture = target;

            Canvas canvas = Find<Canvas>(ui, "Canvas");
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = capture;
            canvas.planeDistance = 0.5f;

            player.CaptureLook(true);
            ui.Hotbar.Select(1);
            yield return null;
            yield return null;
            Save(capture, target, Path.Combine(directory, "hud-first-person.png"));

            player.SetViewDistance(5f);
            Press(keyboard.wKey);
            yield return new WaitForSeconds(0.9f);
            Save(capture, target, Path.Combine(directory, "hud-third-person.png"));
            Release(keyboard.wKey);
            yield return null;

            ui.OpenMenu();
            yield return null;
            yield return null;
            Save(capture, target, Path.Combine(directory, "menu-shortcuts.png"));

            string[] names = { "menu-people.png", "menu-settings.png", "menu-help.png" };
            for (int i = 0; i < names.Length; i++)
            {
                ui.Menu.ShowTab(i + 1);
                yield return null;
                yield return null;
                Save(capture, target, Path.Combine(directory, names[i]));
            }

            capture.targetTexture = null;
            UnityEngine.Object.Destroy(holder);
            target.Release();
            UnityEngine.Object.Destroy(target);
        }

        private IEnumerator LoadSandbox()
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
        private IEnumerator Tap(ButtonControl button)
        {
            Press(button);
            yield return null;
            yield return null;
            Release(button);
            yield return null;
            yield return null;
        }

        private IEnumerator Scroll(float notches)
        {
            Set(mouse.scroll, new Vector2(0f, notches));
            yield return null;
            yield return null;
            Set(mouse.scroll, Vector2.zero);
            yield return null;
            yield return null;
        }

        private static T Find<T>(Component root, string name) where T : Component
        {
            foreach (T candidate in root.GetComponentsInChildren<T>(true))
            {
                if (candidate.gameObject.name == name) return candidate;
            }

            Assert.Fail($"{root.name} 아래에서 {name}({typeof(T).Name})을 찾을 수 없습니다.");
            return null;
        }

        private void Save(Camera capture, RenderTexture target, string path)
        {
            Camera source = player.ViewCamera;
            capture.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            capture.fieldOfView = source.fieldOfView;
            capture.nearClipPlane = source.nearClipPlane;
            capture.clearFlags = source.clearFlags;
            capture.backgroundColor = source.backgroundColor;

            Canvas.ForceUpdateCanvases();
            capture.Render();

            var picture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            picture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            picture.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(path, picture.EncodeToPNG());
            UnityEngine.Object.Destroy(picture);
        }

        private static string ReadArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == name) return arguments[i + 1];
            }

            return null;
        }
    }
}
