using AtelierVerse.Core;
using NUnit.Framework;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 빌드 확인용 명령줄 인자(-quitAfter, -screenshotOut, -screenCheck)를 읽는 규칙 검사.
    /// </summary>
    public class BootstrapArgsTests
    {
        [Test]
        public void quitAfter_뒤의_초를_읽는다()
        {
            Assert.AreEqual(10f, Bootstrap.ParseSeconds(new[] { "game.exe", "-quitAfter", "10" }, Bootstrap.QuitAfterArgument));
            Assert.AreEqual(2.5f, Bootstrap.ParseSeconds(new[] { "-quitAfter", "2.5", "-other" }, Bootstrap.QuitAfterArgument));
        }

        [Test]
        public void 없거나_숫자가_아니거나_0_이하이면_0이다()
        {
            Assert.AreEqual(0f, Bootstrap.ParseSeconds(new[] { "game.exe" }, Bootstrap.QuitAfterArgument));
            Assert.AreEqual(0f, Bootstrap.ParseSeconds(new[] { "-quitAfter" }, Bootstrap.QuitAfterArgument));
            Assert.AreEqual(0f, Bootstrap.ParseSeconds(new[] { "-quitAfter", "soon" }, Bootstrap.QuitAfterArgument));
            Assert.AreEqual(0f, Bootstrap.ParseSeconds(new[] { "-quitAfter", "-3" }, Bootstrap.QuitAfterArgument));
            Assert.AreEqual(0f, Bootstrap.ParseSeconds(null, Bootstrap.QuitAfterArgument));
        }

        [Test]
        public void screenshotOut_뒤의_경로를_읽는다()
        {
            Assert.AreEqual(@"C:\out\shot.png", Bootstrap.ReadArgument(new[] { "-screenshotOut", @"C:\out\shot.png" }, Bootstrap.ScreenshotArgument));
            Assert.IsNull(Bootstrap.ReadArgument(new[] { "-screenshotOut" }, Bootstrap.ScreenshotArgument));
        }

        [Test]
        public void 값이_없는_인자는_있는지만_본다()
        {
            Assert.IsTrue(Bootstrap.HasFlag(new[] { "game.exe", "-quitAfter", "8", "-screenCheck" }, Bootstrap.ScreenCheckArgument));
            Assert.IsFalse(Bootstrap.HasFlag(new[] { "game.exe", "-quitAfter", "8" }, Bootstrap.ScreenCheckArgument));
            Assert.IsFalse(Bootstrap.HasFlag(new[] { "game.exe", "-screencheck" }, Bootstrap.ScreenCheckArgument), "대소문자가 다르면 다른 인자입니다.");
            Assert.IsFalse(Bootstrap.HasFlag(null, Bootstrap.ScreenCheckArgument));
        }
    }
}
