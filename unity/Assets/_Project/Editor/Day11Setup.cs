using System;
using System.IO;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 11일차 구성을 한 번에 적용한다: PC(Standalone)의 XR 설정에 OpenXR을 넣되 시작할 때 저절로 켜지지 않게 하고,
    /// 자주 쓰는 VR 컨트롤러의 프로파일을 켠다. VR 화면은 실행할 때 -vr을 주었을 때만 XrSession이 켠다.
    /// 여러 번 실행해도 결과가 같도록 작성했다. XR Plugin Management와 OpenXR Plugin 패키지가 있어야 한다.
    /// 메뉴: Atelier Verse/11일차 셋업 실행
    /// </summary>
    public static class Day11Setup
    {
        public const string OpenXrLoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";

        private const string SettingsDirectory = "Assets/XR";
        private const string SettingsPath = SettingsDirectory + "/XRGeneralSettingsPerBuildTarget.asset";
        private const BuildTargetGroup Target = BuildTargetGroup.Standalone;

        // 컨트롤러마다 단추의 이름이 달라 프로파일을 켜 두어야 그 컨트롤러의 입력이 들어온다.
        private static readonly Type[] ControllerProfiles =
        {
            typeof(OculusTouchControllerProfile),
            typeof(MetaQuestTouchPlusControllerProfile),
            typeof(MetaQuestTouchProControllerProfile),
            typeof(ValveIndexControllerProfile),
            typeof(HTCViveControllerProfile),
            typeof(MicrosoftMotionControllerProfile),
        };

        [MenuItem("Atelier Verse/11일차 셋업 실행")]
        public static void Run()
        {
            Apply();
            EditorUtility.DisplayDialog("Atelier Verse",
                "11일차 셋업 완료\n\n- PC의 XR 설정에 OpenXR 추가(시작할 때 저절로 켜지 않음)\n- VR 컨트롤러 프로파일 켜기\n- 실행할 때 -vr을 주면 VR 화면으로 시작",
                "확인");
        }

        /// <summary>대화상자 없이 적용한다. 자동 셋업과 명령줄 실행이 이 메서드를 부른다.</summary>
        public static void Apply()
        {
            XRGeneralSettings general = EnsureGeneralSettings();
            general.InitManagerOnStart = false;
            EditorUtility.SetDirty(general);

            if (!XRPackageMetadataStore.AssignLoader(general.Manager, OpenXrLoaderType, Target))
            {
                throw new InvalidOperationException("XR 설정에 OpenXR 로더를 넣지 못했습니다. OpenXR Plugin 패키지가 설치되어 있는지 확인하세요.");
            }

            EnableControllerProfiles();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>빌드 대상별 XR 설정 자산을 찾거나 만들고, PC용 설정과 로더 목록을 준비한다.</summary>
        private static XRGeneralSettings EnsureGeneralSettings()
        {
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget);
            if (perTarget == null) perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(SettingsPath);

            if (perTarget == null)
            {
                if (!Directory.Exists(SettingsDirectory)) Directory.CreateDirectory(SettingsDirectory);
                AssetDatabase.Refresh();

                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, SettingsPath);
                AssetDatabase.SaveAssets();
            }

            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);

            if (!perTarget.HasSettingsForBuildTarget(Target)) perTarget.CreateDefaultSettingsForBuildTarget(Target);
            if (!perTarget.HasManagerSettingsForBuildTarget(Target)) perTarget.CreateDefaultManagerSettingsForBuildTarget(Target);

            EditorUtility.SetDirty(perTarget);
            return perTarget.SettingsForBuildTarget(Target);
        }

        private static void EnableControllerProfiles()
        {
            FeatureHelpers.RefreshFeatures(Target);

            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(Target);
            if (settings == null) throw new InvalidOperationException("OpenXR 설정을 찾지 못했습니다.");

            foreach (Type profile in ControllerProfiles)
            {
                OpenXRFeature feature = settings.GetFeature(profile);
                if (feature == null)
                {
                    Debug.LogWarning($"[Atelier Verse] OpenXR 컨트롤러 프로파일을 찾지 못했습니다: {profile.Name}");
                    continue;
                }

                feature.enabled = true;
                EditorUtility.SetDirty(feature);
            }

            EditorUtility.SetDirty(settings);
        }
    }
}
