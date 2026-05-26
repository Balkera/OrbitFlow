using UnityEngine;

namespace SquareFlow.UI
{
    public readonly struct SquareFlowTheme
    {
        public SquareFlowTheme(bool dark)
        {
            Background = dark ? new Color32(39, 34, 85, 255) : new Color32(219, 224, 255, 255);
            Panel = dark ? new Color32(58, 53, 112, 238) : new Color32(245, 246, 255, 236);
            Text = dark ? new Color32(255, 255, 255, 255) : new Color32(34, 30, 76, 255);
            SubtleText = dark ? new Color32(146, 139, 191, 255) : new Color32(88, 82, 146, 255);
            Score = dark ? new Color32(255, 220, 54, 255) : new Color32(139, 92, 0, 255);
            Red = dark ? new Color32(255, 107, 107, 255) : new Color32(218, 47, 82, 255);
            Blue = dark ? new Color32(66, 153, 225, 255) : new Color32(35, 125, 215, 255);
            Yellow = dark ? new Color32(236, 201, 75, 255) : new Color32(218, 163, 18, 255);
            Green = dark ? new Color32(72, 187, 120, 255) : new Color32(22, 160, 107, 255);
            Wild = dark ? new Color32(224, 229, 241, 255) : new Color32(245, 250, 255, 255);
            Bomb = dark ? new Color32(245, 84, 144, 255) : new Color32(224, 91, 138, 255);
            CellEmpty = dark ? new Color32(21, 18, 53, 255) : new Color32(218, 223, 245, 255);
            Border = dark ? new Color32(94, 88, 143, 255) : new Color32(160, 164, 210, 255);
            Button = dark ? new Color32(68, 91, 176, 255) : new Color32(225, 238, 244, 255);
            DockSlot = dark ? new Color32(45, 54, 83, 255) : new Color32(235, 239, 249, 255);
            Header = dark ? new Color32(25, 21, 61, 245) : new Color32(228, 232, 255, 245);
            PlayfieldPanel = dark ? new Color32(62, 57, 119, 255) : new Color32(235, 238, 255, 255);
            BoardSurface = dark ? new Color32(32, 29, 72, 255) : new Color32(208, 217, 250, 255);
            BoardInset = dark ? new Color32(78, 72, 134, 255) : new Color32(178, 184, 226, 255);
            InactiveSlot = dark ? new Color32(67, 62, 113, 255) : new Color32(205, 212, 245, 255);
            SelectedLevel = dark ? new Color32(255, 220, 54, 255) : new Color32(205, 144, 20, 255);
            TitleGlow = dark ? new Color32(104, 146, 255, 96) : new Color32(118, 150, 255, 92);
            Chip = dark ? new Color32(44, 39, 92, 255) : new Color32(220, 226, 255, 255);
            PlayButton = dark ? new Color32(84, 76, 186, 255) : new Color32(77, 133, 215, 255);
            PlayButtonAlt = dark ? new Color32(78, 166, 225, 255) : new Color32(99, 105, 203, 255);
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
        public Color Header { get; }
        public Color PlayfieldPanel { get; }
        public Color BoardSurface { get; }
        public Color BoardInset { get; }
        public Color InactiveSlot { get; }
        public Color SelectedLevel { get; }
        public Color TitleGlow { get; }
        public Color Chip { get; }
        public Color PlayButton { get; }
        public Color PlayButtonAlt { get; }
    }
}
