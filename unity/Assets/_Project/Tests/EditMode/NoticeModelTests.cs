using AtelierVerse.Core;
using NUnit.Framework;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 알림 띠 상태의 검사. 보이는 시간, 새 알림으로 바뀌는 것, 빈 알림을 거르는 것을 확인한다.
    /// </summary>
    public class NoticeModelTests
    {
        [Test]
        public void 알림을_올리면_보이고_시간이_지나면_사라진다()
        {
            var model = new NoticeModel(2.5f);

            model.Show("놓을 수 없습니다", NoticeKind.Warning, 10f);
            Assert.IsTrue(model.IsVisible);
            Assert.AreEqual("놓을 수 없습니다", model.Message);
            Assert.AreEqual(NoticeKind.Warning, model.Kind);

            model.Update(12.4f);
            Assert.IsTrue(model.IsVisible);

            model.Update(12.5f);
            Assert.IsFalse(model.IsVisible);
        }

        [Test]
        public void 새_알림이_오면_내용과_시간이_바뀐다()
        {
            var model = new NoticeModel(2.5f);
            model.Show("첫째", NoticeKind.Info, 10f);

            model.Show("둘째", NoticeKind.Error, 11f);

            Assert.AreEqual("둘째", model.Message);
            Assert.AreEqual(NoticeKind.Error, model.Kind);
            model.Update(12.6f);
            Assert.IsTrue(model.IsVisible, "둘째 알림의 시간은 11초부터 세어야 합니다.");
            model.Update(13.5f);
            Assert.IsFalse(model.IsVisible);
        }

        [Test]
        public void 빈_알림은_올리지_않는다()
        {
            var model = new NoticeModel();
            int posted = 0;
            void Count(string message, NoticeKind kind) => posted++;

            Notice.Posted += Count;
            try
            {
                Notice.Post("");
                Notice.Post(null);
                Notice.Post("있음");
            }
            finally
            {
                Notice.Posted -= Count;
            }

            model.Show("", NoticeKind.Info, 0f);

            Assert.AreEqual(1, posted);
            Assert.IsFalse(model.IsVisible);
        }

        [Test]
        public void 바뀔_때마다_알린다()
        {
            var model = new NoticeModel(1f);
            int changes = 0;
            model.Changed += () => changes++;

            model.Show("알림", NoticeKind.Info, 0f);
            model.Update(0.5f);
            model.Update(1f);
            model.Hide();

            Assert.AreEqual(2, changes);
        }

        [Test]
        public void 시간이_0_이하이면_기본_시간을_쓴다()
        {
            Assert.AreEqual(NoticeModel.DefaultDuration, new NoticeModel(0f).Duration);
            Assert.AreEqual(NoticeModel.DefaultDuration, new NoticeModel(-3f).Duration);
        }
    }
}
