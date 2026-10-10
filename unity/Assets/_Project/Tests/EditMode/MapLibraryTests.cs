using System;
using System.Collections.Generic;
using System.IO;
using AtelierVerse.Core;
using AtelierVerse.UI;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 이 기기의 맵들을 다루는 규칙의 검사(17일차): 번호표, 이름 다듬기, 목록, 새 맵, 이름 바꾸기, 지우기(휴지통 폴더로 옮기기),
    /// 마지막으로 연 맵 기억하기. 테스트마다 임시 폴더를 쓰므로 이 기기의 실제 맵 파일은 건드리지 않는다.
    /// 사본 만들기, 휴지통의 맵 보기, 되살리기, 아주 지우기도 본다(22일차).
    /// </summary>
    public class MapLibraryTests
    {
        private static readonly Vector3 Min = new Vector3(-8f, 0f, -8f);
        private static readonly Vector3 Max = new Vector3(8f, 12f, 8f);

        private string previousDirectory;
        private string directory;

        [SetUp]
        public void SetUp()
        {
            previousDirectory = MapStorage.Directory;
            directory = Path.Combine(Path.GetTempPath(), "atelier-verse-tests", "library-" + Path.GetRandomFileName());
            MapStorage.Directory = directory;
        }

        [TearDown]
        public void TearDown()
        {
            MapStorage.Directory = previousDirectory;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        /// <summary>휴지통 폴더에 맵 파일을 바로 써 넣는다. key는 휴지통의 파일 이름(번호표-날짜-시각)이다.</summary>
        private static void WriteTrash(string key, string name, int blocks)
        {
            MapDocument document = MapDocument.Create(name, Min, Max);
            for (int i = 0; i < blocks; i++)
            {
                document.blocks.Add(new MapBlock { id = i + 1, part = "block.gold", position = new Vector3(i + 0.5f, 0.5f, 0.5f) });
            }

            MapStorage.Save(document, MapLibrary.TrashPathOf(key));
        }

        private string Save(string id, string name, int blocks, string updatedAt)
        {
            MapDocument document = MapDocument.Create(name, Min, Max);
            for (int i = 0; i < blocks; i++)
            {
                document.blocks.Add(new MapBlock { id = i + 1, part = "block.gold", position = new Vector3(i + 0.5f, 0.5f, 0.5f) });
            }

            document.updatedAt = updatedAt;
            MapStorage.Save(document, MapLibrary.PathOf(id));
            return id;
        }

        [Test]
        public void 번호표는_영문_소문자와_숫자와_줄표만_받는다()
        {
            Assert.IsTrue(MapLibrary.IsValidId("local"));
            Assert.IsTrue(MapLibrary.IsValidId("map-20261010-093000-2"));

            Assert.IsFalse(MapLibrary.IsValidId(null));
            Assert.IsFalse(MapLibrary.IsValidId(string.Empty));
            Assert.IsFalse(MapLibrary.IsValidId("../secret"), "폴더를 벗어나는 글자는 받지 않습니다.");
            Assert.IsFalse(MapLibrary.IsValidId("a\\b"));
            Assert.IsFalse(MapLibrary.IsValidId("Map"));
            Assert.IsFalse(MapLibrary.IsValidId("맵"));
            Assert.IsFalse(MapLibrary.IsValidId(new string('a', MapLibrary.MaxIdLength + 1)));
        }

        [Test]
        public void 이름은_빈칸을_다듬고_너무_길면_자른다()
        {
            Assert.AreEqual("우리 집", MapLibrary.CleanName("  우리   집  "));
            Assert.AreEqual("첫 줄 둘째 줄", MapLibrary.CleanName("첫 줄\n둘째\t줄"));
            Assert.AreEqual(string.Empty, MapLibrary.CleanName("   "));
            Assert.AreEqual(string.Empty, MapLibrary.CleanName(null));
            Assert.AreEqual(MapLibrary.MaxNameLength, MapLibrary.CleanName(new string('가', 60)).Length);
        }

        [Test]
        public void 같은_이름이_있으면_뒤에_번호를_붙인다()
        {
            var maps = new List<MapInfo>
            {
                new MapInfo { Id = "a", Name = "새 맵" },
                new MapInfo { Id = "b", Name = "새 맵 2" },
            };

            Assert.AreEqual("새 맵 3", MapLibrary.UniqueName("새 맵", maps));
            Assert.AreEqual("탑", MapLibrary.UniqueName("탑", maps));
            Assert.AreEqual("새 맵 3", MapLibrary.UniqueName(string.Empty, maps), "이름이 비어 있으면 \"새 맵\"으로 짓습니다.");
            Assert.AreEqual("새 맵", MapLibrary.UniqueName(null, null));
        }

        [Test]
        public void 이름이_비어_있으면_이름_없는_맵으로_보인다()
        {
            Assert.AreEqual(MapLibrary.UnnamedLabel, MapLibrary.DisplayName(string.Empty));
            Assert.AreEqual(MapLibrary.UnnamedLabel, MapLibrary.DisplayName(null));
            Assert.AreEqual("탑", MapLibrary.DisplayName("탑"));
        }

        [Test]
        public void 폴더가_없으면_맵이_없고_처음부터_있던_맵을_열게_된다()
        {
            Assert.AreEqual(0, MapLibrary.List().Count);
            Assert.AreEqual(MapLibrary.DefaultId, MapLibrary.ResolveCurrent());
            Assert.IsFalse(MapLibrary.Exists(MapLibrary.DefaultId));
        }

        [Test]
        public void 목록은_최근에_저장한_맵부터_보이고_이름과_블록_수를_안다()
        {
            Save("local", "시험 작업실", 3, "2026-10-08T03:00:00Z");
            Save("map-b", "탑", 12, "2026-10-10T09:30:00Z");
            Save("map-a", string.Empty, 0, "2026-10-09T01:00:00Z");

            List<MapInfo> maps = MapLibrary.List();

            Assert.AreEqual(3, maps.Count);
            Assert.AreEqual("map-b", maps[0].Id);
            Assert.AreEqual("map-a", maps[1].Id);
            Assert.AreEqual("local", maps[2].Id);
            Assert.AreEqual("탑", maps[0].Name);
            Assert.AreEqual(12, maps[0].BlockCount);
            Assert.IsTrue(maps[0].Readable);
            Assert.AreEqual(new DateTime(2026, 10, 10, 9, 30, 0, DateTimeKind.Utc), maps[0].UpdatedAt);
            Assert.AreEqual(string.Empty, maps[1].Name);
        }

        [Test]
        public void 목록에는_맵_파일만_들어간다()
        {
            Save("local", "시험 작업실", 1, "2026-10-08T03:00:00Z");
            File.WriteAllText(Path.Combine(directory, "local.map.json.v1.bak"), "{}");
            File.WriteAllText(Path.Combine(directory, "local.map.json.tmp"), "{}");
            File.WriteAllText(Path.Combine(directory, "local.map.json.broken-20261008-000000"), "{}");
            File.WriteAllText(Path.Combine(directory, MapLibrary.CurrentFileName), "local");
            File.WriteAllText(Path.Combine(directory, "Bad Name.map.json"), "{}");
            Directory.CreateDirectory(MapLibrary.TrashDirectory);
            File.WriteAllText(Path.Combine(MapLibrary.TrashDirectory, "old-20261008-000000.map.json"), "{}");

            List<MapInfo> maps = MapLibrary.List();

            Assert.AreEqual(1, maps.Count, "사본, 임시 파일, 옮겨 둔 파일, 휴지통의 파일, 번호표가 바르지 않은 파일은 맵이 아닙니다.");
            Assert.AreEqual("local", maps[0].Id);
        }

        [Test]
        public void 읽을_수_없는_파일도_목록에는_보이되_읽을_수_없다고_표시한다()
        {
            Save("local", "시험 작업실", 1, "2026-10-08T03:00:00Z");
            File.WriteAllText(MapLibrary.PathOf("broken"), "{ \"format\": \"atelier-verse-map\", ");

            List<MapInfo> maps = MapLibrary.List();
            MapInfo broken = maps.Find(map => map.Id == "broken");

            Assert.AreEqual(2, maps.Count);
            Assert.IsFalse(broken.Readable);
            Assert.AreEqual("읽을 수 없는 파일", MapListView.Describe(broken));
            Assert.AreEqual("local", maps[0].Id, "읽을 수 있는 맵이 앞에 옵니다.");
        }

        [Test]
        public void 새_맵은_빈_맵이고_이름이_겹치면_번호가_붙는다()
        {
            string first = MapLibrary.Create(null, Min, Max);
            string second = MapLibrary.Create("  새 맵 ", Min, Max);
            string third = MapLibrary.Create("탑", Min, Max);

            Assert.IsTrue(MapLibrary.IsValidId(first));
            Assert.AreNotEqual(first, second, "같은 때에 만들어도 번호표가 겹치면 안 됩니다.");
            Assert.AreNotEqual(second, third);

            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf(first), out MapDocument a, out _));
            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf(second), out MapDocument b, out _));
            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf(third), out MapDocument c, out _));
            Assert.AreEqual("새 맵", a.name);
            Assert.AreEqual("새 맵 2", b.name);
            Assert.AreEqual("탑", c.name);
            Assert.AreEqual(0, a.blocks.Count);
            Assert.AreEqual(MapDocument.CurrentVersion, a.version);
            Assert.AreEqual(Max, a.bounds.max);
        }

        [Test]
        public void 맵이_상한에_닿으면_더_만들지_않는다()
        {
            for (int i = 0; i < MapLibrary.MaxMaps; i++)
            {
                Save($"map-{i}", $"맵 {i}", 0, "2026-10-08T03:00:00Z");
            }

            Assert.IsNull(MapLibrary.Create("하나 더", Min, Max));
            Assert.AreEqual(MapLibrary.MaxMaps, MapLibrary.List().Count);
        }

        [Test]
        public void 이름을_바꿔도_번호표와_블록은_그대로다()
        {
            Save("map-a", "탑", 5, "2026-10-09T01:00:00Z");

            Assert.IsTrue(MapLibrary.Rename("map-a", "  높은   탑 "));

            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf("map-a"), out MapDocument document, out _));
            Assert.AreEqual("높은 탑", document.name);
            Assert.AreEqual(5, document.blocks.Count);
            Assert.AreEqual(1, MapLibrary.List().Count);

            Assert.IsFalse(MapLibrary.Rename("map-a", "   "), "빈 이름으로는 바꾸지 않습니다.");
            Assert.IsFalse(MapLibrary.Rename("no-such-map", "이름"));
            Assert.IsFalse(MapLibrary.Rename("../map-a", "이름"));
        }

        [Test]
        public void 이름과_설명을_함께_바꾸고_설명만_그대로_둘_수도_있다()
        {
            Save("map-a", "탑", 5, "2026-10-09T01:00:00Z");

            Assert.IsTrue(MapLibrary.UpdateInfo("map-a", " 높은 탑 ", "  친구들과   올라가 보는 탑 "));
            MapInfo info = MapLibrary.List()[0];
            Assert.AreEqual("높은 탑", info.Name);
            Assert.AreEqual("친구들과 올라가 보는 탑", info.Description, "설명도 빈칸을 다듬어 넣습니다.");
            Assert.AreEqual(5, info.BlockCount);

            // 설명을 주지 않으면(null) 그대로 두고, 빈 글자를 주면 지운다.
            Assert.IsTrue(MapLibrary.UpdateInfo("map-a", "탑", null));
            Assert.AreEqual("친구들과 올라가 보는 탑", MapLibrary.List()[0].Description);
            Assert.IsTrue(MapLibrary.UpdateInfo("map-a", "탑", string.Empty));
            Assert.AreEqual(string.Empty, MapLibrary.List()[0].Description);

            Assert.IsTrue(MapLibrary.UpdateInfo("map-a", "탑", new string('가', 200)));
            Assert.AreEqual(MapDocument.MaxDescriptionLength, MapLibrary.List()[0].Description.Length, "설명이 너무 길면 자릅니다.");
            Assert.IsFalse(MapLibrary.UpdateInfo("map-a", "  ", "설명"), "빈 이름으로는 바꾸지 않습니다.");
        }

        [Test]
        public void 다른_프로그램이_파일을_잠깐_잡고_있어도_기다렸다_저장한다()
        {
            Save("map-a", "탑", 5, "2026-10-09T01:00:00Z");
            string path = MapLibrary.PathOf("map-a");

            // 파일을 잡았다가 0.04초 뒤에 놓는다. 방금 쓴 파일을 백신이 잠깐 훑는 경우와 같다.
            var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            var release = new System.Threading.Thread(() =>
            {
                System.Threading.Thread.Sleep(40);
                locked.Dispose();
            });
            release.Start();

            try
            {
                Assert.IsTrue(MapLibrary.UpdateInfo("map-a", "높은 탑", "설명"), "파일이 곧 풀렸는데 저장하지 못했습니다.");
            }
            finally
            {
                release.Join();
            }

            MapInfo info = MapLibrary.List()[0];
            Assert.AreEqual("높은 탑", info.Name);
            Assert.AreEqual("설명", info.Description);
            Assert.AreEqual(5, info.BlockCount);
        }

        [Test]
        public void 파일이_계속_잡혀_있으면_저장하지_못했다고_알리고_파일은_그대로다()
        {
            Save("map-a", "탑", 5, "2026-10-09T01:00:00Z");
            string path = MapLibrary.PathOf("map-a");

            using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.IsFalse(MapLibrary.UpdateInfo("map-a", "높은 탑", "설명"), "파일을 쓸 수 없는데 저장했다고 했습니다.");
            }

            MapInfo info = MapLibrary.List()[0];
            Assert.AreEqual("탑", info.Name, "저장하지 못했으면 파일의 내용이 그대로여야 합니다.");
            Assert.AreEqual(5, info.BlockCount);
        }

        [Test]
        public void 사본은_블록과_설명과_시작_위치와_대표_그림이_같고_이름에_사본이_붙는다()
        {
            MapDocument document = MapDocument.Create("탑", Min, Max);
            document.createdAt = "2026-10-08T03:00:00Z";
            document.updatedAt = "2026-10-09T01:00:00Z";
            document.description = "높이 올라가는 탑";
            document.spawn = new MapSpawn { custom = true, position = new Vector3(1f, 0f, 2f), yaw = 90f };
            document.blocks.Add(new MapBlock { id = 1, part = "block.gold", position = new Vector3(0.5f, 0.5f, 0.5f) });
            document.blocks.Add(new MapBlock { id = 2, part = "slab.blue", position = new Vector3(1.3f, 0.5f, 0.7f) });
            MapStorage.Save(document, MapLibrary.PathOf("map-a"));
            Assert.IsTrue(MapLibrary.SaveThumbnail("map-a", SceneSnapshot.PngSignature));

            string copy = MapLibrary.Duplicate("map-a");

            Assert.IsNotNull(copy);
            Assert.AreNotEqual("map-a", copy);
            Assert.IsTrue(MapLibrary.IsValidId(copy));
            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf(copy), out MapDocument copied, out _));
            Assert.AreEqual("탑 사본", copied.name);
            Assert.AreEqual("높이 올라가는 탑", copied.description);
            Assert.AreEqual(2, copied.blocks.Count);
            Assert.AreEqual("slab.blue", copied.blocks[1].part);
            Assert.Less(Vector3.Distance(new Vector3(1.3f, 0.5f, 0.7f), copied.blocks[1].position), 0.001f);
            Assert.IsTrue(copied.spawn.custom);
            Assert.AreEqual(90f, copied.spawn.yaw, 0.01f);
            Assert.AreNotEqual("2026-10-08T03:00:00Z", copied.createdAt, "사본을 만든 때는 지금입니다.");
            CollectionAssert.AreEqual(SceneSnapshot.PngSignature, MapLibrary.LoadThumbnail(copy), "대표 그림도 베껴야 합니다.");

            // 원래 맵은 그대로다.
            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf("map-a"), out MapDocument original, out _));
            Assert.AreEqual("탑", original.name);
            Assert.AreEqual("2026-10-09T01:00:00Z", original.updatedAt);
            Assert.AreEqual(2, original.blocks.Count);
            Assert.AreEqual(2, MapLibrary.List().Count);
            Assert.AreEqual(copy, MapLibrary.List()[0].Id, "방금 만든 사본이 목록의 맨 앞에 와야 합니다.");
        }

        [Test]
        public void 목록과_사본은_맵의_하늘과_해와_바닥_크기를_안다()
        {
            MapDocument document = MapDocument.Create("밤의 성", MapSize.MinOf(32), MapSize.MaxOf(32));
            document.environment = new MapEnvironment { sky = "night", sunYaw = 45f, sunPitch = 70f };
            document.updatedAt = "2026-10-09T01:00:00Z";
            MapStorage.Save(document, MapLibrary.PathOf("map-a"));
            Save("map-b", "보통 맵", 0, "2026-10-08T01:00:00Z");

            MapInfo night = MapLibrary.List().Find(map => map.Id == "map-a");
            Assert.AreEqual("night", night.Sky);
            Assert.AreEqual(45f, night.SunYaw, 0.001f);
            Assert.AreEqual(70f, night.SunPitch, 0.001f);
            Assert.AreEqual(32, night.FloorSize);

            MapInfo plain = MapLibrary.List().Find(map => map.Id == "map-b");
            Assert.AreEqual(MapSky.DefaultId, plain.Sky);
            Assert.AreEqual(MapSize.Default, plain.FloorSize);

            // 사본에도 분위기와 크기가 그대로 들어간다.
            string copy = MapLibrary.Duplicate("map-a");
            MapInfo copied = MapLibrary.List().Find(map => map.Id == copy);
            Assert.AreEqual("night", copied.Sky);
            Assert.AreEqual(45f, copied.SunYaw, 0.001f);
            Assert.AreEqual(32, copied.FloorSize);
        }

        [Test]
        public void 사본을_거듭_만들면_이름에_번호가_붙고_긴_이름은_사본이_잘리지_않게_줄인다()
        {
            Save("map-a", "탑", 1, "2026-10-09T01:00:00Z");

            string first = MapLibrary.Duplicate("map-a");
            string second = MapLibrary.Duplicate("map-a");

            Assert.AreNotEqual(first, second);
            Assert.AreEqual("탑 사본", NameOf(first));
            Assert.AreEqual("탑 사본 2", NameOf(second));

            Assert.AreEqual("새 맵 사본", MapLibrary.CopyName("   "));
            string copyName = MapLibrary.CopyName(new string('가', MapLibrary.MaxNameLength));
            Assert.AreEqual(MapLibrary.MaxNameLength, copyName.Length);
            StringAssert.EndsWith(MapLibrary.CopySuffix, copyName);
        }

        [Test]
        public void 사본은_맵이_상한에_닿았거나_원래_맵을_읽지_못하면_만들지_않는다()
        {
            Assert.IsNull(MapLibrary.Duplicate("missing"));
            Assert.IsNull(MapLibrary.Duplicate("../map-a"), "번호표로 쓸 수 없는 글자는 받지 않습니다.");

            Directory.CreateDirectory(directory);
            File.WriteAllText(MapLibrary.PathOf("broken"), "{ 깨진 파일");
            Assert.IsNull(MapLibrary.Duplicate("broken"));
            Assert.AreEqual(1, MapLibrary.Count());

            for (int i = 1; i < MapLibrary.MaxMaps; i++)
            {
                Save($"map-{i}", $"맵 {i}", 0, "2026-10-08T03:00:00Z");
            }

            Assert.AreEqual(MapLibrary.MaxMaps, MapLibrary.Count());
            Assert.IsNull(MapLibrary.Duplicate("map-1"), "맵이 가득 찼으면 사본을 만들지 않습니다.");
            Assert.AreEqual(MapLibrary.MaxMaps, MapLibrary.Count());
        }

        [Test]
        public void 맵의_수는_맵_파일만_세고_휴지통과_다른_파일은_세지_않는다()
        {
            Assert.AreEqual(0, MapLibrary.Count());

            Save("map-a", "탑", 1, "2026-10-09T01:00:00Z");
            Save("map-b", "성", 1, "2026-10-09T01:00:00Z");
            MapLibrary.SaveThumbnail("map-a", SceneSnapshot.PngSignature);
            File.WriteAllText(Path.Combine(directory, MapLibrary.CurrentFileName), "map-a");
            WriteTrash("map-c-20261008-010203", "지운 맵", 2);

            Assert.AreEqual(2, MapLibrary.Count());
            Assert.AreEqual(MapLibrary.List().Count, MapLibrary.Count());
        }

        [Test]
        public void 휴지통의_맵은_지우기_전의_번호표와_지운_때를_알고_최근에_지운_것이_앞에_온다()
        {
            WriteTrash("map-a-20261008-010203", "탑", 3);
            WriteTrash("map-20261001-120000-20261009-040506", "성", 1);
            WriteTrash("map-a-20261009-040506-2", "탑 둘", 0);
            File.WriteAllText(MapLibrary.TrashPathOf("memo"), "{}");
            File.WriteAllText(MapLibrary.TrashPathOf("broken-20261007-000000"), "{ 깨진 파일");

            List<MapInfo> trash = MapLibrary.ListTrash();

            Assert.AreEqual(4, trash.Count, "휴지통의 꼴(번호표-날짜-시각)이 아닌 파일은 넣지 않습니다.");

            // 번호표에 날짜가 들어 있어도 맨 끝의 날짜와 시각을 지운 때로 읽는다.
            Assert.AreEqual("map-20261001-120000", trash[0].Id);
            Assert.AreEqual("map-20261001-120000-20261009-040506", trash[0].TrashKey);
            Assert.AreEqual("성", trash[0].Name);
            Assert.IsTrue(trash[0].InTrash);
            Assert.AreEqual(new DateTime(2026, 10, 9, 4, 5, 6, DateTimeKind.Utc), trash[0].DeletedAt);

            // 같은 때에 둘을 지워 뒤에 번호가 붙은 것.
            Assert.AreEqual("map-a", trash[1].Id);
            Assert.AreEqual("map-a-20261009-040506-2", trash[1].TrashKey);
            Assert.AreEqual(trash[0].DeletedAt, trash[1].DeletedAt);

            Assert.AreEqual("map-a-20261008-010203", trash[2].TrashKey);
            Assert.AreEqual(3, trash[2].BlockCount);

            Assert.AreEqual("broken", trash[3].Id);
            Assert.IsFalse(trash[3].Readable, "읽을 수 없는 파일도 보이되 되살릴 수 없다고 알 수 있어야 합니다.");

            Assert.AreEqual(0, MapLibrary.List().Count, "휴지통의 맵은 맵 목록에 나오지 않습니다.");
        }

        [Test]
        public void 지운_맵을_되살리면_같은_번호표와_내용과_대표_그림으로_돌아온다()
        {
            Save("map-a", "탑", 5, "2026-10-09T01:00:00Z");
            Assert.IsTrue(MapLibrary.SaveThumbnail("map-a", SceneSnapshot.PngSignature));
            Assert.IsNotNull(MapLibrary.Trash("map-a"));
            Assert.AreEqual(0, MapLibrary.List().Count);

            List<MapInfo> trash = MapLibrary.ListTrash();
            Assert.AreEqual(1, trash.Count);
            Assert.AreEqual("탑", trash[0].Name);
            Assert.AreEqual(5, trash[0].BlockCount);
            Assert.IsTrue(trash[0].HasThumbnail);
            Assert.Less(Math.Abs((DateTime.UtcNow - trash[0].DeletedAt).TotalSeconds), 30.0, "지운 때는 방금이어야 합니다.");
            string key = trash[0].TrashKey;
            CollectionAssert.AreEqual(SceneSnapshot.PngSignature, MapLibrary.LoadTrashThumbnail(key));

            Assert.AreEqual("map-a", MapLibrary.Restore(key));

            List<MapInfo> maps = MapLibrary.List();
            Assert.AreEqual(1, maps.Count);
            Assert.AreEqual("map-a", maps[0].Id);
            Assert.AreEqual("탑", maps[0].Name);
            Assert.AreEqual(5, maps[0].BlockCount);
            Assert.IsTrue(maps[0].HasThumbnail, "대표 그림도 함께 돌아와야 합니다.");
            Assert.IsFalse(maps[0].InTrash);
            Assert.AreEqual(new DateTime(2026, 10, 9, 1, 0, 0, DateTimeKind.Utc), maps[0].UpdatedAt, "되살린다고 고친 때가 바뀌지 않습니다.");
            Assert.AreEqual(0, MapLibrary.ListTrash().Count);
            Assert.IsFalse(File.Exists(MapLibrary.TrashThumbnailPathOf(key)));
        }

        [Test]
        public void 되살릴_때_같은_번호표의_맵이_이미_있으면_새_번호표로_돌아온다()
        {
            Save("map-a", "탑", 5, "2026-10-09T01:00:00Z");
            Assert.IsNotNull(MapLibrary.Trash("map-a"));
            Save("map-a", "새 탑", 1, "2026-10-10T01:00:00Z");
            string key = MapLibrary.ListTrash()[0].TrashKey;

            string restored = MapLibrary.Restore(key);

            Assert.IsNotNull(restored);
            Assert.AreNotEqual("map-a", restored, "지금 있는 맵을 덮어쓰면 안 됩니다.");
            Assert.AreEqual(2, MapLibrary.Count());
            Assert.AreEqual("새 탑", NameOf("map-a"));
            Assert.AreEqual("탑", NameOf(restored));
            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf(restored), out MapDocument document, out _));
            Assert.AreEqual(5, document.blocks.Count);
        }

        [Test]
        public void 되살리기는_맵이_상한에_닿았거나_휴지통에_없는_것이면_하지_않는다()
        {
            Save("map-x", "지울 맵", 2, "2026-10-09T01:00:00Z");
            Assert.IsNotNull(MapLibrary.Trash("map-x"));
            string key = MapLibrary.ListTrash()[0].TrashKey;

            for (int i = 0; i < MapLibrary.MaxMaps; i++)
            {
                Save($"map-{i}", $"맵 {i}", 0, "2026-10-08T03:00:00Z");
            }

            Assert.IsNull(MapLibrary.Restore(key), "맵이 가득 찼으면 되살리지 않습니다.");
            Assert.AreEqual(1, MapLibrary.ListTrash().Count, "되살리지 못한 맵은 휴지통에 그대로 있어야 합니다.");
            Assert.IsNull(MapLibrary.Restore("map-none-20261008-000000"));
        }

        [Test]
        public void 휴지통을_가리키는_이름은_휴지통_폴더를_벗어나지_못한다()
        {
            Assert.IsTrue(MapLibrary.IsValidTrashKey("map-a-20261008-010203"));
            Assert.IsTrue(MapLibrary.IsValidTrashKey("map-a-20261008-010203-2"));
            Assert.IsFalse(MapLibrary.IsValidTrashKey("map-a"), "지운 때가 붙지 않은 이름은 휴지통의 것이 아닙니다.");
            Assert.IsFalse(MapLibrary.IsValidTrashKey("../map-a-20261008-010203"));
            Assert.IsFalse(MapLibrary.IsValidTrashKey("..\\map-a-20261008-010203"));
            Assert.IsFalse(MapLibrary.IsValidTrashKey("MAP-A-20261008-010203"));
            Assert.IsFalse(MapLibrary.IsValidTrashKey(string.Empty));
            Assert.IsFalse(MapLibrary.IsValidTrashKey(null));
            Assert.IsFalse(MapLibrary.IsValidTrashKey(new string('a', MapLibrary.MaxTrashKeyLength) + "-20261008-010203"));

            // 맵 목록에 있는 맵은 아주 지우기로 지울 수 없다. 휴지통을 거쳐야 한다.
            Save("map-a", "탑", 1, "2026-10-09T01:00:00Z");
            Assert.IsFalse(MapLibrary.Purge("map-a"));
            Assert.IsFalse(MapLibrary.Purge("../map-a"));
            Assert.IsNull(MapLibrary.Restore("../../x-20261008-010203"));
            Assert.IsNull(MapLibrary.LoadTrashThumbnail("../map-a"));
            Assert.IsTrue(MapLibrary.Exists("map-a"));
        }

        [Test]
        public void 아주_지우면_휴지통의_맵_파일과_대표_그림이_없어지고_다른_맵은_그대로다()
        {
            Save("map-a", "탑", 3, "2026-10-09T01:00:00Z");
            Save("map-b", "성", 2, "2026-10-09T02:00:00Z");
            Save("map-c", "남는 맵", 1, "2026-10-09T03:00:00Z");
            Assert.IsTrue(MapLibrary.SaveThumbnail("map-a", SceneSnapshot.PngSignature));
            Assert.IsNotNull(MapLibrary.Trash("map-a"));
            Assert.IsNotNull(MapLibrary.Trash("map-b"));

            string keyA = null;
            string keyB = null;
            foreach (MapInfo trashed in MapLibrary.ListTrash())
            {
                if (trashed.Id == "map-a") keyA = trashed.TrashKey;
                if (trashed.Id == "map-b") keyB = trashed.TrashKey;
            }

            Assert.IsTrue(MapLibrary.Purge(keyA));

            Assert.IsFalse(File.Exists(MapLibrary.TrashPathOf(keyA)));
            Assert.IsFalse(File.Exists(MapLibrary.TrashThumbnailPathOf(keyA)), "대표 그림도 함께 없어져야 합니다.");
            Assert.AreEqual(1, MapLibrary.ListTrash().Count);
            Assert.IsFalse(MapLibrary.Purge(keyA), "이미 없는 것을 또 지웠다고 하면 안 됩니다.");
            Assert.IsNull(MapLibrary.Restore(keyA), "아주 지운 맵은 되살릴 수 없습니다.");

            // 휴지통의 다른 맵과 맵 목록의 맵은 그대로다.
            Assert.IsTrue(MapLibrary.Exists("map-c"));
            Assert.AreEqual("map-b", MapLibrary.Restore(keyB));
            Assert.AreEqual(2, MapLibrary.Count());
        }

        private static string NameOf(string id)
        {
            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf(id), out MapDocument document, out _), $"맵 {id}을(를) 읽지 못했습니다.");
            return document.name;
        }

        [Test]
        public void 목록은_만든_때와_시작_위치를_정했는지와_대표_그림이_있는지를_안다()
        {
            MapDocument document = MapDocument.Create("탑", Min, Max);
            document.createdAt = "2026-10-08T03:00:00Z";
            document.updatedAt = "2026-10-10T09:30:00Z";
            document.spawn = new MapSpawn { custom = true, position = new Vector3(1f, 0f, 2f), yaw = 90f };
            MapStorage.Save(document, MapLibrary.PathOf("map-a"));
            Save("map-b", "성", 0, "2026-10-09T01:00:00Z");
            Assert.IsTrue(MapLibrary.SaveThumbnail("map-a", SceneSnapshot.PngSignature));

            List<MapInfo> maps = MapLibrary.List();
            MapInfo tower = maps.Find(map => map.Id == "map-a");
            MapInfo castle = maps.Find(map => map.Id == "map-b");

            Assert.AreEqual(new DateTime(2026, 10, 8, 3, 0, 0, DateTimeKind.Utc), tower.CreatedAt);
            Assert.IsTrue(tower.HasSpawn);
            Assert.IsTrue(tower.HasThumbnail);
            Assert.IsFalse(castle.HasSpawn);
            Assert.IsFalse(castle.HasThumbnail);
            Assert.AreEqual(2, maps.Count, "대표 그림 파일을 맵으로 세면 안 됩니다.");
        }

        [Test]
        public void 대표_그림은_맵_파일_옆에_쓰고_다시_읽으며_지운_맵의_그림은_휴지통으로_따라간다()
        {
            Save("map-a", "탑", 1, "2026-10-09T01:00:00Z");
            byte[] first = { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 };
            byte[] second = { 0x89, 0x50, 0x4E, 0x47, 9, 9 };

            Assert.IsNull(MapLibrary.LoadThumbnail("map-a"), "찍기 전에는 그림이 없습니다.");
            Assert.IsTrue(MapLibrary.SaveThumbnail("map-a", first));
            CollectionAssert.AreEqual(first, MapLibrary.LoadThumbnail("map-a"));
            Assert.IsTrue(MapLibrary.SaveThumbnail("map-a", second), "다시 찍으면 덮어씁니다.");
            CollectionAssert.AreEqual(second, MapLibrary.LoadThumbnail("map-a"));
            Assert.IsFalse(File.Exists(MapLibrary.ThumbnailPathOf("map-a") + ".tmp"), "임시 파일이 남으면 안 됩니다.");

            Assert.IsFalse(MapLibrary.SaveThumbnail("map-a", null));
            Assert.IsFalse(MapLibrary.SaveThumbnail("../map-a", first));
            Assert.IsNull(MapLibrary.LoadThumbnail("../map-a"));

            string moved = MapLibrary.Trash("map-a");
            Assert.IsNotNull(moved);
            Assert.IsFalse(File.Exists(MapLibrary.ThumbnailPathOf("map-a")), "지운 맵의 그림이 맵 폴더에 남았습니다.");
            string movedThumbnail = moved.Substring(0, moved.Length - MapLibrary.Extension.Length) + MapLibrary.ThumbnailExtension;
            Assert.IsTrue(File.Exists(movedThumbnail), "그림이 휴지통의 맵 파일 옆으로 가지 않았습니다.");
        }

        [Test]
        public void 옛_판의_맵은_정보를_고칠_때_사본을_남기고_지금_판으로_쓴다()
        {
            Directory.CreateDirectory(directory);
            string old = "{\"format\":\"atelier-verse-map\",\"version\":2,\"name\":\"옛 맵\",\"createdAt\":\"2026-10-09T01:00:00Z\",\"updatedAt\":\"2026-10-09T02:00:00Z\","
                + "\"bounds\":{\"min\":{\"x\":-8.0,\"y\":0.0,\"z\":-8.0},\"max\":{\"x\":8.0,\"y\":12.0,\"z\":8.0}},"
                + "\"blocks\":[{\"id\":1,\"part\":\"block.gold\",\"position\":{\"x\":0.5,\"y\":0.5,\"z\":0.5},\"rotation\":{\"x\":0.0,\"y\":0.0,\"z\":0.0}}],"
                + "\"assemblies\":[],\"rules\":[]}";
            File.WriteAllText(MapLibrary.PathOf("old"), old);

            Assert.IsTrue(MapLibrary.UpdateInfo("old", "고친 맵", "설명"));

            string backup = MapLibrary.PathOf("old") + ".v2.bak";
            Assert.IsTrue(File.Exists(backup), "옛 판의 파일을 남겨 두지 않았습니다.");
            Assert.AreEqual(old, File.ReadAllText(backup));
            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf("old"), out MapDocument document, out _));
            Assert.IsFalse(document.WasUpgraded);
            Assert.AreEqual("고친 맵", document.name);
            Assert.AreEqual(1, document.blocks.Count);
            Assert.AreEqual(1, MapLibrary.List().Count, "사본을 맵으로 세면 안 됩니다.");
        }

        [Test]
        public void 찍은_그림이_PNG인지_머리로_알아본다()
        {
            Assert.IsTrue(SceneSnapshot.IsPng(SceneSnapshot.PngSignature));
            Assert.IsTrue(SceneSnapshot.IsPng(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0 }));
            Assert.IsFalse(SceneSnapshot.IsPng(null));
            Assert.IsFalse(SceneSnapshot.IsPng(new byte[] { 0x89, 0x50 }));
            Assert.IsFalse(SceneSnapshot.IsPng(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0 }));
        }

        [Test]
        public void 맵_정보_창에_적는_글자를_만든다()
        {
            var map = new MapInfo
            {
                Id = "a", Name = "탑", BlockCount = 12, Readable = true,
                CreatedAt = new DateTime(2026, 10, 8, 3, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 10, 10, 9, 30, 0, DateTimeKind.Utc),
            };

            string facts = MapInfoView.Facts(map);
            StringAssert.StartsWith("블록 12개 · 만든 날 ", facts);
            StringAssert.Contains(map.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd"), facts);
            StringAssert.Contains("고친 때 " + map.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), facts);

            Assert.AreEqual(MapInfoView.DefaultSpawnText, MapInfoView.DescribeSpawn(false, new Vector3(3f, 0f, 2f), 90f));
            Assert.AreEqual("시작 위치 · 정한 자리 (3, 0, 2.5) · 방향 90°", MapInfoView.DescribeSpawn(true, new Vector3(3f, 0f, 2.5f), 90f));

            // 목록의 줄에는 설명이 있으면 뒤에 붙는다.
            map.Description = "친구들과 올라가 보는 탑";
            StringAssert.EndsWith(" · 친구들과 올라가 보는 탑", MapListView.Describe(map));
        }

        [Test]
        public void 지운_맵은_휴지통_폴더로_옮겨지고_목록에서_빠진다()
        {
            Save("local", "시험 작업실", 1, "2026-10-08T03:00:00Z");
            Save("map-a", "탑", 5, "2026-10-09T01:00:00Z");

            string moved = MapLibrary.Trash("map-a");

            Assert.IsNotNull(moved, "휴지통 폴더로 옮기지 못했습니다.");
            Assert.IsTrue(File.Exists(moved));
            StringAssert.StartsWith(MapLibrary.TrashDirectory, moved);
            Assert.IsFalse(File.Exists(MapLibrary.PathOf("map-a")));
            Assert.AreEqual(1, MapLibrary.List().Count);

            // 옮긴 파일은 그대로 읽힌다. 잘못 지웠을 때 되찾을 수 있다.
            Assert.IsTrue(MapStorage.TryLoad(moved, out MapDocument document, out _));
            Assert.AreEqual(5, document.blocks.Count);

            Assert.IsNull(MapLibrary.Trash("map-a"), "없는 맵은 지울 수 없습니다.");
            Assert.IsNull(MapLibrary.Trash("../local"));
        }

        [Test]
        public void 마지막으로_연_맵을_기억하고_없어졌으면_다른_맵을_연다()
        {
            Save("local", "시험 작업실", 1, "2026-10-08T03:00:00Z");
            Save("map-a", "탑", 5, "2026-10-09T01:00:00Z");

            // 처음부터 있던 맵뿐일 때는 적어 두지 않아도 그 맵이 열린다. 파일도 만들지 않는다.
            MapLibrary.RememberCurrent(MapLibrary.DefaultId);
            Assert.IsFalse(File.Exists(Path.Combine(directory, MapLibrary.CurrentFileName)));
            Assert.AreEqual(MapLibrary.DefaultId, MapLibrary.ResolveCurrent());

            MapLibrary.RememberCurrent("map-a");
            Assert.AreEqual("map-a", MapLibrary.ReadCurrent());
            Assert.AreEqual("map-a", MapLibrary.ResolveCurrent());

            MapLibrary.RememberCurrent(MapLibrary.DefaultId);
            Assert.AreEqual(MapLibrary.DefaultId, MapLibrary.ResolveCurrent(), "한 번 적기 시작했으면 처음부터 있던 맵으로 돌아간 것도 적습니다.");

            // 기억한 맵이 없어졌으면 처음부터 있던 맵을, 그것도 없으면 가장 최근의 맵을 연다.
            MapLibrary.RememberCurrent("map-a");
            MapLibrary.Trash("map-a");
            Assert.AreEqual(MapLibrary.DefaultId, MapLibrary.ResolveCurrent());

            Save("map-b", "성", 2, "2026-10-10T01:00:00Z");
            MapLibrary.Trash(MapLibrary.DefaultId);
            Assert.AreEqual("map-b", MapLibrary.ResolveCurrent());
        }

        [Test]
        public void 적어_둔_번호표가_바르지_않으면_없는_것으로_본다()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, MapLibrary.CurrentFileName), "../../outside");

            Assert.IsNull(MapLibrary.ReadCurrent());
            Assert.AreEqual(MapLibrary.DefaultId, MapLibrary.ResolveCurrent());
        }

        [Test]
        public void 목록의_한_줄에는_블록_수와_저장한_때를_적는다()
        {
            var map = new MapInfo { Id = "a", Name = "탑", BlockCount = 12, Readable = true, UpdatedAt = new DateTime(2026, 10, 10, 9, 30, 0, DateTimeKind.Utc) };

            string text = MapListView.Describe(map);

            StringAssert.StartsWith("블록 12개 · ", text);
            StringAssert.Contains(map.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), text);

            map.UpdatedAt = DateTime.MinValue;
            Assert.AreEqual("블록 12개", MapListView.Describe(map), "때를 모르면 블록 수만 적습니다.");
        }
    }
}
