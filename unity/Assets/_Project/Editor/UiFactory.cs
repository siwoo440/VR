using AtelierVerse.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 셋업 스크립트가 화면 요소를 만들 때 쓰는 도우미. 글꼴과 둥근 모서리 그림을 한곳에서 정한다.
    /// 꾸미기용 요소는 누르기를 가로채지 않도록 raycastTarget을 끄고, 단추로 만들 때만 켠다.
    /// </summary>
    internal static class UiFactory
    {
        public const int UiLayer = 5;

        /// <summary>둥근 모서리 그림(Rounded.png)에 그려진 모서리 반지름(픽셀).</summary>
        public const float SpriteCorner = 24f;

        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 MiddleLeft = new Vector2(0f, 0.5f);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 MiddleRight = new Vector2(1f, 0.5f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        public static TMP_FontAsset Font { get; set; }

        public static Sprite Rounded { get; set; }

        public static Sprite RoundedLine { get; set; }

        public static RectTransform Rect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = UiLayer;
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>부모의 한 점(anchor)에 붙여 놓는다. 기준점(pivot)도 같은 곳으로 맞춘다.</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>부모를 가득 채운다. 값은 각 변에서 안쪽으로 띄우는 거리다.</summary>
        public static void Fill(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Center;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>부모의 위쪽에 붙여 가로로 가득 채운다. y는 위에서 아래로 내려오는 거리(음수)다.</summary>
        public static void StretchTop(RectTransform rect, float y, float height, float inset = 0f)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-2f * inset, height);
        }

        /// <summary>둥근 모서리 상자. corner는 화면에 보일 모서리 반지름이다.</summary>
        public static Image Box(string name, Transform parent, Color color, float corner = 20f)
        {
            RectTransform rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            Style(image, Rounded, color, corner);
            return image;
        }

        /// <summary>상자 위에 같은 모서리의 테두리 선을 겹친다.</summary>
        public static Image Outline(Image box, Color color)
        {
            RectTransform rect = Rect("Line", box.transform);
            Fill(rect);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RoundedLine;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = box.pixelsPerUnitMultiplier;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>테두리와 어긋난 그림자가 있는 종이 카드. 웹 화면 시안의 카드와 같은 모양이다.</summary>
        public static Image Card(string name, Transform parent, Color fill, Color line, float corner = 20f, float lift = 5f)
        {
            Image box = Box(name, parent, fill, corner);
            if (lift > 0f)
            {
                var shadow = box.gameObject.AddComponent<Shadow>();
                shadow.effectColor = AtelierPalette.Ink;
                shadow.effectDistance = new Vector2(lift, -lift);
                shadow.useGraphicAlpha = true;
            }

            Outline(box, line);
            return box;
        }

        public static void Style(Image image, Sprite sprite, Color color, float corner)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = SpriteCorner / Mathf.Max(1f, corner);
            image.color = color;
            image.raycastTarget = false;
        }

        /// <summary>모서리가 없는 가로 구분선.</summary>
        public static Image Divider(Transform parent, float y, float inset, Color color)
        {
            RectTransform rect = Rect("Divider", parent);
            StretchTop(rect, y, 2f, inset);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static TMP_Text Text(string name, Transform parent, string content, float size, Color color, bool bold = false, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            RectTransform rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = Font;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.text = content;
            return text;
        }

        /// <summary>글자를 한 줄로 썼을 때의 너비.</summary>
        public static float WidthOf(TMP_Text text)
        {
            text.ForceMeshUpdate();
            return text.preferredWidth;
        }

        public static Image Icon(string name, Transform parent, Sprite sprite, Color color, float size)
        {
            RectTransform rect = Rect(name, parent);
            Place(rect, Center, Vector2.zero, new Vector2(size, size));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>키 이름이나 짧은 표시를 담는 작은 딱지. 너비는 글자에 맞춘다.</summary>
        public static RectTransform Badge(string name, Transform parent, string content, Color fill, Color textColor, float height = 28f, float fontSize = 16f)
        {
            Image box = Box(name, parent, fill, 8f);
            // 작은 한글은 굵게 하면 획이 뭉치므로 보통 굵기로 쓴다.
            TMP_Text label = Text("Label", box.transform, content, fontSize, textColor, false, TextAlignmentOptions.Center);
            Fill(label.rectTransform);
            box.rectTransform.sizeDelta = new Vector2(Mathf.Max(height, WidthOf(label) + 18f), height);
            return box.rectTransform;
        }

        /// <summary>그림을 누를 수 있는 단추로 만든다. 키보드로 옮겨 다니는 선택은 쓰지 않는다.</summary>
        public static Button Clickable(Image target)
        {
            target.raycastTarget = true;

            var button = target.gameObject.AddComponent<Button>();
            button.targetGraphic = target;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.86f);
            colors.pressedColor = new Color(0.92f, 0.86f, 0.74f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            NoNavigation(button);
            return button;
        }

        public static void NoNavigation(Selectable selectable)
        {
            Navigation navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.None;
            selectable.navigation = navigation;
        }

        public static void SetLayer(GameObject root, int layer)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }
    }
}
