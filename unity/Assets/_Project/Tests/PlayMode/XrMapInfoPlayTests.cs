using System.Collections;
using System.IO;
using AtelierVerse.Core;
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
    /// VR에서 맵 정보 창을 가상 기기로 실행해 확인한다(18일차): 줄의 정보를 가리켜 눌러 열고,
    /// 시작 위치 정하기와 대표 그림 찍기를 오른손 광선으로 누른다. VR에는 글자판이 없어 이름과 설명은 고칠 수 없다.
    /// 창의 크기와 글자가 헤드셋에서 읽히는지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class XrMapInfoPlayTests : XrPlayTestBase
    {
        private MapAutoSave autoSave;

        [UnityTest]
        public IEnumerator VR에서는_이름과_설명을_고칠_수_없고_시작_위치와_그림은_정할_수_있다()
        {
            yield return LoadVrInfo();
            ui.OpenMapList();
            yield return Frames(3);

            yield return PointRightHandAt(CenterOf(ui.MapList.GetRow(0).infoButton));
            yield return PullTrigger();
            Assert.IsTrue(ui.IsMapInfoOpen, "줄의 정보를 눌렀는데 맵 정보 창이 열리지 않았습니다.");
            Assert.IsFalse(ui.XrPanel.Follow, "창은 열린 자리에 머물러야 가리켜 누를 수 있습니다.");
            Assert.IsFalse(Find<TMP_InputField>(ui.MapInfo, "NameField").interactable, "VR에는 글자판이 없으므로 이름 칸을 잠급니다.");
            Assert.IsFalse(Find<TMP_InputField>(ui.MapInfo, "DescriptionField").interactable);
            Assert.IsFalse(Find<Button>(ui.MapInfo, "SaveInfo").gameObject.activeSelf, "고칠 수 없으므로 저장 단추를 감춥니다.");
            yield return Frames(3);

            // 잠긴 채로 저장을 불러도 바뀌지 않는다.
            string name = autoSave.MapName;
            ui.MapInfo.SetNameText("VR에서 바꾼 이름");
            ui.MapInfo.Save();
            Assert.AreEqual(name, autoSave.MapName);

            yield return PointRightHandAt(CenterOf(Find<Button>(ui.MapInfo, "SetSpawn")));
            Assert.IsTrue(ui.Player.IsPointingAtUi);
            yield return PullTrigger();
            Assert.IsTrue(autoSave.HasSpawn, "시작 위치 단추를 가리켜 눌렀는데 정해지지 않았습니다.");
            Assert.Less(Vector3.Distance(player.transform.position, autoSave.SpawnPosition), 0.1f);

            yield return PointRightHandAt(CenterOf(Find<Button>(ui.MapInfo, "Snapshot")));
            yield return PullTrigger();
            string path = MapLibrary.ThumbnailPathOf(autoSave.MapId);
            Assert.IsTrue(File.Exists(path), "대표 그림 단추를 가리켜 눌렀는데 그림 파일이 없습니다.");
            Assert.IsTrue(SceneSnapshot.IsPng(File.ReadAllBytes(path)));
            Assert.IsTrue(ui.MapInfo.HasThumbnail);

            // 왼손의 메뉴 단추로 목록으로 돌아가고, 한 번 더 누르면 닫힌다.
            yield return Tap(Button(leftController, "menu"));
            yield return Frames(2);
            Assert.IsTrue(ui.IsMapListOpen);
            yield return Tap(Button(leftController, "menu"));
            yield return Frames(2);
            Assert.IsFalse(ui.IsModalOpen);
            Assert.IsTrue(ui.XrPanel.Follow);
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadVrInfo();
            BeginCapture();

            Set(leftController.devicePosition, new Vector3(-0.24f, 1.2f, 0.3f));
            Set(leftController.deviceRotation, Quaternion.Euler(-25f, 10f, 0f));
            Set(headset.centerEyeRotation, Quaternion.Euler(4f, 0f, 0f));
            yield return Frames(3);
            autoSave.UpdateInfo(autoSave.MapId, autoSave.MapName, "집과 계단이 있는 처음의 맵");

            ui.OpenMapList();
            yield return Frames(3);
            ui.MapList.GetRow(0).infoButton.onClick.Invoke();
            yield return Frames(3);
            Find<Button>(ui.MapInfo, "Snapshot").onClick.Invoke();
            yield return PointRightHandAt(CenterOf(Find<Button>(ui.MapInfo, "SetSpawn")));
            yield return Frames(3);

            // 글자를 확인할 수 있게 판만 가까이 당겨 찍은 그림. 헤드셋에서 보이는 크기가 아니다.
            Camera view = player.Rig.ViewCamera;
            float fieldOfView = view.fieldOfView;
            view.fieldOfView = 44f;
            SaveCapture(Path.Combine(directory, "vr-map-info-close.png"));
            view.fieldOfView = fieldOfView;
        }

        private IEnumerator LoadVrInfo()
        {
            yield return LoadVr();
            autoSave = Object.FindAnyObjectByType<MapAutoSave>();
            Assert.IsNotNull(autoSave);
            Assert.IsNotNull(ui.MapInfo, "게임 화면에 맵 정보 창이 없습니다.");

            Set(headset.centerEyePosition, new Vector3(0f, 1.6f, 0f));
            yield return RaiseLeftHand();
        }
    }
}
