using System;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Wires the log up to everything that produces one, and keeps it across a reload.
	/// <para>
	/// The live stream is Application.logMessageReceivedThreaded rather than Unity's console
	/// backend, because the backend is filtered by what is typed into the built-in console. The
	/// threaded form is used so that a log from a worker thread is still caught, and says that it
	/// came from one.
	/// </para>
	/// </summary>
	[InitializeOnLoad]
	public static class SifterCapture
	{
		private static readonly int Main = Thread.CurrentThread.ManagedThreadId;

		static SifterCapture()
		{
			SifterSession.Restore(SifterLogs.Current);
			if (SifterBackfill.Run(SifterLogs.Current) > 0)
			{
				SifterLogs.Current.Sort();
			}

			Listen();
		}

		private static void Listen()
		{
			Application.logMessageReceivedThreaded -= Heard;
			Application.logMessageReceivedThreaded += Heard;
			AssemblyReloadEvents.beforeAssemblyReload -= Leaving;
			AssemblyReloadEvents.beforeAssemblyReload += Leaving;
			EditorApplication.playModeStateChanged -= Played;
			EditorApplication.playModeStateChanged += Played;
			EditorApplication.quitting -= Quitting;
			EditorApplication.quitting += Quitting;
			EditorApplication.update -= Tick;
			EditorApplication.update += Tick;
			SifterCompiler.Listen(SifterLogs.Current);
		}

		private static void Heard(string message, string trace, LogType type)
		{
			if (string.IsNullOrEmpty(trace) && SifterCompiler.Reported(message))
			{
				return;
			}

			var off = Thread.CurrentThread.ManagedThreadId != Main;
			SifterLogs.Current.Add(new SifterEntry(SifterKinds.Of(type), message, trace,
				DateTime.UtcNow.Ticks, string.Empty, 0, Frame(off), off));
		}

		/// <summary>
		/// Which frame this happened on, which is often the whole answer and which Unity's console
		/// does not show. Time.frameCount may only be read on the main thread, and outside play
		/// mode it counts editor repaints, which means nothing to anybody.
		/// </summary>
		private static int Frame(bool offMainThread)
		{
			if (offMainThread || !EditorApplication.isPlaying)
			{
				return -1;
			}

			return Time.frameCount;
		}

		private static void Tick()
		{
			SifterLogs.Current.Settle();
		}

		private static void Leaving()
		{
			SifterSession.Save(SifterLogs.Current);
		}

		private static void Played(PlayModeStateChange change)
		{
			if (change != PlayModeStateChange.ExitingEditMode || SifterSettings.SurvivePlay)
			{
				return;
			}

			SifterLogs.Current.Clear();
		}

		private static void Quitting()
		{
			if (!SifterSettings.SurviveRestart)
			{
				SifterSession.Forget();
				return;
			}

			SifterSession.Save(SifterLogs.Current);
		}
	}
}
