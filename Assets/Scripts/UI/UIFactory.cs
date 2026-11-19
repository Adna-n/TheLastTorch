// UIFactory: helper functions that build uGUI elements from code (canvas, event system, text,
// image, button, slider, input field). Used by the editor scene builders and at runtime (floating text).
// Uses the built-in legacy font so no TextMeshPro import is needed.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastTorch
{
    public static class UIFactory
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        static Font font;
        public static Font DefaultFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Canvas CreateCanvas(string name, int sortingOrder = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>EventSystem with the new Input System UI module (default actions are assigned automatically).</summary>
        public static EventSystem CreateEventSystem()
        {
            var existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null) return existing;
            var go = new GameObject("EventSystem");
            var es = go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            return es;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchors the rect to one point of its parent (0..1 coords) with a position and size.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Stretches the rect over its parent with margins.</summary>
        public static RectTransform Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Text CreateText(Transform parent, string name, string content, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = CreateRect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = CreateRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = raycast;
            return img;
        }

        public static Button CreateButton(Transform parent, string name, string label, Sprite sprite, Color background,
            Color textColor, int fontSize = 30)
        {
            var img = CreateImage(parent, name, sprite, background, true);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var text = CreateText(img.transform, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(text.rectTransform);
            img.gameObject.AddComponent<ButtonSounds>();
            return button;
        }

        public static Slider CreateSlider(Transform parent, string name, float min, float max, float value,
            Sprite sprite, Color background, Color fill, Color handle)
        {
            var root = CreateRect(name, parent);
            var slider = root.gameObject.AddComponent<Slider>();

            var bg = CreateImage(root, "Background", sprite, background);
            Stretch(bg.rectTransform, 0f, 0f, 12f, 12f);

            var fillArea = CreateRect("Fill Area", root);
            Stretch(fillArea, 6f, 6f, 14f, 14f);
            var fillImg = CreateImage(fillArea, "Fill", sprite, fill);
            fillImg.rectTransform.sizeDelta = Vector2.zero;

            var handleArea = CreateRect("Handle Slide Area", root);
            Stretch(handleArea, 14f, 14f, 0f, 0f);
            var handleImg = CreateImage(handleArea, "Handle", sprite, handle, true);
            handleImg.rectTransform.sizeDelta = new Vector2(28f, 0f);

            slider.fillRect = fillImg.rectTransform;
            slider.handleRect = handleImg.rectTransform;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            return slider;
        }

        public static InputField CreateInputField(Transform parent, string name, string placeholder, Sprite sprite,
            Color background, Color textColor, int fontSize, int characterLimit)
        {
            var bg = CreateImage(parent, name, sprite, background, true);
            var field = bg.gameObject.AddComponent<InputField>();

            var text = CreateText(bg.transform, "Text", string.Empty, fontSize, textColor, TextAnchor.MiddleLeft);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(text.rectTransform, 20f, 20f, 4f, 4f);

            var ph = CreateText(bg.transform, "Placeholder", placeholder, fontSize, new Color(textColor.r, textColor.g, textColor.b, 0.4f),
                TextAnchor.MiddleLeft, FontStyle.Italic);
            Stretch(ph.rectTransform, 20f, 20f, 4f, 4f);

            field.textComponent = text;
            field.placeholder = ph;
            field.targetGraphic = bg;
            field.characterLimit = characterLimit;
            field.lineType = InputField.LineType.SingleLine;
            field.contentType = InputField.ContentType.Standard;
            return field;
        }

        /// <summary>Fake glow around text using an outline + soft shadow.</summary>
        public static void AddGlow(Graphic graphic, Color color, float distance)
        {
            var outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(color.r, color.g, color.b, color.a * 0.6f);
            shadow.effectDistance = new Vector2(-distance * 1.5f, distance * 1.5f);
        }
    }
}
