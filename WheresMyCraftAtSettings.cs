using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;
using System.Collections.Generic;
using System.Windows.Forms;
using Vector2 = System.Numerics.Vector2;

namespace WheresMyCraftAt;

public class WheresMyCraftAtSettings : ISettings
{
    public RunOptions RunOptions { get; set; } = new();
    public DelayOptions DelayOptions { get; set; } = new();
    public DebugOptions Debugging { get; set; } = new();
    public StylingDooDads Styling { get; set; } = new();
    public NonUser NonUserData { get; set; } = new();
    public ToggleNode Enable { get; set; } = new(false);
}

[Submenu(CollapsedByDefault = false)]
public class RunOptions
{
    public HotkeyNode RunButton { get; set; } = Keys.NumPad6;
}

public class NonUser
{
    public string CraftingSequenceLastSaved { get; set; } = "";
    public string CraftingSequenceLastSelected { get; set; } = "";
    public List<CraftingSequence.CraftingSequence.CraftingStepInput> SelectedCraftingStepInputs { get; set; } = [];
}

[Submenu(CollapsedByDefault = false)]
public class DelayOptions
{
    public RangeNode<Vector2> MinMaxRandomDelayMS { get; set; } = new(
        new Vector2(5, 15),
        new Vector2(1, 2),
        new Vector2(999, 1000)
    );
    public RangeNode<Vector2> MinMaxButtonDownUpDelayMS { get; set; } = new(
        new Vector2(5, 15),
        new Vector2(1, 2),
        new Vector2(999, 1000)
    );

    public RangeNode<int> ActionTimeoutInSeconds { get; set; } = new(2, 1, 3);
}

[Submenu(CollapsedByDefault = false)]
public class DebugOptions
{
    public Dictionary<Enums.WheresMyCraftAt.LogMessageType, (bool enabled, Color color)> LogMessageFilters = [];

    public ToggleNode LogWindow { get; set; } = new(false);
    public HotkeyNode ToggleLogWindow { get; set; } = new(Keys.NumPad3);
    public ToggleNode PrintTopLeft { get; set; } = new(true);
    public RangeNode<int> PrintLingerTime { get; set; } = new(5, 0, 20);
}

[Submenu(CollapsedByDefault = true)]
public class StylingDooDads
{
    public ToggleNode CustomMenuStyling { get; set; } = new(true);
    public RemovalButtonStyle RemovalButtons { get; set; } = new();
    public AdditionButtonStyle AdditionButtons { get; set; } = new();
    public ConditionalGroupStyle ConditionGroupBackgrounds { get; set; } = new();
}

[Submenu]
public class RemovalButtonStyle
{
    public ColorNode Normal { get; set; } = new(new Color(250, 66, 66, 102));
    public ColorNode Hovered { get; set; } = new(new Color(250, 66, 66, 255));
    public ColorNode Active { get; set; } = new(new Color(250, 15, 15, 255));
}

[Submenu]
public class AdditionButtonStyle
{
    public ColorNode Normal { get; set; } = new(new Color(66, 250, 66, 102));
    public ColorNode Hovered { get; set; } = new(new Color(66, 250, 66, 150));
    public ColorNode Active { get; set; } = new(new Color(15, 250, 15, 200));
}

[Submenu]
public class ConditionalGroupStyle
{
    public ColorNode And { get; set; } = new(new Color(76, 209, 65, 18));
    public ColorNode Or { get; set; } = new(new Color(76, 100, 209, 30));
    public ColorNode Not { get; set; } = new(new Color(209, 76, 65, 18));
}