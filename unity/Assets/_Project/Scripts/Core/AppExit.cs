using System;
using UnityEngine;

namespace AtelierVerse.Core
{
    /// <summary>
    /// 게임 끝내기 요청이 모이는 곳. 검사에서는 Handler를 바꿔 실제로 끝나지 않게 한다.
    /// </summary>
    public static class AppExit
    {
        public static Action Handler = Quit;

        public static void Request()
        {
            Handler?.Invoke();
        }

        /// <summary>앱을 끝낸다. 에디터에서는 실행을 멈춘다.</summary>
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
