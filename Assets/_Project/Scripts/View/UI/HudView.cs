using System;
using RollAndRuin.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndRuin.View
{
    /// <summary>Bar atas (HP, gold, lantai, giliran), pesan, dan tombol lempar.</summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private Text hpText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text floorText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text infoText;
        [SerializeField] private Button rollButton;
        [SerializeField] private Text rollLabel;
        [SerializeField] private Button sabotageButton;
        [SerializeField] private Text sabotageLabel;
        [SerializeField] private Button menuButton;

        public event Action RollClicked;
        public event Action SabotageClicked;
        public event Action MenuClicked;

        private void Awake()
        {
            if (rollButton != null) rollButton.onClick.AddListener(() => RollClicked?.Invoke());
            if (sabotageButton != null) sabotageButton.onClick.AddListener(() => SabotageClicked?.Invoke());
            if (menuButton != null) menuButton.onClick.AddListener(() => MenuClicked?.Invoke());
        }

        public void Bind(Text hp, Text gold, Text floor, Text message, Text info, Button roll, Text rollText)
        {
            hpText = hp; goldText = gold; floorText = floor; messageText = message; infoText = info;
            rollButton = roll; rollLabel = rollText;
        }

        public void BindExtras(Button sabotage, Text sabotageText, Button menu)
        {
            sabotageButton = sabotage; sabotageLabel = sabotageText; menuButton = menu;
        }

        public void SetSabotage(bool visible, bool interactable, string label)
        {
            if (sabotageButton == null) return;
            sabotageButton.gameObject.SetActive(visible);
            sabotageButton.interactable = interactable;
            if (sabotageLabel != null) sabotageLabel.text = label;
        }

        public void SetStats(Actor player, int floorIndex, int turn)
        {
            if (player == null) return;
            if (hpText != null)
            {
                hpText.text = $"HP {player.Hp}/{player.MaxHp}" + (player.PoisonTurns > 0 ? $"  racun {player.PoisonTurns}" : "");
                hpText.color = player.Hp <= player.MaxHp / 4 ? UiFactory.DangerColor : UiFactory.TextColor;
            }
            if (goldText != null) goldText.text = $"Gold {player.Gold}";
            if (floorText != null) floorText.text = $"Lantai {floorIndex + 1}  ·  Giliran {turn}";
        }

        public void ShowMessage(string message)
        {
            if (messageText != null) messageText.text = message ?? string.Empty;
        }

        public void ShowInfo(string info)
        {
            if (infoText != null) infoText.text = info ?? string.Empty;
        }

        public void SetRoll(bool visible, string label = "LEMPAR")
        {
            if (rollButton == null) return;
            rollButton.gameObject.SetActive(visible);
            if (rollLabel != null) rollLabel.text = label;
        }
    }
}
