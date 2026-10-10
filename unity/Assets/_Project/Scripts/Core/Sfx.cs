using System;
using UnityEngine;

namespace AtelierVerse.Core
{
    /// <summary>소리의 종류. 클립을 이 순서로 만들어 두므로, 더할 때는 뒤에 더한다.</summary>
    public enum SfxId
    {
        // 몸
        Step,
        Jump,
        Land,
        FlyOn,
        FlyOff,

        // 만들기
        Place,
        Remove,
        Paint,
        Move,
        Rotate,
        Grab,
        Release,
        Snap,
        Undo,
        Redo,

        // 화면
        Click,
        Select,
        Open,
        Close,
        Warn,
        Error,
        Shutter,
    }

    /// <summary>
    /// 소리를 내 달라는 요청이 모이는 곳(19일차). 알림(Notice)처럼, 소리를 모르는 코드가 Play로 올리고 소리를 내는 부품(SfxPlayer)이 받아 낸다.
    /// 자리가 있는 소리(블록, 발소리)는 PlayAt으로 올려 그 자리에서 나게 하고, 화면의 소리는 Play로 올려 자리 없이 나게 한다.
    /// 듣는 부품이 없으면(소리가 없는 씬, 테스트) 아무 일도 일어나지 않는다.
    /// </summary>
    public static class Sfx
    {
        private static readonly int IdCount = Enum.GetValues(typeof(SfxId)).Length;

        /// <summary>소리를 내 달라는 요청이 올라오면 알린다. 자리가 없는 소리는 position이 null이다.</summary>
        public static event Action<SfxId, Vector3?> Played;

        /// <summary>소리의 가짓수.</summary>
        public static int Count => IdCount;

        /// <summary>자리 없이 나는 소리를 낸다. 단추와 알림처럼 화면에서 나는 소리에 쓴다.</summary>
        public static void Play(SfxId id)
        {
            Played?.Invoke(id, null);
        }

        /// <summary>월드의 한 자리에서 나는 소리를 낸다. 멀면 작게, 옆이면 옆에서 들린다.</summary>
        public static void PlayAt(SfxId id, Vector3 position)
        {
            Played?.Invoke(id, position);
        }
    }
}
