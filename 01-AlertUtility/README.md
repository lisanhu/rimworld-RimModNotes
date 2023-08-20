# AlertUtility Notes

## Key points

- How to add an icon to the toggle icon area?
  - Using Harmony to patch RimWorld.PlaySettings.DoPlaySettingsGlobalControls
- How to draw a window?
  - Create a class extends Verse.Window
  - Use Find.WindowStack.Add and the class ctor
- How to remove/close a window?
  - Find.WindowStack.TryRemove with typeof(WindowClass)
- How to draw window?
  - Several functions to check:
    - SetInitialSizeAndPosition
    - PreOpen
    - DoWindowContents
    - PostClose
    - etc.
  - Listing_Standard, Widgets classes are having useful functions to create elements
- How to run things on every tick of the game?
  - Extends the WorldComponent/GameComponent class
  - Every subclass of WorldComponent/GameComponent will automatically be created at the start of the game
  - WorldComponentTick function
