using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Viberaria;

public class ViberariaConfig : ModConfig
{
    public static ViberariaConfig Instance;
    public override ConfigScope Mode => ConfigScope.ClientSide;

    private const int MinTime = 10;
    private const int MaxTime = 3000;
    private const int IncrementTime = 10;
    private const float MinIntensity = 0.00f;
    private const float MaxIntensity = 1f;
    private const float IncrementIntensity = 0.01f;

    #region Main Configuration
    [Header("MainConfiguration")]
    [DefaultValue(true)] public bool ViberariaEnabled;
    [Range(0f,1f)] [Increment(0.01f)] [DefaultValue(1f)] public float VibratorMaxIntensity;
    #endregion

    #region Health config
    [Header("HealthVibrationScaling")]
    [DefaultValue(true)] public bool HealthVibratationScalingEnabled;
    [Range(MinIntensity,MaxIntensity)] [Increment(IncrementIntensity)] [DefaultValue(1f)] public float HealthMaxIntensity;
    [Range(MinIntensity,MaxIntensity)] [Increment(IncrementIntensity)] [DefaultValue(0.2f)] public float HealthMinIntensity;
    #endregion

    #region Damage config
    [Header("DamageVibration")]
    [DefaultValue(true)] public bool DamageVibrationEnabled;
    [DefaultValue(true)] public bool StaticDamageVibration;
    [Range(MinIntensity,MaxIntensity)] [Increment(IncrementIntensity)] [DefaultValue(.5f)] public float DamageVibrationIntensity;
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

    #region Debuff config
    [Header("DebuffVibration")]

    [DefaultValue(true)] public bool DebuffVibrationEnabled;
    [Range(MinIntensity,MaxIntensity), Increment(IncrementIntensity), DefaultValue(.45f)] public float DebuffMaxIntensity;
    [Range(MinIntensity,MaxIntensity), Increment(IncrementIntensity), DefaultValue(.2f)] public float DebuffMinIntensity;
    [Range(MinTime,MaxTime), Increment(IncrementTime), DefaultValue(500)] public int DebuffDelayMsec;
    #endregion

    #region Other
    [Header("OtherConfigs")]
    [DefaultValue(false)] public bool DebugChatMessages;
    #endregion
}