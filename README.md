# GAMA's Immersive Tour — VR Campus & Intranet Tour

A Meta Quest VR application built in Unity that lets you explore two 360° environments:
an **Intranet Tour** (four video-based rooms) and a **Custom Campus Tour** (three
photo-based locations: Stairs, Entrance, and the Fab Lab), navigated entirely with the
Quest Touch controllers.

## Requirements

- Unity **6000.4.5f1** (Unity 6) with the Android Build Support module
- Meta Quest 2 or Quest 3, or Quest Link for testing from the Editor
- Android SDK/NDK/OpenJDK (installed automatically with Unity's Android module)

## Scenes

| Scene | Purpose |
|---|---|
| `Assets/Scenes/MainMenuScene.unity` | Entry point. World-space VR menu with two buttons that load the tours. |
| `Assets/Scenes/IntranetTourScene.unity` | Four video panoramas — Living Room, Cantina, Cube, Mezzanine — fully interconnected. |
| `Assets/Scenes/CustomCampusTourScene.unity` | Three photo panoramas — Stairs, Entrance, Fab Lab — linear tour (Stairs ↔ Entrance ↔ Fab Lab). |

Build Settings order: **Main Menu → Intranet Tour → Custom Campus Tour**.

## Controls

Point a Touch controller at any button and pull the trigger to select it:

- **Hotspot buttons** — move to the next location, with a fade transition.
- **Info buttons** (the small circular icon at each location) — toggle a short description of where you are.
- **Back to Menu** — always-visible panel near the bottom of your view in both tour scenes; fades out and returns to the Main Menu.
- **Main Menu buttons** — launch the Intranet Tour or the Custom Campus Tour.

## How the Custom Campus Tour photos were made

The three Custom Campus Tour photos were shot on an **Insta360 ONE X2** and are stored
as raw, unstitched dual-fisheye captures in `Assets/CUSTOM IMG/*.dng`. They were
converted to usable equirectangular panoramas with a custom fisheye-to-equirectangular
stitcher (no third-party stitching software was available on the build machine), and
the results are in `Assets/CUSTOM IMG/Converted/`. There is a small amount of ghosting
right at the seam between the two lenses where people happened to be standing during
capture — an inherent limit of stitching without that camera's exact lens calibration
data. For a cleaner result in the future, re-export the same three `.dng` files through
Insta360 Studio (free) and drop the equirectangular JPGs into
`Assets/CUSTOM IMG/Converted/` in place of the current PNGs.

## Building the APK

`File → Build Settings → Android → Build` (or `Build And Run` with a Quest connected).

**If Gradle fails with a `PKIX path building failed` SSL error**, it's not a Unity or
project problem — some antivirus/network-inspection software intercepts HTTPS and
Java's bundled certificate store doesn't trust it (Windows itself does, hence why other
tools work fine). Fix: import that root certificate into a Java trust store and point
Gradle at it, or fix it at the system/antivirus level. A prebuilt APK from this fix is
kept at `build/CampusVRTour.apk`.

## Known limitations

- The two tour scenes are independent — quitting one and picking the other from the
  Main Menu is the only way to switch between them (no direct link between Intranet and
  Custom Campus).
- The stitched panorama photos have a soft seam near people who were standing close to
  the camera at capture time (see above).
- Background music currently plays only in the Custom Campus Tour scene.
