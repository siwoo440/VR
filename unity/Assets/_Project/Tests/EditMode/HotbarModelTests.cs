using AtelierVerse.UI;
using NUnit.Framework;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 부품 칸의 규칙 검사. 빈 칸과 같은 칸을 다시 고르는 경우, 끝에서 넘어가는 경우, 칸에 부품을 넣고 빼는 경우를 확인한다.
    /// </summary>
    public class HotbarModelTests
    {
        private static HotbarModel CreateWithFilled(int slotCount, params int[] filled)
        {
            var model = new HotbarModel(slotCount);
            foreach (int index in filled)
            {
                model.SetPart(index, index);
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
            model.SetPart(3, HotbarModel.None);
            Assert.AreEqual(HotbarModel.None, model.SelectedIndex);
        }

        [Test]
        public void 칸에_부품을_넣으면_그_칸의_부품이_되고_빼면_빈_칸이_된다()
        {
            var model = new HotbarModel(9);
            Assert.AreEqual(HotbarModel.None, model.GetPart(4));
            Assert.IsFalse(model.IsFilled(4));

            model.SetPart(4, 17);
            Assert.AreEqual(17, model.GetPart(4));
            Assert.IsTrue(model.IsFilled(4));

            model.SetPart(4, HotbarModel.None);
            Assert.IsFalse(model.IsFilled(4));
            Assert.AreEqual(HotbarModel.None, model.GetPart(99), "범위 밖의 칸은 빈 칸으로 봅니다.");
        }

        [Test]
        public void 고른_부품은_칸의_번호가_아니라_칸에_든_부품이다()
        {
            var model = new HotbarModel(9);
            model.SetPart(2, 23);
            Assert.AreEqual(HotbarModel.None, model.SelectedPart);

            model.Select(2);
            Assert.AreEqual(2, model.SelectedIndex);
            Assert.AreEqual(23, model.SelectedPart);

            // 고른 칸의 부품을 바꾸면 고른 부품도 바뀐다. 선택은 그대로다.
            model.SetPart(2, 8);
            Assert.AreEqual(2, model.SelectedIndex);
            Assert.AreEqual(8, model.SelectedPart);
        }

        [Test]
        public void 칸에_든_부품이_바뀔_때만_알린다()
        {
            var model = new HotbarModel(9);
            int changes = 0;
            model.SlotsChanged += () => changes++;

            model.SetPart(0, 5);
            model.SetPart(0, 5);
            model.SetPart(0, 6);
            model.SetPart(-1, 6);
            model.SetPart(0, HotbarModel.None);

            Assert.AreEqual(3, changes);
        }

        [Test]
        public void Choose는_이미_고른_칸이어도_선택을_풀지_않는다()
        {
            HotbarModel model = CreateWithFilled(9, 1);

            model.Choose(1);
            Assert.AreEqual(1, model.SelectedIndex);
            model.Choose(1);
            Assert.AreEqual(1, model.SelectedIndex, "부품을 넣은 칸을 바로 쓰게 하려는 것이므로 풀리면 안 됩니다.");

            model.Choose(5);
            Assert.AreEqual(1, model.SelectedIndex, "빈 칸은 고를 수 없습니다.");
        }

        [Test]
        public void 가장_앞의_빈_칸을_찾는다()
        {
            HotbarModel model = CreateWithFilled(4, 0, 1, 3);
            Assert.AreEqual(2, model.FirstEmpty());

            model.SetPart(2, 9);
            Assert.AreEqual(HotbarModel.None, model.FirstEmpty(), "빈 칸이 없으면 None입니다.");
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
