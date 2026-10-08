using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// Windows용 실행 파일을 만든다. 에디터 메뉴와 명령줄 양쪽에서 쓴다.
    /// 명령줄 예: Unity.exe -batchmode -quit -projectPath unity -executeMethod AtelierVerse.EditorTools.BuildPlayer.BuildWindows -buildOut Builds/Windows -logFile build.log
    /// -buildOut이 없으면 unity/Builds/Windows에 만들고, -development를 주면 개발용 빌드가 된다.
    /// 실패하면 예외를 던져 Unity가 0이 아닌 값으로 끝나게 한다.
    /// </summary>
    public static class BuildPlayer
    {
        public const string DefaultOutputDirectory = "Builds/Windows";
        public const string ExecutableName = "AtelierVerse.exe";
        public const string OutputArgument = "-buildOut";
        public const string DevelopmentArgument = "-development";

        [MenuItem("Atelier Verse/Windows 빌드 만들기")]
        public static void BuildWindowsFromMenu()
        {
            string directory = ResolveOutputDirectory(Array.Empty<string>(), Directory.GetCurrentDirectory());
            BuildReport report = Build(directory, false);
            EditorUtility.DisplayDialog("Atelier Verse", $"Windows 빌드 {Describe(report)}\n\n{ExecutablePath(directory)}", "확인");
        }

        /// <summary>명령줄 진입점. 인자를 읽어 빌드하고, 실패하면 예외를 던진다.</summary>
        public static void BuildWindows()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            string directory = ResolveOutputDirectory(arguments, Directory.GetCurrentDirectory());
            bool development = Array.IndexOf(arguments, DevelopmentArgument) >= 0;

            BuildReport report = Build(directory, development);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows 빌드가 끝나지 않았습니다: {report.summary.result}, 오류 {report.summary.totalErrors}개");
            }
        }

        /// <summary>빌드 설정에 켜진 씬을 모두 넣어 Windows 64비트 실행 파일을 만든다. 스크립팅 백엔드는 Mono로 둔다(IL2CPP는 C++ 도구가 필요).</summary>
        public static BuildReport Build(string outputDirectory, bool development)
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Directory.CreateDirectory(outputDirectory);

            var options = new BuildPlayerOptions
            {
                scenes = EnabledScenes(),
                locationPathName = ExecutablePath(outputDirectory),
                target = BuildTarget.StandaloneWindows64,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[Atelier Verse] Windows 빌드 {Describe(report)} → {options.locationPathName}");
            return report;
        }

        /// <summary>빌드 설정에서 켜진 씬의 경로. 첫 씬이 처음 열리는 씬이다.</summary>
        public static string[] EnabledScenes()
        {
            return EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        }

        /// <summary>-buildOut 인자를 읽어 출력 폴더를 정한다. 상대 경로는 프로젝트 폴더 기준이다.</summary>
        public static string ResolveOutputDirectory(string[] arguments, string projectRoot)
        {
            string value = ReadArgument(arguments, OutputArgument);
            string directory = string.IsNullOrEmpty(value) ? DefaultOutputDirectory : value;
            return Path.IsPathRooted(directory) ? Path.GetFullPath(directory) : Path.GetFullPath(Path.Combine(projectRoot, directory));
        }

        public static string ExecutablePath(string outputDirectory)
        {
            return Path.Combine(outputDirectory, ExecutableName);
        }

        public static string ReadArgument(string[] arguments, string name)
        {
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == name) return arguments[i + 1];
            }

            return null;
        }

        private static string Describe(BuildReport report)
        {
            BuildSummary summary = report.summary;
            return $"{summary.result}: {summary.totalSize / (1024 * 1024)}MB, {summary.totalTime.TotalSeconds:F0}초, 오류 {summary.totalErrors}개, 경고 {summary.totalWarnings}개";
        }
    }
}
