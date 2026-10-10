using System.Collections;
using AtelierVerse.Player;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// VR에서 끌어서 놓기와 지우기를 가상 기기로 확인한다(21일차): 방아쇠를 당긴 채 손을 옮기면 이어 놓이고, 한 번에 되돌아온다.
    /// 실제 컨트롤러로 끌 때 손이 떨려 줄이 어긋나지 않는지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class XrDragPlayTests : XrPlayTestBase
    {
        private const int GoldPart = 0;

        // 캐릭터가 처음 서는 자리(0.5, 0, -5.5)의 3m 앞 빈 바닥.
        private static readonly Vector3 FloorPoint = new Vector3(0.5f, 0f, -2.5f);

        private BlockWorld world;
        private BlockBuilder builder;

        [UnityTest]
        public IEnumerator 방아쇠를_당긴_채_손을_옮기면_이어_놓이고_한_번에_되돌아온다()
        {
            yield return LoadVrBuild();
            ui.Hotbar.Select(GoldPart);
            int before = world.Count;
            int undoBefore = world.History.UndoCount;

            yield return PointRightHandAt(FloorPoint);
            Assert.IsTrue(builder.CanPlaceAtTarget);

            Press(Button(rightController, "triggerPressed"));
            yield return Frames(3);
            Assert.AreEqual(before + 1, world.Count, "방아쇠를 당긴 그때 첫 블록이 놓여야 합니다.");
            Assert.AreEqual(StrokeKind.Place, builder.Stroke);
            Vector3 first = FloorPoint + Vector3.up * 0.5f;
            Assert.IsTrue(HasBlockNear(world, first, 0.02f));
            yield return Hold();

            // 방아쇠를 당긴 채 오른쪽으로 2만큼 떨어진 바닥을 가리킨다.
            yield return PointRightHandAt(FloorPoint + Vector3.right * 2f);
            Assert.AreEqual(before + 3, world.Count, "지나온 칸까지 이어 놓여야 합니다.");
            Assert.IsTrue(HasBlockNear(world, first + Vector3.right, 0.02f), "사이의 칸이 비었습니다.");
            Assert.IsTrue(HasBlockNear(world, first + Vector3.right * 2f, 0.02f));
            Assert.AreEqual(undoBefore, world.History.UndoCount);

            Release(Button(rightController, "triggerPressed"));
            yield return Frames(3);
            Assert.IsFalse(builder.IsStroking);
            Assert.AreEqual(undoBefore + 1, world.History.UndoCount, "한 번 끈 것은 기록 하나여야 합니다.");

            Assert.IsTrue(world.History.Undo());
            yield return Frames(2);
            Assert.AreEqual(before, world.Count, "되돌리기 한 번에 모두 돌아와야 합니다.");
        }

        [UnityTest]
        public IEnumerator 옆_단추를_쥔_채_손을_옮기면_같은_면의_블록이_이어_지워진다()
        {
            yield return LoadVrBuild();
            ui.Hotbar.Select(GoldPart);

            // 바닥에 블록 셋을 가로로 놓는다. 윗면이 같은 높이다.
            var ids = new int[3];
            for (int i = 0; i < ids.Length; i++)
            {
                Assert.AreEqual(PlaceResult.Ok, world.Add(GoldPart, new Vector3(0.5f + i, 0.5f, -2.5f), Quaternion.identity, out ids[i]));
            }

            // 물리 갱신 도중에는 가상 기기의 값을 넣을 수 없으므로 보통 프레임으로 돌아온 뒤에 손을 옮긴다.
            yield return new WaitForFixedUpdate();
            yield return null;
            int undoBefore = world.History.UndoCount;

            // 첫 블록의 윗면을 가리키고 옆 단추를 쥔 채, 나머지 블록의 윗면으로 옮긴다.
            yield return PointRightHandAt(new Vector3(0.5f, 1f, -2.5f));
            Assert.IsTrue(builder.HasBlockTarget);
            Press(Button(rightController, "gripPressed"));
            yield return Frames(3);
            Assert.IsFalse(world.Has(ids[0]));
            Assert.AreEqual(StrokeKind.Remove, builder.Stroke);
            yield return Hold();

            yield return PointRightHandAt(new Vector3(1.5f, 1f, -2.5f));
            yield return PointRightHandAt(new Vector3(2.5f, 1f, -2.5f));
            Release(Button(rightController, "gripPressed"));
            yield return Frames(3);

            foreach (int id in ids) Assert.IsFalse(world.Has(id), "같은 면의 블록이 지워지지 않았습니다.");
            Assert.AreEqual(undoBefore + 1, world.History.UndoCount);

            Assert.IsTrue(world.History.Undo());
            foreach (int id in ids) Assert.IsTrue(world.Has(id));
        }

        /// <summary>당긴 뒤 끌기로 보기 시작할 때까지(BlockBuilder.StrokeHoldDelay) 그대로 당기고 있는다.</summary>
        private static IEnumerator Hold()
        {
            yield return new WaitForSeconds(BlockBuilder.StrokeHoldDelay + 0.08f);
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
