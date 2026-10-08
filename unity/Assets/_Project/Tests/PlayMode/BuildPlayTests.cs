using System.Collections;
using System.IO;
using AtelierVerse.Player;
using AtelierVerse.UI;
using AtelierVerse.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 블록 놓기를 실제로 실행해 확인한다. 캐릭터는 (0.5, 0, -5.5)에서 +z 쪽을 보고 시작하므로,
    /// 아래로 45도를 보면 바닥의 (0, 0, -4) 칸을 가리킨다.
    /// </summary>
    public class BuildPlayTests : PlayTestBase
    {
        private const int SceneBlockCount = 19;
        private const int BluePart = 1;

        // 45도 아래를 보고 놓은 블록이 놓이는 자리쯤(칸의 가운데가 아니다).
        private static readonly Vector3 FloorSpot = AimedFloorSpot;

        private BlockWorld world;
        private BlockBuilder builder;

        [UnityTest]
        public IEnumerator 씬에_미리_놓인_블록이_블록_세계에_올라_있다()
        {
            yield return LoadBuildScene();

            Assert.AreEqual(SceneBlockCount, world.Count);
            Assert.IsTrue(TryGetPartNear(world, CellCenter(2, 0, 2), out int housePart), "집의 블록이 기록에 없습니다.");
            Assert.AreEqual("block.gold", world.Catalog.Get(housePart).id);
            Assert.IsTrue(TryGetPartNear(world, CellCenter(-5, 0, 1), out int stagePart), "단의 블록이 기록에 없습니다.");
            Assert.AreEqual("block.blue", world.Catalog.Get(stagePart).id);
        }

        [UnityTest]
        public IEnumerator 부품_칸의_이름은_부품_목록과_같다()
        {
            yield return LoadBuildScene();

            PartCatalog catalog = world.Catalog;
            Assert.AreEqual(6, catalog.Count);
            for (int i = 0; i < catalog.Count; i++)
            {
                Assert.AreEqual(catalog.Get(i).displayName, ui.Hotbar.GetItemName(i));
            }
        }

        [UnityTest]
        public IEnumerator 부품을_고르고_왼쪽을_누르면_조준한_자리에_블록이_놓인다()
        {
            yield return LoadBuildScene();
            yield return AimWithPart(45f, keyboard.digit2Key);

            Assert.IsTrue(builder.HasTarget, "조준한 곳에 놓일 자리가 없습니다.");
            Vector3 target = builder.TargetPosition;
            Assert.Less(Vector3.Distance(FloorSpot, target), 0.3f, "45도 아래로 보이는 바닥의 자리가 아닙니다.");
            Assert.AreEqual(0.5f, target.y, 0.001f, "바닥 위에 얹은 블록의 가운데 높이는 반 변(0.5)이어야 합니다.");
            Assert.IsTrue(builder.CanPlaceAtTarget);

            yield return Tap(mouse.leftButton);

            Assert.AreEqual(SceneBlockCount + 1, world.Count);
            Assert.IsTrue(BlockNear(world, target, out BlockRecord record, 0.002f), "가리킨 바로 그 자리에 놓이지 않았습니다.");
            Assert.AreEqual(BluePart, record.Part);

            PlacedBlock block = FindViewNear(world, target, 0.002f);
            Assert.IsNotNull(block, "기록에는 있는데 화면에 블록이 없습니다.");
            Assert.AreEqual(record.Id, block.Id, "화면의 블록이 기록의 번호를 알고 있어야 합니다.");
            Assert.AreEqual(world.Catalog.Get(BluePart).material, block.GetComponent<Renderer>().sharedMaterial);
        }

        [UnityTest]
        public IEnumerator 칸에_맞지_않는_자리를_가리켜도_그_자리에_놓인다()
        {
            yield return LoadBuildScene();
            player.CaptureLook(true);
            player.SetLook(20f, 40f);
            yield return Tap(keyboard.digit1Key);
            yield return Frames(2);

            Assert.IsTrue(builder.HasTarget);
            Vector3 target = builder.TargetPosition;
            Vector3 cellCenter = GridMath.SnapToCellCenter(target);
            Assert.Greater(Mathf.Abs(target.x - cellCenter.x) + Mathf.Abs(target.z - cellCenter.z), 0.05f, "가리킨 자리가 칸의 가운데로 당겨졌습니다.");
            Assert.AreEqual(0.5f, target.y, 0.001f);

            yield return Tap(mouse.leftButton);

            Assert.IsTrue(BlockNear(world, target, out _, 0.002f), "칸에 맞지 않는 자리에 놓이지 않았습니다.");
            Assert.AreEqual(SceneBlockCount + 1, world.Count);
        }

        [UnityTest]
        public IEnumerator 블록끼리는_겹쳐_놓을_수_있다()
        {
            yield return LoadBuildScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            Vector3 first = builder.TargetPosition;
            yield return Tap(mouse.leftButton);

            // 오른쪽으로 35도 돌려 첫 블록 바로 옆의 바닥을 가리킨다. 새 블록은 첫 블록과 조금 겹친다.
            player.SetLook(35f, 45f);
            yield return Frames(3);
            Vector3 second = builder.TargetPosition;
            Assert.Less(Mathf.Abs(second.x - first.x), 1f, "두 블록이 옆으로 겹치는 자리가 아닙니다.");
            Assert.Less(Mathf.Abs(second.z - first.z), 1f);
            Assert.Greater(Vector3.Distance(first, second), 0.2f);
            Assert.AreEqual(BlockedReason.None, builder.Blocked, "다른 블록과 겹친다는 까닭으로 막으면 안 됩니다.");

            yield return Tap(mouse.leftButton);

            Assert.AreEqual(SceneBlockCount + 2, world.Count);
            Assert.IsTrue(BlockNear(world, second, out _, 0.002f));
        }

        [UnityTest]
        public IEnumerator 블록의_옆면_아래쪽을_가리켜도_바닥에_묻히지_않는다()
        {
            yield return LoadBuildScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            yield return Tap(mouse.leftButton);

            // 방금 놓은 블록의 앞면을 바닥에서 0.2쯤 되는 낮은 곳으로 가리킨다.
            player.SetLook(0f, 52f);
            yield return Frames(3);

            Assert.IsTrue(builder.HasBlockTarget, "놓은 블록의 앞면을 가리키고 있어야 합니다.");
            Assert.AreEqual(0.5f, builder.TargetPosition.y, 0.001f, "낮은 곳을 가리켜도 블록은 바닥 위에 놓여야 합니다.");
        }

        [UnityTest]
        public IEnumerator 오른쪽을_누르면_조준한_블록이_지워진다()
        {
            yield return LoadBuildScene();
            yield return AimWithPart(45f, keyboard.digit2Key);
            yield return Tap(mouse.leftButton);
            Assert.IsTrue(HasBlockNear(world, FloorSpot));

            yield return Tap(mouse.rightButton);

            Assert.IsFalse(HasBlockNear(world, FloorSpot));
            Assert.AreEqual(SceneBlockCount, world.Count);
            yield return null;
            Assert.IsNull(FindViewNear(world, FloorSpot), "기록에서는 지워졌는데 화면에 블록이 남아 있습니다.");
        }

        [UnityTest]
        public IEnumerator 놓인_블록의_윗면을_가리키면_그_위에_쌓인다()
        {
            yield return LoadBuildScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            yield return Tap(mouse.leftButton);

            // 머리 높이 1.55에서 19.5도 아래를 보면 방금 놓은 블록의 윗면 가운데쯤에 닿는다.
            player.SetLook(0f, 19.5f);
            yield return Frames(2);
            Vector3 target = builder.TargetPosition;
            Assert.AreEqual(1.5f, target.y, 0.001f, "윗면을 가리키면 그 위에 얹혀야 합니다.");
            Assert.Less(Vector3.Distance(FloorSpot + Vector3.up, target), 0.4f);

            yield return Tap(mouse.leftButton);
            Assert.IsTrue(BlockNear(world, target, out _, 0.002f));
        }

        [UnityTest]
        public IEnumerator 부품을_고르지_않으면_놓이지_않는다()
        {
            yield return LoadBuildScene();
            player.CaptureLook(true);
            player.SetLook(0f, 45f);
            yield return Frames(3);

            Assert.IsFalse(builder.HasTarget);
            yield return Tap(mouse.leftButton);
            Assert.AreEqual(SceneBlockCount, world.Count);
        }

        [UnityTest]
        public IEnumerator 마우스를_잡는_첫_누름으로는_놓이지_않는다()
        {
            yield return LoadBuildScene();
            player.SetLook(0f, 45f);
            yield return Tap(keyboard.digit3Key);
            Assert.IsFalse(player.LookCaptured);

            yield return Tap(mouse.leftButton);
            Assert.IsTrue(player.LookCaptured);
            Assert.AreEqual(SceneBlockCount, world.Count, "마우스를 잡는 누름에 블록이 놓였습니다.");

            yield return Tap(mouse.leftButton);
            Assert.AreEqual(SceneBlockCount + 1, world.Count);
        }

        [UnityTest]
        public IEnumerator 캐릭터가_서_있는_자리에는_놓이지_않는다()
        {
            yield return LoadBuildScene();
            yield return AimWithPart(80f, keyboard.digit1Key);

            Assert.IsTrue(builder.HasTarget);
            Assert.AreEqual(BlockedReason.Overlap, builder.Blocked);
            Assert.IsFalse(builder.CanPlaceAtTarget, "캐릭터와 겹치는 자리인데 놓을 수 있다고 나옵니다.");

            yield return Tap(mouse.leftButton);
            Assert.AreEqual(SceneBlockCount, world.Count);
        }

        [UnityTest]
        public IEnumerator 메뉴가_열려_있으면_놓이지_않는다()
        {
            yield return LoadBuildScene();
            yield return AimWithPart(45f, keyboard.digit1Key);

            ui.OpenMenu();
            yield return Frames(2);
            Assert.IsFalse(builder.HasTarget);

            yield return Tap(mouse.leftButton);
            Assert.AreEqual(SceneBlockCount, world.Count);
        }

        [UnityTest]
        public IEnumerator 씬에_미리_놓인_블록도_지울_수_있다()
        {
            yield return LoadBuildScene();

            // 계단의 흰 블록(칸 (0, 0, 4)의 가운데에 놓여 있음) 앞에 서서 정면 아래쪽을 본다.
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.position = new Vector3(0.5f, 0f, 1.5f);
            body.enabled = true;

            yield return AimWithPart(20f, keyboard.digit1Key);
            Assert.IsTrue(HasBlockNear(world, CellCenter(0, 0, 4)));

            yield return Tap(mouse.rightButton);
            Assert.IsFalse(HasBlockNear(world, CellCenter(0, 0, 4)), "씬에 미리 놓인 블록이 지워지지 않았습니다.");
            Assert.AreEqual(SceneBlockCount - 1, world.Count);
        }

        [UnityTest]
        public IEnumerator 부품을_골랐을_때만_놓기_안내가_보이고_블록_수가_표시된다()
        {
            yield return LoadBuildScene();
            Transform hint = Find<Transform>(ui, "BuildHint");
            TMP_Text count = Find<TMP_Text>(ui, "BlockCount");
            Assert.IsFalse(hint.gameObject.activeSelf);

            yield return AimWithPart(45f, keyboard.digit1Key);
            Assert.IsTrue(hint.gameObject.activeSelf);
            Assert.AreEqual($"블록 {SceneBlockCount}/{world.MaxBlocks}", count.text);

            yield return Tap(mouse.leftButton);
            Assert.AreEqual($"블록 {SceneBlockCount + 1}/{world.MaxBlocks}", count.text);

            yield return Tap(keyboard.digit1Key);
            Assert.IsFalse(hint.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadBuildScene();
            BeginCapture();

            // 작은 담을 쌓아 두고, 그 옆 칸을 가리켜 미리 보기 블록이 보이게 한다.
            world.Add(0, CellCenter(-1, 0, -3));
            world.Add(1, CellCenter(0, 0, -3));
            world.Add(2, CellCenter(1, 0, -3));
            world.Add(3, CellCenter(-1, 1, -3));
            world.Add(5, CellCenter(1, 1, -3));

            // 10.4도 아래를 보면 가운데 블록의 윗면에 닿아, 담의 빈 자리에 미리 보기 블록이 나타난다.
            yield return AimWithPart(10.4f, keyboard.digit1Key);
            Assert.AreEqual(1.5f, builder.TargetPosition.y, 0.001f);
            SaveCapture(Path.Combine(directory, "build-first-person.png"));

            player.Rig.SetViewDistance(4f);
            yield return new WaitForSeconds(0.6f);
            SaveCapture(Path.Combine(directory, "build-third-person.png"));

            player.Rig.SetViewDistance(0f);
            player.SetLook(0f, 80f);
            yield return new WaitForSeconds(0.6f);
            SaveCapture(Path.Combine(directory, "build-blocked.png"));
        }

        [UnityTest]
        public IEnumerator 자유_배치_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadBuildScene();
            BeginCapture();

            // 모눈의 선에 걸치거나 서로 겹치게 놓은 블록들. 칸에 맞추지 않아도 놓인다는 것을 보여 준다.
            world.Add(0, new Vector3(-0.85f, 0.5f, -2.9f));
            world.Add(1, new Vector3(0.2f, 0.5f, -2.65f));
            world.Add(2, new Vector3(0.95f, 0.5f, -2.95f));
            world.Add(3, new Vector3(0.45f, 1.5f, -2.8f));
            world.Add(5, new Vector3(1.9f, 0.5f, -2.3f));

            // 왼쪽 앞의 바닥을 가리켜, 칸에 맞지 않는 자리에 미리 보기 블록이 보이게 한다.
            player.CaptureLook(true);
            player.SetLook(-18f, 36f);
            yield return Tap(keyboard.digit1Key);
            yield return Frames(3);
            Assert.IsTrue(builder.HasTarget);
            Assert.AreEqual(BlockedReason.None, builder.Blocked);
            SaveCapture(Path.Combine(directory, "free-placement.png"));
        }

        private IEnumerator LoadBuildScene()
        {
            yield return LoadSandbox();

            world = Object.FindAnyObjectByType<BlockWorld>();
            builder = Object.FindAnyObjectByType<BlockBuilder>();
            Assert.IsNotNull(world, "Sandbox 씬에서 블록 세계를 찾을 수 없습니다.");
            Assert.IsNotNull(builder, "PC 캐릭터에 블록 놓기가 없습니다.");
        }
    }
}
