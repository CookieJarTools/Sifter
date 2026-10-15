using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Turns the text of a stack into frames.
	/// <para>
	/// Unity writes a stack three ways and all three turn up in the same project. A log gives
	/// <c>Namespace.Class:Method (args) (at Assets/Thing.cs:12)</c>. An exception logged with
	/// LogException gives the same thing with no space before the bracket, and sometimes a dot
	/// instead of the colon. An exception's own ToString gives
	/// <c>at Namespace.Class.Method (args) [0x00001] in /path/Thing.cs:12</c>.
	/// </para>
	/// <para>
	/// So nothing is matched against a shape. The location comes off the end first, then the
	/// parameters are found by counting brackets back from the end, and what is left in front is
	/// the type and the method however they were separated. Looking for the first " (" instead,
	/// which is the obvious thing to do, quietly reads a whole file path as a parameter list on
	/// every frame of every exception.
	/// </para>
	/// </summary>
	public static class SifterFrames
	{
		/// <summary>Generic types arrive spelt List`1&lt;int&gt;, and the arity says nothing to anybody.</summary>
		private static readonly Regex Arity = new Regex("`\\d+", RegexOptions.Compiled);

		public static List<SifterFrame> Of(string trace)
		{
			var frames = new List<SifterFrame>();
			if (string.IsNullOrEmpty(trace))
			{
				return frames;
			}

			foreach (var line in trace.Split('\n'))
			{
				Take(frames, line.Trim());
			}

			return frames;
		}

		private static void Take(List<SifterFrame> frames, string line)
		{
			if (line.Length == 0)
			{
				return;
			}

			frames.Add(Read(line));
		}

		private static SifterFrame Read(string line)
		{
			var file = string.Empty;
			var number = 0;
			var text = Shorn(line, ref file, ref number);
			return Framed(line, text, file, number) ?? new SifterFrame(line);
		}

		/// <summary>
		/// Takes the file and the line off the end, written either way, and hands back what is left.
		/// </summary>
		private static string Shorn(string line, ref string file, ref int number)
		{
			var at = line.LastIndexOf(" (at ");
			if (at >= 0 && line.EndsWith(")") && Where(line.Substring(at + 5, line.Length - at - 6), ref file, ref number))
			{
				return line.Substring(0, at);
			}

			var within = line.LastIndexOf(" in ");
			if (within >= 0 && Where(line.Substring(within + 4).TrimEnd(), ref file, ref number))
			{
				return line.Substring(0, within);
			}

			return line;
		}

		/// <summary>
		/// Splits a file from its line on the last colon, because a Windows path has one of its own
		/// two characters along. A name in angle brackets is a module's id rather than a file, which
		/// is what a frame compiled without debug information looks like: the shape is understood,
		/// so the location still comes off the line, but there is nowhere to go.
		/// </summary>
		private static bool Where(string location, ref string file, ref int number)
		{
			var colon = location.LastIndexOf(':');
			int read;
			if (colon < 1 || !int.TryParse(location.Substring(colon + 1), out read))
			{
				return false;
			}

			number = read;
			file = location.StartsWith("<") ? string.Empty : location.Substring(0, colon);
			return true;
		}

		private static SifterFrame Framed(string raw, string text, string file, int line)
		{
			var body = Bare(text);
			var open = body.EndsWith(")") ? Opening(body) : -1;
			if (open < 1)
			{
				return null;
			}

			var head = body.Substring(0, open).TrimEnd();
			var colon = head.IndexOf(':');
			var cut = colon >= 0 ? colon : head.LastIndexOf('.');
			if (cut < 1)
			{
				return null;
			}

			return Made(raw, head.Substring(0, cut), head.Substring(cut + 1),
				body.Substring(open + 1, body.Length - open - 2), file, line);
		}

		/// <summary>The exception form puts an IL offset between the parameters and the file.</summary>
		private static string Bare(string text)
		{
			var trimmed = text.Trim();
			var offset = trimmed.LastIndexOf(" [0x");
			var body = offset < 0 ? trimmed : trimmed.Substring(0, offset).TrimEnd();
			return body.StartsWith("at ") ? body.Substring(3) : body;
		}

		/// <summary>
		/// Which bracket opens the parameter list, counted back from the closing one. A parameter
		/// can be a nested generic and a method name can be compiler generated, so the first
		/// bracket in the line is not reliably the right one.
		/// </summary>
		private static int Opening(string text)
		{
			var depth = 0;
			for (var i = text.Length - 1; i >= 0; i--)
			{
				depth += Step(text[i]);
				if (depth == 0 && text[i] == '(')
				{
					return i;
				}
			}

			return -1;
		}

		private static int Step(char letter)
		{
			if (letter == ')')
			{
				return 1;
			}

			return letter == '(' ? -1 : 0;
		}

		private static SifterFrame Made(string raw, string owner, string method, string parameters,
			string file, int line)
		{
			string whole, name;
			Written(owner, method, out whole, out name);
			var dot = whole.LastIndexOf('.');
			var space = dot < 0 ? string.Empty : whole.Substring(0, dot);
			var type = dot < 0 ? whole : whole.Substring(dot + 1);
			return new SifterFrame(raw, space, Tidy(type), name, parameters, file, line,
				owner.Replace('/', '+'));
		}

		/// <summary>
		/// Turns what the compiler generated back into what somebody wrote.
		/// <para>
		/// A lambda ends up as a method called &lt;Outer&gt;b__3_1 on a class called
		/// &lt;&gt;c__DisplayClass18_0, and an async or iterator body ends up as MoveNext on a class
		/// called &lt;Outer&gt;d__12. Left alone, half of a real stack is machinery nobody wrote and
		/// the frame you are looking for is hard to pick out of it.
		/// </para>
		/// </summary>
		private static void Written(string owner, string method, out string whole, out string name)
		{
			// A dot is in here with the two nesting marks because the compact form an exception is
			// logged with separates a nested type with one, so BasePipelineServer/<>c__DisplayClass134_0
			// arrives as BasePipelineServer.<>c__DisplayClass134_0. Only a part that opens with an
			// angle bracket is treated as generated, so an ordinary type name is never cut in half.
			var cut = owner.LastIndexOfAny(new[] { '+', '/', '.' });
			var nested = cut < 0 ? string.Empty : owner.Substring(cut + 1);
			if (!nested.StartsWith("<"))
			{
				whole = owner;
				name = Inner(method);
				return;
			}

			whole = owner.Substring(0, cut);
			name = nested.StartsWith("<>") ? Inner(method) : Inner(nested);
		}

		/// <summary>The name between the angle brackets, which is the one in the source.</summary>
		private static string Inner(string name)
		{
			var close = name.IndexOf('>');
			return name.StartsWith("<") && close > 1 ? name.Substring(1, close - 1) : name;
		}

		private static string Tidy(string type)
		{
			return Arity.Replace(type.Replace('/', '.').Replace('+', '.'), string.Empty);
		}
	}
}
