using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Viberaria.IntifaceConnection;
using Viberaria.tModAdapters;
using static Viberaria.VibrationManager.VibrationManager;
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace Viberaria.Config;

public class ViberariaConfig : ModConfig
{
    public static ViberariaConfig Instance;
    public static int[] DebuffsSelected;
    public override ConfigScope Mode => ConfigScope.ClientSide;

    /// <summary>
    /// Parse the config subpage to get a string of the intiface IP and port.
    /// </summary>
    public static string IntifaceConnectionAddress
    {
        get
        {
            string ip;
            if (Instance.IntifaceAddress.UseLocalhost)
            {
                ip = "localhost";
            }
            else
            {
                // convert [192, 168, 0, 0] to "192.168.0.0"
                ip = String.Join(".",
                    Instance.IntifaceAddress.IntifaceIpAddress.Select(x => x.ToString()).ToArray()
                );
            }
            string port = Instance.IntifaceAddress.IntifaceIpPort.ToString();
            return $"{ip}:{port}";
        }
    }

    #region Functions
    public static int[] FindModBuffs(List<string> debuffStrings)  // public so it can be called in tPlayer.OnWorldLoad if buffs are not found.
    {
        List<int> debuffs = new();
        Dictionary<String, int> modBuffs = new();

        foreach (ModBuff buff in ModContent.GetContent<ModBuff>())
        {
            string modName = buff.Mod.Name;
            if (Instance.Debuffs.ModNameReplacement.TryGetValue(modName, out string replacement))
                modName = replacement;
            modBuffs.Add(modName + "." + buff.Name, buff.Type);
        }

        foreach (string debuffString in debuffStrings)
        {
            if (BuffID.Search.TryGetId(debuffString, out int debuffId) ||  // search Vanilla (de)buffs by Name
                modBuffs.TryGetValue(debuffString, out debuffId) ||        // search Mod (de)buffs by Name
                Int32.TryParse(debuffString, out debuffId) && (            // Convert name to int
                    BuffID.Search.ContainsId(debuffId) ||                  // search Vanilla (de)buffs by ID
                    modBuffs.ContainsValue(debuffId)))                     // search Mod (de)buffs by ID (Type)
            {
                debuffs.Add(debuffId);
            }
            else
            {
                if (!Instance.Debuffs.NotifyUnknownBuffsOnLoad) continue;
                tChat.Logger.WarnFormat("Could not find debuff: {0}", debuffString);
                tChat.LogToPlayer("Viberaria: Could not find debuff `" + debuffString + "`. " +
                                  "Make sure the name is correct and reload the world.", Color.Orange);
            }
        }

        return debuffs.ToArray();
    }

    public static int[] FindModInstruments(List<string> instrumentStrings)  // public so it can be called in tPlayer.OnWorldLoad if buffs are not found.
    {
        List<int> instruments = new();
        Dictionary<String, int> modItems = new();

        foreach (ModItem item in ModContent.GetContent<ModItem>())
        {
            string modName = item.Mod.Name;
            if (Instance.Instruments.ModNameReplacement.TryGetValue(modName, out string replacement))
                modName = replacement;
            modItems.Add(modName + "." + item.Name, item.Type);
        }

        foreach (string instrumentString in instrumentStrings)
        {
            if (ItemID.Search.TryGetId(instrumentString, out int instrumentId) ||  // search Vanilla (de)buffs by Name
                modItems.TryGetValue(instrumentString, out instrumentId) ||        // search Mod (de)buffs by Name
                Int32.TryParse(instrumentString, out instrumentId) && (            // Convert name to int
                    ItemID.Search.ContainsId(instrumentId) ||                  // search Vanilla (de)buffs by ID
                    modItems.ContainsValue(instrumentId)))                     // search Mod (de)buffs by ID (Type)
            {
                instruments.Add(instrumentId);
            }
            else
            {
                if (!Instance.Instruments.NotifyUnknownInstrumentsOnLoad) continue;
                tChat.Logger.WarnFormat("Could not find item: {0}", instrumentString);
                tChat.LogToPlayer("Viberaria: Could not find item `" + instrumentString + "`. " +
                                  "Make sure the name is correct and reload the world.", Color.Orange);
            }
        }

        return instruments.ToArray();
    }

    private static void PrintDebuffsToLog()
    {
        List<string> modBuffs = new();

        foreach (ModBuff buff in ModContent.GetContent<ModBuff>())
        {
            string modName = buff.Mod.Name;
            if (Instance.Debuffs.ModNameReplacement.TryGetValue(modName, out string replacement))
                modName = replacement;
            modBuffs.Add($"{modName}.{buff.Name}, {buff.Type}");
        }

        tChat.Logger.Info("Printing all Debuffs to log:\n  " + String.Join("\n  ", modBuffs));
    }

    public override void OnChanged()
    {
        DebuffsSelected = FindModBuffs(Instance.Debuffs.DebuffNames);
        if (Instance.Debuffs.PrintAllDebuffsToLog)
        {
            PrintDebuffsToLog();
            tChat.LogToPlayer("Printed debuffs to log successfully.", Color.Green);
            Instance.Debuffs.PrintAllDebuffsToLog = false;
        }

        Halt();
        if (Instance.ViberariaEnabled)
            ClientHandler.ClientConnect();
        else
            ClientHandler.ClientDisconnect();
    }

    private static float ClampAndRound(float value)
    {
        return Math.Clamp(MathF.Round(value, 2), 0f, 1f);
    }

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context) {
        // From ExampleMod:
        //   Range is just a suggestion to the UI. If we want to enforce constraints, we need to validate the data here.
        //    Users can edit config files manually with values outside the RangeAttribute, so we fix here if necessary.
        //   Both enforcing ranges and not enforcing ranges have uses in mods. Make sure you fix config values if
        //    values outside the range will mess up your mod.

        HealthMaxIntensity = ClampAndRound(HealthMaxIntensity);
        HealthMinIntensity = ClampAndRound(HealthMinIntensity);
        MaxManaUsageIntensity = ClampAndRound(MaxManaUsageIntensity);
        ManaUsageIntensityFactor = ClampAndRound(ManaUsageIntensityFactor);
        MaxBlastingIntensity = ClampAndRound(MaxBlastingIntensity);
        BlastingIntensityFactor = ClampAndRound(BlastingIntensityFactor);
    }
    #endregion

    private const int MinTime = 10;
    private const int MaxTime = 3000;
    private const int IncrementTime = 10;
    private const float MinIntensity = 0.05f;
    private const float MaxIntensity = 1f;
    private const float IncrementIntensity = 0.01f;

    #region Main Configuration
    [Header("MainConfiguration")]
    [DefaultValue(true)] public bool ViberariaEnabled;
    [Range(0f,1f)] [Increment(0.01f)] [DefaultValue(1f)] public float VibratorMaxIntensity;
    public IntifaceIpSubpage IntifaceAddress = new();

    [SeparatePage]
    public class IntifaceIpSubpage
    {
        [Header("IntifaceIP")]
        public bool UseLocalhost = true;
        [Range(0,255)] public int[] IntifaceIpAddress = [192, 168, 0, 0];
        public int IntifaceIpPort = 12345;

        public override string ToString()
        {
            return IntifaceConnectionAddress;
        }

        // "Implementing Equals and GetHashCode are critical for any classes you use."
        //   - tModLoader/CustomDataTypes/Pair
        public override bool Equals(object obj) {
            if (obj is IntifaceIpSubpage other)
                return UseLocalhost == other.UseLocalhost && IntifaceIpAddress == other.IntifaceIpAddress && IntifaceIpPort == other.IntifaceIpPort;
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(obj);
        }

        public override int GetHashCode() {
            // ReSharper disable thrice NonReadonlyMemberInGetHashCode
            return new { UseLocalhost, IntifaceIpAddress, IntifaceIpPort }.GetHashCode();
        }
    }
    #endregion

    #region Health config
    [Header("HealthVibrationScaling")]
    [DefaultValue(false)] public bool HealthVibratationScalingEnabled;
    [Range(MinIntensity,MaxIntensity)] [Increment(IncrementIntensity)] [DefaultValue(.6f)] public float HealthMaxIntensity;
    [Range(0,MaxIntensity)] [Increment(IncrementIntensity)] [DefaultValue(0f)] public float HealthMinIntensity;
    #endregion

    #region Damage config
    [Header("DamageVibration")]
    [DefaultValue(true)] public bool DamageVibrationEnabled;
    [SeparatePage] public VibrationPattern DamagePattern = new()
    {
        Pattern = [
            new VibrationStep { Intensity = 0.5f, Duration = 600 }
        ]
    };
    [DefaultValue(true)] public bool StaticDamageVibration;
    [Range(0,10000)] [DefaultValue(0)] public int MinimumDamageForVibration;
    #endregion

    #region Death config
    [Header("DeathVibration")]
    [DefaultValue(true)] public bool DeathVibrationEnabled;
    [SeparatePage] public VibrationPattern DeathPattern = new()
    {
        Pattern = [
            new VibrationStep { Intensity = 0.7f, Duration = 1000 }
        ]
    };
    #endregion

    #region Potion Use config
    [Header("PotionUseVibration")]
    [DefaultValue(true)] public bool PotionUseVibrationEnabled;
    [SeparatePage] public VibrationPattern PotionPattern = new()
    {
        Pattern = [
            new VibrationStep { Intensity = 0.4f, Duration = 400 }
        ]
    };
    #endregion

    # region Mana Usage config
    [Header("ManaUsageVibration")]
    [DefaultValue(false)] public bool ManaUsageVibrationEnabled;
    [Range(MinIntensity,MaxIntensity), Increment(IncrementIntensity), DefaultValue(.8f)] public float MaxManaUsageIntensity;
    [Range(0.01f,5f), Increment(0.01f), DefaultValue(1f)] public float ManaUsageIntensityFactor;
    [Range(MinTime,60000), Increment(IncrementTime), DefaultValue(2000)] public int ManaUsageBuildupTimeMsec;
    [Range(MinTime,MaxTime), Increment(IncrementTime), DefaultValue(1000)] public int ManaUsageFadeDelayMsec;
    # endregion Mana Usage config

    # region Ammo Usage config
    [Header("AmmoUsageVibration")]
    [DefaultValue(false)] public bool BlastingEnabled;
    [Range(MinIntensity,MaxIntensity), Increment(IncrementIntensity), DefaultValue(.8f)] public float MaxBlastingIntensity;
    [Range(0.01f,3f), Increment(0.01f), DefaultValue(0.25f)] public float BlastingIntensityFactor;
    [Range(MinTime,60000), Increment(IncrementTime), DefaultValue(2000)] public int BlastingBuildupTimeMsec;
    [Range(MinTime,MaxTime), Increment(IncrementTime), DefaultValue(1000)] public int BlastingFadeDelayMsec;
    # endregion Ammo Usage config

    #region Debuff config
    [Header("DebuffVibration")]
    [DefaultValue(true)] public bool DebuffVibrationEnabled;
    [SeparatePage] public VibrationPattern DebuffPattern = new()
    {
        Pattern = [
            new VibrationStep { Intensity = 0.45f, Duration = 500 },
            new VibrationStep { Intensity = 0.2f, Duration = 500 }
        ]
    };
    public DebuffSubpage Debuffs = new();

    [SeparatePage]
    public class DebuffSubpage
    {
        [Header("Debuffs")]
        [DefaultValue(false)] public bool NotifyUnknownBuffsOnLoad;
        [DefaultValue(false)] public bool PrintAllDebuffsToLog;
        public List<string> DebuffNames = [
            "Poisoned",
            "Darkness",
            "OnFire",
            "Bleeding",
            "Confused",
            "Slow",
            "Weak",
            "Silenced",
            "BrokenArmor",
            "Horrified",
            "CursedInferno",
            "Frostburn",
            "Chilled",
            "Frozen",
            "Burning", // Stepping on hot blocks
            "Suffocation", // In gravity blocks or in water
            "Venom",
            "Blackout", // a stronger version of 'Darkness'
            "Wet", // When you get shot by a water gun
            "Slimed", // When you get shot by a slime gun
            "Electrified",
            "ShadowFlame",
            "Stoned",
            "Dazed",
            "Obstructed", // A stronger version of 'Blackout'
            "VortexDebuff", // Distorted
            "OnFire3", // Hellfire
            "Frostburn2", // Frostburn
            "Starving",
            "CM.Nightwither",
            "CM.CrushDepth",
            "CM.RiptideDebuff",
            "CM.AstralInfectionDebuff",
            "CM.Plague",
            "CM.SulphuricPoisoning",
            "CM.WhisperingDeath",
            "CM.BanishingFire",
            "CM.BrimstoneFlames",
            "CM.Dragonfire",
            "CM.GodSlayerInferno",
            "CM.HolyFlames",
            "CM.VulnerabilityHex"
        ];

        public Dictionary<string, string> ModNameReplacement = new() { { "CalamityMod", "CM" } };

        public override string ToString()
        {
            return DebuffNames.Count + " selected";
        }

        // "Implementing Equals and GetHashCode are critical for any classes you use."
        //   - tModLoader/CustomDataTypes/Pair
        public override bool Equals(object obj) {
            if (obj is DebuffSubpage other)
                return DebuffNames == other.DebuffNames &&
                       ModNameReplacement == other.ModNameReplacement &&
                       NotifyUnknownBuffsOnLoad == other.NotifyUnknownBuffsOnLoad &&
                       PrintAllDebuffsToLog == other.PrintAllDebuffsToLog;
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(obj);
        }

        public override int GetHashCode() {
            return new { DebuffNames, ModNameReplacement, NotifyUnknownBuffsOnLoad, PrintAllDebuffsToLog }.GetHashCode();
        }
    }
    #endregion

    # region Fishing config
    [Header("FishingVibration")]
    [DefaultValue(true)] public bool FishingVibrationEnabled;
    [SeparatePage] public VibrationPattern FishingPattern = new()
    {
        ZerosOverrideLowerPriority = true,
        Pattern = [
            new VibrationStep { Intensity = .30f, Duration = 200 },
            new VibrationStep { Intensity = .00f, Duration = 200 },
            new VibrationStep { Intensity = .30f, Duration = 300 }
        ]
    };
    # endregion Fishing config

    # region Instrument config
    [Header("InstrumentVibration")]
    [DefaultValue(false)] public bool InstrumentVibrationEnabled;
    [SeparatePage] public VibrationPattern InstrumentPattern = new()
    {
        Pattern = [
            new VibrationStep { Intensity = 1f, Duration = 750 }
        ]
    };
    public InstrumentsSubpage Instruments = new();

    [SeparatePage]
    public class InstrumentsSubpage
    {
        [Header("Debuffs")]
        [DefaultValue(false)] public bool NotifyUnknownInstrumentsOnLoad;
        [DefaultValue(false)] public bool PrintInstrumentsToChatWhenUsed;
        public List<string> InstrumentNames = ["Rain Song"];

        public Dictionary<string, string> ModNameReplacement = new() { { "CalamityMod", "CM" } };

        public override string ToString()
        {
            return InstrumentNames.Count + " selected";
        }

        // "Implementing Equals and GetHashCode are critical for any classes you use."
        //   - tModLoader/CustomDataTypes/Pair
        public override bool Equals(object obj) {
            if (obj is InstrumentsSubpage other)
                return InstrumentNames == other.InstrumentNames &&
                       ModNameReplacement == other.ModNameReplacement &&
                       NotifyUnknownInstrumentsOnLoad == other.NotifyUnknownInstrumentsOnLoad &&
                       PrintInstrumentsToChatWhenUsed == other.PrintInstrumentsToChatWhenUsed;
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return new { InstrumentNames, ModNameReplacement, NotifyUnknownInstrumentsOnLoad, PrintAllInstrumentsToLog = PrintInstrumentsToChatWhenUsed }.GetHashCode();
        }
    }
    # endregion Instrument config

    #region Debug config
    [Header("DebugConfigs")]
    public DebugToolsSubpage Debug = new();

    [SeparatePage]
    public class DebugToolsSubpage
    {
        [Header("DebuggingTools")]
        [DefaultValue(false)] public bool Enabled;
        [DefaultValue(true)] public bool ToyStrengthMessages;
        [DefaultValue(false)] public bool ProcessEventLogs;
        [DefaultValue(false)] public bool ManaAmmoUsageMessages;
        [DefaultValue(false)] public bool InstrumentMessages;

        public override string ToString()
        {
            if (!Enabled)
                return "Disabled";
            List<string> enabled = ["time"];
            if (ToyStrengthMessages) enabled.Add("toy");
            if (ProcessEventLogs) enabled.Add("queue");
            if (ManaAmmoUsageMessages) enabled.Add("usage");
            if (InstrumentMessages) enabled.Add("instrument");
            return string.Join(", ", enabled);
        }

        public override bool Equals(object obj) {
            if (obj is DebugToolsSubpage other)
                return Enabled == other.Enabled &&
                ToyStrengthMessages == other.ToyStrengthMessages &&
                ProcessEventLogs == other.ProcessEventLogs &&
                ManaAmmoUsageMessages == other.ManaAmmoUsageMessages &&
                InstrumentMessages == other.InstrumentMessages;
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(obj);
        }

        public override int GetHashCode() {
            return new {
                Enabled,
                ToyStrengthMessages,
                ProcessEventLogs,
                ManaAmmoUsageMessages,
                InstrumentMessages
            }.GetHashCode();
        }
    }
    #endregion Debug config
}