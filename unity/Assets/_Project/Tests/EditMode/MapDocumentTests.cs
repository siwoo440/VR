using System.Collections.Generic;
using System.IO;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 맵 파일 형식(2판)의 검사. 저장하고 다시 읽은 문서가 같은지, 잘못된 파일을 거르는지, 문제 있는 블록을 건너뛰는지,
    /// 블록을 칸으로 적던 1판의 파일을 올려 읽는지 확인한다.
    /// </summary>
    public class MapDocumentTests
    {
        private static readonly Dictionary<string, int> Parts = new Dictionary<string, int>
        {
            { "block.gold", 0 },
            { "block.blue", 1 },
            { "block.clay", 2 },
        };

        private static readonly string[] PartIds = { "block.gold", "block.blue", "block.clay" };

        // 1판의 파일. 블록을 모눈의 칸과 방향(0~3)으로 적었다.
        private const string VersionOneJson = "{\"format\":\"atelier-verse-map\",\"version\":1,\"name\":\"옛 맵\",\"createdAt\":\"2026-10-08T03:51:00Z\",\"updatedAt\":\"2026-10-08T04:12:30Z\","
            + "\"bounds\":{\"min\":{\"x\":-2,\"y\":0,\"z\":-2},\"max\":{\"x\":2,\"y\":3,\"z\":2}},"
            + "\"blocks\":[{\"part\":\"block.gold\",\"cell\":{\"x\":1,\"y\":0,\"z\":-1},\"facing\":0},{\"part\":\"block.blue\",\"cell\":{\"x\":-2,\"y\":2,\"z\":0},\"facing\":1},{\"part\":\"block.clay\",\"cell\":{\"x\":0,\"y\":0,\"z\":0},\"facing\":7}],"
            + "\"assemblies\":[],\"rules\":[]}";

        private static int IndexOf(string id)
        {
            return Parts.TryGetValue(id, out int index) ? index : -1;
        }

        private static string IdOf(int index)
        {
            return index >= 0 && index < PartIds.Length ? PartIds[index] : null;
        }

        // 1판의 칸 범위 (-2,0,-2)~(2,3,2)와 같은 상자.
        private static BlockMap CreateMap(int capacity = 10)
        {
            return new BlockMap(new Vector3(-2f, 0f, -2f), new Vector3(3f, 4f, 3f), capacity);
        }

        [Test]
        public void 저장하고_다시_읽으면_같은_번호와_자리와_방향의_블록이_나온다()
        {
            BlockMap source = CreateMap();
            source.Add(0, new Vector3(1.37f, 0.5f, -0.82f), Quaternion.identity, out _);
            source.Add(1, new Vector3(0.5f, 2.5f, 2.5f), Quaternion.Euler(0f, 30f, 0f), out _);
            source.Add(2, new Vector3(-1.5f, 1.25f, 0.004f), Quaternion.Euler(0f, 270f, 0f), out _);

            MapDocument header = MapDocument.Create("시험", source.Min, source.Max);
            MapDocument saved = MapDocument.FromBlocks(header, source.Blocks, IdOf);
            string json = MapDocument.ToJson(saved);

            Assert.IsTrue(MapDocument.TryParse(json, out MapDocument loaded, out MapFileError error), error.ToString());
            Assert.AreEqual(MapDocument.FormatName, loaded.format);
            Assert.AreEqual(MapDocument.CurrentVersion, loaded.version);
            Assert.IsFalse(loaded.WasUpgraded);
            Assert.AreEqual("시험", loaded.name);
            Assert.AreEqual(header.createdAt, loaded.createdAt);
            Assert.AreEqual(3, loaded.blocks.Count);
            Assert.AreEqual(source.Min, loaded.bounds.min);
            Assert.AreEqual(source.Max, loaded.bounds.max);

            BlockMap target = CreateMap();
            MapLoadReport report = MapDocument.Apply(loaded, target, IndexOf);

            Assert.AreEqual(3, report.Loaded);
            Assert.AreEqual(0, report.Skipped);
            foreach (BlockRecord expected in source.Blocks)
            {
                Assert.IsTrue(target.TryGet(expected.Id, out BlockRecord actual), $"{expected.Id}번 블록이 없습니다.");
                Assert.AreEqual(expected.Part, actual.Part);
                Assert.Less(Vector3.Distance(expected.Position, actual.Position), 0.0001f, $"{expected.Id}번 블록의 자리가 달라졌습니다.");
                Assert.Less(Quaternion.Angle(expected.Rotation, actual.Rotation), 0.1f, $"{expected.Id}번 블록의 방향이 달라졌습니다.");
            }
        }

        [Test]
        public void 블록은_번호와_저장용_이름과_자리로_적히고_칸은_적히지_않는다()
        {
            BlockMap source = CreateMap();
            source.Add(1, new Vector3(0.25f, 0.5f, 0.75f), Quaternion.identity, out _);

            string json = MapDocument.ToJson(MapDocument.FromBlocks(null, source.Blocks, IdOf));

            StringAssert.Contains("\"version\": 2", json);
            StringAssert.Contains("\"id\": 1", json);
            StringAssert.Contains("\"part\"", json);
            StringAssert.Contains("\"block.blue\"", json);
            StringAssert.Contains("\"position\"", json);
            StringAssert.Contains("\"rotation\"", json);
            StringAssert.DoesNotContain("partIndex", json);
            StringAssert.DoesNotContain("\"cell\"", json);
            StringAssert.DoesNotContain("\"facing\"", json);
            StringAssert.DoesNotContain("LoadedVersion", json);
        }

        [Test]
        public void 블록을_칸으로_적은_1판의_파일은_칸의_가운데_자리로_올려_읽는다()
        {
            Assert.IsTrue(MapDocument.TryParse(VersionOneJson, out MapDocument document, out MapFileError error), error.ToString());

            Assert.AreEqual(MapDocument.CurrentVersion, document.version);
            Assert.AreEqual(1, document.LoadedVersion);
            Assert.IsTrue(document.WasUpgraded);
            Assert.AreEqual("옛 맵", document.name);
            Assert.AreEqual("2026-10-08T03:51:00Z", document.createdAt);
            Assert.AreEqual(new Vector3(-2f, 0f, -2f), document.bounds.min);
            Assert.AreEqual(new Vector3(3f, 4f, 3f), document.bounds.max, "칸 범위의 가장 큰 칸은 그 칸의 위 모서리까지가 됩니다.");

            Assert.AreEqual(3, document.blocks.Count);
            Assert.AreEqual(1, document.blocks[0].id);
            Assert.AreEqual("block.gold", document.blocks[0].part);
            Assert.AreEqual(new Vector3(1.5f, 0.5f, -0.5f), document.blocks[0].position);
            Assert.AreEqual(2, document.blocks[1].id);
            Assert.AreEqual(new Vector3(-1.5f, 2.5f, 0.5f), document.blocks[1].position);
            Assert.AreEqual(new Vector3(0f, 90f, 0f), document.blocks[1].rotation, "1판의 방향 1은 오른쪽으로 90도입니다.");
            Assert.AreEqual(Vector3.zero, document.blocks[2].rotation, "1판의 방향이 0~3 밖이면 돌리지 않은 것으로 읽습니다.");

            BlockMap map = CreateMap();
            MapLoadReport report = MapDocument.Apply(document, map, IndexOf);
            Assert.AreEqual(3, report.Loaded);
            Assert.AreEqual(0, report.Skipped, "1판에서 놓을 수 있던 칸은 2판의 범위에도 들어와야 합니다.");
        }

        [Test]
        public void 형식_이름이_다르면_거부한다()
        {
            string json = "{\"format\":\"something-else\",\"version\":1,\"blocks\":[]}";

            Assert.IsFalse(MapDocument.TryParse(json, out MapDocument document, out MapFileError error));
            Assert.IsNull(document);
            Assert.AreEqual(MapFileError.WrongFormat, error);
        }

        [Test]
        public void 판_번호가_없거나_새_판이면_거부한다()
        {
            Assert.IsFalse(MapDocument.TryParse("{\"format\":\"atelier-verse-map\",\"blocks\":[]}", out _, out MapFileError missing));
            Assert.AreEqual(MapFileError.WrongFormat, missing);

            string newer = $"{{\"format\":\"atelier-verse-map\",\"version\":{MapDocument.CurrentVersion + 1},\"blocks\":[]}}";
            Assert.IsFalse(MapDocument.TryParse(newer, out _, out MapFileError tooNew));
            Assert.AreEqual(MapFileError.NewerVersion, tooNew);
        }

        [Test]
        public void 깨진_JSON과_빈_글은_거부한다()
        {
            Assert.IsFalse(MapDocument.TryParse("{\"format\": ", out _, out MapFileError broken));
            Assert.AreEqual(MapFileError.InvalidJson, broken);

            Assert.IsFalse(MapDocument.TryParse("", out _, out MapFileError empty));
            Assert.AreEqual(MapFileError.InvalidJson, empty);

            Assert.IsFalse(MapDocument.TryParse("null", out _, out MapFileError none));
            Assert.AreEqual(MapFileError.InvalidJson, none);
        }

        [Test]
        public void 모르는_부품과_범위_밖과_같은_자리의_중복은_건너뛰고_수를_센다()
        {
            var document = new MapDocument
            {
                format = MapDocument.FormatName,
                version = MapDocument.CurrentVersion,
                blocks = new List<MapBlock>
                {
                    new MapBlock { id = 1, part = "block.gold", position = new Vector3(0.5f, 0.5f, 0.5f) },
                    new MapBlock { id = 2, part = "block.unknown", position = new Vector3(1.5f, 0.5f, 0.5f) },
                    new MapBlock { id = 3, part = "block.blue", position = new Vector3(9.5f, 0.5f, 0.5f) },
                    new MapBlock { id = 4, part = "block.clay", position = new Vector3(0.5f, 0.5f, 0.5f) },
                    new MapBlock { id = 5, part = "", position = new Vector3(2.5f, 0.5f, 0.5f) },
                    null,
                },
            };

            BlockMap map = CreateMap();
            MapLoadReport report = MapDocument.Apply(document, map, IndexOf);

            Assert.AreEqual(1, report.Loaded);
            Assert.AreEqual(2, report.UnknownPart);
            Assert.AreEqual(1, report.OutOfBounds);
            Assert.AreEqual(1, report.Duplicate);
            Assert.AreEqual(4, report.Skipped);
            Assert.IsTrue(map.TryGet(1, out BlockRecord record));
            Assert.AreEqual(0, record.Part, "가운데가 같은 자리의 뒤 블록이 앞 블록을 덮으면 안 됩니다.");
        }

        [Test]
        public void 번호가_없거나_겹치는_블록은_새_번호를_붙여_읽는다()
        {
            var document = new MapDocument
            {
                format = MapDocument.FormatName,
                version = MapDocument.CurrentVersion,
                blocks = new List<MapBlock>
                {
                    new MapBlock { id = 0, part = "block.gold", position = new Vector3(0.5f, 0.5f, 0.5f) },
                    new MapBlock { id = 7, part = "block.blue", position = new Vector3(1.5f, 0.5f, 0.5f) },
                    new MapBlock { id = 7, part = "block.clay", position = new Vector3(2.5f, 0.5f, 0.5f) },
                    new MapBlock { id = 8, part = "block.gold", position = new Vector3(0.5f, 1.5f, 0.5f) },
                },
            };

            BlockMap map = CreateMap();
            MapLoadReport report = MapDocument.Apply(document, map, IndexOf);

            Assert.AreEqual(4, report.Loaded, "번호에 문제가 있어도 블록을 버리지 않습니다.");
            Assert.AreEqual(0, report.Skipped);
            Assert.IsTrue(map.TryGet(7, out BlockRecord seven));
            Assert.AreEqual(1, seven.Part, "같은 번호는 앞의 블록이 가집니다.");
            Assert.IsTrue(map.TryGet(8, out BlockRecord eight));
            Assert.AreEqual(0, eight.Part, "번호가 온전한 뒤의 블록이 새로 붙인 번호에 밀리면 안 됩니다.");
            Assert.IsTrue(map.Contains(9));
            Assert.IsTrue(map.Contains(10));
        }

        [Test]
        public void 상한을_넘는_블록은_상한까지만_읽는다()
        {
            var document = new MapDocument { format = MapDocument.FormatName, version = MapDocument.CurrentVersion };
            for (int i = 0; i < 5; i++)
            {
                document.blocks.Add(new MapBlock { id = i + 1, part = "block.gold", position = new Vector3(-1.5f + i * 0.9f, 0.5f, 0f) });
            }

            BlockMap map = CreateMap(3);
            MapLoadReport report = MapDocument.Apply(document, map, IndexOf);

            Assert.AreEqual(3, report.Loaded);
            Assert.AreEqual(2, report.OverCapacity);
            Assert.AreEqual(3, map.Count);
        }

        [Test]
        public void 항목이_빠진_파일도_빈_값으로_읽힌다()
        {
            foreach (int version in new[] { 1, 2 })
            {
                string json = $"{{\"format\":\"atelier-verse-map\",\"version\":{version}}}";

                Assert.IsTrue(MapDocument.TryParse(json, out MapDocument document, out _), $"{version}판");
                Assert.IsNotNull(document.blocks);
                Assert.IsNotNull(document.bounds);
                Assert.IsNotNull(document.assemblies);
                Assert.IsNotNull(document.rules);
                Assert.AreEqual(string.Empty, document.name);
                Assert.AreEqual(MapDocument.CurrentVersion, document.version);
            }
        }

        [Test]
        public void 파일로_쓰고_읽을_수_있고_임시_파일은_남지_않는다()
        {
            string directory = Path.Combine(Path.GetTempPath(), "atelier-verse-tests", Path.GetRandomFileName());
            string path = Path.Combine(directory, MapStorage.LocalFileName);

            try
            {
                MapDocument first = MapDocument.Create("첫 맵", Vector3.zero, Vector3.one);
                MapStorage.Save(first, path);
                Assert.IsTrue(MapStorage.Exists(path));

                MapDocument second = MapDocument.Create("둘째 맵", Vector3.zero, Vector3.one);
                MapStorage.Save(second, path);
                Assert.IsFalse(File.Exists(path + ".tmp"), "임시 파일이 남아 있습니다.");

                Assert.IsTrue(MapStorage.TryLoad(path, out MapDocument loaded, out MapFileError error), error.ToString());
                Assert.AreEqual("둘째 맵", loaded.name);

                Assert.IsFalse(MapStorage.TryLoad(Path.Combine(directory, "none.json"), out _, out MapFileError missing));
                Assert.AreEqual(MapFileError.Missing, missing);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void 읽지_못한_파일은_옆으로_옮겨_둔다()
        {
            string directory = Path.Combine(Path.GetTempPath(), "atelier-verse-tests", Path.GetRandomFileName());
            string path = Path.Combine(directory, MapStorage.LocalFileName);

            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(path, "{ broken");

                string aside = MapStorage.SetAside(path);

                Assert.IsNotNull(aside);
                Assert.IsFalse(File.Exists(path));
                Assert.IsTrue(File.Exists(aside));
                Assert.AreEqual("{ broken", File.ReadAllText(aside));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void 옛_판의_파일은_고쳐_쓰기_전에_사본을_남길_수_있다()
        {
            string directory = Path.Combine(Path.GetTempPath(), "atelier-verse-tests", Path.GetRandomFileName());
            string path = Path.Combine(directory, MapStorage.LocalFileName);

            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(path, VersionOneJson);

                string copy = MapStorage.Backup(path, ".v1.bak");
                Assert.AreEqual(path + ".v1.bak", copy);
                Assert.IsTrue(File.Exists(path), "사본을 남겨도 원래 파일은 그대로 있어야 합니다.");
                Assert.AreEqual(VersionOneJson, File.ReadAllText(copy));

                // 이미 사본이 있으면 덮어쓰지 않는다. 처음의 원본을 지키기 위해서다.
                File.WriteAllText(path, "{}");
                Assert.AreEqual(copy, MapStorage.Backup(path, ".v1.bak"));
                Assert.AreEqual(VersionOneJson, File.ReadAllText(copy));

                Assert.IsNull(MapStorage.Backup(Path.Combine(directory, "none.json"), ".v1.bak"));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
