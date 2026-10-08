using AtelierVerse.Core;
using AtelierVerse.Player;
using NUnit.Framework;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 휠로 바꾸는 시점 거리와 개인 설정의 범위 검사.
    /// </summary>
    public class ViewAndSettingsTests
    {
        private const float Near = 2.5f;
        private const float Far = 8f;
        private const float Step = 1f;

        private float savedLook;
        private float savedFieldOfView;
        private bool savedPeopleList;

        [SetUp]
        public void SaveSettings()
        {
            savedLook = GameSettings.LookSensitivity;
            savedFieldOfView = GameSettings.FieldOfView;
            savedPeopleList = GameSettings.ShowPeopleList;
        }

        [TearDown]
        public void RestoreSettings()
        {
            GameSettings.LookSensitivity = savedLook;
            GameSettings.FieldOfView = savedFieldOfView;
            GameSettings.ShowPeopleList = savedPeopleList;
        }

        [Test]
        public void 일인칭에서_휠을_당기면_가까운_3인칭_거리가_된다()
        {
            Assert.AreEqual(Near, ViewRig.StepViewDistance(0f, false, Near, Far, Step));
        }

        [Test]
        public void 삼인칭에서_휠을_당기면_한_칸씩_멀어지고_가장_먼_거리에서_멈춘다()
        {
            Assert.AreEqual(3.5f, ViewRig.StepViewDistance(Near, false, Near, Far, Step));
            Assert.AreEqual(Far, ViewRig.StepViewDistance(7.6f, false, Near, Far, Step));
            Assert.AreEqual(Far, ViewRig.StepViewDistance(Far, false, Near, Far, Step));
        }

        [Test]
        public void 휠을_밀면_한_칸씩_가까워지고_가장_가까운_거리에서는_1인칭이_된다()
        {
            Assert.AreEqual(4f, ViewRig.StepViewDistance(5f, true, Near, Far, Step));
            Assert.AreEqual(Near, ViewRig.StepViewDistance(3f, true, Near, Far, Step));
            Assert.AreEqual(0f, ViewRig.StepViewDistance(Near, true, Near, Far, Step));
            Assert.AreEqual(0f, ViewRig.StepViewDistance(0f, true, Near, Far, Step));
        }

        [Test]
        public void 마우스_감도는_범위_안으로_맞춰진다()
        {
            GameSettings.LookSensitivity = 99f;
            Assert.AreEqual(GameSettings.MaxLookSensitivity, GameSettings.LookSensitivity, 0.0001f);

            GameSettings.LookSensitivity = -1f;
            Assert.AreEqual(GameSettings.MinLookSensitivity, GameSettings.LookSensitivity, 0.0001f);
        }

        [Test]
        public void 시야각은_범위_안으로_맞춰진다()
        {
            GameSettings.FieldOfView = 500f;
            Assert.AreEqual(GameSettings.MaxFieldOfView, GameSettings.FieldOfView, 0.0001f);

            GameSettings.FieldOfView = 1f;
            Assert.AreEqual(GameSettings.MinFieldOfView, GameSettings.FieldOfView, 0.0001f);
        }

        [Test]
        public void 설정은_값이_실제로_바뀔_때만_알린다()
        {
            GameSettings.FieldOfView = 70f;
            int changes = 0;
            void Count() => changes++;

            GameSettings.Changed += Count;
            try
            {
                GameSettings.FieldOfView = 80f;
                GameSettings.FieldOfView = 80f;
                GameSettings.ShowPeopleList = !GameSettings.ShowPeopleList;
            }
            finally
            {
                GameSettings.Changed -= Count;
            }

            Assert.AreEqual(2, changes);
        }

        [Test]
        public void 기본값으로_돌리면_처음_값이_된다()
        {
            GameSettings.LookSensitivity = GameSettings.MaxLookSensitivity;
            GameSettings.FieldOfView = GameSettings.MaxFieldOfView;
            GameSettings.ShowPeopleList = false;

            GameSettings.ResetToDefaults();

            Assert.AreEqual(GameSettings.DefaultLookSensitivity, GameSettings.LookSensitivity, 0.0001f);
            Assert.AreEqual(GameSettings.DefaultFieldOfView, GameSettings.FieldOfView, 0.0001f);
            Assert.IsTrue(GameSettings.ShowPeopleList);
        }
    }
}
