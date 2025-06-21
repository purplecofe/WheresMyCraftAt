using System;
using System.Collections.Generic;

namespace WheresMyCraftAt.CraftingMenu.CraftofExileStructs;

public class CoECurrencyDict
{
    public static readonly Dictionary<string, string> OrbNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        {"transmute", "Orb of Transmutation"},
        {"alteration", "Orb of Alteration"},
        {"augmentation", "Orb of Augmentation"},
        {"regal", "Regal Orb"},
        {"alchemy", "Orb of Alchemy"},
        {"chaos", "Chaos Orb"},
        {"exalted", "Exalted Orb"},
        {"scour", "Orb of Scouring"},
        {"annul", "Orb of Annulment"},
        {"crusader", "Crusader's Exalted Orb"},
        {"hunter", "Hunter's Exalted Orb"},
        {"redeemer", "Redeemer's Exalted Orb"},
        {"warlord", "Warlord's Exalted Orb"},
        {"blessed", "Blessed Orb"},
        {"divine", "Divine Orb"},
        {"veiled", "Veiled Orb"},
        {"woke", "Awakener's Orb"},
        {"maven", "Orb of Dominance"},
        {"fracturing", "Fracturing Orb"},
        {"vaal", "Vaal Orb"}
    };
}