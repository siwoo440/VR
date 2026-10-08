using System.Collections.Generic;
using System.IO;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 맵 파일 형식의 검사. 저장하고 다시 읽은 문서가 같은지, 잘못된 파일을 거르는지, 문제 있는 블록을 건너뛰는지 확인한다.
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

        private static int IndexOf(string id)
        {
            return Parts.TryGetValue(id, out int index) ? index : -1;
        }

        private static string IdOf(int index)
        {
            return index >= 0 && index < PartIds.Length ? PartIds[index] : null;
        }

        private static BlockMap CreateMap(int capacity = 10)
        {
            return new BlockMap(new Vector3Int(-2, 0, -2), new Vector3Int(2, 3, 2), capacity);
        }

        [Test]
        public void 저장하고_다시_읽으면_같은_블록이_나온다()
        {
            BlockMap source = CreateMap();
            source.Place(new Vector3Int(1, 0, -1), 0);
            source.Place(new Vector3Int(0, 2, 2), 1);
            source.Place(new Vector3Int(-2, 1, 0), 2);

            MapDocument header = MapDocument.Create("시험", source.Min, source.Max);
            MapDocument saved = MapDocument.FromBlocks(header, source.Blocks, IdOf);
            string json = MapDocument.ToJson(saved);

            Assert.IsTrue(MapDocument.TryParse(json, out MapDocument loaded, out MapFileError error), error.ToString());
            Assert.AreEqual(MapDocument.FormatName, loaded.format);
            Assert.AreEqual(MapDocument.CurrentVersion, loaded.version);
            Assert.AreEqual("시험", loaded.name);
            Assert.AreEqual(header.createdAt, loaded.createdAt);
            Assert.AreEqual(3, loaded.blocks.Count);

            BlockMap target = CreateMap();
            MapLoadReport report = MapDocument.Apply(loaded, target, IndexOf);

            Assert.AreEqual(3, report.Loaded);
            Assert.AreEqual(0, report.Skipped);
            foreach (KeyValuePair<Vector3Int, int> entry in source.Blocks)
            {
                Assert.IsTrue(target.TryGet(entry.Key, out int part), $"{entry.Key} 칸이 없습니다.");
                Assert.AreEqual(entry.Value, part);
            }
        }

        [Test]
        public void 블록은_번호가_아니라_저장용_이름으로_적힌다()
        {
            BlockMap source = CreateMap();
            source.Place(Vector3Int.zero, 1);

            string json = MapDocument.ToJson(MapDocument.FromBlocks(null, source.Blocks, IdOf));

            StringAssert.Contains("\"part\"", json);
            StringAssert.Contains("\"block.blue\"", json);
            StringAssert.DoesNotContain("partIndex", json);
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
        public void 모르는_부품과_범위_밖과_중복은_건너뛰고_수를_센다()
        {
            var document = new MapDocument
            {
                format = MapDocument.FormatName,
                version = MapDocument.CurrentVersion,
                blocks = new List<MapBlock>
                {
                    new MapBlock { part = "block.gold", cell = new Vector3Int(0, 0, 0) },
                    new MapBlock { part = "block.unknown", cell = new Vector3Int(1, 0, 0) },
                    new MapBlock { part = "block.blue", cell = new Vector3Int(9, 0, 0) },
                    new MapBlock { part = "block.clay", cell = new Vector3Int(0, 0, 0) },
                    new MapBlock { part = "", cell = new Vector3Int(2, 0, 0) },
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
            Assert.IsTrue(map.TryGet(Vector3Int.zero, out int part));
            Assert.AreEqual(0, part, "같은 칸의 뒤 블록이 앞 블록을 덮으면 안 됩니다.");
        }

        [Test]
        public void 상한을_넘는_블록은_상한까지만_읽는다()
        {
            var document = new MapDocument { format = MapDocument.FormatName, version = MapDocument.CurrentVersion };
            for (int x = -2; x <= 2; x++)
            {
                document.blocks.Add(new MapBlock { part = "block.gold", cell = new Vector3Int(x, 0, 0) });
            }

            BlockMap map = CreateMap(3);
            MapLoadReport report = MapDocument.Apply(document, map, IndexOf);

            Assert.AreEqual(3, report.Loaded);
            Assert.AreEqual(2, report.OverCapacity);
            Assert.AreEqual(3, map.Count);
        }

        [Test]
        public void 방향_값이_범위_밖이면_0으로_읽는다()
        {
            string json = "{\"format\":\"atelier-verse-map\",\"version\":1,\"blocks\":[{\"part\":\"block.gold\",\"cell\":{\"x\":0,\"y\":0,\"z\":0},\"facing\":7}]}";

            Assert.IsTrue(MapDocument.TryParse(json, out MapDocument document, out _));
            Assert.AreEqual(0, document.blocks[0].facing);
        }

        [Test]
        public void 항목이_빠진_파일도_빈_값으로_읽힌다()
        {
            string json = "{\"format\":\"atelier-verse-map\",\"version\":1}";

            Assert.IsTrue(MapDocument.TryParse(json, out MapDocument document, out _));
            Assert.IsNotNull(document.blocks);
            Assert.IsNotNull(document.bounds);
            Assert.IsNotNull(document.assemblies);
            Assert.IsNotNull(document.rules);
            Assert.AreEqual(string.Empty, document.name);
        }

        [Test]
        public void 파일로_쓰고_읽을_수_있고_임시_파일은_남지_않는다()
        {
            string directory = Path.Combine(Path.GetTempPath(), "atelier-verse-tests", Path.GetRandomFileName());
            string path = Path.Combine(directory, MapStorage.LocalFileName);

            try
            {
                MapDocument first = MapDocument.Create("첫 맵", Vector3Int.zero, Vector3Int.one);
                MapStorage.Save(first, path);
                Assert.IsTrue(MapStorage.Exists(path));

                MapDocument second = MapDocument.Create("둘째 맵", Vector3Int.zero, Vector3Int.one);
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
    }
}
