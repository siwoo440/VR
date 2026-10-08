using AtelierVerse.Player;
using NUnit.Framework;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 시작할 때의 조작 방식을 고르는 규칙 검사. VR 화면이 켜져 있지 않으면 키보드·마우스다.
    /// </summary>
    public class ControlModeTests
    {
        [TearDown]
        public void Reset()
        {
            PlayerModeSwitch.Forced = null;
        }

        [Test]
        public void VR_화면이_켜져_있지_않으면_키보드와_마우스로_시작한다()
        {
            PlayerModeSwitch.Forced = null;

            Assert.AreEqual(ControlMode.Desktop, PlayerModeSwitch.Decide());
        }

        [Test]
        public void 강제한_값이_있으면_그_방식으로_시작한다()
        {
            PlayerModeSwitch.Forced = ControlMode.Vr;
            Assert.AreEqual(ControlMode.Vr, PlayerModeSwitch.Decide());

            PlayerModeSwitch.Forced = ControlMode.Desktop;
            Assert.AreEqual(ControlMode.Desktop, PlayerModeSwitch.Decide());
        }
    }
}
