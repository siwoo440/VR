using System.Collections;
using System.IO;
using AtelierVerse.Player;
using AtelierVerse.UI;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// VR에서 부품 고르는 창과 모양이 다른 부품을 가상 기기로 실행해 확인한다(16일차): 왼손 부품 판의 단추로 창을 열고,
    /// 오른손 광선으로 부품을 가리켜 눌러 칸에 넣는다. 창의 크기와 글자가 헤드셋에서 읽히는지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class XrPartsPlayTests : XrPlayTestBase
    {
        private const int FirstEmptySlot = 6;

        // 캐릭터가 처음 서는 자리(0.5, 0, -5.5)의 3m 앞 빈 바닥의 한 점.
        private static readonly Vector3 FloorPoint = new Vector3(0.5f, 0f, -2.5f);

        private BlockWorld world;
        private BlockBuilder builder;

        [UnityTest]
        public IEnumerator 부품_판의_부품_창_단추로_창을_열고_부품을_가리켜_눌러_칸에_넣는다()
        {
            yield return LoadVrBuild();
            int before = world.Count;
            int slab = Part("slab.gold");

            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Palette, "Parts")));
            Assert.IsTrue(ui.Player.IsPointingAtUi, "부품 판의 단추를 가리키고 있는데 화면을 가리킨다고 알지 못합니다.");
            yield return PullTrigger();

            Assert.IsTrue(ui.IsPickerOpen, "부품 판의 부품 창 단추로 창이 열리지 않았습니다.");
            Assert.AreEqual(FirstEmptySlot, ui.Picker.TargetSlot);
            Assert.IsFalse(ui.Palette.IsShown, "창이 열려 있는 동안에는 부품 판을 감춥니다.");
            Assert.IsFalse(ui.XrPanel.Follow, "창은 메뉴처럼 열린 자리에 머물러야 가리켜 누를 수 있습니다.");
            Assert.IsTrue(xrControl.InputBlocked, "창이 열려 있는 동안에는 걷기와 돌기를 막아야 합니다.");
            yield return Frames(3);

            Button cell = ui.Picker.FindCell(slab);
            yield return PointRightHandAt(CenterOf(cell));
            Assert.IsTrue(ui.Player.IsPointingAtUi, "창의 부품을 가리키고 있는데 화면을 가리킨다고 알지 못합니다.");
            Assert.AreEqual(Vector3.Distance(rig.RightPointer.position, CenterOf(cell)), rig.PointerLength, 0.02f, "광선이 가리킨 부품에서 끝나지 않았습니다.");
            yield return PullTrigger();

            Assert.IsFalse(ui.IsPickerOpen, "부품을 누르면 창이 닫혀야 합니다.");
            Assert.AreEqual(slab, ui.Hotbar.Model.GetPart(FirstEmptySlot));
            Assert.AreEqual(FirstEmptySlot, ui.Hotbar.SelectedIndex);
            Assert.AreEqual(slab, builder.SelectedPart);
            Assert.AreEqual(before, world.Count, "창을 누른 방아쇠로 블록이 놓였습니다.");
            Assert.IsFalse(xrControl.InputBlocked);

            yield return Frames(3);
            Assert.IsTrue(ui.Palette.IsShown, "창을 닫으면 부품 판이 다시 보여야 합니다.");
            Assert.AreEqual("골드 판", ui.Palette.Title);
            Assert.AreEqual(slab, ui.Palette.Hotbar.Model.GetPart(FirstEmptySlot), "부품 판의 칸도 같은 부품을 보여야 합니다.");
        }

        [UnityTest]
        public IEnumerator 부품_판의_빈_칸을_누르면_그_칸에_넣도록_창이_열린다()
        {
            yield return LoadVrBuild();

            yield return PointRightHandAt(CenterOf(Find<Button>(ui.Palette, "Slot9")));
            yield return PullTrigger();

            Assert.IsTrue(ui.IsPickerOpen, "빈 칸을 눌렀는데 창이 열리지 않았습니다.");
            Assert.AreEqual(8, ui.Picker.TargetSlot);
            Assert.AreEqual("9번 칸 · 비어 있음", ui.Picker.TargetText);
            Assert.AreEqual(HotbarModel.None, ui.Hotbar.SelectedIndex);
        }

        [UnityTest]
        public IEnumerator 왼손의_메뉴_단추는_열려_있는_창을_닫고_메뉴는_열지_않는다()
        {
            yield return LoadVrBuild();
            ui.OpenPicker();
            yield return Frames(2);
            Assert.IsTrue(ui.IsPickerOpen);

            yield return Tap(Button(leftController, "menu"));
            yield return Frames(2);

            Assert.IsFalse(ui.IsPickerOpen);
            Assert.IsFalse(ui.IsMenuOpen, "창을 닫는 단추로 메뉴가 열렸습니다.");
            Assert.IsTrue(ui.XrPanel.Follow, "창을 닫으면 판이 다시 머리를 따라와야 합니다.");
            Assert.IsTrue(ui.Palette.IsShown);
        }

        [UnityTest]
        public IEnumerator VR에서도_판은_반_높이로_놓이고_블록_위에_맞춰_쌓인다()
        {
            yield return LoadVrBuild();
            int slab = Part("slab.blue");
            ui.Hotbar.Model.SetPart(FirstEmptySlot, slab);
            ui.Hotbar.Select(FirstEmptySlot);

            yield return PointRightHandAt(FloorPoint);
            Assert.IsTrue(builder.HasTarget);
            Assert.Less(Vector3.Distance(FloorPoint + Vector3.up * 0.25f, builder.TargetPosition), 0.01f, "판이 바닥에 반 높이로 얹히지 않았습니다.");
            Assert.AreSame(world.Catalog.Get(slab).mesh, GameObject.Find("BlockGhost").GetComponent<MeshFilter>().sharedMesh);

            yield return PullTrigger();
            Assert.IsTrue(BlockNear(world, FloorPoint + Vector3.up * 0.25f, out BlockRecord placed, 0.01f));
            Assert.AreEqual(slab, placed.Part);

            // 놓은 판의 윗면을 가리키면 그 위에 딱 붙어 쌓인다(맞추기 1칸).
            builder.SetSnapLevel(1);
            yield return new WaitForFixedUpdate();
            yield return Frames(2);
            yield return PointRightHandAt(placed.Position + new Vector3(0.1f, 0.25f, 0.1f));
            Assert.IsTrue(builder.HasBlockTarget, "놓은 판의 윗면을 가리키고 있어야 합니다.");
            Assert.Less(Vector3.Distance(placed.Position + Vector3.up * 0.5f, builder.TargetPosition), 0.002f, "판 위에 판이 딱 붙지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadVrBuild();
            BeginCapture();

            Set(leftController.devicePosition, new Vector3(-0.24f, 1.2f, 0.3f));
            Set(leftController.deviceRotation, Quaternion.Euler(-25f, 10f, 0f));
            ui.Hotbar.Model.SetPart(6, Part("stairs.gold"));
            ui.Hotbar.Model.SetPart(7, Part("slab.blue"));
            yield return Frames(3);

            ui.OpenPickerFor(8);
            yield return Frames(3);
            yield return PointRightHandAt(CenterOf(ui.Picker.FindCell(Part("wedge.leaf"))));
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "vr-parts-picker.png"));

            // 글자를 확인할 수 있게 판만 가까이 당겨 찍은 그림. 헤드셋에서 보이는 크기가 아니다.
            Camera view = player.Rig.ViewCamera;
            float fieldOfView = view.fieldOfView;
            view.fieldOfView = 44f;
            SaveCapture(Path.Combine(directory, "vr-parts-picker-close.png"));
            view.fieldOfView = fieldOfView;
        }

        private int Part(string id)
        {
            int index = world.Catalog.IndexOf(id);
            Assert.GreaterOrEqual(index, 0, $"부품 목록에 {id}이(가) 없습니다.");
            return index;
        }

        private IEnumerator LoadVrBuild()
        {
            yield return LoadVr();
            builder = player.GetComponent<BlockBuilder>();
            world = Object.FindAnyObjectByType<BlockWorld>();
            Assert.IsNotNull(builder);
            Assert.IsNotNull(world);

            Set(headset.centerEyePosition, new Vector3(0f, 1.6f, 0f));
            yield return RaiseLeftHand();
        }
    }
}
