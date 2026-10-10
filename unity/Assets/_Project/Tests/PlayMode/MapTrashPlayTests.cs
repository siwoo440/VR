using System.Collections;
using System.IO;
using AtelierVerse.Core;
using AtelierVerse.UI;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 맵의 사본 만들기와 휴지통을 실제로 실행해 확인한다(22일차): 맵 정보 창의 "사본 만들기", 맵 목록 창의 휴지통 보기,
    /// 되살리기, 아주 지우기. 창의 단추는 누름 동작을 직접 부른다. 테스트마다 임시 맵 폴더를 쓰므로 이 기기의 실제 맵은 건드리지 않는다.
    /// </summary>
    public class MapTrashPlayTests : PlayTestBase
    {
        private const int SceneBlockCount = 19;
        private const int BluePart = 1;
        private const string FirstMapName = "시험 작업실";

        private BlockWorld world;
        private MapAutoSave autoSave;
        private NoticeBar notice;

        [UnityTest]
        public IEnumerator 맵_정보의_사본_만들기는_같은_블록의_새_맵을_만들고_목록으로_돌아온다()
        {
            yield return LoadTrashScene();

            // 아직 저장되지 않은 변경이 있다. 사본에는 이것까지 들어가야 한다.
            Assert.AreEqual(PlaceResult.Ok, world.Add(BluePart, new Vector3(0.5f, 0.5f, -3f)));
            yield return OpenInfo(MapLibrary.DefaultId);

            Find<Button>(ui.MapInfo, "Duplicate").onClick.Invoke();
            yield return Frames(2);

            Assert.IsFalse(ui.IsMapInfoOpen, "사본을 만들면 맵 목록으로 돌아가야 합니다.");
            Assert.IsTrue(ui.IsMapListOpen);
            Assert.AreEqual(2, ui.MapList.MapCount);
            Assert.AreEqual(MapLibrary.DefaultId, autoSave.MapId, "사본을 만들어도 지금 맵에 그대로 있어야 합니다.");

            // 사본이 목록에 보인다. 원래 맵과 같은 초에 저장되었으면 맨 앞이 아닐 수 있으므로 번호표로 찾는다.
            string copy = null;
            foreach (MapInfo map in autoSave.ListMaps())
            {
                if (map.Id != MapLibrary.DefaultId) copy = map.Id;
            }

            Assert.IsNotNull(copy);
            int copyRow = ui.MapList.RowOf(copy);
            Assert.GreaterOrEqual(copyRow, 0, "사본이 보이는 쪽에 있어야 합니다.");
            MapListView.Row row = ui.MapList.GetRow(copyRow);
            Assert.AreEqual($"{FirstMapName} 사본", row.nameLabel.text);
            Assert.AreEqual($"{FirstMapName} 사본 · {MapAutoSave.CopiedMessage}", notice.Message);

            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf(copy), out MapDocument document, out _));
            Assert.AreEqual(SceneBlockCount + 1, document.blocks.Count, "저장하지 않았던 변경이 사본에 들어가지 않았습니다.");

            // 사본을 열면 같은 블록이 보이고, 사본을 고쳐도 원래 맵은 그대로다.
            row.openButton.onClick.Invoke();
            yield return Frames(3);
            Assert.AreEqual(copy, autoSave.MapId);
            Assert.AreEqual(SceneBlockCount + 1, world.Count);

            Assert.AreEqual(PlaceResult.Ok, world.Add(BluePart, new Vector3(2.5f, 0.5f, -3f)));
            Assert.IsTrue(autoSave.Open(MapLibrary.DefaultId));
            yield return Frames(3);
            Assert.AreEqual(SceneBlockCount + 1, world.Count, "사본을 고쳤는데 원래 맵이 바뀌었습니다.");
        }

        [UnityTest]
        public IEnumerator 휴지통에서_지운_맵을_되살린다()
        {
            yield return LoadTrashScene();
            string other = MapLibrary.Create("지울 맵", world.BoundsMin, world.BoundsMax);

            ui.OpenMapList();
            yield return Frames(2);
            Assert.AreEqual(MapListView.TrashTitle, ui.MapList.TrashButtonText, "휴지통이 비어 있으면 수를 붙이지 않습니다.");

            MapListView.Row doomed = ui.MapList.GetRow(ui.MapList.RowOf(other));
            doomed.deleteButton.onClick.Invoke();
            doomed.deleteButton.onClick.Invoke();
            yield return Frames(2);
            Assert.IsFalse(MapLibrary.Exists(other));
            Assert.AreEqual($"{MapListView.TrashTitle} 1", ui.MapList.TrashButtonText, "휴지통에 든 수가 단추에 보여야 합니다.");

            // 휴지통을 본다.
            Find<Button>(ui.MapList, "Trash").onClick.Invoke();
            yield return Frames(2);
            Assert.IsTrue(ui.MapList.ShowingTrash);
            Assert.AreEqual(MapListView.TrashTitle, ui.MapList.TitleText);
            Assert.AreEqual(MapListView.BackToListText, ui.MapList.TrashButtonText);
            Assert.AreEqual(1, ui.MapList.MapCount);
            Assert.IsFalse(Find<Button>(ui.MapList, "CreateMap").gameObject.activeSelf, "휴지통에서는 새 맵을 만들지 않습니다.");

            MapListView.Row row = ui.MapList.GetRow(0);
            Assert.AreEqual("지울 맵", row.nameLabel.text);
            StringAssert.Contains("지운 때", row.infoLabel.text);
            Assert.IsTrue(row.restoreButton.gameObject.activeSelf);
            Assert.IsTrue(row.purgeButton.gameObject.activeSelf);
            Assert.IsFalse(row.openButton.gameObject.activeSelf, "휴지통의 맵은 열 수 없습니다.");
            Assert.IsFalse(row.infoButton.gameObject.activeSelf);
            Assert.IsFalse(row.deleteButton.gameObject.activeSelf);
            Assert.IsFalse(row.currentBadge.activeSelf);

            row.restoreButton.onClick.Invoke();
            yield return Frames(2);

            Assert.IsTrue(MapLibrary.Exists(other), "되살렸는데 맵 목록에 없습니다.");
            Assert.AreEqual($"지울 맵 · {MapAutoSave.RestoredMessage}", notice.Message);
            Assert.IsTrue(ui.MapList.ShowingTrash, "되살린 뒤에도 휴지통을 계속 보입니다.");
            Assert.AreEqual(0, ui.MapList.MapCount);
            Assert.IsTrue(ui.MapList.IsEmptyShown, "휴지통이 비었다고 보여야 합니다.");
            Assert.AreEqual(MapLibrary.DefaultId, autoSave.MapId, "되살린 맵을 열지는 않습니다.");

            // 맵 목록으로 돌아가면 되살린 맵이 있다.
            Find<Button>(ui.MapList, "Trash").onClick.Invoke();
            yield return Frames(2);
            Assert.IsFalse(ui.MapList.ShowingTrash);
            Assert.AreEqual(MapListView.ListTitle, ui.MapList.TitleText);
            Assert.AreEqual(2, ui.MapList.MapCount);
            Assert.GreaterOrEqual(ui.MapList.RowOf(other), 0);
            Assert.AreEqual(MapListView.TrashTitle, ui.MapList.TrashButtonText);
            Assert.IsFalse(ui.MapList.IsEmptyShown);
            Assert.IsTrue(Find<Button>(ui.MapList, "CreateMap").gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator 지금_맵을_지워도_휴지통에서_되살려_다시_열_수_있다()
        {
            yield return LoadTrashScene();
            Assert.AreEqual(SceneBlockCount, world.Count);

            // 하나뿐인 맵을 지운다. 빈 맵이 새로 열린다.
            Assert.IsTrue(autoSave.Delete(MapLibrary.DefaultId));
            yield return Frames(3);
            Assert.AreNotEqual(MapLibrary.DefaultId, autoSave.MapId);
            Assert.AreEqual(0, world.Count);

            ui.OpenMapList();
            ui.ShowMapTrash();
            yield return Frames(2);
            Assert.AreEqual(1, ui.MapList.MapCount);
            MapListView.Row row = ui.MapList.GetRow(0);
            Assert.AreEqual(FirstMapName, row.nameLabel.text);
            StringAssert.StartsWith($"블록 {SceneBlockCount}개", row.infoLabel.text);

            row.restoreButton.onClick.Invoke();
            yield return Frames(2);
            Assert.IsTrue(MapLibrary.Exists(MapLibrary.DefaultId), "지우기 전의 번호표로 돌아와야 합니다.");

            Assert.IsTrue(autoSave.Open(MapLibrary.DefaultId));
            yield return Frames(3);
            Assert.AreEqual(SceneBlockCount, world.Count, "되살린 맵의 블록이 그대로여야 합니다.");
        }

        [UnityTest]
        public IEnumerator 아주_지우기는_두_번_눌러야_하고_파일이_없어진다()
        {
            yield return LoadTrashScene();
            string other = MapLibrary.Create("지울 맵", world.BoundsMin, world.BoundsMax);
            Assert.IsTrue(autoSave.Delete(other));

            ui.OpenMapList();
            ui.ShowMapTrash();
            yield return Frames(2);
            MapListView.Row row = ui.MapList.GetRow(0);
            Assert.AreEqual("아주 지우기", row.purgeLabel.text);

            row.purgeButton.onClick.Invoke();
            Assert.AreEqual("한 번 더", row.purgeLabel.text, "한 번 누르면 확인을 기다려야 합니다.");
            Assert.AreEqual(1, Directory.GetFiles(MapLibrary.TrashDirectory, "*" + MapLibrary.Extension).Length, "한 번 눌렀는데 지워졌습니다.");

            row.purgeButton.onClick.Invoke();
            yield return Frames(2);

            Assert.AreEqual(0, Directory.GetFiles(MapLibrary.TrashDirectory, "*" + MapLibrary.Extension).Length, "두 번 눌렀는데 파일이 남아 있습니다.");
            Assert.AreEqual(0, ui.MapList.MapCount);
            Assert.IsTrue(ui.MapList.IsEmptyShown);
            Assert.AreEqual($"지울 맵 · {MapAutoSave.PurgedMessage}", notice.Message);
            Assert.IsFalse(MapLibrary.Exists(other));
            Assert.AreEqual(1, MapLibrary.Count(), "맵 목록의 맵은 그대로여야 합니다.");
        }

        [UnityTest]
        public IEnumerator 아주_지우기를_한_번만_누르고_맵_목록에_다녀오면_확인이_풀린다()
        {
            yield return LoadTrashScene();
            string other = MapLibrary.Create("지울 맵", world.BoundsMin, world.BoundsMax);
            Assert.IsTrue(autoSave.Delete(other));

            ui.OpenMapList();
            ui.ShowMapTrash();
            yield return Frames(2);
            ui.MapList.GetRow(0).purgeButton.onClick.Invoke();
            Assert.AreEqual("한 번 더", ui.MapList.GetRow(0).purgeLabel.text);

            ui.ShowMapList();
            yield return Frames(2);
            ui.ShowMapTrash();
            yield return Frames(2);

            Assert.AreEqual("아주 지우기", ui.MapList.GetRow(0).purgeLabel.text, "다른 곳에 다녀왔는데 확인을 기다리고 있습니다.");
            ui.MapList.GetRow(0).purgeButton.onClick.Invoke();
            yield return Frames(2);
            Assert.AreEqual(1, ui.MapList.MapCount, "한 번 누른 것으로 지워지면 안 됩니다.");
        }

        [UnityTest]
        public IEnumerator 휴지통에서_Esc는_맵_목록으로_돌아가고_M은_창을_닫는다()
        {
            yield return LoadTrashScene();
            ui.OpenMapList();
            ui.ShowMapTrash();
            yield return Frames(2);
            Assert.IsTrue(ui.MapList.ShowingTrash);

            yield return Tap(keyboard.escapeKey);
            Assert.IsTrue(ui.IsMapListOpen, "휴지통에서 Esc를 눌렀는데 창이 닫혔습니다.");
            Assert.IsFalse(ui.MapList.ShowingTrash);

            ui.ShowMapTrash();
            yield return Frames(2);
            yield return Tap(keyboard.mKey);
            Assert.IsFalse(ui.IsMapListOpen);
            Assert.IsFalse(ui.IsModalOpen);

            // 다시 열면 휴지통이 아니라 맵 목록부터 보인다.
            ui.OpenMapList();
            yield return Frames(2);
            Assert.IsFalse(ui.MapList.ShowingTrash);
            Assert.AreEqual(1, ui.MapList.MapCount);
        }

        [UnityTest]
        public IEnumerator 맵이_가득_차면_사본과_되살리기를_하지_않고_알린다()
        {
            yield return LoadTrashScene();
            string trashed = MapLibrary.Create("지울 맵", world.BoundsMin, world.BoundsMax);
            Assert.IsTrue(autoSave.Delete(trashed));
            while (MapLibrary.Count() < MapLibrary.MaxMaps)
            {
                Assert.IsNotNull(MapLibrary.Create("채우는 맵", world.BoundsMin, world.BoundsMax));
            }

            ui.OpenMapList();
            ui.OpenMapInfo(MapLibrary.DefaultId);
            yield return Frames(2);
            Assert.IsTrue(ui.IsMapInfoOpen);

            Find<Button>(ui.MapInfo, "Duplicate").onClick.Invoke();
            yield return Frames(2);
            Assert.AreEqual(MapLibrary.MaxMaps, MapLibrary.Count(), "가득 찼는데 사본이 만들어졌습니다.");
            StringAssert.Contains(MapAutoSave.TooManyMessage, notice.Message);
            Assert.IsTrue(ui.IsMapInfoOpen, "사본을 만들지 못했으면 맵 정보 창에 그대로 있어야 합니다.");

            ui.BackToMapList();
            ui.ShowMapTrash();
            yield return Frames(2);
            ui.MapList.GetRow(0).restoreButton.onClick.Invoke();
            yield return Frames(2);

            Assert.AreEqual(MapLibrary.MaxMaps, MapLibrary.Count(), "가득 찼는데 되살아났습니다.");
            StringAssert.Contains("되살릴 수 없습니다", notice.Message);
            Assert.AreEqual(1, ui.MapList.MapCount, "되살리지 못한 맵은 휴지통에 그대로 있어야 합니다.");
        }

        [UnityTest]
        public IEnumerator 읽을_수_없는_휴지통의_파일은_되살릴_수_없고_아주_지울_수는_있다()
        {
            yield return LoadTrashScene();
            Directory.CreateDirectory(MapLibrary.TrashDirectory);
            File.WriteAllText(MapLibrary.TrashPathOf("broken-20261008-010203"), "{ 깨진 파일");

            ui.OpenMapList();
            ui.ShowMapTrash();
            yield return Frames(2);
            Assert.AreEqual(1, ui.MapList.MapCount);
            MapListView.Row row = ui.MapList.GetRow(0);
            StringAssert.StartsWith("읽을 수 없는 파일", row.infoLabel.text);
            Assert.IsFalse(row.restoreButton.interactable, "읽을 수 없는 파일은 되살릴 수 없어야 합니다.");

            row.purgeButton.onClick.Invoke();
            row.purgeButton.onClick.Invoke();
            yield return Frames(2);
            Assert.IsFalse(File.Exists(MapLibrary.TrashPathOf("broken-20261008-010203")));
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadTrashScene();
            BeginCapture();

            // 지운 맵 둘과 사본 하나.
            string tower = autoSave.CreateNew("언덕 위의 탑");
            for (int y = 0; y < 4; y++)
            {
                world.Add(BluePart, CellCenter(0, y, -2));
            }

            autoSave.SaveNow();
            string draft = autoSave.CreateNew("연습한 맵");
            world.Add(world.Catalog.IndexOf("stairs.wood"), new Vector3(0.5f, 0.5f, -2.5f));
            autoSave.SaveNow();
            Assert.IsTrue(autoSave.Open(MapLibrary.DefaultId));
            Assert.IsNotNull(autoSave.Duplicate(MapLibrary.DefaultId));
            Assert.IsTrue(autoSave.Delete(tower));
            Assert.IsTrue(autoSave.Delete(draft));
            yield return Frames(3);

            player.CaptureLook(true);
            player.SetLook(0f, 12f);
            yield return Frames(3);
            yield return Tap(keyboard.mKey);
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "maps-list-trash-button.png"));

            ui.ShowMapTrash();
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "maps-trash.png"));

            ui.ShowMapList();
            ui.OpenMapInfo(MapLibrary.DefaultId);
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "map-info-duplicate.png"));
        }

        /// <summary>맵 목록 창을 열고 그 맵의 줄에 있는 정보를 눌러 맵 정보 창으로 간다.</summary>
        private IEnumerator OpenInfo(string id)
        {
            ui.OpenMapList();
            yield return Frames(2);
            int row = ui.MapList.RowOf(id);
            Assert.GreaterOrEqual(row, 0, $"맵 목록에 {id}이(가) 없습니다.");
            ui.MapList.GetRow(row).infoButton.onClick.Invoke();
            yield return Frames(2);
            Assert.IsTrue(ui.IsMapInfoOpen, "맵 정보 창이 열리지 않았습니다.");
        }

        private IEnumerator LoadTrashScene()
        {
            yield return LoadSandbox();

            world = Object.FindAnyObjectByType<BlockWorld>();
            autoSave = Object.FindAnyObjectByType<MapAutoSave>();
            notice = Object.FindAnyObjectByType<NoticeBar>();
            Assert.IsNotNull(world, "Sandbox 씬에서 블록 세계를 찾을 수 없습니다.");
            Assert.IsNotNull(autoSave, "Sandbox 씬에서 자동 저장을 찾을 수 없습니다.");
            Assert.IsNotNull(notice, "게임 화면에 알림 띠가 없습니다.");
            Assert.IsNotNull(ui.MapList, "게임 화면에 맵 목록 창이 없습니다.");
            Assert.IsNotNull(ui.MapInfo, "게임 화면에 맵 정보 창이 없습니다.");
            yield return Frames(2);
        }
    }
}
