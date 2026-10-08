using AtelierVerse.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 화면 위 가운데의 알림 띠. Notice.Post로 올라온 알림을 잠시 보여 준다.
    /// 띠의 너비는 글자에 맞춘다. 종류에 따라 왼쪽 점의 색이 바뀐다.
    /// </summary>
    public class NoticeBar : MonoBehaviour
    {
        private const float SidePadding = 34f;
        private const float MinWidth = 160f;

        [SerializeField] private GameObject bar;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image dot;
        [SerializeField] private Color infoColor = Color.green;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color errorColor = Color.red;
        [SerializeField] private float duration = NoticeModel.DefaultDuration;

        private NoticeModel model;

        public NoticeModel Model => model ??= new NoticeModel(duration);

        public bool IsVisible => Model.IsVisible;

        public string Message => Model.Message;

        private void OnEnable()
        {
            Notice.Posted += Show;
            Model.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Model.Changed -= Refresh;
            Notice.Posted -= Show;
        }

        private void Update()
        {
            Model.Update(Time.unscaledTime);
        }

        public void Show(string message, NoticeKind kind)
        {
            Model.Show(message, kind, Time.unscaledTime);
        }

        private void Refresh()
        {
            if (bar == null) return;

            bar.SetActive(Model.IsVisible);
            if (!Model.IsVisible) return;

            if (label != null)
            {
                label.text = Model.Message;
                label.ForceMeshUpdate();
                var rect = (RectTransform)bar.transform;
                rect.sizeDelta = new Vector2(Mathf.Max(MinWidth, label.preferredWidth + SidePadding * 2f), rect.sizeDelta.y);
            }

            if (dot != null) dot.color = ColorOf(Model.Kind);
        }

        private Color ColorOf(NoticeKind kind)
        {
            switch (kind)
            {
                case NoticeKind.Warning: return warningColor;
                case NoticeKind.Error: return errorColor;
                default: return infoColor;
            }
        }
    }
}
