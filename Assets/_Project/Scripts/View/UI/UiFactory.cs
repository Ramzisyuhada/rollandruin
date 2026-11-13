using UnityEngine;
using UnityEngine.UI;

namespace RollAndRuin.View
{
    /// <summary>
    /// Helper membuat elemen uGUI dari kode (dipakai View dan scene builder di Editor).
    /// Memakai font bawaan Unity supaya tidak perlu import TextMeshPro Essentials.
    /// </summary>
    public static class UiFactory
    {
        public static readonly Color TextColor = new Color(0.96f, 0.92f, 0.82f);
        public static readonly Color DimColor = new Color(0.55f, 0.55f, 0.55f);
        public static readonly Color GoldColor = new Color(1f, 0.82f, 0.3f);
        public static readonly Color DangerColor = new Color(0.95f, 0.35f, 0.3f);

        private static Font _font;

        public static Font DefaultFont
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static Text CreateText(string name, Transform parent, string text, int size,
            TextAnchor align = TextAnchor.MiddleCenter, Color? color = null)
        {
            var rt = CreateRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color ?? TextColor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Image CreateImage(string name, Transform parent, Sprite sprite, Color? color = null)
        {
            var rt = CreateRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color ?? Color.white;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 0.25f; // border 2px tetap terlihat di layar besar
            }
            return img;
        }

        public static Button CreateButton(string name, Transform parent, Sprite sprite, string label,
            int fontSize, out Text labelText)
        {
            var img = CreateImage(name, parent, sprite, sprite == null ? new Color(0.35f, 0.25f, 0.18f) : (Color?)null);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            labelText = CreateText("Label", img.transform, label, fontSize);
            Stretch(labelText.rectTransform, 8f);
            return button;
        }

        public static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>Tempel ke sisi atas/bawah parent dengan tinggi tetap.</summary>
        public static void AnchorBand(RectTransform rt, bool top, float height, float offset = 0f, float sidePadding = 0f)
        {
            float y = top ? 1f : 0f;
            rt.anchorMin = new Vector2(0f, y);
            rt.anchorMax = new Vector2(1f, y);
            rt.pivot = new Vector2(0.5f, y);
            rt.sizeDelta = new Vector2(-sidePadding * 2f, height);
            rt.anchoredPosition = new Vector2(0f, top ? -offset : offset);
        }

        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                if (Application.isPlaying) Object.Destroy(child);
                else Object.DestroyImmediate(child);
            }
        }
    }
}
