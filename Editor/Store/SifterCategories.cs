using System.Collections.Generic;
using UnityEditor;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Every category this project has produced, and which of them are being shown.
	/// <para>
	/// Kept rather than worked out afresh, so that turning one off does not make it vanish from the
	/// list the moment its last log ages out of the buffer. A category is remembered from the first
	/// time it is seen until it is explicitly forgotten.
	/// </para>
	/// </summary>
	public static class SifterCategories
	{
		/// <summary>Anything that came from Unity itself or from a package.</summary>
		public const string Unity = "Unity";

		/// <summary>Anything the compiler said.</summary>
		public const string Compiler = "Compiler";

		/// <summary>The project's own code, where it is in no namespace to be named after.</summary>
		public const string Project = "Project";

		private const string KnownKey = "CookieJarTools.Sifter.Categories";
		private const string ShownKey = "CookieJarTools.Sifter.Category.";
		private const char Apart = '\n';

		private static readonly List<string> known = new List<string>();
		private static bool read;

		/// <summary>Raised when a category is met for the first time, so a list of them can be redrawn.</summary>
		public static event System.Action Grew = delegate { };

		/// <summary>Every category ever met, the two built-in ones first and the rest in order.</summary>
		public static List<string> Known
		{
			get
			{
				Read();
				return new List<string>(known);
			}
		}

		public static bool Builtin(string name)
		{
			return name == Unity || name == Compiler || name == Project;
		}

		/// <summary>
		/// Remembers a category and everything it sits under, so the list fills itself into a tree.
		/// Meeting CookieJarTools.Sifter puts CookieJarTools in the list as well, which is what
		/// makes turning off a whole tool's worth of logs one click rather than six.
		/// </summary>
		public static void Met(string name)
		{
			Read();
			if (name == null || name.Length == 0 || known.Contains(name))
			{
				return;
			}

			foreach (var branch in Branches(name))
			{
				Add(branch);
			}

			Sort();
			Save();
			Grew();
		}

		/// <summary>A category and every category it sits under, outermost first.</summary>
		public static List<string> Branches(string name)
		{
			var branches = new List<string>();
			for (var at = name.IndexOf('.'); at > 0; at = name.IndexOf('.', at + 1))
			{
				branches.Add(name.Substring(0, at));
			}

			branches.Add(name);
			return branches;
		}

		/// <summary>How deep in the tree a category sits, which is how far the list indents it.</summary>
		public static int Depth(string name)
		{
			var deep = 0;
			foreach (var letter in name)
			{
				deep += letter == '.' ? 1 : 0;
			}

			return deep;
		}

		/// <summary>The last part of the name, which is the only part that is not its parents.</summary>
		public static string Leaf(string name)
		{
			var at = name.LastIndexOf('.');
			return at < 0 ? name : name.Substring(at + 1);
		}

		/// <summary>
		/// Whether a category is being shown. Turning off a parent turns off everything under it,
		/// because a tree where switching off CookieJarTools left its six tools still talking would
		/// not be a tree.
		/// </summary>
		public static bool Shown(string name)
		{
			foreach (var branch in Branches(name))
			{
				if (!Alone(branch))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>Whether this one is on, ignoring whatever it sits under.</summary>
		public static bool Alone(string name)
		{
			return EditorPrefs.GetBool(ShownKey + name, true);
		}

		public static void Show(string name, bool shown)
		{
			EditorPrefs.SetBool(ShownKey + name, shown);
		}

		public static void ShowAll(bool shown)
		{
			foreach (var name in Known)
			{
				Show(name, shown);
			}
		}

		/// <summary>Drops every category that nothing held belongs to any more.</summary>
		public static void Forget(IEnumerable<string> keeping)
		{
			Read();
			var alive = new HashSet<string>(keeping);
			known.RemoveAll(name => !Builtin(name) && !alive.Contains(name));
			Save();
			Grew();
		}

		private static void Read()
		{
			if (read)
			{
				return;
			}

			read = true;
			known.Clear();
			known.AddRange(EditorPrefs.GetString(KnownKey, string.Empty)
				.Split(Apart, System.StringSplitOptions.RemoveEmptyEntries));
			Builtins();
			Sort();
		}

		private static void Builtins()
		{
			foreach (var name in new[] { Unity, Compiler, Project })
			{
				Add(name);
			}
		}

		private static void Add(string name)
		{
			if (!known.Contains(name))
			{
				known.Add(name);
			}
		}

		/// <summary>The built-in three first, because they are the ones always there, then the rest.</summary>
		private static void Sort()
		{
			known.Sort(Before);
		}

		private static int Before(string left, string right)
		{
			var one = Builtin(left) ? 0 : 1;
			var two = Builtin(right) ? 0 : 1;
			return one == two ? string.CompareOrdinal(left, right) : one - two;
		}

		private static void Save()
		{
			EditorPrefs.SetString(KnownKey, string.Join(Apart.ToString(), known));
		}
	}
}
