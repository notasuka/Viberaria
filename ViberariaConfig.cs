using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using XPT.Core.Audio.MP3Sharp.Decoding;

namespace Viberaria;

public class ViberariaConfig : ModConfig
{
    public static ViberariaConfig Instance;
    public override ConfigScope Mode => ConfigScope.ClientSide;

    #region Main Toggles
    [Header("Toggles")]
    
    [DefaultValue(true)] public bool ViberariaEnabled;
    [DefaultValue(true)] public bool HealthVibratationScalingEnabled;
    [DefaultValue(true)] public bool DamageVibrationEnabled;
    [DefaultValue(true)] public bool PotionUseVibrationEnabled;
    [DefaultValue(true)] public bool EffectDamageVibrationEnabled;
    [DefaultValue(true)] public bool RespawnTimerVibrationEnabled;
    #endregion

    #region Intensities Configurations
    [Header("HP")]
    
    [Range(0.2f,1f)] [Increment(0.01f)] [DefaultValue(1f)] public float MaxIntensity { get; set; }
    [Range(0.1f,0.9f)] [Increment(0.01f)] [DefaultValue(0.2f)] public float MinIntensity { get; set; }
    #endregion
}