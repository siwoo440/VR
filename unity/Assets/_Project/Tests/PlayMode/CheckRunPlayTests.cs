using System.Collections;
using AtelierVerse.Core;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 빌드를 자동으로 확인하는 실행에서 사람의 입력을 받지 않는지 확인한다(20일차).
    /// 확인하는 창이 떠 있는 동안 들어온 누름으로 실제 맵에 블록이 놓인 일이 있어서 넣었다.
    /// </summary>
    public class CheckRunPlayTests : PlayTestBase
    {
        public override void TearDown()
        {
            CheckRunInput.Unblock();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator 자동_확인_실행에서는_키보드와_마우스가_듣지_않는다()
        {
            yield return LoadSandbox();
            BlockWorld world = Object.FindAnyObjectByType<BlockWorld>();
            int blocks = world.Count;
            Vector3 start = player.transform.position;
            float yaw = player.transform.eulerAngles.y;

            CheckRunInput.Block();
            Assert.IsTrue(CheckRunInput.IsBlocking);
            Assert.IsFalse(keyboard.enabled);
            Assert.IsFalse(mouse.enabled);

            // 화면을 눌러도 마우스를 잡지 않고, 부품을 골라 눌러도 블록이 놓이지 않는다.
            yield return Tap(mouse.leftButton);
            Assert.IsFalse(player.LookCaptured, "막고 있는데 화면을 누르자 마우스를 잡았습니다.");
            yield return Tap(keyboard.digit2Key);
            yield return Tap(mouse.leftButton);
            Set(mouse.delta, new Vector2(120f, 0f));
            yield return Frames(2);
            Set(mouse.delta, Vector2.zero);

            // 걷지도 않고 메뉴도 열리지 않는다.
            Press(keyboard.wKey);
            yield return new WaitForSeconds(0.4f);
            Release(keyboard.wKey);
            yield return Tap(keyboard.escapeKey);

            Assert.AreEqual(blocks, world.Count, "막고 있는데 블록이 놓였습니다.");
            Assert.Less(Vector3.Distance(start, player.transform.position), 0.05f, "막고 있는데 걸었습니다.");
            Assert.AreEqual(0f, Mathf.DeltaAngle(yaw, player.transform.eulerAngles.y), 0.01f);
            Assert.IsFalse(ui.IsMenuOpen);

            // 확인하는 동안 새로 꽂힌 기기도 듣지 않는다.
            Mouse second = InputSystem.AddDevice<Mouse>();
            Assert.IsFalse(second.enabled);

            // 풀면 다시 듣는다.
            CheckRunInput.Unblock();
            Assert.IsFalse(CheckRunInput.IsBlocking);
            Assert.IsTrue(keyboard.enabled);
            Assert.IsTrue(mouse.enabled);
            yield return Tap(mouse.leftButton);
            Assert.IsTrue(player.LookCaptured, "풀었는데 화면을 눌러도 마우스를 잡지 않습니다.");
        }
    }
}
