using System.Collections;
using AtelierVerse.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 몸(CharacterMotor)이 조작 스크립트 없이도 움직이는지 실제로 실행해 확인한다.
    /// 시험용 캐릭터는 캐릭터 컨트롤러와 모터만 붙여 (-6.5, 0, -6.5)에 세운다. 그 둘레에는 블록이나 나무가 없다.
    /// </summary>
    public class MotorPlayTests : PlayTestBase
    {
        private const float SettleSeconds = 0.5f;
        private const float MoveSeconds = 1f;

        private static readonly Vector3 PuppetStart = new Vector3(-6.5f, 0.05f, -6.5f);

        [UnityTest]
        public IEnumerator PC_캐릭터는_몸과_카메라_리그와_조작으로_나뉘어_있다()
        {
            yield return LoadSandbox();

            Assert.IsNotNull(player.Motor, "PC 캐릭터에 몸(CharacterMotor)이 없습니다.");
            Assert.IsNotNull(player.Rig, "PC 캐릭터에 카메라 리그(ViewRig)가 없습니다.");
            Assert.AreSame(player.gameObject, player.Motor.gameObject);
            Assert.IsNotNull(player.Motor.Avatar, "몸에 겉모습이 연결되지 않았습니다.");
            Assert.IsNotNull(player.Rig.ViewCamera, "카메라 리그에 카메라가 연결되지 않았습니다.");
            Assert.AreEqual(player.Rig.HeadPosition.y, player.transform.position.y + 1.55f, 0.01f);
        }

        [UnityTest]
        public IEnumerator 조작_없이_몸만_있는_캐릭터가_정해_준_방향으로_걷고_멈춘다()
        {
            yield return LoadSandbox();
            CharacterMotor puppet = CreatePuppet();
            Assert.IsNull(puppet.GetComponent<DesktopPlayerController>());
            yield return new WaitForSeconds(SettleSeconds);
            Assert.IsTrue(puppet.IsGrounded, "몸만 있는 캐릭터가 바닥에 서지 않았습니다.");

            Vector3 before = puppet.transform.position;
            puppet.MoveDirection = Vector3.right;
            yield return new WaitForSeconds(MoveSeconds);
            puppet.Stop();
            yield return null;

            Vector3 stopped = puppet.transform.position;
            Assert.That(stopped.x - before.x, Is.InRange(puppet.WalkSpeed - 1f, puppet.WalkSpeed + 1f), "걷는 속도로 1초 동안 간 거리와 다릅니다.");
            Assert.AreEqual(before.z, stopped.z, 0.05f, "정해 준 방향이 아닌 쪽으로 움직였습니다.");

            yield return new WaitForSeconds(0.3f);
            Vector3 drift = puppet.transform.position - stopped;
            drift.y = 0f;
            Assert.Less(drift.magnitude, 0.05f, "멈추라고 한 뒤에도 계속 움직입니다.");
        }

        [UnityTest]
        public IEnumerator 몸만_있는_캐릭터도_달리고_뛰어오른다()
        {
            yield return LoadSandbox();
            CharacterMotor puppet = CreatePuppet();
            yield return new WaitForSeconds(SettleSeconds);

            Vector3 before = puppet.transform.position;
            puppet.MoveDirection = Vector3.right;
            puppet.Sprint = true;
            yield return new WaitForSeconds(MoveSeconds);
            puppet.Stop();
            yield return null;
            Assert.That(puppet.transform.position.x - before.x, Is.InRange(puppet.SprintSpeed - 1.5f, puppet.SprintSpeed + 1.5f), "달리는 속도로 1초 동안 간 거리와 다릅니다.");

            yield return new WaitForSeconds(0.2f);
            float ground = puppet.transform.position.y;
            puppet.Jump();
            yield return new WaitForSeconds(0.25f);
            Assert.Greater(puppet.transform.position.y, ground + 0.3f, "뛰어오르지 않았습니다.");

            yield return new WaitForSeconds(1f);
            Assert.IsTrue(puppet.IsGrounded, "뛰어오른 뒤 바닥에 내려서지 않았습니다.");
            Assert.AreEqual(ground, puppet.transform.position.y, 0.1f);
        }

        [UnityTest]
        public IEnumerator 방향의_길이가_1을_넘어도_더_빠르지_않다()
        {
            yield return LoadSandbox();
            CharacterMotor puppet = CreatePuppet();
            yield return new WaitForSeconds(SettleSeconds);

            Vector3 before = puppet.transform.position;
            puppet.MoveDirection = new Vector3(3f, 0f, 3f);
            yield return new WaitForSeconds(MoveSeconds);
            puppet.Stop();
            yield return null;

            Vector3 moved = puppet.transform.position - before;
            moved.y = 0f;
            Assert.That(moved.magnitude, Is.InRange(puppet.WalkSpeed - 1f, puppet.WalkSpeed + 1f));
        }

        [UnityTest]
        public IEnumerator 몸만_있는_캐릭터도_날고_끄면_떨어진다()
        {
            yield return LoadSandbox();
            CharacterMotor puppet = CreatePuppet();
            yield return new WaitForSeconds(SettleSeconds);

            int changes = 0;
            puppet.FlyModeChanged += _ => changes++;

            puppet.SetFlying(true);
            puppet.Ascend = true;
            yield return new WaitForSeconds(0.5f);
            puppet.Stop();
            yield return null;
            float risen = puppet.transform.position.y;
            Assert.Greater(risen, 2f, "오르라고 한 동안 충분히 오르지 않았습니다.");

            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(risen, puppet.transform.position.y, 0.05f, "날고 있는데 떨어졌습니다.");
            Assert.IsTrue(puppet.IsFlying, "멈추라고 한 것만으로 날기가 꺼지면 안 됩니다.");

            puppet.SetFlying(false);
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(puppet.IsGrounded, "날기를 껐는데 바닥에 내려서지 않았습니다.");
            Assert.AreEqual(2, changes);
        }

        [UnityTest]
        public IEnumerator 몸의_방향을_정하고_돌릴_수_있다()
        {
            yield return LoadSandbox();
            CharacterMotor puppet = CreatePuppet();
            yield return null;

            puppet.SetYaw(90f);
            Assert.Less(Vector3.Angle(Vector3.right, puppet.transform.forward), 0.5f);

            puppet.Turn(90f);
            Assert.Less(Vector3.Angle(Vector3.back, puppet.transform.forward), 0.5f);
        }

        [UnityTest]
        public IEnumerator PC_조작을_끄면_몸이_멈춘다()
        {
            yield return LoadSandbox();
            yield return new WaitForSeconds(SettleSeconds);

            Vector3 start = player.transform.position;
            Press(keyboard.wKey);
            yield return new WaitForSeconds(0.4f);
            Assert.Greater(Vector3.Dot(player.transform.position - start, player.transform.forward), 0.5f, "W를 눌렀는데 걷지 않았습니다.");

            player.enabled = false;
            yield return null;
            yield return null;
            Vector3 stopped = player.transform.position;

            yield return new WaitForSeconds(0.4f);
            Release(keyboard.wKey);
            Vector3 drift = player.transform.position - stopped;
            drift.y = 0f;
            Assert.Less(drift.magnitude, 0.05f, "조작을 껐는데 몸이 계속 걷습니다.");
            Assert.IsTrue(player.Motor.IsGrounded, "조작을 꺼도 몸은 바닥에 서 있어야 합니다.");
        }

        /// <summary>캐릭터 컨트롤러와 모터만 붙인 시험용 캐릭터. 크기는 PC 캐릭터와 같다.</summary>
        private static CharacterMotor CreatePuppet()
        {
            var root = new GameObject("Puppet");
            root.transform.position = PuppetStart;

            var body = root.AddComponent<CharacterController>();
            body.height = 1.7f;
            body.radius = 0.3f;
            body.center = new Vector3(0f, 0.85f, 0f);
            body.stepOffset = 0.35f;
            body.minMoveDistance = 0f;

            return root.AddComponent<CharacterMotor>();
        }
    }
}
