using UnityEngine;

namespace CookieJarTools.Sifter.Tests
{
	/// <summary>
	/// A wrapper around Debug.Log, which is exactly what HideInCallstack is for and exactly the
	/// thing that puts a useless frame at the top of every trace in a project when it is ignored.
	/// </summary>
	public static class SifterHideProbe
	{
		[HideInCallstack]
		public static void Wrapper(string message)
		{
			Debug.Log(message);
		}

		public static void Plain(string message)
		{
			Debug.Log(message);
		}
	}
}
