using System.Collections;
using System.Collections.Generic;
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
    /// 기본 소리를 실제로 실행해 확인한다(19일차). 소리를 내 달라는 요청(Sfx)을 받아 적어, 맞는 때에 맞는 소리가 맞는 자리에서 올라오는지를 본다.
    /// 소리가 실제로 스피커에서 나는지와 듣기 좋은지는 이 테스트로 확인되지 않는다.
    /// </summary>
    public class SoundPlayTests : PlayTestBase
    {
        private const int BluePart = 1;

        private readonly List<(SfxId id, Vector3? position)> heard = new List<(SfxId id, Vector3? position)>();

        private BlockWorld world;
        private BlockBuilder builder;

        public override void Setup()
        {
            base.Setup();
            heard.Clear();
            Sfx.Played += Listen;
        }

        public override void TearDown()
        {
            Sfx.Played -= Listen;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator 게임_화면에_소리를_내는_부품이_있고_소리마다_클립이_있다()
        {
            yield return LoadSoundScene();

            SfxPlayer sfx = Object.FindAnyObjectByType<SfxPlayer>();
            Assert.IsNotNull(sfx, "게임 화면에 소리를 내는 부품이 없습니다.");
            Assert.IsNotNull(Object.FindAnyObjectByType<GameSounds>(), "게임 화면에 소리를 고르는 부품이 없습니다.");
            Assert.AreEqual(Sfx.Count, sfx.ClipCount);
            for (int i = 0; i < Sfx.Count; i++)
            {
                AudioClip clip = sfx.GetClip((SfxId)i);
                Assert.IsNotNull(clip, $"{(SfxId)i}의 클립이 없습니다.");
                Assert.Greater(clip.samples, 0);
            }

            Assert.GreaterOrEqual(sfx.VoiceCount, 8, "소리 여러 개가 겹칠 수 있어야 합니다.");
            Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, "소리를 듣는 귀는 하나여야 합니다.");
            Assert.AreEqual(GameSettings.DefaultSoundVolume, AudioListener.volume, 0.001f);

            int before = sfx.PlayedCount;
            Sfx.Play(SfxId.Click);
            Sfx.PlayAt(SfxId.Place, new Vector3(1f, 0.5f, -3f));
            Assert.AreEqual(before + 2, sfx.PlayedCount, "올라온 요청을 소리로 내지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 블록을_놓고_칠하고_지우면_그_자리에서_소리가_난다()
        {
            yield return LoadSoundScene();
            yield return AimWithPart(45f, keyboard.digit2Key);
            heard.Clear();
            Vector3 target = builder.TargetPosition;

            yield return Tap(mouse.leftButton);
            AssertHeardAt(SfxId.Place, target);
            Assert.AreEqual(1, heard.Count, "놓는 소리 하나만 나야 합니다.");

            heard.Clear();
            yield return Tap(keyboard.digit1Key);
            yield return Frames(2);
            AssertHeardFlat(SfxId.Select);

            heard.Clear();
            yield return Tap(mouse.middleButton);
            AssertHeardAt(SfxId.Paint, target);

            heard.Clear();
            yield return Tap(mouse.rightButton);
            AssertHeardAt(SfxId.Remove, target);
            Assert.AreEqual(1, heard.Count);
        }

        [UnityTest]
        public IEnumerator 되돌리기와_다시_실행은_자리_없이_소리가_난다()
        {
            yield return LoadSoundScene();
            yield return AimWithPart(45f, keyboard.digit2Key);
            yield return Tap(mouse.leftButton);
            heard.Clear();

            yield return TapWithCtrl(keyboard.zKey);
            AssertHeardFlat(SfxId.Undo);
            Assert.AreEqual(0, Count(SfxId.Remove), "되돌리기로 블록이 없어질 때 지우는 소리까지 나면 안 됩니다.");

            heard.Clear();
            yield return TapWithCtrl(keyboard.yKey);
            AssertHeardFlat(SfxId.Redo);
            Assert.AreEqual(0, Count(SfxId.Place));

            // 다시 실행할 것이 없으면 알림만 뜨고 소리는 나지 않는다.
            heard.Clear();
            yield return TapWithCtrl(keyboard.yKey);
            Assert.AreEqual(0, heard.Count);
        }

        [UnityTest]
        public IEnumerator 돌리기_맞추기_잡기_내려놓기_옮기기에_소리가_난다()
        {
            yield return LoadSoundScene();
            yield return AimWithPart(45f, keyboard.digit2Key);
            heard.Clear();

            yield return Tap(keyboard.rKey);
            AssertHeardFlat(SfxId.Rotate);
            yield return Tap(keyboard.tKey);
            Assert.AreEqual(2, Count(SfxId.Rotate));

            heard.Clear();
            yield return Tap(keyboard.cKey);
            AssertHeardFlat(SfxId.Snap);
            builder.SetSnapLevel(0);
            yield return Frames(2);

            // 블록을 놓고, 잡았다가 그만두고, 다시 잡아 다른 자리로 옮긴다.
            yield return Tap(mouse.leftButton);
            yield return Tap(keyboard.digit1Key);
            yield return Frames(2);
            Assert.IsTrue(builder.HasBlockTarget);

            heard.Clear();
            yield return Tap(keyboard.gKey);
            AssertHeardFlat(SfxId.Grab);

            heard.Clear();
            yield return Tap(keyboard.gKey);
            AssertHeardFlat(SfxId.Release);

            yield return Frames(2);
            yield return Tap(keyboard.gKey);
            player.SetLook(25f, 45f);
            yield return Frames(3);
            Vector3 to = builder.TargetPosition;
            heard.Clear();
            yield return Tap(mouse.leftButton);
            AssertHeardAt(SfxId.Move, to);
            Assert.AreEqual(0, Count(SfxId.Release), "옮겼을 때는 옮기는 소리만 나야 합니다.");
        }

        [UnityTest]
        public IEnumerator 놓을_수_없으면_경고_소리가_나고_놓는_소리는_나지_않는다()
        {
            yield return LoadSoundScene();
            yield return AimWithPart(80f, keyboard.digit1Key);
            Assert.AreEqual(BlockedReason.Overlap, builder.Blocked);
            heard.Clear();

            yield return Tap(mouse.leftButton);

            AssertHeardFlat(SfxId.Warn);
            Assert.AreEqual(0, Count(SfxId.Place));
        }

        [UnityTest]
        public IEnumerator 걸으면_발소리가_나고_서_있거나_날_때는_나지_않는다()
        {
            yield return LoadSoundScene();
            yield return new WaitForSeconds(0.5f);
            heard.Clear();

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, Count(SfxId.Step), "서 있는데 발소리가 났습니다.");

            Vector3 start = player.transform.position;
            Press(keyboard.wKey);
            yield return new WaitForSeconds(1.5f);
            Release(keyboard.wKey);
            yield return Frames(2);

            // 걷는 빠르기(초에 3.5)로 1.5초면 5쯤 간다. 한 걸음이 1.6이므로 발소리는 서너 번이다.
            int steps = Count(SfxId.Step);
            Assert.GreaterOrEqual(steps, 2, "걸었는데 발소리가 거의 나지 않았습니다.");
            Assert.LessOrEqual(steps, 5, "발소리가 너무 잦습니다.");
            foreach ((SfxId id, Vector3? position) in heard)
            {
                if (id != SfxId.Step) continue;

                Assert.IsTrue(position.HasValue, "발소리는 캐릭터의 자리에서 나야 합니다.");
                Assert.Less(Mathf.Abs(position.Value.x - start.x), 0.3f);
                Assert.Greater(position.Value.z, start.z - 0.1f);
            }

            // 날 때는 발소리가 나지 않는다.
            player.Motor.SetFlying(true);
            yield return Frames(2);
            heard.Clear();
            Press(keyboard.wKey);
            yield return new WaitForSeconds(1f);
            Release(keyboard.wKey);
            yield return Frames(2);
            Assert.AreEqual(0, Count(SfxId.Step), "날고 있는데 발소리가 났습니다.");
        }

        [UnityTest]
        public IEnumerator 벽에_막혀_제자리걸음을_하면_발소리가_나지_않는다()
        {
            yield return LoadSoundScene();

            // 캐릭터의 바로 앞에 벽을 세운다.
            for (int x = -1; x <= 1; x++)
            {
                world.Add(0, CellCenter(x, 0, -5));
                world.Add(0, CellCenter(x, 1, -5));
            }

            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.4f);
            heard.Clear();

            Press(keyboard.wKey);
            yield return new WaitForSeconds(1.2f);
            Release(keyboard.wKey);
            yield return Frames(2);

            Assert.AreEqual(0, Count(SfxId.Step), "벽에 막혀 나아가지 못하는데 발소리가 났습니다.");
        }

        [UnityTest]
        public IEnumerator 뛰면_뛰는_소리가_나고_내려앉으면_내려앉는_소리가_난다()
        {
            yield return LoadSoundScene();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, Count(SfxId.Land), "시작할 때 바닥에 닿는 것은 내려앉는 소리를 내지 않습니다.");
            heard.Clear();

            yield return Tap(keyboard.spaceKey);
            Assert.AreEqual(1, Count(SfxId.Jump));
            Assert.AreEqual(0, Count(SfxId.Land));

            yield return new WaitForSeconds(1f);
            Assert.AreEqual(1, Count(SfxId.Land), "뛰었다가 내려앉았는데 소리가 나지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 날기를_켜고_끄면_소리가_난다()
        {
            yield return LoadSoundScene();
            yield return new WaitForSeconds(0.3f);
            heard.Clear();

            yield return Tap(keyboard.vKey);
            AssertHeardFlat(SfxId.FlyOn);

            heard.Clear();
            yield return Tap(keyboard.vKey);
            AssertHeardFlat(SfxId.FlyOff);
        }

        [UnityTest]
        public IEnumerator 부품을_고르고_창을_열고_단추를_누르고_창을_닫을_때_소리가_난다()
        {
            yield return LoadSoundScene();
            player.CaptureLook(true);
            yield return Frames(2);
            heard.Clear();

            yield return Tap(keyboard.digit3Key);
            AssertHeardFlat(SfxId.Select);

            heard.Clear();
            yield return Tap(keyboard.escapeKey);
            AssertHeardFlat(SfxId.Open);

            heard.Clear();
            Find<Button>(ui.Menu, "Tab1").onClick.Invoke();
            yield return Frames(2);
            AssertHeardFlat(SfxId.Click);
            Assert.AreEqual(1, heard.Count);

            heard.Clear();
            yield return Tap(keyboard.escapeKey);
            AssertHeardFlat(SfxId.Close);

            // 메뉴에서 맵 목록으로 넘어갈 때는 창이 계속 열려 있으므로 단추 소리만 난다.
            yield return Tap(keyboard.escapeKey);
            heard.Clear();
            Find<Button>(ui.Menu, "HomeTile").onClick.Invoke();
            yield return Frames(2);
            Assert.IsTrue(ui.IsMapListOpen);
            AssertHeardFlat(SfxId.Click);
            Assert.AreEqual(0, Count(SfxId.Open) + Count(SfxId.Close));
        }

        [UnityTest]
        public IEnumerator 여러_일이_한꺼번에_일어나도_화면의_소리는_하나만_난다()
        {
            yield return LoadSoundScene();
            player.CaptureLook(true);
            yield return Tap(keyboard.bKey);
            Assert.IsTrue(ui.IsPickerOpen);
            heard.Clear();

            // 부품을 누르면 단추가 눌리고, 부품이 골라지고, 창이 닫힌다. 그 가운데 부품이 골라지는 소리만 낸다.
            ui.Picker.FindCell(world.Catalog.IndexOf("slab.gold")).onClick.Invoke();
            yield return Frames(2);

            Assert.IsFalse(ui.IsPickerOpen);
            Assert.AreEqual(1, heard.Count, "소리가 겹쳐 났습니다.");
            AssertHeardFlat(SfxId.Select);
        }

        [UnityTest]
        public IEnumerator 경고와_오류_알림에만_소리가_나고_오류가_경고보다_먼저다()
        {
            yield return LoadSoundScene();
            yield return Frames(2);
            heard.Clear();

            Notice.Post("보통 알림");
            yield return Frames(2);
            Assert.AreEqual(0, heard.Count, "보통 알림에는 소리가 없습니다.");

            Notice.Post("경고", NoticeKind.Warning);
            yield return Frames(2);
            AssertHeardFlat(SfxId.Warn);

            heard.Clear();
            Notice.Post("경고", NoticeKind.Warning);
            Notice.Post("오류", NoticeKind.Error);
            yield return Frames(2);
            Assert.AreEqual(1, heard.Count);
            AssertHeardFlat(SfxId.Error);
        }

        [UnityTest]
        public IEnumerator 설정의_소리_크기를_바꾸면_바로_적용되고_저장된다()
        {
            yield return LoadSoundScene();
            yield return Tap(keyboard.escapeKey);
            ui.Menu.ShowTab(2);
            yield return Frames(2);

            Slider slider = Find<Slider>(ui.Menu, "SoundSlider");
            TMP_Text value = slider.transform.parent.Find("Value").GetComponent<TMP_Text>();
            Assert.AreEqual(GameSettings.DefaultSoundVolume, slider.value, 0.001f);
            Assert.AreEqual("70%", value.text);

            heard.Clear();
            slider.value = 0.25f;
            yield return Frames(2);
            Assert.AreEqual(0.25f, GameSettings.SoundVolume, 0.001f);
            Assert.AreEqual(0.25f, AudioListener.volume, 0.001f, "바꾼 크기가 바로 적용되지 않았습니다.");
            Assert.AreEqual("25%", value.text);
            Assert.GreaterOrEqual(heard.Count, 1, "크기를 들어 볼 수 있게 소리가 나야 합니다.");

            slider.value = 0f;
            yield return Frames(2);
            Assert.AreEqual(0f, AudioListener.volume, 0.001f);
            Assert.AreEqual("끔", value.text);

            Find<Button>(ui.Menu, "Reset").onClick.Invoke();
            yield return Frames(2);
            Assert.AreEqual(GameSettings.DefaultSoundVolume, AudioListener.volume, 0.001f);
            Assert.AreEqual(GameSettings.DefaultSoundVolume, slider.value, 0.001f);

            // 다시 열어도 저장한 크기가 남는다.
            GameSettings.SoundVolume = 0.4f;
            yield return LoadSoundScene();
            Assert.AreEqual(0.4f, AudioListener.volume, 0.001f);
        }

        [UnityTest]
        public IEnumerator 맵을_불러올_때는_블록_소리가_나지_않는다()
        {
            PrepareMapDirectory();
            MapDocument document = MapDocument.Create("블록이 많은 맵", new Vector3(-8f, 0f, -8f), new Vector3(8f, 12f, 8f));
            for (int i = 0; i < 30; i++)
            {
                document.blocks.Add(new MapBlock { id = i + 1, part = "block.gold", position = CellCenter(-7 + i % 15, i / 15, 3) });
            }

            MapStorage.Save(document, MapStorage.LocalPath);

            yield return LoadSoundScene();
            yield return Frames(3);

            Assert.AreEqual(30, world.Count);
            Assert.AreEqual(0, Count(SfxId.Place), "맵을 불러올 때 블록마다 소리가 났습니다.");
            Assert.AreEqual(0, Count(SfxId.Remove));

            // 기록을 거치지 않고 바로 놓는 것(불러오기, 테스트)도 소리를 내지 않는다.
            world.Add(BluePart, CellCenter(0, 0, -3));
            world.Import(document);
            yield return Frames(2);
            Assert.AreEqual(0, Count(SfxId.Place) + Count(SfxId.Remove));
        }

        private void Listen(SfxId id, Vector3? position)
        {
            heard.Add((id, position));
        }

        private int Count(SfxId id)
        {
            int count = 0;
            foreach ((SfxId heardId, Vector3? _) in heard)
            {
                if (heardId == id) count++;
            }

            return count;
        }

        /// <summary>자리가 있는 소리가 그 자리 근처에서 났는지 본다.</summary>
        private void AssertHeardAt(SfxId id, Vector3 position)
        {
            foreach ((SfxId heardId, Vector3? heardPosition) in heard)
            {
                if (heardId != id) continue;

                Assert.IsTrue(heardPosition.HasValue, $"{id}는 자리가 있는 소리여야 합니다.");
                Assert.Less(Vector3.Distance(position, heardPosition.Value), 0.05f, $"{id}가 블록의 자리에서 나지 않았습니다.");
                return;
            }

            Assert.Fail($"{id} 소리가 나지 않았습니다. 난 소리: {Describe()}");
        }

        /// <summary>자리 없는 소리가 났는지 본다.</summary>
        private void AssertHeardFlat(SfxId id)
        {
            foreach ((SfxId heardId, Vector3? heardPosition) in heard)
            {
                if (heardId != id) continue;

                Assert.IsFalse(heardPosition.HasValue, $"{id}는 자리 없는 소리여야 합니다.");
                return;
            }

            Assert.Fail($"{id} 소리가 나지 않았습니다. 난 소리: {Describe()}");
        }

        private string Describe()
        {
            var names = new List<string>();
            foreach ((SfxId id, Vector3? _) in heard) names.Add(id.ToString());
            return names.Count == 0 ? "없음" : string.Join(", ", names);
        }

        private IEnumerator LoadSoundScene()
        {
            yield return LoadSandbox();

            world = Object.FindAnyObjectByType<BlockWorld>();
            builder = Object.FindAnyObjectByType<BlockBuilder>();
            Assert.IsNotNull(world, "Sandbox 씬에서 블록 세계를 찾을 수 없습니다.");
            Assert.IsNotNull(builder, "PC 캐릭터에 블록 놓기가 없습니다.");
            yield return Frames(2);
        }
    }
}
