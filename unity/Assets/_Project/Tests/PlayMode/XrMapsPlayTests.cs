using System.Collections;
using System.IO;
using AtelierVerse.UI;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// VR에서 여러 맵 다루기를 가상 기기로 실행해 확인한다(17일차): 메뉴의 "내 작업실" 타일로 맵 목록 창을 열고,
    /// 오른손 광선으로 새 맵과 열기를 누른다. 맵 정보 창은 XrMapInfoPlayTests가 본다.
    /// 창의 크기와 글자가 헤드셋에서 읽히는지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class XrMapsPlayTests : XrPlayTestBase
    {
        private const int SceneBlockCount = 19;

        private BlockWorld world;
        private MapAutoSave autoSave;

        [UnityTest]
        public IEnumerator 메뉴의_내_작업실_타일을_가리켜_눌러_창을_열고_새_맵을_만든다()
        {
            yield return LoadVrMaps();
            yield return OpenMenuWithController();

            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Menu, "HomeTile")));
            yield return PullTrigger();

            Assert.IsTrue(ui.IsMapListOpen, "내 작업실 타일을 눌렀는데 맵 목록 창이 열리지 않았습니다.");
            Assert.IsFalse(ui.IsMenuOpen);
            Assert.IsFalse(ui.XrPanel.Follow, "창은 메뉴처럼 열린 자리에 머물러야 가리켜 누를 수 있습니다.");
            Assert.IsTrue(xrControl.InputBlocked);
            Assert.IsFalse(ui.Palette.IsShown, "창이 열려 있는 동안에는 부품 판을 감춥니다.");
            Assert.IsTrue(ui.MapList.GetRow(0).infoButton.gameObject.activeSelf, "정보 단추는 VR에서도 보입니다(시작 위치와 대표 그림을 정할 수 있다).");
            Assert.IsTrue(ui.MapList.GetRow(0).deleteButton.gameObject.activeSelf);
            yield return Frames(3);

            Button create = Find<Button>(ui.MapList, "CreateMap");
            yield return PointRightHandAt(CenterOf(create));
            Assert.IsTrue(ui.Player.IsPointingAtUi, "창의 단추를 가리키고 있는데 화면을 가리킨다고 알지 못합니다.");
            yield return PullTrigger();

            Assert.IsFalse(ui.IsMapListOpen, "새 맵을 만들면 창이 닫혀야 합니다.");
            Assert.AreNotEqual(MapLibrary.DefaultId, autoSave.MapId);
            Assert.AreEqual(MapLibrary.DefaultName, autoSave.MapName, "VR에서는 이름을 자동으로 붙입니다.");
            Assert.AreEqual(0, world.Count, "창을 누른 방아쇠로 블록이 놓였거나 새 맵이 비어 있지 않습니다.");
            Assert.IsFalse(xrControl.InputBlocked);
            Assert.IsTrue(ui.XrPanel.Follow, "창을 닫으면 판이 다시 머리를 따라와야 합니다.");

            yield return Frames(3);
            Assert.IsTrue(ui.Palette.IsShown);
            Assert.AreEqual($"블록 0/{world.MaxBlocks}", ui.Palette.BlockCountText, "부품 판의 블록 수가 새 맵의 것이 아닙니다.");
        }

        [UnityTest]
        public IEnumerator 맵_줄의_열기를_가리켜_눌러_다른_맵으로_간다()
        {
            yield return LoadVrMaps();
            string created = autoSave.CreateNew("탑");
            Assert.IsNotNull(created);
            Assert.AreEqual(0, world.Count);
            yield return Frames(3);

            ui.OpenMapList();
            yield return Frames(3);
            int row = ui.MapList.RowOf(MapLibrary.DefaultId);
            Assert.GreaterOrEqual(row, 0);

            yield return PointRightHandAt(CenterOf(ui.MapList.GetRow(row).openButton));
            yield return PullTrigger();

            Assert.AreEqual(MapLibrary.DefaultId, autoSave.MapId, "열기를 눌렀는데 맵이 바뀌지 않았습니다.");
            Assert.AreEqual(SceneBlockCount, world.Count);
            Assert.IsFalse(ui.IsMapListOpen);

            // 왼손의 메뉴 단추는 열려 있는 창을 닫는다.
            ui.OpenMapList();
            yield return Frames(2);
            yield return Tap(Button(leftController, "menu"));
            yield return Frames(2);
            Assert.IsFalse(ui.IsMapListOpen);
            Assert.IsFalse(ui.IsMenuOpen, "창을 닫는 단추로 메뉴가 열렸습니다.");
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadVrMaps();
            BeginCapture();

            Set(leftController.devicePosition, new Vector3(-0.24f, 1.2f, 0.3f));
            Set(leftController.deviceRotation, Quaternion.Euler(-25f, 10f, 0f));
            autoSave.CreateNew("언덕 위의 탑");
            world.Add(1, CellCenter(0, 0, -2));
            autoSave.SaveNow();
            autoSave.CreateNew();
            yield return Frames(3);

            ui.OpenMapList();
            yield return Frames(3);
            yield return PointRightHandAt(CenterOf(ui.MapList.GetRow(1).openButton));
            yield return Frames(3);

            // 글자를 확인할 수 있게 판만 가까이 당겨 찍은 그림. 헤드셋에서 보이는 크기가 아니다.
            Camera view = player.Rig.ViewCamera;
            float fieldOfView = view.fieldOfView;
            view.fieldOfView = 44f;
            SaveCapture(Path.Combine(directory, "vr-maps-list-close.png"));
            view.fieldOfView = fieldOfView;
        }

        private IEnumerator LoadVrMaps()
        {
            yield return LoadVr();
            world = Object.FindAnyObjectByType<BlockWorld>();
            autoSave = Object.FindAnyObjectByType<MapAutoSave>();
            Assert.IsNotNull(world);
            Assert.IsNotNull(autoSave);

            Set(headset.centerEyePosition, new Vector3(0f, 1.6f, 0f));
            yield return RaiseLeftHand();
        }
    }
}
