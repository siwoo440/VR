using System.Collections;
using System.IO;
using AtelierVerse.UI;
using AtelierVerse.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 날기(만들기 시점)를 실제로 실행해 확인한다. 캐릭터는 (0.5, 0, -5.5)에서 +z 쪽을 보고 시작한다.
    /// </summary>
    public class FlyPlayTests : PlayTestBase
    {
        private const int SceneBlockCount = 19;
        private const float RiseSeconds = 0.6f;
        private const float HoverSeconds = 0.6f;
        private const float WalkSeconds = 1f;

        private NoticeBar notice;
        private TMP_Text modeLabel;

        [UnityTest]
        public IEnumerator V_키로_날기를_켜고_끈다()
        {
            yield return LoadFlyScene();
            Assert.IsFalse(player.Motor.IsFlying);
            Assert.AreEqual(GameUi.WalkLabel, modeLabel.text);

            yield return Tap(keyboard.vKey);
            Assert.IsTrue(player.Motor.IsFlying);
            Assert.AreEqual(GameUi.FlyLabel, modeLabel.text);
            Assert.AreEqual(GameUi.FlyOnMessage, notice.Message);

            yield return Tap(keyboard.vKey);
            Assert.IsFalse(player.Motor.IsFlying);
            Assert.AreEqual(GameUi.WalkLabel, modeLabel.text);
            Assert.AreEqual(GameUi.FlyOffMessage, notice.Message);
        }

        [UnityTest]
        public IEnumerator 날_때는_Space로_오르고_떨어지지_않으며_Shift로_내려온다()
        {
            yield return LoadFlyScene();
            yield return Tap(keyboard.vKey);
            float ground = player.transform.position.y;

            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(RiseSeconds);
            Release(keyboard.spaceKey);
            yield return null;
            float risen = player.transform.position.y;
            Assert.Greater(risen - ground, 2f, "Space를 누르는 동안 충분히 오르지 않았습니다.");

            yield return new WaitForSeconds(HoverSeconds);
            Assert.AreEqual(risen, player.transform.position.y, 0.05f, "날고 있는데 떨어졌습니다.");

            Press(keyboard.leftShiftKey);
            yield return new WaitForSeconds(0.3f);
            Release(keyboard.leftShiftKey);
            yield return null;
            Assert.Less(player.transform.position.y, risen - 1f, "Shift를 누르는 동안 내려오지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 날_때_W로_앞으로_가고_점프_높이보다_높이_오를_수_있다()
        {
            yield return LoadFlyScene();
            yield return Tap(keyboard.vKey);

            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(RiseSeconds);
            Release(keyboard.spaceKey);
            yield return null;
            Assert.Greater(player.transform.position.y, 2.5f, "점프로는 닿지 못할 높이에 있어야 합니다.");

            Vector3 before = player.transform.position;
            Press(keyboard.wKey);
            yield return new WaitForSeconds(WalkSeconds);
            Release(keyboard.wKey);
            yield return null;

            float moved = Vector3.Dot(player.transform.position - before, player.transform.forward);
            Assert.That(moved, Is.InRange(player.Motor.FlySpeed - 1.5f, player.Motor.FlySpeed + 1.5f), "날 때의 속도로 1초 동안 간 거리와 다릅니다.");
            Assert.AreEqual(before.y, player.transform.position.y, 0.05f, "앞으로 가는 동안 높이가 바뀌었습니다.");
        }

        [UnityTest]
        public IEnumerator 날기를_끄면_그_자리에서_떨어져_바닥에_선다()
        {
            yield return LoadFlyScene();
            yield return Tap(keyboard.vKey);
            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(RiseSeconds);
            Release(keyboard.spaceKey);
            yield return null;
            Assert.Greater(player.transform.position.y, 2f);

            yield return Tap(keyboard.vKey);
            yield return new WaitForSeconds(1.5f);

            Assert.IsTrue(player.GetComponent<CharacterController>().isGrounded, "걷기로 돌아왔는데 바닥에 닿지 않았습니다.");
            Assert.That(player.transform.position.y, Is.InRange(-0.1f, 0.3f));
        }

        [UnityTest]
        public IEnumerator 메뉴가_열려_있으면_V가_동작하지_않고_날던_캐릭터는_멈춰_있는다()
        {
            yield return LoadFlyScene();
            ui.OpenMenu();
            yield return null;
            yield return Tap(keyboard.vKey);
            Assert.IsFalse(player.Motor.IsFlying);
            ui.CloseMenu();
            yield return null;

            yield return Tap(keyboard.vKey);
            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(RiseSeconds);
            Release(keyboard.spaceKey);
            yield return null;
            float height = player.transform.position.y;

            ui.OpenMenu();
            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(0.4f);
            Release(keyboard.spaceKey);
            yield return null;

            Assert.IsTrue(player.Motor.IsFlying);
            Assert.AreEqual(height, player.transform.position.y, 0.05f, "메뉴가 열린 동안 움직였습니다.");
        }

        [UnityTest]
        public IEnumerator 시작_위치로_돌아가면_날기가_꺼진다()
        {
            yield return LoadFlyScene();
            yield return Tap(keyboard.vKey);
            Assert.IsTrue(player.Motor.IsFlying);

            player.Motor.Respawn();
            yield return null;

            Assert.IsFalse(player.Motor.IsFlying);
            Assert.AreEqual(GameUi.WalkLabel, modeLabel.text);
        }

        [UnityTest]
        public IEnumerator 날면서도_블록을_놓을_수_있다()
        {
            yield return LoadFlyScene();
            BlockWorld world = Object.FindAnyObjectByType<BlockWorld>();
            yield return Tap(keyboard.vKey);
            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(0.3f);
            Release(keyboard.spaceKey);
            yield return null;

            yield return AimWithPart(60f, keyboard.digit1Key);
            yield return Tap(mouse.leftButton);

            Assert.AreEqual(SceneBlockCount + 1, world.Count, "날면서 블록이 놓이지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 키_안내와_도움말에_날기가_있다()
        {
            yield return LoadFlyScene();

            Assert.IsNotNull(Find<Transform>(ui, "Key_V"), "키 안내에 V가 없습니다.");

            Transform help = Find<Transform>(ui.Menu, "HelpPage");
            bool listed = false;
            foreach (TMP_Text text in help.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.text == "날기 켜고 끄기") listed = true;
            }

            Assert.IsTrue(listed, "도움말에 날기 줄이 없습니다.");
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadFlyScene();
            BeginCapture();

            yield return Tap(keyboard.vKey);
            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(0.7f);
            Release(keyboard.spaceKey);
            yield return null;

            player.CaptureLook(true);
            player.SetLook(20f, 35f);
            player.Rig.SetViewDistance(5f);
            yield return new WaitForSeconds(0.6f);
            SaveCapture(Path.Combine(directory, "fly-third-person.png"));
        }

        private IEnumerator LoadFlyScene()
        {
            yield return LoadSandbox();

            notice = Object.FindAnyObjectByType<NoticeBar>();
            modeLabel = Find<Transform>(ui, "ModeChip").GetComponentInChildren<TMP_Text>();
            Assert.IsNotNull(notice, "게임 화면에 알림 띠가 없습니다.");
            yield return new WaitForSeconds(0.5f);
        }
    }
}
