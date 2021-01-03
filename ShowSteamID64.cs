namespace Oxide.Plugins
{
	[Info("Show SteamID64", "klauz24", "1.0.0")]
	internal class ShowSteamID64 : HurtworldPlugin
	{
		[ChatCommand("id")]
		private void ShowSteamId64Command(PlayerSession session)
        {
			hurt.SendChatMessage(session, "Your SteamID64:", session.SteamId.ToString());
        }
	}
}