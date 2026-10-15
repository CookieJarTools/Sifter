using UnityEditor;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Which of the three toolbar toggles are on. Kept apart from the window so the choice outlives
	/// closing it, which is what everybody expects of Unity's own three.
	/// </summary>
	public static class SifterLevels
	{
		private const string Key = "CookieJarTools.Sifter.Level.";

		public static bool Shown(SifterKind kind)
		{
			return EditorPrefs.GetBool(Key + Bucket(kind), true);
		}

		public static void Show(SifterKind kind, bool shown)
		{
			EditorPrefs.SetBool(Key + Bucket(kind), shown);
		}

		/// <summary>
		/// Which of the three a kind counts against. The same grouping Unity uses, so an exception
		/// hides with the errors and a compile warning with the warnings.
		/// </summary>
		private static string Bucket(SifterKind kind)
		{
			if (SifterKinds.IsError(kind))
			{
				return "Error";
			}

			return SifterKinds.IsWarning(kind) ? "Warning" : "Log";
		}
	}
}
