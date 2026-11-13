using System;
using System.Collections.Generic;
using System.Text;
using RollAndRuin.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndRuin.View
{
    [Serializable]
    public struct DieSkin
    {
        public string dieId;
        public Sprite icon;
    }

    /// <summary>
    /// Tangan dadu di zona jempol. Tap sekali = pilih (preview), tap lagi = lempar.
    /// </summary>
    public sealed class DiceHandView : MonoBehaviour
    {
        [SerializeField] private RectTransform container;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite defaultIcon;
        [SerializeField] private DieSkin[] skins = Array.Empty<DieSkin>();

        private static readonly Color SelectedColor = new Color(1f, 0.85f, 0.4f);
        private static readonly Color NormalColor = Color.white;
        private static readonly Color CooldownColor = new Color(0.45f, 0.45f, 0.45f);

        private readonly List<Button> _buttons = new List<Button>();

        /// <summary>Dipanggil saat pemain tap dadu ke-i.</summary>
        public event Action<int> DieClicked;

        public void Refresh(DiceHand hand, int selected, bool interactable, Func<int, bool> canUse = null)
        {
            if (container == null) container = (RectTransform)transform;
            UiFactory.ClearChildren(container);
            _buttons.Clear();
            if (hand == null) return;

            for (int i = 0; i < hand.Count; i++)
            {
                int index = i;
                var die = hand.Dice[i];
                bool usable = canUse != null ? canUse(i) : die.IsReady;

                var button = UiFactory.CreateButton("Die " + i, container, buttonSprite, string.Empty, 28, out var label);
                var img = (Image)button.targetGraphic;
                img.color = !die.IsReady ? CooldownColor : (i == selected ? SelectedColor : NormalColor);

                var icon = UiFactory.CreateImage("Icon", button.transform, FindIcon(die.Id));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var irt = icon.rectTransform;
                irt.anchorMin = new Vector2(0.2f, 0.42f);
                irt.anchorMax = new Vector2(0.8f, 0.92f);
                irt.offsetMin = irt.offsetMax = Vector2.zero;

                label.text = Describe(die);
                label.alignment = TextAnchor.LowerCenter;
                label.fontSize = 26;

                button.interactable = interactable && usable;
                button.onClick.AddListener(() => DieClicked?.Invoke(index));
                _buttons.Add(button);
            }
        }

        private Sprite FindIcon(string dieId)
        {
            foreach (var skin in skins)
                if (skin.dieId == dieId) return skin.icon;
            return defaultIcon;
        }

        private static string Describe(DiceModel die)
        {
            var sb = new StringBuilder();
            sb.Append(die.Id.ToUpperInvariant()).Append('\n');
            if (die.Special == DieSpecial.Compass)
            {
                sb.Append("pilih ").Append(die.CompassMin).Append('–').Append(die.CompassMax);
            }
            else
            {
                for (int f = 0; f < die.Faces.Count; f++)
                {
                    if (f > 0) sb.Append(' ');
                    sb.Append(die.Faces[f].Value);
                    if (die.Faces[f].HasEffect) sb.Append('*');
                }
            }
            if (!die.IsReady) sb.Append("\ncooldown ").Append(die.CurrentCooldown);
            return sb.ToString();
        }
    }
}
