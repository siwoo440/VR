using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 스크립트가 컴파일되면 아직 적용하지 않은 일차별 셋업을 자동으로 한 번씩 실행한다.
    /// 적용 기록은 ProjectSettings/AtelierVerseSetupState.txt 에 남겨 다른 PC에서도 중복 실행되지 않게 한다.
    /// 에디터 창 없이 적용할 때는 명령줄에서 RunFromCommandLine을 부른다.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetupRunner
    {
        private const string StatePath = "ProjectSettings/AtelierVerseSetupState.txt";

        private static readonly (string id, Action apply)[] Steps =
        {
            ("Day01", Day1Setup.Apply),
            ("Day02", Day2Setup.Apply),
        };

        static ProjectSetupRunner()
        {
            EditorApplication.delayCall += TryRun;
        }

        /// <summary>
        /// 명령줄 진입점. 예: Unity.exe -batchmode -quit -projectPath unity -executeMethod AtelierVerse.EditorTools.ProjectSetupRunner.RunFromCommandLine
        /// 실패하면 예외를 그대로 던져 Unity가 0이 아닌 값으로 끝나게 한다.
        /// </summary>
        public static void RunFromCommandLine()
        {
            RunPending(true);
        }

        private static void TryRun()
        {
            if (Application.isBatchMode) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryRun;
                return;
            }

            if (!HasPending()) return;

            // 셋업은 씬을 새로 열기 때문에, 저장하지 않은 변경이 있으면 먼저 저장할지 묻는다. 취소하면 이번에는 건너뛴다.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[Atelier Verse] 저장하지 않은 씬이 있어 자동 셋업을 건너뛰었습니다. 메뉴 Atelier Verse에서 직접 실행할 수 있습니다.");
                return;
            }

            RunPending(false);
        }

        private static bool HasPending()
        {
            HashSet<string> done = ReadState();
            foreach ((string id, Action _) in Steps)
            {
                if (!done.Contains(id)) return true;
            }

            return false;
        }

        private static void RunPending(bool rethrow)
        {
            HashSet<string> done = ReadState();

            foreach ((string id, Action apply) in Steps)
            {
                if (done.Contains(id)) continue;

                try
                {
                    apply();
                    done.Add(id);
                    WriteState(done);
                    Debug.Log($"[Atelier Verse] {id} 셋업을 적용했습니다.");
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[Atelier Verse] {id} 셋업을 적용하지 못했습니다.");
                    Debug.LogException(exception);
                    if (rethrow) throw;
                    return;
                }
            }
        }

        private static HashSet<string> ReadState()
        {
            var done = new HashSet<string>();
            if (!File.Exists(StatePath)) return done;

            foreach (string line in File.ReadAllLines(StatePath))
            {
                string id = line.Trim();
                if (id.Length > 0) done.Add(id);
            }

            return done;
        }

        private static void WriteState(HashSet<string> done)
        {
            var ordered = new List<string>(done);
            ordered.Sort(StringComparer.Ordinal);
            File.WriteAllLines(StatePath, ordered);
        }
    }
}
