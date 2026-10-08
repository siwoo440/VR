using System.Collections;
using System.IO;
using AtelierVerse.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.TestTools;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// VR 조작과 추적을 가상 기기로 실행해 확인한다. 가상 기기와 도우미는 XrPlayTestBase에 있다.
    /// 실제 기기에서의 화면과 착용감은 이 테스트로 확인되지 않는다.
    /// </summary>
    public class XrPlayTests : XrPlayTestBase
    {
        [UnityTest]
        public IEnumerator 기본은_키보드와_마우스_조작이다()
        {
            yield return LoadSandbox();
            FindParts();

            Assert.AreEqual(ControlMode.Desktop, modeSwitch.Mode);
            Assert.IsTrue(player.enabled, "PC 조작이 꺼져 있습니다.");
            Assert.IsTrue(player.Rig.enabled);
            Assert.IsFalse(xrControl.enabled, "VR 기기가 연결되어 있어도 VR 화면이 켜지지 않았으면 VR 조작은 꺼져 있어야 합니다.");
            Assert.IsFalse(rig.enabled);
            Assert.IsFalse(rig.LeftHand.gameObject.activeSelf);
            Assert.AreEqual("CameraPivot", player.Rig.ViewCamera.transform.parent.name);

            Vector3 before = player.transform.position;
            Press(keyboard.wKey);
            yield return new WaitForSeconds(0.5f);
            Release(keyboard.wKey);
            yield return null;
            Assert.Greater(Vector3.Dot(player.transform.position - before, player.transform.forward), 1f, "키보드로 걷지 못했습니다.");
        }

        [UnityTest]
        public IEnumerator VR로_시작하면_VR_조작이_켜지고_PC_조작은_꺼진다()
        {
            yield return LoadVr();

            Assert.AreEqual(ControlMode.Vr, modeSwitch.Mode);
            Assert.IsTrue(xrControl.enabled);
            Assert.IsTrue(rig.enabled);
            Assert.IsFalse(player.enabled, "VR에서는 PC 조작이 꺼져 있어야 합니다.");
            Assert.IsFalse(player.Rig.enabled, "VR에서는 PC용 카메라 리그가 꺼져 있어야 합니다.");
            Assert.IsTrue(player.GetComponent<BlockBuilder>().enabled, "블록 놓기는 VR에서도 켜져 있어야 합니다.");
            Assert.AreSame(rig.Origin, rig.Head.parent, "카메라가 추적 기준점 아래로 옮겨지지 않았습니다.");
            Assert.IsTrue(motor.Avatar.IsFirstPerson, "VR에서는 자기 몸이 시야를 가리지 않아야 합니다.");
            Assert.AreSame(motor, xrControl.Motor, "VR 조작이 PC 조작과 같은 몸을 써야 합니다.");
            Assert.IsFalse(Find<Transform>(ui, "FocusHint").gameObject.activeSelf, "VR 조작에서는 마우스 안내가 뜨지 않아야 합니다.");
        }

        [UnityTest]
        public IEnumerator 머리의_위치와_방향이_카메라에_옮겨진다()
        {
            yield return LoadVr();
            Vector3 feet = motor.transform.position;
            Assert.AreEqual(XrRig.DefaultHeadHeight, rig.Head.localPosition.y, 0.001f, "기기가 위치를 주기 전에는 선 키의 눈높이여야 합니다.");

            Set(headset.centerEyePosition, new Vector3(0f, 1.72f, 0f));
            Set(headset.centerEyeRotation, Quaternion.Euler(10f, 90f, 0f));
            yield return Frames(3);

            Assert.AreEqual(feet.y + 1.72f, rig.Head.position.y, 0.02f);
            Assert.Less(Vector3.Angle(Quaternion.Euler(10f, 90f, 0f) * Vector3.forward, rig.Head.forward), 0.5f);
            Assert.Less(Vector3.Angle(Vector3.right, rig.HeadForwardOnPlane), 0.5f);
        }

        [UnityTest]
        public IEnumerator 손은_컨트롤러를_따르고_기기가_없으면_보이지_않는다()
        {
            yield return LoadVr();
            var leftPosition = new Vector3(-0.25f, 1.1f, 0.3f);
            var rightPosition = new Vector3(0.2f, 1.25f, 0.35f);

            Set(leftController.devicePosition, leftPosition);
            Set(leftController.deviceRotation, Quaternion.Euler(0f, 30f, 0f));
            Set(rightController.devicePosition, rightPosition);
            yield return Frames(3);

            Assert.IsTrue(rig.LeftHand.gameObject.activeSelf);
            Assert.IsTrue(rig.RightHand.gameObject.activeSelf);
            Assert.Less(Vector3.Distance(rig.Origin.TransformPoint(leftPosition), rig.LeftHand.position), 0.01f);
            Assert.Less(Vector3.Distance(rig.Origin.TransformPoint(rightPosition), rig.RightHand.position), 0.01f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 30f, 0f), rig.LeftHand.localRotation), 0.5f);

            InputSystem.RemoveDevice(rightController);
            yield return Frames(3);
            Assert.IsFalse(rig.RightHand.gameObject.activeSelf, "컨트롤러가 없는 손이 보입니다.");
            Assert.IsTrue(rig.LeftHand.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator 왼쪽_스틱을_밀면_머리가_보는_쪽으로_걷는다()
        {
            yield return LoadVr();
            Set(headset.centerEyePosition, new Vector3(0f, 1.6f, 0f));
            Set(headset.centerEyeRotation, Quaternion.Euler(0f, 90f, 0f));
            yield return Frames(3);

            Vector3 before = motor.transform.position;
            Set(Stick(leftController), new Vector2(0f, 1f));
            yield return new WaitForSeconds(1f);
            Set(Stick(leftController), Vector2.zero);
            yield return Frames(2);

            Vector3 moved = motor.transform.position - before;
            Assert.That(moved.x, Is.InRange(motor.WalkSpeed - 1f, motor.WalkSpeed + 1f), "머리가 보는 쪽(+x)으로 걷는 속도만큼 가지 않았습니다.");
            Assert.Less(Mathf.Abs(moved.z), 0.3f, "머리가 보는 쪽이 아닌 방향으로 움직였습니다.");
            Assert.AreEqual(0f, motor.transform.eulerAngles.y, 0.5f, "걷는 것만으로 몸이 돌면 안 됩니다.");
        }

        [UnityTest]
        public IEnumerator 오른쪽_스틱을_밀_때마다_한_번씩_끊어서_돈다()
        {
            yield return LoadVr();
            float step = xrControl.SnapTurnAngle;

            Set(Stick(rightController), new Vector2(1f, 0f));
            yield return Frames(3);
            Assert.AreEqual(step, motor.transform.eulerAngles.y, 0.5f);

            yield return Frames(20);
            Assert.AreEqual(step, motor.transform.eulerAngles.y, 0.5f, "스틱을 민 채로 있으면 더 돌지 않아야 합니다.");

            Set(Stick(rightController), Vector2.zero);
            yield return Frames(3);
            Set(Stick(rightController), new Vector2(1f, 0f));
            yield return Frames(3);
            Assert.AreEqual(step * 2f, motor.transform.eulerAngles.y, 0.5f);

            Set(Stick(rightController), Vector2.zero);
            yield return Frames(3);
            Set(Stick(rightController), new Vector2(-1f, 0f));
            yield return Frames(3);
            Assert.AreEqual(step, motor.transform.eulerAngles.y, 0.5f, "왼쪽으로 밀면 반대로 돌아야 합니다.");
        }

        [UnityTest]
        public IEnumerator 오른손_단추로_뛰고_날며_날_때는_스틱으로_오르내린다()
        {
            yield return LoadVr();
            float ground = motor.transform.position.y;

            yield return Tap(Button(rightController, "primaryButton"));
            yield return new WaitForSeconds(0.2f);
            Assert.Greater(motor.transform.position.y, ground + 0.3f, "첫째 단추로 뛰어오르지 않았습니다.");
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(motor.IsGrounded);

            yield return Tap(Button(rightController, "secondaryButton"));
            Assert.IsTrue(motor.IsFlying, "둘째 단추로 날기가 켜지지 않았습니다.");

            Set(Stick(rightController), new Vector2(0f, 1f));
            yield return new WaitForSeconds(0.5f);
            Set(Stick(rightController), Vector2.zero);
            yield return Frames(2);
            float risen = motor.transform.position.y;
            Assert.Greater(risen, ground + 2f, "스틱을 위로 민 동안 충분히 오르지 않았습니다.");
            Assert.AreEqual(0f, motor.transform.eulerAngles.y, 0.5f, "위아래로 미는 것만으로 돌면 안 됩니다.");

            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(risen, motor.transform.position.y, 0.05f);

            yield return Tap(Button(rightController, "secondaryButton"));
            Assert.IsFalse(motor.IsFlying);
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(motor.IsGrounded, "날기를 껐는데 바닥에 내려서지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 왼쪽_스틱을_누른_채_밀면_달린다()
        {
            yield return LoadVr();
            Vector3 before = motor.transform.position;

            Press(Button(leftController, "thumbstickClicked"));
            Set(Stick(leftController), new Vector2(1f, 0f));
            yield return new WaitForSeconds(1f);
            Set(Stick(leftController), Vector2.zero);
            Release(Button(leftController, "thumbstickClicked"));
            yield return Frames(2);

            Assert.That(motor.transform.position.x - before.x, Is.InRange(motor.SprintSpeed - 1.5f, motor.SprintSpeed + 1.5f), "달리는 속도로 오른쪽으로 가지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 실제로_걸어_머리가_움직이면_몸이_머리_밑으로_따라온다()
        {
            yield return LoadVr();
            Set(headset.centerEyePosition, new Vector3(0f, 1.6f, 0f));
            yield return Frames(3);
            Vector3 feet = motor.transform.position;

            Set(headset.centerEyePosition, new Vector3(1f, 1.6f, 0.5f));
            yield return Frames(5);

            Vector3 body = motor.transform.position;
            Assert.AreEqual(feet.x + 1f, body.x, 0.05f, "몸이 머리 밑으로 따라오지 않았습니다.");
            Assert.AreEqual(feet.z + 0.5f, body.z, 0.05f);
            Assert.Less(Vector3.Distance(feet + new Vector3(1f, 1.6f, 0.5f), rig.Head.position), 0.05f, "몸이 따라온 만큼 머리가 더 밀려났습니다.");
            Assert.Less(rig.HeadOffsetOnPlane.magnitude, 0.05f);
        }

        [UnityTest]
        public IEnumerator VR에서는_키보드로_걷지_않는다()
        {
            yield return LoadVr();
            Vector3 before = motor.transform.position;

            Press(keyboard.wKey);
            yield return new WaitForSeconds(0.5f);
            Release(keyboard.wKey);
            yield return null;

            Vector3 moved = motor.transform.position - before;
            moved.y = 0f;
            Assert.Less(moved.magnitude, 0.05f);
        }

        [UnityTest]
        public IEnumerator 실행_중에_PC_조작으로_바꾸면_카메라가_돌아오고_다시_VR로_바꿀_수_있다()
        {
            yield return LoadVr();
            Set(headset.centerEyePosition, new Vector3(0.4f, 1.6f, 0f));
            Set(leftController.devicePosition, new Vector3(-0.2f, 1.2f, 0.3f));
            yield return Frames(3);

            int changes = 0;
            modeSwitch.ModeChanged += _ => changes++;
            modeSwitch.SetMode(ControlMode.Desktop);
            yield return Frames(3);

            Assert.AreEqual(ControlMode.Desktop, modeSwitch.Mode);
            Assert.IsTrue(player.enabled);
            Assert.IsFalse(xrControl.enabled);
            Assert.AreEqual("CameraPivot", player.Rig.ViewCamera.transform.parent.name, "카메라가 PC용 자리로 돌아오지 않았습니다.");
            Assert.Less(player.Rig.ViewCamera.transform.localPosition.magnitude, 0.01f);
            Assert.IsFalse(rig.LeftHand.gameObject.activeSelf, "PC 조작에서는 손이 보이지 않아야 합니다.");
            Assert.AreEqual(Vector3.zero, rig.Origin.localPosition);
            Assert.IsTrue(player.GetComponent<BlockBuilder>().enabled);

            Vector3 before = player.transform.position;
            Press(keyboard.wKey);
            yield return new WaitForSeconds(0.5f);
            Release(keyboard.wKey);
            yield return null;
            Assert.Greater(Vector3.Dot(player.transform.position - before, player.transform.forward), 1f, "PC 조작으로 돌아왔는데 걷지 못했습니다.");

            modeSwitch.SetMode(ControlMode.Vr);
            yield return Frames(3);
            Assert.IsTrue(xrControl.enabled);
            Assert.AreSame(rig.Origin, rig.Head.parent);
            Assert.AreEqual(2, changes);
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadVr();
            BeginCapture();

            Set(headset.centerEyePosition, new Vector3(0f, 1.6f, 0f));
            Set(headset.centerEyeRotation, Quaternion.Euler(18f, 0f, 0f));
            Set(leftController.devicePosition, new Vector3(-0.22f, 1.28f, 0.42f));
            Set(leftController.deviceRotation, Quaternion.Euler(-20f, 15f, 0f));
            Set(rightController.devicePosition, new Vector3(0.22f, 1.32f, 0.45f));
            Set(rightController.deviceRotation, Quaternion.Euler(-30f, -15f, 0f));
            yield return Frames(3);

            SaveCapture(Path.Combine(directory, "vr-hands.png"));
        }
    }
}
