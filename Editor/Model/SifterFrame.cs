namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// One line of a stack, taken apart.
	/// <para>
	/// Raw is kept whatever happens. A stack is text and its shape varies with the backend, the
	/// compiler, and whether what ran was a lambda or an async body, so a line that is not
	/// understood is shown exactly as it arrived rather than mangled into something that looks
	/// parsed and is wrong.
	/// </para>
	/// </summary>
	public sealed class SifterFrame
	{
		/// <summary>A line that could not be taken apart, kept whole.</summary>
		public SifterFrame(string raw)
		{
			Raw = raw ?? string.Empty;
			Namespace = string.Empty;
			Class = string.Empty;
			Method = string.Empty;
			Parameters = string.Empty;
			File = string.Empty;
			Declaring = string.Empty;
		}

		public SifterFrame(string raw, string space, string type, string method, string parameters,
			string file, int line, string declaring)
		{
			Raw = raw ?? string.Empty;
			Namespace = space ?? string.Empty;
			Class = type ?? string.Empty;
			Method = method ?? string.Empty;
			Parameters = parameters ?? string.Empty;
			File = file ?? string.Empty;
			Line = line;
			Declaring = declaring ?? string.Empty;
			Read = true;
		}

		public string Raw { get; }

		/// <summary>Whether this was understood. When it was not, only Raw means anything.</summary>
		public bool Read { get; }

		public string Namespace { get; }

		public string Class { get; }

		public string Method { get; }

		/// <summary>The parameter types as written, with nothing between them but commas.</summary>
		public string Parameters { get; }

		public string File { get; }

		public int Line { get; }

		/// <summary>
		/// The type's name as reflection spells it, kept so the attributes on a method can be
		/// looked up. Not for showing: it still carries the nesting marks and the generic arity.
		/// </summary>
		public string Declaring { get; }

		/// <summary>Whether there is a file and a line to go to.</summary>
		public bool Somewhere
		{
			get { return File.Length > 0 && Line > 0; }
		}

		/// <summary>
		/// Whether this is the project's own code.
		/// <para>
		/// Decided by the file being under Assets, which is the same thing as an assembly defined
		/// by an asmdef there, since that is the only way a file under Assets gets compiled. It
		/// needs no list kept up to date and it is right on the day it is installed.
		/// </para>
		/// </summary>
		public bool Mine
		{
			get { return Ours(File); }
		}

		private static bool Ours(string file)
		{
			if (file.Length == 0)
			{
				return false;
			}

			var path = file.Replace('\\', '/');
			return path.StartsWith("Assets/") || path.Contains("/Assets/");
		}
	}
}
