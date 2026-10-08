using TMPro;
using UnityEditor;
using UnityEngine;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 한글 글꼴 자산을 저장하기 직전에, 실행하면서 채워진 글자 그림을 비운다.
    /// 비우지 않으면 글자를 쓸 때마다 수 MB짜리 그림이 자산에 저장되어 커밋이 커진다. 비운 글자는 쓸 때 다시 채워진다.
    /// </summary>
    internal class DynamicFontGuard : AssetModificationProcessor
    {
        private const double BurstWindowSeconds = 2.0;
        private const int BurstLimit = 3;

        private static double windowStart;
        private static int countInWindow;
        private static bool disabled;

        private static string[] OnWillSaveAssets(string[] paths)
        {
            if (disabled) return paths;

            foreach (string path in paths)
            {
                if (path != Day2Setup.FontAssetPath) continue;

                // 저장이 짧은 시간에 되풀이되면 비우기와 다시 채우기가 맞물린 것이므로 이번 실행에서는 그만둔다.
                double now = EditorApplication.timeSinceStartup;
                if (now - windowStart > BurstWindowSeconds)
                {
                    windowStart = now;
                    countInWindow = 0;
                }

                if (++countInWindow > BurstLimit)
                {
                    disabled = true;
                    Debug.LogWarning("[Atelier Verse] 글꼴 자산 저장이 되풀이되어 글자 그림 비우기를 멈춥니다. 에디터를 다시 열면 다시 동작합니다.");
                    break;
                }

                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (fontAsset != null && fontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                {
                    fontAsset.ClearFontAssetData(true);
                }
            }

            return paths;
        }
    }
}
