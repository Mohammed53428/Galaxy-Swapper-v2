﻿using Galaxy_Swapper_v2.Workspace.Generation.Formats;
using Galaxy_Swapper_v2.Workspace.Utilities;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Galaxy_Swapper_v2.Workspace.Generation.Types
{
    /// <summary>
    /// All the code below was provided from: https://github.com/GalaxySwapperOfficial/Galaxy-Swapper-v2
    /// You can also find us at https://galaxyswapperv2.com/Guilded
    /// </summary>
    public static class Neutral
    {
        public static void Format(Cosmetic Cosmetic, List<Option> Options, JToken Empty, Generate.Type CacheType, params string[] Types)
        {
            var Parse = Cosmetic.Parse;
            var OverrideOptions = new List<Option>();

            if (Parse["OverrideOptions"] != null && (Parse["OverrideOptions"] as JArray).Any())
            {
                foreach (var Override in Parse["OverrideOptions"])
                {
                    switch (Override["Type"].Value<string>())
                    {
                        case "Exception":
                            {
                                var OverrideCosmetic = Generate.Cache[CacheType].Cosmetics[Override["CacheKey"].Value<string>()];
                                OverrideOptions.Add(new Option
                                {
                                    Name = OverrideCosmetic.Name,
                                    ID = OverrideCosmetic.ID,
                                    Parse = OverrideCosmetic.Parse,
                                    Icon = OverrideCosmetic.Icon
                                });
                            }
                            break;
                        case "Override":
                            {
                                var NewOption = new Option
                                {
                                    Name = $"{Override["Name"].Value<string>()} to {Cosmetic.Name}",
                                    ID = Override["ID"].Value<string>(),
                                    OverrideIcon = Cosmetic.Icon,
                                    Parse = null // Not needed we will never use it
                                };

                                if (Override["Message"] != null)
                                    NewOption.Message = Override["Message"].Value<string>();

                                if (!Override["Override"].KeyIsNullOrEmpty())
                                    NewOption.Icon = Override["Override"].Value<string>();
                                else if (!Override["Icon"].KeyIsNullOrEmpty())
                                    NewOption.Icon = Override["Icon"].Value<string>();
                                else
                                    NewOption.Icon = string.Format(Generate.Domain, NewOption.ID);

                                foreach (var Asset in Override["Assets"])
                                {
                                    var NewAsset = new Asset() { Object = Asset["AssetPath"].Value<string>() };

                                    if (Asset["AssetPathTo"] != null)
                                        NewAsset.OverrideObject = Asset["AssetPathTo"].Value<string>();

                                    if (Asset["Buffer"] != null)
                                        NewAsset.OverrideBuffer = Asset["Buffer"].Value<string>();

                                    if (Asset["Swaps"] != null)
                                        NewAsset.Swaps = Asset["Swaps"];

                                    NewOption.Exports.Add(NewAsset);
                                }

                                Cosmetic.Options.Add(NewOption);
                            }
                            break;
                        default:
                            continue;
                    }
                }
            }

            var BlackListed = new List<string>();

            if (Parse["BlackList"] != null && (Parse["BlackList"] as JArray).Any())
                BlackListed = (Parse["BlackList"] as JArray).ToObject<List<string>>();

            if (CacheType == Generate.Type.Characters && !BlackListed.Contains("Default"))
            {
                //Defaults!
            }

            if (Parse["UseOptions"] != null && !Parse["UseOptions"].Value<bool>())
                return;

            foreach (var Option in Options.Concat(OverrideOptions))
            {
                var OParse = Option.Parse;
                bool Continue = false;

                if (BlackListed.Contains($"{Option.Name}:{Option.ID}") || $"{Cosmetic.Name}:{Cosmetic.ID}" == $"{Option.Name}:{Option.ID}")
                    continue;

                foreach (string Type in Types)
                {
                    if (OParse[Type] != null && OParse[Type].Value<string>() != Parse[Type].Value<string>() && OParse[Type].Value<string>() != "Any" && Parse[Type].Value<string>() != "Any")
                    {
                        Continue = true;
                    }
                }

                if (Continue)
                    continue;

                var NewOption = (Option)Option.Clone();
                var Objects = OParse["Objects"].ToDictionary(obj => obj["Type"].Value<string>(), obj => obj["Object"].Value<string>());

                NewOption.Exports = new List<Asset>();

                foreach (var Object in Parse["Objects"])
                {
                    Asset NewAsset = null;

                    if (Objects.ContainsKey(Object["Type"].Value<string>()))
                    {
                        string ObjectPath = Objects[Object["Type"].Value<string>()];
                        string OverrideObjectPath = Object["Object"].Value<string>();

                        NewAsset = new Asset() { Object = ObjectPath, OverrideObject = OverrideObjectPath };
                        Objects.Remove(Object["Type"].Value<string>());
                    }
                    else if (Object["Exceptions"] != null && ((JArray)Object["Exceptions"]).ToObject<List<string>>().Intersect(Objects.Keys).Any())
                    {
                        string Exception = ((JArray)Object["Exceptions"]).ToObject<List<string>>().Intersect(Objects.Keys).First();
                        string ObjectPath = Objects[Exception];
                        string OverrideObjectPath = Object["Object"].Value<string>();

                        NewAsset = new Asset() { Object = ObjectPath, OverrideObject = OverrideObjectPath };
                        Objects.Remove(Exception);
                    }
                    else
                    {
                        Continue = true;
                        break;
                    }

                    if (Object["Buffer"] != null && !string.IsNullOrEmpty(Object["Buffer"].Value<string>()))
                        NewAsset.OverrideBuffer = Object["Buffer"].Value<string>();

                    NewAsset.Swaps = Object["Swaps"];
                    NewOption.Exports.Add(NewAsset);
                }

                if (Continue)
                    continue;

                if (Cosmetic.Downloadables != null && Cosmetic.Downloadables.Count > 0)
                    NewOption.Downloadables = Cosmetic.Downloadables;

                if (Objects.Count != 0)
                {
                    foreach (var Object in Objects)
                    {
                        NewOption.Exports.Add(new Asset() { Object = Object.Value, OverrideObject = Empty[Object.Key].Value<string>() });
                    }
                }

                if (Parse["Additional"] != null)
                {
                    foreach (var Additional in Parse["Additional"])
                    {
                        var NewAsset = new Asset() { Object = Additional["Object"].Value<string>(), Swaps = Additional["Swaps"] };
                        if (Additional["OverrideObject"] != null)
                            NewAsset.OverrideObject = Additional["OverrideObject"].Value<string>();
                        if (Additional["Buffer"] != null && !string.IsNullOrEmpty(Additional["Buffer"].Value<string>()))
                            NewAsset.OverrideBuffer = Additional["Buffer"].Value<string>();

                        NewOption.Exports.Add(NewAsset);
                    }
                }

                NewOption.Message = Cosmetic.Message;
                NewOption.Name = $"{Option.Name} to {Cosmetic.Name}";
                NewOption.OverrideIcon = Cosmetic.Icon;
                NewOption.Nsfw = Cosmetic.Nsfw;

                Cosmetic.Options.Add(NewOption);
            }
        }
    }
}