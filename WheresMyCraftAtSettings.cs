using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using System.Collections.Generic;
using System.Windows.Forms;
using WheresMyCraftAt.CraftingSequence;

namespace WheresMyCraftAt
{
    public class WheresMyCraftAtSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);

        public ToggleNode DebugPrint { get; set; } = new ToggleNode(true);
        public RangeNode<int> DebugPrintLingerTime { get; set; } = new RangeNode<int>(5, 0, 20);

        public HotkeyNode TestButton1 { get; set; } = Keys.NumPad6;

        public RangeNode<int> MouseMoveX { get; set; } = new RangeNode<int>(0, 0, 2560);
        public RangeNode<int> MouseMoveY { get; set; } = new RangeNode<int>(0, 0, 1440);
        public RangeNode<int> ActionTimeoutInSeconds { get; set; } = new RangeNode<int>(2, 1, 3);
        public List<CraftingSequence.CraftingSequence.CraftingStepInput> SelectedCraftingStepInputs { get; set; } = [];
    }
}