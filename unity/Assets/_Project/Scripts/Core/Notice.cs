using System;

namespace AtelierVerse.Core
{
    /// <summary>알림의 종류. 띠의 점 색이 달라진다.</summary>
    public enum NoticeKind
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// 짧은 알림이 모이는 곳. 블록 놓기, 저장, 되돌리기처럼 화면을 모르는 코드가 Post로 올리고, 알림 띠(NoticeBar)가 받아 보여 준다.
    /// </summary>
    public static class Notice
    {
        public static event Action<string, NoticeKind> Posted;

        public static void Post(string message, NoticeKind kind = NoticeKind.Info)
        {
            if (string.IsNullOrEmpty(message)) return;
            Posted?.Invoke(message, kind);
        }
    }

    /// <summary>
    /// 알림 띠의 상태. 마지막 알림 하나를 정해진 시간 동안 보이고, 새 알림이 오면 바로 바꾼다.
    /// 화면과 무관해서 편집 모드 테스트로 검사한다.
    /// </summary>
    public class NoticeModel
    {
        public const float DefaultDuration = 2.5f;

        private float hideAt;

        public NoticeModel(float duration = DefaultDuration)
        {
            Duration = duration > 0f ? duration : DefaultDuration;
        }

        /// <summary>보이거나 사라지거나 내용이 바뀌면 알린다.</summary>
        public event Action Changed;

        /// <summary>알림이 보이는 시간(초).</summary>
        public float Duration { get; }

        public string Message { get; private set; } = string.Empty;

        public NoticeKind Kind { get; private set; }

        public bool IsVisible { get; private set; }

        /// <summary>알림을 보인다. now는 지금 시각(초)이며 Duration이 지나면 사라진다.</summary>
        public void Show(string message, NoticeKind kind, float now)
        {
            if (string.IsNullOrEmpty(message)) return;

            Message = message;
            Kind = kind;
            IsVisible = true;
            hideAt = now + Duration;
            Changed?.Invoke();
        }

        /// <summary>시간이 지났으면 알림을 감춘다. 매 프레임 부른다.</summary>
        public void Update(float now)
        {
            if (IsVisible && now >= hideAt) Hide();
        }

        public void Hide()
        {
            if (!IsVisible) return;

            IsVisible = false;
            Changed?.Invoke();
        }
    }
}
