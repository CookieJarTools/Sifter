using CookieJarTools.Sifter.Editor;
using NUnit.Framework;
using UnityEditor.Compilation;
using UnityEngine;

namespace CookieJarTools.Sifter.Tests
{
	public sealed class SifterKindsTests
	{
		[Test]
		public void EveryRuntimeLogTypeHasAKind()
		{
			Assert.AreEqual(SifterKind.Log, SifterKinds.Of(LogType.Log));
			Assert.AreEqual(SifterKind.Warning, SifterKinds.Of(LogType.Warning));
			Assert.AreEqual(SifterKind.Error, SifterKinds.Of(LogType.Error));
			Assert.AreEqual(SifterKind.Assert, SifterKinds.Of(LogType.Assert));
			Assert.AreEqual(SifterKind.Exception, SifterKinds.Of(LogType.Exception));
		}

		[Test]
		public void TheCompilerKeepsItsOwnTwoKinds()
		{
			Assert.AreEqual(SifterKind.CompileError, SifterKinds.Of(CompilerMessageType.Error));
			Assert.AreEqual(SifterKind.CompileWarning, SifterKinds.Of(CompilerMessageType.Warning));
			Assert.AreEqual(SifterKind.Log, SifterKinds.Of(CompilerMessageType.Info));
		}

		[Test]
		public void TheRedNumberCountsFourKinds()
		{
			Assert.IsTrue(SifterKinds.IsError(SifterKind.Error));
			Assert.IsTrue(SifterKinds.IsError(SifterKind.Exception));
			Assert.IsTrue(SifterKinds.IsError(SifterKind.Assert));
			Assert.IsTrue(SifterKinds.IsError(SifterKind.CompileError));
			Assert.IsFalse(SifterKinds.IsError(SifterKind.Warning));
			Assert.IsFalse(SifterKinds.IsError(SifterKind.Log));
		}

		[Test]
		public void OnlyTheCompilerKindsSayTheyHaveNoStack()
		{
			Assert.IsTrue(SifterKinds.IsCompiler(SifterKind.CompileError));
			Assert.IsTrue(SifterKinds.IsCompiler(SifterKind.CompileWarning));
			Assert.IsFalse(SifterKinds.IsCompiler(SifterKind.Error));
		}
	}
}
