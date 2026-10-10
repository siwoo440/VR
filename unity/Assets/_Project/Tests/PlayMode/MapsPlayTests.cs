using System.Collections;
using System.Collections.Generic;
using System.IO;
using AtelierVerse.Player;
using AtelierVerse.UI;
using AtelierVerse.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 여러 맵 다루기를 실제로 실행해 확인한다(17일차): 맵 목록 창, 새 맵, 맵을 오가기, 이름 바꾸기, 지우기, 다시 켰을 때.
    /// 창의 단추는 누름 동작을 직접 부르고, 이름은 글자 칸에 글자를 직접 넣는다.
    /// 실제 글자판으로 한글 이름을 치는 것은 이 테스트로 확인되지 않는다.
    /// </summary>
    public class MapsPlayTests : PlayTestBase
    {
        private const int SceneBlockCount = 19;
        private const string FirstMapName = "시험 작업실";

        private static readonly Vector3 SpawnPoint = new Vector3(0.5f, 0f, -5.5f);

        private BlockWorld world;
        private MapAutoSave autoSave;
        private NoticeBar notice;

        [UnityTest]
        public IEnumerator M으로_맵_목록_창을_열고_닫으며_열려_있는_동안_조작이_막힌다()
        {
            yield return LoadMapsScene();
            player.CaptureLook(true);
            yield return Frames(2);
            Assert.IsFalse(ui.IsMapListOpen);

            yield return Tap(keyboard.mKey);
            Assert.IsTrue(ui.IsMapListOpen, "M으로 맵 목록 창이 열리지 않았습니다.");
            Assert.IsTrue(ui.IsModalOpen);
            Assert.IsTrue(player.InputBlocked);
            Assert.IsFalse(player.LookCaptured, "창을 누를 수 있게 마우스를 놓아야 합니다.");

            yield return Tap(keyboard.mKey);
            Assert.IsFalse(ui.IsMapListOpen);
            Assert.IsFalse(player.InputBlocked);
            Assert.IsTrue(player.LookCaptured, "창을 닫으면 바로 시점 조작으로 돌아가야 합니다.");

            // Esc는 창만 닫고 메뉴를 열지 않는다.
            yield return Tap(keyboard.mKey);
            yield return Tap(keyboard.escapeKey);
            Assert.IsFalse(ui.IsMapListOpen);
            Assert.IsFalse(ui.IsMenuOpen, "창을 닫는 Esc로 메뉴가 열렸습니다.");
        }

        [UnityTest]
        public IEnumerator 메뉴의_내_작업실_타일로_맵_목록_창이_열린다()
        {
            yield return LoadMapsScene();
            player.CaptureLook(true);
            yield return Tap(keyboard.escapeKey);
            Assert.IsTrue(ui.IsMenuOpen);

            Find<Button>(ui.Menu, "HomeTile").onClick.Invoke();
            yield return Frames(2);

            Assert.IsFalse(ui.IsMenuOpen, "맵 목록 창을 열면 메뉴는 닫혀야 합니다.");
            Assert.IsTrue(ui.IsMapListOpen, "내 작업실 타일로 맵 목록 창이 열리지 않았습니다.");
            Assert.IsTrue(player.InputBlocked, "메뉴에서 넘어와도 조작은 계속 막혀 있어야 합니다.");

            Find<Button>(ui.MapList, "CloseMapList").onClick.Invoke();
            Assert.IsFalse(ui.IsMapListOpen);
            Assert.IsFalse(player.InputBlocked);

            // 메뉴가 열려 있을 때 M을 눌러도 넘어간다.
            yield return Tap(keyboard.escapeKey);
            yield return Tap(keyboard.mKey);
            Assert.IsTrue(ui.IsMapListOpen);
            Assert.IsFalse(ui.IsMenuOpen);
        }

        [UnityTest]
        public IEnumerator 처음에는_처음부터_있던_맵_하나가_지금_맵으로_보인다()
        {
            yield return LoadMapsScene();
            Assert.AreEqual(MapLibrary.DefaultId, autoSave.MapId);
            Assert.AreEqual(FirstMapName, autoSave.MapName);
            StringAssert.StartsWith(FirstMapName, ui.RoomText, "화면 위쪽에 지금 맵의 이름이 보여야 합니다.");

            ui.OpenMapList();
            yield return Frames(2);

            Assert.AreEqual(1, ui.MapList.MapCount);
            Assert.AreEqual(MapLibrary.DefaultId, ui.MapList.IdAt(0));
            Assert.IsNull(ui.MapList.IdAt(1));

            MapListView.Row row = ui.MapList.GetRow(0);
            Assert.AreEqual(FirstMapName, row.nameLabel.text);
            StringAssert.StartsWith($"블록 {SceneBlockCount}개", row.infoLabel.text);
            Assert.IsTrue(row.currentBadge.activeSelf, "지금 맵 표시가 없습니다.");
            Assert.IsFalse(row.openButton.interactable, "이미 열려 있는 맵의 열기는 누를 수 없어야 합니다.");
            Assert.IsFalse(ui.MapList.GetRow(1).root.activeSelf, "맵이 없는 줄은 보이지 않아야 합니다.");
            Assert.AreEqual("1 / 1", ui.MapList.PageText);
        }

        [UnityTest]
        public IEnumerator 새_맵은_빈_바닥으로_시작하고_처음의_맵은_그대로_남는다()
        {
            yield return LoadMapsScene();
            GameObject scenery = Scenery();
            Assert.IsTrue(scenery.activeSelf, "처음부터 있던 맵에서는 나무와 지붕이 보여야 합니다.");

            // 처음의 맵에 블록을 하나 놓고, 저장되기 전에 새 맵을 만든다.
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(0, CellCenter(3, 0, -3)));
            Assert.IsTrue(autoSave.HasPendingChanges);
            player.transform.position = new Vector3(3f, 0f, 3f);

            ui.OpenMapList();
            Find<Button>(ui.MapList, "CreateMap").onClick.Invoke();
            yield return Frames(3);

            Assert.IsFalse(ui.IsMapListOpen, "새 맵을 만들면 창이 닫혀야 합니다.");
            Assert.AreNotEqual(MapLibrary.DefaultId, autoSave.MapId);
            Assert.AreEqual(MapLibrary.DefaultName, autoSave.MapName);
            Assert.AreEqual(0, world.Count, "새 맵은 빈 맵이어야 합니다.");
            Assert.AreEqual(0, world.GetComponentsInChildren<PlacedBlock>().Length, "화면에 앞 맵의 블록이 남았습니다.");
            Assert.IsFalse(world.History.CanUndo, "맵을 바꾸면 되돌리기 기록을 비웁니다.");
            Assert.IsFalse(scenery.activeSelf, "새 맵에서는 처음 장면의 나무와 지붕을 감춥니다.");
            Assert.Less(Vector3.Distance(SpawnPoint, player.transform.position), 0.6f, "맵을 바꾸면 캐릭터가 시작 위치로 가야 합니다.");
            StringAssert.StartsWith(MapLibrary.DefaultName, ui.RoomText);
            Assert.AreEqual($"블록 0/{world.MaxBlocks}", Find<TMP_Text>(ui, "BlockCount").text);
            Assert.IsTrue(notice.IsVisible);
            StringAssert.Contains(MapAutoSave.CreatedMessage, notice.Message);

            // 처음의 맵 파일에는 방금 놓은 블록까지 저장되어 있다.
            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf(MapLibrary.DefaultId), out MapDocument first, out _));
            Assert.AreEqual(SceneBlockCount + 1, first.blocks.Count, "맵을 바꾸기 전에 남은 변경을 저장해야 합니다.");
            Assert.IsTrue(File.Exists(autoSave.FilePath), "새 맵의 파일이 없습니다.");
            Assert.AreEqual(2, MapLibrary.List().Count);
        }

        [UnityTest]
        public IEnumerator 맵을_오가도_각자의_블록이_남는다()
        {
            yield return LoadMapsScene();
            string created = autoSave.CreateNew("탑");
            Assert.IsNotNull(created);
            var spot = new Vector3(1.37f, 0.5f, -2.2f);
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(2, spot, Quaternion.Euler(0f, 30f, 0f), out _));
            Assert.AreEqual(1, world.Count);

            // 목록에서 처음의 맵을 연다.
            ui.OpenMapList();
            yield return Frames(2);
            Assert.AreEqual(2, ui.MapList.MapCount);
            int row = ui.MapList.RowOf(MapLibrary.DefaultId);
            Assert.GreaterOrEqual(row, 0);
            ui.MapList.GetRow(row).openButton.onClick.Invoke();
            yield return Frames(3);

            Assert.AreEqual(MapLibrary.DefaultId, autoSave.MapId);
            Assert.AreEqual(SceneBlockCount, world.Count);
            Assert.IsFalse(HasBlockNear(world, spot), "다른 맵의 블록이 따라왔습니다.");
            Assert.IsTrue(Scenery().activeSelf);
            Assert.IsFalse(ui.IsMapListOpen);
            StringAssert.Contains(MapAutoSave.OpenedMessage, notice.Message);
            StringAssert.StartsWith(FirstMapName, ui.RoomText);

            // 다시 새 맵으로 돌아가면 놓았던 블록이 방향까지 그대로 있다.
            Assert.IsTrue(autoSave.Open(created));
            yield return Frames(2);
            Assert.AreEqual(1, world.Count);
            Assert.IsTrue(BlockNear(world, spot, out BlockRecord record, 0.002f));
            Assert.AreEqual(2, record.Part);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 30f, 0f), record.Rotation), 0.1f);
            Assert.AreEqual("탑", autoSave.MapName);
        }

        [UnityTest]
        public IEnumerator 이름을_바꾸면_파일과_화면에_반영되고_고치는_동안_키가_게임에_듣지_않는다()
        {
            yield return LoadMapsScene();
            player.CaptureLook(true);
            yield return Tap(keyboard.mKey);

            ui.MapList.GetRow(0).renameButton.onClick.Invoke();
            yield return Frames(2);
            Assert.IsTrue(ui.MapList.IsEditingName);
            Assert.IsTrue(Find<Transform>(ui.MapList, "RenameBar").gameObject.activeSelf);
            Assert.AreEqual(FirstMapName, ui.MapList.NameText, "글자 칸에는 지금 이름이 들어 있어야 합니다.");

            // 이름에 들어갈 수 있는 글자의 키는 창을 닫거나 다른 창을 열지 않는다.
            yield return Tap(keyboard.mKey);
            Assert.IsTrue(ui.IsMapListOpen, "이름을 고치는 중에 M으로 창이 닫혔습니다.");
            yield return Tap(keyboard.bKey);
            Assert.IsFalse(ui.IsPickerOpen, "이름을 고치는 중에 B로 부품 고르는 창이 열렸습니다.");

            // Esc는 고치기만 그만둔다.
            yield return Tap(keyboard.escapeKey);
            Assert.IsFalse(ui.MapList.IsEditingName);
            Assert.IsTrue(ui.IsMapListOpen, "고치기를 그만두는 Esc로 창까지 닫혔습니다.");
            Assert.AreEqual(FirstMapName, autoSave.MapName);

            ui.MapList.GetRow(0).renameButton.onClick.Invoke();
            ui.MapList.SetNameText("  우리   집 ");
            Find<Button>(ui.MapList, "RenameConfirm").onClick.Invoke();
            yield return Frames(2);

            Assert.IsFalse(ui.MapList.IsEditingName);
            Assert.AreEqual("우리 집", autoSave.MapName, "이름은 빈칸을 다듬어 넣어야 합니다.");
            Assert.AreEqual(MapLibrary.DefaultId, autoSave.MapId, "이름을 바꿔도 번호표는 그대로여야 합니다.");
            Assert.AreEqual("우리 집", ui.MapList.GetRow(0).nameLabel.text);
            StringAssert.StartsWith("우리 집", ui.RoomText);
            Assert.AreEqual(MapAutoSave.RenamedMessage, notice.Message);
            Assert.IsTrue(MapStorage.TryLoad(autoSave.FilePath, out MapDocument saved, out _));
            Assert.AreEqual("우리 집", saved.name);
            Assert.AreEqual(SceneBlockCount, saved.blocks.Count);

            // 화면 위쪽의 이름 칸은 이름의 길이에 맞게 넓어지고, 아주 긴 이름에서도 정해 둔 너비를 넘지 않는다.
            var chip = (RectTransform)Find<Transform>(ui, "RoomChip");
            float shortWidth = chip.sizeDelta.x;
            Assert.IsTrue(autoSave.Rename(autoSave.MapId, "아주아주 길게 지은 우리 마을의 이름입니다"));
            Assert.Greater(chip.sizeDelta.x, shortWidth, "긴 이름인데 이름 칸이 넓어지지 않았습니다.");
            Assert.LessOrEqual(chip.sizeDelta.x, 560.5f);
            Assert.IsTrue(autoSave.Rename(autoSave.MapId, "우리 집"));
            Assert.AreEqual(shortWidth, chip.sizeDelta.x, 0.5f);
            ui.OpenMapList();
            yield return Frames(2);

            // 빈 이름으로는 바뀌지 않고 고치기가 이어진다.
            ui.MapList.GetRow(0).renameButton.onClick.Invoke();
            ui.MapList.SetNameText("   ");
            Find<Button>(ui.MapList, "RenameConfirm").onClick.Invoke();
            Assert.IsTrue(ui.MapList.IsEditingName);
            Assert.AreEqual("우리 집", autoSave.MapName);
        }

        [UnityTest]
        public IEnumerator 열려_있지_않은_맵의_이름도_바꾼다()
        {
            yield return LoadMapsScene();
            string other = MapLibrary.Create("탑", world.BoundsMin, world.BoundsMax);

            ui.OpenMapList();
            yield return Frames(2);
            int row = ui.MapList.RowOf(other);
            ui.MapList.GetRow(row).renameButton.onClick.Invoke();
            ui.MapList.SetNameText("높은 탑");
            ui.MapList.ConfirmRename();
            yield return Frames(2);

            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf(other), out MapDocument document, out _));
            Assert.AreEqual("높은 탑", document.name);
            Assert.AreEqual(FirstMapName, autoSave.MapName, "지금 맵의 이름은 그대로여야 합니다.");
            Assert.AreEqual("높은 탑", ui.MapList.GetRow(ui.MapList.RowOf(other)).nameLabel.text);
        }

        [UnityTest]
        public IEnumerator 지우기는_두_번_눌러야_하고_파일은_휴지통_폴더에_남는다()
        {
            yield return LoadMapsScene();
            string other = MapLibrary.Create("지울 맵", world.BoundsMin, world.BoundsMax);

            ui.OpenMapList();
            yield return Frames(2);
            int row = ui.MapList.RowOf(other);
            MapListView.Row view = ui.MapList.GetRow(row);
            Assert.AreEqual("지우기", view.deleteLabel.text);

            view.deleteButton.onClick.Invoke();
            Assert.AreEqual("한 번 더", view.deleteLabel.text, "한 번 누르면 확인을 기다려야 합니다.");
            Assert.IsTrue(MapLibrary.Exists(other), "한 번 눌렀는데 지워졌습니다.");

            view.deleteButton.onClick.Invoke();
            yield return Frames(2);

            Assert.IsFalse(MapLibrary.Exists(other));
            Assert.AreEqual(1, ui.MapList.MapCount);
            Assert.AreEqual(-1, ui.MapList.RowOf(other));
            Assert.AreEqual(MapAutoSave.DeletedMessage, notice.Message);
            Assert.IsTrue(ui.IsMapListOpen, "지운 뒤에도 창은 열려 있어야 합니다.");
            Assert.AreEqual(MapLibrary.DefaultId, autoSave.MapId);
            Assert.AreEqual(SceneBlockCount, world.Count, "열려 있지 않은 맵을 지워도 지금 맵은 그대로여야 합니다.");

            string[] trashed = Directory.GetFiles(MapLibrary.TrashDirectory);
            Assert.AreEqual(1, trashed.Length, "지운 맵의 파일이 휴지통 폴더에 없습니다.");
            Assert.IsTrue(MapStorage.TryLoad(trashed[0], out MapDocument document, out _));
            Assert.AreEqual("지울 맵", document.name);
        }

        [UnityTest]
        public IEnumerator 지우기를_한_번만_누르면_잠시_뒤_되돌아간다()
        {
            yield return LoadMapsScene();
            ui.OpenMapList();
            yield return Frames(2);
            MapListView.Row view = ui.MapList.GetRow(0);

            view.deleteButton.onClick.Invoke();
            Assert.AreEqual("한 번 더", view.deleteLabel.text);

            yield return new WaitForSecondsRealtime(3.2f);
            Assert.AreEqual("지우기", view.deleteLabel.text);
            Assert.IsTrue(MapLibrary.Exists(MapLibrary.DefaultId));
        }

        [UnityTest]
        public IEnumerator 지금_맵을_지우면_다른_맵이_열리고_마지막_맵을_지우면_빈_맵으로_이어진다()
        {
            yield return LoadMapsScene();
            string created = autoSave.CreateNew("둘째");
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(1, CellCenter(0, 0, -3)));
            player.transform.position = new Vector3(3f, 0f, 3f);

            ui.OpenMapList();
            yield return Frames(2);
            int row = ui.MapList.RowOf(created);
            ui.MapList.GetRow(row).deleteButton.onClick.Invoke();
            ui.MapList.GetRow(row).deleteButton.onClick.Invoke();
            yield return Frames(3);

            Assert.AreEqual(MapLibrary.DefaultId, autoSave.MapId, "지금 맵을 지우면 남은 맵이 열려야 합니다.");
            Assert.AreEqual(SceneBlockCount, world.Count);
            Assert.IsFalse(MapLibrary.Exists(created));
            Assert.Less(Vector3.Distance(SpawnPoint, player.transform.position), 0.6f);
            Assert.AreEqual(1, ui.MapList.MapCount);

            // 휴지통의 파일에는 지우기 직전에 놓은 블록까지 들어 있다.
            string[] trashed = Directory.GetFiles(MapLibrary.TrashDirectory);
            Assert.AreEqual(1, trashed.Length);
            Assert.IsTrue(MapStorage.TryLoad(trashed[0], out MapDocument document, out _));
            Assert.AreEqual(1, document.blocks.Count);

            // 마지막 남은 맵을 지운다.
            ui.MapList.GetRow(0).deleteButton.onClick.Invoke();
            ui.MapList.GetRow(0).deleteButton.onClick.Invoke();
            yield return Frames(3);

            Assert.IsFalse(MapLibrary.Exists(MapLibrary.DefaultId));
            Assert.AreNotEqual(MapLibrary.DefaultId, autoSave.MapId);
            Assert.AreEqual(0, world.Count, "남은 맵이 없으면 빈 맵으로 이어 가야 합니다.");
            Assert.AreEqual(MapLibrary.DefaultName, autoSave.MapName);
            Assert.IsTrue(File.Exists(autoSave.FilePath));
            Assert.AreEqual(1, ui.MapList.MapCount);
            Assert.AreEqual(2, Directory.GetFiles(MapLibrary.TrashDirectory).Length);
        }

        [UnityTest]
        public IEnumerator 다시_켜면_마지막으로_연_맵이_열린다()
        {
            yield return LoadMapsScene();
            string created = autoSave.CreateNew("이어서 만들 맵");
            var spot = new Vector3(-1.4f, 0.5f, -3.3f);
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(3, spot));

            yield return LoadMapsScene();

            Assert.AreEqual(created, autoSave.MapId, "마지막으로 연 맵이 다시 열리지 않았습니다.");
            Assert.AreEqual("이어서 만들 맵", autoSave.MapName);
            Assert.AreEqual(1, world.Count);
            Assert.IsTrue(HasBlockNear(world, spot, 0.002f));
            Assert.IsFalse(Scenery().activeSelf);
            StringAssert.StartsWith("이어서 만들 맵", ui.RoomText);
        }

        [UnityTest]
        public IEnumerator 맵이_많으면_쪽을_넘겨_본다()
        {
            yield return LoadMapsScene();
            for (int i = 0; i < 7; i++)
            {
                Assert.IsNotNull(MapLibrary.Create($"맵 {i + 1}", world.BoundsMin, world.BoundsMax));
            }

            ui.OpenMapList();
            yield return Frames(2);

            Assert.AreEqual(8, ui.MapList.MapCount);
            Assert.AreEqual(6, ui.MapList.RowsPerPage);
            Assert.AreEqual(2, ui.MapList.PageCount);
            Assert.AreEqual("1 / 2", ui.MapList.PageText);
            Assert.IsFalse(Find<Button>(ui.MapList, "PreviousPage").interactable);

            Find<Button>(ui.MapList, "NextPage").onClick.Invoke();
            Assert.AreEqual(1, ui.MapList.Page);
            Assert.AreEqual("2 / 2", ui.MapList.PageText);
            Assert.IsNotNull(ui.MapList.IdAt(1));
            Assert.IsNull(ui.MapList.IdAt(2), "둘째 쪽에는 맵이 둘뿐이어야 합니다.");
            Assert.IsFalse(ui.MapList.GetRow(2).root.activeSelf);
            Assert.IsFalse(Find<Button>(ui.MapList, "NextPage").interactable);

            var seen = new HashSet<string>();
            for (int page = 0; page < ui.MapList.PageCount; page++)
            {
                ui.MapList.ShowPage(page);
                for (int i = 0; i < ui.MapList.RowsPerPage; i++)
                {
                    string id = ui.MapList.IdAt(i);
                    if (id != null) Assert.IsTrue(seen.Add(id), "같은 맵이 두 번 보입니다.");
                }
            }

            Assert.AreEqual(8, seen.Count);
        }

        [UnityTest]
        public IEnumerator 읽을_수_없는_맵은_열_수_없다고_보이고_눌러도_지금_맵이_그대로다()
        {
            yield return LoadMapsScene();
            File.WriteAllText(MapLibrary.PathOf("broken"), "{ \"format\": \"atelier-verse-map\", ");

            ui.OpenMapList();
            yield return Frames(2);
            int row = ui.MapList.RowOf("broken");
            Assert.GreaterOrEqual(row, 0);
            MapListView.Row view = ui.MapList.GetRow(row);
            Assert.AreEqual("읽을 수 없는 파일", view.infoLabel.text);
            Assert.IsFalse(view.openButton.interactable);

            Assert.IsFalse(autoSave.Open("broken"), "읽을 수 없는 맵이 열렸습니다.");
            Assert.AreEqual(MapLibrary.DefaultId, autoSave.MapId);
            Assert.AreEqual(SceneBlockCount, world.Count);
            Assert.IsTrue(File.Exists(MapLibrary.PathOf("broken")), "읽지 못했다고 파일을 건드리면 안 됩니다.");
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadMapsScene();
            BeginCapture();

            // 맵 셋: 처음의 맵, 이름을 붙인 맵, 방금 만든 새 맵(지금 맵).
            string tower = autoSave.CreateNew("언덕 위의 탑");
            for (int y = 0; y < 4; y++)
            {
                world.Add(1, CellCenter(0, y, -2));
            }

            autoSave.SaveNow();
            autoSave.CreateNew();
            world.Add(world.Catalog.IndexOf("stairs.wood"), new Vector3(0.5f, 0.5f, -2.5f));
            world.Add(world.Catalog.IndexOf("slab.clay"), new Vector3(1.5f, 0.25f, -2.5f));
            yield return Frames(3);
            Assert.IsNotNull(tower);

            // 새 맵: 처음 장면의 나무와 집이 없는 빈 바닥.
            player.CaptureLook(true);
            player.SetLook(0f, 12f);
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "maps-new.png"));

            yield return Tap(keyboard.mKey);
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "maps-list.png"));

            ui.MapList.GetRow(ui.MapList.RowOf(tower)).renameButton.onClick.Invoke();
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "maps-rename.png"));
        }

        private static GameObject Scenery()
        {
            GameObject root = GameObject.Find("Day1_Sandbox");
            Assert.IsNotNull(root, "씬에서 처음 장면의 뿌리를 찾을 수 없습니다.");
            Transform scenery = root.transform.Find("Blocks");
            Assert.IsNotNull(scenery, "씬에서 처음 장면의 꾸밈 묶음을 찾을 수 없습니다.");
            return scenery.gameObject;
        }

        private IEnumerator LoadMapsScene()
        {
            yield return LoadSandbox();

            world = Object.FindAnyObjectByType<BlockWorld>();
            autoSave = Object.FindAnyObjectByType<MapAutoSave>();
            notice = Object.FindAnyObjectByType<NoticeBar>();
            Assert.IsNotNull(world, "Sandbox 씬에서 블록 세계를 찾을 수 없습니다.");
            Assert.IsNotNull(autoSave, "Sandbox 씬에서 자동 저장을 찾을 수 없습니다.");
            Assert.IsNotNull(notice, "게임 화면에 알림 띠가 없습니다.");
            Assert.IsNotNull(ui.MapList, "게임 화면에 맵 목록 창이 없습니다.");
            yield return Frames(2);
        }
    }
}
