using AtelierVerse.Player;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 날 때의 속도 계산 검사. 위아래 키와 앞뒤 입력을 합쳐도 속도가 일정한지 확인한다.
    /// </summary>
    public class FlyMoveTests
    {
        private const float Speed = 7f;

        [Test]
        public void Space를_누르면_위로_오르고_Shift를_누르면_내려온다()
        {
            Assert.AreEqual(new Vector3(0f, Speed, 0f), CharacterMotor.ComposeFlyMove(Vector3.zero, true, false, Speed));
            Assert.AreEqual(new Vector3(0f, -Speed, 0f), CharacterMotor.ComposeFlyMove(Vector3.zero, false, true, Speed));
        }

        [Test]
        public void 둘_다_누르면_오르내리지_않는다()
        {
            Assert.AreEqual(Vector3.zero, CharacterMotor.ComposeFlyMove(Vector3.zero, true, true, Speed));
        }

        [Test]
        public void 앞으로_가며_오르면_대각선이_더_빠르지_않다()
        {
            Vector3 velocity = CharacterMotor.ComposeFlyMove(Vector3.forward, true, false, Speed);

            Assert.AreEqual(Speed, velocity.magnitude, 0.001f);
            Assert.Greater(velocity.y, 0f);
            Assert.Greater(velocity.z, 0f);
        }

        [Test]
        public void 입력이_없으면_제자리에_머문다()
        {
            Assert.AreEqual(Vector3.zero, CharacterMotor.ComposeFlyMove(Vector3.zero, false, false, Speed));
        }
    }
}
