using System.IO;
using AtelierVerse.EditorTools;
using NUnit.Framework;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// Windows 빌드 스크립트의 인자 처리와 씬 목록 검사. 실제 빌드는 명령줄에서 따로 한다.
    /// </summary>
    public class BuildPlayerTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "atelier-verse-project"));

        [Test]
        public void 출력_폴더를_주지_않으면_프로젝트_아래_Builds_Windows다()
        {
            string directory = BuildPlayer.ResolveOutputDirectory(new string[0], ProjectRoot);

            Assert.AreEqual(Path.GetFullPath(Path.Combine(ProjectRoot, "Builds", "Windows")), directory);
            StringAssert.EndsWith("AtelierVerse.exe", BuildPlayer.ExecutablePath(directory));
        }

        [Test]
        public void 상대_출력_폴더는_프로젝트_기준이고_절대_경로는_그대로_쓴다()
        {
            string relative = BuildPlayer.ResolveOutputDirectory(new[] { "-batchmode", "-buildOut", "out/win" }, ProjectRoot);
            Assert.AreEqual(Path.GetFullPath(Path.Combine(ProjectRoot, "out", "win")), relative);

            string absolute = Path.Combine(Path.GetTempPath(), "atelier-verse-build");
            Assert.AreEqual(Path.GetFullPath(absolute), BuildPlayer.ResolveOutputDirectory(new[] { "-buildOut", absolute }, ProjectRoot));
        }

        [Test]
        public void 값이_없는_인자는_무시한다()
        {
            Assert.IsNull(BuildPlayer.ReadArgument(new[] { "-buildOut" }, "-buildOut"));
            Assert.IsNull(BuildPlayer.ReadArgument(new[] { "-other", "x" }, "-buildOut"));
        }

        [Test]
        public void 빌드할_씬은_Boot로_시작하고_Sandbox를_포함한다()
        {
            string[] scenes = BuildPlayer.EnabledScenes();

            Assert.GreaterOrEqual(scenes.Length, 2);
            StringAssert.EndsWith("Boot.unity", scenes[0]);
            Assert.IsTrue(System.Array.Exists(scenes, scene => scene.EndsWith("Sandbox.unity")), "Sandbox 씬이 빌드 목록에 없습니다.");
        }
    }
}
