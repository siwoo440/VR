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
    /// 칠하기와 알림 띠를 실제로 실행해 확인한다. 캐릭터는 (0.5, 0, -5.5)에서 +z 쪽을 보고 시작하므로,
    /// 아래로 45도를 보면 바닥의 (0, 0, -4) 칸을 가리키고, 그 칸에 블록을 놓으면 그 블록의 앞면을 가리킨다.
    /// </summary>
    public class PaintPlayTests : PlayTestBase
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
        public IEnumerator 가운데_단추로_조준한_블록을_고른_부품으로_칠한다()
        {
            yield return LoadPaintScene();
            yield return PlaceBlueAndSelectGold();

            yield return Tap(mouse.middleButton);

            Assert.IsTrue(TryGetPartNear(world, FloorSpot, out int part));
            Assert.AreEqual(GoldPart, part);
            Assert.AreEqual(SceneBlockCount + 1, world.Count, "칠하기는 블록 수를 바꾸지 않습니다.");
            Assert.AreEqual(world.Catalog.Get(GoldPart).material, FindViewNear(world, FloorSpot).GetComponent<Renderer>().sharedMaterial);
            Assert.AreEqual(2, world.History.UndoCount);
        }

        [UnityTest]
        public IEnumerator F_키로도_칠한다()
        {
            yield return LoadPaintScene();
            yield return PlaceBlueAndSelectGold();

            yield return Tap(keyboard.fKey);

            Assert.IsTrue(TryGetPartNear(world, FloorSpot, out int part));
            Assert.AreEqual(GoldPart, part);
        }

        [UnityTest]
        public IEnumerator 칠하기를_되돌리면_이전_부품으로_돌아온다()
        {
            yield return LoadPaintScene();
            yield return PlaceBlueAndSelectGold();
            yield return Tap(mouse.middleButton);

            yield return TapWithCtrl(keyboard.zKey);
            Assert.IsTrue(TryGetPartNear(world, FloorSpot, out int restored));
            Assert.AreEqual(BluePart, restored);
            Assert.AreEqual(world.Catalog.Get(BluePart).material, FindViewNear(world, FloorSpot).GetComponent<Renderer>().sharedMaterial);

            yield return TapWithCtrl(keyboard.yKey);
            Assert.IsTrue(TryGetPartNear(world, FloorSpot, out int again));
            Assert.AreEqual(GoldPart, again);
        }

        [UnityTest]
        public IEnumerator 같은_부품으로_칠하면_바뀌지_않고_알림이_나온다()
        {
            yield return LoadPaintScene();
            yield return AimWithPart(45f, keyboard.digit2Key);
            yield return Tap(mouse.leftButton);
            Assert.IsTrue(builder.HasBlockTarget);

            yield return Tap(mouse.middleButton);

            Assert.IsTrue(TryGetPartNear(world, FloorSpot, out int part));
            Assert.AreEqual(BluePart, part);
            Assert.AreEqual(1, world.History.UndoCount);
            Assert.IsTrue(notice.IsVisible);
            Assert.AreEqual(BlockBuilder.SamePartMessage, notice.Message);
        }

        [UnityTest]
        public IEnumerator 블록이_없는_곳에서_칠하면_알림이_나온다()
        {
            yield return LoadPaintScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            Assert.IsFalse(builder.HasBlockTarget);

            yield return Tap(keyboard.fKey);

            Assert.AreEqual(SceneBlockCount, world.Count);
            Assert.IsTrue(notice.IsVisible);
            Assert.AreEqual(BlockBuilder.PaintTargetMessage, notice.Message);
        }

        [UnityTest]
        public IEnumerator 놓을_수_없는_곳을_누르면_까닭이_알림으로_나온다()
        {
            yield return LoadPaintScene();
            yield return AimWithPart(80f, keyboard.digit1Key);
            Assert.AreEqual(BlockedReason.Overlap, builder.Blocked);
            Assert.IsFalse(notice.IsVisible);

            yield return Tap(mouse.leftButton);

            Assert.AreEqual(SceneBlockCount, world.Count);
            Assert.IsTrue(notice.IsVisible);
            StringAssert.Contains("겹쳐", notice.Message);
            Assert.AreEqual(NoticeKind.Warning, notice.Model.Kind);
        }

        [UnityTest]
        public IEnumerator 알림은_잠시_뒤_사라진다()
        {
            yield return LoadPaintScene();

            Notice.Post("시험 알림");
            yield return null;
            Assert.IsTrue(notice.IsVisible);
            Assert.IsTrue(Find<Transform>(notice, "Bar").gameObject.activeSelf);

            yield return new WaitForSecondsRealtime(NoticeModel.DefaultDuration + 0.5f);

            Assert.IsFalse(notice.IsVisible);
            Assert.IsFalse(Find<Transform>(notice, "Bar").gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator 되돌릴_것이_없으면_알림이_나온다()
        {
            yield return LoadPaintScene();
            Assert.IsFalse(world.History.CanUndo);

            yield return TapWithCtrl(keyboard.zKey);
            Assert.AreEqual(GameUi.NothingToUndoMessage, notice.Message);

            yield return TapWithCtrl(keyboard.yKey);
            Assert.AreEqual(GameUi.NothingToRedoMessage, notice.Message);
        }

        [UnityTest]
        public IEnumerator 읽지_못한_블록이_있으면_알림이_나온다()
        {
            PrepareMapDirectory();
            MapDocument document = MapDocument.Create("내 맵", new Vector3(-8f, 0f, -8f), new Vector3(8f, 12f, 8f));
            document.blocks.Add(new MapBlock { part = "block.gold", position = CellCenter(0, 0, 0) });
            document.blocks.Add(new MapBlock { part = "block.future", position = CellCenter(1, 0, 0) });
            MapStorage.Save(document, MapStorage.LocalPath);

            yield return LoadPaintScene();

            Assert.IsTrue(notice.IsVisible);
            Assert.AreEqual("블록 1개를 읽지 못했습니다", notice.Message);
            Assert.AreEqual(NoticeKind.Warning, notice.Model.Kind);
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadPaintScene();
            BeginCapture();

            // 발밑을 가리켜 겹침 알림을 띄운 모습
            yield return AimWithPart(80f, keyboard.digit3Key);
            yield return Tap(mouse.leftButton);
            yield return Frames(2);
            SaveCapture(Path.Combine(directory, "notice-blocked.png"));

            // 블루 블록을 테라코타로 칠한 모습
            player.SetLook(0f, 45f);
            yield return Tap(keyboard.digit2Key);
            yield return Frames(2);
            yield return Tap(mouse.leftButton);
            yield return Tap(keyboard.digit3Key);
            yield return Frames(2);
            yield return Tap(mouse.middleButton);
            yield return Frames(2);
            SaveCapture(Path.Combine(directory, "paint-first-person.png"));
        }

        private IEnumerator LoadPaintScene()
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

        /// <summary>바닥 칸에 블루 블록을 놓은 뒤 골드 부품을 고른다. 조준은 그 블록의 앞면에 남는다.</summary>
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
