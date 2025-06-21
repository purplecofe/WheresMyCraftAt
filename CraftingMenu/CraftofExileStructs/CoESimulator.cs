using Newtonsoft.Json;
using System.Collections.Generic;

namespace WheresMyCraftAt.CraftingMenu.CraftofExileStructs;

public class CoESimulator
{
    [JsonProperty("settings")]
    public Settings settings { get; set; }

    [JsonProperty("data")]
    public Data data { get; set; }

    [JsonProperty("config")]
    public List<Config> config { get; set; }

    [JsonProperty("flow")]
    public object flow { get; set; }

    [JsonProperty("states")]
    public States states { get; set; }

    [JsonProperty("results")]
    public object results { get; set; }

    [JsonProperty("items")]
    public object items { get; set; }
}

public class Config
{
    [JsonProperty("method")]
    public List<string> method { get; set; }

    [JsonProperty("mopts")]
    public object mopts { get; set; }

    [JsonProperty("autopass")]
    public bool autopass { get; set; }

    [JsonProperty("filters")]
    public List<Filter> filters { get; set; }

    [JsonProperty("vfilter")]
    public List<object> vfilter { get; set; }

    [JsonProperty("actions")]
    public Actions actions { get; set; }
}

public class Actions
{
    [JsonProperty("win")]
    public string win { get; set; }

    [JsonProperty("win_route")]
    public long? win_route { get; set; }

    [JsonProperty("fail")]
    public string fail { get; set; }

    [JsonProperty("fail_route")]
    public long? fail_route { get; set; }
}

public class Filter
{
    [JsonProperty("type")]
    public string type { get; set; }

    [JsonProperty("treshold")]
    public long? treshold { get; set; }

    [JsonProperty("conds")]
    public List<Cond> conds { get; set; }
}

public class Cond
{
    [JsonProperty("id")]
    public string id { get; set; }

    [JsonProperty("treshold")]
    public long treshold { get; set; }

    [JsonProperty("max")]
    public long? max { get; set; }

    [JsonProperty("base")]
    public object @base { get; set; }
}

public class Data
{
    [JsonProperty("fmodpool")]
    public object fmodpool { get; set; }

    [JsonProperty("eldritch")]
    public object eldritch { get; set; }

    [JsonProperty("dominance")]
    public object dominance { get; set; }

    [JsonProperty("mtypes")]
    public object mtypes { get; set; }

    [JsonProperty("implicits")]
    public object implicits { get; set; }

    [JsonProperty("rollable_implicits")]
    public long rollable_implicits { get; set; }

    [JsonProperty("cmodpool")]
    public object cmodpool { get; set; }

    [JsonProperty("hmodpool")]
    public object hmodpool { get; set; }

    [JsonProperty("maxaffgrp")]
    public Cmaxaffgrp maxaffgrp { get; set; }

    [JsonProperty("is_rare")]
    public long is_rare { get; set; }

    [JsonProperty("is_fossil")]
    public long is_fossil { get; set; }

    [JsonProperty("is_craftable")]
    public long is_craftable { get; set; }

    [JsonProperty("is_influenced")]
    public long is_influenced { get; set; }

    [JsonProperty("is_essence")]
    public long is_essence { get; set; }

    [JsonProperty("is_catalyst")]
    public long is_catalyst { get; set; }

    [JsonProperty("is_notable")]
    public long is_notable { get; set; }

    [JsonProperty("unique_notable")]
    public long unique_notable { get; set; }

    [JsonProperty("iaffixes")]
    public List<object> iaffixes { get; set; }

    [JsonProperty("meta_flags")]
    public MetaFlags meta_flags { get; set; }

    [JsonProperty("imprint")]
    public object imprint { get; set; }

    [JsonProperty("enchant")]
    public string enchant { get; set; }

    [JsonProperty("iaffbt")]
    public Cmaxaffgrp iaffbt { get; set; }

    [JsonProperty("cmaxaffgrp")]
    public Cmaxaffgrp cmaxaffgrp { get; set; }

    [JsonProperty("mgrpdata")]
    public object mgrpdata { get; set; }

    [JsonProperty("affbymgrp")]
    public object affbymgrp { get; set; }

    [JsonProperty("veiledmods")]
    public object veiledmods { get; set; }
}

public class Cmaxaffgrp
{
    [JsonProperty("prefix")]
    public long prefix { get; set; }

    [JsonProperty("suffix")]
    public long suffix { get; set; }
}

public class MetaFlags
{
}

public class Settings
{
    [JsonProperty("bgroup")]
    public long bgroup { get; set; }

    [JsonProperty("base")]
    public long @base { get; set; }

    [JsonProperty("bitem")]
    public object bitem { get; set; }

    [JsonProperty("ilvl")]
    public long ilvl { get; set; }

    [JsonProperty("rarity")]
    public string rarity { get; set; }

    [JsonProperty("influences")]
    public object influences { get; set; }

    [JsonProperty("quality")]
    public long quality { get; set; }
}

public class States
{
    [JsonProperty("init")]
    public Current init { get; set; }

    [JsonProperty("current")]
    public Current current { get; set; }

    [JsonProperty("states")]
    public List<Current> states { get; set; }
}

public class Current
{
    [JsonProperty("rarity")]
    public string rarity { get; set; }

    [JsonProperty("dominance")]
    public object dominance { get; set; }

    [JsonProperty("influences")]
    public object influences { get; set; }

    [JsonProperty("meta")]
    public object meta { get; set; }
}