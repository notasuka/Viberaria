using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using static Viberaria.VibrationManager.VibrationManager;
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace Viberaria;

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
            if(Instance.Debuffs.ModNameReplacement.TryGetValue(modName, out string replacement))
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
                tChat.LogToPlayer("Viberaria: Could not find debuff `" + debuffString + "`. " +
                                  "Make sure the name is correct and reload the world.", Color.Orange);
                ModContent.GetInstance<Viberaria>().Logger.WarnFormat("Could not find debuff: {0}", debuffString);
            }
        }

        return debuffs.ToArray();
    }

    public override void OnChanged()
    {
        DebuffsSelected = FindModBuffs(Instance.Debuffs.DebuffNames);
        Halt();
        bClient.ClientConnect();
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
    [DefaultValue(true)] public bool StaticDamageVibration;
    [Range(MinIntensity,MaxIntensity)] [Increment(IncrementIntensity)] [DefaultValue(.5f)] public float DamageVibrationIntensity;
    [Range(0,10000)] [DefaultValue(0)] public int MinimumDamageForVibration;
    [Range(MinTime,MaxTime)] [Increment(IncrementTime)] [DefaultValue(600)] public int DamageVibrationDurationMsec;
    #endregion

    #region Death config
    [Header("DeathVibration")]
    [DefaultValue(true)] public bool DeathVibrationEnabled;
    [Range(MinIntensity,MaxIntensity)] [Increment(IncrementIntensity)] [DefaultValue(.7f)] public float DeathVibrationIntensity;
    [DefaultValue(true)] public bool StaticDeathVibrationLength;
    [Range(MinTime,MaxTime)] [Increment(IncrementTime)] [DefaultValue(1000)] public int DeathVibrationDurationMsec;
    #endregion

    #region Potion Use config
    [Header("PotionUseVibration")]
    [DefaultValue(true)] public bool PotionUseVibrationEnabled;
    [Range(MinIntensity,MaxIntensity), Increment(IncrementIntensity), DefaultValue(.4f)] public float PotionVibrationIntensity;
    [Range(MinTime,MaxTime), Increment(IncrementTime), DefaultValue(400)] public int PotionVibrationDurationMsec;
    #endregion

    # region Mana Usage config
    [Header("ManaUsageVibration")]
    [DefaultValue(false)] public bool ManaUsageVibrationEnabled;
    [Range(MinIntensity,MaxIntensity), Increment(IncrementIntensity), DefaultValue(.8f)] public float MaxManaUsageIntensity;
    [Range(0.01f,5f), Increment(0.01f), DefaultValue(1f)] public float ManaUsageIntensityFactor;
    [Range(MinTime,60000), Increment(IncrementTime), DefaultValue(2000)] public int ManaUsageBuildupTimeMsec;
    [Range(MinTime,MaxTime), Increment(IncrementTime), DefaultValue(1000)] public int ManaUsageFadeDelayMsec;

    # endregion Mana Usage config

    #region Debuff config
    [Header("DebuffVibration")]

    [DefaultValue(true)] public bool DebuffVibrationEnabled;
    [Range(MinIntensity,MaxIntensity), Increment(IncrementIntensity), DefaultValue(.45f)] public float DebuffMaxIntensity;
    [Range(MinIntensity,MaxIntensity), Increment(IncrementIntensity), DefaultValue(.2f)] public float DebuffMinIntensity;
    [Range(MinTime,MaxTime), Increment(IncrementTime), DefaultValue(500)] public int DebuffDelayMsec;
    public DebuffSubpage Debuffs = new();

    [SeparatePage]
    public class DebuffSubpage
    {
        [Header("Debuffs")]
        [DefaultValue(false)] public bool NotifyUnknownBuffsOnLoad;
        public List<string> DebuffNames = new()
        {
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
            "Burning",  // Stepping on hot blocks
            "Suffocation",  // In gravity blocks or in water
            "Venom",
            "Blackout",  // a stronger version of 'Darkness'
            "Wet",  // When you get shot by a water gun
            "Slimed",  // When you get shot by a slime gun
            "Electrified",
            "ShadowFlame",
            "Stoned",
            "Dazed",
            "Obstructed",  // A stronger version of 'Blackout'
            "VortexDebuff",  // Distorted
            "OnFire3",  // Hellfire
            "Frostburn2",  // Frostburn
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
        };

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
                       NotifyUnknownBuffsOnLoad == other.NotifyUnknownBuffsOnLoad;
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(obj);
        }

        public override int GetHashCode() {
            return new { DebuffNames, ModNameReplacement, NotifyUnknownBuffsOnLoad }.GetHashCode();
        }
    }
    #endregion

    # region Fishing config
    [Header("FishingVibration")]
    [DefaultValue(true)] public bool FishingVibrationEnabled;
    [Range(MinIntensity,MaxIntensity), Increment(IncrementIntensity), DefaultValue(.30f)] public float FishingIntensity1;
    [Range(MinTime,MaxTime), Increment(IncrementTime), DefaultValue(200)] public int FishingLengthMsec1;
    [Range(MinTime,MaxTime), Increment(IncrementTime), DefaultValue(200)] public int FishingDelayMsec1;
    [Range(MinIntensity,MaxIntensity), Increment(IncrementIntensity), DefaultValue(.30f)] public float FishingIntensity2;
    [Range(MinTime,MaxTime), Increment(IncrementTime), DefaultValue(300)] public int FishingLengthMsec2;
    # endregion Fishing config

    #region Debug config
    [Header("DebugConfigs")]
    public DebugToolsSubpage Debug = new();

    [SeparatePage]
    public class DebugToolsSubpage
    {
        [Header("DebuggingTools")]
        [DefaultValue(false)] public bool Enabled;
        [DefaultValue(true)] public bool ToyStrengthMessages;
        [DefaultValue(false)] public bool ProcessEventMessages;
        [DefaultValue(false)] public bool ManaAmmoUsageMessages;

        public override string ToString()
        {
            if (!Enabled)
                return "Disabled";
            List<string> enabled = ["time"];
            if (ToyStrengthMessages) enabled.Add("toy");
            if (ProcessEventMessages) enabled.Add("queue");
            if (ManaAmmoUsageMessages) enabled.Add("usage");
            return string.Join(", ", enabled);
        }

        public override bool Equals(object obj) {
            if (obj is DebugToolsSubpage other)
                return Enabled == other.Enabled &&
                ToyStrengthMessages == other.ToyStrengthMessages &&
                ProcessEventMessages == other.ProcessEventMessages &&
                ManaAmmoUsageMessages == other.ManaAmmoUsageMessages;
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(obj);
        }

        public override int GetHashCode() {
            return new { Enabled, ToyStrengthMessages, ProcessEventMessages, ManaAmmoUsageMessages }.GetHashCode();
        }
    }
    #endregion Debug config
}