using System.Collections;
using System.IO;
using AtelierVerse.Core;
using AtelierVerse.Player;
using AtelierVerse.UI;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// VR에서의 게임 화면을 가상 기기로 실행해 확인한다: 눈앞에 뜨는 판, 왼손 메뉴 단추, 오른손 광선으로 누르기, VR용 안내, 알림.
    /// 헤드셋 안에서 글자가 읽히는지와 판의 거리·크기가 편한지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class XrMenuPlayTests : XrPlayTestBase
    {
        [UnityTest]
        public IEnumerator PC에서는_화면에_겹쳐_그리고_손_광선_부품은_꺼져_있다()
        {
            yield return LoadSandbox();
            FindParts();
            Canvas canvas = Find<Canvas>(ui, "Canvas");

            Assert.IsFalse(ui.Player.IsVr);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            Assert.IsFalse(ui.XrPanel.IsWorldSpace);
            Assert.IsFalse(canvas.GetComponent<TrackedDeviceRaycaster>().enabled, "PC에서는 손 광선 부품이 꺼져 있어야 합니다.");
            Assert.IsTrue(Find<Transform>(ui, "Hud").gameObject.activeSelf);
            Assert.IsFalse(rig.PointerShown);

            ui.OpenMenu();
            yield return Frames(2);
            Assert.IsFalse(ui.Menu.IsVrMode);
            Assert.IsTrue(Find<Transform>(ui.Menu, "Scrim").gameObject.activeSelf);
            Assert.IsTrue(Find<Button>(ui.Menu, "ViewTile").interactable);
            Assert.IsFalse(Find<Transform>(ui.Menu, "PcOnly").gameObject.activeSelf, "PC에서는 \"PC 전용\" 딱지가 보이지 않아야 합니다.");
            Assert.IsTrue(Find<Transform>(ui.Menu, "KeysDesktop").gameObject.activeSelf);
            Assert.IsFalse(Find<Transform>(ui.Menu, "KeysVr").gameObject.activeSelf);
            Assert.IsTrue(Find<Transform>(Find<Button>(ui.Menu, "Resume"), "Key").gameObject.activeSelf, "PC에서는 단추의 키 딱지가 보여야 합니다.");
        }

        [UnityTest]
        public IEnumerator VR에서는_화면이_눈앞의_판으로_뜨고_늘_보이는_화면은_숨는다()
        {
            yield return LoadVr();
            Canvas canvas = Find<Canvas>(ui, "Canvas");
            InputSystemUIInputModule module = ui.GetComponentInChildren<InputSystemUIInputModule>();

            Assert.IsTrue(ui.Player.IsVr);
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);
            Assert.IsTrue(ui.XrPanel.IsWorldSpace);
            Assert.AreSame(player.Rig.ViewCamera, canvas.worldCamera, "판을 가리키는 계산에 머리의 카메라를 써야 합니다.");
            Assert.IsTrue(canvas.GetComponent<TrackedDeviceRaycaster>().enabled);
            Assert.AreSame(rig.Origin, module.xrTrackingOrigin, "컨트롤러의 자세는 추적 공간을 기준으로 옮겨야 합니다.");

            Assert.IsFalse(Find<Transform>(ui, "Hud").gameObject.activeSelf, "VR에서는 늘 보이는 화면을 감춥니다.");
            Assert.IsTrue(Find<NoticeBar>(ui, "Notice").gameObject.activeInHierarchy, "알림 띠는 VR에서도 살아 있어야 합니다.");
            Assert.IsFalse(ui.IsMenuOpen);
            Assert.AreEqual(XrRig.IdlePointerLength, rig.PointerLength, 0.001f, "가리켜 할 일이 없으면 광선은 짧아야 합니다.");

            Vector3 toPanel = canvas.transform.position - rig.Head.position;
            toPanel.y = 0f;
            Assert.Less(Vector3.Angle(rig.HeadForwardOnPlane, toPanel), 3f, "판이 머리 앞에 있지 않습니다.");
            Assert.AreEqual(ui.XrPanel.FollowDistance, toPanel.magnitude, 0.05f);
        }

        [UnityTest]
        public IEnumerator 왼손_메뉴_단추로_메뉴를_열면_눈앞에_놓이고_조작이_막힌다()
        {
            yield return LoadVr();
            Canvas canvas = Find<Canvas>(ui, "Canvas");
            Set(headset.centerEyePosition, new Vector3(0f, 1.6f, 0f));
            Set(headset.centerEyeRotation, Quaternion.Euler(0f, 90f, 0f));
            yield return Frames(3);

            yield return OpenMenuWithController();

            Assert.IsTrue(ui.Player.InputBlocked);
            Assert.IsTrue(xrControl.InputBlocked, "메뉴가 열려 있으면 VR 조작이 막혀야 합니다.");
            Assert.IsTrue(rig.PointerShown, "메뉴가 열려 있으면 오른손 광선이 보여야 합니다.");
            Assert.AreEqual(XrRig.DefaultPointerLength, rig.PointerLength, 0.001f, "메뉴가 열려 있으면 광선이 길게 나가야 합니다.");

            Vector3 offset = canvas.transform.position - rig.Head.position;
            Assert.That(ui.XrPanel.Distance, Is.InRange(0.7f - 0.001f, ui.XrPanel.MenuDistance + 0.001f));
            Assert.AreEqual(ui.XrPanel.Distance, offset.x, 0.02f, "판이 머리가 보는 쪽(+x)에 놓이지 않았습니다.");
            Assert.AreEqual(0f, offset.z, 0.02f);
            Assert.That(offset.y, Is.InRange(-0.3f, 0f), "판의 가운데는 눈높이보다 조금 아래여야 합니다.");
            Assert.Less(Vector3.Angle(Vector3.right, canvas.transform.forward), 1f, "판이 머리 쪽을 보고 있지 않습니다.");
            Assert.AreEqual(XrUiPanel.ScaleAt(ui.XrPanel.MetersPerPixel, ui.XrPanel.Distance, ui.XrPanel.MenuDistance), canvas.transform.localScale.x, 0.00001f);

            Vector3 before = motor.transform.position;
            Set(Stick(leftController), new Vector2(0f, 1f));
            yield return new WaitForSeconds(0.5f);
            Set(Stick(leftController), Vector2.zero);
            yield return Frames(2);
            Vector3 moved = motor.transform.position - before;
            moved.y = 0f;
            Assert.Less(moved.magnitude, 0.05f, "메뉴가 열려 있는데 스틱으로 걸었습니다.");

            Vector3 placed = canvas.transform.position;
            Set(headset.centerEyeRotation, Quaternion.Euler(0f, 200f, 0f));
            yield return new WaitForSeconds(0.5f);
            Assert.Less(Vector3.Distance(placed, canvas.transform.position), 0.001f, "메뉴가 열려 있는 동안 판은 놓인 자리에 머물러야 합니다.");

            // 메뉴 단추를 앱이 쓸 수 없는 컨트롤러를 위해 왼손의 둘째 단추로도 열고 닫는다.
            yield return Tap(Button(leftController, "secondaryButton"));
            Assert.IsFalse(ui.IsMenuOpen);
            Assert.IsFalse(xrControl.InputBlocked);
            yield return Frames(2);
            Assert.AreEqual(XrRig.IdlePointerLength, rig.PointerLength, 0.001f, "메뉴를 닫으면 광선은 다시 짧아져야 합니다.");
        }

        [UnityTest]
        public IEnumerator 오른손으로_가리키고_방아쇠를_당기면_단추가_눌린다()
        {
            yield return LoadVr();
            yield return OpenMenuWithController();

            Vector3 settingsTab = CenterOf(Find<Button>(ui.Menu, "Tab2"));
            yield return PointRightHandAt(settingsTab);
            Assert.AreEqual(Vector3.Distance(rig.RightPointer.position, settingsTab), rig.PointerLength, 0.02f, "광선이 가리킨 단추에서 끝나지 않았습니다.");

            yield return PullTrigger();
            Assert.AreEqual(2, ui.Menu.CurrentTab, "가리킨 탭이 눌리지 않았습니다.");

            // 오른손이 다른 탭을 가리키는 동안 왼손의 방아쇠를 당겨도 눌리지 않는다.
            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Menu, "Tab0")));
            yield return Tap(Button(leftController, "triggerPressed"));
            Assert.AreEqual(2, ui.Menu.CurrentTab, "왼손의 방아쇠로 단추가 눌렸습니다.");

            // 아무것도 없는 곳을 가리키면 광선은 기본 길이로 돌아간다.
            yield return PointRightHandAt(rig.Head.position + Vector3.up * 5f);
            Assert.AreEqual(XrRig.DefaultPointerLength, rig.PointerLength, 0.001f);

            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Menu, "Resume")));
            yield return PullTrigger();
            Assert.IsFalse(ui.IsMenuOpen, "\"돌아가기\"를 가리켜 눌렀는데 메뉴가 닫히지 않았습니다.");
            Assert.IsFalse(xrControl.InputBlocked);
        }

        [UnityTest]
        public IEnumerator 메뉴에서_시작_위치로를_가리켜_누르면_돌아가고_메뉴가_닫힌다()
        {
            yield return LoadVr();
            Vector3 start = motor.transform.position;

            Set(Stick(leftController), new Vector2(0f, 1f));
            yield return new WaitForSeconds(1f);
            Set(Stick(leftController), Vector2.zero);
            yield return Frames(2);
            Assert.Greater((motor.transform.position - start).magnitude, 2f, "걸어 나가지 못했습니다.");

            yield return OpenMenuWithController();
            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Menu, "Respawn")));
            yield return PullTrigger();
            yield return Frames(3);

            Assert.IsFalse(ui.IsMenuOpen);
            Assert.Less((motor.transform.position - start).magnitude, 0.3f, "시작 위치로 돌아가지 않았습니다.");
            Assert.IsFalse(xrControl.InputBlocked);
        }

        [UnityTest]
        public IEnumerator VR_메뉴에는_VR_조작_안내가_보이고_시점_타일은_누를_수_없다()
        {
            yield return LoadVr();
            yield return OpenMenuWithController();
            Button viewTile = Find<Button>(ui.Menu, "ViewTile");

            Assert.IsTrue(ui.Menu.IsVrMode);
            Assert.IsFalse(Find<Transform>(ui.Menu, "Scrim").gameObject.activeSelf, "VR에서는 메뉴 뒤의 어두운 막을 감춥니다.");
            Assert.IsFalse(viewTile.interactable, "VR은 늘 1인칭이라 시점 타일을 누를 수 없어야 합니다.");
            Assert.IsTrue(Find<Transform>(ui.Menu, "PcOnly").gameObject.activeSelf);
            Assert.IsTrue(Find<Transform>(ui.Menu, "KeysVr").gameObject.activeSelf);
            Assert.IsFalse(Find<Transform>(ui.Menu, "KeysDesktop").gameObject.activeSelf);
            Assert.IsTrue(Find<Transform>(ui.Menu, "VrNote").gameObject.activeSelf);
            Assert.IsFalse(Find<Transform>(Find<Button>(ui.Menu, "Resume"), "Key").gameObject.activeSelf, "VR에서는 키 딱지를 감춥니다.");

            yield return PointRightHandAt(CenterOf(viewTile));
            yield return PullTrigger();
            Assert.IsTrue(ui.IsMenuOpen, "누를 수 없는 타일을 눌렀는데 메뉴가 닫혔습니다.");
            Assert.IsTrue(ui.Player.IsFirstPerson);
        }

        [UnityTest]
        public IEnumerator 설정의_막대를_가리켜_누르면_그_자리의_값이_된다()
        {
            yield return LoadVr();
            yield return OpenMenuWithController();
            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Menu, "Tab2")));
            yield return PullTrigger();
            Assert.AreEqual(2, ui.Menu.CurrentTab);

            Slider slider = Find<Slider>(ui.Menu, "LookSlider");
            var rect = (RectTransform)slider.transform;
            float before = slider.normalizedValue;
            float target = before < 0.5f ? 0.8f : 0.2f;
            Vector3 point = rect.TransformPoint(new Vector3(Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, target), rect.rect.center.y, 0f));

            yield return PointRightHandAt(point);
            yield return PullTrigger();

            Assert.AreEqual(target, slider.normalizedValue, 0.08f, "가리켜 누른 자리로 막대가 옮겨지지 않았습니다.");
            Assert.AreNotEqual(before, slider.normalizedValue);
        }

        [UnityTest]
        public IEnumerator 알림은_VR에서도_눈앞에_보이고_머리를_따라온다()
        {
            yield return LoadVr();
            Set(headset.centerEyePosition, new Vector3(0f, 1.6f, 0f));
            yield return Frames(3);

            NoticeBar notice = Find<NoticeBar>(ui, "Notice");
            Transform bar = Find<Transform>(notice, "Bar");

            Notice.Post("시험 알림");
            yield return Frames(2);
            Assert.IsTrue(notice.IsVisible);
            Assert.IsTrue(bar.gameObject.activeInHierarchy, "VR에서 알림 띠가 보이지 않습니다.");
            Assert.Less(AngleFromView(bar.position), 20f, "알림이 시야 가운데 가까이에 있지 않습니다.");

            Set(headset.centerEyeRotation, Quaternion.Euler(0f, 120f, 0f));
            yield return new WaitForSeconds(1.2f);
            Notice.Post("돌아본 뒤의 알림");
            yield return Frames(2);
            Assert.IsTrue(bar.gameObject.activeInHierarchy);
            Assert.Less(AngleFromView(bar.position), 20f, "머리를 돌린 뒤 알림이 시야에 따라오지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 앞이_막혀_있으면_판을_벽_앞으로_당겨_놓는다()
        {
            yield return LoadVr();
            Canvas canvas = Find<Canvas>(ui, "Canvas");
            Set(headset.centerEyePosition, new Vector3(0f, 1.6f, 0f));
            yield return Frames(3);

            // 머리 앞 1.2m쯤에 블록을 세운다.
            BlockWorld world = Object.FindAnyObjectByType<BlockWorld>();
            Vector3Int cell = GridMath.WorldToCell(rig.Head.position + rig.HeadForwardOnPlane * 1.2f);
            Assert.AreEqual(PlaceResult.Ok, world.Place(cell, 0), "앞을 막을 블록을 놓지 못했습니다.");
            yield return new WaitForFixedUpdate();
            yield return Frames(2);
            Assert.IsTrue(Physics.Raycast(rig.Head.position, rig.HeadForwardOnPlane, out RaycastHit wall, 3f), "블록이 머리 앞을 막고 있지 않습니다.");

            yield return OpenMenuWithController();

            Assert.Less(ui.XrPanel.Distance, ui.XrPanel.MenuDistance - 0.2f, "앞이 막혔는데 판을 당겨 놓지 않았습니다.");
            Assert.GreaterOrEqual(ui.XrPanel.Distance, 0.7f - 0.001f, "판을 너무 가까이 놓았습니다.");
            Vector3 toPanel = canvas.transform.position - rig.Head.position;
            toPanel.y = 0f;
            Assert.LessOrEqual(toPanel.magnitude, Mathf.Max(0.7f, wall.distance) + 0.001f, "판이 벽 뒤에 놓였습니다.");
            Assert.Less(canvas.transform.localScale.x, ui.XrPanel.MetersPerPixel, "가까이 놓인 판은 그만큼 작아져야 합니다.");
        }

        [UnityTest]
        public IEnumerator 실행_중에_조작_방식을_바꾸면_화면도_따라_바뀐다()
        {
            yield return LoadVr();
            Canvas canvas = Find<Canvas>(ui, "Canvas");
            yield return OpenMenuWithController();

            modeSwitch.SetMode(ControlMode.Desktop);
            yield return Frames(3);

            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            Assert.IsTrue(Find<Transform>(ui, "Hud").gameObject.activeSelf);
            Assert.IsTrue(Find<Transform>(ui.Menu, "Scrim").gameObject.activeSelf);
            Assert.IsFalse(ui.Menu.IsVrMode);
            Assert.IsTrue(ui.IsMenuOpen, "조작 방식을 바꿔도 열려 있던 메뉴는 그대로여야 합니다.");
            Assert.IsTrue(player.InputBlocked, "메뉴가 열린 채로 PC 조작이 되었으면 PC 조작도 막혀 있어야 합니다.");
            Assert.IsFalse(rig.PointerShown);

            yield return Tap(keyboard.escapeKey);
            Assert.IsFalse(ui.IsMenuOpen);
            Assert.IsTrue(player.LookCaptured, "메뉴를 닫으면 마우스를 다시 잡아야 합니다.");

            modeSwitch.SetMode(ControlMode.Vr);
            yield return Frames(3);
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);
            Assert.IsFalse(Find<Transform>(ui, "Hud").gameObject.activeSelf);
            Assert.IsTrue(ui.Menu.IsVrMode);
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadVr();
            BeginCapture();

            Set(headset.centerEyePosition, new Vector3(0f, 1.6f, 0f));
            Set(leftController.devicePosition, new Vector3(-0.24f, 1.2f, 0.3f));
            Set(leftController.deviceRotation, Quaternion.Euler(-25f, 10f, 0f));
            yield return Frames(3);

            yield return OpenMenuWithController();
            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Menu, "RespawnTile")));
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "vr-menu.png"));

            // 글자를 확인할 수 있게 판만 가까이 당겨 찍은 그림. 헤드셋에서 보이는 크기가 아니다.
            Camera view = player.Rig.ViewCamera;
            float fieldOfView = view.fieldOfView;
            view.fieldOfView = 44f;
            SaveCapture(Path.Combine(directory, "vr-menu-close.png"));
            ui.Menu.ShowTab(3);
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "vr-menu-help-close.png"));
            ui.Menu.ShowTab(2);
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "vr-menu-settings-close.png"));
            ui.Menu.ShowTab(0);
            view.fieldOfView = fieldOfView;

            ui.Menu.ShowTab(3);
            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Menu, "Tab3")));
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "vr-menu-help.png"));

            yield return Tap(Button(leftController, "menu"));
            Notice.Post(XrSession.StartedMessage);
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "vr-notice.png"));
        }

        /// <summary>머리가 보는 방향과 한 점이 이루는 각도.</summary>
        private float AngleFromView(Vector3 world)
        {
            return Vector3.Angle(rig.Head.forward, world - rig.Head.position);
        }
    }
}
