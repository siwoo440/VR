using System.Collections;
using System.IO;
using AtelierVerse.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 맵 저장과 불러오기를 실제로 실행해 확인한다. 맵 파일은 PlayTestBase가 정한 임시 폴더에 쓴다.
    /// </summary>
    public class SavePlayTests : PlayTestBase
    {
        private const int SceneBlockCount = 19;
        private const float SaveWait = 1.6f;

        private BlockWorld world;
        private MapAutoSave autoSave;

        [UnityTest]
        public IEnumerator 파일이_없으면_씬의_블록을_첫_맵으로_저장한다()
        {
            yield return LoadSaveScene();

            Assert.AreEqual(SaveState.Saved, autoSave.State, autoSave.Message);
            Assert.IsTrue(File.Exists(autoSave.FilePath), "맵 파일이 만들어지지 않았습니다.");
            Assert.IsTrue(MapStorage.TryLoad(autoSave.FilePath, out MapDocument document, out MapFileError error), error.ToString());
            Assert.AreEqual(SceneBlockCount, document.blocks.Count);
            Assert.AreEqual("시험 작업실", document.name);
            Assert.AreEqual(MapDocument.CurrentVersion, document.version);
            Assert.AreEqual(world.BoundsMin, document.bounds.min);
            Assert.AreEqual(world.BoundsMax, document.bounds.max);
            Assert.AreEqual(new Vector3(-8f, 0f, -8f), world.BoundsMin);
            Assert.AreEqual(new Vector3(8f, 12f, 8f), world.BoundsMax, "범위는 가장 큰 칸의 위 모서리까지의 상자입니다.");
        }

        [UnityTest]
        public IEnumerator 칸에_맞지_않는_자리의_블록도_저장되고_다시_열면_같은_자리에_있다()
        {
            yield return LoadSaveScene();
            var spot = new Vector3(3.37f, 0.5f, -2.81f);
            Quaternion turned = Quaternion.Euler(0f, 30f, 0f);

            Assert.AreEqual(PlaceResult.Ok, world.Add(2, spot, turned, out int id));
            yield return new WaitForSeconds(SaveWait);

            yield return LoadSaveScene();
            Assert.IsTrue(world.TryGet(id, out BlockRecord record), "다시 열면 같은 번호의 블록이 있어야 합니다.");
            Assert.Less(Vector3.Distance(spot, record.Position), 0.0015f, "저장한 자리와 다릅니다.");
            Assert.Less(Quaternion.Angle(turned, record.Rotation), 0.1f, "저장한 방향과 다릅니다.");

            PlacedBlock view = FindViewNear(world, spot, 0.002f);
            Assert.IsNotNull(view, "화면의 블록이 저장한 자리에 없습니다.");
            Assert.Less(Quaternion.Angle(turned, view.transform.rotation), 0.1f);
        }

        [UnityTest]
        public IEnumerator 블록을_칸으로_적은_옛_판의_파일도_읽고_사본을_남긴_뒤_새_판으로_저장한다()
        {
            PrepareMapDirectory();
            Directory.CreateDirectory(MapStorage.Directory);
            string old = "{\"format\":\"atelier-verse-map\",\"version\":1,\"name\":\"옛 맵\",\"createdAt\":\"2026-10-08T03:51:00Z\",\"updatedAt\":\"2026-10-08T04:12:30Z\","
                + "\"bounds\":{\"min\":{\"x\":-8,\"y\":0,\"z\":-8},\"max\":{\"x\":7,\"y\":11,\"z\":7}},"
                + "\"blocks\":[{\"part\":\"block.gold\",\"cell\":{\"x\":2,\"y\":0,\"z\":-3},\"facing\":0},{\"part\":\"block.wood\",\"cell\":{\"x\":7,\"y\":11,\"z\":-8},\"facing\":0}],"
                + "\"assemblies\":[],\"rules\":[]}";
            File.WriteAllText(MapStorage.LocalPath, old);

            yield return LoadSaveScene();

            Assert.AreEqual(2, world.Count, "옛 판의 블록을 모두 읽어야 합니다.");
            Assert.AreEqual(0, autoSave.LastLoad.Skipped);
            Assert.IsTrue(TryGetPartNear(world, CellCenter(2, 0, -3), out int gold), "칸의 가운데 자리에 블록이 없습니다.");
            Assert.AreEqual("block.gold", world.Catalog.Get(gold).id);
            Assert.IsTrue(HasBlockNear(world, CellCenter(7, 11, -8)), "범위의 가장자리 칸에 있던 블록이 빠졌습니다.");

            Assert.IsNotNull(autoSave.BackupPath, "옛 판의 파일을 남겨 두지 않았습니다.");
            Assert.AreEqual(old, File.ReadAllText(autoSave.BackupPath));

            yield return new WaitForSeconds(SaveWait);
            Assert.AreEqual(SaveState.Saved, autoSave.State, autoSave.Message);
            string saved = File.ReadAllText(autoSave.FilePath);
            StringAssert.Contains($"\"version\": {MapDocument.CurrentVersion}", saved);
            StringAssert.DoesNotContain("\"cell\"", saved);
            Assert.IsTrue(MapStorage.TryLoad(autoSave.FilePath, out MapDocument document, out MapFileError error), error.ToString());
            Assert.IsFalse(document.WasUpgraded);
            Assert.AreEqual("옛 맵", document.name);
            Assert.AreEqual("2026-10-08T03:51:00Z", document.createdAt, "만든 시각은 옛 파일의 것을 이어받아야 합니다.");
        }

        [UnityTest]
        public IEnumerator 블록을_놓으면_잠시_뒤_저장되고_다시_열면_남아_있다()
        {
            yield return LoadSaveScene();
            Vector3 cell = CellCenter(3, 0, -3);

            Assert.AreEqual(PlaceResult.Ok, world.Add(2, cell));
            Assert.AreEqual(SaveState.Pending, autoSave.State);
            Assert.IsTrue(autoSave.HasPendingChanges);

            yield return new WaitForSeconds(SaveWait);
            Assert.AreEqual(SaveState.Saved, autoSave.State, autoSave.Message);
            Assert.IsFalse(autoSave.HasPendingChanges);

            yield return LoadSaveScene();
            Assert.AreEqual(SceneBlockCount + 1, world.Count);
            Assert.IsTrue(TryGetPartNear(world, cell, out int part));
            Assert.AreEqual("block.clay", world.Catalog.Get(part).id);
            Assert.AreEqual("불러왔습니다", autoSave.Message);
        }

        [UnityTest]
        public IEnumerator 씬을_떠날_때_남은_변경을_저장한다()
        {
            yield return LoadSaveScene();
            Vector3 cell = CellCenter(-3, 0, -3);
            world.Add(0, cell);
            Assert.IsTrue(autoSave.HasPendingChanges);

            // 자동 저장의 1초를 기다리지 않고 바로 씬을 다시 연다.
            yield return LoadSaveScene();

            Assert.IsTrue(HasBlockNear(world, cell), "씬을 떠날 때 저장되지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 저장_파일이_있으면_씬의_블록_대신_파일의_블록이_보인다()
        {
            PrepareMapDirectory();
            MapDocument document = MapDocument.Create("내 맵", new Vector3(-8f, 0f, -8f), new Vector3(8f, 12f, 8f));
            document.blocks.Add(new MapBlock { part = "block.gold", position = CellCenter(0, 0, 0) });
            document.blocks.Add(new MapBlock { part = "block.wood", position = CellCenter(0, 1, 0) });
            MapStorage.Save(document, MapStorage.LocalPath);

            yield return LoadSaveScene();

            Assert.AreEqual(2, world.Count);
            Assert.IsFalse(HasBlockNear(world, CellCenter(2, 0, 2)), "씬에 미리 놓인 집의 블록이 남아 있습니다.");
            yield return null;
            Assert.AreEqual(2, world.GetComponentsInChildren<PlacedBlock>().Length, "화면의 블록 수가 기록과 다릅니다.");

            PlacedBlock top = FindViewNear(world, CellCenter(0, 1, 0), 0.002f);
            Assert.IsNotNull(top, "화면의 블록이 파일에 적힌 자리에 없습니다.");
            Assert.AreEqual(world.Catalog.Get(world.Catalog.IndexOf("block.wood")).material, top.GetComponent<Renderer>().sharedMaterial);
        }

        [UnityTest]
        public IEnumerator 읽지_못한_블록은_건너뛰고_수를_알린다()
        {
            PrepareMapDirectory();
            MapDocument document = MapDocument.Create("내 맵", new Vector3(-8f, 0f, -8f), new Vector3(8f, 12f, 8f));
            document.blocks.Add(new MapBlock { part = "block.gold", position = CellCenter(0, 0, 0) });
            document.blocks.Add(new MapBlock { part = "block.future", position = CellCenter(1, 0, 0) });
            document.blocks.Add(new MapBlock { part = "block.blue", position = CellCenter(50, 0, 0) });
            MapStorage.Save(document, MapStorage.LocalPath);

            yield return LoadSaveScene();

            Assert.AreEqual(1, world.Count);
            Assert.AreEqual(2, autoSave.LastLoad.Skipped);
            Assert.AreEqual("블록 2개를 읽지 못했습니다", autoSave.Message);
        }

        [UnityTest]
        public IEnumerator 읽지_못하는_파일은_옆으로_옮기고_씬의_블록으로_시작한다()
        {
            PrepareMapDirectory();
            Directory.CreateDirectory(MapStorage.Directory);
            File.WriteAllText(MapStorage.LocalPath, "{ \"format\": \"atelier-verse-map\", ");

            yield return LoadSaveScene();

            Assert.AreEqual(SceneBlockCount, world.Count);
            Assert.AreEqual(SaveState.Failed, autoSave.State);
            Assert.IsNotNull(autoSave.SetAsidePath, "읽지 못한 파일을 옮겨 두지 않았습니다.");
            Assert.IsTrue(File.Exists(autoSave.SetAsidePath));

            yield return new WaitForSeconds(SaveWait);
            Assert.AreEqual(SaveState.Saved, autoSave.State, "옮겨 둔 뒤 씬의 블록을 새 파일로 저장해야 합니다.");
            Assert.IsTrue(File.Exists(autoSave.FilePath));
        }

        [UnityTest]
        public IEnumerator 화면의_저장_표시는_상태를_따라_바뀐다()
        {
            yield return LoadSaveScene();
            TMP_Text label = Find<Transform>(ui, "SaveChip").GetComponentInChildren<TMP_Text>();
            Assert.AreEqual("저장했습니다", label.text);

            world.Add(1, CellCenter(4, 0, -4));
            yield return null;
            Assert.AreEqual("저장 대기 중", label.text);

            yield return new WaitForSeconds(SaveWait);
            Assert.AreEqual("저장했습니다", label.text);
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadSaveScene();
            BeginCapture();

            world.Add(0, CellCenter(-1, 0, -3));
            world.Add(1, CellCenter(0, 0, -3));
            world.Add(2, CellCenter(1, 0, -3));
            yield return new WaitForSeconds(SaveWait);

            player.CaptureLook(true);
            player.SetLook(0f, 12f);
            yield return Frames(2);
            SaveCapture(Path.Combine(directory, "save-first-person.png"));
        }

        private IEnumerator LoadSaveScene()
        {
            yield return LoadSandbox();

            world = Object.FindAnyObjectByType<BlockWorld>();
            autoSave = Object.FindAnyObjectByType<MapAutoSave>();
            Assert.IsNotNull(world, "Sandbox 씬에서 블록 세계를 찾을 수 없습니다.");
            Assert.IsNotNull(autoSave, "블록 세계에 자동 저장이 없습니다.");
            yield return Frames(2);
        }
    }
}
