using System.Collections;
using System.IO;
using AtelierVerse.Core;
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
    /// 모양이 다른 부품과 부품 고르는 창을 실제로 실행해 확인한다(16일차). 캐릭터는 (0.5, 0, -5.5)에서 +z 쪽을 보고 시작한다.
    /// 창의 단추는 누름 동작을 직접 부른다. 실제 마우스로 누르는 느낌과 부품의 모양이 보기 좋은지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class PartsPlayTests : PlayTestBase
    {
        private const int SceneBlockCount = 19;
        private const int GoldBlock = 0;
        private const int BlueBlock = 1;
        private const int FirstEmptySlot = 6;

        // 45도 아래를 보고 놓은 블록이 놓이는 자리쯤(칸의 가운데가 아니다).
        private static readonly Vector3 FloorSpot = AimedFloorSpot;

        private BlockWorld world;
        private BlockBuilder builder;
        private NoticeBar notice;
        private float highest;

        [UnityTest]
        public IEnumerator B로_부품_고르는_창을_열고_닫으며_열려_있는_동안_조작이_막힌다()
        {
            yield return LoadPartsScene();
            player.CaptureLook(true);
            yield return Frames(2);
            Assert.IsFalse(ui.IsPickerOpen);

            yield return Tap(keyboard.bKey);
            Assert.IsTrue(ui.IsPickerOpen, "B로 부품 고르는 창이 열리지 않았습니다.");
            Assert.IsTrue(ui.IsModalOpen);
            Assert.IsFalse(ui.IsMenuOpen);
            Assert.IsTrue(player.InputBlocked, "창이 열려 있는 동안에는 캐릭터 조작을 막아야 합니다.");
            Assert.IsFalse(player.LookCaptured, "창을 누를 수 있게 마우스를 놓아야 합니다.");

            yield return Tap(keyboard.bKey);
            Assert.IsFalse(ui.IsPickerOpen, "B를 다시 눌러도 창이 닫히지 않았습니다.");
            Assert.IsFalse(player.InputBlocked);
            Assert.IsTrue(player.LookCaptured, "창을 닫으면 바로 시점 조작으로 돌아가야 합니다.");
        }

        [UnityTest]
        public IEnumerator Esc는_부품_고르는_창을_닫고_메뉴는_열지_않는다()
        {
            yield return LoadPartsScene();
            player.CaptureLook(true);
            yield return Tap(keyboard.bKey);
            Assert.IsTrue(ui.IsPickerOpen);

            yield return Tap(keyboard.escapeKey);
            Assert.IsFalse(ui.IsPickerOpen);
            Assert.IsFalse(ui.IsMenuOpen, "창을 닫는 Esc로 메뉴가 열렸습니다.");
            Assert.IsFalse(player.InputBlocked);

            // 메뉴가 열려 있으면 부품 고르는 창은 열리지 않는다.
            yield return Tap(keyboard.escapeKey);
            Assert.IsTrue(ui.IsMenuOpen);
            yield return Tap(keyboard.bKey);
            Assert.IsFalse(ui.IsPickerOpen, "메뉴가 열려 있는데 부품 고르는 창이 열렸습니다.");
        }

        [UnityTest]
        public IEnumerator 창에는_부품_목록의_부품이_모두_있다()
        {
            yield return LoadPartsScene();
            PartCatalog catalog = world.Catalog;

            Assert.AreEqual(catalog.Count, ui.Picker.CellCount);
            for (int part = 0; part < catalog.Count; part++)
            {
                Assert.IsNotNull(ui.Picker.FindCell(part), $"창에 {catalog.Get(part).displayName}이(가) 없습니다.");
            }
        }

        [UnityTest]
        public IEnumerator 창에서_부품을_누르면_빈_칸에_들어가고_바로_골라진다()
        {
            yield return LoadPartsScene();
            int slab = Part("slab.gold");
            player.CaptureLook(true);

            yield return Tap(keyboard.bKey);
            Assert.AreEqual(FirstEmptySlot, ui.Picker.TargetSlot, "고른 칸이 없으면 가장 앞의 빈 칸에 넣어야 합니다.");
            Assert.AreEqual("7번 칸 · 비어 있음", ui.Picker.TargetText);

            ui.Picker.FindCell(slab).onClick.Invoke();
            yield return Frames(2);

            Assert.IsFalse(ui.IsPickerOpen, "부품을 누르면 창이 닫혀야 합니다.");
            Assert.AreEqual(slab, ui.Hotbar.Model.GetPart(FirstEmptySlot));
            Assert.AreEqual(FirstEmptySlot, ui.Hotbar.SelectedIndex, "넣은 칸이 바로 골라져야 합니다.");
            Assert.AreEqual("골드 판", ui.Hotbar.SelectedItemName);
            Assert.AreEqual(slab, builder.SelectedPart, "블록 놓기가 칸의 번호가 아니라 칸에 든 부품을 알아야 합니다.");
            Assert.IsTrue(Find<Transform>(ui, "BuildHint").gameObject.activeSelf);
            Assert.IsTrue(player.LookCaptured);

            // 일곱째 칸의 숫자 키로도 고르고 푼다.
            yield return Tap(keyboard.digit7Key);
            Assert.AreEqual(HotbarModel.None, ui.Hotbar.SelectedIndex);
            yield return Tap(keyboard.digit7Key);
            Assert.AreEqual(FirstEmptySlot, ui.Hotbar.SelectedIndex);
        }

        [UnityTest]
        public IEnumerator 고른_칸이_있으면_그_칸의_부품을_바꾼다()
        {
            yield return LoadPartsScene();
            int wedge = Part("wedge.clay");
            player.CaptureLook(true);
            yield return Tap(keyboard.digit2Key);
            Assert.AreEqual(BlueBlock, builder.SelectedPart);

            yield return Tap(keyboard.bKey);
            Assert.AreEqual(1, ui.Picker.TargetSlot);
            Assert.AreEqual("2번 칸 · 블루 블록", ui.Picker.TargetText);

            ui.Picker.FindCell(wedge).onClick.Invoke();
            yield return Frames(2);

            Assert.AreEqual(wedge, ui.Hotbar.Model.GetPart(1));
            Assert.AreEqual(1, ui.Hotbar.SelectedIndex, "칸의 부품을 바꿔도 그 칸이 고른 칸으로 남아야 합니다.");
            Assert.AreEqual(wedge, builder.SelectedPart);
            Assert.AreEqual("테라코타 경사", ui.Hotbar.SelectedItemName);
        }

        [UnityTest]
        public IEnumerator 창이_열려_있을_때_숫자_키와_칸_단추로_넣을_칸을_바꾸고_칸을_비운다()
        {
            yield return LoadPartsScene();
            player.CaptureLook(true);
            yield return Tap(keyboard.bKey);

            yield return Tap(keyboard.digit8Key);
            Assert.AreEqual(7, ui.Picker.TargetSlot, "창이 열려 있을 때 숫자 키는 넣을 칸을 바꿉니다.");
            Assert.AreEqual(HotbarModel.None, ui.Hotbar.SelectedIndex, "창이 열려 있을 때 숫자 키로 부품이 골라지면 안 됩니다.");

            ui.Picker.GetTargetButton(3).onClick.Invoke();
            Assert.AreEqual(3, ui.Picker.TargetSlot);
            Assert.AreEqual("4번 칸 · 잎 블록", ui.Picker.TargetText);

            Find<Button>(ui.Picker, "ClearSlot").onClick.Invoke();
            Assert.IsFalse(ui.Hotbar.Model.IsFilled(3), "칸 비우기가 칸을 비우지 않았습니다.");
            Assert.AreEqual("4번 칸 · 비어 있음", ui.Picker.TargetText);
            Assert.IsTrue(ui.IsPickerOpen, "칸을 비워도 창은 열려 있어야 합니다.");

            Find<Button>(ui.Picker, "ClosePicker").onClick.Invoke();
            Assert.IsFalse(ui.IsPickerOpen);

            // 비운 칸은 숫자 키로 고를 수 없다.
            yield return Tap(keyboard.digit4Key);
            Assert.AreEqual(HotbarModel.None, ui.Hotbar.SelectedIndex);
        }

        [UnityTest]
        public IEnumerator 부품_칸의_배치는_이_기기에_저장되어_다시_열어도_남는다()
        {
            yield return LoadPartsScene();
            Assert.AreEqual(string.Empty, GameSettings.HotbarParts, "아무것도 바꾸지 않았으면 저장한 배치가 없어야 합니다.");
            int stairs = Part("stairs.wood");

            ui.Hotbar.Model.SetPart(8, stairs);
            ui.Hotbar.Model.SetPart(0, HotbarModel.None);
            StringAssert.EndsWith(",stairs.wood", GameSettings.HotbarParts);
            StringAssert.StartsWith(",block.blue,", GameSettings.HotbarParts);

            yield return LoadPartsScene();

            Assert.AreEqual(stairs, ui.Hotbar.Model.GetPart(8), "저장한 배치가 다시 열었을 때 돌아오지 않았습니다.");
            Assert.IsFalse(ui.Hotbar.Model.IsFilled(0), "비운 칸은 비어 있어야 합니다.");
            Assert.AreEqual(BlueBlock, ui.Hotbar.Model.GetPart(1));
            Assert.AreEqual("나무 계단", ui.Hotbar.GetItemName(8));
        }

        [UnityTest]
        public IEnumerator 판은_바닥에_반_높이로_놓이고_모양과_충돌체가_맞는다()
        {
            yield return LoadPartsScene();
            int slab = Part("slab.blue");
            ui.Hotbar.Model.SetPart(FirstEmptySlot, slab);

            yield return AimWithPart(45f, keyboard.digit7Key);
            Assert.IsTrue(builder.HasTarget);
            Vector3 target = builder.TargetPosition;
            Assert.AreEqual(0.25f, target.y, 0.001f, "판을 바닥에 얹으면 가운데 높이는 반 높이의 반(0.25)이어야 합니다.");
            Assert.IsTrue(builder.CanPlaceAtTarget);

            PartCatalog.Part part = world.Catalog.Get(slab);
            GameObject ghost = GameObject.Find("BlockGhost");
            Assert.IsNotNull(ghost);
            Assert.AreSame(part.mesh, ghost.GetComponent<MeshFilter>().sharedMesh, "미리 보기가 판의 모양이 아닙니다.");

            yield return Tap(mouse.leftButton);

            Assert.IsTrue(BlockNear(world, target, out BlockRecord record, 0.002f), "판이 놓이지 않았습니다.");
            Assert.AreEqual(slab, record.Part);

            PlacedBlock view = FindViewNear(world, target, 0.002f);
            Assert.IsNotNull(view);
            Assert.AreSame(part.mesh, view.GetComponent<MeshFilter>().sharedMesh, "놓인 블록이 판의 모양이 아닙니다.");
            Assert.AreSame(part.material, view.GetComponent<Renderer>().sharedMaterial);

            var box = view.GetComponent<BoxCollider>();
            Assert.IsTrue(box != null && box.enabled, "판은 상자 충돌체를 써야 합니다.");
            Assert.Less(Vector3.Distance(new Vector3(1f, 0.5f, 1f), box.size), 0.001f, "충돌체가 판의 크기가 아닙니다.");
        }

        [UnityTest]
        public IEnumerator 블록_위를_가리키면_판이_그_위에_얹히고_맞추기를_켜면_딱_붙는다()
        {
            yield return LoadPartsScene();
            int slab = Part("slab.gold");
            ui.Hotbar.Model.SetPart(FirstEmptySlot, slab);

            // 가까이 놓인 블록. 30도 아래를 보면 윗면을 가리킨다.
            var block = new Vector3(0.5f, 0.5f, -4.2f);
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(BlueBlock, block));
            yield return new WaitForFixedUpdate();
            yield return Frames(2);

            yield return AimWithPart(30f, keyboard.digit7Key);
            Assert.IsTrue(builder.HasBlockTarget, "놓아 둔 블록을 가리키고 있어야 합니다.");
            Assert.AreEqual(1.25f, builder.TargetPosition.y, 0.002f, "판의 아랫면이 블록의 윗면(높이 1)에 닿아야 합니다.");

            yield return Tap(keyboard.cKey);
            yield return Frames(2);
            Assert.Less(Vector3.Distance(block + Vector3.up * 0.75f, builder.TargetPosition), 0.002f, "맞추기를 켜면 블록의 바로 위에 나란히 얹혀야 합니다.");

            yield return Tap(mouse.leftButton);
            Assert.IsTrue(BlockNear(world, block + Vector3.up * 0.75f, out BlockRecord record, 0.002f));
            Assert.AreEqual(slab, record.Part);
        }

        [UnityTest]
        public IEnumerator 경사와_계단은_메시_충돌체를_쓰고_비스듬한_면과_단이_조준에_걸린다()
        {
            yield return LoadPartsScene();
            int wedge = Part("wedge.leaf");
            int stairs = Part("stairs.ivory");
            Vector3 wedgeSpot = CellCenter(3, 0, -4);
            Vector3 stairsSpot = CellCenter(5, 0, -4);

            Assert.AreEqual(PlaceResult.Ok, world.History.Place(wedge, wedgeSpot));
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(stairs, stairsSpot));
            yield return new WaitForFixedUpdate();
            yield return Frames(2);

            PlacedBlock wedgeView = FindViewNear(world, wedgeSpot, 0.01f);
            Assert.IsNotNull(wedgeView);
            Assert.AreSame(world.Catalog.Get(wedge).mesh, wedgeView.GetComponent<MeshFilter>().sharedMesh);
            var meshCollider = wedgeView.GetComponent<MeshCollider>();
            Assert.IsTrue(meshCollider != null && meshCollider.enabled, "경사는 메시 충돌체를 써야 합니다.");
            Assert.IsFalse(wedgeView.GetComponent<BoxCollider>().enabled, "경사에 상자 충돌체가 남아 있으면 비스듬한 면 위의 빈 곳이 막힙니다.");

            // 경사의 가운데를 위에서 내려다보면 반 높이의 비스듬한 면에 닿는다.
            Assert.IsTrue(Physics.Raycast(wedgeSpot + Vector3.up * 3f, Vector3.down, out RaycastHit slope, 5f));
            Assert.AreSame(wedgeView.gameObject, slope.collider.gameObject);
            Assert.AreEqual(0.5f, slope.point.y, 0.01f, "경사의 가운데 높이는 0.5여야 합니다.");
            Assert.AreEqual(0.7071f, slope.normal.y, 0.01f, "경사면은 45도여야 합니다.");
            Assert.Less(slope.normal.z, -0.5f, "경사는 +z 쪽으로 오릅니다(면은 -z 쪽을 봅니다).");

            // 계단의 첫 단과 마지막 단.
            Assert.IsTrue(Physics.Raycast(stairsSpot + new Vector3(0f, 3f, -0.375f), Vector3.down, out RaycastHit first, 5f));
            Assert.AreEqual(0.25f, first.point.y, 0.01f, "첫 단의 높이는 0.25여야 합니다.");
            Assert.IsTrue(Physics.Raycast(stairsSpot + new Vector3(0f, 3f, 0.375f), Vector3.down, out RaycastHit last, 5f));
            Assert.AreEqual(1f, last.point.y, 0.01f, "마지막 단의 높이는 1이어야 합니다.");
        }

        [UnityTest]
        public IEnumerator 계단을_걸어_오를_수_있다()
        {
            yield return LoadPartsScene();

            // 캐릭터의 바로 앞에 계단을 놓고 그 뒤에 블록을 둔다. 앞으로 걸으면 계단을 올라 블록 위에 선다.
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(Part("stairs.wood"), CellCenter(0, 0, -4)));
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(GoldBlock, CellCenter(0, 0, -3)));
            yield return new WaitForFixedUpdate();
            yield return Frames(2);

            yield return WalkForward(1.1f);
            Assert.Greater(highest, 0.9f, "계단을 걸어 오르지 못했습니다.");
        }

        [UnityTest]
        public IEnumerator 경사를_걸어_오를_수_있다()
        {
            yield return LoadPartsScene();

            Assert.AreEqual(PlaceResult.Ok, world.History.Place(Part("wedge.wood"), CellCenter(0, 0, -4)));
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(GoldBlock, CellCenter(0, 0, -3)));
            yield return new WaitForFixedUpdate();
            yield return Frames(2);

            yield return WalkForward(1.3f);
            Assert.Greater(highest, 0.9f, "경사를 걸어 오르지 못했습니다.");
        }

        [UnityTest]
        public IEnumerator 칠하면_색만_바뀌고_모양은_남는다()
        {
            yield return LoadPartsScene();
            int blueSlab = Part("slab.blue");
            int goldSlab = Part("slab.gold");

            // 판의 앞면이 45도 아래로 보는 자리에 오게 놓는다.
            var spot = new Vector3(0.5f, 0.25f, -3.6f);
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(blueSlab, spot));
            yield return new WaitForFixedUpdate();
            yield return Frames(2);

            yield return AimWithPart(45f, keyboard.digit1Key);
            Assert.IsTrue(builder.HasBlockTarget, "놓아 둔 판을 가리키고 있어야 합니다.");

            yield return Tap(mouse.middleButton);
            Assert.IsTrue(BlockNear(world, spot, out BlockRecord painted, 0.002f));
            Assert.AreEqual(goldSlab, painted.Part, "골드 블록으로 칠한 판은 골드 판이 되어야 합니다.");
            PlacedBlock view = FindViewNear(world, spot, 0.002f);
            Assert.AreSame(world.Catalog.Get(goldSlab).mesh, view.GetComponent<MeshFilter>().sharedMesh, "칠했더니 모양이 바뀌었습니다.");
            Assert.AreSame(world.Catalog.Get(goldSlab).material, view.GetComponent<Renderer>().sharedMaterial);

            yield return Tap(mouse.middleButton);
            Assert.IsTrue(notice.IsVisible);
            Assert.AreEqual(BlockBuilder.SamePartMessage, notice.Message, "이미 같은 색이면 알려야 합니다.");
            Assert.AreEqual(2, world.History.UndoCount);

            yield return TapWithCtrl(keyboard.zKey);
            Assert.IsTrue(BlockNear(world, spot, out BlockRecord restored, 0.002f));
            Assert.AreEqual(blueSlab, restored.Part);
        }

        [UnityTest]
        public IEnumerator 판을_잡아_옮겨도_판이고_미리_보기도_판이다()
        {
            yield return LoadPartsScene();
            int slab = Part("slab.clay");
            var spot = new Vector3(0.5f, 0.25f, -3.6f);
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(slab, spot, Quaternion.identity, out int id));
            yield return new WaitForFixedUpdate();
            yield return Frames(2);

            yield return AimWithPart(45f, keyboard.digit1Key);
            yield return Tap(keyboard.gKey);
            Assert.IsTrue(builder.IsCarrying);
            Assert.AreEqual(slab, builder.TargetPart, "잡은 블록의 부품으로 자리를 잡아야 합니다.");

            player.SetLook(25f, 45f);
            yield return Frames(3);
            Assert.AreEqual(0.25f, builder.TargetPosition.y, 0.001f, "잡은 판은 판의 높이로 바닥에 얹혀야 합니다(고른 부품은 블록이다).");
            Assert.AreSame(world.Catalog.Get(slab).mesh, GameObject.Find("BlockGhost").GetComponent<MeshFilter>().sharedMesh);
            Vector3 to = builder.TargetPosition;

            yield return Tap(mouse.leftButton);
            Assert.IsTrue(world.TryGet(id, out BlockRecord moved));
            Assert.AreEqual(slab, moved.Part);
            Assert.Less(Vector3.Distance(to, moved.Position), 0.002f);

            // 놓고 나면 미리 보기는 다시 고른 부품(블록)의 모양이다.
            yield return Frames(2);
            Assert.AreSame(world.Catalog.Get(GoldBlock).mesh, GameObject.Find("BlockGhost").GetComponent<MeshFilter>().sharedMesh);
        }

        [UnityTest]
        public IEnumerator 모양이_다른_부품도_맵_문서로_내보냈다_다시_읽으면_같다()
        {
            yield return LoadPartsScene();
            var wedgeSpot = new Vector3(2.4f, 0.5f, -3.3f);
            var slabSpot = new Vector3(-2.1f, 0.25f, -3.7f);
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(Part("wedge.clay"), wedgeSpot, Quaternion.Euler(0f, 90f, 0f), out _));
            Assert.AreEqual(PlaceResult.Ok, world.History.Place(Part("slab.ivory"), slabSpot));

            MapDocument document = world.Export(null);
            Assert.AreEqual(MapDocument.CurrentVersion, document.version, "모양이 다른 부품은 저장용 이름만 새로 생기므로 형식의 판은 그대로입니다.");
            MapBlock savedWedge = document.blocks.Find(block => Vector3.Distance(block.position, wedgeSpot) < 0.002f);
            MapBlock savedSlab = document.blocks.Find(block => Vector3.Distance(block.position, slabSpot) < 0.002f);
            Assert.AreEqual("wedge.clay", savedWedge.part);
            Assert.AreEqual(90f, savedWedge.rotation.y, 0.01f);
            Assert.AreEqual("slab.ivory", savedSlab.part);

            MapLoadReport report = world.Import(document);
            yield return Frames(2);

            Assert.AreEqual(SceneBlockCount + 2, world.Count, "읽지 못한 블록이 있습니다(판이 바닥 높이에서 범위 밖으로 보였을 수 있습니다).");
            Assert.IsTrue(BlockNear(world, slabSpot, out BlockRecord slab, 0.002f));
            Assert.AreEqual(Part("slab.ivory"), slab.Part);
            Assert.IsTrue(BlockNear(world, wedgeSpot, out BlockRecord wedge, 0.002f));
            Assert.AreEqual(Part("wedge.clay"), wedge.Part);
            Assert.AreSame(world.Catalog.Get(wedge.Part).mesh, FindViewNear(world, wedgeSpot, 0.002f).GetComponent<MeshFilter>().sharedMesh);
            Assert.IsNotNull(report);
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadPartsScene();
            BeginCapture();

            // 한눈에 들어오게 두 걸음 물러선다.
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.position = new Vector3(0.5f, 0f, -7.5f);
            body.enabled = true;

            // 왼쪽부터 블록, 판, 기둥, 경사, 계단을 색을 달리해 한 줄로 놓고(가운데는 미리 보기가 들어갈 자리로 비움), 그 뒤에 섞어 지은 작은 단을 둔다.
            string[] row = { "block.gold", "slab.blue", "pillar.clay", "wedge.leaf", "stairs.ivory" };
            float[] rowX = { -2.9f, -1.6f, 1.3f, 2.3f, 3.5f };
            for (int i = 0; i < row.Length; i++)
            {
                int part = Part(row[i]);
                Vector3 half = world.Catalog.HalfSizeOf(part);
                world.Add(part, new Vector3(rowX[i], half.y, -4f), Quaternion.identity, out _);
            }

            int wood = Part("block.wood");
            world.Add(Part("stairs.wood"), new Vector3(-0.5f, 0.5f, -2.5f), Quaternion.identity, out _);
            world.Add(wood, new Vector3(-0.5f, 0.5f, -1.5f), Quaternion.identity, out _);
            world.Add(wood, new Vector3(0.5f, 0.5f, -1.5f), Quaternion.identity, out _);
            world.Add(Part("slab.clay"), new Vector3(0.5f, 1.25f, -1.5f), Quaternion.identity, out _);
            world.Add(Part("pillar.ivory"), new Vector3(1.25f, 0.5f, -1.75f), Quaternion.identity, out _);
            world.Add(Part("wedge.gold"), new Vector3(1.5f, 0.5f, -2.5f), Quaternion.Euler(0f, 90f, 0f), out _);
            yield return new WaitForFixedUpdate();
            yield return Frames(2);

            // 계단을 일곱째 칸에 넣고 바닥을 가리킨 모습.
            ui.Hotbar.Model.SetPart(6, Part("stairs.gold"));
            ui.Hotbar.Model.SetPart(7, Part("slab.blue"));
            ui.Hotbar.Model.SetPart(8, Part("wedge.leaf"));
            player.CaptureLook(true);
            player.SetLook(-4f, 24f);
            yield return Tap(keyboard.digit7Key);
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "parts-shapes.png"));

            // 부품 고르는 창.
            yield return Tap(keyboard.bKey);
            yield return Frames(3);
            Assert.IsTrue(ui.IsPickerOpen);
            SaveCapture(Path.Combine(directory, "parts-picker.png"));
        }

        /// <summary>앞으로 가는 키를 누른 채 seconds초 동안 걷고, 그동안 발이 가장 높이 올라간 높이를 highest에 남긴다.</summary>
        private IEnumerator WalkForward(float seconds)
        {
            highest = player.transform.position.y;
            Press(keyboard.wKey);

            float until = Time.time + seconds;
            while (Time.time < until)
            {
                highest = Mathf.Max(highest, player.transform.position.y);
                yield return null;
            }

            Release(keyboard.wKey);
            yield return Frames(2);
        }

        private int Part(string id)
        {
            int index = world.Catalog.IndexOf(id);
            Assert.GreaterOrEqual(index, 0, $"부품 목록에 {id}이(가) 없습니다.");
            return index;
        }

        private IEnumerator LoadPartsScene()
        {
            yield return LoadSandbox();

            world = Object.FindAnyObjectByType<BlockWorld>();
            builder = Object.FindAnyObjectByType<BlockBuilder>();
            notice = Object.FindAnyObjectByType<NoticeBar>();
            Assert.IsNotNull(world, "Sandbox 씬에서 블록 세계를 찾을 수 없습니다.");
            Assert.IsNotNull(builder, "PC 캐릭터에 블록 놓기가 없습니다.");
            Assert.IsNotNull(notice, "게임 화면에 알림 띠가 없습니다.");
            Assert.IsNotNull(ui.Picker, "게임 화면에 부품 고르는 창이 없습니다.");
            yield return Frames(2);
        }
    }
}
