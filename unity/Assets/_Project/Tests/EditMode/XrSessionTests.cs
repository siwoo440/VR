using System.Linq;
using AtelierVerse.Core;
using AtelierVerse.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// VR 화면을 켜는 조건과 XR 설정의 검사. 평소에는 VR이 저절로 켜지지 않아야 한다.
    /// </summary>
    public class XrSessionTests
    {
        [Test]
        public void 실행할_때_vr을_주었을_때만_VR로_시작한다()
        {
            Assert.IsTrue(XrSession.IsRequested(new[] { "AtelierVerse.exe", "-vr" }));
            Assert.IsTrue(XrSession.IsRequested(new[] { "-quitAfter", "5", "-VR" }), "대소문자를 가리지 않습니다.");
            Assert.IsFalse(XrSession.IsRequested(new[] { "AtelierVerse.exe" }));
            Assert.IsFalse(XrSession.IsRequested(new[] { "-vrmode", "-novr" }), "비슷한 다른 인자로는 켜지지 않아야 합니다.");
            Assert.IsFalse(XrSession.IsRequested(null));
        }

        [Test]
        public void 에디터의_설정이_켜져_있으면_VR로_시작한다()
        {
            Assert.IsTrue(XrSession.IsRequested(new[] { "Unity.exe" }, true));
            Assert.IsFalse(XrSession.IsRequested(new[] { "Unity.exe" }, false));
        }

        [Test]
        public void 평소에는_VR_화면이_꺼져_있고_추적_공간을_올리지_않는다()
        {
            Assert.IsFalse(XrSession.Started);
            Assert.AreEqual(0f, XrSession.OriginHeight);
        }

        [Test]
        public void PC의_XR_설정은_OpenXR을_쓰되_시작할_때_저절로_켜지_않는다()
        {
            XRGeneralSettings settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);

            Assert.IsNotNull(settings, "PC용 XR 설정이 없습니다. 11일차 셋업을 실행하세요.");
            Assert.IsFalse(settings.InitManagerOnStart, "시작할 때 XR을 저절로 켜면 VR 프로그램이 있는 PC에서 헤드셋 프로그램이 뜹니다.");
            Assert.IsNotNull(settings.Manager);
            Assert.IsTrue(settings.Manager.activeLoaders.Any(loader => loader != null && loader.GetType().FullName == Day11Setup.OpenXrLoaderType), "XR 설정에 OpenXR 로더가 없습니다.");
        }

        [Test]
        public void 자주_쓰는_컨트롤러의_프로파일이_켜져_있다()
        {
            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);

            Assert.IsNotNull(settings);
            Assert.IsTrue(settings.GetFeature<OculusTouchControllerProfile>().enabled, "Quest 컨트롤러 프로파일이 꺼져 있습니다.");
            Assert.IsTrue(settings.GetFeature<ValveIndexControllerProfile>().enabled);
            Assert.IsTrue(settings.GetFeature<HTCViveControllerProfile>().enabled);
        }
    }
}
