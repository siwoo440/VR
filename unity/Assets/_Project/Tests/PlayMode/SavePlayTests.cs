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
            Assert.AreEqual(world.MinCell, document.bounds.min);
            Assert.AreEqual(world.MaxCell, document.bounds.max);
        }

        [UnityTest]
        public IEnumerator 블록을_놓으면_잠시_뒤_저장되고_다시_열면_남아_있다()
        {
            yield return LoadSaveScene();
            var cell = new Vector3Int(3, 0, -3);

            Assert.AreEqual(PlaceResult.Ok, world.Place(cell, 2));
            Assert.AreEqual(SaveState.Pending, autoSave.State);
            Assert.IsTrue(autoSave.HasPendingChanges);

            yield return new WaitForSeconds(SaveWait);
            Assert.AreEqual(SaveState.Saved, autoSave.State, autoSave.Message);
            Assert.IsFalse(autoSave.HasPendingChanges);

            yield return LoadSaveScene();
            Assert.AreEqual(SceneBlockCount + 1, world.Count);
            Assert.IsTrue(world.TryGetPart(cell, out int part));
            Assert.AreEqual("block.clay", world.Catalog.Get(part).id);
            Assert.AreEqual("불러왔습니다", autoSave.Message);
        }

        [UnityTest]
        public IEnumerator 씬을_떠날_때_남은_변경을_저장한다()
        {
            yield return LoadSaveScene();
            var cell = new Vector3Int(-3, 0, -3);
            world.Place(cell, 0);
            Assert.IsTrue(autoSave.HasPendingChanges);

            // 자동 저장의 1초를 기다리지 않고 바로 씬을 다시 연다.
            yield return LoadSaveScene();

            Assert.IsTrue(world.Has(cell), "씬을 떠날 때 저장되지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 저장_파일이_있으면_씬의_블록_대신_파일의_블록이_보인다()
        {
            PrepareMapDirectory();
            MapDocument document = MapDocument.Create("내 맵", new Vector3Int(-8, 0, -8), new Vector3Int(7, 11, 7));
            document.blocks.Add(new MapBlock { part = "block.gold", cell = new Vector3Int(0, 0, 0) });
            document.blocks.Add(new MapBlock { part = "block.wood", cell = new Vector3Int(0, 1, 0) });
            MapStorage.Save(document, MapStorage.LocalPath);

            yield return LoadSaveScene();

            Assert.AreEqual(2, world.Count);
            Assert.IsFalse(world.Has(new Vector3Int(2, 0, 2)), "씬에 미리 놓인 집의 블록이 남아 있습니다.");
            yield return null;
            Assert.AreEqual(2, world.GetComponentsInChildren<PlacedBlock>().Length, "화면의 블록 수가 기록과 다릅니다.");

            PlacedBlock top = null;
            foreach (PlacedBlock block in world.GetComponentsInChildren<PlacedBlock>())
            {
                if (block.Cell == new Vector3Int(0, 1, 0)) top = block;
            }

            Assert.IsNotNull(top);
            Assert.AreEqual(world.Catalog.Get(world.Catalog.IndexOf("block.wood")).material, top.GetComponent<Renderer>().sharedMaterial);
        }

        [UnityTest]
        public IEnumerator 읽지_못한_블록은_건너뛰고_수를_알린다()
        {
            PrepareMapDirectory();
            MapDocument document = MapDocument.Create("내 맵", new Vector3Int(-8, 0, -8), new Vector3Int(7, 11, 7));
            document.blocks.Add(new MapBlock { part = "block.gold", cell = new Vector3Int(0, 0, 0) });
            document.blocks.Add(new MapBlock { part = "block.future", cell = new Vector3Int(1, 0, 0) });
            document.blocks.Add(new MapBlock { part = "block.blue", cell = new Vector3Int(50, 0, 0) });
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

            world.Place(new Vector3Int(4, 0, -4), 1);
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

            world.Place(new Vector3Int(-1, 0, -3), 0);
            world.Place(new Vector3Int(0, 0, -3), 1);
            world.Place(new Vector3Int(1, 0, -3), 2);
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
