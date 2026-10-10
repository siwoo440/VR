using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>맵 목록의 한 줄. 파일 하나가 맵 하나다.</summary>
    public struct MapInfo
    {
        /// <summary>맵의 번호표. 파일 이름의 앞부분이며 한 번 정하면 바뀌지 않는다. 이름을 바꿔도 그대로다.</summary>
        public string Id;

        /// <summary>보이는 이름. 비어 있으면 화면에서 "이름 없는 맵"으로 보인다.</summary>
        public string Name;

        /// <summary>맵의 설명. 없으면 빈 문자열이다.</summary>
        public string Description;

        public int BlockCount;

        /// <summary>처음 만든 시각(UTC). 알 수 없으면 DateTime.MinValue다.</summary>
        public DateTime CreatedAt;

        /// <summary>마지막으로 저장한 시각(UTC). 알 수 없으면 DateTime.MinValue다.</summary>
        public DateTime UpdatedAt;

        /// <summary>파일을 읽을 수 있는지. 읽지 못하는 파일도 목록에는 보이되 열 수 없다.</summary>
        public bool Readable;

        /// <summary>시작 위치를 따로 정했는지.</summary>
        public bool HasSpawn;

        /// <summary>대표 그림이 있는지.</summary>
        public bool HasThumbnail;

        /// <summary>하늘의 이름(MapSky). 읽지 못한 파일이면 맑은 낮이다.</summary>
        public string Sky;

        /// <summary>해의 방향과 높이(도).</summary>
        public float SunYaw;

        public float SunPitch;

        /// <summary>바닥의 한 변(칸 수).</summary>
        public int FloorSize;

        /// <summary>휴지통에 있는 맵인지. 휴지통의 맵은 열 수 없고 되살리거나 아주 지울 수 있다.</summary>
        public bool InTrash;

        /// <summary>휴지통에 있는 파일의 이름(확장자 없이). 되살리거나 아주 지울 때 이것으로 가리킨다. 휴지통의 맵이 아니면 null이다.</summary>
        public string TrashKey;

        /// <summary>지운 시각(UTC). 휴지통의 맵이 아니거나 알 수 없으면 DateTime.MinValue다.</summary>
        public DateTime DeletedAt;
    }

    /// <summary>
    /// 이 기기에 저장된 맵들(17일차). 맵 파일의 폴더(MapStorage.Directory)에서 맵마다 파일 하나(번호표.map.json)를 다룬다.
    /// 목록 보기, 새 맵 만들기, 이름과 설명 바꾸기, 지우기(휴지통 폴더로 옮기기), 마지막으로 연 맵 기억하기를 맡는다.
    /// 맵의 사본을 만들고, 휴지통의 맵을 보고 되살리거나 아주 지운다(22일차). 아주 지운 맵은 되찾을 수 없다.
    /// 맵의 대표 그림은 맵 파일 옆에 그림 파일(번호표.png)로 둔다(18일차).
    /// 지금 열려 있는 맵의 내용을 읽고 쓰는 일은 MapAutoSave가 맡는다. 파일만 다루므로 편집 모드 테스트로 검사한다.
    /// </summary>
    public static class MapLibrary
    {
        /// <summary>처음부터 있던 맵의 번호표. 16일차까지는 이 맵 하나뿐이었다(local.map.json).</summary>
        public const string DefaultId = "local";

        public const string Extension = ".map.json";
        public const string ThumbnailExtension = ".png";
        public const string CurrentFileName = "current.txt";
        public const string TrashFolderName = "trash";
        public const string DefaultName = "새 맵";
        public const string UnnamedLabel = "이름 없는 맵";
        public const int MaxNameLength = 24;
        public const int MaxIdLength = 40;

        /// <summary>사본의 이름 뒤에 붙는 글자.</summary>
        public const string CopySuffix = " 사본";

        /// <summary>휴지통의 파일 이름(확장자 없이)으로 받는 가장 긴 길이. 번호표에 지운 시각이 붙은 꼴이다.</summary>
        public const int MaxTrashKeyLength = 80;

        /// <summary>이 기기에 둘 수 있는 맵의 수.</summary>
        public const int MaxMaps = 50;

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        // 휴지통의 파일 이름: 번호표-지운날(8자리)-지운시각(6자리), 같은 때에 둘이면 뒤에 -번호. 번호표에도 날짜가 들어 있을 수 있으므로
        // 번호표를 되도록 길게 잡아, 맨 끝의 날짜와 시각을 지운 때로 읽는다.
        private static readonly Regex TrashName = new Regex(@"^(?<id>[a-z0-9-]+)-(?<date>\d{8})-(?<time>\d{6})(?:-\d+)?$", RegexOptions.CultureInvariant);

        public static string PathOf(string id)
        {
            return Path.Combine(MapStorage.Directory, id + Extension);
        }

        /// <summary>맵의 대표 그림 파일의 경로. 맵 파일 옆에 번호표.png로 둔다.</summary>
        public static string ThumbnailPathOf(string id)
        {
            return Path.Combine(MapStorage.Directory, id + ThumbnailExtension);
        }

        public static string TrashDirectory => Path.Combine(MapStorage.Directory, TrashFolderName);

        /// <summary>휴지통에 있는 맵 파일의 경로.</summary>
        public static string TrashPathOf(string key)
        {
            return Path.Combine(TrashDirectory, key + Extension);
        }

        /// <summary>휴지통에 있는 맵의 대표 그림 파일의 경로. 맵 파일 옆에 같은 이름으로 있다.</summary>
        public static string TrashThumbnailPathOf(string key)
        {
            return Path.Combine(TrashDirectory, key + ThumbnailExtension);
        }

        /// <summary>
        /// 휴지통의 파일을 가리키는 이름으로 쓸 수 있는지. 번호표와 같은 글자만 받아 폴더를 벗어나는 글자를 막고,
        /// 지운 때가 붙은 꼴(번호표-날짜-시각)이어야 한다.
        /// </summary>
        public static bool IsValidTrashKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length > MaxTrashKeyLength) return false;

            foreach (char letter in key)
            {
                bool allowed = (letter >= 'a' && letter <= 'z') || (letter >= '0' && letter <= '9') || letter == '-';
                if (!allowed) return false;
            }

            Match match = TrashName.Match(key);
            return match.Success && IsValidId(match.Groups["id"].Value);
        }

        /// <summary>
        /// 번호표로 쓸 수 있는 글자인지. 영문 소문자, 숫자, 줄표만 받는다. 번호표가 파일 이름이 되므로 폴더를 벗어나는 글자를 막는다.
        /// </summary>
        public static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > MaxIdLength) return false;

            foreach (char letter in id)
            {
                bool allowed = (letter >= 'a' && letter <= 'z') || (letter >= '0' && letter <= '9') || letter == '-';
                if (!allowed) return false;
            }

            return true;
        }

        public static bool Exists(string id)
        {
            return IsValidId(id) && File.Exists(PathOf(id));
        }

        /// <summary>이 기기의 맵의 수. 파일을 읽지 않고 센다.</summary>
        public static int Count()
        {
            string directory = MapStorage.Directory;
            if (!Directory.Exists(directory)) return 0;

            int count = 0;
            foreach (string path in Directory.GetFiles(directory, "*" + Extension))
            {
                if (IsValidId(StemOf(path))) count++;
            }

            return count;
        }

        /// <summary>맵 파일의 이름에서 확장자를 뗀 것. 맵 파일의 이름이 아니면 null이다.</summary>
        private static string StemOf(string path)
        {
            string fileName = Path.GetFileName(path);
            return fileName.EndsWith(Extension, StringComparison.Ordinal) ? fileName.Substring(0, fileName.Length - Extension.Length) : null;
        }

        /// <summary>이 기기의 맵 목록. 최근에 저장한 맵이 앞에 온다.</summary>
        public static List<MapInfo> List()
        {
            var maps = new List<MapInfo>();
            string directory = MapStorage.Directory;
            if (!Directory.Exists(directory)) return maps;

            foreach (string path in Directory.GetFiles(directory, "*" + Extension))
            {
                string id = StemOf(path);
                if (!IsValidId(id)) continue;

                maps.Add(Describe(id, path, ThumbnailPathOf(id)));
            }

            maps.Sort((a, b) =>
            {
                int byTime = b.UpdatedAt.CompareTo(a.UpdatedAt);
                return byTime != 0 ? byTime : string.CompareOrdinal(a.Id, b.Id);
            });
            return maps;
        }

        /// <summary>
        /// 휴지통에 있는 맵의 목록. 최근에 지운 맵이 앞에 온다. Id는 지우기 전의 번호표이고, 되살리거나 아주 지울 때는 TrashKey로 가리킨다.
        /// 이름이 휴지통의 꼴(번호표-날짜-시각)이 아닌 파일은 넣지 않는다.
        /// </summary>
        public static List<MapInfo> ListTrash()
        {
            var maps = new List<MapInfo>();
            string directory = TrashDirectory;
            if (!Directory.Exists(directory)) return maps;

            foreach (string path in Directory.GetFiles(directory, "*" + Extension))
            {
                string key = StemOf(path);
                if (!IsValidTrashKey(key)) continue;

                Match match = TrashName.Match(key);
                MapInfo info = Describe(match.Groups["id"].Value, path, TrashThumbnailPathOf(key));
                info.InTrash = true;
                info.TrashKey = key;
                info.DeletedAt = ParseStamp(match.Groups["date"].Value + match.Groups["time"].Value);
                maps.Add(info);
            }

            maps.Sort((a, b) =>
            {
                int byTime = b.DeletedAt.CompareTo(a.DeletedAt);
                return byTime != 0 ? byTime : string.CompareOrdinal(a.TrashKey, b.TrashKey);
            });
            return maps;
        }

        /// <summary>휴지통의 파일 이름에 붙은 날짜와 시각(yyyyMMddHHmmss, UTC)을 읽는다. 읽지 못하면 DateTime.MinValue다.</summary>
        private static DateTime ParseStamp(string stamp)
        {
            return DateTime.TryParseExact(stamp, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime parsed)
                ? parsed
                : DateTime.MinValue;
        }

        private static MapInfo Describe(string id, string path, string thumbnailPath)
        {
            var info = new MapInfo
            {
                Id = id,
                Name = string.Empty,
                Description = string.Empty,
                UpdatedAt = DateTime.MinValue,
                CreatedAt = DateTime.MinValue,
                Sky = MapSky.DefaultId,
                SunYaw = MapSky.DefaultSunYaw,
                SunPitch = MapSky.DefaultSunPitch,
                FloorSize = MapSize.Default,
            };
            info.HasThumbnail = File.Exists(thumbnailPath);
            if (!MapStorage.TryLoad(path, out MapDocument document, out _)) return info;

            info.Readable = true;
            info.Name = document.name ?? string.Empty;
            info.Description = document.description ?? string.Empty;
            info.BlockCount = document.blocks != null ? document.blocks.Count : 0;
            info.UpdatedAt = ParseTime(document.updatedAt, path);
            info.CreatedAt = ParseTime(document.createdAt, path);
            info.HasSpawn = document.spawn != null && document.spawn.custom;
            info.Sky = document.environment.sky;
            info.SunYaw = document.environment.sunYaw;
            info.SunPitch = document.environment.sunPitch;
            info.FloorSize = MapSize.FromBounds(document.bounds);
            return info;
        }

        private static DateTime ParseTime(string text, string path)
        {
            if (DateTime.TryParseExact(text, MapDocument.TimeFormat, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime parsed))
            {
                return parsed;
            }

            try
            {
                return File.GetLastWriteTimeUtc(path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// 시작할 때 열 맵. 마지막으로 연 맵이 있으면 그것, 없으면 처음부터 있던 맵, 그것도 없으면 가장 최근에 저장한 맵이다.
        /// 맵이 하나도 없으면 처음부터 있던 맵의 번호표를 돌려준다(그 이름으로 첫 맵을 만들게 된다).
        /// </summary>
        public static string ResolveCurrent()
        {
            string remembered = ReadCurrent();
            if (Exists(remembered)) return remembered;
            if (Exists(DefaultId)) return DefaultId;

            List<MapInfo> maps = List();
            return maps.Count > 0 ? maps[0].Id : DefaultId;
        }

        /// <summary>마지막으로 연 맵으로 적혀 있는 번호표. 적힌 것이 없거나 바르지 않으면 null이다.</summary>
        public static string ReadCurrent()
        {
            string path = Path.Combine(MapStorage.Directory, CurrentFileName);
            try
            {
                if (!File.Exists(path)) return null;

                string id = File.ReadAllText(path, Encoding.UTF8).Trim();
                return IsValidId(id) ? id : null;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>
        /// 마지막으로 연 맵을 적어 둔다. 처음부터 있던 맵뿐이고 적어 둔 것도 없으면 파일을 만들지 않는다(적지 않아도 그 맵이 열린다).
        /// 적지 못해도 맵을 여는 데는 지장이 없으므로 실패는 조용히 넘긴다.
        /// </summary>
        public static void RememberCurrent(string id)
        {
            if (!IsValidId(id)) return;

            string path = Path.Combine(MapStorage.Directory, CurrentFileName);
            try
            {
                if (id == DefaultId && !File.Exists(path)) return;
                if (ReadCurrent() == id) return;

                Directory.CreateDirectory(MapStorage.Directory);
                File.WriteAllText(path, id, Utf8NoBom);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Atelier Verse] 마지막으로 연 맵을 적어 두지 못했습니다: {exception.Message}");
            }
        }

        /// <summary>아직 쓰이지 않은 새 번호표. 만든 시각으로 짓고, 같은 것이 있으면 뒤에 번호를 붙인다.</summary>
        public static string NewId()
        {
            string stem = $"map-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            string id = stem;
            for (int i = 2; File.Exists(PathOf(id)); i++)
            {
                id = $"{stem}-{i}";
            }

            return id;
        }

        /// <summary>
        /// 빈 맵을 만들어 파일로 쓰고 번호표를 돌려준다. 이름은 다듬어 넣고, 같은 이름이 있으면 뒤에 번호를 붙인다.
        /// 맵이 상한에 닿았으면 만들지 않고 null을 돌려준다. 쓰지 못하면 IOException이나 UnauthorizedAccessException을 던진다.
        /// </summary>
        public static string Create(string name, Vector3 minCorner, Vector3 maxCorner)
        {
            List<MapInfo> maps = List();
            if (maps.Count >= MaxMaps) return null;

            string id = NewId();
            MapDocument document = MapDocument.Create(UniqueName(CleanName(name), maps), minCorner, maxCorner);
            MapStorage.Save(document, PathOf(id));
            return id;
        }

        /// <summary>
        /// 맵의 사본을 만들어 새 번호표를 돌려준다. 블록, 설명, 시작 위치, 대표 그림이 같고 이름 뒤에 " 사본"이 붙는다
        /// (같은 이름이 있으면 번호가 더 붙는다). 만든 때와 고친 때는 지금이다. 원래 맵은 건드리지 않는다.
        /// 맵이 상한에 닿았거나, 원래 맵을 읽지 못하거나, 쓰지 못하면 null이다.
        /// </summary>
        public static string Duplicate(string id)
        {
            if (!IsValidId(id)) return null;

            List<MapInfo> maps = List();
            if (maps.Count >= MaxMaps) return null;
            if (!MapStorage.TryLoad(PathOf(id), out MapDocument document, out _)) return null;

            string copyId = NewId();
            string now = MapDocument.Now();
            document.name = UniqueName(CopyName(document.name), maps);
            document.createdAt = now;
            document.updatedAt = now;

            try
            {
                MapStorage.Save(document, PathOf(copyId));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Atelier Verse] 맵의 사본을 만들지 못했습니다: {exception.Message}");
                return null;
            }

            CopyFileQuietly(ThumbnailPathOf(id), ThumbnailPathOf(copyId));
            return copyId;
        }

        /// <summary>사본에 붙일 이름. 이름이 길면 " 사본"이 잘리지 않게 앞을 줄인다. 이름이 비어 있으면 "새 맵 사본"이다.</summary>
        public static string CopyName(string name)
        {
            string stem = CleanName(name);
            if (stem.Length == 0) stem = DefaultName;

            int room = MaxNameLength - CopySuffix.Length;
            if (stem.Length > room) stem = stem.Substring(0, room).Trim();
            return stem + CopySuffix;
        }

        /// <summary>
        /// 휴지통의 맵을 되살려 맵 목록으로 돌려놓고 번호표를 돌려준다. 지우기 전의 번호표가 비어 있으면 그 번호표로,
        /// 그 사이에 같은 번호표의 맵이 생겼으면 새 번호표로 돌아온다. 대표 그림도 함께 돌아온다.
        /// 맵이 상한에 닿았거나, 휴지통에 그 파일이 없거나, 옮기지 못하면 null이다.
        /// </summary>
        public static string Restore(string key)
        {
            if (!IsValidTrashKey(key)) return null;

            string source = TrashPathOf(key);
            if (!File.Exists(source) || Count() >= MaxMaps) return null;

            string id = TrashName.Match(key).Groups["id"].Value;
            if (File.Exists(PathOf(id))) id = NewId();

            try
            {
                Directory.CreateDirectory(MapStorage.Directory);
                File.Move(source, PathOf(id));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Atelier Verse] 휴지통의 맵을 되살리지 못했습니다: {exception.Message}");
                return null;
            }

            MoveFileQuietly(TrashThumbnailPathOf(key), ThumbnailPathOf(id));
            return id;
        }

        /// <summary>
        /// 휴지통의 맵을 아주 지운다. 맵 파일과 대표 그림이 없어지며 되찾을 수 없다. 맵 목록에 있는 맵은 이것으로 지울 수 없다
        /// (먼저 Trash로 휴지통에 옮겨야 한다). 지웠으면 true다.
        /// </summary>
        public static bool Purge(string key)
        {
            if (!IsValidTrashKey(key)) return false;

            string path = TrashPathOf(key);
            if (!File.Exists(path)) return false;

            try
            {
                File.Delete(path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Atelier Verse] 휴지통의 맵을 지우지 못했습니다: {exception.Message}");
                return false;
            }

            try
            {
                string thumbnail = TrashThumbnailPathOf(key);
                if (File.Exists(thumbnail)) File.Delete(thumbnail);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Atelier Verse] 휴지통의 대표 그림을 지우지 못했습니다: {exception.Message}");
            }

            return true;
        }

        /// <summary>휴지통에 있는 맵의 대표 그림(PNG)을 읽는다. 없거나 읽지 못하면 null이다.</summary>
        public static byte[] LoadTrashThumbnail(string key)
        {
            if (!IsValidTrashKey(key)) return null;

            string path = TrashThumbnailPathOf(key);
            try
            {
                return File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>파일을 베낀다. 대표 그림처럼 없어도 되는 파일에 쓰며, 원래 파일이 없거나 베끼지 못해도 넘어간다.</summary>
        private static void CopyFileQuietly(string from, string to)
        {
            try
            {
                if (File.Exists(from)) File.Copy(from, to, true);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Atelier Verse] 대표 그림을 베끼지 못했습니다: {exception.Message}");
            }
        }

        /// <summary>파일을 옮긴다. 옮길 자리에 이미 파일이 있으면 그것을 대신한다. 원래 파일이 없거나 옮기지 못해도 넘어간다.</summary>
        private static void MoveFileQuietly(string from, string to)
        {
            try
            {
                if (!File.Exists(from)) return;

                if (File.Exists(to)) File.Delete(to);
                File.Move(from, to);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Atelier Verse] 대표 그림을 옮기지 못했습니다: {exception.Message}");
            }
        }

        /// <summary>맵의 이름을 바꾼다. 번호표와 블록은 그대로다. 파일이 없거나 읽고 쓰지 못하면 false다.</summary>
        public static bool Rename(string id, string name)
        {
            return UpdateInfo(id, name, null);
        }

        /// <summary>
        /// 맵의 이름과 설명을 바꾼다. 번호표와 블록은 그대로다. description이 null이면 설명은 그대로 둔다.
        /// 이름이 비어 있거나, 파일이 없거나, 읽고 쓰지 못하면 false다. 옛 판의 파일이면 원래 파일의 사본을 남기고 지금 판으로 쓴다.
        /// </summary>
        public static bool UpdateInfo(string id, string name, string description)
        {
            string cleaned = CleanName(name);
            if (!IsValidId(id) || cleaned.Length == 0) return false;

            string path = PathOf(id);
            if (!MapStorage.TryLoad(path, out MapDocument document, out _)) return false;

            document.name = cleaned;
            if (description != null) document.description = CleanDescription(description);
            if (document.WasUpgraded) MapStorage.Backup(path, $".v{document.LoadedVersion}.bak");
            try
            {
                MapStorage.Save(document, path);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// 맵을 지운다. 파일을 없애지 않고 휴지통 폴더로 옮겨, 잘못 지웠을 때 파일을 되찾을 수 있게 한다.
        /// 옮긴 파일의 경로를 돌려주며, 옮기지 못하면 null이다.
        /// </summary>
        public static string Trash(string id)
        {
            if (!IsValidId(id)) return null;

            string path = PathOf(id);
            if (!File.Exists(path)) return null;

            try
            {
                Directory.CreateDirectory(TrashDirectory);
                string moved = Path.Combine(TrashDirectory, $"{id}-{DateTime.UtcNow:yyyyMMdd-HHmmss}{Extension}");
                for (int i = 2; File.Exists(moved); i++)
                {
                    moved = Path.Combine(TrashDirectory, $"{id}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{i}{Extension}");
                }

                File.Move(path, moved);
                MoveThumbnailToTrash(id, moved);
                return moved;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Atelier Verse] 맵 파일을 휴지통 폴더로 옮기지 못했습니다: {exception.Message}");
                return null;
            }
        }

        /// <summary>지운 맵의 대표 그림도 휴지통의 맵 파일 옆으로 옮긴다. 그림은 없어도 되는 것이라 옮기지 못해도 넘어간다.</summary>
        private static void MoveThumbnailToTrash(string id, string movedMapPath)
        {
            string thumbnail = ThumbnailPathOf(id);
            if (!File.Exists(thumbnail)) return;

            try
            {
                string stem = movedMapPath.Substring(0, movedMapPath.Length - Extension.Length);
                File.Move(thumbnail, stem + ThumbnailExtension);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Atelier Verse] 대표 그림을 휴지통 폴더로 옮기지 못했습니다: {exception.Message}");
            }
        }

        /// <summary>
        /// 맵의 대표 그림(PNG)을 맵 파일 옆에 쓴다. 임시 파일에 먼저 쓰고 이름을 바꾼다. 쓰지 못하면 false다.
        /// </summary>
        public static bool SaveThumbnail(string id, byte[] png)
        {
            if (!IsValidId(id) || png == null || png.Length == 0) return false;

            string path = ThumbnailPathOf(id);
            string temp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(MapStorage.Directory);
                File.WriteAllBytes(temp, png);
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Atelier Verse] 대표 그림을 저장하지 못했습니다: {exception.Message}");
                return false;
            }
        }

        /// <summary>맵의 대표 그림(PNG)을 읽는다. 없거나 읽지 못하면 null이다.</summary>
        public static byte[] LoadThumbnail(string id)
        {
            if (!IsValidId(id)) return null;

            string path = ThumbnailPathOf(id);
            try
            {
                return File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>
        /// 이름을 다듬는다: 앞뒤의 빈칸을 떼고, 줄 바꿈 같은 보이지 않는 글자를 빈칸으로 바꾸고, 이어진 빈칸을 하나로 줄이고,
        /// 너무 길면 자른다. 남는 글자가 없으면 빈 문자열이다.
        /// </summary>
        public static string CleanName(string name)
        {
            return Clean(name, MaxNameLength);
        }

        /// <summary>설명을 이름과 같은 방법으로 다듬는다. 설명은 비어 있어도 된다.</summary>
        public static string CleanDescription(string description)
        {
            return Clean(description, MapDocument.MaxDescriptionLength);
        }

        private static string Clean(string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            var cleaned = new StringBuilder(name.Length);
            bool lastWasSpace = true;
            foreach (char letter in name)
            {
                bool space = char.IsWhiteSpace(letter) || char.IsControl(letter);
                if (space)
                {
                    if (!lastWasSpace) cleaned.Append(' ');
                    lastWasSpace = true;
                    continue;
                }

                cleaned.Append(letter);
                lastWasSpace = false;
            }

            string result = cleaned.ToString().Trim();
            return result.Length > maxLength ? result.Substring(0, maxLength).Trim() : result;
        }

        /// <summary>목록에 같은 이름이 있으면 뒤에 번호를 붙인다("새 맵", "새 맵 2", "새 맵 3" …). 이름이 비어 있으면 "새 맵"으로 짓는다.</summary>
        public static string UniqueName(string name, IReadOnlyList<MapInfo> maps)
        {
            string stem = string.IsNullOrEmpty(name) ? DefaultName : name;
            string candidate = stem;

            for (int number = 2; Contains(maps, candidate); number++)
            {
                candidate = $"{stem} {number}";
            }

            return candidate;
        }

        /// <summary>화면에 보일 이름. 이름이 비어 있으면 "이름 없는 맵"이다.</summary>
        public static string DisplayName(string name)
        {
            return string.IsNullOrWhiteSpace(name) ? UnnamedLabel : name;
        }

        private static bool Contains(IReadOnlyList<MapInfo> maps, string name)
        {
            if (maps == null) return false;

            foreach (MapInfo map in maps)
            {
                if (string.Equals(map.Name, name, StringComparison.Ordinal)) return true;
            }

            return false;
        }
    }
}
