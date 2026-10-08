using System;
using AtelierVerse.Core;
using UnityEngine;
using UnityEngine.XR;

namespace AtelierVerse.Player
{
    /// <summary>캐릭터를 무엇으로 조작하는지.</summary>
    public enum ControlMode
    {
        Desktop,
        Vr,
    }

    /// <summary>
    /// 이 기기의 캐릭터를 키보드·마우스로 조작할지 VR 기기로 조작할지 고른다. 기본은 키보드·마우스다.
    /// VR 화면이 켜져 있으면(XR 표시 장치가 돌고 있으면) VR 조작으로 시작한다. 두 조작은 같은 몸(CharacterMotor)을 쓴다.
    /// VR에서는 PC용 카메라 리그를 끈다. 블록 놓기는 두 조작에서 모두 켜 두며, 어느 쪽 입력을 읽을지는 블록 놓기가 LocalPlayer에게 묻는다.
    /// 다른 스크립트보다 먼저 실행해 조작이 켜지기 전에 고른다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerModeSwitch : MonoBehaviour
    {
        [SerializeField] private DesktopPlayerController desktopControl;
        [SerializeField] private ViewRig viewRig;
        [SerializeField] private XrPlayerController xrControl;
        [SerializeField] private XrRig xrRig;

        /// <summary>값을 넣으면 기기와 상관없이 그 방식으로 시작한다. 테스트와 개발 확인에 쓰고, 평소에는 null이다.</summary>
        public static ControlMode? Forced { get; set; }

        /// <summary>조작 방식이 바뀌면 알린다.</summary>
        public event Action<ControlMode> ModeChanged;

        public ControlMode Mode { get; private set; }

        public XrPlayerController XrControl => xrControl;

        public XrRig XrRig => xrRig;

        private void Awake()
        {
            Apply(Decide());
        }

        /// <summary>VR 화면을 켜려다 생긴 안내(켰다, 기기를 찾지 못했다)를 화면이 준비된 뒤에 한 번 알린다.</summary>
        private void Start()
        {
            if (XrSession.TakeNotice(out string message, out NoticeKind kind)) Notice.Post(message, kind);
        }

        /// <summary>시작할 때의 조작 방식. 강제한 값이 있으면 그 값, 없으면 VR 화면이 켜져 있는지로 정한다.</summary>
        public static ControlMode Decide()
        {
            if (Forced.HasValue) return Forced.Value;
            return XRSettings.isDeviceActive ? ControlMode.Vr : ControlMode.Desktop;
        }

        /// <summary>실행 중에 조작 방식을 바꾼다.</summary>
        public void SetMode(ControlMode mode)
        {
            if (mode == Mode) return;

            Apply(mode);
            ModeChanged?.Invoke(mode);
        }

        /// <summary>끄는 쪽을 먼저 꺼서, 카메라를 돌려받은 뒤에 다른 쪽이 쓰게 한다.</summary>
        private void Apply(ControlMode mode)
        {
            Mode = mode;

            if (mode == ControlMode.Vr)
            {
                Enable(desktopControl, false);
                Enable(viewRig, false);
                Enable(xrRig, true);
                Enable(xrControl, true);
            }
            else
            {
                Enable(xrControl, false);
                Enable(xrRig, false);
                Enable(viewRig, true);
                Enable(desktopControl, true);
            }
        }

        private static void Enable(Behaviour behaviour, bool enabled)
        {
            if (behaviour != null && behaviour.enabled != enabled) behaviour.enabled = enabled;
        }
    }
}
