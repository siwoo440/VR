using UnityEngine;

namespace AtelierVerse.Core
{
    /// <summary>
    /// 햇살 작업실 테마의 기준 색. 웹 화면 시안(design/assets/tokens.css)의 밝은 화면 값과 같다.
    /// 부품과 바닥의 재질 색을 이 값에서 가져온다.
    /// </summary>
    public static class AtelierPalette
    {
        public static readonly Color Canvas = FromHex(0xFBF6EC);
        public static readonly Color Ivory = FromHex(0xFFFDF8);
        public static readonly Color Ink = FromHex(0x1D2740);
        public static readonly Color Gold = FromHex(0xF7A71D);
        public static readonly Color Blue = FromHex(0x025EE7);
        public static readonly Color Clay = FromHex(0xB8440F);
        public static readonly Color Leaf = FromHex(0x2B7A4B);
        public static readonly Color Wood = FromHex(0x8E7F63);

        private static Color FromHex(int rgb)
        {
            return new Color32((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF), 255);
        }
    }
}
