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

        private static readonly Vector3Int FloorCell = new Vector3Int(0, 0, -4);

        private BlockWorld world;
        private BlockBuilder builder;

        [UnityTest]
        public IEnumerator 씬에_미리_놓인_블록이_블록_세계에_올라_있다()
        {
            yield return LoadBuildScene();

            Assert.AreEqual(SceneBlockCount, world.Count);
            Assert.IsTrue(world.TryGetPart(new Vector3Int(2, 0, 2), out int housePart), "집의 블록이 기록에 없습니다.");
            Assert.AreEqual("block.gold", world.Catalog.Get(housePart).id);
            Assert.IsTrue(world.TryGetPart(new Vector3Int(-5, 0, 1), out int stagePart), "단의 블록이 기록에 없습니다.");
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
        public IEnumerator 부품을_고르고_왼쪽을_누르면_조준한_칸에_블록이_놓인다()
        {
            yield return LoadBuildScene();
            yield return AimWithPart(45f, keyboard.digit2Key);

            Assert.IsTrue(builder.HasTarget, "조준한 곳에 놓일 칸이 없습니다.");
            Assert.AreEqual(FloorCell, builder.TargetCell);
            Assert.IsTrue(builder.CanPlaceAtTarget);

            yield return Tap(mouse.leftButton);

            Assert.AreEqual(SceneBlockCount + 1, world.Count);
            Assert.IsTrue(world.TryGetPart(FloorCell, out int part));
            Assert.AreEqual(BluePart, part);

            PlacedBlock block = FindBlock(FloorCell);
            Assert.IsNotNull(block, "기록에는 있는데 화면에 블록이 없습니다.");
            Assert.AreEqual(GridMath.CellToWorldCenter(FloorCell), block.transform.position);
            Assert.AreEqual(world.Catalog.Get(BluePart).material, block.GetComponent<Renderer>().sharedMaterial);
        }

        [UnityTest]
        public IEnumerator 오른쪽을_누르면_조준한_블록이_지워진다()
        {
            yield return LoadBuildScene();
            yield return AimWithPart(45f, keyboard.digit2Key);
            yield return Tap(mouse.leftButton);
            Assert.IsTrue(world.Has(FloorCell));

            yield return Tap(mouse.rightButton);

            Assert.IsFalse(world.Has(FloorCell));
            Assert.AreEqual(SceneBlockCount, world.Count);
            yield return null;
            Assert.IsNull(FindBlock(FloorCell), "기록에서는 지워졌는데 화면에 블록이 남아 있습니다.");
        }

        [UnityTest]
        public IEnumerator 놓인_블록의_윗면을_가리키면_그_위에_쌓인다()
        {
            yield return LoadBuildScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            yield return Tap(mouse.leftButton);

            // 머리 높이 1.55에서 15.4도 아래를 보면 방금 놓은 블록의 윗면 가운데쯤에 닿는다.
            player.SetLook(0f, 15.4f);
            yield return Frames(2);
            Assert.AreEqual(new Vector3Int(0, 1, -4), builder.TargetCell);

            yield return Tap(mouse.leftButton);
            Assert.IsTrue(world.Has(new Vector3Int(0, 1, -4)));
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
        public IEnumerator 캐릭터가_서_있는_칸에는_놓이지_않는다()
        {
            yield return LoadBuildScene();
            yield return AimWithPart(80f, keyboard.digit1Key);

            Assert.IsTrue(builder.HasTarget);
            Assert.AreEqual(new Vector3Int(0, 0, -6), builder.TargetCell);
            Assert.IsFalse(builder.CanPlaceAtTarget, "캐릭터와 겹치는 칸인데 놓을 수 있다고 나옵니다.");

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

            // 계단의 흰 블록 (0, 0, 4) 앞에 서서 정면 아래쪽을 본다.
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.position = new Vector3(0.5f, 0f, 1.5f);
            body.enabled = true;

            yield return AimWithPart(20f, keyboard.digit1Key);
            Assert.IsTrue(world.Has(new Vector3Int(0, 0, 4)));

            yield return Tap(mouse.rightButton);
            Assert.IsFalse(world.Has(new Vector3Int(0, 0, 4)), "씬에 미리 놓인 블록이 지워지지 않았습니다.");
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
            world.Place(new Vector3Int(-1, 0, -3), 0);
            world.Place(new Vector3Int(0, 0, -3), 1);
            world.Place(new Vector3Int(1, 0, -3), 2);
            world.Place(new Vector3Int(-1, 1, -3), 3);
            world.Place(new Vector3Int(1, 1, -3), 5);

            // 10.4도 아래를 보면 가운데 블록의 윗면에 닿아, 담의 빈 칸에 미리 보기 블록이 나타난다.
            yield return AimWithPart(10.4f, keyboard.digit1Key);
            Assert.AreEqual(new Vector3Int(0, 1, -3), builder.TargetCell);
            SaveCapture(Path.Combine(directory, "build-first-person.png"));

            player.SetViewDistance(4f);
            yield return new WaitForSeconds(0.6f);
            SaveCapture(Path.Combine(directory, "build-third-person.png"));

            player.SetViewDistance(0f);
            player.SetLook(0f, 80f);
            yield return new WaitForSeconds(0.6f);
            SaveCapture(Path.Combine(directory, "build-blocked.png"));
        }

        private IEnumerator LoadBuildScene()
        {
            yield return LoadSandbox();

            world = Object.FindAnyObjectByType<BlockWorld>();
            builder = Object.FindAnyObjectByType<BlockBuilder>();
            Assert.IsNotNull(world, "Sandbox 씬에서 블록 세계를 찾을 수 없습니다.");
            Assert.IsNotNull(builder, "PC 캐릭터에 블록 놓기가 없습니다.");
        }

        /// <summary>마우스를 잡고, 아래로 pitch도를 보고, 숫자 키로 부품을 고른 뒤 조준이 잡힐 때까지 기다린다.</summary>
        private IEnumerator AimWithPart(float pitch, UnityEngine.InputSystem.Controls.KeyControl partKey)
        {
            player.CaptureLook(true);
            player.SetLook(0f, pitch);
            yield return Tap(partKey);
            yield return Frames(2);
        }

        private PlacedBlock FindBlock(Vector3Int cell)
        {
            foreach (PlacedBlock block in world.GetComponentsInChildren<PlacedBlock>())
            {
                if (block.Cell == cell) return block;
            }

            return null;
        }
    }
}
