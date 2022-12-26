namespace Oxide.Plugins
{
	[Info("Anti Active Session", "klauz24", "1.0.1")]
	internal class AntiActiveSession : HurtworldPlugin
	{
		private void OnServerInitialized()
		{
			timer.Every(5f, () =>
			{
				foreach (var session in GameManager.Instance._steamIdSession.Values)
				{
					if (session != null && !session.IsLoaded)
					{
						GameManager.Instance._steamIdSession.Remove(session.SteamId);
					}
				}
			});
		}
	}
}
