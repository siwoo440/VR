using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 화면을 누르는 입력의 검사. 누르는 입력은 이 프로젝트의 입력 자산(UI 묶음)에 있고, VR에서는 오른손만 가리키고 누른다.
    /// 게임 화면 프리팹이 그 입력에 이어져 있는지도 본다.
    /// </summary>
    public class UiInputTests
    {
        private const string InputPath = "Assets/_Project/Input/AtelierInput.inputactions";
        private const string GameUiPrefabPath = "Assets/_Project/Prefabs/GameUI.prefab";

        [Test]
        public void 화면을_누르는_입력은_마우스와_오른손의_방아쇠다()
        {
            string[] paths = Paths("UI/Click");

            CollectionAssert.Contains(paths, "<Mouse>/leftButton");
            CollectionAssert.Contains(paths, "<XRController>{RightHand}/{TriggerButton}");
            Assert.IsFalse(paths.Any(path => path.Contains("LeftHand")), "왼손의 방아쇠로는 화면을 누르지 않습니다.");
            CollectionAssert.Contains(Paths("UI/Point"), "<Mouse>/position");
        }

        [Test]
        public void 가리키는_자세는_오른손의_조준_자세를_쓴다()
        {
            CollectionAssert.AreEqual(new[] { "<XRController>{RightHand}/pointerPosition" }, Paths("UI/TrackedDevicePosition"));
            CollectionAssert.AreEqual(new[] { "<XRController>{RightHand}/pointerRotation" }, Paths("UI/TrackedDeviceOrientation"));

            // 보이는 광선(XrRig)과 누르는 광선(화면 입력)이 같은 자세를 읽어야 어긋나지 않는다.
            CollectionAssert.AreEqual(Paths("UI/TrackedDevicePosition"), Paths("XR/RightPointerPosition"));
            CollectionAssert.AreEqual(Paths("UI/TrackedDeviceOrientation"), Paths("XR/RightPointerRotation"));
        }

        [Test]
        public void VR에서는_왼손의_메뉴_단추와_둘째_단추로_메뉴를_연다()
        {
            string[] paths = Paths("Game/Menu");

            CollectionAssert.Contains(paths, "<Keyboard>/escape");
            CollectionAssert.Contains(paths, "<XRController>{LeftHand}/{MenuButton}");
            CollectionAssert.Contains(paths, "<XRController>{LeftHand}/{SecondaryButton}");
        }

        [Test]
        public void 게임_화면은_이_프로젝트의_입력_자산으로_누르고_손_광선_부품은_꺼_둔다()
        {
            InputActionAsset actions = LoadActions();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameUiPrefabPath);
            Assert.IsNotNull(prefab, "게임 화면 프리팹이 없습니다. 12일차 셋업을 실행하세요.");

            InputSystemUIInputModule module = prefab.GetComponentInChildren<InputSystemUIInputModule>(true);
            Assert.IsNotNull(module);
            Assert.AreSame(actions, module.actionsAsset, "화면 입력이 이 프로젝트의 입력 자산에 이어져 있지 않습니다.");
            Assert.AreEqual(actions.FindAction("UI/Point", true).id, module.point.action.id);
            Assert.AreEqual(actions.FindAction("UI/Click", true).id, module.leftClick.action.id);
            Assert.AreEqual(actions.FindAction("UI/TrackedDevicePosition", true).id, module.trackedDevicePosition.action.id);
            Assert.AreEqual(actions.FindAction("UI/TrackedDeviceOrientation", true).id, module.trackedDeviceOrientation.action.id);
            Assert.IsNull(module.move, "키보드로 옮겨 다니는 선택은 쓰지 않습니다.");

            TrackedDeviceRaycaster raycaster = prefab.GetComponentInChildren<TrackedDeviceRaycaster>(true);
            Assert.IsNotNull(raycaster, "캔버스에 손 광선 부품이 없습니다.");
            Assert.IsFalse(raycaster.enabled, "손 광선 부품은 VR일 때만 켭니다.");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, raycaster.GetComponent<Canvas>().renderMode, "프리팹은 PC의 방식(화면에 겹쳐 그리기)으로 저장합니다.");
        }

        private static InputActionAsset LoadActions()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            Assert.IsNotNull(actions, $"입력 자산이 없습니다: {InputPath}");
            return actions;
        }

        private static string[] Paths(string actionPath)
        {
            return LoadActions().FindAction(actionPath, true).bindings.Select(binding => binding.path).ToArray();
        }
    }
}
