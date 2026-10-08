using System.Collections;
using System.IO;
using AtelierVerse.Player;
using AtelierVerse.UI;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// VR에서 만들기를 가상 기기로 실행해 확인한다: 왼손 위의 부품 판, 오른손 광선으로 놓기·지우기·칠하기, 되돌리기.
    /// 손의 각도와 놓는 느낌이 실제 컨트롤러에서 자연스러운지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class XrBuildPlayTests : XrPlayTestBase
    {
        private const int GoldPart = 0;
        private const int BluePart = 1;

        // 캐릭터가 처음 서는 자리(0.5, 0, -5.5)의 3m 앞에 있는 빈 바닥 칸.
        private static readonly Vector3Int FloorCell = new Vector3Int(0, 0, -3);

        private BlockWorld world;
        private BlockBuilder builder;

        [UnityTest]
        public IEnumerator PC에서는_부품_판이_보이지_않는다()
        {
            yield return LoadSandbox();

            Assert.IsNotNull(ui.Palette, "게임 화면에 부품 판이 없습니다.");
            Assert.IsFalse(ui.Palette.IsAttached);
            Assert.IsFalse(ui.Palette.IsShown, "PC에서는 부품 판이 보이지 않아야 합니다.");

            player.CaptureLook(true);
            yield return Tap(keyboard.digit2Key);
            Assert.AreEqual(BluePart, ui.Palette.Hotbar.SelectedIndex, "부품 판의 부품 칸은 화면 아래의 부품 칸과 같은 선택을 가져야 합니다.");
            Assert.IsFalse(ui.Palette.IsShown);
        }

        [UnityTest]
        public IEnumerator VR에서도_블록_놓기가_켜져_있고_왼손_위에_부품_판이_뜬다()
        {
            yield return LoadVrBuild();
            Canvas canvas = ui.Palette.GetComponentInChildren<Canvas>(true);

            Assert.IsTrue(builder.enabled, "블록 놓기는 VR에서도 켜져 있어야 합니다.");
            Assert.IsTrue(ui.Palette.IsAttached);
            Assert.IsTrue(ui.Palette.IsShown, "왼손 컨트롤러가 있는데 부품 판이 보이지 않습니다.");
            Assert.AreSame(player.Rig.ViewCamera, canvas.worldCamera, "부품 판을 가리키는 계산에 머리의 카메라를 써야 합니다.");

            Vector3 expected = rig.LeftHand.position + Vector3.up * ui.Palette.Lift;
            Assert.Less(Vector3.Distance(expected, canvas.transform.position), 0.01f, "부품 판이 왼손 위에 있지 않습니다.");
            Assert.Less(Vector3.Angle(canvas.transform.position - rig.Head.position, canvas.transform.forward), 1f, "부품 판이 머리 쪽을 보고 있지 않습니다.");

            Assert.AreEqual("부품을 고르세요", ui.Palette.Title);
            Assert.AreEqual($"블록 {world.Count}/{world.MaxBlocks}", ui.Palette.BlockCountText);
            Assert.AreEqual(GameUi.WalkLabel, ui.Palette.ModeText);
            Assert.IsFalse(Find<Transform>(ui, "Hud").gameObject.activeSelf, "늘 보이는 화면은 VR에서 여전히 감춥니다.");

            Assert.IsTrue(rig.PointerShown, "부품 판을 가리킬 수 있게 광선이 보여야 합니다.");
            Assert.AreEqual(XrRig.IdlePointerLength, rig.PointerLength, 0.001f, "부품을 고르지 않았으면 광선은 짧아야 합니다.");
        }

        [UnityTest]
        public IEnumerator 부품_판의_칸을_가리켜_누르면_부품이_골라지고_블록은_놓이지_않는다()
        {
            yield return LoadVrBuild();
            int before = world.Count;
            Button slot = Find<Button>(ui.Palette, "Slot2");

            yield return PointRightHandAt(CenterOf(slot));
            Assert.IsTrue(ui.Player.IsPointingAtUi, "부품 판을 가리키고 있는데 화면을 가리킨다고 알지 못합니다.");
            Assert.AreEqual(Vector3.Distance(rig.RightPointer.position, CenterOf(slot)), rig.PointerLength, 0.01f, "광선이 가리킨 칸에서 끝나지 않았습니다.");

            yield return PullTrigger();
            Assert.AreEqual(BluePart, ui.Hotbar.SelectedIndex, "가리켜 누른 칸의 부품이 골라지지 않았습니다.");
            Assert.AreEqual(BluePart, builder.SelectedPart);
            Assert.AreEqual(ui.Hotbar.GetItemName(BluePart), ui.Palette.Title);
            Assert.AreEqual(before, world.Count, "부품 판을 누른 방아쇠로 블록이 놓였습니다.");

            // 같은 칸을 다시 누르면 선택이 풀린다.
            yield return PullTrigger();
            Assert.AreEqual(HotbarModel.None, ui.Hotbar.SelectedIndex);
            Assert.AreEqual("부품을 고르세요", ui.Palette.Title);
            Assert.AreEqual(before, world.Count);
        }

        [UnityTest]
        public IEnumerator 오른손으로_바닥을_가리켜_방아쇠를_당기면_블록이_놓인다()
        {
            yield return LoadVrBuild();
            int before = world.Count;
            ui.Hotbar.Select(GoldPart);

            Vector3 floorPoint = GridMath.CellToWorldCenter(FloorCell);
            floorPoint.y = 0f;
            yield return PointRightHandAt(floorPoint);

            Assert.IsFalse(ui.Player.IsPointingAtUi);
            Assert.IsTrue(builder.HasTarget, "가리킨 곳에 놓일 칸이 없습니다.");
            Assert.AreEqual(FloorCell, builder.TargetCell);
            Assert.IsTrue(builder.CanPlaceAtTarget);
            Assert.IsTrue(builder.HasAimHit);
            Assert.AreEqual(Vector3.Distance(rig.RightPointer.position, floorPoint), rig.PointerLength, 0.05f, "광선이 블록을 놓을 자리에서 끝나지 않았습니다.");

            GameObject ghost = GameObject.Find("BlockGhost");
            Assert.IsNotNull(ghost, "놓일 자리의 미리 보기 블록이 보이지 않습니다.");
            Assert.AreEqual(GridMath.CellToWorldCenter(FloorCell), ghost.transform.position);

            yield return PullTrigger();

            Assert.AreEqual(before + 1, world.Count, "방아쇠로 블록이 놓이지 않았습니다.");
            Assert.IsTrue(world.TryGetPart(FloorCell, out int part));
            Assert.AreEqual(GoldPart, part);
            Assert.AreEqual($"블록 {world.Count}/{world.MaxBlocks}", ui.Palette.BlockCountText);
        }

        [UnityTest]
        public IEnumerator 왼손_방아쇠로_칠하고_오른손_옆_단추로_지운다()
        {
            yield return LoadVrBuild();
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(FloorCell, GoldPart));
            // 물리 갱신을 기다린 바로 그 시점에는 가상 기기에 값을 넣을 수 없으므로 프레임을 더 넘긴다.
            yield return new WaitForFixedUpdate();
            yield return Frames(2);
            int before = world.Count;

            ui.Hotbar.Select(BluePart);
            yield return PointRightHandAt(GridMath.CellToWorldCenter(FloorCell));
            Assert.IsTrue(builder.HasBlockTarget, "가리킨 블록을 찾지 못했습니다.");

            yield return PullLeftTrigger();
            Assert.IsTrue(world.TryGetPart(FloorCell, out int painted));
            Assert.AreEqual(BluePart, painted, "왼손 방아쇠로 칠해지지 않았습니다.");
            Assert.AreEqual(before, world.Count, "칠하기가 블록 수를 바꿨습니다.");

            yield return SqueezeGrip();
            Assert.IsFalse(world.Has(FloorCell), "오른손 옆 단추로 지워지지 않았습니다.");
            Assert.AreEqual(before - 1, world.Count);
        }

        [UnityTest]
        public IEnumerator 부품_판의_되돌리기와_다시_실행_단추가_동작한다()
        {
            yield return LoadVrBuild();
            ui.Hotbar.Select(GoldPart);
            Vector3 floorPoint = GridMath.CellToWorldCenter(FloorCell);
            floorPoint.y = 0f;
            yield return PointRightHandAt(floorPoint);
            yield return PullTrigger();
            Assert.IsTrue(world.Has(FloorCell));

            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Palette, "Undo")));
            yield return PullTrigger();
            Assert.IsFalse(world.Has(FloorCell), "부품 판의 되돌리기 단추가 놓은 블록을 되돌리지 않았습니다.");

            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Palette, "Redo")));
            yield return PullTrigger();
            Assert.IsTrue(world.Has(FloorCell), "부품 판의 다시 실행 단추가 동작하지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 메뉴가_열려_있으면_방아쇠로_블록이_놓이지_않고_부품_판은_숨는다()
        {
            yield return LoadVrBuild();
            int before = world.Count;
            ui.Hotbar.Select(GoldPart);

            yield return OpenMenuWithController();
            Assert.IsFalse(ui.Palette.IsShown, "메뉴가 열려 있으면 부품 판을 감춥니다.");

            Vector3 floorPoint = GridMath.CellToWorldCenter(FloorCell);
            floorPoint.y = 0f;
            yield return PointRightHandAt(floorPoint);
            Assert.IsFalse(builder.HasTarget, "메뉴가 열려 있는데 놓일 칸을 가리키고 있습니다.");

            yield return PullTrigger();
            Assert.AreEqual(before, world.Count, "메뉴가 열려 있는데 블록이 놓였습니다.");

            yield return Tap(Button(leftController, "menu"));
            yield return Frames(3);
            Assert.IsTrue(ui.Palette.IsShown, "메뉴를 닫으면 부품 판이 다시 보여야 합니다.");

            yield return PullTrigger();
            Assert.AreEqual(before + 1, world.Count, "메뉴를 닫은 뒤에는 다시 놓을 수 있어야 합니다.");
        }

        [UnityTest]
        public IEnumerator 부품_판은_왼손을_따라오고_컨트롤러가_없으면_보이지_않는다()
        {
            yield return LoadVrBuild();
            Canvas canvas = ui.Palette.GetComponentInChildren<Canvas>(true);

            var moved = new Vector3(-0.1f, 1.4f, 0.5f);
            Set(leftController.devicePosition, moved);
            yield return Frames(3);
            Assert.Less(Vector3.Distance(rig.Origin.TransformPoint(moved) + Vector3.up * ui.Palette.Lift, canvas.transform.position), 0.01f, "부품 판이 왼손을 따라오지 않았습니다.");

            // 날기를 켜면 부품 판의 표시도 바뀐다.
            yield return Tap(Button(rightController, "secondaryButton"));
            Assert.AreEqual(GameUi.FlyLabel, ui.Palette.ModeText);
            yield return Tap(Button(rightController, "secondaryButton"));
            Assert.AreEqual(GameUi.WalkLabel, ui.Palette.ModeText);

            InputSystem.RemoveDevice(leftController);
            yield return Frames(3);
            Assert.IsFalse(ui.Palette.IsShown, "왼손 컨트롤러가 없는데 부품 판이 보입니다.");
        }

        [UnityTest]
        public IEnumerator 실행_중에_PC_조작으로_바꾸면_마우스로_놓고_부품_판은_사라진다()
        {
            yield return LoadVrBuild();
            int before = world.Count;

            modeSwitch.SetMode(ControlMode.Desktop);
            yield return Frames(3);
            Assert.IsFalse(ui.Palette.IsShown);
            Assert.IsFalse(rig.PointerShown);

            yield return AimWithPart(45f, keyboard.digit1Key);
            Assert.IsTrue(builder.HasTarget, "PC 조작으로 돌아왔는데 화면 가운데로 조준되지 않습니다.");
            yield return Tap(mouse.leftButton);
            Assert.AreEqual(before + 1, world.Count, "PC 조작으로 돌아온 뒤 마우스로 블록이 놓이지 않았습니다.");

            modeSwitch.SetMode(ControlMode.Vr);
            yield return Frames(3);
            Assert.IsTrue(ui.Palette.IsShown, "다시 VR로 바꾸면 부품 판이 보여야 합니다.");
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadVrBuild();
            BeginCapture();

            // 쌓아 둔 블록 옆의 바닥을 가리키는 모습.
            world.History.Place(new Vector3Int(1, 0, -3), BluePart);
            world.History.Place(new Vector3Int(1, 1, -3), BluePart);
            Set(leftController.devicePosition, new Vector3(-0.2f, 1.08f, 0.5f));
            Set(headset.centerEyeRotation, Quaternion.Euler(22f, -4f, 0f));
            ui.Hotbar.Select(GoldPart);
            Vector3 floorPoint = GridMath.CellToWorldCenter(FloorCell);
            floorPoint.y = 0f;
            yield return PointRightHandAt(floorPoint);
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "vr-build.png"));

            // 글자를 확인할 수 있게 부품 판만 가까이 당겨 찍은 그림. 헤드셋에서 보이는 크기가 아니다.
            Canvas canvas = ui.Palette.GetComponentInChildren<Canvas>(true);
            Camera view = player.Rig.ViewCamera;
            float fieldOfView = view.fieldOfView;
            Vector3 toPalette = rig.Origin.InverseTransformPoint(canvas.transform.position) - new Vector3(0f, 1.6f, 0f);
            Set(headset.centerEyeRotation, Quaternion.LookRotation(toPalette));
            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Palette, "Slot3")));
            yield return Frames(3);
            view.fieldOfView = 34f;
            SaveCapture(Path.Combine(directory, "vr-palette-close.png"));
            view.fieldOfView = fieldOfView;
        }

        private IEnumerator LoadVrBuild()
        {
            yield return LoadVr();
            builder = player.GetComponent<BlockBuilder>();
            world = Object.FindAnyObjectByType<BlockWorld>();
            Assert.IsNotNull(builder);
            Assert.IsNotNull(world);

            Set(headset.centerEyePosition, new Vector3(0f, 1.6f, 0f));
            yield return RaiseLeftHand();
        }
    }
}
