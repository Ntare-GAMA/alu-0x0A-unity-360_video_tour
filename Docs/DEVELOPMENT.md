# Development Guide

This project is a Unity 6 VR tour for Meta Quest. The three scenes are ordered
as Main Menu, Intranet Tour, and Custom Campus Tour in Build Settings.

## Script ownership

- `MenuSceneLoader` handles scene changes from the main menu.
- `SceneNavigator` controls the four intranet panorama objects.
- `CampusTourNavigator` controls the three campus panorama objects.
- `InfoPanelToggle` owns the visibility of a location description panel.

Keep scene wiring in the Inspector and keep behavior in these scripts. This
makes hotspot changes reviewable without duplicating transition logic.