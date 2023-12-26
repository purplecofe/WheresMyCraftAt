using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Forms;

namespace WheresMyCraftAt;

public class WheresMyCraftAtSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(false);

    public ToggleNode DebugPrint { get; set; } = new(true);
    public ToggleNode ShowLogWindow { get; set; } = new(false);
    public ToggleNode MenuStyling { get; set; } = new(true);
    public HotkeyNode ToggleDebugWindow { get; set; } = new(Keys.NumPad3);
    public RangeNode<int> DebugPrintLingerTime { get; set; } = new(5, 0, 20);
    public HotkeyNode RunButton { get; set; } = Keys.NumPad6;
    public RangeNode<Vector2> MinMaxRandomDelay { get; set; } = new(
        new Vector2(20, 80),
        Vector2.Zero,
        new Vector2(600, 600)
    );
    public RangeNode<int> ActionTimeoutInSeconds { get; set; } = new(2, 1, 3);
    public string CraftingSequenceLastSaved { get; set; } = "";
    public string CraftingSequenceLastSelected { get; set; } = "";
    public List<CraftingSequence.CraftingSequence.CraftingStepInput> SelectedCraftingStepInputs { get; set; } = [];

    public Dictionary<Enums.WheresMyCraftAt.LogMessageType, bool> LogMessageFilters = new()
    {
        { Enums.WheresMyCraftAt.LogMessageType.Info, true },
        { Enums.WheresMyCraftAt.LogMessageType.Warning, true },
        { Enums.WheresMyCraftAt.LogMessageType.Error, true },
        { Enums.WheresMyCraftAt.LogMessageType.Critical, true },
        { Enums.WheresMyCraftAt.LogMessageType.Trace, true },
        { Enums.WheresMyCraftAt.LogMessageType.Debug, true },
        { Enums.WheresMyCraftAt.LogMessageType.Profiler, true },
        { Enums.WheresMyCraftAt.LogMessageType.Special, true }
    };
}