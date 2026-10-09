using System.Collections;
using System.IO;
using AtelierVerse.Core;
using AtelierVerse.Player;
using AtelierVerse.UI;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 돌리기·옮기기·맞추기 도우미를 실제로 실행해 확인한다(15일차). 캐릭터는 (0.5, 0, -5.5)에서 +z 쪽을 보고 시작하므로,
    /// 아래로 45도를 보면 앞의 바닥을 가리키고, 그 자리에 블록을 놓으면 그 블록의 앞면을 가리킨다.
    /// 손으로 돌리고 옮기는 느낌이 자연스러운지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class ArrangePlayTests : PlayTestBase
    {
        private const int SceneBlockCount = 19;
        private const int GoldPart = 0;
        private const int BluePart = 1;

        // 45도 아래를 보고 놓은 블록이 놓이는 자리쯤(칸의 가운데가 아니다).
        private static readonly Vector3 FloorSpot = AimedFloorSpot;

        private BlockWorld world;
        private BlockBuilder builder;
        private NoticeBar notice;

        [UnityTest]
        public IEnumerator R로_돌리면_미리_보기와_놓인_블록이_15도씩_돌아간다()
        {
            yield return LoadArrangeScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            Assert.AreEqual(0f, builder.Yaw, 0.001f, "처음에는 돌리지 않은 상태여야 합니다.");
            Assert.AreEqual("돌리기 0°", ui.RotateText);

            yield return Tap(keyboard.rKey);
            Assert.AreEqual(15f, builder.Yaw, 0.001f, "R을 한 번 누르면 15도 돌아야 합니다.");
            yield return Tap(keyboard.rKey);
            Assert.AreEqual(30f, builder.Yaw, 0.001f);
            Assert.AreEqual("돌리기 30°", ui.RotateText);

            GameObject ghost = GameObject.Find("BlockGhost");
            Assert.IsNotNull(ghost, "놓일 자리의 미리 보기 블록이 보이지 않습니다.");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 30f, 0f), ghost.transform.rotation), 0.1f, "미리 보기가 돌린 방향으로 보이지 않습니다.");

            Vector3 target = builder.TargetPosition;
            yield return Tap(mouse.leftButton);

            Assert.IsTrue(BlockNear(world, target, out BlockRecord record, 0.002f), "돌린 블록이 놓이지 않았습니다.");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 30f, 0f), record.Rotation), 0.1f, "기록의 방향이 돌린 각도가 아닙니다.");
            PlacedBlock view = FindViewNear(world, target, 0.002f);
            Assert.IsNotNull(view);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 30f, 0f), view.transform.rotation), 0.1f, "화면의 블록이 돌아가 있지 않습니다.");
            Assert.AreEqual(30f, builder.Yaw, 0.001f, "놓은 뒤에도 돌린 각도는 그대로 남아야 다음 블록을 나란히 놓을 수 있습니다.");
        }

        [UnityTest]
        public IEnumerator T는_반대로_돌리고_0도에서_누르면_345도가_된다()
        {
            yield return LoadArrangeScene();
            yield return AimWithPart(45f, keyboard.digit1Key);

            yield return Tap(keyboard.tKey);
            Assert.AreEqual(345f, builder.Yaw, 0.001f);
            Assert.AreEqual("돌리기 345°", ui.RotateText);

            yield return Tap(keyboard.rKey);
            Assert.AreEqual(0f, builder.Yaw, 0.001f);
        }

        [UnityTest]
        public IEnumerator 돌리기_키를_누르고_있으면_이어서_돈다()
        {
            yield return LoadArrangeScene();
            yield return AimWithPart(45f, keyboard.digit1Key);

            Press(keyboard.rKey);
            yield return new WaitForSeconds(1f);
            Release(keyboard.rKey);
            yield return Frames(2);

            Assert.Greater(builder.Yaw, 30f, "누르고 있는데 한 번만 돌았습니다.");
            Assert.AreEqual(0f, Mathf.Repeat(builder.Yaw, PlacementMath.YawStep), 0.001f, "돌린 각도는 15도의 배수여야 합니다.");

            float held = builder.Yaw;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(held, builder.Yaw, 0.001f, "키를 뗐는데 계속 돕니다.");
        }

        [UnityTest]
        public IEnumerator 부품을_고르지_않았으면_돌리기_키가_듣지_않는다()
        {
            yield return LoadArrangeScene();
            player.CaptureLook(true);
            yield return Frames(2);

            yield return Tap(keyboard.rKey);
            yield return Tap(keyboard.cKey);

            Assert.AreEqual(0f, builder.Yaw, 0.001f);
            Assert.AreEqual(0, builder.SnapLevel);
        }

        [UnityTest]
        public IEnumerator 돌린_블록은_맵_문서로_내보냈다_다시_읽어도_방향이_같다()
        {
            yield return LoadArrangeScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            yield return Tap(keyboard.rKey);
            yield return Tap(keyboard.rKey);
            yield return Tap(keyboard.rKey);
            Vector3 target = builder.TargetPosition;
            yield return Tap(mouse.leftButton);

            MapDocument document = world.Export(null);
            MapBlock saved = document.blocks.Find(block => Vector3.Distance(block.position, target) < 0.002f);
            Assert.IsNotNull(saved, "내보낸 문서에 놓은 블록이 없습니다.");
            Assert.AreEqual(45f, saved.rotation.y, 0.01f, "문서의 방향이 돌린 각도가 아닙니다.");

            world.Import(document);
            yield return Frames(2);

            Assert.IsTrue(BlockNear(world, target, out BlockRecord record, 0.002f));
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 45f, 0f), record.Rotation), 0.1f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 45f, 0f), FindViewNear(world, target, 0.002f).transform.rotation), 0.1f);
        }

        [UnityTest]
        public IEnumerator G로_잡으면_블록이_숨고_미리_보기가_대신_보인다()
        {
            yield return LoadArrangeScene();
            yield return PlaceBlueAndSelectGold();
            Assert.IsTrue(BlockNear(world, FloorSpot, out BlockRecord placed));
            Assert.AreEqual(GameUi.GrabLabel, ui.GrabText);

            yield return Tap(keyboard.gKey);

            Assert.IsTrue(builder.IsCarrying, "G로 블록이 잡히지 않았습니다.");
            Assert.AreEqual(placed.Id, builder.CarriedBlockId);
            Assert.IsFalse(world.IsShown(placed.Id), "잡은 블록은 화면에서 잠시 감춰야 합니다.");
            Assert.AreEqual(SceneBlockCount + 1, world.Count, "잡는 것만으로 기록이 바뀌면 안 됩니다.");
            Assert.AreEqual(1, world.History.UndoCount);
            Assert.AreEqual(GameUi.CarryingLabel, ui.GrabText);
            Assert.IsTrue(notice.IsVisible);
            Assert.AreEqual(BlockBuilder.GrabbedMessage, notice.Message);

            // 감춘 블록은 조준에 걸리지 않으므로 그 뒤의 바닥을 가리키고, 미리 보기가 그 자리에 보인다.
            yield return Frames(2);
            Assert.IsTrue(builder.HasTarget);
            Assert.IsFalse(builder.HasBlockTarget, "잡은 블록이 조준에 걸렸습니다.");
            GameObject ghost = GameObject.Find("BlockGhost");
            Assert.IsNotNull(ghost);
            Assert.Less(Vector3.Distance(builder.TargetPosition, ghost.transform.position), 0.001f);
        }

        [UnityTest]
        public IEnumerator 잡은_블록을_다른_자리에_놓으면_옮겨지고_한_번에_되돌려진다()
        {
            yield return LoadArrangeScene();
            yield return PlaceBlueAndSelectGold();
            Assert.IsTrue(BlockNear(world, FloorSpot, out BlockRecord placed));
            Vector3 from = placed.Position;

            yield return Tap(keyboard.gKey);
            player.SetLook(25f, 45f);
            yield return Frames(3);
            Assert.IsTrue(builder.CanPlaceAtTarget);
            Vector3 to = builder.TargetPosition;
            Assert.Greater(Vector3.Distance(from, to), 0.5f, "다른 자리를 가리키고 있어야 합니다.");

            yield return Tap(mouse.leftButton);

            Assert.IsFalse(builder.IsCarrying, "놓은 뒤에는 잡고 있지 않아야 합니다.");
            Assert.AreEqual(SceneBlockCount + 1, world.Count, "옮기기는 블록 수를 바꾸지 않습니다.");
            Assert.IsTrue(world.TryGet(placed.Id, out BlockRecord moved), "옮긴 블록의 번호가 바뀌었습니다.");
            Assert.Less(Vector3.Distance(to, moved.Position), 0.002f, "가리킨 자리로 옮겨지지 않았습니다.");
            Assert.AreEqual(BluePart, moved.Part, "옮긴 블록의 부품은 고른 부품과 상관없이 그대로여야 합니다.");
            Assert.IsTrue(world.IsShown(placed.Id), "옮긴 블록이 다시 보이지 않습니다.");
            Assert.IsNotNull(FindViewNear(world, to, 0.002f), "화면의 블록이 새 자리에 없습니다.");
            Assert.IsFalse(HasBlockNear(world, from, 0.1f), "원래 자리에 블록이 남았습니다.");
            Assert.AreEqual(2, world.History.UndoCount, "옮기기는 한 번의 편집으로 기록되어야 합니다.");
            Assert.AreEqual(GameUi.GrabLabel, ui.GrabText);

            yield return TapWithCtrl(keyboard.zKey);
            Assert.IsTrue(world.TryGet(placed.Id, out BlockRecord restored));
            Assert.Less(Vector3.Distance(from, restored.Position), 0.002f, "되돌리기로 원래 자리에 돌아오지 않았습니다.");
            Assert.IsNotNull(FindViewNear(world, from, 0.002f));

            yield return TapWithCtrl(keyboard.yKey);
            Assert.IsTrue(world.TryGet(placed.Id, out BlockRecord again));
            Assert.Less(Vector3.Distance(to, again.Position), 0.002f);
        }

        [UnityTest]
        public IEnumerator 잡은_채_돌리면_옮긴_블록의_방향이_바뀐다()
        {
            yield return LoadArrangeScene();
            yield return PlaceBlueAndSelectGold();
            Assert.IsTrue(BlockNear(world, FloorSpot, out BlockRecord placed));

            yield return Tap(keyboard.gKey);
            yield return Tap(keyboard.rKey);
            yield return Tap(keyboard.rKey);
            yield return Tap(mouse.leftButton);

            // 잡은 블록이 숨으면 그 뒤의 같은 바닥을 가리키므로 자리는 그대로이고 방향만 바뀐다.
            Assert.IsTrue(world.TryGet(placed.Id, out BlockRecord turned));
            Assert.Less(Vector3.Distance(placed.Position, turned.Position), 0.01f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 30f, 0f), turned.Rotation), 0.1f, "잡은 채 돌린 각도로 놓이지 않았습니다.");
            Assert.AreEqual(2, world.History.UndoCount);
        }

        [UnityTest]
        public IEnumerator 돌아간_블록을_잡으면_그_블록의_각도를_이어받는다()
        {
            yield return LoadArrangeScene();
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(BluePart, FloorSpot, Quaternion.Euler(0f, 60f, 0f), out int id));
            yield return new WaitForFixedUpdate();
            yield return Frames(2);

            yield return AimWithPart(45f, keyboard.digit1Key);
            Assert.IsTrue(builder.HasBlockTarget, "놓아 둔 블록을 가리키고 있어야 합니다.");
            Assert.AreEqual(0f, builder.Yaw, 0.001f);

            yield return Tap(keyboard.gKey);
            Assert.AreEqual(id, builder.CarriedBlockId);
            Assert.AreEqual(60f, builder.Yaw, 0.01f, "잡은 블록의 각도가 돌리기의 각도가 되어야 합니다.");

            yield return Tap(keyboard.gKey);
            Assert.AreEqual(60f, builder.Yaw, 0.01f, "잡았다 놓으면 그 각도가 남아, 다음 블록을 같은 방향으로 놓을 수 있습니다.");
        }

        [UnityTest]
        public IEnumerator 잡은_채_G나_오른쪽을_누르면_지워지지_않고_제자리로_돌아온다()
        {
            yield return LoadArrangeScene();
            yield return PlaceBlueAndSelectGold();
            Assert.IsTrue(BlockNear(world, FloorSpot, out BlockRecord placed));

            yield return Tap(keyboard.gKey);
            Assert.IsTrue(builder.IsCarrying);
            yield return Tap(keyboard.gKey);

            Assert.IsFalse(builder.IsCarrying);
            Assert.IsTrue(world.IsShown(placed.Id));
            Assert.AreEqual(BlockBuilder.GrabCancelledMessage, notice.Message);
            Assert.AreEqual(1, world.History.UndoCount, "그만둔 옮기기는 기록하지 않습니다.");

            yield return Frames(2);
            yield return Tap(keyboard.gKey);
            Assert.IsTrue(builder.IsCarrying);
            yield return Tap(mouse.rightButton);

            Assert.IsFalse(builder.IsCarrying);
            Assert.IsTrue(world.TryGet(placed.Id, out BlockRecord kept), "잡은 채 오른쪽을 눌렀더니 블록이 지워졌습니다.");
            Assert.Less(Vector3.Distance(placed.Position, kept.Position), 0.001f);
            Assert.IsTrue(world.IsShown(placed.Id));
            Assert.AreEqual(SceneBlockCount + 1, world.Count);
        }

        [UnityTest]
        public IEnumerator 블록이_없는_곳에서_G를_누르면_알림이_나온다()
        {
            yield return LoadArrangeScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            Assert.IsFalse(builder.HasBlockTarget);

            yield return Tap(keyboard.gKey);

            Assert.IsFalse(builder.IsCarrying);
            Assert.IsTrue(notice.IsVisible);
            Assert.AreEqual(BlockBuilder.GrabTargetMessage, notice.Message);
        }

        [UnityTest]
        public IEnumerator 메뉴를_열면_잡은_블록이_제자리로_돌아온다()
        {
            yield return LoadArrangeScene();
            yield return PlaceBlueAndSelectGold();
            Assert.IsTrue(BlockNear(world, FloorSpot, out BlockRecord placed));
            yield return Tap(keyboard.gKey);
            Assert.IsFalse(world.IsShown(placed.Id));

            yield return Tap(keyboard.escapeKey);
            yield return Frames(2);

            Assert.IsTrue(ui.IsMenuOpen);
            Assert.IsFalse(builder.IsCarrying, "메뉴가 열렸는데 블록을 계속 잡고 있습니다.");
            Assert.IsTrue(world.IsShown(placed.Id), "메뉴를 열면 감췄던 블록이 다시 보여야 합니다.");
            Assert.AreEqual(1, world.History.UndoCount);
        }

        [UnityTest]
        public IEnumerator 잡고_있는_블록이_되돌리기로_사라지면_잡은_것이_풀린다()
        {
            yield return LoadArrangeScene();
            yield return PlaceBlueAndSelectGold();
            Assert.IsTrue(BlockNear(world, FloorSpot, out BlockRecord placed));
            yield return Tap(keyboard.gKey);
            Assert.IsTrue(builder.IsCarrying);

            // 놓은 것을 되돌리면 잡고 있던 블록이 없어진다.
            yield return TapWithCtrl(keyboard.zKey);
            yield return Frames(2);

            Assert.IsFalse(builder.IsCarrying);
            Assert.AreEqual(SceneBlockCount, world.Count);

            yield return TapWithCtrl(keyboard.yKey);
            yield return Frames(2);
            Assert.IsTrue(world.Has(placed.Id));
            Assert.IsTrue(world.IsShown(placed.Id), "다시 실행으로 돌아온 블록이 보이지 않습니다.");
        }

        [UnityTest]
        public IEnumerator 블록이_상한에_닿아도_옮길_수_있다()
        {
            yield return LoadArrangeScene();
            yield return PlaceBlueAndSelectGold();
            Assert.IsTrue(BlockNear(world, FloorSpot, out BlockRecord placed));

            // 높은 곳에 블록을 채워 상한에 닿게 한다.
            int index = 0;
            while (world.Count < world.MaxBlocks)
            {
                var spot = new Vector3(-7.5f + index % 16, 9.5f + index / 256, -7.5f + index / 16 % 16);
                Assert.AreEqual(PlaceResult.Ok, world.Add(GoldPart, spot), $"{index}번째 블록을 채우지 못했습니다.");
                index++;
            }

            yield return Frames(3);
            Assert.AreEqual(BlockedReason.Full, builder.Blocked, "상한에 닿으면 새로 놓을 수는 없습니다.");

            yield return Tap(keyboard.gKey);
            player.SetLook(25f, 45f);
            yield return Frames(3);
            Assert.AreEqual(BlockedReason.None, builder.Blocked, "옮기기는 블록 수가 늘지 않으므로 상한에 걸리지 않아야 합니다.");
            Vector3 to = builder.TargetPosition;
            yield return Tap(mouse.leftButton);

            Assert.IsTrue(world.TryGet(placed.Id, out BlockRecord moved));
            Assert.Less(Vector3.Distance(to, moved.Position), 0.002f);
            Assert.AreEqual(world.MaxBlocks, world.Count);
        }

        [UnityTest]
        public IEnumerator C로_맞추기_단계가_바뀌고_알림과_표시가_따라온다()
        {
            yield return LoadArrangeScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            Assert.AreEqual(0, builder.SnapLevel, "맞추기 도우미는 끈 것이 기본입니다.");
            Assert.AreEqual("맞추기 끔", ui.SnapText);

            yield return Tap(keyboard.cKey);
            Assert.AreEqual(1, builder.SnapLevel);
            Assert.AreEqual(1f, builder.SnapStep);
            Assert.AreEqual("맞추기 1칸", ui.SnapText);
            Assert.AreEqual(1, GameSettings.SnapLevel, "맞추기 단계는 이 기기에 저장됩니다.");
            Assert.IsTrue(notice.IsVisible);
            Assert.AreEqual(BlockBuilder.SnapMessage(1), notice.Message);

            yield return Tap(keyboard.cKey);
            Assert.AreEqual("맞추기 1/2칸", ui.SnapText);
            yield return Tap(keyboard.cKey);
            Assert.AreEqual("맞추기 1/4칸", ui.SnapText);
            Assert.AreEqual(0.25f, builder.SnapStep);

            yield return Tap(keyboard.cKey);
            Assert.AreEqual(0, builder.SnapLevel);
            Assert.AreEqual("맞추기 끔", ui.SnapText);
            Assert.AreEqual(BlockBuilder.SnapMessage(0), notice.Message);
        }

        [UnityTest]
        public IEnumerator 맞추기를_켜면_바닥에서는_모눈에_맞춰_놓인다()
        {
            yield return LoadArrangeScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            Vector3 free = builder.TargetPosition;

            yield return Tap(keyboard.cKey);
            yield return Frames(2);
            Vector3 snapped = builder.TargetPosition;

            var expected = new Vector3(PlacementMath.SnapCenter(free.x, 1f), 0.5f, PlacementMath.SnapCenter(free.z, 1f));
            Assert.Less(Vector3.Distance(expected, snapped), 0.002f, "칸의 가운데로 당겨지지 않았습니다.");
            Assert.AreEqual(0.5f, Mathf.Repeat(snapped.x, 1f), 0.002f);
            Assert.AreEqual(0.5f, Mathf.Repeat(snapped.z, 1f), 0.002f);

            GameObject ghost = GameObject.Find("BlockGhost");
            Assert.Less(Vector3.Distance(snapped, ghost.transform.position), 0.001f, "미리 보기가 맞춘 자리에 있지 않습니다.");

            yield return Tap(mouse.leftButton);
            Assert.IsTrue(BlockNear(world, snapped, out _, 0.002f), "맞춘 자리에 놓이지 않았습니다.");

            // 반 칸: 0.5의 배수 자리.
            player.SetLook(25f, 45f);
            yield return Tap(keyboard.cKey);
            yield return Frames(3);
            Vector3 half = builder.TargetPosition;
            Assert.AreEqual(0f, Mathf.Abs(Mathf.DeltaAngle(0f, half.x * 720f)), 0.8f, "반 칸에 맞지 않았습니다.");
            Assert.AreEqual(0f, Mathf.Abs(Mathf.DeltaAngle(0f, half.z * 720f)), 0.8f, "반 칸에 맞지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 맞추기를_켜고_블록의_면을_가리키면_그_블록에_나란히_붙는다()
        {
            yield return LoadArrangeScene();

            // 칸에 맞지 않게 놓인 블록. 낮은 각도로 보면 앞면을 가리킨다.
            var block = new Vector3(0.37f, 0.5f, -2.9f);
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(BluePart, block));
            yield return new WaitForFixedUpdate();
            yield return Frames(2);

            builder.SetSnapLevel(1);
            yield return AimWithPart(30f, keyboard.digit1Key);
            Assert.IsTrue(builder.HasBlockTarget, "놓아 둔 블록의 앞면을 가리키고 있어야 합니다.");

            Vector3 expected = block + Vector3.back;
            Assert.Less(Vector3.Distance(expected, builder.TargetPosition), 0.002f, "가리킨 면 쪽으로 정확히 한 변 떨어진 자리가 아닙니다.");
            Assert.IsTrue(builder.CanPlaceAtTarget);

            yield return Tap(mouse.leftButton);
            Assert.IsTrue(BlockNear(world, expected, out _, 0.002f), "블록이 나란히 붙어 놓이지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 돌아간_블록에는_돌아간_방향을_따라_붙는다()
        {
            yield return LoadArrangeScene();

            var block = new Vector3(0.37f, 0.5f, -2.9f);
            Quaternion rotation = Quaternion.Euler(0f, 30f, 0f);
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(BluePart, block, rotation, out _));
            yield return new WaitForFixedUpdate();
            yield return Frames(2);

            builder.SetSnapLevel(1);
            yield return AimWithPart(30f, keyboard.digit1Key);
            Assert.IsTrue(builder.HasBlockTarget);

            // 어느 면을 가리켰든, 그 블록이 돌아간 방향의 한 축으로 정확히 한 변 떨어진 자리여야 한다.
            Vector3 local = Quaternion.Inverse(rotation) * (builder.TargetPosition - block);
            Assert.AreEqual(1f, local.magnitude, 0.003f, "가리킨 블록에서 한 변 떨어진 자리가 아닙니다.");
            float along = Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
            Assert.AreEqual(1f, along, 0.003f, "돌아간 블록의 면을 따라 붙지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 맞추기를_꺼_두면_가리킨_그_자리에_놓인다()
        {
            yield return LoadArrangeScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            Vector3 free = builder.TargetPosition;

            yield return Tap(keyboard.cKey);
            yield return Frames(2);
            Assert.Greater(Vector3.Distance(free, builder.TargetPosition), 0.01f, "이 자리는 칸의 가운데가 아니므로 켜면 당겨져야 합니다.");

            builder.SetSnapLevel(0);
            yield return Frames(2);
            Assert.Less(Vector3.Distance(free, builder.TargetPosition), 0.002f, "끄면 다시 가리킨 그 자리여야 합니다.");
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadArrangeScene();
            BeginCapture();

            // 한눈에 들어오게 두 걸음 물러선다.
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.position = new Vector3(0.5f, 0f, -7.5f);
            body.enabled = true;

            // 모눈에 맞춰 세운 반듯한 벽과, 30도 돌린 채 나란히 붙인 블록 줄.
            for (int x = -3; x <= -1; x++)
            {
                world.Add(BluePart, CellCenter(x, 0, -3));
                world.Add(BluePart, CellCenter(x, 1, -3));
            }

            Quaternion turned = Quaternion.Euler(0f, 30f, 0f);
            var start = new Vector3(0.9f, 0.5f, -3.4f);
            for (int i = 0; i < 3; i++)
            {
                world.Add(2, start + turned * Vector3.forward * i, turned, out _);
            }

            yield return new WaitForFixedUpdate();
            yield return Frames(2);

            // 맞추기 1칸, 30도 돌린 채 돌아간 블록 줄의 앞쪽 끝을 가리킨 모습. 미리 보기가 줄에 나란히 붙는다.
            builder.SetSnapLevel(1);
            player.CaptureLook(true);
            player.SetLook(2.3f, 16f);
            yield return Tap(keyboard.digit1Key);
            yield return Tap(keyboard.rKey);
            yield return Tap(keyboard.rKey);
            yield return Frames(3);
            Assert.IsTrue(builder.HasBlockTarget, "돌려 놓은 블록 줄의 끝을 가리키고 있어야 합니다.");
            SaveCapture(Path.Combine(directory, "arrange-snap.png"));

            // 벽의 위쪽 블록 하나를 잡아 앞의 바닥으로 옮기는 모습.
            player.SetLook(-24f, 0.6f);
            yield return Frames(3);
            Assert.IsTrue(builder.HasBlockTarget, "벽의 블록을 가리키고 있어야 합니다.");
            yield return Tap(keyboard.gKey);
            player.SetLook(-18f, 26f);
            yield return Frames(3);
            Assert.IsTrue(builder.IsCarrying);

            SaveCapture(Path.Combine(directory, "arrange-carry.png"));
        }

        private IEnumerator LoadArrangeScene()
        {
            yield return LoadSandbox();

            world = Object.FindAnyObjectByType<BlockWorld>();
            builder = Object.FindAnyObjectByType<BlockBuilder>();
            notice = Object.FindAnyObjectByType<NoticeBar>();
            Assert.IsNotNull(world, "Sandbox 씬에서 블록 세계를 찾을 수 없습니다.");
            Assert.IsNotNull(builder, "PC 캐릭터에 블록 놓기가 없습니다.");
            Assert.IsNotNull(notice, "게임 화면에 알림 띠가 없습니다.");
            yield return Frames(2);
        }

        /// <summary>바닥에 블루 블록을 놓은 뒤 골드 부품을 고른다. 조준은 그 블록의 앞면에 남는다.</summary>
        private IEnumerator PlaceBlueAndSelectGold()
        {
            yield return AimWithPart(45f, keyboard.digit2Key);
            yield return Tap(mouse.leftButton);
            Assert.IsTrue(TryGetPartNear(world, FloorSpot, out int part));
            Assert.AreEqual(BluePart, part);

            yield return Tap(keyboard.digit1Key);
            yield return Frames(2);
            Assert.AreEqual(GoldPart, ui.Hotbar.SelectedIndex);
            Assert.IsTrue(builder.HasBlockTarget, "놓은 블록을 가리키고 있어야 합니다.");
        }
    }
}
