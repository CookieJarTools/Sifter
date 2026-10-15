using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Whether a frame asked not to be shown.
	/// <para>
	/// Unity defines HideInCallstack and strips the frame when it draws its own console, but it
	/// leaves the frame in the text every other way of reading a log hands over. Measured in
	/// 6000.4: a marked wrapper is still in the string from Application.logMessageReceived and
	/// still in the one from the console backend. So honouring it is work this has to do, and it
	/// matters the moment anybody wraps Debug.Log in a helper, because otherwise every trace in
	/// the project starts at the wrapper.
	/// </para>
	/// <para>
	/// A lookup costs a walk over the loaded assemblies, so the answer is kept. It cannot change
	/// without a recompile, and a recompile takes the whole cache with it.
	/// </para>
	/// </summary>
	public static class SifterHidden
	{
		private const BindingFlags Anywhere = BindingFlags.Static | BindingFlags.Instance
			| BindingFlags.Public | BindingFlags.NonPublic;

		private static readonly Dictionary<string, bool> Known = new Dictionary<string, bool>();

		public static bool Is(SifterFrame frame)
		{
			if (frame == null || !frame.Read || frame.Method.Length == 0)
			{
				return false;
			}

			var key = frame.Declaring + ":" + frame.Method;
			bool hidden;
			if (Known.TryGetValue(key, out hidden))
			{
				return hidden;
			}

			hidden = Marked(frame);
			Known[key] = hidden;
			return hidden;
		}

		private static bool Marked(SifterFrame frame)
		{
			var owner = Find(frame.Declaring);
			return owner != null && (Attributed(owner) || Any(owner, frame.Method));
		}

		private static bool Any(Type owner, string method)
		{
			foreach (var found in owner.GetMethods(Anywhere))
			{
				if (found.Name == method && Attributed(found))
				{
					return true;
				}
			}

			return false;
		}

		private static bool Attributed(MemberInfo member)
		{
			return member.IsDefined(typeof(HideInCallstackAttribute), false);
		}

		/// <summary>
		/// A stack gives a type's name and nothing about which assembly it is in, so the only way
		/// to it is to ask each of them. Done once per type and then remembered.
		/// </summary>
		private static Type Find(string name)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var found = Got(assembly, name);
				if (found != null)
				{
					return found;
				}
			}

			return null;
		}

		private static Type Got(Assembly assembly, string name)
		{
			try
			{
				return assembly.GetType(name, false);
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
