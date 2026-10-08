using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>
    /// 파일에 적는 블록 하나. 부품은 번호가 아니라 저장용 이름으로 적는다.
    /// 자리는 블록 가운데의 좌표이고 방향은 x·y·z 축으로 돈 각도(도)다. 길이의 단위는 표준 블록 한 변(1)이다.
    /// </summary>
    [Serializable]
    public class MapBlock
    {
        public int id;
        public string part;
        public Vector3 position;
        public Vector3 rotation;
    }

    /// <summary>놓을 수 있는 범위. 상자의 두 모서리다.</summary>
    [Serializable]
    public class MapBounds
    {
        public Vector3 min;
        public Vector3 max;
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
    /// 맵 파일의 구조(형식 2판). 자세한 설명은 docs/MAP-FORMAT.md에 있다.
    /// JSON으로 바꾸는 것과 읽은 문서를 검사하는 것만 맡고, 파일을 다루는 일은 MapStorage가 맡는다.
    /// 1판(블록을 칸으로 적던 형식)의 파일은 읽을 때 2판으로 올린다. 조립품과 동작 규칙은 자리만 두었으며 지금은 항상 비어 있다.
    /// </summary>
    [Serializable]
    public class MapDocument
    {
        public const string FormatName = "atelier-verse-map";
        public const int CurrentVersion = 2;
        public const string TimeFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

        // 1판의 방향은 0~3(90도씩)이었다.
        private const int FacingCountV1 = 4;
        private const float AnglePrecision = 0.001f;

        public string format;
        public int version;
        public string name;
        public string createdAt;
        public string updatedAt;
        public MapBounds bounds = new MapBounds();
        public List<MapBlock> blocks = new List<MapBlock>();
        public string[] assemblies = Array.Empty<string>();
        public string[] rules = Array.Empty<string>();

        /// <summary>파일에 적혀 있던 판 번호. 지금 판보다 작으면 읽으면서 올린 것이다. 파일에는 적지 않는다.</summary>
        public int LoadedVersion { get; private set; } = CurrentVersion;

        /// <summary>옛 판의 파일을 읽으면서 지금 판으로 올렸는지.</summary>
        public bool WasUpgraded => LoadedVersion < CurrentVersion;

        /// <summary>지금 판의 빈 문서를 만든다. 범위는 상자의 두 모서리다. 만든 시각과 저장 시각은 지금이다.</summary>
        public static MapDocument Create(string mapName, Vector3 minCorner, Vector3 maxCorner)
        {
            string now = Now();
            return new MapDocument
            {
                format = FormatName,
                version = CurrentVersion,
                name = mapName ?? string.Empty,
                createdAt = now,
                updatedAt = now,
                bounds = new MapBounds { min = Vector3.Min(minCorner, maxCorner), max = Vector3.Max(minCorner, maxCorner) },
            };
        }

        /// <summary>블록 기록을 문서로 옮긴다. 부품 번호는 partIdOf로 저장용 이름이 된다. 블록은 번호 순으로 적는다.</summary>
        public static MapDocument FromBlocks(MapDocument header, IEnumerable<BlockRecord> blocks, Func<int, string> partIdOf)
        {
            var document = new MapDocument
            {
                format = FormatName,
                version = CurrentVersion,
                name = header?.name ?? string.Empty,
                createdAt = string.IsNullOrEmpty(header?.createdAt) ? Now() : header.createdAt,
                updatedAt = Now(),
                bounds = new MapBounds { min = header?.bounds?.min ?? Vector3.zero, max = header?.bounds?.max ?? Vector3.zero },
            };

            foreach (BlockRecord record in blocks)
            {
                string id = partIdOf(record.Part);
                if (string.IsNullOrEmpty(id)) continue;
                document.blocks.Add(new MapBlock { id = record.Id, part = id, position = record.Position, rotation = ToAngles(record.Rotation) });
            }

            document.blocks.Sort((a, b) => a.id.CompareTo(b.id));
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

            // 판마다 블록의 모양이 달라, 먼저 형식 이름과 판 번호만 읽고 그 판의 구조로 다시 읽는다.
            MapHeader header;
            MapDocument parsed = null;
            try
            {
                header = JsonUtility.FromJson<MapHeader>(json);
                if (header != null && header.format == FormatName && header.version >= 1 && header.version <= CurrentVersion)
                {
                    parsed = header.version == 1
                        ? UpgradeFromV1(JsonUtility.FromJson<MapDocumentV1>(json))
                        : JsonUtility.FromJson<MapDocument>(json);
                }
            }
            catch (ArgumentException)
            {
                error = MapFileError.InvalidJson;
                return false;
            }

            if (header == null)
            {
                error = MapFileError.InvalidJson;
                return false;
            }

            if (header.format != FormatName || header.version < 1)
            {
                error = MapFileError.WrongFormat;
                return false;
            }

            if (header.version > CurrentVersion)
            {
                error = MapFileError.NewerVersion;
                return false;
            }

            if (parsed == null)
            {
                error = MapFileError.InvalidJson;
                return false;
            }

            parsed.LoadedVersion = header.version;
            parsed.version = CurrentVersion;
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
        /// 문서의 블록을 기록에 올린다. 모르는 부품, 범위 밖, 가운데가 같은 자리의 중복, 상한 초과는 건너뛰고 수를 센다.
        /// 번호가 없거나(1보다 작음) 앞의 블록과 같으면 새 번호를 붙여 올린다(건너뛰지 않는다).
        /// partIndexOf는 저장용 이름을 부품 번호로 바꾸며 모르면 -1을 돌려준다.
        /// </summary>
        public static MapLoadReport Apply(MapDocument document, BlockMap map, Func<string, int> partIndexOf)
        {
            var report = new MapLoadReport();
            if (document?.blocks == null) return report;

            // 번호가 온전한 블록을 먼저 올려야, 번호가 없는 블록에 새로 붙인 번호가 뒤에 나오는 블록의 번호와 부딪히지 않는다.
            var unnumbered = new List<(MapBlock block, int partIndex)>();

            foreach (MapBlock block in document.blocks)
            {
                if (block == null) continue;

                int partIndex = string.IsNullOrEmpty(block.part) ? -1 : partIndexOf(block.part);
                if (partIndex < 0)
                {
                    report.UnknownPart++;
                    continue;
                }

                if (block.id < 1 || map.Contains(block.id))
                {
                    unnumbered.Add((block, partIndex));
                    continue;
                }

                Count(ref report, map.Restore(new BlockRecord(block.id, partIndex, block.position, Quaternion.Euler(block.rotation))));
            }

            foreach ((MapBlock block, int partIndex) in unnumbered)
            {
                Count(ref report, map.Add(partIndex, block.position, Quaternion.Euler(block.rotation), out _));
            }

            return report;
        }

        public static string Now()
        {
            return DateTime.UtcNow.ToString(TimeFormat, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void Count(ref MapLoadReport report, PlaceResult result)
        {
            switch (result)
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

        /// <summary>방향을 파일에 적을 각도로 바꾼다. 계산에서 생기는 아주 작은 값은 0으로 다듬는다.</summary>
        private static Vector3 ToAngles(Quaternion rotation)
        {
            Vector3 angles = rotation.eulerAngles;
            return new Vector3(Trim(angles.x), Trim(angles.y), Trim(angles.z));
        }

        private static float Trim(float angle)
        {
            float trimmed = Mathf.Round(angle / AnglePrecision) * AnglePrecision;
            return trimmed >= 360f || trimmed <= 0f ? 0f : trimmed;
        }

        /// <summary>
        /// 1판의 문서를 2판으로 올린다. 1판은 블록을 모눈의 칸으로 적었다. 칸의 가운데가 블록의 자리가 되고,
        /// 방향(0~3)은 위에서 보아 시계 방향으로 90도씩 돈 각도가 된다. 번호는 파일에 적힌 순서대로 1부터 붙인다.
        /// 범위는 가장 작은 칸의 아래 모서리부터 가장 큰 칸의 위 모서리까지의 상자가 된다.
        /// </summary>
        private static MapDocument UpgradeFromV1(MapDocumentV1 old)
        {
            if (old == null) return null;

            var document = new MapDocument
            {
                format = FormatName,
                version = CurrentVersion,
                name = old.name,
                createdAt = old.createdAt,
                updatedAt = old.updatedAt,
                assemblies = old.assemblies,
                rules = old.rules,
            };

            if (old.bounds != null)
            {
                document.bounds = new MapBounds
                {
                    min = (Vector3)old.bounds.min * GridMath.DefaultCellSize,
                    max = (Vector3)(old.bounds.max + Vector3Int.one) * GridMath.DefaultCellSize,
                };
            }

            if (old.blocks == null) return document;

            int nextId = 1;
            foreach (MapBlockV1 block in old.blocks)
            {
                if (block == null) continue;

                int facing = block.facing >= 0 && block.facing < FacingCountV1 ? block.facing : 0;
                document.blocks.Add(new MapBlock
                {
                    id = nextId++,
                    part = block.part,
                    position = GridMath.CellToWorldCenter(block.cell),
                    rotation = new Vector3(0f, facing * 90f, 0f),
                });
            }

            return document;
        }

        /// <summary>어느 판인지 알아보는 데 쓰는 머리 부분.</summary>
        [Serializable]
        private class MapHeader
        {
            public string format;
            public int version;
        }

        /// <summary>1판의 블록. 칸과 방향(0~3)으로 적었다.</summary>
        [Serializable]
        private class MapBlockV1
        {
            public string part;
            public Vector3Int cell;
            public int facing;
        }

        [Serializable]
        private class MapBoundsV1
        {
            public Vector3Int min;
            public Vector3Int max;
        }

        /// <summary>1판의 문서 구조. 읽어서 2판으로 올리는 데만 쓴다.</summary>
        [Serializable]
        private class MapDocumentV1
        {
            public string format;
            public int version;
            public string name;
            public string createdAt;
            public string updatedAt;
            public MapBoundsV1 bounds;
            public List<MapBlockV1> blocks;
            public string[] assemblies;
            public string[] rules;
        }
    }
}
