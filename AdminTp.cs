namespace Oxide.Plugins
{

    [Info("Admin Tp", "klauz24", "1.0.0")]
    internal class AdminTp : HurtworldPlugin
    {
        [ChatCommand("tp")]
        private void TpCommand(PlayerSession session, string command, string[] args)
        {
            if (session.IsAdmin)
            {
                if (args.Length == 1)
                {
                    var targetIPlayer = covalence.Players.FindPlayer(args[0].ToLower());
                    if (targetIPlayer != null)
                    {
                        session.IPlayer.Teleport(targetIPlayer.Position());
                        hurt.SendChatMessage(session, "<color=red>[Admin Tp]]</color>", $"Teleported to {targetIPlayer.Name}.");
                    }
                    else
                    {
                        hurt.SendChatMessage(session, "<color=red>[Admin Tp]</color>", $"Failed to find {args[0]}.");
                    }


                }
                else
                {
                    hurt.SendChatMessage(session, "<color=red>[Admin Tp]</color>", "Syntax: /tp <playerName>.");
                }
            }
        }
    }
}
