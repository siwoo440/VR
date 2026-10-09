using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 블록을 놓을 자리와 방향을 다듬는 계산의 검사(15일차): 돌리는 각도의 단계와 맞추기 도우미.
    /// 맞추기 도우미는 바닥에서는 모눈에, 놓인 블록 위에서는 그 블록에 나란히 붙는 자리로 당긴다.
    /// </summary>
    public class PlacementMathTests
    {
        private const float Close = 0.0001f;

        private static void AssertNear(Vector3 expected, Vector3 actual, string message = null)
        {
            Assert.Less(Vector3.Distance(expected, actual), Close, message ?? $"기대 {expected:F3}, 실제 {actual:F3}");
        }

        [Test]
        public void 한_단계는_15도이고_한_바퀴를_넘으면_다시_0부터다()
        {
            Assert.AreEqual(15f, PlacementMath.StepYaw(0f, 1), Close);
            Assert.AreEqual(0f, PlacementMath.StepYaw(345f, 1), Close);
            Assert.AreEqual(345f, PlacementMath.StepYaw(0f, -1), Close);
            Assert.AreEqual(0f, PlacementMath.StepYaw(30f, -2), Close);

            float yaw = 0f;
            for (int i = 0; i < 24; i++) yaw = PlacementMath.StepYaw(yaw, 1);
            Assert.AreEqual(0f, yaw, Close, "스물네 번 돌리면 한 바퀴입니다.");
        }

        [Test]
        public void 방향에서_좌우로_돈_각도만_꺼낸다()
        {
            Assert.AreEqual(0f, PlacementMath.YawOf(Quaternion.identity), 0.01f);
            Assert.AreEqual(30f, PlacementMath.YawOf(Quaternion.Euler(0f, 30f, 0f)), 0.01f);
            Assert.AreEqual(270f, PlacementMath.YawOf(Quaternion.Euler(0f, -90f, 0f)), 0.01f);
            Assert.AreEqual(345f, PlacementMath.YawOf(PlacementMath.YawRotation(345f)), 0.01f);
            Assert.AreEqual(0f, PlacementMath.YawOf(default), 0.01f, "값을 넣지 않은 방향은 돌리지 않은 것으로 봅니다.");
        }

        [Test]
        public void 맞추기_단계는_끔에서_시작해_한_바퀴_돈다()
        {
            Assert.AreEqual(4, PlacementMath.SnapLevelCount);
            Assert.AreEqual(1, PlacementMath.NextSnapLevel(0));
            Assert.AreEqual(2, PlacementMath.NextSnapLevel(1));
            Assert.AreEqual(3, PlacementMath.NextSnapLevel(2));
            Assert.AreEqual(0, PlacementMath.NextSnapLevel(3));

            Assert.AreEqual(0f, PlacementMath.SnapStepOf(0));
            Assert.AreEqual(1f, PlacementMath.SnapStepOf(1));
            Assert.AreEqual(0.5f, PlacementMath.SnapStepOf(2));
            Assert.AreEqual(0.25f, PlacementMath.SnapStepOf(3));

            Assert.AreEqual("끔", PlacementMath.SnapLabelOf(0));
            Assert.AreEqual("1칸", PlacementMath.SnapLabelOf(1));
            Assert.AreEqual("1/2칸", PlacementMath.SnapLabelOf(2));
            Assert.AreEqual("1/4칸", PlacementMath.SnapLabelOf(3));
        }

        [Test]
        public void 범위_밖의_단계는_끔으로_본다()
        {
            Assert.AreEqual(0, PlacementMath.ClampSnapLevel(-1));
            Assert.AreEqual(0, PlacementMath.ClampSnapLevel(9));
            Assert.AreEqual(0f, PlacementMath.SnapStepOf(9));
            Assert.AreEqual("끔", PlacementMath.SnapLabelOf(-3));
        }

        [Test]
        public void 간격이_1이면_칸의_가운데로_맞춘다()
        {
            Assert.AreEqual(0.5f, PlacementMath.SnapCenter(0.2f, 1f), Close);
            Assert.AreEqual(0.5f, PlacementMath.SnapCenter(0.9f, 1f), Close);
            Assert.AreEqual(1.5f, PlacementMath.SnapCenter(1.1f, 1f), Close);
            Assert.AreEqual(-0.5f, PlacementMath.SnapCenter(-0.2f, 1f), Close, "음수 쪽도 같은 규칙입니다.");
            Assert.AreEqual(-1.5f, PlacementMath.SnapCenter(-1.3f, 1f), Close);
        }

        [Test]
        public void 간격이_작아지면_칸의_사이에도_놓인다()
        {
            Assert.AreEqual(0f, PlacementMath.SnapCenter(0.2f, 0.5f), Close);
            Assert.AreEqual(0.5f, PlacementMath.SnapCenter(0.3f, 0.5f), Close);
            Assert.AreEqual(1f, PlacementMath.SnapCenter(0.8f, 0.5f), Close);
            Assert.AreEqual(-0.5f, PlacementMath.SnapCenter(-0.3f, 0.5f), Close);

            Assert.AreEqual(0.25f, PlacementMath.SnapCenter(0.3f, 0.25f), Close);
            Assert.AreEqual(0.5f, PlacementMath.SnapCenter(0.4f, 0.25f), Close);
            Assert.AreEqual(-0.75f, PlacementMath.SnapCenter(-0.7f, 0.25f), Close);
        }

        [Test]
        public void 끄면_값이_그대로다()
        {
            Assert.AreEqual(0.37f, PlacementMath.SnapCenter(0.37f, 0f), Close);

            var point = new Vector3(0.83f, 0f, -2.37f);
            AssertNear(point + Vector3.up * 0.5f, PlacementMath.SnapToGrid(point, Vector3.up, 0f), "끈 채로는 가리킨 면 위의 그 자리여야 합니다.");
        }

        [Test]
        public void 바닥을_가리키면_칸의_가운데_바닥_위에_놓인다()
        {
            var point = new Vector3(0.83f, 0f, -2.37f);

            AssertNear(new Vector3(0.5f, 0.5f, -2.5f), PlacementMath.SnapToGrid(point, Vector3.up, 1f));
            AssertNear(new Vector3(1f, 0.5f, -2.5f), PlacementMath.SnapToGrid(point, Vector3.up, 0.5f));
            AssertNear(new Vector3(0.75f, 0.5f, -2.25f), PlacementMath.SnapToGrid(point, Vector3.up, 0.25f));
        }

        [Test]
        public void 축과_나란한_벽면에서는_면에_붙은_채로_나머지만_맞춘다()
        {
            // x = 3.2에 서 있고 -x 쪽을 보는 벽. 블록은 벽에 닿은 채(가운데 x = 2.7)로 높이와 z만 모눈에 맞는다.
            Vector3 snapped = PlacementMath.SnapToGrid(new Vector3(3.2f, 0.7f, 1.3f), Vector3.left, 1f);

            AssertNear(new Vector3(2.7f, 0.5f, 1.5f), snapped);
        }

        [Test]
        public void 비스듬한_면에서는_세_쪽을_모두_맞춘다()
        {
            Vector3 normal = new Vector3(1f, 1f, 0f).normalized;

            Vector3 snapped = PlacementMath.SnapToGrid(new Vector3(2f, 2f, 0.2f), normal, 1f);

            AssertNear(new Vector3(2.5f, 2.5f, 0.5f), snapped);
        }

        [Test]
        public void 블록의_윗면을_가리키면_바로_위에_나란히_쌓인다()
        {
            // 칸에 맞지 않게 놓인 블록. 윗면의 어디를 가리켜도 간격이 1이면 바로 위에 쌓인다.
            var block = new Vector3(2.3f, 0.5f, -1.7f);

            Vector3 snapped = PlacementMath.SnapToBlock(block, Quaternion.identity, new Vector3(2.6f, 1f, -1.9f), Vector3.up, 1f);

            AssertNear(new Vector3(2.3f, 1.5f, -1.7f), snapped);
        }

        [Test]
        public void 블록의_옆면을_가리키면_한_변_떨어진_옆에_붙는다()
        {
            var block = new Vector3(2.3f, 0.5f, -1.7f);

            Vector3 right = PlacementMath.SnapToBlock(block, Quaternion.identity, new Vector3(2.8f, 0.7f, -1.6f), Vector3.right, 1f);
            Vector3 back = PlacementMath.SnapToBlock(block, Quaternion.identity, new Vector3(2.1f, 0.3f, -2.2f), Vector3.back, 1f);

            AssertNear(new Vector3(3.3f, 0.5f, -1.7f), right);
            AssertNear(new Vector3(2.3f, 0.5f, -2.7f), back);
        }

        [Test]
        public void 간격이_반이면_면_위에서_반_변씩_어긋나게_붙는다()
        {
            var block = new Vector3(2.3f, 0.5f, -1.7f);

            // 옆면의 위쪽을 가리킴: 블록의 가운데에서 위로 0.4, 앞으로 0.1 떨어진 곳.
            Vector3 snapped = PlacementMath.SnapToBlock(block, Quaternion.identity, new Vector3(2.8f, 0.9f, -1.6f), Vector3.right, 0.5f);

            AssertNear(new Vector3(3.3f, 1f, -1.7f), snapped);
        }

        [Test]
        public void 돌아간_블록에는_돌아간_방향을_따라_붙는다()
        {
            var block = new Vector3(0f, 0.5f, 0f);
            Quaternion rotation = Quaternion.Euler(0f, 30f, 0f);
            Vector3 normal = rotation * Vector3.right;
            Vector3 point = block + rotation * new Vector3(0.5f, 0.1f, 0.2f);

            Vector3 snapped = PlacementMath.SnapToBlock(block, rotation, point, normal, 1f);

            AssertNear(block + rotation * Vector3.right, snapped, "돌아간 블록의 옆면 쪽으로 정확히 한 변 떨어져야 합니다.");
            Assert.AreEqual(1f, Vector3.Distance(block, snapped), Close);
        }

        [Test]
        public void 면에_얹는_거리는_부품의_크기와_방향을_따른다()
        {
            var block = new Vector3(0.5f, 0.5f, 0.5f);
            var slab = new Vector3(0.5f, 0.25f, 0.5f);
            var pillar = new Vector3(0.25f, 0.5f, 0.25f);

            Assert.AreEqual(0.5f, PlacementMath.RestOffset(block, Quaternion.identity, Vector3.up), Close);
            Assert.AreEqual(0.25f, PlacementMath.RestOffset(slab, Quaternion.identity, Vector3.up), Close, "판은 바닥에서 반 높이의 반만큼 뜹니다.");
            Assert.AreEqual(0.5f, PlacementMath.RestOffset(slab, Quaternion.identity, Vector3.right), Close);
            Assert.AreEqual(0.25f, PlacementMath.RestOffset(pillar, Quaternion.identity, Vector3.forward), Close);

            // 좌우로 돌려도 바닥에 얹는 높이는 그대로다.
            Assert.AreEqual(0.25f, PlacementMath.RestOffset(slab, Quaternion.Euler(0f, 30f, 0f), Vector3.up), Close);

            // 45도 돌린 블록을 벽에 대면 모서리가 닿으므로 반 변보다 멀다.
            Assert.AreEqual(0.5f * Mathf.Sqrt(2f), PlacementMath.RestOffset(block, Quaternion.Euler(0f, 45f, 0f), Vector3.right), Close);
        }

        [Test]
        public void 부품을_면에_얹으면_파고들지_않고_닿는다()
        {
            var slab = new Vector3(0.5f, 0.25f, 0.5f);
            var point = new Vector3(1.3f, 0f, -0.7f);

            AssertNear(new Vector3(1.3f, 0.25f, -0.7f), PlacementMath.RestOn(point, Vector3.up, slab, Quaternion.identity));
            AssertNear(new Vector3(1.3f, 0.25f, -0.7f), PlacementMath.RestOn(point, Vector3.up, slab, Quaternion.Euler(0f, 75f, 0f)));
        }

        [Test]
        public void 크기가_다른_부품도_모눈에_맞춘다()
        {
            // 판을 바닥에: 높이는 바닥에 붙고(0.25), 가로세로는 칸의 가운데.
            Vector3 snapped = PlacementMath.SnapToGrid(new Vector3(0.83f, 0f, -2.37f), Vector3.up, 1f, 0.25f);

            AssertNear(new Vector3(0.5f, 0.25f, -2.5f), snapped);
        }

        [Test]
        public void 블록_위에_판을_맞춰_얹으면_두_부품의_반_높이를_더한_만큼_올라간다()
        {
            var block = new Vector3(2.3f, 0.5f, -1.7f);
            var blockHalf = new Vector3(0.5f, 0.5f, 0.5f);
            var slabHalf = new Vector3(0.5f, 0.25f, 0.5f);

            Vector3 snapped = PlacementMath.SnapToBlock(block, Quaternion.identity, blockHalf, new Vector3(2.6f, 1f, -1.9f), Vector3.up, 1f, slabHalf);

            AssertNear(new Vector3(2.3f, 1.25f, -1.7f), snapped, "판의 아랫면이 블록의 윗면에 딱 붙어야 합니다.");
        }

        [Test]
        public void 판_위에_블록을_맞춰_얹거나_기둥을_옆에_붙인다()
        {
            var slab = new Vector3(0f, 0.25f, 0f);
            var slabHalf = new Vector3(0.5f, 0.25f, 0.5f);
            var blockHalf = new Vector3(0.5f, 0.5f, 0.5f);
            var pillarHalf = new Vector3(0.25f, 0.5f, 0.25f);

            Vector3 onTop = PlacementMath.SnapToBlock(slab, Quaternion.identity, slabHalf, new Vector3(0.2f, 0.5f, 0.1f), Vector3.up, 1f, blockHalf);
            AssertNear(new Vector3(0f, 1f, 0f), onTop);

            // 기둥을 판의 옆면에: 반 굵기(0.25)만큼만 떨어진다. 높이는 간격의 배수로만 맞추므로 판의 가운데 높이다.
            Vector3 beside = PlacementMath.SnapToBlock(slab, Quaternion.identity, slabHalf, new Vector3(0.5f, 0.3f, 0.1f), Vector3.right, 1f, pillarHalf);
            AssertNear(new Vector3(0.75f, 0.25f, 0f), beside);
        }

        [Test]
        public void 블록에_맞추기를_끄면_면_위의_그_자리다()
        {
            var block = new Vector3(2.3f, 0.5f, -1.7f);
            var point = new Vector3(2.6f, 1f, -1.9f);

            Vector3 snapped = PlacementMath.SnapToBlock(block, Quaternion.identity, point, Vector3.up, 0f);

            AssertNear(point + Vector3.up * 0.5f, snapped);
        }
    }
}
