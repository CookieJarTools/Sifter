using System.Collections.Generic;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Which category a log belongs to, worked out from the log itself.
	/// <para>
	/// There is deliberately no attribute and no API. A game should not have to reference a console
	/// to be readable by it, and anything that needed call sites edited would only ever cover the
	/// logs somebody got round to editing. Everything already in a project is categorised the day
	/// this is installed.
	/// </para>
	/// <para>
	/// The order the four rules are tried in is the whole of it. A bracketed word at the front wins,
	/// because somebody wrote that on purpose. Then the compiler, which is its own thing. Then the
	/// first frame of the project's own code, whose root namespace is the nearest thing a log has to
	/// a department. Anything left came from Unity or a package.
	/// </para>
	/// </summary>
	public static class SifterCategory
	{
		/// <summary>Longer than this and it is a sentence in brackets, not a tag.</summary>
		private const int Longest = 24;

		public static string Named(SifterEntry entry)
		{
			var tagged = Bracketed(entry.Message);
			if (tagged.Length > 0)
			{
				return tagged;
			}

			if (SifterKinds.IsCompiler(entry.Kind))
			{
				return SifterCategories.Compiler;
			}

			return Where(entry.Frames);
		}

		private static string Where(List<SifterFrame> frames)
		{
			var frame = Ours(frames);
			if (frame == null)
			{
				return SifterCategories.Unity;
			}

			var root = Root(frame.Namespace, SifterShown.CategoryDepth);
			return root.Length > 0 ? root : SifterCategories.Project;
		}

		private static SifterFrame Ours(List<SifterFrame> frames)
		{
			foreach (var frame in frames)
			{
				if (frame.Mine)
				{
					return frame;
				}
			}

			return null;
		}

		/// <summary>
		/// The first few parts of a namespace. One part is the company and tells you nothing in a
		/// project where everything shares it, so by default it takes two and a log from
		/// CookieJarTools.Sifter.Editor belongs to CookieJarTools.Sifter.
		/// </summary>
		private static string Root(string space, int parts)
		{
			var at = -1;
			for (var i = 0; i < parts; i++)
			{
				var next = space.IndexOf('.', at + 1);
				if (next < 0)
				{
					return space;
				}

				at = next;
			}

			return space.Substring(0, at);
		}

		/// <summary>
		/// A word in brackets at the very front, which is how most projects already mark a log and
		/// how it still reads correctly in Unity's own console.
		/// </summary>
		private static string Bracketed(string message)
		{
			if (message.Length < 3 || message[0] != '[')
			{
				return string.Empty;
			}

			var close = message.IndexOf(']');
			if (close < 2 || close > Longest)
			{
				return string.Empty;
			}

			var name = message.Substring(1, close - 1).Trim();
			return Wordlike(name) ? name : string.Empty;
		}

		/// <summary>
		/// Something that reads as a name. Without this a timestamp or an index in brackets, which
		/// plenty of logs begin with, would turn every single one of them into its own category.
		/// </summary>
		private static bool Wordlike(string name)
		{
			var letters = 0;
			foreach (var letter in name)
			{
				if (!Allowed(letter))
				{
					return false;
				}

				letters += char.IsLetter(letter) ? 1 : 0;
			}

			return letters > 0;
		}

		private static bool Allowed(char letter)
		{
			return char.IsLetter(letter) || char.IsDigit(letter)
				|| letter == ' ' || letter == '-' || letter == '_' || letter == '.';
		}
	}
}
