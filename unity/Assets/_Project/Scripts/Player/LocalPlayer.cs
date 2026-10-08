using System;
using UnityEngine;

namespace AtelierVerse.Player
{
    /// <summary>
    /// 이 기기의 캐릭터를 가리키는 한곳. 게임 화면과 도구(블록 놓기)는 PC 조작이나 VR 조작을 직접 알지 않고 이곳을 거친다.
    /// 지금 어느 조작인지, 조작 막기, 조준하고 있는지, 조준 광선, 시점을 조작 방식과 상관없이 같은 이름으로 준다.
    /// 캐릭터의 뿌리에 붙으며, 같은 뿌리의 조작과 리그는 처음 쓸 때 찾는다. VR 쪽이 없는 캐릭터에서도 PC 조작만으로 동작한다.
    /// </summary>
    public class LocalPlayer : MonoBehaviour
    {
        private CharacterMotor motor;
        private PlayerModeSwitch modeSwitch;
        private DesktopPlayerController desktop;
        private ViewRig viewRig;
        private XrPlayerController xrControl;
        private XrRig xrRig;
        private bool found;
        private bool listening;

        /// <summary>조작 방식이 바뀌면 알린다.</summary>
        public event Action<ControlMode> ModeChanged;

        /// <summary>지금의 조작 방식. 조작 방식 고르기가 없는 캐릭터는 키보드·마우스다.</summary>
        public ControlMode Mode
        {
            get
            {
                Find();
                return modeSwitch != null ? modeSwitch.Mode : ControlMode.Desktop;
            }
        }

        public bool IsVr => Mode == ControlMode.Vr;

        /// <summary>이 캐릭터의 몸. 두 조작이 같은 몸을 쓴다.</summary>
        public CharacterMotor Motor
        {
            get
            {
                Find();
                return motor;
            }
        }

        /// <summary>PC 조작. 키보드·마우스에만 있는 일(마우스 잡기, 바라보는 방향 정하기)에 쓴다.</summary>
        public DesktopPlayerController Desktop
        {
            get
            {
                Find();
                return desktop;
            }
        }

        /// <summary>PC용 카메라 리그.</summary>
        public ViewRig ViewRig
        {
            get
            {
                Find();
                return viewRig;
            }
        }

        /// <summary>VR 리그. VR 쪽이 없는 캐릭터에서는 null이다.</summary>
        public XrRig XrRig
        {
            get
            {
                Find();
                return xrRig;
            }
        }

        /// <summary>이 캐릭터가 보는 카메라. 두 조작이 같은 카메라를 쓴다.</summary>
        public Camera ViewCamera
        {
            get
            {
                Find();
                return viewRig != null ? viewRig.ViewCamera : null;
            }
        }

        /// <summary>메뉴처럼 조작을 막아야 하는 화면이 열려 있는지.</summary>
        public bool InputBlocked { get; private set; }

        /// <summary>
        /// 조준하고 있는지. PC에서는 마우스를 잡았을 때, VR에서는 조작이 막혀 있지 않을 때다.
        /// 조준점을 보일지와 도구를 쓸 수 있는지를 정한다.
        /// </summary>
        public bool IsAiming
        {
            get
            {
                Find();
                if (IsVr) return !InputBlocked && xrRig != null && xrRig.isActiveAndEnabled;
                return desktop != null && desktop.LookCaptured;
            }
        }

        /// <summary>자기 몸이 보이지 않는 1인칭인지. VR은 늘 1인칭이다.</summary>
        public bool IsFirstPerson
        {
            get
            {
                Find();
                return IsVr || viewRig == null || viewRig.IsFirstPerson;
            }
        }

        /// <summary>1인칭과 3인칭을 바꿀 수 있는지. VR에서는 바꿀 수 없다.</summary>
        public bool CanToggleView
        {
            get
            {
                Find();
                return !IsVr && viewRig != null;
            }
        }

        private void OnEnable()
        {
            Find();
            if (modeSwitch == null || listening) return;

            modeSwitch.ModeChanged += OnModeChanged;
            listening = true;
        }

        private void OnDisable()
        {
            if (!listening) return;

            modeSwitch.ModeChanged -= OnModeChanged;
            listening = false;
        }

        /// <summary>조작을 막거나 푼다. 두 조작에 함께 알려, 막힌 채로 조작 방식이 바뀌어도 그대로 막혀 있게 한다.</summary>
        public void SetInputBlocked(bool blocked)
        {
            Find();
            InputBlocked = blocked;
            if (desktop != null) desktop.SetInputBlocked(blocked);
            if (xrControl != null) xrControl.SetInputBlocked(blocked);
        }

        /// <summary>막았던 조작을 풀고 바로 움직일 수 있는 상태로 돌아간다. PC에서는 마우스를 다시 잡는다.</summary>
        public void ResumeControl()
        {
            SetInputBlocked(false);
            if (!IsVr && desktop != null) desktop.CaptureLook(true);
        }

        /// <summary>1인칭과 3인칭을 바꾼다. 바꿀 수 없는 조작에서는 아무 일도 하지 않는다.</summary>
        public void ToggleView()
        {
            if (CanToggleView) viewRig.ToggleView();
        }

        /// <summary>
        /// 지금 조준하는 광선. PC에서는 화면 가운데, VR에서는 오른손이 가리키는 쪽이다.
        /// extraReach는 광선이 몸 밖에서 출발할 때(3인칭의 카메라) 닿는 거리에 더해 줄 길이다.
        /// </summary>
        public bool TryGetAim(out Ray ray, out float extraReach)
        {
            Find();
            ray = default;
            extraReach = 0f;

            if (IsVr) return xrRig != null && xrRig.TryGetPointerRay(out ray);

            Camera camera = ViewCamera;
            if (camera == null) return false;

            Transform view = camera.transform;
            ray = new Ray(view.position, view.forward);
            extraReach = Vector3.Distance(view.position, viewRig.HeadPosition);
            return true;
        }

        private void OnModeChanged(ControlMode mode)
        {
            ModeChanged?.Invoke(mode);
        }

        private void Find()
        {
            if (found) return;

            found = true;
            motor = GetComponent<CharacterMotor>();
            modeSwitch = GetComponent<PlayerModeSwitch>();
            desktop = GetComponent<DesktopPlayerController>();
            viewRig = GetComponent<ViewRig>();
            xrControl = GetComponent<XrPlayerController>();
            xrRig = GetComponent<XrRig>();
        }
    }
}
