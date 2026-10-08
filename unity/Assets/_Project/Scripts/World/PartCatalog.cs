using System;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>
    /// 놓을 수 있는 부품의 목록. 부품 칸과 블록 세계가 같은 목록을 본다.
    /// id는 맵을 저장할 때 쓰는 이름이므로 한 번 정하면 바꾸지 않는다.
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
        }

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

        /// <summary>재질로 부품 번호를 찾는다. 없으면 -1이다.</summary>
        public int IndexOf(Material material)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].material == material) return i;
            }

            return -1;
        }
    }
}
