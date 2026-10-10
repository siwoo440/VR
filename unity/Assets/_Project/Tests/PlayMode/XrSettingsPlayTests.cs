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
    /// VR에서의 설정을 가상 기기로 확인한다(20일차): 키보드·마우스에만 해당하는 줄은 흐리고 눌리지 않으며,
    /// 화면 품질과 갈래의 칸은 오른손 광선으로 누른다. 헤드셋에서 칸이 누르기 알맞은 크기인지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class XrSettingsPlayTests : XrPlayTestBase
    {
        [UnityTest]
        public IEnumerator VR에서는_PC_전용_줄이_흐리고_눌리지_않으며_화면_품질은_바꿀_수_있다()
        {
            yield return LoadVr();
            yield return OpenMenuWithController();
            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Menu, "Tab2")));
            yield return PullTrigger();
            Assert.AreEqual(2, ui.Menu.CurrentTab);

            // 화면 방식: 창과 전체 화면은 PC의 창에만 해당한다.
            ChoiceBar display = Find<ChoiceBar>(ui.Menu, "DisplayBar");
            AssertDesktopOnly(display.transform.parent);
            yield return PointRightHandAt(CenterOf(display.GetButton(1)));
            yield return PullTrigger();
            Assert.IsFalse(screen.Fullscreen, "VR에서 누를 수 없는 줄이 눌렸습니다.");
            Assert.AreEqual(0, screen.ApplyCount);

            // 수직 동기화도 PC 전용이다.
            Toggle vSync = Find<Toggle>(ui.Menu, "VSyncToggle");
            AssertDesktopOnly(vSync.transform.parent);
            bool vSyncBefore = vSync.isOn;
            yield return PointRightHandAt(CenterOf(vSync));
            yield return PullTrigger();
            Assert.AreEqual(vSyncBefore, vSync.isOn);

            // 화면 품질은 VR에서도 바꾼다.
            ChoiceBar quality = Find<ChoiceBar>(ui.Menu, "QualityBar");
            Assert.IsNull(quality.transform.parent.GetComponent<CanvasGroup>(), "화면 품질은 VR에서도 쓰는 줄입니다.");
            yield return PointRightHandAt(CenterOf(quality.GetButton(GameSettings.QualityLow)));
            yield return PullTrigger();
            Assert.AreEqual(GameSettings.QualityLow, GameSettings.Quality, "화면 품질의 칸을 가리켜 눌렀는데 바뀌지 않았습니다.");
            Assert.AreEqual("PC Low", GraphicsQuality.CurrentLevelName);
            Assert.AreEqual(GameSettings.QualityLow, quality.Value);

            // 조작 갈래의 줄은 모두 PC 전용이다.
            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Menu, "Section1")));
            yield return PullTrigger();
            Assert.IsTrue(Find<Transform>(ui.Menu, "ControlSection").gameObject.activeInHierarchy);

            Toggle invert = Find<Toggle>(ui.Menu, "InvertLookToggle");
            AssertDesktopOnly(invert.transform.parent);
            yield return PointRightHandAt(CenterOf(invert));
            yield return PullTrigger();
            Assert.IsFalse(GameSettings.InvertLookY);

            Slider look = Find<Slider>(ui.Menu, "LookSlider");
            AssertDesktopOnly(look.transform.parent);
            float lookBefore = look.value;
            var rect = (RectTransform)look.transform;
            yield return PointRightHandAt(rect.TransformPoint(new Vector3(Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, 0.9f), rect.rect.center.y, 0f)));
            yield return PullTrigger();
            Assert.AreEqual(lookBefore, look.value, 0.0001f, "VR에서 누를 수 없는 막대가 움직였습니다.");

            AssertDesktopOnly(Find<Slider>(ui.Menu, "FieldOfViewSlider").transform.parent);
        }

        /// <summary>이 줄이 VR에서 흐리게 보이고 눌리지 않으며 "PC 전용" 딱지가 보이는지 확인한다.</summary>
        private static void AssertDesktopOnly(Transform row)
        {
            CanvasGroup group = row.GetComponent<CanvasGroup>();
            Assert.IsNotNull(group, $"{row.parent.name}/{row.name} 줄이 PC 전용으로 묶여 있지 않습니다.");
            Assert.IsFalse(group.interactable);
            Assert.IsFalse(group.blocksRaycasts);
            Assert.Less(group.alpha, 0.9f);

            Transform badge = row.Find("PcOnly");
            Assert.IsNotNull(badge, "PC 전용 딱지가 없습니다.");
            Assert.IsTrue(badge.gameObject.activeSelf, "VR인데 PC 전용 딱지가 보이지 않습니다.");
        }
    }
}
