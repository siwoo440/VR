using System.Collections;
using System.Collections.Generic;
using System.IO;
using AtelierVerse.Core;
using AtelierVerse.Player;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 끌어서 놓기·지우기·칠하기의 검사(21일차). 단추를 누른 채 조준을 옮기면 이어서 되고, 한 번 끈 것은 되돌리기 한 번에 돌아온다.
    /// 끌어서 놓기는 첫 블록을 놓은 면 위에 줄을 맞춰 놓고, 끌어서 지우기는 처음 가리킨 면과 같은 면의 블록만 지운다.
    /// 손으로 끌어 보았을 때 쓰기 편한지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class DragPlayTests : PlayTestBase
    {
        private const int GoldPart = 0;
        private const int BluePart = 1;

        private readonly List<(string message, NoticeKind kind)> notices = new List<(string message, NoticeKind kind)>();
        private readonly List<SfxId> sounds = new List<SfxId>();

        private BlockWorld world;
        private BlockBuilder builder;

        public override void Setup()
        {
            base.Setup();
            notices.Clear();
            sounds.Clear();
            Notice.Posted += OnNotice;
            Sfx.Played += OnSound;
        }

        public override void TearDown()
        {
            Sfx.Played -= OnSound;
            Notice.Posted -= OnNotice;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator 놓기를_누른_채_조준을_옮기면_첫_블록에_줄을_맞춰_이어_놓인다()
        {
            yield return LoadDragScene();
            yield return AimWithPart(45f, keyboard.digit2Key);
            int before = world.Count;
            int lastId = MaxId();
            int undoBefore = world.History.UndoCount;

            Press(mouse.leftButton);
            yield return Frames(2);
            Assert.AreEqual(before + 1, world.Count, "누른 그때 첫 블록이 놓여야 합니다.");
            Assert.AreEqual(StrokeKind.Place, builder.Stroke);

            yield return Hold();

            // 멀리 보도록 시점을 조금씩 든다. 조준이 바닥을 따라 앞으로 나아간다.
            for (float pitch = 43f; pitch >= 22f; pitch -= 3f)
            {
                player.SetLook(0f, pitch);
                yield return null;
            }

            yield return Frames(2);
            List<BlockRecord> placed = NewBlocks(lastId);
            Assert.GreaterOrEqual(placed.Count, 3, "끌었는데 이어 놓이지 않았습니다.");
            Assert.AreEqual(placed.Count, builder.StrokeCount);
            Assert.AreEqual(undoBefore, world.History.UndoCount, "끄는 동안에는 기록이 아직 닫히지 않습니다.");

            Release(mouse.leftButton);
            yield return Frames(2);
            Assert.IsFalse(builder.IsStroking);
            Assert.AreEqual(undoBefore + 1, world.History.UndoCount, "한 번 끈 것은 기록 하나여야 합니다.");
            Assert.IsTrue(Posted(BlockBuilder.StrokeMessage(StrokeKind.Place, placed.Count)), "몇 개를 놓았는지 알리지 않았습니다.");

            // 모두 바닥 위에 있고, 첫 블록에서 블록 한 변의 정수 배만큼 떨어져 있으며, 고른 부품이다.
            Vector3 first = placed[0].Position;
            foreach (BlockRecord block in placed)
            {
                Vector3 offset = block.Position - first;
                Assert.AreEqual(0.5f, block.Position.y, 0.002f);
                Assert.AreEqual(Mathf.Round(offset.x), offset.x, 0.002f, "줄이 맞지 않습니다.");
                Assert.AreEqual(Mathf.Round(offset.z), offset.z, 0.002f, "줄이 맞지 않습니다.");
                Assert.AreEqual(BluePart, block.Part);
            }

            AssertNoOverlap(placed);

            // 되돌리기 한 번에 모두 돌아오고, 다시 실행하면 모두 놓인다.
            yield return TapWithCtrl(keyboard.zKey);
            Assert.AreEqual(before, world.Count, "되돌리기 한 번에 끈 것이 모두 돌아와야 합니다.");
            yield return TapWithCtrl(keyboard.yKey);
            Assert.AreEqual(before + placed.Count, world.Count);
        }

        [UnityTest]
        public IEnumerator 한_프레임에_여러_칸을_건너뛰어도_사이가_비지_않는다()
        {
            yield return LoadDragScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            int lastId = MaxId();

            Press(mouse.leftButton);
            yield return Frames(2);

            yield return Hold();

            // 한 번에 멀리 본다(손이 닿는 거리 안에서 네 칸쯤).
            player.SetLook(0f, 18f);
            yield return Frames(3);
            Release(mouse.leftButton);
            yield return Frames(2);

            List<BlockRecord> placed = NewBlocks(lastId);
            Assert.GreaterOrEqual(placed.Count, 4);

            // 앞으로 곧게 한 칸씩 이어져 있다.
            placed.Sort((a, b) => a.Position.z.CompareTo(b.Position.z));
            for (int i = 1; i < placed.Count; i++)
            {
                Assert.AreEqual(1f, placed[i].Position.z - placed[i - 1].Position.z, 0.003f, "사이에 빈 칸이 있습니다.");
                Assert.AreEqual(placed[0].Position.x, placed[i].Position.x, 0.003f);
            }
        }

        [UnityTest]
        public IEnumerator 짧게_누르면_하나만_놓이고_조금_흔들려도_더_놓이지_않는다()
        {
            yield return LoadDragScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            int before = world.Count;
            int undoBefore = world.History.UndoCount;

            Press(mouse.leftButton);
            yield return Frames(2);

            yield return Hold();

            // 누른 채 조준이 조금 흔들린다(블록 반 개에 못 미침).
            player.SetLook(4f, 44f);
            yield return Frames(2);
            player.SetLook(-4f, 46f);
            yield return Frames(2);
            Release(mouse.leftButton);
            yield return Frames(2);

            Assert.AreEqual(before + 1, world.Count);
            Assert.AreEqual(undoBefore + 1, world.History.UndoCount);
            Assert.IsFalse(PostedContaining("이어"), "하나만 놓았는데 이어 놓았다고 알렸습니다.");
        }

        [UnityTest]
        public IEnumerator 짧게_누르는_사이에_조준이_크게_움직여도_하나만_놓인다()
        {
            yield return LoadDragScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            int before = world.Count;

            // 누르자마자 시점을 휙 돌리고 곧 뗀다(끌기로 보는 시간에 못 미침).
            Press(mouse.leftButton);
            yield return null;
            player.SetLook(0f, 18f);
            yield return Frames(2);
            Release(mouse.leftButton);
            yield return Frames(2);

            Assert.AreEqual(before + 1, world.Count, "짧게 눌렀는데 여러 개가 놓였습니다.");
            Assert.IsFalse(builder.IsStroking);
        }

        [UnityTest]
        public IEnumerator 끌어서_놓는_줄은_맞추기_도우미가_켜져_있으면_모눈에_맞는다()
        {
            GameSettings.SnapLevel = 1;
            yield return LoadDragScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            int lastId = MaxId();

            Press(mouse.leftButton);
            yield return Frames(2);
            yield return Hold();
            player.SetLook(0f, 18f);
            yield return Frames(3);
            Release(mouse.leftButton);
            yield return Frames(2);

            List<BlockRecord> placed = NewBlocks(lastId);
            Assert.GreaterOrEqual(placed.Count, 3);
            foreach (BlockRecord block in placed)
            {
                Assert.AreEqual(0.5f, Mathf.Repeat(block.Position.x, 1f), 0.002f, "모눈의 칸 가운데가 아닙니다.");
                Assert.AreEqual(0.5f, Mathf.Repeat(block.Position.z, 1f), 0.002f, "모눈의 칸 가운데가 아닙니다.");
            }
        }

        [UnityTest]
        public IEnumerator 이미_블록이_있는_자리는_건너뛴다()
        {
            yield return LoadDragScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            int lastId = MaxId();

            Press(mouse.leftButton);
            yield return Frames(2);
            List<BlockRecord> placed = NewBlocks(lastId);
            Assert.AreEqual(1, placed.Count);
            Vector3 first = placed[0].Position;

            // 바로 다음 칸에, 줄에서 조금 비켜난 자리에 블록이 이미 있다.
            Vector3 obstacle = first + new Vector3(0.1f, 0f, 1f);
            Assert.AreEqual(PlaceResult.Ok, world.Add(BluePart, obstacle));
            yield return SettlePhysics();
            int obstacleId = MaxId();
            yield return Hold();

            player.SetLook(0f, 18f);
            yield return Frames(3);
            Release(mouse.leftButton);
            yield return Frames(2);

            placed = NewBlocks(obstacleId);
            Assert.GreaterOrEqual(placed.Count, 2, "막힌 칸의 뒤는 이어 놓여야 합니다.");
            foreach (BlockRecord block in placed)
            {
                Assert.Greater(Vector3.Distance(block.Position, obstacle), 0.95f, "이미 있는 블록의 속에 놓였습니다.");
            }

            Assert.IsFalse(HasBlockNear(world, first + Vector3.forward, 0.05f), "막힌 칸에 블록이 놓였습니다.");
            Assert.IsTrue(HasBlockNear(world, first + Vector3.forward * 2f, 0.05f), "막힌 칸의 다음 칸이 비었습니다.");
        }

        [UnityTest]
        public IEnumerator 맵_밖의_칸은_건너뛰고_한_번만_알린다()
        {
            yield return LoadDragScene();
            player.CaptureLook(true);
            player.SetLook(180f, 45f);
            yield return Tap(keyboard.digit1Key);
            yield return Frames(2);
            Assert.IsTrue(builder.CanPlaceAtTarget, "맵의 끝 가까이의 바닥에 놓을 수 있어야 합니다.");
            int before = world.Count;

            Press(mouse.leftButton);
            yield return Frames(2);
            Assert.AreEqual(before + 1, world.Count);
            notices.Clear();

            yield return Hold();

            // 맵의 끝을 넘어가도록 멀리 본다. 여러 칸이 맵 밖이다.
            player.SetLook(180f, 30f);
            yield return Frames(2);
            player.SetLook(180f, 22f);
            yield return Frames(3);
            Release(mouse.leftButton);
            yield return Frames(2);

            Assert.AreEqual(before + 1, world.Count, "맵 밖에 블록이 놓였습니다.");
            Assert.AreEqual(1, CountNotices(NoticeKind.Warning), "끌기 한 번에 한 번만 알려야 합니다.");
        }

        [UnityTest]
        public IEnumerator 지우기를_누른_채_옮기면_같은_면의_블록만_지워지고_뒤의_블록은_남는다()
        {
            yield return LoadDragScene();
            int[] front = AddRow(-3);
            int[] back = AddRow(-2);
            yield return SettlePhysics();
            yield return AimWithPart(45f, keyboard.digit1Key);
            int undoBefore = world.History.UndoCount;

            // 앞줄 가운데 블록의 앞면을 가리키고 지우기를 누른다.
            LookAt(new Vector3(0.5f, 0.5f, -3f));
            yield return Frames(3);
            Assert.IsTrue(builder.HasBlockTarget);
            Assert.AreEqual(front[1], builder.TargetBlockId);

            Press(mouse.rightButton);
            yield return Frames(2);
            Assert.IsFalse(world.Has(front[1]));
            Assert.AreEqual(StrokeKind.Remove, builder.Stroke);

            // 그대로 누르고 있으면 조준이 뒤의 블록에 닿는다. 뒤의 블록은 면이 달라 지워지지 않아야 한다.
            yield return Hold();
            yield return Frames(4);
            Assert.IsTrue(world.Has(back[1]), "누르고 있었더니 뒤의 블록까지 뚫고 지웠습니다.");

            // 옆의 블록들의 앞면으로 옮긴다.
            LookAt(new Vector3(1.5f, 0.5f, -3f));
            yield return Frames(3);
            LookAt(new Vector3(-0.5f, 0.5f, -3f));
            yield return Frames(3);
            Release(mouse.rightButton);
            yield return Frames(2);

            foreach (int id in front) Assert.IsFalse(world.Has(id), "같은 면의 블록이 지워지지 않았습니다.");
            foreach (int id in back) Assert.IsTrue(world.Has(id), "뒤의 블록이 지워졌습니다.");
            Assert.AreEqual(undoBefore + 1, world.History.UndoCount);
            Assert.IsTrue(Posted(BlockBuilder.StrokeMessage(StrokeKind.Remove, 3)));

            yield return TapWithCtrl(keyboard.zKey);
            foreach (int id in front) Assert.IsTrue(world.Has(id), "되돌리기 한 번에 지운 것이 모두 돌아와야 합니다.");
        }

        [UnityTest]
        public IEnumerator 칠하기를_누른_채_옮기면_지나가는_블록이_모두_칠해진다()
        {
            yield return LoadDragScene();
            int[] row = AddRow(-3);
            yield return SettlePhysics();
            yield return AimWithPart(45f, keyboard.digit2Key);
            int undoBefore = world.History.UndoCount;

            LookAt(new Vector3(0.5f, 0.5f, -3f));
            yield return Frames(3);
            Press(mouse.middleButton);
            yield return Frames(2);
            Assert.AreEqual(StrokeKind.Paint, builder.Stroke);
            yield return Hold();

            LookAt(new Vector3(1.5f, 0.5f, -3f));
            yield return Frames(3);
            LookAt(new Vector3(-0.5f, 0.5f, -3f));
            yield return Frames(3);

            // 이미 칠한 블록 위를 다시 지나가도 편집이 늘지 않는다.
            LookAt(new Vector3(0.5f, 0.5f, -3f));
            yield return Frames(3);
            Assert.AreEqual(3, builder.StrokeCount);

            Release(mouse.middleButton);
            yield return Frames(2);

            foreach (int id in row)
            {
                Assert.IsTrue(world.TryGet(id, out BlockRecord record));
                Assert.AreEqual(BluePart, record.Part, "지나간 블록이 칠해지지 않았습니다.");
            }

            Assert.AreEqual(undoBefore + 1, world.History.UndoCount);
            Assert.IsFalse(PostedContaining("이미 같은 색"), "끄는 동안에는 같은 색이라는 알림을 올리지 않습니다.");

            yield return TapWithCtrl(keyboard.zKey);
            foreach (int id in row)
            {
                Assert.IsTrue(world.TryGet(id, out BlockRecord record));
                Assert.AreEqual(GoldPart, record.Part, "되돌리기 한 번에 모두 원래 색으로 돌아와야 합니다.");
            }
        }

        [UnityTest]
        public IEnumerator 끄는_도중에_메뉴를_열면_끌기가_끝나고_그때까지가_한_묶음이다()
        {
            yield return LoadDragScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            int before = world.Count;
            int undoBefore = world.History.UndoCount;

            Press(mouse.leftButton);
            yield return Frames(2);
            yield return Hold();
            player.SetLook(0f, 30f);
            yield return Frames(3);
            int placed = world.Count - before;
            Assert.GreaterOrEqual(placed, 2);

            yield return Tap(keyboard.escapeKey);
            Assert.IsTrue(ui.IsMenuOpen);
            Assert.IsFalse(builder.IsStroking, "메뉴를 열었는데 끌기가 이어집니다.");
            Assert.AreEqual(undoBefore + 1, world.History.UndoCount);

            // 메뉴를 닫아도 단추를 다시 누르기 전에는 놓이지 않는다.
            ui.CloseMenu();
            player.CaptureLook(true);
            yield return Frames(2);
            player.SetLook(0f, 22f);
            yield return Frames(3);
            Assert.AreEqual(before + placed, world.Count, "끌기가 끝났는데 블록이 더 놓였습니다.");

            Release(mouse.leftButton);
            yield return Frames(2);
        }

        [UnityTest]
        public IEnumerator 끄는_도중에_되돌리면_그때까지_끈_것이_돌아오고_끌기가_끝난다()
        {
            yield return LoadDragScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            int before = world.Count;

            Press(mouse.leftButton);
            yield return Frames(2);
            yield return Hold();
            player.SetLook(0f, 30f);
            yield return Frames(3);
            Assert.GreaterOrEqual(world.Count - before, 2);
            notices.Clear();

            yield return TapWithCtrl(keyboard.zKey);
            Assert.AreEqual(before, world.Count, "끄는 도중의 되돌리기는 그때까지 끈 것을 모두 돌려야 합니다.");
            Assert.IsFalse(builder.IsStroking);
            Assert.IsFalse(PostedContaining("이어 놓았습니다"), "방금 되돌린 것을 놓았다고 알렸습니다.");

            // 단추를 누른 채 더 옮겨도 놓이지 않는다.
            player.SetLook(0f, 22f);
            yield return Frames(3);
            Assert.AreEqual(before, world.Count);

            Release(mouse.leftButton);
            yield return Frames(2);
        }

        [UnityTest]
        public IEnumerator 끄는_동안_블록_소리가_쏟아지지_않고_묶음을_되돌릴_때는_한_번만_난다()
        {
            yield return LoadDragScene();
            yield return AimWithPart(45f, keyboard.digit1Key);
            int before = world.Count;
            sounds.Clear();

            Press(mouse.leftButton);
            yield return Frames(2);

            yield return Hold();

            // 한 프레임에 여러 블록이 놓인다.
            player.SetLook(0f, 18f);
            yield return Frames(3);
            Release(mouse.leftButton);
            yield return Frames(2);

            int placed = world.Count - before;
            Assert.GreaterOrEqual(placed, 4);
            int placeSounds = Count(SfxId.Place);
            Assert.GreaterOrEqual(placeSounds, 1);
            Assert.LessOrEqual(placeSounds, 2, "한꺼번에 놓인 블록마다 소리가 났습니다.");

            sounds.Clear();
            yield return TapWithCtrl(keyboard.zKey);
            Assert.AreEqual(before, world.Count);
            Assert.AreEqual(1, Count(SfxId.Undo), "묶음을 되돌릴 때는 소리가 한 번만 나야 합니다.");
            Assert.AreEqual(0, Count(SfxId.Remove));
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadDragScene();
            BeginCapture();
            yield return AimWithPart(45f, keyboard.digit2Key);

            // 한 번 누른 채로: 앞으로, 오른쪽으로, 다시 몸 쪽으로 끌어 ㄷ자 모양으로 놓는다.
            Press(mouse.leftButton);
            yield return Frames(2);
            yield return Hold();
            for (float pitch = 42f; pitch >= 24f; pitch -= 3f)
            {
                player.SetLook(0f, pitch);
                yield return null;
            }

            for (float yaw = 4f; yaw <= 28f; yaw += 4f)
            {
                player.SetLook(yaw, 24f);
                yield return null;
            }

            for (float pitch = 27f; pitch <= 42f; pitch += 3f)
            {
                player.SetLook(28f, pitch);
                yield return null;
            }

            yield return Frames(2);
            Release(mouse.leftButton);
            yield return Frames(2);

            // 놓은 모양이 한눈에 보이게 3인칭으로 물러나 위에서 내려다본다. 부품 선택을 풀어 미리 보기 블록을 감춘다.
            builder.SelectedPart = BlockBuilder.NoPart;
            player.Rig.SetViewDistance(7f);
            player.SetLook(10f, 42f);
            yield return new WaitForSeconds(0.7f);
            SaveCapture(Path.Combine(directory, "drag-place.png"));
        }

        private IEnumerator LoadDragScene()
        {
            yield return LoadSandbox();

            world = Object.FindAnyObjectByType<BlockWorld>();
            builder = Object.FindAnyObjectByType<BlockBuilder>();
            Assert.IsNotNull(world, "Sandbox 씬에서 블록 세계를 찾을 수 없습니다.");
            Assert.IsNotNull(builder, "PC 캐릭터에 블록 놓기가 없습니다.");
        }

        /// <summary>
        /// 방금 놓은 블록의 충돌체가 조준과 겹침 검사에 잡히도록 물리 갱신을 한 번 기다린다.
        /// 물리 갱신 도중에는 가상 기기의 값을 넣을 수 없으므로 보통 프레임으로 돌아온 뒤에 끝낸다.
        /// </summary>
        private static IEnumerator SettlePhysics()
        {
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        /// <summary>누른 뒤 끌기로 보기 시작할 때까지(BlockBuilder.StrokeHoldDelay) 그대로 누르고 있는다.</summary>
        private static IEnumerator Hold()
        {
            yield return new WaitForSeconds(BlockBuilder.StrokeHoldDelay + 0.08f);
        }

        /// <summary>캐릭터 앞에 가로로 블록 셋을 놓는다(x 칸 -1, 0, 1). 앞면은 z = cellZ의 자리다.</summary>
        private int[] AddRow(int cellZ)
        {
            var ids = new int[3];
            for (int i = 0; i < ids.Length; i++)
            {
                Assert.AreEqual(PlaceResult.Ok, world.Add(GoldPart, CellCenter(i - 1, 0, cellZ), Quaternion.identity, out ids[i]));
            }

            return ids;
        }

        /// <summary>눈에서 point를 똑바로 바라보게 한다.</summary>
        private void LookAt(Vector3 point)
        {
            Vector3 direction = point - player.Rig.ViewCamera.transform.position;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Atan2(-direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
            player.SetLook(yaw, pitch);
        }

        private int MaxId()
        {
            int max = 0;
            foreach (BlockRecord block in world.Blocks)
            {
                max = Mathf.Max(max, block.Id);
            }

            return max;
        }

        /// <summary>번호가 afterId보다 큰 블록들(그 뒤에 놓인 것). 놓인 순서대로다.</summary>
        private List<BlockRecord> NewBlocks(int afterId)
        {
            var blocks = new List<BlockRecord>();
            foreach (BlockRecord block in world.Blocks)
            {
                if (block.Id > afterId) blocks.Add(block);
            }

            blocks.Sort((a, b) => a.Id.CompareTo(b.Id));
            return blocks;
        }

        private static void AssertNoOverlap(List<BlockRecord> blocks)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                for (int k = i + 1; k < blocks.Count; k++)
                {
                    Assert.GreaterOrEqual(Vector3.Distance(blocks[i].Position, blocks[k].Position), 0.99f, "이어 놓은 블록이 서로 겹쳤습니다.");
                }
            }
        }

        private void OnNotice(string message, NoticeKind kind)
        {
            notices.Add((message, kind));
        }

        private void OnSound(SfxId id, Vector3? position)
        {
            sounds.Add(id);
        }

        private bool Posted(string message)
        {
            foreach ((string posted, NoticeKind _) in notices)
            {
                if (posted == message) return true;
            }

            return false;
        }

        private bool PostedContaining(string part)
        {
            foreach ((string posted, NoticeKind _) in notices)
            {
                if (posted.Contains(part)) return true;
            }

            return false;
        }

        private int CountNotices(NoticeKind kind)
        {
            int count = 0;
            foreach ((string _, NoticeKind posted) in notices)
            {
                if (posted == kind) count++;
            }

            return count;
        }

        private int Count(SfxId id)
        {
            int count = 0;
            foreach (SfxId heard in sounds)
            {
                if (heard == id) count++;
            }

            return count;
        }
    }
}
