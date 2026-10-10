using System;
using UnityEngine;

namespace AtelierVerse.Core
{
    /// <summary>
    /// 화면 품질(낮음·보통·높음)을 프로젝트의 품질 단계에 잇는다(20일차). 품질 단계와 단계마다의 그리기 설정은 20일차 셋업이 만든다.
    /// 단계는 번호가 아니라 이름으로 찾는다. 기기에 따라 단계의 수와 차례가 다르기 때문이다(Quest에는 PC의 단계가 없다).
    /// 에디터에서는 실행 중에 바꾼 단계와 수직 동기화가 프로젝트 설정 파일에 그대로 남는다. 그래서 단계를 떠날 때 그 단계의
    /// 수직 동기화를 원래 값으로 돌려놓고, Restore로 처음 상태에 돌아올 수 있게 손대기 전의 값을 기억한다.
    /// </summary>
    public static class GraphicsQuality
    {
        /// <summary>품질 단계의 이름. 낮음·보통·높음의 차례다. "PC"는 19일차까지 쓰던 단계이며 보통이 된다.</summary>
        public static readonly string[] LevelNames = { "PC Low", "PC", "PC High" };

        /// <summary>화면에 보이는 이름.</summary>
        public static readonly string[] Labels = { "낮음", "보통", "높음" };

        public static int Count => LevelNames.Length;

        // 손대기 전의 단계와, 지금 단계의 수직 동기화가 원래 얼마였는지. -1은 아직 손대지 않았다는 뜻이다.
        private static int startLevel = -1;
        private static int baselineLevel = -1;
        private static int baselineVSync;

        /// <summary>품질(0~2)에 해당하는 단계가 names의 몇 번째인지. 없으면 -1이다.</summary>
        public static int FindLevel(string[] names, int quality)
        {
            if (names == null || quality < 0 || quality >= LevelNames.Length) return -1;
            return Array.IndexOf(names, LevelNames[quality]);
        }

        /// <summary>단계의 이름이 어느 품질인지. 이 게임의 단계가 아니면 -1이다.</summary>
        public static int QualityOf(string levelName)
        {
            return Array.IndexOf(LevelNames, levelName);
        }

        /// <summary>지금 쓰이는 단계의 이름.</summary>
        public static string CurrentLevelName
        {
            get
            {
                string[] names = QualitySettings.names;
                int level = QualitySettings.GetQualityLevel();
                return level >= 0 && level < names.Length ? names[level] : string.Empty;
            }
        }

        /// <summary>지금 쓰이는 품질(0~2). 이 게임의 단계가 아니면 -1이다.</summary>
        public static int Current => QualityOf(CurrentLevelName);

        /// <summary>
        /// 품질과 수직 동기화를 적용한다. 이미 그 값이면 건드리지 않는다. 무엇이든 바뀌었으면 true다.
        /// 단계를 바꾸면 수직 동기화가 그 단계에 적힌 값으로 돌아가므로 단계를 먼저 바꾸고 수직 동기화를 맞춘다.
        /// </summary>
        public static bool Apply(int quality, bool vSync)
        {
            bool changed = false;

            int current = QualitySettings.GetQualityLevel();
            if (startLevel < 0) startLevel = current;
            if (baselineLevel != current)
            {
                baselineLevel = current;
                baselineVSync = QualitySettings.vSyncCount;
            }

            int level = FindLevel(QualitySettings.names, quality);
            if (level >= 0 && level != current)
            {
                // 수직 동기화는 단계마다 따로 적힌다. 떠나는 단계의 값을 원래대로 돌려놓고 간다.
                QualitySettings.vSyncCount = baselineVSync;
                QualitySettings.SetQualityLevel(level, true);
                baselineLevel = level;
                baselineVSync = QualitySettings.vSyncCount;
                changed = true;
            }

            int count = vSync ? 1 : 0;
            if (QualitySettings.vSyncCount != count)
            {
                QualitySettings.vSyncCount = count;
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Apply로 바꾸기 전의 상태로 돌려놓는다: 지금 단계의 수직 동기화를 원래 값으로, 단계를 처음의 단계로.
        /// 에디터에서 실행을 끝낼 때와 테스트가 끝날 때 부른다. 빌드한 실행 파일에서는 부를 일이 없다.
        /// </summary>
        public static void Restore()
        {
            int current = QualitySettings.GetQualityLevel();
            if (baselineLevel == current) QualitySettings.vSyncCount = baselineVSync;
            if (startLevel >= 0 && startLevel != current && startLevel < QualitySettings.names.Length) QualitySettings.SetQualityLevel(startLevel, true);

            startLevel = -1;
            baselineLevel = -1;
        }
    }
}
