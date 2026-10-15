using System;
using System.Collections.Generic;
using UnityEditor.Compilation;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Compiler errors and warnings, which are the one thing a game cannot log for itself and the
	/// one thing Application.logMessageReceived never delivers.
	/// <para>
	/// This fires while the old domain is still alive, so what it captures is saved with everything
	/// else on the way out and is there to read the moment the editor comes back.
	/// </para>
	/// </summary>
	public static class SifterCompiler
	{
		private static readonly HashSet<string> Said = new HashSet<string>();

		private static SifterLog into;

		public static void Listen(SifterLog log)
		{
			into = log;
			CompilationPipeline.compilationStarted -= Starting;
			CompilationPipeline.compilationStarted += Starting;
			CompilationPipeline.assemblyCompilationFinished -= Finished;
			CompilationPipeline.assemblyCompilationFinished += Finished;
		}

		/// <summary>
		/// Whether the compiler has already reported this, which is how the same error is kept from
		/// landing twice.
		/// <para>
		/// Unity puts a compiler message through Application.logMessageReceived as well, as a plain
		/// error with no file, no line and no stack. That copy is the worse of the two and is the one
		/// dropped, because it cannot say which file it is about or that the compiler said it.
		/// </para>
		/// </summary>
		public static bool Reported(string message)
		{
			return message != null && Said.Contains(message);
		}

		private static void Starting(object what)
		{
			Said.Clear();
		}

		private static void Finished(string assembly, CompilerMessage[] messages)
		{
			foreach (var message in messages)
			{
				Said.Add(message.message);
				into.Add(Entry(message));
			}
		}

		private static SifterEntry Entry(CompilerMessage message)
		{
			return new SifterEntry(SifterKinds.Of(message.type), message.message, string.Empty,
				DateTime.UtcNow.Ticks, message.file, message.line);
		}
	}
}
