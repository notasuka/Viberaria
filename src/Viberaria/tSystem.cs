using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

using static Viberaria.bVibration;

namespace Viberaria;

class ConfigUI : UIState
{
    
}

public class tSystem : ModSystem
{
    public static tSystem tSys;
    
    public bool WorldLoaded = false;

    public override void OnWorldUnload()
    {
        WorldLoaded = false;
        Halt();
    }

    public override void OnWorldLoad()
        => WorldLoaded = true;

    internal UserInterface _configInterface;
    internal ConfigUI _configUi;

    internal void ShowConfigUI()
    {
        _configInterface?.SetState(_configUi);
    }

    internal void HideConfigUI()
    {
        _configInterface?.SetState(null);
    }

    private GameTime _lastUpdateUiGameTime;
    public override void UpdateUI(GameTime gameTime)
    {
        _lastUpdateUiGameTime = gameTime;
        if (_configInterface?.CurrentState != null)
        {
            _configInterface.Update(gameTime);
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
        if (mouseTextIndex != -1)
        {
            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "Viberaria: Config",
                delegate
                {
                    if (_lastUpdateUiGameTime != null && _configInterface?.CurrentState != null)
                    {
                        _configInterface.Draw(Main.spriteBatch, _lastUpdateUiGameTime);
                    }
                    return true;
                },
                InterfaceScaleType.UI
            ));
        }
    }

    public override void Load()
    {
        if (!Main.dedServ)
        {
            _configInterface = new UserInterface();
            _configUi = new ConfigUI();
            _configUi.Activate();
        }
    }

    public override void Unload()
    {
        _configUi = null;
    }
}