using System;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>
    /// 놓을 수 있는 부품의 목록. 부품 칸, 부품 고르는 창, 블록 세계가 같은 목록을 본다.
    /// 부품 하나는 모양(블록, 판, 기둥, 경사, 계단)과 색의 짝이다. 크기는 표준 블록 한 변을 1로 보고 정한다.
    /// id는 맵을 저장할 때 쓰는 이름이므로 한 번 정하면 바꾸지 않는다. 순서도 바꾸지 않고 뒤에만 더한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Atelier Verse/Part Catalog", fileName = "PartCatalog")]
    public class PartCatalog : ScriptableObject
    {
        [Serializable]
        public struct Part
        {
            public string id;
            public string displayName;
            public Material material;
            public Color color;
            public PartShape shape;
            public Mesh mesh;
            public Vector3 size;
            public bool boxCollider;
        }

        /// <summary>표준 블록의 반 변. 크기를 적지 않은 부품은 표준 블록으로 본다.</summary>
        public static readonly Vector3 StandardHalfSize = Vector3.one * (GridMath.DefaultCellSize * 0.5f);

        [SerializeField] private Part[] parts = Array.Empty<Part>();

        public int Count => parts.Length;

        public bool IsValid(int index)
        {
            return index >= 0 && index < parts.Length;
        }

        public Part Get(int index)
        {
            return parts[index];
        }

        /// <summary>id로 부품 번호를 찾는다. 없으면 -1이다.</summary>
        public int IndexOf(string id)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].id == id) return i;
            }

            return -1;
        }

        /// <summary>재질로 부품 번호를 찾는다. 같은 재질의 부품이 여럿이면 먼저 나오는 것(블록)이다. 없으면 -1이다.</summary>
        public int IndexOf(Material material)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].material == material) return i;
            }

            return -1;
        }

        public PartShape ShapeOf(int index)
        {
            return IsValid(index) ? parts[index].shape : PartShape.Block;
        }

        /// <summary>부품을 둘러싸는 상자의 크기(가로, 높이, 세로). 블록 한 변이 1이다.</summary>
        public Vector3 SizeOf(int index)
        {
            return HalfSizeOf(index) * 2f;
        }

        /// <summary>부품을 둘러싸는 상자의 반 크기. 놓는 높이, 범위 검사, 겹침 검사가 쓴다.</summary>
        public Vector3 HalfSizeOf(int index)
        {
            if (!IsValid(index)) return StandardHalfSize;

            Vector3 size = parts[index].size;
            return size.x > 0f && size.y > 0f && size.z > 0f ? size * 0.5f : StandardHalfSize;
        }

        /// <summary>모양과 색(색은 colorSource 부품의 것)이 맞는 부품을 찾는다. 없으면 -1이다.</summary>
        public int Find(PartShape shape, int colorSource)
        {
            if (!IsValid(colorSource)) return -1;

            Material material = parts[colorSource].material;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].shape == shape && parts[i].material == material) return i;
            }

            return -1;
        }

        /// <summary>
        /// target 부품을 brush 부품의 색으로 칠했을 때의 부품. 모양은 target의 것이 그대로 남는다.
        /// 그 색의 같은 모양이 목록에 없으면 target 그대로다.
        /// </summary>
        public int Repaint(int target, int brush)
        {
            if (!IsValid(target) || !IsValid(brush)) return target;

            int found = Find(parts[target].shape, brush);
            return found >= 0 ? found : target;
        }
    }
}
