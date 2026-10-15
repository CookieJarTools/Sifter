using UnityEditor.Compilation;
using UnityEngine;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Unity says the same few things in three different vocabularies: a LogType from the runtime,
	/// a CompilerMessageType from the compiler, and a bitmask from the console backend. This turns
	/// the first two into one word. The third is read in SifterConsole, where the bits live.
	/// </summary>
	public static class SifterKinds
	{
		public static SifterKind Of(LogType type)
		{
			switch (type)
			{
				case LogType.Warning:
					return SifterKind.Warning;
				case LogType.Assert:
					return SifterKind.Assert;
				case LogType.Exception:
					return SifterKind.Exception;
				case LogType.Error:
					return SifterKind.Error;
				default:
					return SifterKind.Log;
			}
		}

		public static SifterKind Of(CompilerMessageType type)
		{
			switch (type)
			{
				case CompilerMessageType.Error:
					return SifterKind.CompileError;
				case CompilerMessageType.Warning:
					return SifterKind.CompileWarning;
				default:
					return SifterKind.Log;
			}
		}

		/// <summary>What a kind is called when it is shown to somebody.</summary>
		public static string Say(SifterKind kind)
		{
			if (kind == SifterKind.CompileError)
			{
				return "Compile Error";
			}

			return kind == SifterKind.CompileWarning ? "Compile Warning" : kind.ToString();
		}

		/// <summary>Whether Unity's console would count this against the red number.</summary>
		public static bool IsError(SifterKind kind)
		{
			return kind == SifterKind.Error || kind == SifterKind.Exception
				|| kind == SifterKind.Assert || kind == SifterKind.CompileError;
		}

		/// <summary>Whether Unity's console would count this against the yellow number.</summary>
		public static bool IsWarning(SifterKind kind)
		{
			return kind == SifterKind.Warning || kind == SifterKind.CompileWarning;
		}

		/// <summary>Whether the compiler produced it, which is how you know it carries no stack.</summary>
		public static bool IsCompiler(SifterKind kind)
		{
			return kind == SifterKind.CompileError || kind == SifterKind.CompileWarning;
		}
	}
}
