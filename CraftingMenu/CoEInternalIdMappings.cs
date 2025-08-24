using System;
using System.Collections.Generic;
using System.Linq;

namespace WheresMyCraftAt.CraftingMenu;

/// <summary>
/// 大規模 CoE 模組 ID 到 PoE 內部 ID 映射表
/// 分類管理不同類型的詞綴映射
/// </summary>
public static class CoEInternalIdMappings
{
    /// <summary>
    /// 詞綴分類
    /// </summary>
    public enum ModCategory
    {
        Weapon,      // 武器詞綴 (LocalIncreasedPhysicalDamagePercent 等)
        Armour,      // 護甲詞綴 (LocalIncreasedEnergyShieldPercent 等)  
        Accessory,   // 飾品詞綴 (IncreasedLife, AllResistances 等)
        Flask,       // Flask 詞綴 (FlaskEffectReducedDuration 等)
        Jewel,       // 珠寶詞綴 (PassiveSkillsInRadiusAreConvertedToStrength 等)
        Special      // 特殊詞綴 (open_prefix, open_suffix 等)
    }

    /// <summary>
    /// Flask 類型分類
    /// </summary>
    public enum FlaskType
    {
        Utility,     // Utility Flask (Diamond, Ruby, Topaz, etc.)
        Life,        // Life Flask
        Mana,        // Mana Flask
        Hybrid       // Hybrid Flask (Life + Mana)
    }

    // 暫時註解掉其他分類，專注於 Flask 詞綴
    // TODO: 未來擴展時啟用
    /*
    private static readonly Dictionary<string, string> WeaponMods = new();
    private static readonly Dictionary<string, string> ArmourMods = new();
    */

    /// <summary>
    /// Flask 詞綴映射 - 完整的 Flask 詞綴覆蓋系統
    /// 參考資料：
    /// - https://www.poewiki.net/wiki/List_of_modifiers_for_utility_flasks
    /// - https://www.poewiki.net/wiki/List_of_modifiers_for_life_flasks
    /// - https://www.poewiki.net/wiki/List_of_modifiers_for_mana_flasks
    /// </summary>
    private static readonly Dictionary<string, string> FlaskMods = new()
    {
        // === 已驗證成功的映射 ===
        ["1654"] = "FlaskEffectReducedDuration",             // #% reduced Duration, #% increased effect
        ["3887"] = "FlaskBuffCurseEffect",                   // #% reduced Effect of Curses on you during Effect
        
        // === Utility Flask Prefix 詞綴 ===
        
        // 減少充能消耗
        ["4001"] = "FlaskChargesUsed",                       // #% reduced Charges per use ("Apprentice's", "Scholar's", "Practitioner's", "Brewer's", "Chemist's")
        
        // 暴擊充能
        ["4002"] = "FlaskChanceRechargeOnCrit",              // #% chance to gain Flask Charge on Critical Strike ("Medic's", "Physician's", "Doctor's", "Surgeon's")
        
        // 增加持續時間
        ["4003"] = "FlaskIncreasedDuration",                 // #% increased Duration ("Investigator's", "Analyst's")
        
        // 額外充能數量
        ["4004"] = "FlaskExtraCharges",                      // +# Maximum Charges (各種層級)
        
        // 充能恢復速度
        ["4017"] = "FlaskIncreasedRecoveryRate",             // #% increased Charge Recovery ("Experimenter's")
        ["4018"] = "FlaskReducedRecoveryRateIncreasedEffect", // #% reduced Charge Recovery, #% increased effect
        
        // 被擊中時充能
        ["4019"] = "FlaskChargeGainedWhenHit",               // Gain # Flask Charge when you take a Critical Strike
        
        // === Utility Flask Suffix 詞綴 ===
        
        // 防禦效果
        ["4005"] = "FlaskIncreasedArmour",                   // #% increased Armour during Effect ("of the Abalone")
        ["4006"] = "FlaskIncreasedEvasion",                  // #% increased Evasion Rating during Effect ("of the Gazelle")
        ["4007"] = "FlaskElementalResistances",              // +#% to all Elemental Resistances during Effect ("of the Crystal")
        
        // 基本免疫效果
        ["4008"] = "FlaskImmunityPoison",                    // Immunity to Poison during Effect ("of the Skunk")
        ["4009"] = "FlaskImmunityFreezeChill",               // Immunity to Freeze and Chill during Effect ("of the Deer")
        ["4010"] = "FlaskImmunityIgnite",                    // Immunity to Ignite during Effect, Removes Burning ("of the Urchin")
        ["4011"] = "FlaskImmunityBleeding",                  // Immunity to Bleeding during Effect ("of the Staunching")
        ["4012"] = "FlaskImmunityFreeze",                    // Immunity to Freeze during Effect ("of the Heat")
        ["4013"] = "FlaskImmunityShock",                     // Immunity to Shock during Effect ("of the Grounding")
        
        // 進階免疫和避免效果
        ["4020"] = "FlaskDispellMaim",                       // Immunity to Maim during Effect ("of the Cheetah")
        ["4021"] = "FlaskDispellHinder",                     // Immunity to Hinder during Effect
        ["4022"] = "FlaskAvoidChill",                        // #% chance to Avoid being Chilled during Effect ("of the Seal")
        ["4023"] = "FlaskAvoidIgnite",                       // #% chance to Avoid being Ignited during Effect ("of the Dolphin")
        ["4024"] = "FlaskAvoidShock",                        // #% chance to Avoid being Shocked during Effect ("of the Penguin")
        ["4025"] = "FlaskAvoidStun",                         // #% chance to Avoid being Stunned during Effect ("of the Turtle")
        
        // 減少效果系列
        ["4026"] = "FlaskReducedChillEffect",                // #% reduced Effect of Chill on you during Effect ("of the Walrus")
        ["4027"] = "FlaskReducedShockEffect",                // #% reduced Effect of Shock on you during Effect ("of the Grounding")
        
        // 格擋和暈眩恢復
        ["4028"] = "FlaskBlockAndStunRecovery",              // #% increased Block and Stun Recovery during Effect ("of the Rhino")
        
        // 偷取效果
        ["4029"] = "FlaskSpellDamageLeechedAsEnergyShield",  // #% of Spell Damage Leeched as Energy Shield during Effect ("of the Lamprey")
        ["4030"] = "FlaskAttackDamageLeechedAsLife",         // #% of Attack Damage Leeched as Life during Effect ("of the Leech")
        
        // 元素機率效果
        ["4031"] = "FlaskChanceToFreeze",                    // #% chance to Freeze enemies on Hit during Effect ("of the Yak")
        ["4032"] = "FlaskChanceToShock",                     // #% chance to Shock enemies on Hit during Effect ("of the Eel")
        ["4033"] = "FlaskChanceToIgnite",                    // #% chance to Ignite enemies on Hit during Effect ("of the Newt")
        
        // 攻擊和移動增益
        ["4014"] = "FlaskIncreasedMovementSpeed",            // #% increased Movement Speed during Effect ("of the Cheetah")
        ["4015"] = "FlaskIncreasedAttackCastSpeed",          // #% increased Attack and Cast Speed during Effect ("of the Mongoose")
        ["4016"] = "FlaskIncreasedCriticalStrikeChance",     // #% increased Critical Strike Chance during Effect ("of the Falcon")
        
        // === Life Flask 詞綴 ===
        
        // Life Flask Prefix
        ["4034"] = "FlaskInstantRecovery",                   // Instant Recovery ("Seething")
        ["4035"] = "FlaskPartialInstantRecovery",            // Recovers #% of Life instantly, #% over # seconds ("Bubbling", "Effervescent")
        ["4036"] = "FlaskInstantRecoveryOnLowLife",          // Instant Recovery when on Low Life ("Panicked", "Terrified", "Alarmed")
        ["4037"] = "FlaskIncreasedLifeRecoveryRate",         // #% increased Life Recovery Rate ("Catalysed", "Condensed", "Viscous")
        ["4038"] = "FlaskIncreasedRecoveryAmount",           // #% increased Amount Recovered ("Abundant", "Saturated", "Overflowing")
        ["4039"] = "FlaskIncreasedRecoveryOnLowLife",        // #% increased Recovery when on Low Life ("Sanctified")
        ["4040"] = "FlaskExtraLifeCostsMana",                // Recovers #% more Life, Consumes # Mana per second ("Hallowed")
        
        // Life Flask Suffix
        ["4041"] = "FlaskDispellPoison",                     // Immunity to Poison during Effect, Removes Poison on use ("of the Antidote")
        ["4042"] = "FlaskCurseImmunity",                     // Immunity to Curses during Effect ("of the Warding")
        ["4043"] = "LocalFlaskImmuneToHinder",               // Immunity to Hinder during Effect ("of the Cheetah")
        ["4044"] = "FlaskHealsOthers",                       // #% of Recovery applied to Minions ("of Sharing")
        ["4045"] = "FlaskDispellChill",                      // Removes Chill and Freeze when used, Immunity to Chill and Freeze during Effect ("of the Heat")
        ["4046"] = "FlaskRemovesShock",                      // Removes Shock when used, Immunity to Shock during Effect ("of the Grounding")
        ["4047"] = "FlaskDispellBurning",                    // Removes Burning when used, Immunity to Ignite during Effect ("of the Dousing")
        ["4048"] = "FlaskRemovesBleeding",                   // Removes Bleeding when used, Immunity to Bleeding during Effect ("of the Staunching")
        ["4049"] = "LocalLifeFlaskAdditionalLifeRecovery",   // #% of Life Recovery from Flasks also applies as Energy Shield Recovery ("of the Troll")
        
        // === Mana Flask 詞綴 ===
        
        // Mana Flask Prefix (與 Life Flask 共享大部分 Prefix)
        ["4050"] = "FlaskManaInstantRecovery",               // Instant Recovery ("Seething" for Mana)
        ["4051"] = "FlaskManaPartialInstantRecovery",        // Recovers #% of Mana instantly, #% over # seconds
        ["4052"] = "FlaskIncreasedManaRecoveryRate",         // #% increased Mana Recovery Rate
        ["4053"] = "FlaskIncreasedManaRecoveryAmount",       // #% increased Mana Recovered
        
        // Mana Flask Suffix
        ["4054"] = "LocalManaFlaskHinderNearbyEnemies",      // Hinder nearby Enemies during Effect ("of Hindering")
        ["4055"] = "LocalManaFlaskAdditionalManaRecovery",   // #% of Mana Recovery from Flasks also applies as Life Recovery
        
        // === Hybrid Flask 詞綴 ===
        
        ["4056"] = "FlaskHybridLifeManaRecovery",            // Recovers both Life and Mana
        ["4057"] = "FlaskHybridInstantRecovery",             // Instant Recovery of both Life and Mana
    };

    /// <summary>
    /// 獲取 CoE 模組 ID 對應的內部 ID 模式
    /// </summary>
    /// <param name="coeModId">CoE 模組 ID</param>
    /// <returns>內部 ID 模式，如果找不到則返回 null</returns>
    public static string? GetInternalIdPattern(string coeModId)
    {
        // 目前只支援 Flask 詞綴
        if (FlaskMods.TryGetValue(coeModId, out var flaskId))
            return flaskId;
            
        return null;
    }

    /// <summary>
    /// 獲取 CoE 模組 ID 的分類
    /// </summary>
    /// <param name="coeModId">CoE 模組 ID</param>
    /// <returns>詞綴分類，如果找不到則返回 null</returns>
    public static ModCategory? GetModCategory(string coeModId)
    {
        if (FlaskMods.ContainsKey(coeModId))
            return ModCategory.Flask;
            
        return null;
    }

    /// <summary>
    /// 獲取已映射的 CoE ID 總數
    /// </summary>
    /// <returns>映射總數</returns>
    public static int GetMappedCount()
    {
        return FlaskMods.Count;
    }

    /// <summary>
    /// 獲取各分類的映射統計
    /// </summary>
    /// <returns>分類統計字典</returns>
    public static Dictionary<ModCategory, int> GetCategoryStats()
    {
        return new Dictionary<ModCategory, int>
        {
            [ModCategory.Weapon] = 0,    // TODO: 待實作
            [ModCategory.Armour] = 0,    // TODO: 待實作  
            [ModCategory.Flask] = FlaskMods.Count,
            [ModCategory.Accessory] = 0, // TODO: 待實作
            [ModCategory.Jewel] = 0,     // TODO: 待實作
            [ModCategory.Special] = 0,   // TODO: 待實作
        };
    }

    /// <summary>
    /// 獲取所有已映射的 CoE ID 列表
    /// </summary>
    /// <returns>CoE ID 列表</returns>
    public static List<string> GetAllMappedIds()
    {
        var allIds = new List<string>(FlaskMods.Keys);
        return allIds.OrderBy(x => x).ToList();
    }

    /// <summary>
    /// 檢查是否為已知的 Flask CoE ID
    /// </summary>
    /// <param name="coeModId">CoE 模組 ID</param>
    /// <returns>是否為已映射的 Flask 詞綴</returns>
    public static bool IsKnownFlaskMod(string coeModId)
    {
        return FlaskMods.ContainsKey(coeModId);
    }

    /// <summary>
    /// 獲取 Flask 詞綴的描述
    /// </summary>
    /// <param name="coeModId">CoE 模組 ID</param>
    /// <returns>詞綴描述，如果找不到則返回 null</returns>
    public static string? GetFlaskModDescription(string coeModId)
    {
        if (!FlaskMods.TryGetValue(coeModId, out var internalId))
            return null;

        // 根據內部 ID 返回友善的描述
        return internalId switch
        {
            // 已驗證映射
            "FlaskEffectReducedDuration" => "減少持續時間，增加效果",
            "FlaskBuffCurseEffect" => "減少詛咒效果",
            
            // Utility Flask Prefix
            "FlaskChargesUsed" => "減少充能消耗",
            "FlaskChanceRechargeOnCrit" => "暴擊時充能",
            "FlaskIncreasedDuration" => "增加持續時間",
            "FlaskExtraCharges" => "額外充能數量",
            "FlaskIncreasedRecoveryRate" => "增加充能恢復速度",
            "FlaskReducedRecoveryRateIncreasedEffect" => "減少充能恢復但增加效果",
            "FlaskChargeGainedWhenHit" => "被暴擊時獲得充能",
            
            // Utility Flask Suffix - 防禦
            "FlaskIncreasedArmour" => "增加護甲",
            "FlaskIncreasedEvasion" => "增加閃避",
            "FlaskElementalResistances" => "增加元素抗性",
            
            // 基本免疫效果
            "FlaskImmunityPoison" => "中毒免疫",
            "FlaskImmunityFreezeChill" => "冰凍冰緩免疫",
            "FlaskImmunityIgnite" => "點燃免疫",
            "FlaskImmunityBleeding" => "流血免疫",
            "FlaskImmunityFreeze" => "冰凍免疫",
            "FlaskImmunityShock" => "感電免疫",
            
            // 進階免疫和避免
            "FlaskDispellMaim" => "致殘免疫",
            "FlaskDispellHinder" => "阻礙免疫",
            "FlaskAvoidChill" => "避免冰緩",
            "FlaskAvoidIgnite" => "避免點燃",
            "FlaskAvoidShock" => "避免感電",
            "FlaskAvoidStun" => "避免暈眩",
            
            // 減少效果系列
            "FlaskReducedChillEffect" => "減少冰緩效果",
            "FlaskReducedShockEffect" => "減少感電效果",
            
            // 其他增益
            "FlaskBlockAndStunRecovery" => "增加格擋和暈眩恢復",
            "FlaskSpellDamageLeechedAsEnergyShield" => "法術傷害偷取為能量護盾",
            "FlaskAttackDamageLeechedAsLife" => "攻擊傷害偷取為生命",
            
            // 元素機率效果
            "FlaskChanceToFreeze" => "冰凍敵人機率",
            "FlaskChanceToShock" => "感電敵人機率",
            "FlaskChanceToIgnite" => "點燃敵人機率",
            
            // 攻擊和移動
            "FlaskIncreasedMovementSpeed" => "增加移動速度",
            "FlaskIncreasedAttackCastSpeed" => "增加攻擊和施法速度",
            "FlaskIncreasedCriticalStrikeChance" => "增加暴擊率",
            
            // Life Flask 詞綴
            "FlaskInstantRecovery" => "瞬間恢復",
            "FlaskPartialInstantRecovery" => "部分瞬間恢復",
            "FlaskInstantRecoveryOnLowLife" => "低血時瞬間恢復",
            "FlaskIncreasedLifeRecoveryRate" => "增加生命恢復速度",
            "FlaskIncreasedRecoveryAmount" => "增加恢復量",
            "FlaskIncreasedRecoveryOnLowLife" => "低血時增加恢復",
            "FlaskExtraLifeCostsMana" => "額外生命恢復消耗魔力",
            "FlaskDispellPoison" => "解除中毒",
            "FlaskCurseImmunity" => "詛咒免疫",
            "LocalFlaskImmuneToHinder" => "阻礙免疫",
            "FlaskHealsOthers" => "治療召喚物",
            "FlaskDispellChill" => "解除冰緩",
            "FlaskRemovesShock" => "解除感電",
            "FlaskDispellBurning" => "解除燃燒",
            "FlaskRemovesBleeding" => "解除流血",
            "LocalLifeFlaskAdditionalLifeRecovery" => "生命恢復也套用到能量護盾",
            
            // Mana Flask 詞綴
            "FlaskManaInstantRecovery" => "魔力瞬間恢復",
            "FlaskManaPartialInstantRecovery" => "魔力部分瞬間恢復",
            "FlaskIncreasedManaRecoveryRate" => "增加魔力恢復速度",
            "FlaskIncreasedManaRecoveryAmount" => "增加魔力恢復量",
            "LocalManaFlaskHinderNearbyEnemies" => "阻礙附近敵人",
            "LocalManaFlaskAdditionalManaRecovery" => "魔力恢復也套用到生命",
            
            // Hybrid Flask 詞綴
            "FlaskHybridLifeManaRecovery" => "生命魔力混合恢復",
            "FlaskHybridInstantRecovery" => "生命魔力瞬間恢復",
            
            _ => internalId
        };
    }

    /// <summary>
    /// 獲取詞綴適用的 Flask 類型
    /// </summary>
    /// <param name="coeModId">CoE 模組 ID</param>
    /// <returns>Flask 類型，如果找不到則返回 null</returns>
    public static FlaskType? GetFlaskTypeForMod(string coeModId)
    {
        if (!FlaskMods.TryGetValue(coeModId, out var internalId))
            return null;

        // 根據內部 ID 判斷 Flask 類型
        return internalId switch
        {
            // Life Flask 特有詞綴
            var id when id.Contains("FlaskInstantRecovery") || 
                       id.Contains("FlaskPartialInstantRecovery") ||
                       id.Contains("FlaskInstantRecoveryOnLowLife") ||
                       id.Contains("FlaskIncreasedLifeRecoveryRate") ||
                       id.Contains("FlaskIncreasedRecoveryAmount") ||
                       id.Contains("FlaskIncreasedRecoveryOnLowLife") ||
                       id.Contains("FlaskExtraLifeCostsMana") ||
                       id.Contains("FlaskDispellPoison") ||
                       id.Contains("FlaskCurseImmunity") ||
                       id.Contains("FlaskHealsOthers") ||
                       id.Contains("FlaskDispellChill") ||
                       id.Contains("FlaskRemovesShock") ||
                       id.Contains("FlaskDispellBurning") ||
                       id.Contains("FlaskRemovesBleeding") ||
                       id.Contains("LocalLifeFlaskAdditionalLifeRecovery") => FlaskType.Life,

            // Mana Flask 特有詞綴
            var id when id.Contains("FlaskManaInstantRecovery") ||
                       id.Contains("FlaskManaPartialInstantRecovery") ||
                       id.Contains("FlaskIncreasedManaRecoveryRate") ||
                       id.Contains("FlaskIncreasedManaRecoveryAmount") ||
                       id.Contains("LocalManaFlaskHinderNearbyEnemies") ||
                       id.Contains("LocalManaFlaskAdditionalManaRecovery") => FlaskType.Mana,

            // Hybrid Flask 詞綴
            var id when id.Contains("FlaskHybridLifeManaRecovery") ||
                       id.Contains("FlaskHybridInstantRecovery") => FlaskType.Hybrid,

            // Utility Flask 詞綴 (所有其他詞綴)
            _ => FlaskType.Utility
        };
    }

    /// <summary>
    /// 檢查是否為 Life Flask 詞綴
    /// </summary>
    /// <param name="coeModId">CoE 模組 ID</param>
    /// <returns>是否為 Life Flask 詞綴</returns>
    public static bool IsLifeFlaskMod(string coeModId) => GetFlaskTypeForMod(coeModId) == FlaskType.Life;

    /// <summary>
    /// 檢查是否為 Mana Flask 詞綴
    /// </summary>
    /// <param name="coeModId">CoE 模組 ID</param>
    /// <returns>是否為 Mana Flask 詞綴</returns>
    public static bool IsManaFlaskMod(string coeModId) => GetFlaskTypeForMod(coeModId) == FlaskType.Mana;

    /// <summary>
    /// 檢查是否為 Utility Flask 詞綴
    /// </summary>
    /// <param name="coeModId">CoE 模組 ID</param>
    /// <returns>是否為 Utility Flask 詞綴</returns>
    public static bool IsUtilityFlaskMod(string coeModId) => GetFlaskTypeForMod(coeModId) == FlaskType.Utility;

    /// <summary>
    /// 檢查是否為 Hybrid Flask 詞綴
    /// </summary>
    /// <param name="coeModId">CoE 模組 ID</param>
    /// <returns>是否為 Hybrid Flask 詞綴</returns>
    public static bool IsHybridFlaskMod(string coeModId) => GetFlaskTypeForMod(coeModId) == FlaskType.Hybrid;

    /// <summary>
    /// 獲取各 Flask 類型的詞綴統計
    /// </summary>
    /// <returns>Flask 類型統計字典</returns>
    public static Dictionary<FlaskType, int> GetFlaskTypeStats()
    {
        var stats = new Dictionary<FlaskType, int>
        {
            [FlaskType.Utility] = 0,
            [FlaskType.Life] = 0,
            [FlaskType.Mana] = 0,
            [FlaskType.Hybrid] = 0
        };

        foreach (var modId in FlaskMods.Keys)
        {
            var flaskType = GetFlaskTypeForMod(modId);
            if (flaskType.HasValue)
                stats[flaskType.Value]++;
        }

        return stats;
    }

    /// <summary>
    /// 獲取指定 Flask 類型的所有詞綴 ID
    /// </summary>
    /// <param name="flaskType">Flask 類型</param>
    /// <returns>該類型的所有詞綴 ID 列表</returns>
    public static List<string> GetModIdsByFlaskType(FlaskType flaskType)
    {
        return FlaskMods.Keys
            .Where(modId => GetFlaskTypeForMod(modId) == flaskType)
            .OrderBy(x => x)
            .ToList();
    }
}