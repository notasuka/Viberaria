using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.Localization;

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
            ChatHelper.SendChatMessageToClient(LiteralText(msg), color, Main.myPlayer);
        }
    }
}