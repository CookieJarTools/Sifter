# Changelog

All notable changes to Sifter are written down here. The format is
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.1] - 2026-10-17

### Fixed

- **The list stopped following new logs for no reason you could see.** It worked out whether to
  scroll by looking at where the list was sitting just before it refilled, and a log arriving makes
  the list longer, which moves the end away from you without anybody having touched anything. So it
  read its own growth as you having scrolled off, and quietly stopped. Following is a thing the
  window remembers now: on from the moment it opens, off the moment you scroll away yourself, and
  on again as soon as you scroll back to the end. Clearing the list turns it back on, since an empty
  list is at its end by definition.

## [0.1.0] - 2026-10-15

First release.

### Added

- **A console window that colours a stack trace by what each part is.** The namespace, class,
  method, parameters, file and line each have their own colour, and each can be turned off on its
  own. Unity draws all of them in the same grey, which is why reading one has always been a
  scanning exercise.
- **Clicking any frame opens that file at that line.** Not only the frame that called `Debug.Log`,
  because the line you want is almost never the line that logged.
- **Compiler generated names are turned back into what you wrote.** A lambda arrives as
  `<Resolve>b__0` on a class called `<>c__DisplayClass18_0` and an async body as `MoveNext` on a
  class called `<Resolve>d__12`. Both read as `Resolve`, with the line as it arrived in the tip.
- **Categories worked out from the log itself.** A bracketed word at the front of a message, or the
  namespace of the first frame of your own code, two parts deep by default. They form a tree, each
  branch gets a colour from its own name, and turning off a parent turns off everything under it.
  Nothing in a game references this package and there is no attribute to add.
- **The list survives a recompile**, and by choice entering play mode and closing the editor. Logs
  are held in a ring buffer and kept in `Library`, where losing them costs nothing.
- **My Code Only**, which folds away every frame that is not under `Assets`, and `[HideInCallstack]`,
  which Unity defines and leaves in the text everywhere except its own console.
- **A settings screen with a live sample**, drawn with the console's own row and painter so it
  cannot drift away from what you will actually get. Row height, colours per part, colours per kind,
  colours per category, how deep a category goes, how many logs are held, and what survives a reload.
- **Compact mode and a log pane that can open beside the list** rather than under it, for a window
  docked in a corner where there is height and no width.

### Known

- **It cannot fully replace Unity's console.** The profiler window and some of the editor's own
  double clicks open the built-in one regardless, and nothing a package can do changes that.
- **What was on screen before Sifter loaded is read once from an undocumented API.** If a Unity
  version moves it the window says so rather than going quiet, and everything else carries on.
