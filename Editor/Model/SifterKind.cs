namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// What a captured log is.
	/// <para>
	/// The two compiler kinds are kept apart from the ordinary error and warning on purpose. A
	/// compile error is not something the game did, it has no stack, and it is almost always the
	/// only thing you want to look at while it is there.
	/// </para>
	/// </summary>
	public enum SifterKind
	{
		Log,
		Warning,
		Error,
		Assert,
		Exception,
		CompileError,
		CompileWarning
	}
}
