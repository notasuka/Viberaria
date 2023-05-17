using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.Localization;

namespace Viberaria.Utilities;

public static class Chat
{
    private static NetworkText LiteralText(string msg)
    {
        return NetworkText.FromLiteral(msg);
    }
    public static void Log(string msg, Color color)
    {
        ChatHelper.SendChatMessageToClient(LiteralText(msg), color, Main.myPlayer);
    }
}