using System;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>
    /// 맵 문서를 이 기기의 파일로 읽고 쓴다. 쓸 때는 임시 파일에 먼저 쓰고 이름을 바꾸어, 쓰는 도중 꺼져도 이전 파일이 남게 한다.
    /// 저장 폴더는 바꿀 수 있어 검사에서는 임시 폴더를 쓴다.
    /// 읽고 쓰다가 실패하면 조금 기다렸다 몇 번 다시 해 본다(21일차). 방금 쓴 파일을 백신이나 색인 프로그램이 잠깐 잡고 있어
    /// 이어지는 쓰기가 실패하는 일이 있기 때문이다. 끝내 안 되면 전과 같이 실패로 알린다.
    /// </summary>
    public static class MapStorage
    {
        public const string FolderName = "maps";
        public const string LocalFileName = "local.map.json";
        private const string TempSuffix = ".tmp";
        private const string BrokenSuffix = ".broken";

        /// <summary>읽기나 쓰기를 해 보는 횟수. 처음 한 번과 다시 해 보는 것을 합친 수다.</summary>
        public const int IoAttempts = 4;

        // 다시 해 보기 전에 기다리는 시간(밀리초). 해 볼 때마다 이만큼씩 더 기다린다(25, 50, 75).
        private const int IoRetryMilliseconds = 25;

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static string directoryOverride;

        /// <summary>맵 파일을 두는 폴더. 값을 넣으면 그 폴더를 쓰고, null이면 기본 위치로 돌아간다.</summary>
        public static string Directory
        {
            get => directoryOverride ?? Path.Combine(Application.persistentDataPath, FolderName);
            set => directoryOverride = string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>처음부터 있던 맵(번호표 local)의 파일 경로. 16일차까지는 맵이 이것 하나였다. 여러 맵은 MapLibrary가 다룬다.</summary>
        public static string LocalPath => Path.Combine(Directory, LocalFileName);

        public static bool Exists(string path)
        {
            return File.Exists(path);
        }

        /// <summary>문서를 파일로 쓴다. 폴더가 없으면 만든다. 실패하면 IOException이나 UnauthorizedAccessException을 던진다.</summary>
        public static void Save(MapDocument document, string path)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) System.IO.Directory.CreateDirectory(directory);

            string temp = path + TempSuffix;
            string json = MapDocument.ToJson(document);
            WithRetry(() => File.WriteAllText(temp, json, Utf8NoBom));
            WithRetry(() =>
            {
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            });
        }

        /// <summary>
        /// 파일을 다루는 일을 해 보고, 파일이 잠겨 있어 실패하면 조금 기다렸다 다시 해 본다.
        /// 정한 횟수만큼 해 보아도 안 되면 마지막 예외를 그대로 던진다.
        /// </summary>
        private static void WithRetry(Action action)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception exception) when ((exception is IOException || exception is UnauthorizedAccessException) && attempt < IoAttempts)
                {
                    Thread.Sleep(IoRetryMilliseconds * attempt);
                }
            }
        }

        /// <summary>파일을 읽어 문서로 만든다. 실패하면 document는 null이고 error에 까닭이 담긴다.</summary>
        public static bool TryLoad(string path, out MapDocument document, out MapFileError error)
        {
            document = null;
            if (!File.Exists(path))
            {
                error = MapFileError.Missing;
                return false;
            }

            string json = null;
            try
            {
                WithRetry(() => json = File.ReadAllText(path, Encoding.UTF8));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                error = MapFileError.ReadFailed;
                return false;
            }

            return MapDocument.TryParse(json, out document, out error);
        }

        /// <summary>
        /// 파일의 사본을 옆에 남긴다(이름 뒤에 suffix). 옛 판의 파일을 새 판으로 고쳐 쓰기 전에 원래 내용을 남겨 두는 데 쓴다.
        /// 이미 같은 이름의 사본이 있으면 그대로 두고 그 경로를 돌려준다. 남기지 못하면 null이다.
        /// </summary>
        public static string Backup(string path, string suffix)
        {
            if (!File.Exists(path)) return null;

            string copy = path + suffix;
            try
            {
                if (!File.Exists(copy)) File.Copy(path, copy);
                return copy;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>읽지 못한 파일을 옆에 두고(.broken-시각) 원래 이름을 비운다. 덮어써서 내용을 잃지 않게 하기 위해서다.</summary>
        public static string SetAside(string path)
        {
            if (!File.Exists(path)) return null;

            string aside = $"{path}{BrokenSuffix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            try
            {
                if (File.Exists(aside)) File.Delete(aside);
                File.Move(path, aside);
                return aside;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
