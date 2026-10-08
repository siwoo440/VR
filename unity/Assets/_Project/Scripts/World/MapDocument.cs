using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>파일에 적는 블록 하나. 부품은 번호가 아니라 저장용 이름으로 적는다.</summary>
    [Serializable]
    public class MapBlock
    {
        public string part;
        public Vector3Int cell;
        public int facing;
    }

    /// <summary>놓을 수 있는 범위. min과 max를 포함한다.</summary>
    [Serializable]
    public class MapBounds
    {
        public Vector3Int min;
        public Vector3Int max;
    }

    /// <summary>파일을 읽지 못한 까닭.</summary>
    public enum MapFileError
    {
        None,
        Missing,
        ReadFailed,
        InvalidJson,
        WrongFormat,
        NewerVersion,
    }

    /// <summary>문서를 블록 기록에 올린 결과. 건너뛴 블록은 까닭별로 센다.</summary>
    public struct MapLoadReport
    {
        public int Loaded;
        public int UnknownPart;
        public int OutOfBounds;
        public int Duplicate;
        public int OverCapacity;

        public int Skipped => UnknownPart + OutOfBounds + Duplicate + OverCapacity;
    }

    /// <summary>
    /// 맵 파일의 구조(형식 1판). 자세한 설명은 docs/MAP-FORMAT.md에 있다.
    /// JSON으로 바꾸는 것과 읽은 문서를 검사하는 것만 맡고, 파일을 다루는 일은 MapStorage가 맡는다.
    /// 조립품과 동작 규칙은 자리만 두었으며 지금은 항상 비어 있다.
    /// </summary>
    [Serializable]
    public class MapDocument
    {
        public const string FormatName = "atelier-verse-map";
        public const int CurrentVersion = 1;
        public const int FacingCount = 4;
        public const string TimeFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

        public string format;
        public int version;
        public string name;
        public string createdAt;
        public string updatedAt;
        public MapBounds bounds = new MapBounds();
        public List<MapBlock> blocks = new List<MapBlock>();
        public string[] assemblies = Array.Empty<string>();
        public string[] rules = Array.Empty<string>();

        /// <summary>지금 판의 빈 문서를 만든다. 만든 시각과 저장 시각은 지금이다.</summary>
        public static MapDocument Create(string mapName, Vector3Int minCell, Vector3Int maxCell)
        {
            string now = Now();
            return new MapDocument
            {
                format = FormatName,
                version = CurrentVersion,
                name = mapName ?? string.Empty,
                createdAt = now,
                updatedAt = now,
                bounds = new MapBounds { min = Vector3Int.Min(minCell, maxCell), max = Vector3Int.Max(minCell, maxCell) },
            };
        }

        /// <summary>블록 기록을 문서로 옮긴다. 부품 번호는 partIdOf로 저장용 이름이 된다.</summary>
        public static MapDocument FromBlocks(MapDocument header, IReadOnlyDictionary<Vector3Int, int> blocks, Func<int, string> partIdOf)
        {
            var document = new MapDocument
            {
                format = FormatName,
                version = CurrentVersion,
                name = header?.name ?? string.Empty,
                createdAt = string.IsNullOrEmpty(header?.createdAt) ? Now() : header.createdAt,
                updatedAt = Now(),
                bounds = new MapBounds { min = header?.bounds?.min ?? Vector3Int.zero, max = header?.bounds?.max ?? Vector3Int.zero },
            };

            foreach (KeyValuePair<Vector3Int, int> entry in blocks)
            {
                string id = partIdOf(entry.Value);
                if (string.IsNullOrEmpty(id)) continue;
                document.blocks.Add(new MapBlock { part = id, cell = entry.Key, facing = 0 });
            }

            document.blocks.Sort(CompareCells);
            return document;
        }

        public static string ToJson(MapDocument document)
        {
            return JsonUtility.ToJson(document, true);
        }

        /// <summary>
        /// JSON을 문서로 읽는다. 형식 이름과 판 번호를 검사하고, 옛 판이면 지금 판으로 바꾼다.
        /// 실패하면 document는 null이고 error에 까닭이 담긴다.
        /// </summary>
        public static bool TryParse(string json, out MapDocument document, out MapFileError error)
        {
            document = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = MapFileError.InvalidJson;
                return false;
            }

            MapDocument parsed;
            try
            {
                parsed = JsonUtility.FromJson<MapDocument>(json);
            }
            catch (ArgumentException)
            {
                error = MapFileError.InvalidJson;
                return false;
            }

            if (parsed == null)
            {
                error = MapFileError.InvalidJson;
                return false;
            }

            if (parsed.format != FormatName || parsed.version < 1)
            {
                error = MapFileError.WrongFormat;
                return false;
            }

            if (parsed.version > CurrentVersion)
            {
                error = MapFileError.NewerVersion;
                return false;
            }

            Upgrade(parsed);
            parsed.name ??= string.Empty;
            parsed.createdAt ??= string.Empty;
            parsed.updatedAt ??= string.Empty;
            parsed.bounds ??= new MapBounds();
            parsed.blocks ??= new List<MapBlock>();
            parsed.assemblies ??= Array.Empty<string>();
            parsed.rules ??= Array.Empty<string>();

            document = parsed;
            error = MapFileError.None;
            return true;
        }

        /// <summary>
        /// 문서의 블록을 기록에 올린다. 모르는 부품, 범위 밖, 같은 칸의 중복, 상한 초과는 건너뛰고 수를 센다.
        /// partIndexOf는 저장용 이름을 부품 번호로 바꾸며 모르면 -1을 돌려준다.
        /// </summary>
        public static MapLoadReport Apply(MapDocument document, BlockMap map, Func<string, int> partIndexOf)
        {
            var report = new MapLoadReport();
            if (document?.blocks == null) return report;

            foreach (MapBlock block in document.blocks)
            {
                if (block == null) continue;

                int partIndex = string.IsNullOrEmpty(block.part) ? -1 : partIndexOf(block.part);
                if (partIndex < 0)
                {
                    report.UnknownPart++;
                    continue;
                }

                switch (map.Place(block.cell, partIndex))
                {
                    case PlaceResult.Ok:
                        report.Loaded++;
                        break;
                    case PlaceResult.OutOfBounds:
                        report.OutOfBounds++;
                        break;
                    case PlaceResult.Occupied:
                        report.Duplicate++;
                        break;
                    case PlaceResult.Full:
                        report.OverCapacity++;
                        break;
                    default:
                        report.UnknownPart++;
                        break;
                }
            }

            return report;
        }

        /// <summary>방향 값을 0~3 안으로 맞춘다. 밖의 값은 0이다.</summary>
        public static int ClampFacing(int facing)
        {
            return facing >= 0 && facing < FacingCount ? facing : 0;
        }

        public static string Now()
        {
            return DateTime.UtcNow.ToString(TimeFormat, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>옛 판의 문서를 한 단계씩 지금 판으로 바꾼다. 판을 올릴 때 여기에 단계를 더한다.</summary>
        private static void Upgrade(MapDocument document)
        {
            while (document.version < CurrentVersion)
            {
                // 아직 1판뿐이라 바꿀 단계가 없다. 2판이 생기면 case 1: ... 을 더한다.
                document.version++;
            }

            if (document.blocks == null) return;
            foreach (MapBlock block in document.blocks)
            {
                if (block != null) block.facing = ClampFacing(block.facing);
            }
        }

        private static int CompareCells(MapBlock a, MapBlock b)
        {
            int result = a.cell.y.CompareTo(b.cell.y);
            if (result == 0) result = a.cell.z.CompareTo(b.cell.z);
            if (result == 0) result = a.cell.x.CompareTo(b.cell.x);
            return result;
        }
    }
}
