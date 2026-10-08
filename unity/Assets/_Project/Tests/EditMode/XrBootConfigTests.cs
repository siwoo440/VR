using System.IO;
using AtelierVerse.EditorTools;
using NUnit.Framework;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 빌드가 끝난 뒤 시작 설정을 다듬는 일의 검사. 평소 실행이 VR 프로그램을 찾지 않게 XR 사전 초기화를 빼고, VR로 시작하는 파일을 둔다.
    /// </summary>
    public class XrBootConfigTests
    {
        private static readonly string PreInitLine = XrBootConfig.PreInitKey + "=UnityOpenXR";

        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "AtelierVerseBootConfig-" + Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(directory, "Game_Data"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void 사전_초기화_줄만_빼고_나머지는_그대로_둔다()
        {
            string[] lines = { "gfx-enable-gfx-jobs=1", PreInitLine, "xr-meta-enabled=0", "build-guid=abc" };

            string[] kept = XrBootConfig.WithoutPreInit(lines);

            CollectionAssert.AreEqual(new[] { "gfx-enable-gfx-jobs=1", "xr-meta-enabled=0", "build-guid=abc" }, kept);
            Assert.IsFalse(XrBootConfig.IsPreInit("xr-meta-enabled=0"), "XR의 다른 설정은 건드리지 않아야 합니다.");
            Assert.IsFalse(XrBootConfig.IsPreInit(null));
        }

        [Test]
        public void 시작_설정에서_사전_초기화를_빼고_VR로_시작하는_파일을_만든다()
        {
            string executable = Path.Combine(directory, "Game.exe");
            string bootConfig = XrBootConfig.BootConfigPath(executable);
            File.WriteAllText(bootConfig, "gfx-enable-gfx-jobs=1\n" + PreInitLine + "\nbuild-guid=abc\n");

            Assert.IsTrue(XrBootConfig.Apply(executable), "사전 초기화 줄을 뺐다고 알려야 합니다.");

            Assert.AreEqual("gfx-enable-gfx-jobs=1\nbuild-guid=abc\n", File.ReadAllText(bootConfig));

            string launcher = Path.Combine(directory, "Game-VR.bat");
            Assert.AreEqual(launcher, XrBootConfig.VrLauncherPath(executable));
            Assert.IsTrue(File.Exists(launcher), "VR로 시작하는 파일이 없습니다.");
            StringAssert.Contains("\"%~dp0Game.exe\" -vr", File.ReadAllText(launcher));
            StringAssert.DoesNotContain(XrBootConfig.PreInitKey, File.ReadAllText(launcher), "백신이 막는 인자를 실행 파일에 넘기면 안 됩니다.");
        }

        [Test]
        public void 두_번_적용해도_결과가_같다()
        {
            string executable = Path.Combine(directory, "Game.exe");
            string bootConfig = XrBootConfig.BootConfigPath(executable);
            File.WriteAllText(bootConfig, "gfx-enable-gfx-jobs=1\r\n" + PreInitLine + "\r\nbuild-guid=abc\r\n");

            XrBootConfig.Apply(executable);
            string first = File.ReadAllText(bootConfig);

            Assert.IsFalse(XrBootConfig.Apply(executable), "이미 뺀 뒤에는 뺄 것이 없어야 합니다.");
            Assert.AreEqual(first, File.ReadAllText(bootConfig));
            Assert.AreEqual("gfx-enable-gfx-jobs=1\r\nbuild-guid=abc\r\n", first, "줄바꿈 방식은 원래대로 두어야 합니다.");
        }

        [Test]
        public void 시작_설정이_없어도_VR로_시작하는_파일은_만든다()
        {
            string executable = Path.Combine(directory, "Other.exe");

            Assert.IsFalse(XrBootConfig.Apply(executable));
            Assert.IsTrue(File.Exists(Path.Combine(directory, "Other-VR.bat")));
        }
    }
}
