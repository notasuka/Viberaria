using System;
using log4net;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.Localization;
using Terraria.ModLoader;
using static Viberaria.Config.ViberariaConfig;

namespace Viberaria;

public static class tChat
{
    public static ILog Logger => ModContent.GetInstance<Viberaria>().Logger;

    private static NetworkText LiteralText(string msg)
    {
        return NetworkText.FromLiteral(msg);
    }

    /// <summary>
    /// Send a message to the player client in chat with a given color.
    /// </summary>
    /// <param name="msg">The message to send. Is automatically prefixed with the timestamp when the Debug config has been enabled.</param>
    /// <param name="color">The color the message should have.</param>
    public static void LogToPlayer(string msg, Color color)
    {
        if (!Main.dedServ)
        {
            if (Instance.Debug.Enabled)
            {
                string time = DateTime.Now.Minute + ":" +
                              (DateTime.Now.Second + DateTime.Now.Millisecond * 0.001f).ToString("00.00");
                msg = time + "| " + msg;
            }

            ChatHelper.SendChatMessageToClient(LiteralText(msg), color, Main.myPlayer);
        }
    }
}