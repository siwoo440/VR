using System.Collections;
using System.Collections.Generic;
using AtelierVerse.Core;
using AtelierVerse.Player;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// VR에서도 같은 소리가 올라오는지를 가상 기기로 확인한다(19일차): 오른손으로 놓은 블록의 자리에서 나는 소리,
    /// 부품 판의 단추를 눌렀을 때의 소리. 헤드셋에서 소리의 방향과 크기가 알맞은지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class XrSoundPlayTests : XrPlayTestBase
    {
        private readonly List<(SfxId id, Vector3? position)> heard = new List<(SfxId id, Vector3? position)>();

        public override void Setup()
        {
            base.Setup();
            heard.Clear();
            Sfx.Played += Listen;
        }

        public override void TearDown()
        {
            Sfx.Played -= Listen;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator VR에서_놓은_블록의_자리에서_소리가_나고_부품_판의_단추에도_소리가_난다()
        {
            yield return LoadVr();
            BlockBuilder builder = player.GetComponent<BlockBuilder>();
            Set(headset.centerEyePosition, new Vector3(0f, 1.6f, 0f));
            yield return RaiseLeftHand();

            ui.Hotbar.Select(0);
            yield return PointRightHandAt(new Vector3(0.5f, 0f, -2.5f));
            Assert.IsTrue(builder.HasTarget);
            Vector3 target = builder.TargetPosition;
            heard.Clear();

            yield return PullTrigger();
            Assert.AreEqual(1, Count(SfxId.Place), "방아쇠로 놓았는데 놓는 소리가 나지 않았습니다.");
            (SfxId _, Vector3? position) = heard.Find(sound => sound.id == SfxId.Place);
            Assert.IsTrue(position.HasValue, "VR에서 방향을 알 수 있게 블록의 자리에서 나야 합니다.");
            Assert.Less(Vector3.Distance(target, position.Value), 0.05f);

            // 부품 판의 되돌리기 단추: 단추 소리가 아니라 되돌리는 소리 하나만 난다.
            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Palette, "Undo")));
            heard.Clear();
            yield return PullTrigger();
            Assert.AreEqual(1, Count(SfxId.Undo));
            Assert.AreEqual(0, Count(SfxId.Click), "되돌리는 소리와 단추 소리가 겹쳤습니다.");

            // 부품 판의 칸을 누르면 고르는 소리가 난다.
            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Palette, "Slot2")));
            heard.Clear();
            yield return PullTrigger();
            Assert.AreEqual(1, Count(SfxId.Select));
            Assert.AreEqual(0, Count(SfxId.Click));
        }

        private void Listen(SfxId id, Vector3? position)
        {
            heard.Add((id, position));
        }

        private int Count(SfxId id)
        {
            int count = 0;
            foreach ((SfxId heardId, Vector3? _) in heard)
            {
                if (heardId == id) count++;
            }

            return count;
        }
    }
}
