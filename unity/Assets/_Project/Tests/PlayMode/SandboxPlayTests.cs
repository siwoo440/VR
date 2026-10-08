using System.Collections;
using AtelierVerse.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 씬을 실제로 실행해 캐릭터가 바닥에 서고 키 입력으로 걷는지 확인한다.
    /// 입력은 InputTestFixture가 만든 가상 키보드로 넣으므로 에디터 창이 없어도 실행된다.
    /// </summary>
    public class SandboxPlayTests : InputTestFixture
    {
        private const string BootScene = "Boot";
        private const string SandboxScene = "Sandbox";
        private const float SettleSeconds = 0.5f;
        private const float WalkSeconds = 1f;
        private const float SceneWaitSeconds = 5f;
        private const int TestFrameRate = 60;

        private int previousFrameRate;

        /// <summary>창 없이 실행하면 초당 수천 프레임으로 돌아 실제 화면과 조건이 달라지므로 60으로 맞춘다.</summary>
        public override void Setup()
        {
            base.Setup();
            previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = TestFrameRate;
        }

        public override void TearDown()
        {
            Application.targetFrameRate = previousFrameRate;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Boot_씬은_Sandbox_씬으로_넘어간다()
        {
            yield return SceneManager.LoadSceneAsync(BootScene, LoadSceneMode.Single);

            float deadline = Time.realtimeSinceStartup + SceneWaitSeconds;
            while (SceneManager.GetActiveScene().name != SandboxScene && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(SandboxScene, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator 캐릭터는_시작하면_바닥에_선다()
        {
            yield return LoadSandbox();
            DesktopPlayerController player = FindPlayer();
            yield return new WaitForSeconds(SettleSeconds);

            Assert.IsTrue(player.GetComponent<CharacterController>().isGrounded, "캐릭터가 바닥에 닿아 있지 않습니다.");
            Assert.That(player.transform.position.y, Is.InRange(-0.1f, 0.3f));
        }

        [UnityTest]
        public IEnumerator W_키를_누르고_있으면_앞으로_걷고_놓으면_멈춘다()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadSandbox();
            DesktopPlayerController player = FindPlayer();
            yield return new WaitForSeconds(SettleSeconds);

            Vector3 before = player.transform.position;
            Vector3 forward = player.transform.forward;

            Press(keyboard.wKey);
            yield return new WaitForSeconds(WalkSeconds);
            Release(keyboard.wKey);
            yield return null;

            Vector3 released = player.transform.position;
            float walked = Vector3.Dot(released - before, forward);
            Assert.That(walked, Is.InRange(2.5f, 4.5f), "걷는 속도 3.5m/s로 1초 동안 걸은 거리와 다릅니다.");

            yield return new WaitForSeconds(0.3f);

            Vector3 drift = player.transform.position - released;
            drift.y = 0f;
            Assert.Less(drift.magnitude, 0.05f, "키를 놓은 뒤에도 계속 움직입니다.");
        }

        [UnityTest]
        public IEnumerator 프레임_수_제한이_없어도_같은_속도로_걷는다()
        {
            Application.targetFrameRate = -1;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadSandbox();
            DesktopPlayerController player = FindPlayer();
            yield return new WaitForSeconds(SettleSeconds);

            Vector3 before = player.transform.position;

            Press(keyboard.wKey);
            yield return new WaitForSeconds(WalkSeconds);
            Release(keyboard.wKey);
            yield return null;

            float walked = Vector3.Dot(player.transform.position - before, player.transform.forward);
            Assert.That(walked, Is.InRange(2.5f, 4.5f), "한 프레임 이동량이 아주 작을 때 캐릭터가 제대로 움직이지 않습니다.");
        }

        [UnityTest]
        public IEnumerator 바닥_밖으로_떨어지면_시작_위치로_돌아온다()
        {
            yield return LoadSandbox();
            DesktopPlayerController player = FindPlayer();
            yield return new WaitForSeconds(SettleSeconds);

            Vector3 start = player.transform.position;
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.position = new Vector3(40f, -11f, 40f);
            body.enabled = true;

            yield return new WaitForSeconds(SettleSeconds);

            Vector3 offset = player.transform.position - start;
            Assert.Less(offset.magnitude, 0.5f, "시작 위치로 돌아오지 않았습니다.");
        }

        private static IEnumerator LoadSandbox()
        {
            yield return SceneManager.LoadSceneAsync(SandboxScene, LoadSceneMode.Single);
            yield return null;
        }

        private static DesktopPlayerController FindPlayer()
        {
            var player = Object.FindAnyObjectByType<DesktopPlayerController>();
            Assert.IsNotNull(player, "Sandbox 씬에서 PC 캐릭터를 찾을 수 없습니다.");
            return player;
        }
    }
}
