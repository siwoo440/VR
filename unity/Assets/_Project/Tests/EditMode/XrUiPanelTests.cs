using AtelierVerse.UI;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// VR에서 화면을 눈앞의 판으로 놓는 계산의 검사. 판은 머리가 보는 수평 방향의 앞에 놓이고, 거리에 비례해 커진다.
    /// </summary>
    public class XrUiPanelTests
    {
        [Test]
        public void 판은_머리가_보는_수평_방향의_앞에_눈높이보다_조금_아래로_놓인다()
        {
            XrUiPanel.PoseInFront(new Vector3(1f, 1.6f, 2f), Vector3.right, Vector3.forward, 1.6f, 0.1f, out Vector3 position, out Quaternion rotation);

            Assert.Less(Vector3.Distance(new Vector3(2.6f, 1.5f, 2f), position), 0.0001f);
            Assert.Less(Vector3.Angle(Vector3.right, rotation * Vector3.forward), 0.01f, "판의 앞면이 머리 쪽을 보려면 판의 앞(+z)이 머리에서 멀어지는 쪽이어야 합니다.");
            Assert.Less(Vector3.Angle(Vector3.up, rotation * Vector3.up), 0.01f, "판은 기울지 않고 똑바로 서야 합니다.");
        }

        [Test]
        public void 머리를_숙이거나_들어도_판의_거리와_높이는_같다()
        {
            Vector3 lookingDown = Quaternion.Euler(45f, 0f, 0f) * Vector3.forward;

            XrUiPanel.PoseInFront(new Vector3(0f, 1.6f, 0f), lookingDown, Vector3.right, 1.6f, 0.1f, out Vector3 position, out Quaternion rotation);

            Assert.Less(Vector3.Distance(new Vector3(0f, 1.5f, 1.6f), position), 0.0001f);
            Assert.Less(Vector3.Angle(Vector3.forward, rotation * Vector3.forward), 0.01f);
        }

        [Test]
        public void 똑바로_위나_아래를_보면_대신_준_방향을_쓴다()
        {
            Assert.AreEqual(Vector3.right, XrUiPanel.ForwardOnPlane(Vector3.down, new Vector3(2f, 5f, 0f)));
            Assert.AreEqual(Vector3.forward, XrUiPanel.ForwardOnPlane(Vector3.up, Vector3.down), "둘 다 수직이면 앞(+z)을 씁니다.");
            Assert.AreEqual(Vector3.back, XrUiPanel.ForwardOnPlane(new Vector3(0f, 0.3f, -4f), Vector3.right));
        }

        [Test]
        public void 판의_배율은_거리에_비례해_보이는_크기가_같다()
        {
            Assert.AreEqual(0.001f, XrUiPanel.ScaleAt(0.001f, 1.6f, 1.6f), 0.0000001f);
            Assert.AreEqual(0.0005f, XrUiPanel.ScaleAt(0.001f, 0.8f, 1.6f), 0.0000001f);
            Assert.AreEqual(0.001f, XrUiPanel.ScaleAt(0.001f, 0.8f, 0f), 0.0000001f, "기준 거리가 없으면 배율을 바꾸지 않습니다.");
        }
    }
}
