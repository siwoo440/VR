using System.Collections;
using AtelierVerse.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// VR 테스트의 공통 바탕. 실제 헤드셋 없이, 입력 시스템에 가상의 머리 기기와 두 컨트롤러를 붙여 값을 넣는다.
    /// 시험용 컨트롤러는 실제 컨트롤러의 공통 쓰임새(스틱, 단추, 방아쇠, 옆 단추, 메뉴 단추)와 가리키는 자세만 가진다.
    /// 실제 기기에서의 화면과 착용감은 이 바탕으로 확인되지 않는다.
    /// </summary>
    public abstract class XrPlayTestBase : PlayTestBase
    {
        protected const float Settle = 0.5f;

        private const string ControllerLayout = "AtelierTestXrController";

        // 가리키는 자세(pointerPosition, pointerRotation)는 OpenXR의 컨트롤러들이 같은 이름으로 준다.
        private const string ControllerJson = @"{
            ""name"": ""AtelierTestXrController"",
            ""extend"": ""XRController"",
            ""controls"": [
                { ""name"": ""thumbstick"", ""layout"": ""Stick"", ""usage"": ""Primary2DAxis"" },
                { ""name"": ""thumbstickClicked"", ""layout"": ""Button"", ""usage"": ""Primary2DAxisClick"" },
                { ""name"": ""primaryButton"", ""layout"": ""Button"", ""usage"": ""PrimaryButton"" },
                { ""name"": ""secondaryButton"", ""layout"": ""Button"", ""usage"": ""SecondaryButton"" },
                { ""name"": ""triggerPressed"", ""layout"": ""Button"", ""usage"": ""TriggerButton"" },
                { ""name"": ""gripPressed"", ""layout"": ""Button"", ""usage"": ""GripButton"" },
                { ""name"": ""menu"", ""layout"": ""Button"", ""usage"": ""MenuButton"" },
                { ""name"": ""pointerPosition"", ""layout"": ""Vector3"" },
                { ""name"": ""pointerRotation"", ""layout"": ""Quaternion"" }
            ]
        }";

        // 추적 공간에서 오른손을 두는 자리. 몸의 오른쪽 앞, 가슴 높이다.
        private static readonly Vector3 RightHandRest = new Vector3(0.22f, 1.25f, 0.25f);

        /// <summary>추적 공간에서 왼손을 두는 자리. 몸의 왼쪽 앞, 가슴 높이다. 부품 판이 이 손 위에 뜬다.</summary>
        protected static readonly Vector3 LeftHandRest = new Vector3(-0.25f, 1.15f, 0.35f);

        protected XRHMD headset;
        protected XRController leftController;
        protected XRController rightController;
        protected PlayerModeSwitch modeSwitch;
        protected CharacterMotor motor;
        protected XrRig rig;
        protected XrPlayerController xrControl;

        public override void Setup()
        {
            base.Setup();

            InputSystem.RegisterLayout(ControllerJson);
            headset = InputSystem.AddDevice<XRHMD>();
            leftController = (XRController)InputSystem.AddDevice(ControllerLayout);
            rightController = (XRController)InputSystem.AddDevice(ControllerLayout);
            InputSystem.SetDeviceUsage(leftController, CommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(rightController, CommonUsages.RightHand);
        }

        /// <summary>VR 조작으로 Sandbox 씬을 연다.</summary>
        protected IEnumerator LoadVr()
        {
            PlayerModeSwitch.Forced = ControlMode.Vr;
            yield return LoadSandbox();
            FindParts();
            yield return new WaitForSeconds(Settle);
        }

        protected void FindParts()
        {
            modeSwitch = Object.FindAnyObjectByType<PlayerModeSwitch>();
            Assert.IsNotNull(modeSwitch, "캐릭터에 조작 방식 고르기가 없습니다.");
            motor = player.Motor;
            rig = modeSwitch.XrRig;
            xrControl = modeSwitch.XrControl;
            Assert.IsNotNull(rig, "캐릭터에 VR 리그가 없습니다.");
            Assert.IsNotNull(xrControl, "캐릭터에 VR 조작이 없습니다.");
        }

        /// <summary>왼손의 메뉴 단추로 메뉴를 연다.</summary>
        protected IEnumerator OpenMenuWithController()
        {
            yield return Tap(Button(leftController, "menu"));
            Assert.IsTrue(ui.IsMenuOpen, "왼손의 메뉴 단추로 메뉴가 열리지 않았습니다.");
            yield return Frames(2);
        }

        /// <summary>오른손을 가슴 앞에 두고 월드의 한 점을 가리키게 한다. 가리키는 자세와 손의 자세를 함께 넣는다.</summary>
        protected IEnumerator PointRightHandAt(Vector3 worldTarget)
        {
            Vector3 worldPosition = rig.Origin.TransformPoint(RightHandRest);
            Quaternion worldRotation = Quaternion.LookRotation(worldTarget - worldPosition);
            Quaternion trackedRotation = Quaternion.Inverse(rig.Origin.rotation) * worldRotation;

            Set((Vector3Control)rightController["pointerPosition"], RightHandRest);
            Set((QuaternionControl)rightController["pointerRotation"], trackedRotation);
            Set(rightController.devicePosition, RightHandRest);
            Set(rightController.deviceRotation, trackedRotation);
            yield return Frames(3);
        }

        /// <summary>왼손을 가슴 앞에 든다. 그래야 왼손 위의 부품 판이 눈에 보이는 자리에 뜬다.</summary>
        protected IEnumerator RaiseLeftHand()
        {
            Set(leftController.devicePosition, LeftHandRest);
            yield return Frames(3);
        }

        /// <summary>오른손의 옆 단추를 한 번 쥐었다 놓는다.</summary>
        protected IEnumerator SqueezeGrip()
        {
            yield return Tap(Button(rightController, "gripPressed"));
            yield return Frames(2);
        }

        /// <summary>왼손의 방아쇠를 한 번 당겼다 놓는다.</summary>
        protected IEnumerator PullLeftTrigger()
        {
            yield return Tap(Button(leftController, "triggerPressed"));
            yield return Frames(2);
        }

        /// <summary>오른손의 방아쇠를 한 번 당겼다 놓는다.</summary>
        protected IEnumerator PullTrigger()
        {
            yield return Tap(Button(rightController, "triggerPressed"));
            yield return Frames(2);
        }

        /// <summary>화면 요소의 가운데가 월드에서 어디인지. 화면이 눈앞의 판으로 떠 있을 때 쓴다.</summary>
        protected static Vector3 CenterOf(Component element)
        {
            var rect = (RectTransform)element.transform;
            return rect.TransformPoint(rect.rect.center);
        }

        protected static Vector2Control Stick(InputDevice controller)
        {
            return (Vector2Control)controller["thumbstick"];
        }

        protected static ButtonControl Button(InputDevice controller, string name)
        {
            return (ButtonControl)controller[name];
        }
    }
}
