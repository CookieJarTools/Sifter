using UnityEditor;
using UnityEngine;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// What the user has chosen. Kept per machine rather than in the project, because how many logs
	/// someone wants to hold on to is about their machine and not about the game.
	/// </summary>
	public static class SifterSettings
	{
		public const int Fewest = 100;
		public const int Most = 100000;

		private const string CapacityKey = "CookieJarTools.Sifter.Capacity";
		private const string PlayKey = "CookieJarTools.Sifter.SurvivePlay";
		private const string RestartKey = "CookieJarTools.Sifter.SurviveRestart";
		private const int Usually = 5000;

		/// <summary>How many entries are held before the oldest starts being dropped.</summary>
		public static int Capacity
		{
			get { return Mathf.Clamp(EditorPrefs.GetInt(CapacityKey, Usually), Fewest, Most); }
			set
			{
				EditorPrefs.SetInt(CapacityKey, Mathf.Clamp(value, Fewest, Most));
				SifterLogs.Current.Resize(Capacity);
			}
		}

		/// <summary>Whether entering play mode keeps what is already there.</summary>
		public static bool SurvivePlay
		{
			get { return EditorPrefs.GetBool(PlayKey, true); }
			set { EditorPrefs.SetBool(PlayKey, value); }
		}

		/// <summary>Whether closing the editor keeps what is already there.</summary>
		public static bool SurviveRestart
		{
			get { return EditorPrefs.GetBool(RestartKey, true); }
			set { EditorPrefs.SetBool(RestartKey, value); }
		}
	}
}
