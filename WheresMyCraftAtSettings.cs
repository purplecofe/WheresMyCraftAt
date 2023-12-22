using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WheresMyCraftAt
{
    public class WheresMyCraftAtSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);

        public ToggleNode DebugPrint { get; set; } = new ToggleNode(true);
        public ToggleNode ShowLogWindow { get; set; } = new ToggleNode(false);
        public RangeNode<int> DebugPrintLingerTime { get; set; } = new RangeNode<int>(5, 0, 20);
        public HotkeyNode RunButton { get; set; } = Keys.NumPad6;
        public RangeNode<int> ActionTimeoutInSeconds { get; set; } = new RangeNode<int>(2, 1, 3);
        public List<CraftingSequence.CraftingSequence.CraftingStepInput> SelectedCraftingStepInputs { get; set; } = [];
    }
}