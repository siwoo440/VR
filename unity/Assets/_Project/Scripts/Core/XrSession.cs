using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace AtelierVerse.Core
{
    /// <summary>
    /// VR 화면(OpenXR)을 켜고 끄는 곳. 평소에는 켜지 않고, 실행할 때 -vr을 주었을 때만 첫 씬이 열리기 전에 켠다.
    /// 그래서 VR 프로그램이 깔린 PC에서도 그냥 실행하면 키보드·마우스로 시작하고, 헤드셋 프로그램이 저절로 뜨지 않는다.
    /// 켜지 못하면(헤드셋이나 VR 프로그램이 없으면) 키보드·마우스로 시작하고 까닭을 남긴다.
    /// 에디터에서는 메뉴 "Atelier Verse/재생할 때 VR 켜기"로 같은 일을 한다. 창 없이 돌리는 명령줄 실행(자동 테스트)은 이 메뉴 설정을 보지 않는다.
    /// </summary>
    public static class XrSession
    {
        public const string VrArgument = "-vr";
        public const string EditorStartKey = "AtelierVerse.Editor.StartVr";

        /// <summary>추적 기준이 바닥이 아닐 때 추적 공간을 올려 두는 선 키의 눈높이.</summary>
        public const float StandingEyeHeight = 1.55f;

        public const string StartedMessage = "VR 기기로 시작했습니다 · 왼손의 메뉴 단추로 메뉴를 엽니다";
        public const string NoSettingsMessage = "VR 설정이 없어 키보드·마우스로 시작합니다";
        public const string NoDeviceMessage = "VR 기기를 찾지 못해 키보드·마우스로 시작합니다";

        /// <summary>VR 화면이 켜져 있는지.</summary>
        public static bool Started { get; private set; }

        /// <summary>추적 기준이 바닥인지. 바닥이면 기기가 주는 머리 높이가 바닥에서 잰 값이다.</summary>
        public static bool FloorOrigin { get; private set; }

        /// <summary>추적 공간을 발밑에서 얼마나 올려 둘지. 추적 기준이 바닥이면 0이다.</summary>
        public static float OriginHeight => Started && !FloorOrigin ? StandingEyeHeight : 0f;

        /// <summary>시작할 때 생긴 안내 문구. 화면이 준비된 뒤 TakeNotice로 한 번 가져가 알린다.</summary>
        private static string pendingNotice;
        private static NoticeKind pendingKind;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartIfRequested()
        {
            if (!IsRequested(Environment.GetCommandLineArgs(), EditorWantsVr())) return;
            TryStart();
        }

        /// <summary>VR로 시작하라는 요청이 있는지. 명령줄의 -vr 또는 에디터의 메뉴 설정이다.</summary>
        public static bool IsRequested(string[] arguments, bool editorFlag = false)
        {
            if (editorFlag) return true;
            if (arguments == null) return false;

            foreach (string argument in arguments)
            {
                if (string.Equals(argument, VrArgument, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        /// <summary>
        /// VR 화면을 켠다. 켜지 못하면 false이고 까닭을 안내 문구로 남긴다. 이미 켜져 있으면 true다.
        /// </summary>
        public static bool TryStart()
        {
            if (Started) return true;

            XRGeneralSettings settings = XRGeneralSettings.Instance;
            XRManagerSettings manager = settings != null ? settings.Manager : null;
            if (manager == null)
            {
                Fail(NoSettingsMessage);
                return false;
            }

            if (manager.activeLoader == null) manager.InitializeLoaderSync();
            if (manager.activeLoader == null)
            {
                Fail(NoDeviceMessage);
                return false;
            }

            manager.StartSubsystems();
            FloorOrigin = TrySetFloorOrigin();
            Started = true;
            Application.quitting += Stop;

            pendingNotice = StartedMessage;
            pendingKind = NoticeKind.Info;
            Debug.Log($"[Atelier Verse] VR 화면을 켰습니다({manager.activeLoader.name}, 추적 기준 {(FloorOrigin ? "바닥" : "기기")}).");
            return true;
        }

        /// <summary>VR 화면을 끈다. 앱을 끝낼 때 부른다.</summary>
        public static void Stop()
        {
            if (!Started) return;

            Started = false;
            FloorOrigin = false;
            Application.quitting -= Stop;

            XRGeneralSettings settings = XRGeneralSettings.Instance;
            XRManagerSettings manager = settings != null ? settings.Manager : null;
            if (manager == null || manager.activeLoader == null) return;

            manager.StopSubsystems();
            manager.DeinitializeLoader();
        }

        /// <summary>시작할 때 남긴 안내 문구를 한 번 가져간다. 없으면 false다.</summary>
        public static bool TakeNotice(out string message, out NoticeKind kind)
        {
            message = pendingNotice;
            kind = pendingKind;
            pendingNotice = null;
            return !string.IsNullOrEmpty(message);
        }

        private static void Fail(string message)
        {
            pendingNotice = message;
            pendingKind = NoticeKind.Warning;
            Debug.LogWarning($"[Atelier Verse] {message}");
        }

        /// <summary>추적 기준을 바닥으로 맞춘다. 기기가 바닥 기준을 지원하지 않으면 false다.</summary>
        private static bool TrySetFloorOrigin()
        {
            var subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);

            bool floor = false;
            foreach (XRInputSubsystem subsystem in subsystems)
            {
                if (subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor)) floor = true;
            }

            return floor;
        }

        private static bool EditorWantsVr()
        {
#if UNITY_EDITOR
            return !Application.isBatchMode && UnityEditor.EditorPrefs.GetBool(EditorStartKey, false);
#else
            return false;
#endif
        }
    }
}
