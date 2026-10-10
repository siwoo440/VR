using System;
using System.Collections.Generic;
using AtelierVerse.Core;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 코드로 만드는 기본 소리의 검사(19일차). 소리가 듣기 좋은지는 검사할 수 없고, 소리로서 탈이 없는지만 본다:
    /// 짧은지, 처음과 끝이 조용한지(딱 소리가 나지 않게), 너무 크거나 비어 있지 않은지, 늘 같게 만들어지는지.
    /// </summary>
    public class SfxSynthTests
    {
        private static readonly SfxId[] Ids = (SfxId[])Enum.GetValues(typeof(SfxId));

        [Test]
        public void 소리는_짧고_처음과_끝이_조용하다([ValueSource(nameof(Ids))] SfxId id)
        {
            float[] data = SfxSynth.Build(id);
            float seconds = SfxSynth.SecondsOf(data);

            Assert.Greater(seconds, 0.015f, $"{id}가 너무 짧아 들리지 않습니다.");
            Assert.Less(seconds, 0.3f, $"{id}가 기본 소리로는 너무 깁니다.");
            Assert.AreEqual(0f, data[0], 0.0001f, "처음이 0이 아니면 딱 하는 잡음이 납니다.");
            Assert.AreEqual(0f, data[data.Length - 1], 0.0001f, "끝이 0이 아니면 딱 하는 잡음이 납니다.");

            // 끝의 1/1000초는 거의 조용해야 한다.
            int tail = SfxSynth.SampleRate / 1000;
            for (int i = data.Length - tail; i < data.Length; i++)
            {
                Assert.Less(Mathf.Abs(data[i]), 0.12f, $"{id}의 끝이 갑자기 끊깁니다.");
            }
        }

        [Test]
        public void 소리는_너무_크거나_비어_있지_않다([ValueSource(nameof(Ids))] SfxId id)
        {
            float[] data = SfxSynth.Build(id);
            float peak = 0f;
            double energy = 0.0;

            foreach (float value in data)
            {
                Assert.IsFalse(float.IsNaN(value) || float.IsInfinity(value), $"{id}에 숫자가 아닌 값이 있습니다.");
                peak = Mathf.Max(peak, Mathf.Abs(value));
                energy += value * value;
            }

            Assert.Greater(peak, 0.2f, $"{id}가 너무 작습니다.");
            Assert.LessOrEqual(peak, 0.7f, $"{id}가 너무 큽니다. 여러 소리가 겹칠 때 찢어지지 않게 여유를 둡니다.");
            Assert.Greater(Math.Sqrt(energy / data.Length), 0.02, $"{id}가 거의 비어 있습니다.");
        }

        [Test]
        public void 같은_소리는_늘_같게_만들어진다([ValueSource(nameof(Ids))] SfxId id)
        {
            float[] first = SfxSynth.Build(id);
            float[] second = SfxSynth.Build(id);

            Assert.AreNotSame(first, second);
            CollectionAssert.AreEqual(first, second, $"{id}가 만들 때마다 달라집니다.");
        }

        [Test]
        public void 소리마다_서로_다르다()
        {
            var seen = new Dictionary<string, SfxId>();
            foreach (SfxId id in Ids)
            {
                float[] data = SfxSynth.Build(id);
                double sum = 0.0;
                for (int i = 0; i < data.Length; i++)
                {
                    sum += data[i] * (i % 97 + 1);
                }

                string key = $"{data.Length}:{sum:F4}";
                Assert.IsFalse(seen.ContainsKey(key), $"{id}와 {(seen.ContainsKey(key) ? seen[key] : id)}가 같은 소리입니다.");
                seen[key] = id;
            }

            Assert.AreEqual(Sfx.Count, seen.Count);
        }

        [Test]
        public void 놓는_소리는_지우는_소리보다_짧고_오류는_경고보다_길다()
        {
            Assert.Less(SfxSynth.SecondsOf(SfxSynth.Build(SfxId.Place)), SfxSynth.SecondsOf(SfxSynth.Build(SfxId.Remove)));
            Assert.Less(SfxSynth.SecondsOf(SfxSynth.Build(SfxId.Rotate)), SfxSynth.SecondsOf(SfxSynth.Build(SfxId.Click)), "돌리는 소리는 이어서 나므로 가장 짧아야 합니다.");
            Assert.Less(SfxSynth.SecondsOf(SfxSynth.Build(SfxId.Step)), 0.1f, "발소리가 길면 걸음이 겹칩니다.");
        }

        [Test]
        public void 소리를_재생할_수_있는_클립으로_만든다([ValueSource(nameof(Ids))] SfxId id)
        {
            AudioClip clip = SfxSynth.CreateClip(id);
            try
            {
                Assert.AreEqual($"Sfx_{id}", clip.name);
                Assert.AreEqual(SfxSynth.Build(id).Length, clip.samples);
                Assert.AreEqual(1, clip.channels);
                Assert.AreEqual(SfxSynth.SampleRate, clip.frequency);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void 소리_요청은_자리가_있는_것과_없는_것을_구별해_알린다()
        {
            var heard = new List<(SfxId id, Vector3? position)>();
            void Listen(SfxId id, Vector3? position) => heard.Add((id, position));

            Sfx.Played += Listen;
            try
            {
                Sfx.Play(SfxId.Click);
                Sfx.PlayAt(SfxId.Place, new Vector3(1f, 2f, 3f));
            }
            finally
            {
                Sfx.Played -= Listen;
            }

            // 듣는 쪽이 없어도 탈이 없어야 한다.
            Sfx.Play(SfxId.Click);

            Assert.AreEqual(2, heard.Count);
            Assert.AreEqual(SfxId.Click, heard[0].id);
            Assert.IsNull(heard[0].position, "화면의 소리에는 자리가 없습니다.");
            Assert.AreEqual(SfxId.Place, heard[1].id);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), heard[1].position);
        }
    }
}
