using UnityEngine;

namespace SquareFlow.UI
{
    public readonly struct SquareFlowTheme
    {
        public SquareFlowTheme(bool dark)
        {
            Background = dark ? new Color32(13, 22, 31, 255) : new Color32(224, 241, 247, 255);
            Panel = dark ? new Color32(36, 48, 57, 238) : new Color32(250, 255, 255, 232);
            Text = dark ? new Color32(232, 238, 242, 255) : new Color32(12, 34, 48, 255);
            SubtleText = dark ? new Color32(171, 184, 193, 255) : new Color32(58, 100, 114, 255);
            Score = dark ? new Color32(255, 219, 64, 255) : new Color32(148, 92, 0, 255);
            Red = dark ? new Color32(255, 82, 106, 255) : new Color32(218, 47, 82, 255);
            Blue = dark ? new Color32(52, 176, 232, 255) : new Color32(27, 127, 213, 255);
            Yellow = dark ? new Color32(255, 207, 73, 255) : new Color32(218, 163, 18, 255);
            Green = dark ? new Color32(43, 210, 148, 255) : new Color32(22, 160, 107, 255);
            Wild = dark ? new Color32(232, 238, 242, 255) : new Color32(245, 250, 255, 255);
            Bomb = dark ? new Color32(255, 219, 64, 255) : new Color32(224, 111, 18, 255);
            CellEmpty = dark ? new Color32(7, 16, 23, 255) : new Color32(218, 235, 241, 255);
            Border = dark ? new Color32(82, 96, 106, 255) : new Color32(159, 187, 198, 255);
            Button = dark ? new Color32(77, 91, 103, 255) : new Color32(225, 238, 244, 255);
            DockSlot = dark ? new Color32(50, 60, 69, 255) : new Color32(235, 245, 249, 255);
        }

        public Color Background { get; }
        public Color Panel { get; }
        public Color Text { get; }
        public Color SubtleText { get; }
        public Color Score { get; }
        public Color Red { get; }
        public Color Blue { get; }
        public Color Yellow { get; }
        public Color Green { get; }
        public Color Wild { get; }
        public Color Bomb { get; }
        public Color CellEmpty { get; }
        public Color Border { get; }
        public Color Button { get; }
        public Color DockSlot { get; }
    }
}
