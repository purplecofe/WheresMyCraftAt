using System;
using System.Collections.Generic;
using WheresMyCraftAt.CraftingMenu.CraftofExileStructs;

namespace WheresMyCraftAt.CraftingMenu;

/// <summary>
/// CoE 模組 ID 到 ItemFilter 查詢語法的映射系統
/// </summary>
public static class CoEModMapping
{
    /// <summary>
    /// 特殊 ID 的直接映射（非模組 ID）
    /// </summary>
    private static readonly Dictionary<string, string> SpecialIdMappings = new()
    {
        ["open_prefix"] = "ModsInfo.HasOpenPrefix",
        ["open_suffix"] = "ModsInfo.HasOpenSuffix", 
        ["prefix_count"] = "ModsInfo.Prefixes.Count",
        ["suffix_count"] = "ModsInfo.Suffixes.Count",
        ["total_mods"] = "ModsInfo.Prefixes.Count + ModsInfo.Suffixes.Count"
    };

    /// <summary>
    /// 常見模組 ID 的映射模板
    /// 使用關鍵字匹配來處理模組名稱
    /// </summary>
    private static readonly Dictionary<string, ModMappingTemplate> CommonModMappings = new()
    {
        // Flask 相關模組
        ["1654"] = new ModMappingTemplate 
        {
            Description = "Flask Duration",
            Keywords = new[] { "duration", "持續時間" },
            QueryTemplate = "ModsInfo.ExplicitMods.Any(x => x.RawName.Contains(\"Duration\") && x.Values[0] >= {threshold})"
        },
        ["3887"] = new ModMappingTemplate
        {
            Description = "Curse Effect Reduction", 
            Keywords = new[] { "curse", "詛咒" },
            QueryTemplate = "ModsInfo.ExplicitMods.Any(x => x.RawName.Contains(\"Curse\") && x.Values[0] >= {threshold})"
        }
    };

    /// <summary>
    /// 根據關鍵字的通用模板
    /// </summary>
    private static readonly Dictionary<string, string> KeywordTemplates = new()
    {
        ["life"] = "ModsInfo.ExplicitMods.Any(x => x.RawName.Contains(\"Life\") && x.Values[0] >= {threshold})",
        ["mana"] = "ModsInfo.ExplicitMods.Any(x => x.RawName.Contains(\"Mana\") && x.Values[0] >= {threshold})",
        ["damage"] = "ModsInfo.ExplicitMods.Any(x => x.RawName.Contains(\"Damage\") && x.Values[0] >= {threshold})",
        ["resistance"] = "ModsInfo.ExplicitMods.Any(x => x.RawName.Contains(\"Resistance\") && x.Values[0] >= {threshold})",
        ["speed"] = "ModsInfo.ExplicitMods.Any(x => x.RawName.Contains(\"Speed\") && x.Values[0] >= {threshold})",
        ["critical"] = "ModsInfo.ExplicitMods.Any(x => x.RawName.Contains(\"Critical\") && x.Values[0] >= {threshold})"
    };

    /// <summary>
    /// 取得模組的 ItemFilter 查詢語法
    /// </summary>
    /// <param name="modId">CoE 模組 ID</param>
    /// <param name="coeLang">CoE 語言資料（可為 null）</param>
    /// <param name="threshold">最小值</param>
    /// <param name="maxValue">最大值（可為 null）</param>
    /// <returns>ItemFilter 查詢字串，如果無法轉換則返回 null</returns>
    public static string GetItemFilterQuery(string modId, CoELang coeLang, int threshold, int? maxValue = null)
    {
        // 1. 檢查特殊 ID
        if (SpecialIdMappings.TryGetValue(modId, out var specialQuery))
        {
            return ApplyThreshold(specialQuery, threshold, maxValue);
        }

        // 2. 檢查常見模組 ID 的直接映射
        if (CommonModMappings.TryGetValue(modId, out var mapping))
        {
            return ApplyThreshold(mapping.QueryTemplate, threshold, maxValue);
        }

        // 3. 嘗試使用 CoE 語言資料進行智慧匹配
        if (coeLang?.mod?.TryGetValue(modId, out var modName) == true)
        {
            var smartQuery = GenerateSmartQuery(modName, threshold, maxValue);
            if (smartQuery != null)
                return smartQuery;
        }

        // 4. 無法自動轉換，返回 null
        return null;
    }

    /// <summary>
    /// 根據模組名稱智慧生成查詢
    /// </summary>
    private static string GenerateSmartQuery(string modName, int threshold, int? maxValue)
    {
        var lowerModName = modName.ToLowerInvariant();

        // 檢查關鍵字匹配
        foreach (var (keyword, template) in KeywordTemplates)
        {
            if (lowerModName.Contains(keyword))
            {
                return ApplyThreshold(template, threshold, maxValue);
            }
        }

        // 通用模組匹配模板
        var genericTemplate = $"ModsInfo.ExplicitMods.Any(x => x.RawName.Contains(\"{EscapeString(modName)}\") && x.Values[0] >= {{threshold}})";
        return ApplyThreshold(genericTemplate, threshold, maxValue);
    }

    /// <summary>
    /// 套用門檻值到查詢模板
    /// </summary>
    private static string ApplyThreshold(string template, int threshold, int? maxValue)
    {
        var result = template.Replace("{threshold}", threshold.ToString());
        
        if (maxValue.HasValue && template.Contains("{threshold}"))
        {
            // 如果有最大值，修改條件為範圍檢查
            result = result.Replace($">= {threshold}", $">= {threshold} && x.Values[0] <= {maxValue}");
        }
        
        return result;
    }

    /// <summary>
    /// 跳脫字串中的特殊字元
    /// </summary>
    private static string EscapeString(string input)
    {
        return input.Replace("\"", "\\\"").Replace("\\", "\\\\");
    }

    /// <summary>
    /// 檢查模組 ID 是否可以自動轉換
    /// </summary>
    public static bool CanAutoConvert(string modId, CoELang coeLang = null)
    {
        // 特殊 ID 總是可以轉換
        if (SpecialIdMappings.ContainsKey(modId))
            return true;

        // 常見模組 ID 可以轉換
        if (CommonModMappings.ContainsKey(modId))
            return true;

        // 如果有語言資料，嘗試智慧匹配
        if (coeLang?.mod?.ContainsKey(modId) == true)
            return true;

        return false;
    }

    /// <summary>
    /// 取得模組的描述（用於 UI 顯示）
    /// </summary>
    public static string GetModDescription(string modId, CoELang coeLang = null)
    {
        // 檢查常見模組映射
        if (CommonModMappings.TryGetValue(modId, out var mapping))
            return mapping.Description;

        // 使用 CoE 語言資料
        if (coeLang?.mod?.TryGetValue(modId, out var modName) == true)
            return modName;

        return $"Unknown Mod (ID: {modId})";
    }
}

/// <summary>
/// 模組映射模板
/// </summary>
public class ModMappingTemplate
{
    /// <summary>
    /// 模組描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 關鍵字（用於匹配）
    /// </summary>
    public string[] Keywords { get; set; } = Array.Empty<string>();

    /// <summary>
    /// ItemFilter 查詢模板
    /// </summary>
    public string QueryTemplate { get; set; } = string.Empty;
}