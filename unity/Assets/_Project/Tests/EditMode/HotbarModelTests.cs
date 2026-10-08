using AtelierVerse.UI;
using NUnit.Framework;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 부품 칸 선택 규칙 검사. 빈 칸과 같은 칸을 다시 고르는 경우, 끝에서 넘어가는 경우를 확인한다.
    /// </summary>
    public class HotbarModelTests
    {
        private static HotbarModel CreateWithFilled(int slotCount, params int[] filled)
        {
            var model = new HotbarModel(slotCount);
            foreach (int index in filled)
            {
                model.SetFilled(index, true);
            }

            return model;
        }

        [Test]
        public void 처음에는_아무_칸도_고르지_않은_상태다()
        {
            Assert.AreEqual(HotbarModel.None, CreateWithFilled(9, 0, 1).SelectedIndex);
        }

        [Test]
        public void 부품이_든_칸을_고르면_그_칸이_선택된다()
        {
            HotbarModel model = CreateWithFilled(9, 0, 1, 2);
            model.Select(2);
            Assert.AreEqual(2, model.SelectedIndex);
        }

        [Test]
        public void 같은_칸을_다시_고르면_선택이_풀린다()
        {
            HotbarModel model = CreateWithFilled(9, 0, 1);
            model.Select(1);
            model.Select(1);
            Assert.AreEqual(HotbarModel.None, model.SelectedIndex);
        }

        [Test]
        public void 빈_칸과_범위_밖의_칸은_고를_수_없다()
        {
            HotbarModel model = CreateWithFilled(9, 0);
            model.Select(0);
            model.Select(5);
            model.Select(-1);
            model.Select(9);
            Assert.AreEqual(0, model.SelectedIndex);
        }

        [Test]
        public void 다음_칸은_빈_칸을_건너뛰고_끝에서_처음으로_넘어간다()
        {
            HotbarModel model = CreateWithFilled(9, 0, 2, 5);

            model.SelectNext();
            Assert.AreEqual(0, model.SelectedIndex);
            model.SelectNext();
            Assert.AreEqual(2, model.SelectedIndex);
            model.SelectNext();
            Assert.AreEqual(5, model.SelectedIndex);
            model.SelectNext();
            Assert.AreEqual(0, model.SelectedIndex);
        }

        [Test]
        public void 이전_칸은_처음에서_끝으로_넘어간다()
        {
            HotbarModel model = CreateWithFilled(9, 0, 2, 5);

            model.SelectPrevious();
            Assert.AreEqual(5, model.SelectedIndex);
            model.SelectPrevious();
            Assert.AreEqual(2, model.SelectedIndex);
            model.SelectPrevious();
            Assert.AreEqual(0, model.SelectedIndex);
            model.SelectPrevious();
            Assert.AreEqual(5, model.SelectedIndex);
        }

        [Test]
        public void 부품이_하나도_없으면_다음_칸을_눌러도_그대로다()
        {
            var model = new HotbarModel(9);
            model.SelectNext();
            model.SelectPrevious();
            Assert.AreEqual(HotbarModel.None, model.SelectedIndex);
        }

        [Test]
        public void 고른_칸을_비우면_선택도_풀린다()
        {
            HotbarModel model = CreateWithFilled(9, 3);
            model.Select(3);
            model.SetFilled(3, false);
            Assert.AreEqual(HotbarModel.None, model.SelectedIndex);
        }

        [Test]
        public void 선택이_바뀔_때만_알린다()
        {
            HotbarModel model = CreateWithFilled(9, 0, 1);
            int changes = 0;
            model.SelectionChanged += _ => changes++;

            model.Select(0);
            model.Select(7);
            model.Clear();
            model.Clear();

            Assert.AreEqual(2, changes);
        }
    }
}
