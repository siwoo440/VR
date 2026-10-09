using System.Collections;
using System.IO;
using AtelierVerse.Player;
using AtelierVerse.UI;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// VR에서 돌리기·옮기기·맞추기를 가상 기기로 실행해 확인한다(15일차): 왼손 첫째 단추와 오른쪽 스틱 누르기로 돌리고,
    /// 왼손 옆 단추로 잡아 오른손 방아쇠로 놓고, 부품 판의 단추로 맞추기 단계를 바꾼다.
    /// 단추의 자리가 실제 컨트롤러에서 손에 맞는지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class XrArrangePlayTests : XrPlayTestBase
    {
        private const int GoldPart = 0;
        private const int BluePart = 1;

        // 캐릭터가 처음 서는 자리(0.5, 0, -5.5)의 3m 앞 빈 바닥에 블록을 얹었을 때의 가운데 자리.
        private static readonly Vector3 FloorSpot = new Vector3(0.5f, 0.5f, -2.5f);

        // 칸에 맞지 않는 바닥의 한 점.
        private static readonly Vector3 OffGridPoint = new Vector3(0.83f, 0f, -2.37f);

        private BlockWorld world;
        private BlockBuilder builder;

        [UnityTest]
        public IEnumerator 왼손_첫째_단추로_돌리고_오른쪽_스틱_누르기로_반대로_돌린다()
        {
            yield return LoadVrBuild();
            ui.Hotbar.Select(GoldPart);
            yield return PointRightHandAt(FloorPoint(FloorSpot));
            Assert.IsTrue(builder.HasTarget);
            Assert.AreEqual(0f, builder.Yaw, 0.001f);

            yield return Tap(Button(leftController, "primaryButton"));
            Assert.AreEqual(15f, builder.Yaw, 0.001f, "왼손 첫째 단추로 15도 돌지 않았습니다.");
            yield return Tap(Button(leftController, "primaryButton"));
            Assert.AreEqual(30f, builder.Yaw, 0.001f);

            GameObject ghost = GameObject.Find("BlockGhost");
            Assert.IsNotNull(ghost);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 30f, 0f), ghost.transform.rotation), 0.1f);

            yield return Tap(Button(rightController, "thumbstickClicked"));
            Assert.AreEqual(15f, builder.Yaw, 0.001f, "오른쪽 스틱 누르기로 반대로 돌지 않았습니다.");
            Assert.IsFalse(motor.IsFlying, "돌리기 단추가 날기를 켰습니다.");

            yield return PullTrigger();
            Assert.IsTrue(BlockNear(world, FloorSpot, out BlockRecord record, 0.01f), "돌린 블록이 놓이지 않았습니다.");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 15f, 0f), record.Rotation), 0.1f);
        }

        [UnityTest]
        public IEnumerator 왼손_옆_단추로_잡아_오른손_방아쇠로_옮긴다()
        {
            yield return LoadVrBuild();
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(BluePart, FloorSpot, Quaternion.identity, out int id));
            // 물리 갱신을 기다린 바로 그 시점에는 가상 기기에 값을 넣을 수 없으므로 프레임을 더 넘긴다.
            yield return new WaitForFixedUpdate();
            yield return Frames(2);
            int before = world.Count;

            ui.Hotbar.Select(GoldPart);
            yield return PointRightHandAt(FloorSpot);
            Assert.IsTrue(builder.HasBlockTarget, "가리킨 블록을 찾지 못했습니다.");

            yield return Tap(Button(leftController, "gripPressed"));
            Assert.IsTrue(builder.IsCarrying, "왼손 옆 단추로 블록이 잡히지 않았습니다.");
            Assert.AreEqual(id, builder.CarriedBlockId);
            Assert.IsFalse(world.IsShown(id));

            var destination = new Vector3(1.9f, 0f, -2.2f);
            yield return PointRightHandAt(destination);
            Vector3 expected = destination + Vector3.up * 0.5f;
            Assert.Less(Vector3.Distance(expected, builder.TargetPosition), 0.01f);

            yield return PullTrigger();

            Assert.IsFalse(builder.IsCarrying);
            Assert.AreEqual(before, world.Count, "옮기기가 블록 수를 바꿨습니다.");
            Assert.IsTrue(world.TryGet(id, out BlockRecord moved));
            Assert.Less(Vector3.Distance(expected, moved.Position), 0.01f, "가리킨 자리로 옮겨지지 않았습니다.");
            Assert.AreEqual(BluePart, moved.Part);
            Assert.IsTrue(world.IsShown(id));
            Assert.AreEqual(2, world.History.UndoCount);

            // 부품 판의 되돌리기 단추로 원래 자리에 돌아온다.
            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Palette, "Undo")));
            yield return PullTrigger();
            Assert.IsTrue(world.TryGet(id, out BlockRecord restored));
            Assert.Less(Vector3.Distance(FloorSpot, restored.Position), 0.01f, "되돌리기로 원래 자리에 돌아오지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 잡은_채_오른손_옆_단추를_쥐면_지워지지_않고_제자리로_돌아온다()
        {
            yield return LoadVrBuild();
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(BluePart, FloorSpot, Quaternion.identity, out int id));
            yield return new WaitForFixedUpdate();
            yield return Frames(2);
            int before = world.Count;

            ui.Hotbar.Select(GoldPart);
            yield return PointRightHandAt(FloorSpot);
            yield return Tap(Button(leftController, "gripPressed"));
            Assert.IsTrue(builder.IsCarrying);

            yield return SqueezeGrip();

            Assert.IsFalse(builder.IsCarrying);
            Assert.AreEqual(before, world.Count, "잡은 채 지우기 단추를 쥐었더니 블록이 지워졌습니다.");
            Assert.IsTrue(world.IsShown(id));
        }

        [UnityTest]
        public IEnumerator 부품_판의_맞추기_단추를_가리켜_누르면_단계가_바뀌고_모눈에_맞춰_놓인다()
        {
            yield return LoadVrBuild();
            int before = world.Count;
            Button snap = Find<Button>(ui.Palette, "Snap");
            Assert.AreEqual("맞추기 끔", ui.Palette.SnapText);

            yield return PointRightHandAt(CenterOf(snap));
            Assert.IsTrue(ui.Player.IsPointingAtUi, "부품 판의 단추를 가리키고 있는데 화면을 가리킨다고 알지 못합니다.");
            yield return PullTrigger();

            Assert.AreEqual(1, builder.SnapLevel, "부품 판의 맞추기 단추가 단계를 바꾸지 않았습니다.");
            Assert.AreEqual("맞추기 1칸", ui.Palette.SnapText);
            Assert.AreEqual(before, world.Count, "부품 판을 누른 방아쇠로 블록이 놓였습니다.");

            ui.Hotbar.Select(BluePart);
            yield return PointRightHandAt(OffGridPoint);
            var expected = new Vector3(0.5f, 0.5f, -2.5f);
            Assert.Less(Vector3.Distance(expected, builder.TargetPosition), 0.002f, "칸의 가운데로 당겨지지 않았습니다.");

            yield return PullTrigger();
            Assert.IsTrue(BlockNear(world, expected, out _, 0.002f));

            // 세 번 더 누르면 다시 끔이다.
            yield return PointRightHandAt(CenterOf(snap));
            yield return PullTrigger();
            Assert.AreEqual("맞추기 1/2칸", ui.Palette.SnapText);
            yield return PullTrigger();
            yield return PullTrigger();
            Assert.AreEqual(0, builder.SnapLevel);
            Assert.AreEqual("맞추기 끔", ui.Palette.SnapText);
        }

        [UnityTest]
        public IEnumerator 맞추기를_켜고_블록의_윗면을_가리키면_바로_위에_쌓인다()
        {
            yield return LoadVrBuild();
            Vector3 block = OffGridPoint + Vector3.up * 0.5f;
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(BluePart, block));
            yield return new WaitForFixedUpdate();
            yield return Frames(2);

            builder.SetSnapLevel(1);
            ui.Hotbar.Select(GoldPart);
            yield return PointRightHandAt(new Vector3(0.9f, 1f, -2.3f));
            Assert.IsTrue(builder.HasBlockTarget, "블록의 윗면을 가리키고 있어야 합니다.");

            Assert.Less(Vector3.Distance(block + Vector3.up, builder.TargetPosition), 0.002f, "가리킨 블록의 바로 위가 아닙니다.");

            yield return PullTrigger();
            Assert.IsTrue(BlockNear(world, block + Vector3.up, out _, 0.002f));
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadVrBuild();
            BeginCapture();

            // 30도 돌려 나란히 붙여 놓은 블록 줄의 끝을, 맞추기를 켠 채 가리킨 모습. 미리 보기가 줄을 잇는다.
            Quaternion turned = Quaternion.Euler(0f, 30f, 0f);
            var start = new Vector3(-0.6f, 0.5f, -1.6f);
            world.Add(BluePart, start, turned, out _);
            world.Add(BluePart, start + turned * Vector3.right, turned, out _);
            yield return new WaitForFixedUpdate();
            yield return Frames(2);

            builder.SetSnapLevel(1);
            Set(leftController.devicePosition, new Vector3(-0.2f, 1.08f, 0.5f));
            Set(headset.centerEyeRotation, Quaternion.Euler(22f, -4f, 0f));
            ui.Hotbar.Select(GoldPart);
            yield return Tap(Button(leftController, "primaryButton"));
            yield return Tap(Button(leftController, "primaryButton"));
            yield return PointRightHandAt(start + turned * new Vector3(1.5f, 0f, 0f));
            Assert.IsTrue(builder.HasBlockTarget, "돌려 놓은 블록 줄의 끝을 가리키고 있어야 합니다.");
            Assert.Less(Vector3.Distance(start + turned * Vector3.right * 2f, builder.TargetPosition), 0.01f, "미리 보기가 줄에 나란히 붙지 않았습니다.");
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "vr-arrange.png"));
        }

        private static Vector3 FloorPoint(Vector3 blockCenter)
        {
            blockCenter.y = 0f;
            return blockCenter;
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
