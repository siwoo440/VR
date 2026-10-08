using AtelierVerse.UI;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// VR의 부품 판을 왼손 위에 놓는 계산의 검사. 판은 손 위에 떠서 머리 쪽을 본다.
    /// </summary>
    public class XrHandPaletteTests
    {
        [Test]
        public void 판은_손_위에_뜨고_머리_쪽을_본다()
        {
            var hand = new Vector3(-0.25f, 1.1f, 0.4f);
            var head = new Vector3(0f, 1.6f, 0f);

            XrHandPalette.PoseAbove(hand, head, 0.14f, Vector3.forward, out Vector3 position, out Quaternion rotation);

            Assert.Less(Vector3.Distance(new Vector3(-0.25f, 1.24f, 0.4f), position), 0.0001f);
            Assert.Less(Vector3.Angle(position - head, rotation * Vector3.forward), 0.1f, "판의 앞면이 머리를 향하려면 판의 앞(+z)이 머리에서 멀어지는 쪽이어야 합니다.");
        }

        [Test]
        public void 손이_어느_쪽에_있어도_머리_쪽을_본다()
        {
            var head = new Vector3(2f, 1.5f, -1f);
            var hand = new Vector3(2.4f, 1.0f, -1.5f);

            XrHandPalette.PoseAbove(hand, head, 0.2f, Vector3.forward, out Vector3 position, out Quaternion rotation);

            Assert.Less(Vector3.Angle(position - head, rotation * Vector3.forward), 0.1f);
            Assert.Greater(Vector3.Dot(rotation * Vector3.up, Vector3.up), 0.5f, "판이 뒤집히면 안 됩니다.");
        }

        [Test]
        public void 머리와_판이_같은_자리면_대신_준_방향을_본다()
        {
            var hand = new Vector3(0f, 1f, 0f);
            var head = new Vector3(0f, 1.14f, 0f);

            XrHandPalette.PoseAbove(hand, head, 0.14f, Vector3.right, out _, out Quaternion rotation);

            Assert.Less(Vector3.Angle(Vector3.right, rotation * Vector3.forward), 0.1f);
        }
    }
}
