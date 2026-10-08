using System;
using System.IO;
using System.Linq;
using System.Text;
using AtelierVerse.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// Windows 빌드가 끝나면 실행 파일을 "키보드·마우스가 기본"이 되게 다듬는다. 에디터의 빌드 창으로 만들든 명령줄로 만들든 똑같이 적용된다.
    /// XR Plugin Management는 빌드의 시작 설정(boot.config)에 XR 사전 초기화 줄을 넣는다. 이 줄이 있으면 -vr 없이 켜도
    /// 화면이 뜨기 전에 VR 프로그램(OpenXR 런타임)을 찾아가서, VR 프로그램이 깔린 PC에서는 평소 실행에도 그 프로그램이 깨어날 수 있다.
    /// 그래서 그 줄을 빼고, VR 화면으로 시작하는 배치 파일(-vr을 붙여 실행)을 실행 파일 옆에 둔다.
    /// 사전 초기화를 실행 인자로 되살리는 방법은 쓰지 않는다. Unity의 보안 문제(CVE-2025-59489)에 쓰이던 인자라 백신이 실행을 막는다.
    /// </summary>
    public class XrBootConfig : IPostprocessBuildWithReport
    {
        public const string PreInitKey = "xrsdk-pre-init-library";
        public const string BootConfigName = "boot.config";
        public const string VrLauncherSuffix = "-VR.bat";

        public int callbackOrder => 1000;

        public void OnPostprocessBuild(BuildReport report)
        {
            BuildTarget target = report.summary.platform;
            if (target != BuildTarget.StandaloneWindows64 && target != BuildTarget.StandaloneWindows) return;

            string executable = report.summary.outputPath;
            if (string.IsNullOrEmpty(executable) || !executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[Atelier Verse] 실행 파일 경로를 알 수 없어 시작 설정을 다듬지 못했습니다: {executable}");
                return;
            }

            bool removed = Apply(executable);
            Debug.Log($"[Atelier Verse] 평소 실행은 VR 프로그램을 찾지 않게 함: 시작 설정의 XR 사전 초기화 {(removed ? "뺌" : "없음")}, VR로 시작하는 파일 {Path.GetFileName(VrLauncherPath(executable))}");
        }

        /// <summary>실행 파일 옆의 시작 설정에서 XR 사전 초기화를 빼고, VR로 시작하는 배치 파일을 쓴다. 사전 초기화 줄을 뺐으면 true.</summary>
        public static bool Apply(string executablePath)
        {
            bool removed = false;
            string bootConfig = BootConfigPath(executablePath);

            if (File.Exists(bootConfig))
            {
                string text = File.ReadAllText(bootConfig);
                string newline = text.Contains("\r\n") ? "\r\n" : "\n";
                string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                string[] kept = WithoutPreInit(lines);

                removed = kept.Length != lines.Length;
                if (removed) File.WriteAllText(bootConfig, string.Join(newline, kept) + newline, new UTF8Encoding(false));
            }

            File.WriteAllText(VrLauncherPath(executablePath), VrLauncherText(Path.GetFileName(executablePath)), new UTF8Encoding(false));
            return removed;
        }

        /// <summary>XR 사전 초기화 줄만 뺀 나머지 줄.</summary>
        public static string[] WithoutPreInit(string[] lines)
        {
            return lines.Where(line => !IsPreInit(line)).ToArray();
        }

        public static bool IsPreInit(string line)
        {
            return line != null && line.TrimStart().StartsWith(PreInitKey + "=", StringComparison.Ordinal);
        }

        /// <summary>실행 파일의 시작 설정 경로. Unity는 "실행 파일 이름_Data" 폴더에 둔다.</summary>
        public static string BootConfigPath(string executablePath)
        {
            string directory = Path.GetDirectoryName(executablePath) ?? string.Empty;
            return Path.Combine(directory, Path.GetFileNameWithoutExtension(executablePath) + "_Data", BootConfigName);
        }

        /// <summary>VR로 시작하는 배치 파일의 경로. 실행 파일 옆에 "이름-VR.bat"으로 둔다.</summary>
        public static string VrLauncherPath(string executablePath)
        {
            string directory = Path.GetDirectoryName(executablePath) ?? string.Empty;
            return Path.Combine(directory, Path.GetFileNameWithoutExtension(executablePath) + VrLauncherSuffix);
        }

        /// <summary>배치 파일의 내용. 같은 폴더의 실행 파일에 -vr을 붙여 켠다. 글자가 깨지지 않게 영문만 쓴다.</summary>
        public static string VrLauncherText(string executableName)
        {
            return "@echo off\r\n"
                + "rem Starts the game with the VR screen. Without a headset it falls back to keyboard and mouse.\r\n"
                + $"start \"\" \"%~dp0{executableName}\" {XrSession.VrArgument} %*\r\n";
        }
    }
}
