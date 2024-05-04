using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using System;

namespace WheresMyCraftAt.CraftingSequence;

public class AsyncResult
{
    public bool IsSuccess { get; set; }
    public long Address { get; set; }
    public Entity Entity { get; set; }

    public AsyncResult(Tuple<bool, NormalInventoryItem> result)
    {
        IsSuccess = result.Item1;
        Address = result.Item2.Address;
        Entity = result.Item2.Item;
    }

    public AsyncResult(Tuple<bool, ServerInventory.InventSlotItem> result)
    {
        IsSuccess = result.Item1;
        Address = result.Item2.Address;
        Entity = result.Item2.Item;
    }
}