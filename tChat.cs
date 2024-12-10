using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.Localization;
using static Viberaria.ViberariaConfig;

namespace Viberaria;

public static class tChat
{
    private static NetworkText LiteralText(string msg)
    {
        return NetworkText.FromLiteral(msg);
    }

    public static void LogToPlayer(string msg, Color color)
    {
        if (!Main.dedServ)
        {
            if (Instance.DebugChatMessages)
            {
                string time = DateTime.Now.Minute + ":" +
                              (DateTime.Now.Second + DateTime.Now.Millisecond * 0.001f).ToString("0.00");
                msg = time + "| " + msg;
            }
            ChatHelper.SendChatMessageToClient(LiteralText(msg), color, Main.myPlayer);
        }
    }
}