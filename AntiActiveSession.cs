namespace Oxide.Plugins
{
	// This plugin might cause issues, use it on your own risk!

	[Info("Anti Active Session", "klauz24", "1.0.0")]
	internal class AntiActiveSession : HurtworldPlugin
	{
		private void OnServerInitialized()
		{
			timer.Every(3f, () =>
			{
				foreach(var session in GameManager.Instance._steamIdSession.Values)
                {
					if (session != null && !session.IsLoaded)
                    {
						// This player is not on the server but has active session, removing it.
						GameManager.Instance._steamIdSession.Remove(session.SteamId);
					}
                }
			});
		}
	}
}