using ExileCore;
using ExileCore.Shared;
using ExileCore.Shared.Nodes;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using WheresMyCraftAt.CraftingSequence;
using WheresMyCraftAt.Handlers;
using static WheresMyCraftAt.CraftingSequence.CraftingSequence;
using Vector2N = System.Numerics.Vector2;

namespace WheresMyCraftAt;

public class WheresMyCraftAt : BaseSettingsPlugin<WheresMyCraftAtSettings>
{
    public static WheresMyCraftAt Main;

    public readonly Dictionary<Enums.WheresMyCraftAt.SpecialSlot, Vector2N> SpecialSlotDimensionMap = new()
    {
        { Enums.WheresMyCraftAt.SpecialSlot.CurrencyTab, new Vector2N(126f, 252f) },
        { Enums.WheresMyCraftAt.SpecialSlot.EssenceTab, new Vector2N(127.2f, 254.4f) }
    };

    public Vector2 ClickWindowOffset;
    public SyncTask<bool> CurrentOperation;
    public CancellationTokenSource OperationCts;
    public List<CraftingStep> SelectedCraftingSteps = [];
    public int ServerLatency;

    public WheresMyCraftAt()
    {
        Name = "Wheres My Craft At";
        OperationCts = new CancellationTokenSource();
    }

    public override bool Initialise()
    {
        Main = this;
        RegisterHotkey(Settings.RunButton);
        return true;
    }

    private static void RegisterHotkey(HotkeyNode hotkey)
    {
        Input.RegisterKey(hotkey);
        hotkey.OnValueChanged += () => Input.RegisterKey(hotkey);
    }

    public override Job Tick()
    {
        ClickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
        ServerLatency = GameController.IngameState.ServerData.Latency;

        if (!GameHandler.IsInGameCondition())
        {
            Stop();
            return null;
        }

        if (Settings.RunButton.PressedOnce())
        {
            if (CurrentOperation is not null)
            {
                // Immediate cancellation called, release all buttons.
                // TODO: Get some help on how the hell this works.
                Stop();
            }
            else
            {
                Logging.Logging.Add(
                    $"{Name}: Attempting to Start New Operation.",
                    Enums.WheresMyCraftAt.LogMessageType.Info
                );

                ResetCancellationTokenSource();
                CurrentOperation = AsyncStart(OperationCts.Token);
            }
        }

        if (CurrentOperation is not null)
            TaskUtils.RunOrRestart(ref CurrentOperation, () => null);

        return null;
    }

    public void Stop()
    {
        if (CurrentOperation is null)
            return;

        CurrentOperation = null;

        var keysToRelease = new List<Keys>
        {
            Keys.LControlKey,
            Keys.ShiftKey,
            Keys.LButton,
            Keys.RButton
        };

        foreach (var key in keysToRelease.Where(Input.GetKeyState))
            Input.KeyUp(key);

        if (ItemHandler.IsItemRightClickedCondition())
            Input.KeyPressRelease(Keys.Escape);

        Logging.Logging.Add($"{Name}: Stop() has been ran.", Enums.WheresMyCraftAt.LogMessageType.Warning);
    }

    private void ResetCancellationTokenSource()
    {
        if (OperationCts != null)
        {
            if (!OperationCts.IsCancellationRequested)
                OperationCts.Cancel();

            OperationCts.Dispose();
        }

        OperationCts = new CancellationTokenSource();
    }

    private async SyncTask<bool> AsyncStart(CancellationToken token)
    {
        if (!GameHandler.IsInGameCondition())
        {
            Logging.Logging.Add(
                $"{Name}: Not in game, operation will be terminated.",
                Enums.WheresMyCraftAt.LogMessageType.Error
            );

            return false;
        }

        if (Settings.SelectedCraftingStepInputs.Count == 0 ||
            Settings.SelectedCraftingStepInputs.Any(x => string.IsNullOrEmpty(x.CurrencyItem)))
        {
            Logging.Logging.Add(
                $"{Name}: No Crafting Steps or currency to use is null, operation will be terminated.",
                Enums.WheresMyCraftAt.LogMessageType.Error
            );

            return false;
        }

        try
        {
            var isInvOpen = await InventoryHandler.AsyncWaitForInventoryOpen(token);
            var isStashOpen = await StashHandler.AsyncWaitForStashOpen(token);

            if (!isStashOpen || !isInvOpen)
                return false;

            var giveItems = new CraftingSequenceExecutor(SelectedCraftingSteps);

            if (!await giveItems.Execute(OperationCts.Token))
                return false;

            Logging.Logging.Add($"{Name}: AsyncStart() Completed.", Enums.WheresMyCraftAt.LogMessageType.Success);
        }
        catch (OperationCanceledException)
        {
            Stop();
            return false;
        }

        return true;
    }

    public override void DrawSettings()
    {
        base.DrawSettings();
        CraftingSequenceMenu.Draw();
    }

    public override void Render()
    {
        base.Render();
        Logging.Logging.Render();
    }
}