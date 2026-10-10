using System.Collections;
using System.IO;
using AtelierVerse.Core;
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
    /// 맵 정보와 시작 위치를 실제로 실행해 확인한다(18일차): 맵 정보 창, 이름과 설명 저장, 대표 그림 찍기,
    /// 맵마다의 시작 위치와 바닥의 표식, 옛 판(2판) 파일을 올려 읽기.
    /// 창의 단추는 누름 동작을 직접 부르고, 글자는 글자 칸에 직접 넣는다.
    /// 실제 글자판으로 한글을 치는 것과 찍힌 그림이 보기 좋은지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class MapInfoPlayTests : PlayTestBase
    {
        private const int SceneBlockCount = 19;
        private const string FirstMapName = "시험 작업실";
        private const float SaveWait = 1.6f;

        private static readonly Vector3 DefaultSpawn = new Vector3(0.5f, 0f, -5.5f);

        private BlockWorld world;
        private MapAutoSave autoSave;
        private NoticeBar notice;
        private PlayerSpawn spawn;
        private SpawnMarker marker;

        [UnityTest]
        public IEnumerator 줄의_정보로_맵_정보_창이_열리고_Esc로_목록에_돌아오며_M으로_모두_닫힌다()
        {
            yield return LoadInfoScene();
            player.CaptureLook(true);
            yield return Tap(keyboard.mKey);
            Assert.IsTrue(ui.IsMapListOpen);

            ui.MapList.GetRow(0).infoButton.onClick.Invoke();
            yield return Frames(2);

            Assert.IsTrue(ui.IsMapInfoOpen, "줄의 정보로 맵 정보 창이 열리지 않았습니다.");
            Assert.IsFalse(ui.IsMapListOpen, "맵 정보 창이 열려 있는 동안 맵 목록 창은 닫혀 있어야 합니다.");
            Assert.IsTrue(ui.IsModalOpen);
            Assert.IsTrue(player.InputBlocked, "창이 바뀌어도 조작은 계속 막혀 있어야 합니다.");
            Assert.AreEqual(MapLibrary.DefaultId, ui.MapInfo.MapId);
            Assert.IsTrue(ui.MapInfo.IsCurrentMap);
            Assert.AreEqual(FirstMapName, ui.MapInfo.NameText, "글자 칸에는 지금 이름이 들어 있어야 합니다.");
            Assert.AreEqual(string.Empty, ui.MapInfo.DescriptionText);
            StringAssert.StartsWith($"블록 {SceneBlockCount}개 · 만든 날 ", ui.MapInfo.FactsText);
            Assert.AreEqual(MapInfoView.DefaultSpawnText, ui.MapInfo.SpawnText);
            Assert.IsFalse(ui.MapInfo.HasThumbnail);

            yield return Tap(keyboard.escapeKey);
            Assert.IsFalse(ui.IsMapInfoOpen);
            Assert.IsTrue(ui.IsMapListOpen, "Esc는 맵 목록으로 돌아가야 합니다.");
            Assert.IsFalse(ui.IsMenuOpen);
            Assert.IsTrue(player.InputBlocked);

            ui.MapList.GetRow(0).infoButton.onClick.Invoke();
            Find<Button>(ui.MapInfo, "BackToList").onClick.Invoke();
            Assert.IsTrue(ui.IsMapListOpen, "목록으로 단추가 맵 목록으로 돌아가지 않았습니다.");

            ui.MapList.GetRow(0).infoButton.onClick.Invoke();
            yield return Frames(2);
            yield return Tap(keyboard.mKey);
            Assert.IsFalse(ui.IsMapInfoOpen);
            Assert.IsFalse(ui.IsMapListOpen, "M은 창을 모두 닫아야 합니다.");
            Assert.IsFalse(player.InputBlocked);
            Assert.IsTrue(player.LookCaptured);
        }

        [UnityTest]
        public IEnumerator 이름과_설명을_저장하면_파일과_목록과_화면에_반영된다()
        {
            yield return LoadInfoScene();
            yield return OpenInfo(MapLibrary.DefaultId);

            ui.MapInfo.SetNameText("  우리   집 ");
            ui.MapInfo.SetDescriptionText(" 언덕 위의   작은 집 ");
            Find<Button>(ui.MapInfo, "SaveInfo").onClick.Invoke();
            yield return Frames(2);

            Assert.AreEqual("우리 집", autoSave.MapName, "이름은 빈칸을 다듬어 넣어야 합니다.");
            Assert.AreEqual("언덕 위의 작은 집", autoSave.Description);
            Assert.AreEqual(MapLibrary.DefaultId, autoSave.MapId, "이름을 바꿔도 번호표는 그대로여야 합니다.");
            Assert.AreEqual(MapAutoSave.InfoSavedMessage, notice.Message);
            Assert.IsTrue(ui.IsMapInfoOpen, "저장한 뒤에도 창은 열려 있어야 합니다.");
            Assert.AreEqual("우리 집", ui.MapInfo.NameText, "저장한 뒤 글자 칸에는 다듬은 이름이 보여야 합니다.");
            StringAssert.StartsWith("우리 집", ui.RoomText);

            Assert.IsTrue(MapStorage.TryLoad(autoSave.FilePath, out MapDocument saved, out _));
            Assert.AreEqual("우리 집", saved.name);
            Assert.AreEqual("언덕 위의 작은 집", saved.description);
            Assert.AreEqual(SceneBlockCount, saved.blocks.Count);

            // 빈 이름으로는 저장되지 않는다.
            ui.MapInfo.SetNameText("   ");
            ui.MapInfo.Save();
            Assert.AreEqual("우리 집", autoSave.MapName);

            // 목록의 줄에 이름과 설명이 보인다.
            Find<Button>(ui.MapInfo, "BackToList").onClick.Invoke();
            yield return Frames(2);
            MapListView.Row row = ui.MapList.GetRow(ui.MapList.RowOf(MapLibrary.DefaultId));
            Assert.AreEqual("우리 집", row.nameLabel.text);
            StringAssert.EndsWith(" · 언덕 위의 작은 집", row.infoLabel.text);

            // 화면 위쪽의 이름 칸은 이름의 길이에 맞게 넓어지고, 아주 긴 이름에서도 정해 둔 너비를 넘지 않는다.
            var chip = (RectTransform)Find<Transform>(ui, "RoomChip");
            float shortWidth = chip.sizeDelta.x;
            Assert.IsTrue(autoSave.Rename(autoSave.MapId, "아주아주 길게 지은 우리 마을의 이름입니다"));
            Assert.Greater(chip.sizeDelta.x, shortWidth, "긴 이름인데 이름 칸이 넓어지지 않았습니다.");
            Assert.LessOrEqual(chip.sizeDelta.x, 560.5f);
            Assert.AreEqual("언덕 위의 작은 집", autoSave.Description, "이름만 바꿀 때 설명이 지워지면 안 됩니다.");
        }

        [UnityTest]
        public IEnumerator 저장하지_않고_목록으로_돌아가면_쓴_글자는_버려진다()
        {
            yield return LoadInfoScene();
            yield return OpenInfo(MapLibrary.DefaultId);

            ui.MapInfo.SetNameText("저장하지 않은 이름");
            ui.MapInfo.SetDescriptionText("저장하지 않은 설명");
            Find<Button>(ui.MapInfo, "BackToList").onClick.Invoke();
            yield return Frames(2);

            Assert.AreEqual(FirstMapName, autoSave.MapName);
            Assert.AreEqual(string.Empty, autoSave.Description);

            ui.MapList.GetRow(0).infoButton.onClick.Invoke();
            yield return Frames(2);
            Assert.AreEqual(FirstMapName, ui.MapInfo.NameText, "다시 열었을 때 버린 글자가 남아 있으면 안 됩니다.");
            Assert.AreEqual(string.Empty, ui.MapInfo.DescriptionText);
        }

        [UnityTest]
        public IEnumerator 글자를_쓰는_동안에는_글자_키가_게임에_듣지_않는다()
        {
            yield return LoadInfoScene();
            yield return OpenInfo(MapLibrary.DefaultId);
            Assert.IsFalse(ui.MapInfo.IsEditingText);

            ui.MapInfo.FocusName();
            yield return Frames(3);
            Assert.IsTrue(ui.MapInfo.IsEditingText, "글자 칸을 골랐는데 쓰는 중으로 알지 못합니다.");

            // 이름에 들어갈 수 있는 글자의 키는 창을 닫거나 다른 창을 열지 않는다.
            yield return Tap(keyboard.mKey);
            Assert.IsTrue(ui.IsMapInfoOpen, "글자를 쓰는 중에 M으로 창이 닫혔습니다.");
            yield return Tap(keyboard.bKey);
            Assert.IsFalse(ui.IsPickerOpen, "글자를 쓰는 중에 B로 부품 고르는 창이 열렸습니다.");

            // Esc는 쓰기만 그만둔다. 한 번 더 누르면 목록으로 돌아간다.
            yield return Tap(keyboard.escapeKey);
            yield return Frames(2);
            Assert.IsFalse(ui.MapInfo.IsEditingText);
            Assert.IsTrue(ui.IsMapInfoOpen, "쓰기를 그만두는 Esc로 창까지 닫혔습니다.");

            yield return Tap(keyboard.escapeKey);
            Assert.IsTrue(ui.IsMapListOpen);
        }

        [UnityTest]
        public IEnumerator 열려_있지_않은_맵은_이름과_설명만_고칠_수_있다()
        {
            yield return LoadInfoScene();
            string other = MapLibrary.Create("탑", world.BoundsMin, world.BoundsMax);
            yield return OpenInfo(other);

            Assert.IsFalse(ui.MapInfo.IsCurrentMap);
            Assert.IsFalse(Find<Button>(ui.MapInfo, "Snapshot").interactable, "열려 있지 않은 맵의 그림은 찍을 수 없습니다.");
            Assert.IsFalse(Find<Button>(ui.MapInfo, "SetSpawn").interactable, "열려 있지 않은 맵의 시작 위치는 정할 수 없습니다.");
            Assert.IsFalse(Find<Button>(ui.MapInfo, "ClearSpawn").interactable);
            Assert.AreEqual(MapInfoView.OtherMapNote, Find<TMP_Text>(ui.MapInfo, "Note").text);

            ui.MapInfo.SetNameText("높은 탑");
            ui.MapInfo.SetDescriptionText("아직 짓는 중");
            ui.MapInfo.Save();
            yield return Frames(2);

            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf(other), out MapDocument document, out _));
            Assert.AreEqual("높은 탑", document.name);
            Assert.AreEqual("아직 짓는 중", document.description);
            Assert.AreEqual(FirstMapName, autoSave.MapName, "지금 맵의 이름은 그대로여야 합니다.");
            Assert.AreEqual(MapLibrary.DefaultId, autoSave.MapId);

            // 지금 맵의 정보 창에서는 단추를 누를 수 있다.
            Find<Button>(ui.MapInfo, "BackToList").onClick.Invoke();
            yield return Frames(2);
            ui.MapList.GetRow(ui.MapList.RowOf(MapLibrary.DefaultId)).infoButton.onClick.Invoke();
            yield return Frames(2);
            Assert.IsTrue(Find<Button>(ui.MapInfo, "Snapshot").interactable);
            Assert.IsTrue(Find<Button>(ui.MapInfo, "SetSpawn").interactable);
            Assert.IsFalse(Find<Button>(ui.MapInfo, "ClearSpawn").interactable, "정한 시작 위치가 없으면 되돌릴 것도 없습니다.");
            Assert.AreEqual(string.Empty, Find<TMP_Text>(ui.MapInfo, "Note").text);
        }

        [UnityTest]
        public IEnumerator 대표_그림을_찍으면_그림_파일이_생기고_창과_목록에_보인다()
        {
            yield return LoadInfoScene();
            MapLibrary.Create("그림 없는 맵", world.BoundsMin, world.BoundsMax);
            player.SetLook(0f, 10f);
            yield return Frames(2);
            yield return OpenInfo(MapLibrary.DefaultId);
            string path = MapLibrary.ThumbnailPathOf(MapLibrary.DefaultId);
            Assert.IsFalse(File.Exists(path));
            Assert.IsTrue(Find<Transform>(ui.MapInfo, "Empty").gameObject.activeSelf, "그림이 없으면 없다고 보여야 합니다.");

            Find<Button>(ui.MapInfo, "Snapshot").onClick.Invoke();
            yield return Frames(2);

            Assert.IsTrue(File.Exists(path), "대표 그림 파일이 만들어지지 않았습니다.");
            byte[] png = File.ReadAllBytes(path);
            Assert.IsTrue(SceneSnapshot.IsPng(png), "찍은 그림이 PNG가 아닙니다.");
            Assert.AreEqual(GameUi.SnapshotMessage, notice.Message);
            Assert.IsTrue(ui.MapInfo.HasThumbnail, "찍은 그림이 창에 보이지 않습니다.");

            var picture = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(picture.LoadImage(png));
                Assert.AreEqual(SceneSnapshot.DefaultWidth, picture.width);
                Assert.AreEqual(SceneSnapshot.DefaultHeight, picture.height);

                // 하늘(위)과 바닥(아래)의 색이 다르다. 한 가지 색으로만 찍혔으면 장면이 그려지지 않은 것이다.
                Color sky = picture.GetPixel(picture.width / 2, picture.height - 4);
                Color floor = picture.GetPixel(picture.width / 2, 4);
                float difference = Mathf.Abs(sky.r - floor.r) + Mathf.Abs(sky.g - floor.g) + Mathf.Abs(sky.b - floor.b);
                Assert.Greater(difference, 0.05f, "그림이 한 가지 색으로만 찍혔습니다.");
            }
            finally
            {
                Object.Destroy(picture);
            }

            // 목록에서는 찍은 맵의 줄에만 작은 그림이 보인다.
            Find<Button>(ui.MapInfo, "BackToList").onClick.Invoke();
            yield return Frames(2);
            Assert.AreEqual(2, ui.MapList.MapCount);
            for (int i = 0; i < 2; i++)
            {
                MapListView.Row row = ui.MapList.GetRow(i);
                bool isFirstMap = ui.MapList.IdAt(i) == MapLibrary.DefaultId;
                Assert.AreEqual(isFirstMap, row.thumbnail.enabled && row.thumbnail.texture != null, $"{i}번째 줄의 그림이 맞지 않습니다.");
            }
        }

        [UnityTest]
        public IEnumerator 지금_선_자리를_시작_위치로_정하면_표식이_옮겨지고_시작_위치로_가면_그_자리에_선다()
        {
            yield return LoadInfoScene();
            Assert.Less(Vector3.Distance(DefaultSpawn, marker.Position), 0.05f, "처음에는 표식이 처음 자리에 있어야 합니다.");
            Assert.IsFalse(autoSave.HasSpawn);

            var here = new Vector3(3f, 0f, -2f);
            Teleport(here, 90f);
            yield return OpenInfo(MapLibrary.DefaultId);
            Find<Button>(ui.MapInfo, "SetSpawn").onClick.Invoke();
            yield return Frames(2);

            Assert.IsTrue(autoSave.HasSpawn, "시작 위치가 정해지지 않았습니다.");
            Assert.Less(Vector3.Distance(here, autoSave.SpawnPosition), 0.05f);
            Assert.AreEqual(90f, autoSave.SpawnYaw, 0.5f);
            Assert.AreEqual(PlayerSpawn.SetMessage, notice.Message);
            StringAssert.Contains("정한 자리", ui.MapInfo.SpawnText);
            Assert.IsTrue(Find<Button>(ui.MapInfo, "ClearSpawn").interactable);
            Assert.Less(Vector3.Distance(here, marker.Position), 0.05f, "표식이 새 시작 위치로 옮겨지지 않았습니다.");
            Assert.AreEqual(90f, marker.Yaw, 0.5f);

            Assert.IsTrue(MapStorage.TryLoad(autoSave.FilePath, out MapDocument saved, out _));
            Assert.IsTrue(saved.spawn.custom, "시작 위치가 파일에 저장되지 않았습니다.");
            Assert.Less(Vector3.Distance(here, saved.spawn.position), 0.05f);

            // 다른 곳으로 갔다가 메뉴의 "시작 위치로"(R)를 누르면 정한 자리에 정한 쪽을 보고 선다.
            ui.CloseMapInfo();
            Teleport(new Vector3(-4f, 0f, -6f), 200f);
            yield return Frames(2);
            yield return Tap(keyboard.escapeKey);
            yield return Tap(keyboard.rKey);
            yield return new WaitForSeconds(0.3f);
            Assert.Less(Vector3.Distance(here, player.transform.position), 0.15f, "시작 위치로 돌아갔는데 정한 자리가 아닙니다.");
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(90f, player.transform.eulerAngles.y)), 1f, "정한 쪽을 보고 있지 않습니다.");

            // 처음 자리로 되돌린다.
            yield return OpenInfo(MapLibrary.DefaultId);
            Find<Button>(ui.MapInfo, "ClearSpawn").onClick.Invoke();
            yield return Frames(2);
            Assert.IsFalse(autoSave.HasSpawn);
            Assert.AreEqual(PlayerSpawn.ClearedMessage, notice.Message);
            Assert.AreEqual(MapInfoView.DefaultSpawnText, ui.MapInfo.SpawnText);
            Assert.Less(Vector3.Distance(DefaultSpawn, marker.Position), 0.05f);

            player.Motor.Respawn();
            yield return new WaitForSeconds(0.3f);
            Assert.Less(Vector3.Distance(DefaultSpawn, player.transform.position), 0.15f);
        }

        [UnityTest]
        public IEnumerator 다시_켜면_정해_둔_시작_위치에서_시작한다()
        {
            yield return LoadInfoScene();
            // 씬의 블록(집, 단, 계단)에 닿지 않는 빈 바닥.
            var here = new Vector3(-1f, 0f, -3f);
            Assert.IsTrue(autoSave.SetSpawn(here, 135f));

            yield return LoadInfoScene();
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(autoSave.HasSpawn);
            Assert.Less(Vector3.Distance(here, player.transform.position), 0.15f, "정해 둔 시작 위치에서 시작하지 않았습니다.");
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(135f, player.transform.eulerAngles.y)), 1f);
            Assert.Less(Vector3.Distance(here, marker.Position), 0.05f);
            Assert.IsTrue(player.Motor.HasCustomSpawn);
        }

        [UnityTest]
        public IEnumerator 맵마다_시작_위치가_다르다()
        {
            yield return LoadInfoScene();
            var here = new Vector3(3f, 0f, -2f);
            Assert.IsTrue(autoSave.SetSpawn(here, 0f));

            // 새 맵에는 정한 시작 위치가 없으므로 처음 자리에서 시작한다.
            string created = autoSave.CreateNew("둘째");
            yield return new WaitForSeconds(0.3f);
            Assert.IsNotNull(created);
            Assert.IsFalse(autoSave.HasSpawn);
            Assert.Less(Vector3.Distance(DefaultSpawn, player.transform.position), 0.15f, "새 맵은 처음 자리에서 시작해야 합니다.");
            Assert.Less(Vector3.Distance(DefaultSpawn, marker.Position), 0.05f);
            Assert.IsFalse(player.Motor.HasCustomSpawn);

            // 처음의 맵으로 돌아가면 그 맵에 정해 둔 자리에 선다.
            Assert.IsTrue(autoSave.Open(MapLibrary.DefaultId));
            yield return new WaitForSeconds(0.3f);
            Assert.Less(Vector3.Distance(here, player.transform.position), 0.15f, "맵에 정해 둔 시작 위치로 가지 않았습니다.");
            Assert.Less(Vector3.Distance(here, marker.Position), 0.05f);
        }

        [UnityTest]
        public IEnumerator 시작_위치가_블록으로_막혀_있으면_그_위에_선다()
        {
            yield return LoadInfoScene();
            var here = new Vector3(-2.5f, 0f, -3.5f);
            Assert.IsTrue(autoSave.SetSpawn(here, 0f));

            // 시작 위치에 블록을 두 개 쌓는다.
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(0, CellCenter(-3, 0, -4)));
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(0, CellCenter(-3, 1, -4)));
            yield return new WaitForFixedUpdate();
            yield return Frames(2);

            player.Motor.Respawn();
            yield return new WaitForSeconds(0.4f);

            Vector3 position = player.transform.position;
            Assert.Less(new Vector2(position.x - here.x, position.z - here.z).magnitude, 0.1f, "시작 위치에서 옆으로 밀려났습니다.");
            Assert.AreEqual(2f, position.y, 0.15f, "블록 속이 아니라 쌓인 블록의 위에 서야 합니다.");
        }

        [UnityTest]
        public IEnumerator 맵의_범위_밖에서는_시작_위치를_정할_수_없다()
        {
            yield return LoadInfoScene();
            Teleport(new Vector3(20f, 0f, 0f), 0f);

            Assert.IsFalse(spawn.SetHere());

            Assert.IsFalse(autoSave.HasSpawn);
            Assert.AreEqual(PlayerSpawn.OutsideMessage, notice.Message);
            Assert.Less(Vector3.Distance(DefaultSpawn, marker.Position), 0.05f);
        }

        [UnityTest]
        public IEnumerator 공중에서_정하면_아래의_바닥이나_블록_위가_시작_위치가_된다()
        {
            yield return LoadInfoScene();
            player.Motor.SetFlying(true);
            Teleport(new Vector3(1.5f, 4f, -3.5f), 0f);

            Assert.IsTrue(spawn.SetHere());
            Assert.AreEqual(0f, autoSave.SpawnPosition.y, 0.02f, "공중에서 정하면 아래의 바닥이 시작 위치가 되어야 합니다.");
            Assert.AreEqual(1.5f, autoSave.SpawnPosition.x, 0.02f);

            // 아래에 블록이 있으면 그 블록의 위가 된다.
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(0, CellCenter(1, 0, -4)));
            yield return new WaitForFixedUpdate();
            yield return Frames(2);
            Teleport(new Vector3(1.5f, 4f, -3.5f), 0f);
            Assert.IsTrue(spawn.SetHere());
            Assert.AreEqual(1f, autoSave.SpawnPosition.y, 0.02f);
        }

        [UnityTest]
        public IEnumerator 설명과_시작_위치가_없던_옛_판의_파일도_읽고_사본을_남긴_뒤_새_판으로_저장한다()
        {
            PrepareMapDirectory();
            Directory.CreateDirectory(MapStorage.Directory);
            string old = "{\"format\":\"atelier-verse-map\",\"version\":2,\"name\":\"둘째 판의 맵\",\"createdAt\":\"2026-10-09T01:00:00Z\",\"updatedAt\":\"2026-10-09T02:00:00Z\","
                + "\"bounds\":{\"min\":{\"x\":-8.0,\"y\":0.0,\"z\":-8.0},\"max\":{\"x\":8.0,\"y\":12.0,\"z\":8.0}},"
                + "\"blocks\":[{\"id\":3,\"part\":\"block.gold\",\"position\":{\"x\":2.37,\"y\":0.5,\"z\":-2.82},\"rotation\":{\"x\":0.0,\"y\":30.0,\"z\":0.0}},"
                + "{\"id\":7,\"part\":\"slab.blue\",\"position\":{\"x\":-1.5,\"y\":0.25,\"z\":-2.5},\"rotation\":{\"x\":0.0,\"y\":0.0,\"z\":0.0}}],"
                + "\"assemblies\":[],\"rules\":[]}";
            File.WriteAllText(MapStorage.LocalPath, old);

            yield return LoadInfoScene();

            Assert.AreEqual(2, world.Count, "옛 판의 블록을 모두 읽어야 합니다.");
            Assert.AreEqual(0, autoSave.LastLoad.Skipped);
            Assert.IsTrue(world.TryGet(3, out BlockRecord gold), "블록의 번호가 그대로여야 합니다.");
            Assert.Less(Vector3.Distance(new Vector3(2.37f, 0.5f, -2.82f), gold.Position), 0.002f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 30f, 0f), gold.Rotation), 0.1f);
            Assert.IsTrue(world.TryGet(7, out BlockRecord slab));
            Assert.AreEqual("slab.blue", world.Catalog.Get(slab.Part).id);
            Assert.AreEqual("둘째 판의 맵", autoSave.MapName);
            Assert.IsFalse(autoSave.HasSpawn);
            Assert.Less(Vector3.Distance(DefaultSpawn, player.transform.position), 0.15f);

            Assert.IsNotNull(autoSave.BackupPath, "옛 판의 파일을 남겨 두지 않았습니다.");
            StringAssert.EndsWith(".v2.bak", autoSave.BackupPath);
            Assert.AreEqual(old, File.ReadAllText(autoSave.BackupPath));

            yield return new WaitForSeconds(SaveWait);
            Assert.AreEqual(SaveState.Saved, autoSave.State, autoSave.Message);
            string saved = File.ReadAllText(autoSave.FilePath);
            StringAssert.Contains($"\"version\": {MapDocument.CurrentVersion}", saved);
            StringAssert.Contains("\"description\"", saved);
            StringAssert.Contains("\"spawn\"", saved);
            Assert.IsTrue(MapStorage.TryLoad(autoSave.FilePath, out MapDocument document, out MapFileError error), error.ToString());
            Assert.IsFalse(document.WasUpgraded);
            Assert.AreEqual(2, document.blocks.Count);
            Assert.AreEqual("2026-10-09T01:00:00Z", document.createdAt, "만든 시각은 옛 파일의 것을 이어받아야 합니다.");
            Assert.AreEqual(1, MapLibrary.List().Count, "사본을 맵으로 세면 안 됩니다.");
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadInfoScene();
            BeginCapture();

            // 처음의 맵: 집 쪽을 보고 대표 그림을 찍고, 설명과 시작 위치를 정한다.
            Teleport(new Vector3(-2.5f, 0f, -4.5f), 40f);
            player.SetLook(40f, 6f);
            yield return Frames(3);
            Assert.IsTrue(autoSave.UpdateInfo(autoSave.MapId, "시험 작업실", "집과 계단이 있는 처음의 맵"));
            Assert.IsTrue(spawn.SetHere());
            yield return OpenInfo(MapLibrary.DefaultId);
            Find<Button>(ui.MapInfo, "Snapshot").onClick.Invoke();
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "map-info.png"));

            // 둘째 맵: 탑을 쌓고 그림을 찍는다. 셋째 맵은 그림 없이 둔다.
            ui.CloseMapInfo();
            autoSave.CreateNew("언덕 위의 탑");
            for (int y = 0; y < 4; y++)
            {
                world.Add(1, CellCenter(0, y, -2));
            }

            world.Add(world.Catalog.IndexOf("wedge.clay"), CellCenter(0, 4, -2));
            player.SetLook(0f, -12f);
            yield return Frames(3);
            yield return OpenInfo(autoSave.MapId);
            Find<Button>(ui.MapInfo, "Snapshot").onClick.Invoke();
            ui.CloseMapInfo();
            autoSave.CreateNew();
            yield return Frames(3);

            ui.OpenMapList();
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "maps-list-thumbnails.png"));
            ui.CloseMapList();

            // 시작 위치의 표식: 처음의 맵으로 돌아가, 정해 둔 자리를 조금 떨어져서 내려다본다.
            Assert.IsTrue(autoSave.Open(MapLibrary.DefaultId));
            yield return new WaitForSeconds(0.3f);
            Teleport(new Vector3(-4.2f, 0f, -6.6f), 45f);
            player.SetLook(45f, 28f);
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "spawn-marker.png"));
        }

        /// <summary>캐릭터를 자리와 방향으로 바로 옮긴다. 걷지 않고 놓는 것이라 중간의 물체에 막히지 않는다.</summary>
        private void Teleport(Vector3 position, float yaw)
        {
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.position = position;
            body.enabled = true;
            player.SetLook(yaw, 0f);
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
            Assert.AreEqual(id, ui.MapInfo.MapId);
        }

        private IEnumerator LoadInfoScene()
        {
            yield return LoadSandbox();

            world = Object.FindAnyObjectByType<BlockWorld>();
            autoSave = Object.FindAnyObjectByType<MapAutoSave>();
            notice = Object.FindAnyObjectByType<NoticeBar>();
            spawn = player.GetComponent<PlayerSpawn>();
            marker = Object.FindAnyObjectByType<SpawnMarker>();
            Assert.IsNotNull(world, "Sandbox 씬에서 블록 세계를 찾을 수 없습니다.");
            Assert.IsNotNull(autoSave, "Sandbox 씬에서 자동 저장을 찾을 수 없습니다.");
            Assert.IsNotNull(notice, "게임 화면에 알림 띠가 없습니다.");
            Assert.IsNotNull(spawn, "캐릭터에 시작 위치를 다루는 부품이 없습니다.");
            Assert.IsNotNull(marker, "씬에 시작 위치 표식이 없습니다.");
            Assert.IsNotNull(ui.MapInfo, "게임 화면에 맵 정보 창이 없습니다.");
            yield return Frames(2);
        }
    }
}
