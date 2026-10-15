using System;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Made up logs for the settings screen to draw.
	/// <para>
	/// The screen shows them through the same row and the same painter the console uses, so what is
	/// previewed cannot drift away from what you will actually get. The stack is chosen to have one
	/// of everything in it: a top frame with nowhere to go, a frame of the project's own, an async
	/// body the compiler renamed, and a frame from a package.
	/// </para>
	/// </summary>
	public static class SifterSample
	{
		private const string Trace =
			"UnityEngine.Debug:LogError (object)\n"
			+ "MyGame.Combat.DamageSystem:Apply (MyGame.Combat.Hit,int) (at Assets/Scripts/Combat/DamageSystem.cs:84)\n"
			+ "MyGame.Combat.DamageSystem/<Resolve>d__12:MoveNext () (at Assets/Scripts/Combat/DamageSystem.cs:51)\n"
			+ "UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)\n"
			+ "Unity.Netcode.NetworkManager:Update () (at ./Library/PackageCache/com.unity.netcode/Runtime/NetworkManager.cs:1204)\n";

		public static SifterEntry[] Rows()
		{
			var now = DateTime.UtcNow.Ticks;
			return new[]
			{
				new SifterEntry(SifterKind.Log, "[Audio] mixer snapshot settled", Trace, now, string.Empty, 0, 182),
				new SifterEntry(SifterKind.Warning, "[Combat] two hits resolved on the same frame", Trace, now, string.Empty, 0, 182),
				new SifterEntry(SifterKind.Error, "[Combat] NullReferenceException while applying damage", Trace, now, string.Empty, 0, 183)
			};
		}

		public static SifterEntry Stack()
		{
			return new SifterEntry(SifterKind.Error,
				"NullReferenceException: Object reference not set to an instance of an object",
				Trace, DateTime.UtcNow.Ticks, string.Empty, 0, 183);
		}
	}
}
