using System.Collections;
using AtelierVerse.Core;
using AtelierVerse.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 늘어난 설정의 검사(20일차): 설정의 갈래, 화면 방식, 화면 품질, 수직 동기화, 위아래 시점 반대로, 모두 기본값으로.
    /// 화면 방식은 가짜 창으로 확인하므로 실제로 전체 화면이 되는지는 확인되지 않는다.
    /// 화면 품질은 그리기 단계가 바뀌는지까지 보고, 품질마다 어떻게 보이는지는 보지 않는다.
    /// </summary>
    public class SettingsPlayTests : PlayTestBase
    {
        private SettingsPanel settings;

        [UnityTest]
        public IEnumerator 설정은_세_갈래로_나뉘고_고른_갈래만_보인다()
        {
            yield return OpenSettings();

            Assert.AreEqual(3, settings.SectionCount);
            Assert.AreEqual(0, settings.CurrentSection, "처음에는 화면 갈래가 보입니다.");
            AssertSection("ScreenSection");

            Find<Button>(ui.Menu, "Section1").onClick.Invoke();
            yield return null;
            Assert.AreEqual(1, settings.CurrentSection);
            AssertSection("ControlSection");
            Assert.AreEqual(1, Find<ChoiceBar>(ui.Menu, "Sections").Value);

            Find<Button>(ui.Menu, "Section2").onClick.Invoke();
            yield return null;
            AssertSection("SoundSection");

            // 다른 탭에 갔다 와도, 메뉴를 닫았다 열어도 보던 갈래가 남는다.
            ui.Menu.ShowTab(0);
            yield return null;
            ui.Menu.ShowTab(2);
            yield return null;
            Assert.AreEqual(2, settings.CurrentSection);
            AssertSection("SoundSection");

            ui.CloseMenu();
            yield return null;
            ui.OpenMenu();
            ui.Menu.ShowTab(2);
            yield return null;
            AssertSection("SoundSection");
        }

        [UnityTest]
        public IEnumerator 설정마다_제_갈래에_들어_있다()
        {
            yield return OpenSettings();

            Transform screenSection = Find<Transform>(ui.Menu, "ScreenSection");
            Transform controlSection = Find<Transform>(ui.Menu, "ControlSection");
            Transform soundSection = Find<Transform>(ui.Menu, "SoundSection");

            Assert.IsTrue(Find<ChoiceBar>(ui.Menu, "DisplayBar").transform.IsChildOf(screenSection));
            Assert.IsTrue(Find<ChoiceBar>(ui.Menu, "QualityBar").transform.IsChildOf(screenSection));
            Assert.IsTrue(Find<Toggle>(ui.Menu, "VSyncToggle").transform.IsChildOf(screenSection));
            Assert.IsTrue(Find<Toggle>(ui.Menu, "PeopleToggle").transform.IsChildOf(screenSection));
            Assert.IsTrue(Find<Slider>(ui.Menu, "LookSlider").transform.IsChildOf(controlSection));
            Assert.IsTrue(Find<Toggle>(ui.Menu, "InvertLookToggle").transform.IsChildOf(controlSection));
            Assert.IsTrue(Find<Slider>(ui.Menu, "FieldOfViewSlider").transform.IsChildOf(controlSection));
            Assert.IsTrue(Find<Slider>(ui.Menu, "SoundSlider").transform.IsChildOf(soundSection));

            ChoiceBar quality = Find<ChoiceBar>(ui.Menu, "QualityBar");
            Assert.AreEqual(3, quality.Count);
            Assert.AreEqual("낮음", quality.GetLabel(0));
            Assert.AreEqual("보통", quality.GetLabel(1));
            Assert.AreEqual("높음", quality.GetLabel(2));

            ChoiceBar display = Find<ChoiceBar>(ui.Menu, "DisplayBar");
            Assert.AreEqual(2, display.Count);
            Assert.AreEqual("창", display.GetLabel(0));
            Assert.AreEqual("전체 화면", display.GetLabel(1));

            // 키보드·마우스로 할 때는 "PC 전용" 딱지가 보이지 않고 모든 줄을 누를 수 있다.
            foreach (Transform badge in ui.Menu.GetComponentsInChildren<Transform>(true))
            {
                if (badge.name == "PcOnly" && badge.IsChildOf(settings.transform)) Assert.IsFalse(badge.gameObject.activeSelf, "PC인데 PC 전용 딱지가 보입니다.");
            }

            int rows = 0;
            foreach (CanvasGroup group in settings.GetComponentsInChildren<CanvasGroup>(true))
            {
                Assert.AreEqual(1f, group.alpha, 0.001f);
                if (group.name == "PcOnly") continue;

                rows++;
                Assert.IsTrue(group.interactable);
                Assert.IsTrue(group.blocksRaycasts);
            }

            Assert.AreEqual(5, rows, "PC 전용 줄은 화면 방식, 수직 동기화, 마우스 감도, 위아래 시점 반대로, 시야각 다섯입니다.");
        }

        [UnityTest]
        public IEnumerator 화면_방식을_고르면_전체_화면이_되고_창으로_돌아오면_전의_크기다()
        {
            screen.Width = 1280;
            screen.Height = 720;
            yield return OpenSettings();

            ChoiceBar display = Find<ChoiceBar>(ui.Menu, "DisplayBar");
            Assert.AreEqual(0, display.Value, "창으로 시작했으면 '창'이 골라져 있어야 합니다.");
            Assert.AreEqual(0, screen.ApplyCount, "켤 때는 창을 건드리지 않습니다.");

            display.GetButton(1).onClick.Invoke();
            yield return null;
            Assert.IsTrue(screen.Fullscreen);
            Assert.AreEqual(2560, screen.Width, "전체 화면은 모니터의 크기입니다.");
            Assert.AreEqual(1440, screen.Height);
            Assert.AreEqual(1, display.Value);

            // 같은 칸을 다시 눌러도 아무 일도 없다.
            display.GetButton(1).onClick.Invoke();
            yield return null;
            Assert.AreEqual(1, screen.ApplyCount);

            display.GetButton(0).onClick.Invoke();
            yield return null;
            Assert.IsFalse(screen.Fullscreen);
            Assert.AreEqual(1280, screen.Width, "창으로 돌아오면 전의 크기여야 합니다.");
            Assert.AreEqual(720, screen.Height);
            Assert.AreEqual(0, display.Value);
        }

        [UnityTest]
        public IEnumerator 설정_밖에서_화면_방식이_바뀌어도_따라_보인다()
        {
            yield return OpenSettings();
            ChoiceBar display = Find<ChoiceBar>(ui.Menu, "DisplayBar");
            Assert.AreEqual(0, display.Value);

            // 창의 단추나 운영 체제의 키로 전체 화면이 된 경우.
            screen.Fullscreen = true;
            yield return Frames(2);
            Assert.AreEqual(1, display.Value);

            // 전체 화면으로 켜진 채 메뉴를 열어도 맞게 보인다.
            ui.CloseMenu();
            yield return null;
            yield return OpenSettings();
            Assert.AreEqual(1, Find<ChoiceBar>(ui.Menu, "DisplayBar").Value);
        }

        [UnityTest]
        public IEnumerator 설정을_바꾸지_않았으면_보통_품질로_그린다()
        {
            yield return LoadSandbox();

            Assert.IsNotNull(Object.FindAnyObjectByType<GraphicsApplier>(), "게임 화면에 화면 품질을 적용하는 부품이 없습니다.");
            Assert.AreEqual(GameSettings.QualityNormal, GraphicsQuality.Current);
            Assert.AreEqual("PC", GraphicsQuality.CurrentLevelName);
            Assert.AreEqual("PC_RPAsset", QualitySettings.renderPipeline.name, "보통은 19일차까지의 그리기 설정이어야 합니다.");
        }

        [UnityTest]
        public IEnumerator 화면_품질을_고르면_그리기_단계가_바뀌고_저장된다()
        {
            yield return OpenSettings();
            ChoiceBar quality = Find<ChoiceBar>(ui.Menu, "QualityBar");
            Assert.AreEqual(GameSettings.QualityNormal, quality.Value);

            quality.GetButton(GameSettings.QualityLow).onClick.Invoke();
            yield return Frames(2);
            Assert.AreEqual(GameSettings.QualityLow, GameSettings.Quality);
            Assert.AreEqual("PC Low", GraphicsQuality.CurrentLevelName);
            Assert.AreEqual("PC_Low_RPAsset", QualitySettings.renderPipeline.name);
            Assert.AreEqual(GameSettings.QualityLow, quality.Value);

            quality.GetButton(GameSettings.QualityHigh).onClick.Invoke();
            yield return Frames(2);
            Assert.AreEqual("PC High", GraphicsQuality.CurrentLevelName);
            Assert.AreEqual("PC_High_RPAsset", QualitySettings.renderPipeline.name);

            // 품질을 바꾼 뒤에도 화면이 그려진다(그리기 설정이 바뀌는 동안 예외가 나지 않는다).
            ui.CloseMenu();
            yield return Frames(5);

            // 다시 켜면 고른 품질로 시작한다.
            GraphicsQuality.Apply(GameSettings.QualityNormal, GameSettings.VSync);
            Assert.AreEqual("PC", GraphicsQuality.CurrentLevelName);
            yield return OpenSettings();
            Assert.AreEqual("PC High", GraphicsQuality.CurrentLevelName, "저장한 품질로 시작하지 않았습니다.");
            Assert.AreEqual(GameSettings.QualityHigh, Find<ChoiceBar>(ui.Menu, "QualityBar").Value);
        }

        [UnityTest]
        public IEnumerator 수직_동기화를_켜고_끄면_바로_적용되고_품질을_바꿔도_남는다()
        {
            yield return OpenSettings();
            Toggle vSync = Find<Toggle>(ui.Menu, "VSyncToggle");

            // 테스트의 바탕이 끄고 시작한다.
            Assert.IsFalse(vSync.isOn);
            Assert.AreEqual(0, QualitySettings.vSyncCount);

            vSync.isOn = true;
            yield return null;
            Assert.IsTrue(GameSettings.VSync);
            Assert.AreEqual(1, QualitySettings.vSyncCount);

            vSync.isOn = false;
            yield return null;
            Assert.AreEqual(0, QualitySettings.vSyncCount);

            // 품질 단계에는 "켬"으로 적혀 있다. 단계를 바꿔도 끈 것이 남아야 한다.
            Find<ChoiceBar>(ui.Menu, "QualityBar").GetButton(GameSettings.QualityHigh).onClick.Invoke();
            yield return Frames(2);
            Assert.AreEqual("PC High", GraphicsQuality.CurrentLevelName);
            Assert.AreEqual(0, QualitySettings.vSyncCount, "품질을 바꿨더니 수직 동기화가 다시 켜졌습니다.");
        }

        [UnityTest]
        public IEnumerator 위아래_시점_반대로를_켜면_마우스를_위로_밀_때_아래를_본다()
        {
            yield return LoadSandbox();
            player.CaptureLook(true);
            player.SetLook(0f, 10f);
            yield return Frames(2);

            // 보통: 마우스를 위로 밀면 위를 본다(아래쪽이 양수인 각도가 줄어든다).
            float before = player.Rig.Pitch;
            yield return MoveMouse(new Vector2(0f, 60f));
            float normal = player.Rig.Pitch - before;
            Assert.Less(normal, -1f, "마우스를 위로 밀었는데 위를 보지 않았습니다.");

            GameSettings.InvertLookY = true;
            yield return null;
            before = player.Rig.Pitch;
            yield return MoveMouse(new Vector2(0f, 60f));
            float inverted = player.Rig.Pitch - before;
            Assert.Greater(inverted, 1f, "반대로 했는데 아래를 보지 않았습니다.");
            Assert.AreEqual(-normal, inverted, 0.01f, "반대로 해도 도는 양은 같아야 합니다.");

            // 좌우는 바뀌지 않는다.
            float yaw = player.transform.eulerAngles.y;
            yield return MoveMouse(new Vector2(60f, 0f));
            Assert.Greater(Mathf.DeltaAngle(yaw, player.transform.eulerAngles.y), 1f, "위아래만 반대로 해야 하는데 좌우가 바뀌었습니다.");
        }

        [UnityTest]
        public IEnumerator 위아래_시점_반대로는_설정의_칸으로_켜고_끈다()
        {
            yield return OpenSettings();
            Toggle invert = Find<Toggle>(ui.Menu, "InvertLookToggle");
            Assert.IsFalse(invert.isOn);

            invert.isOn = true;
            yield return null;
            Assert.IsTrue(GameSettings.InvertLookY);

            // 다른 곳에서 바뀐 값도 따라 보인다.
            GameSettings.InvertLookY = false;
            yield return null;
            Assert.IsFalse(invert.isOn);
        }

        [UnityTest]
        public IEnumerator 모두_기본값으로를_누르면_새_설정도_처음_값이_되고_창으로_돌아온다()
        {
            yield return OpenSettings();

            Find<ChoiceBar>(ui.Menu, "QualityBar").GetButton(GameSettings.QualityLow).onClick.Invoke();
            Find<ChoiceBar>(ui.Menu, "DisplayBar").GetButton(1).onClick.Invoke();
            Find<Toggle>(ui.Menu, "InvertLookToggle").isOn = true;
            yield return Frames(2);
            Assert.IsTrue(screen.Fullscreen);
            Assert.AreEqual("PC Low", GraphicsQuality.CurrentLevelName);
            Assert.AreEqual(0, QualitySettings.vSyncCount);

            Find<Button>(ui.Menu, "Reset").onClick.Invoke();
            yield return Frames(2);

            Assert.AreEqual(GameSettings.QualityNormal, GameSettings.Quality);
            Assert.AreEqual("PC", GraphicsQuality.CurrentLevelName);
            Assert.IsTrue(GameSettings.VSync, "수직 동기화의 처음 값은 켬입니다.");
            Assert.AreEqual(1, QualitySettings.vSyncCount);
            Assert.IsFalse(GameSettings.InvertLookY);
            Assert.IsFalse(screen.Fullscreen, "전체 화면이면 창으로 돌아와야 합니다.");
            Assert.AreEqual(ScreenControl.DefaultWindowWidth, screen.Width);
            Assert.AreEqual(ScreenControl.DefaultWindowHeight, screen.Height);

            Assert.AreEqual(GameSettings.QualityNormal, Find<ChoiceBar>(ui.Menu, "QualityBar").Value);
            Assert.AreEqual(0, Find<ChoiceBar>(ui.Menu, "DisplayBar").Value);
            Assert.IsTrue(Find<Toggle>(ui.Menu, "VSyncToggle").isOn);
            Assert.IsFalse(Find<Toggle>(ui.Menu, "InvertLookToggle").isOn);
        }

        private IEnumerator OpenSettings()
        {
            yield return LoadSandbox();
            ui.OpenMenu();
            ui.Menu.ShowTab(2);
            yield return null;
            settings = Find<SettingsPanel>(ui.Menu, "SettingsPage");
        }

        private IEnumerator MoveMouse(Vector2 delta)
        {
            Set(mouse.delta, delta);
            yield return null;
            yield return null;
            Set(mouse.delta, Vector2.zero);
            yield return null;
        }

        private void AssertSection(string shown)
        {
            foreach (string name in new[] { "ScreenSection", "ControlSection", "SoundSection" })
            {
                Assert.AreEqual(name == shown, Find<Transform>(ui.Menu, name).gameObject.activeInHierarchy, $"{name}이 보이는지가 맞지 않습니다.");
            }
        }
    }
}
