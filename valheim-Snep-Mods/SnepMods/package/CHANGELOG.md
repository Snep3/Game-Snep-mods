# Changelog

## 1.2.2
- Fixed a `NullReferenceException` from FloatingItems in the console on the main
  menu (character select). Items created outside a world (e.g. for the character
  preview) are now skipped instead of trying to claim network ownership.

## 1.2.1
- Added **FloatingItems**: dropped items float on the surface of water instead of
  sinking. Toggle key: F10, adjustable buoyancy `Force` in the settings menu.
- FloatingItems actively claims ownership of dropped items (`RequestOwn`) so
  buoyancy applies reliably in multiplayer, instead of only on whichever player
  happens to be nearest when the item spawns.
