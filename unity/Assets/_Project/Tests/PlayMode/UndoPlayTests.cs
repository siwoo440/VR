using System.Collections;
using System.IO;
using AtelierVerse.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 되돌리기를 실제로 실행해 확인한다. 캐릭터는 (0.5, 0, -5.5)에서 +z 쪽을 보고 시작하므로,
    /// 아래로 45도를 보면 바닥의 (0, 0, -4) 칸을 가리킨다.
    /// </summary>
    public class UndoPlayTests : PlayTestBase
    {
        private const int SceneBlockCount = 19;
        private const float SaveWait = 1.6f;

        private static readonly Vector3Int FloorCell = new Vector3Int(0, 0, -4);
        private static readonly Vector3Int StepCell = new Vector3Int(0, 0, 4);

        private BlockWorld world;

        [UnityTest]
        public IEnumerator 놓은_블록을_Ctrl_Z로_되돌리고_Ctrl_Y로_다시_놓는다()
        {
            yield return LoadUndoScene();
            yield return AimWithPart(45f, keyboard.digit2Key);
            yield return Tap(mouse.leftButton);
            Assert.AreEqual(SceneBlockCount + 1, world.Count);
            Assert.IsTrue(world.History.CanUndo);

            yield return TapWithCtrl(keyboard.zKey);
            Assert.AreEqual(SceneBlockCount, world.Count);
            Assert.IsFalse(world.Has(FloorCell));
            yield return null;
            Assert.IsNull(FindBlock(FloorCell), "기록에서는 지워졌는데 화면에 블록이 남아 있습니다.");

            yield return TapWithCtrl(keyboard.yKey);
            Assert.IsTrue(world.TryGetPart(FloorCell, out int part));
            Assert.AreEqual(1, part);
            Assert.IsNotNull(FindBlock(FloorCell), "다시 실행했는데 화면에 블록이 없습니다.");
        }

        [UnityTest]
        public IEnumerator Ctrl_없이_Z만_누르면_되돌리지_않는다()
        {
            yield return LoadUndoScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            yield return Tap(mouse.leftButton);

            yield return Tap(keyboard.zKey);

            Assert.AreEqual(SceneBlockCount + 1, world.Count);
        }

        [UnityTest]
        public IEnumerator 지운_블록을_되돌리면_같은_부품으로_돌아온다()
        {
            yield return LoadUndoScene();

            // 계단의 흰 블록 (0, 0, 4) 앞에 서서 정면 아래쪽을 본다.
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.position = new Vector3(0.5f, 0f, 1.5f);
            body.enabled = true;

            Assert.IsTrue(world.TryGetPart(StepCell, out int original));
            yield return AimWithPart(20f, keyboard.digit1Key);
            yield return Tap(mouse.rightButton);
            Assert.IsFalse(world.Has(StepCell));

            yield return TapWithCtrl(keyboard.zKey);

            Assert.IsTrue(world.TryGetPart(StepCell, out int restored));
            Assert.AreEqual(original, restored, "되돌린 블록의 부품이 다릅니다.");
            Assert.AreEqual("block.ivory", world.Catalog.Get(restored).id);
            Assert.AreEqual(world.Catalog.Get(restored).material, FindBlock(StepCell).GetComponent<Renderer>().sharedMaterial);
        }

        [UnityTest]
        public IEnumerator 메뉴가_열려_있으면_되돌리지_않는다()
        {
            yield return LoadUndoScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            yield return Tap(mouse.leftButton);

            ui.OpenMenu();
            yield return null;
            yield return TapWithCtrl(keyboard.zKey);

            Assert.AreEqual(SceneBlockCount + 1, world.Count);
            Assert.IsTrue(world.History.CanUndo);
        }

        [UnityTest]
        public IEnumerator 되돌린_결과도_저장된다()
        {
            yield return LoadUndoScene();
            MapAutoSave autoSave = Object.FindAnyObjectByType<MapAutoSave>();
            Assert.IsNotNull(autoSave);

            world.History.Place(new Vector3Int(3, 0, -3), 0);
            yield return new WaitForSeconds(SaveWait);
            Assert.IsTrue(MapStorage.TryLoad(autoSave.FilePath, out MapDocument saved, out _));
            Assert.AreEqual(SceneBlockCount + 1, saved.blocks.Count);

            yield return TapWithCtrl(keyboard.zKey);
            Assert.AreEqual(SaveState.Pending, autoSave.State, "되돌리기도 저장을 예약해야 합니다.");
            yield return new WaitForSeconds(SaveWait);

            Assert.IsTrue(MapStorage.TryLoad(autoSave.FilePath, out MapDocument undone, out _));
            Assert.AreEqual(SceneBlockCount, undone.blocks.Count);
        }

        [UnityTest]
        public IEnumerator 씬을_다시_열면_기록이_비워진다()
        {
            yield return LoadUndoScene();
            world.History.Place(new Vector3Int(3, 0, -3), 0);
            Assert.IsTrue(world.History.CanUndo);

            yield return LoadUndoScene();

            Assert.IsFalse(world.History.CanUndo, "파일에서 불러온 맵에는 되돌릴 기록이 없어야 합니다.");
            Assert.IsTrue(world.Has(new Vector3Int(3, 0, -3)), "씬을 떠날 때 저장한 블록은 남아 있어야 합니다.");
        }

        [UnityTest]
        public IEnumerator 놓기_안내와_도움말에_되돌리기가_있다()
        {
            yield return LoadUndoScene();

            Transform hint = Find<Transform>(ui, "BuildHint");
            Assert.IsNotNull(Find<Transform>(hint, "Key_되돌리기"), "놓기 안내에 되돌리기 키가 없습니다.");
            Assert.IsNotNull(Find<Transform>(hint, "Key_다시 실행"), "놓기 안내에 다시 실행 키가 없습니다.");

            Transform help = Find<Transform>(ui.Menu, "HelpPage");
            bool undoListed = false;
            foreach (TMP_Text text in help.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.text == "되돌리기") undoListed = true;
            }

            Assert.IsTrue(undoListed, "도움말에 되돌리기 줄이 없습니다.");
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadUndoScene();
            BeginCapture();

            yield return AimWithPart(45f, keyboard.digit3Key);
            yield return Tap(mouse.leftButton);
            player.SetLook(0f, 20f);
            yield return Frames(2);
            SaveCapture(Path.Combine(directory, "undo-hint.png"));
        }

        private IEnumerator LoadUndoScene()
        {
            yield return LoadSandbox();

            world = Object.FindAnyObjectByType<BlockWorld>();
            Assert.IsNotNull(world, "Sandbox 씬에서 블록 세계를 찾을 수 없습니다.");
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
