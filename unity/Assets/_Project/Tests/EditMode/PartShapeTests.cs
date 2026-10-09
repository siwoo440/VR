using System.Collections.Generic;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 모양이 다른 부품의 검사(16일차). 메시가 정한 크기와 같은지, 빈틈없이 닫혔는지, 면이 바깥을 보는지를 확인하고,
    /// 부품 목록 자산에 모양마다 색이 다 들어 있는지와 칠할 때 모양이 남는지를 본다.
    /// </summary>
    public class PartShapeTests
    {
        private const string CatalogPath = "Assets/_Project/Data/PartCatalog.asset";

        private static readonly PartShape[] Shapes = { PartShape.Block, PartShape.Slab, PartShape.Pillar, PartShape.Wedge, PartShape.Stairs };

        private static PartCatalog LoadCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PartCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, $"부품 목록이 없습니다: {CatalogPath}");
            return catalog;
        }

        [Test]
        public void 모양의_크기는_블록_한_변을_1로_정한다()
        {
            Assert.AreEqual(Vector3.one, PartMeshes.SizeOf(PartShape.Block));
            Assert.AreEqual(new Vector3(1f, 0.5f, 1f), PartMeshes.SizeOf(PartShape.Slab));
            Assert.AreEqual(new Vector3(0.5f, 1f, 0.5f), PartMeshes.SizeOf(PartShape.Pillar));
            Assert.AreEqual(Vector3.one, PartMeshes.SizeOf(PartShape.Wedge));
            Assert.AreEqual(Vector3.one, PartMeshes.SizeOf(PartShape.Stairs));
            Assert.AreEqual(Shapes.Length, PartMeshes.ShapeCount);
        }

        [Test]
        public void 메시는_정한_크기의_상자에_꼭_맞고_가운데가_원점이다([ValueSource(nameof(Shapes))] PartShape shape)
        {
            Mesh mesh = PartMeshes.Build(shape);
            try
            {
                Vector3 size = PartMeshes.SizeOf(shape);
                Assert.Less(Vector3.Distance(size, mesh.bounds.size), 0.0001f, $"{shape}의 메시 크기가 정한 크기와 다릅니다.");
                Assert.Less(mesh.bounds.center.magnitude, 0.0001f, "메시의 가운데는 원점이어야 합니다.");
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void 메시는_빈틈없이_닫혀_있고_면이_바깥을_본다([ValueSource(nameof(Shapes))] PartShape shape)
        {
            Mesh mesh = PartMeshes.Build(shape);
            try
            {
                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                int[] triangles = mesh.triangles;
                Assert.AreEqual(vertices.Length, normals.Length);

                // 면이 모두 바깥을 보고 빈틈이 없으면, 원점과 삼각형이 이루는 뿔의 부피를 더한 값이 모양의 부피가 된다.
                float volume = 0f;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = vertices[triangles[i]];
                    Vector3 b = vertices[triangles[i + 1]];
                    Vector3 c = vertices[triangles[i + 2]];

                    Vector3 face = Vector3.Cross(b - a, c - a);
                    Assert.Greater(face.magnitude, 0.0001f, "넓이가 없는 삼각형이 있습니다.");
                    Assert.Greater(Vector3.Dot(face.normalized, normals[triangles[i]]), 0.999f, "꼭짓점의 법선이 면의 방향과 다릅니다.");
                    Assert.AreEqual(1f, normals[triangles[i]].magnitude, 0.0001f);

                    volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;
                }

                Assert.AreEqual(PartMeshes.VolumeOf(shape), volume, 0.0001f, $"{shape}의 부피가 맞지 않습니다. 면이 뒤집혔거나 빠졌습니다.");
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void 계단의_한_단은_캐릭터가_걸어_오를_수_있는_높이다()
        {
            // 캐릭터가 걸어 오를 수 있는 턱의 높이는 0.35다(캐릭터 프리팹의 stepOffset).
            float rise = PartMeshes.SizeOf(PartShape.Stairs).y / PartMeshes.StairSteps;

            Assert.LessOrEqual(rise, 0.3f);
            Assert.AreEqual(0.625f, PartMeshes.VolumeOf(PartShape.Stairs), 0.0001f);
            Assert.AreEqual(0.5f, PartMeshes.VolumeOf(PartShape.Wedge), 0.0001f);
        }

        [Test]
        public void 상자_모양만_상자_충돌체를_쓴다()
        {
            Assert.IsTrue(PartMeshes.IsBox(PartShape.Block));
            Assert.IsTrue(PartMeshes.IsBox(PartShape.Slab));
            Assert.IsTrue(PartMeshes.IsBox(PartShape.Pillar));
            Assert.IsFalse(PartMeshes.IsBox(PartShape.Wedge));
            Assert.IsFalse(PartMeshes.IsBox(PartShape.Stairs));
        }

        [Test]
        public void 부품_목록의_앞_여섯은_전과_같은_블록이다()
        {
            PartCatalog catalog = LoadCatalog();
            string[] ids = { "block.gold", "block.blue", "block.clay", "block.leaf", "block.ivory", "block.wood" };

            // 씬에 놓인 블록과 옛 설정이 번호로 가리키므로, 앞 여섯의 순서와 이름은 바뀌면 안 된다.
            for (int i = 0; i < ids.Length; i++)
            {
                Assert.AreEqual(ids[i], catalog.Get(i).id);
                Assert.AreEqual(PartShape.Block, catalog.ShapeOf(i));
                Assert.AreEqual(Vector3.one, catalog.SizeOf(i));
            }
        }

        [Test]
        public void 부품_목록에는_모양마다_색이_다_있고_이름이_겹치지_않는다()
        {
            PartCatalog catalog = LoadCatalog();
            Assert.AreEqual(30, catalog.Count, "모양 다섯에 색 여섯입니다.");

            var ids = new HashSet<string>();
            var names = new HashSet<string>();
            for (int i = 0; i < catalog.Count; i++)
            {
                PartCatalog.Part part = catalog.Get(i);
                Assert.IsTrue(ids.Add(part.id), $"저장용 이름이 겹칩니다: {part.id}");
                Assert.IsTrue(names.Add(part.displayName), $"보이는 이름이 겹칩니다: {part.displayName}");
                Assert.IsNotNull(part.material, $"{part.id}에 재질이 없습니다.");
                Assert.IsNotNull(part.mesh, $"{part.id}에 메시가 없습니다.");
                Assert.AreEqual(PartMeshes.SizeOf(part.shape), part.size, $"{part.id}의 크기가 모양의 크기와 다릅니다.");
                Assert.Less(Vector3.Distance(part.size, part.mesh.bounds.size), 0.0001f, $"{part.id}의 메시 크기가 적힌 크기와 다릅니다.");
                Assert.AreEqual(PartMeshes.IsBox(part.shape), part.boxCollider);
                Assert.AreEqual(i, catalog.IndexOf(part.id));
            }

            Assert.AreEqual("골드 판", catalog.Get(catalog.IndexOf("slab.gold")).displayName);
            Assert.AreEqual("나무 계단", catalog.Get(catalog.IndexOf("stairs.wood")).displayName);
            Assert.AreEqual(new Vector3(0.25f, 0.5f, 0.25f), catalog.HalfSizeOf(catalog.IndexOf("pillar.blue")));
        }

        [Test]
        public void 칠하면_색만_바뀌고_모양은_남는다()
        {
            PartCatalog catalog = LoadCatalog();
            int goldSlab = catalog.IndexOf("slab.gold");
            int blueBlock = catalog.IndexOf("block.blue");
            int blueWedge = catalog.IndexOf("wedge.blue");

            Assert.AreEqual(catalog.IndexOf("slab.blue"), catalog.Repaint(goldSlab, blueBlock), "판을 블루 블록으로 칠하면 블루 판이 됩니다.");
            Assert.AreEqual(catalog.IndexOf("slab.blue"), catalog.Repaint(goldSlab, blueWedge), "붓으로 쓴 부품의 모양은 상관없습니다.");
            Assert.AreEqual(goldSlab, catalog.Repaint(goldSlab, catalog.IndexOf("stairs.gold")), "이미 같은 색이면 그대로입니다.");
            Assert.AreEqual(goldSlab, catalog.Repaint(goldSlab, -1), "없는 부품으로는 칠하지 않습니다.");
            Assert.AreEqual(catalog.IndexOf("block.blue"), catalog.Find(PartShape.Block, blueWedge));
        }

        [Test]
        public void 없는_부품의_크기는_표준_블록으로_본다()
        {
            PartCatalog catalog = LoadCatalog();

            Assert.AreEqual(PartCatalog.StandardHalfSize, catalog.HalfSizeOf(-1));
            Assert.AreEqual(PartCatalog.StandardHalfSize, catalog.HalfSizeOf(999));
            Assert.AreEqual(PartShape.Block, catalog.ShapeOf(999));
        }
    }
}
