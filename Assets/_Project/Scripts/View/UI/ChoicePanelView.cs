using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndRuin.View
{
    /// <summary>Satu tombol pilihan di panel.</summary>
    public readonly struct ChoiceOption
    {
        public readonly string Label;
        public readonly bool Enabled;

        public ChoiceOption(string label, bool enabled = true)
        {
            Label = label;
            Enabled = enabled;
        }

        public static implicit operator ChoiceOption(string label) => new ChoiceOption(label);
    }

    /// <summary>
    /// Panel modal serbaguna: judul, teks, dan daftar tombol. Dipakai untuk Dadu Kompas,
    /// pilihan ular, chest, toko, event, api unggun, hadiah boss, dan layar akhir.
    /// </summary>
    public sealed class ChoicePanelView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private RectTransform optionsContainer;
        [SerializeField] private Sprite buttonSprite;

        public bool IsOpen => root != null && root.activeSelf;

        public void Bind(GameObject panelRoot, Text title, Text body, RectTransform options, Sprite button)
        {
            root = panelRoot; titleText = title; bodyText = body; optionsContainer = options; buttonSprite = button;
        }

        private void Awake()
        {
            if (root != null) root.SetActive(false);
        }

        public void Show(string title, string body, IList<ChoiceOption> options, Action<int> onChoose)
        {
            if (root == null) return;
            root.SetActive(true);
            if (titleText != null) titleText.text = title ?? string.Empty;
            if (bodyText != null) bodyText.text = body ?? string.Empty;

            UiFactory.ClearChildren(optionsContainer);
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                var button = UiFactory.CreateButton("Option " + i, optionsContainer, buttonSprite, options[i].Label, 34, out _);
                var layout = button.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 110f;
                button.interactable = options[i].Enabled;
                button.onClick.AddListener(() =>
                {
                    Hide();
                    onChoose?.Invoke(index);
                });
            }
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
