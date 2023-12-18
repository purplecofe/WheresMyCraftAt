using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using System;

namespace WheresMyCraftAt.Handlers
{
    public static class ElementHandler
    {
        private static GameController GC;

        public static void Initialize(WheresMyCraftAt main)
        {
            GC = main.GameController;
        }

        public static bool IsIngameUiElementOpenCondition(Func<IngameUIElements, Element> panelSelector) =>
            panelSelector(GC?.Game?.IngameState?.IngameUi)?.IsVisible ?? false;

        public static bool TryGetCursorStateCondition(out MouseActionType cursorState) =>
            (cursorState = GC?.Game?.IngameState?.IngameUi?.Cursor?.Action ?? MouseActionType.Free) != MouseActionType.Free;
    }
}