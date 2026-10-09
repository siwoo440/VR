using System.Collections.Generic;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>부품의 모양. 저장되는 값이므로 순서를 바꾸지 않고 뒤에만 더한다.</summary>
    public enum PartShape
    {
        Block,
        Slab,
        Pillar,
        Wedge,
        Stairs,
    }

    /// <summary>
    /// 부품의 모양을 메시로 만든다(16일차). 크기는 표준 블록 한 변을 1로 보고 정하며, 메시의 가운데가 원점이다.
    /// 면마다 꼭짓점을 따로 두어 모서리가 또렷하게 보이게 한다. 셋업이 이 메시를 자산으로 저장하고 부품 목록이 가리킨다.
    /// 장면과 무관해 편집 모드 테스트로 검사한다.
    /// </summary>
    public static class PartMeshes
    {
        /// <summary>계단의 단 수. 한 단의 높이(1/4)는 캐릭터가 걸어 오를 수 있는 높이여야 한다.</summary>
        public const int StairSteps = 4;

        private static readonly string[] Names = { "블록", "판", "기둥", "경사", "계단" };

        public static int ShapeCount => Names.Length;

        /// <summary>화면에 보이는 모양의 이름.</summary>
        public static string NameOf(PartShape shape)
        {
            int index = (int)shape;
            return index >= 0 && index < Names.Length ? Names[index] : Names[0];
        }

        /// <summary>모양을 둘러싸는 상자의 크기(가로, 높이, 세로). 블록 한 변이 1이다.</summary>
        public static Vector3 SizeOf(PartShape shape)
        {
            switch (shape)
            {
                case PartShape.Slab: return new Vector3(1f, 0.5f, 1f);
                case PartShape.Pillar: return new Vector3(0.5f, 1f, 0.5f);
                default: return Vector3.one;
            }
        }

        /// <summary>모양이 상자와 같은지. 상자 모양은 상자 충돌체를 쓰고, 경사와 계단은 메시 충돌체를 쓴다.</summary>
        public static bool IsBox(PartShape shape)
        {
            return shape == PartShape.Block || shape == PartShape.Slab || shape == PartShape.Pillar;
        }

        /// <summary>모양의 부피. 메시가 빈틈없이 닫혔는지 검사할 때 견준다.</summary>
        public static float VolumeOf(PartShape shape)
        {
            Vector3 size = SizeOf(shape);
            float box = size.x * size.y * size.z;
            switch (shape)
            {
                case PartShape.Wedge: return box * 0.5f;
                case PartShape.Stairs: return box * (StairSteps + 1) / (2f * StairSteps);
                default: return box;
            }
        }

        public static Mesh Build(PartShape shape)
        {
            var builder = new Builder();
            Vector3 half = SizeOf(shape) * 0.5f;

            switch (shape)
            {
                case PartShape.Wedge:
                    AddWedge(builder, half);
                    break;
                case PartShape.Stairs:
                    AddStairs(builder, half);
                    break;
                default:
                    AddBox(builder, -half, half);
                    break;
            }

            return builder.ToMesh($"Part_{shape}");
        }

        private static void AddBox(Builder builder, Vector3 min, Vector3 max)
        {
            var a = new Vector3(min.x, min.y, min.z);
            var b = new Vector3(max.x, min.y, min.z);
            var c = new Vector3(max.x, min.y, max.z);
            var d = new Vector3(min.x, min.y, max.z);
            var e = new Vector3(min.x, max.y, min.z);
            var f = new Vector3(max.x, max.y, min.z);
            var g = new Vector3(max.x, max.y, max.z);
            var h = new Vector3(min.x, max.y, max.z);

            builder.Quad(a, b, c, d, Vector3.down);
            builder.Quad(e, f, g, h, Vector3.up);
            builder.Quad(a, b, f, e, Vector3.back);
            builder.Quad(d, c, g, h, Vector3.forward);
            builder.Quad(a, d, h, e, Vector3.left);
            builder.Quad(b, c, g, f, Vector3.right);
        }

        /// <summary>경사. 앞(-z)의 바닥에서 뒤(+z)의 꼭대기까지 45도로 오른다. +z 쪽으로 걸으면 올라간다.</summary>
        private static void AddWedge(Builder builder, Vector3 half)
        {
            var frontLeft = new Vector3(-half.x, -half.y, -half.z);
            var frontRight = new Vector3(half.x, -half.y, -half.z);
            var backLeft = new Vector3(-half.x, -half.y, half.z);
            var backRight = new Vector3(half.x, -half.y, half.z);
            var topLeft = new Vector3(-half.x, half.y, half.z);
            var topRight = new Vector3(half.x, half.y, half.z);

            builder.Quad(frontLeft, frontRight, backRight, backLeft, Vector3.down);
            builder.Quad(backLeft, backRight, topRight, topLeft, Vector3.forward);
            builder.Quad(frontLeft, frontRight, topRight, topLeft, new Vector3(0f, 1f, -1f));
            builder.Triangle(frontLeft, backLeft, topLeft, Vector3.left);
            builder.Triangle(frontRight, backRight, topRight, Vector3.right);
        }

        /// <summary>계단. 앞(-z)에서 뒤(+z)로 네 단을 오른다. 안쪽에 숨은 면은 만들지 않는다.</summary>
        private static void AddStairs(Builder builder, Vector3 half)
        {
            float depth = half.z * 2f / StairSteps;
            float rise = half.y * 2f / StairSteps;
            float bottom = -half.y;

            for (int i = 0; i < StairSteps; i++)
            {
                float near = -half.z + depth * i;
                float far = near + depth;
                float below = bottom + rise * i;
                float top = below + rise;

                // 디딤판과 그 앞의 챌판
                builder.Quad(
                    new Vector3(-half.x, top, near), new Vector3(half.x, top, near),
                    new Vector3(half.x, top, far), new Vector3(-half.x, top, far), Vector3.up);
                builder.Quad(
                    new Vector3(-half.x, below, near), new Vector3(half.x, below, near),
                    new Vector3(half.x, top, near), new Vector3(-half.x, top, near), Vector3.back);

                // 양옆: 이 단의 바닥에서 디딤판까지
                builder.Quad(
                    new Vector3(-half.x, bottom, near), new Vector3(-half.x, bottom, far),
                    new Vector3(-half.x, top, far), new Vector3(-half.x, top, near), Vector3.left);
                builder.Quad(
                    new Vector3(half.x, bottom, near), new Vector3(half.x, bottom, far),
                    new Vector3(half.x, top, far), new Vector3(half.x, top, near), Vector3.right);
            }

            builder.Quad(
                new Vector3(-half.x, bottom, -half.z), new Vector3(half.x, bottom, -half.z),
                new Vector3(half.x, bottom, half.z), new Vector3(-half.x, bottom, half.z), Vector3.down);
            builder.Quad(
                new Vector3(-half.x, bottom, half.z), new Vector3(half.x, bottom, half.z),
                new Vector3(half.x, half.y, half.z), new Vector3(-half.x, half.y, half.z), Vector3.forward);
        }

        /// <summary>면을 하나씩 모아 메시로 만든다. 면이 바깥(outward)을 보도록 꼭짓점의 순서를 스스로 맞춘다.</summary>
        private class Builder
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> uvs = new List<Vector2>();
            private readonly List<int> triangles = new List<int>();

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f)
                {
                    (b, d) = (d, b);
                }

                int start = Add(Vector3.Cross(b - a, c - a).normalized, a, b, c, d);
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

            public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f)
                {
                    (b, c) = (c, b);
                }

                int start = Add(Vector3.Cross(b - a, c - a).normalized, a, b, c);
                triangles.AddRange(new[] { start, start + 1, start + 2 });
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                return mesh;
            }

            private int Add(Vector3 normal, params Vector3[] corners)
            {
                int start = vertices.Count;
                foreach (Vector3 corner in corners)
                {
                    vertices.Add(corner);
                    normals.Add(normal);
                    uvs.Add(UvOf(corner, normal));
                }

                return start;
            }

            /// <summary>면이 보는 쪽에서 본 평면 좌표를 그대로 쓴다. 지금의 재질은 무늬가 없어 값이 보이지는 않는다.</summary>
            private static Vector2 UvOf(Vector3 point, Vector3 normal)
            {
                Vector3 size = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
                if (size.y >= size.x && size.y >= size.z) return new Vector2(point.x + 0.5f, point.z + 0.5f);
                if (size.x >= size.z) return new Vector2(point.z + 0.5f, point.y + 0.5f);
                return new Vector2(point.x + 0.5f, point.y + 0.5f);
            }
        }
    }
}
