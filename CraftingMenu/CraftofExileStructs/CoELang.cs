using Newtonsoft.Json;
using System.Collections.Generic;

namespace WheresMyCraftAt.CraftingMenu.CraftofExileStructs;

public class CoELang
{
    [JsonProperty("base")]
    public Dictionary<string, string> @base { get; set; }

    [JsonProperty("bgroup")]
    public Dictionary<string, string> bgroup { get; set; }

    [JsonProperty("mod")]
    public Dictionary<string, string> mod { get; set; }

    [JsonProperty("mdef")]
    public Dictionary<string, string> mdef { get; set; }

    [JsonProperty("mgroup")]
    public Dictionary<string, string> mgroup { get; set; }

    [JsonProperty("mtype")]
    public Dictionary<string, string> mtype { get; set; }

    [JsonProperty("fossil")]
    public Dictionary<string, string> fossil { get; set; }

    [JsonProperty("catalyst")]
    public Dictionary<string, string> catalyst { get; set; }

    [JsonProperty("essence")]
    public Dictionary<string, string> essence { get; set; }

    [JsonProperty("bitem")]
    public Dictionary<string, string> bitem { get; set; }

    [JsonProperty("maven")]
    public Dictionary<string, string> maven { get; set; }

    [JsonProperty("socketable")]
    public List<object> socketable { get; set; }
}