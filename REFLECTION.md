# Reflection

My workflow on this project was iterative and debugging-heavy rather than linear. I
picked it up mid-build, in a state where the Intranet Tour mostly worked but the Custom
Campus Tour didn't — wrong materials on two of the three panoramas, hotspot buttons
wired to methods that no longer existed, and no way to click anything in VR at all in
any of the three scenes. Rather than guess, I worked scene by scene: get one thing
verifiably working, confirm it on the actual headset, then move to the next. That
"confirm on-device" step mattered more than I expected — several bugs (the camera
spawning above the floor, the controller ray passing through buttons, the player
falling through the panorama sphere) only ever showed up once tested on the real Quest,
never in the Editor.

The biggest challenges were environmental rather than conceptual. The VR interaction
pipeline had a subtle mismatch: the scenes used one input module for UI events, but the
XR controller's ray interactor only knows how to register itself with a different one —
so clicking silently did nothing even once every other piece looked correct. Getting
floor-relative head tracking right, and then re-scaling the panorama spheres so a
real standing height didn't put the camera near the sphere's edge, took several rounds
of on-headset feedback to dial in.

Working with the 360 camera was its own lesson. I assumed the three campus photos
(shot on an Insta360 ONE X2) were ready equirectangular panoramas — they weren't. They
were raw, unstitched dual-fisheye captures, so I had to write a fisheye-to-equirectangular
stitching pass to turn them into something a sphere could actually display, which left
a small amount of seam ghosting near people who were standing close to the camera.

For scene navigation, I kept a single navigator script per tour holding references to
every location's GameObject, with one method per destination that hides the rest,
runs a fade, and activates the target — so every hotspot button wires into the same
logic instead of duplicating scene-switching code per room.

For VR UX, the priorities were comfort and reachability: fading to black between
locations instead of hard-cutting, using real controller ray-and-trigger selection
rather than a mouse-only interaction model, and adding a "Back to Menu" control that
stays anchored to the player's view so it's reachable from anywhere, regardless of
which location is active. I also disabled the rig's default gravity/locomotion, since
this is a stand-and-look experience with nothing to walk on.

Compared to the original Intranet experience, the main improvements were making VR
interaction actually functional end to end, moving the Main Menu into proper
world-space VR UI instead of a flat overlay, and adding the info toggles, back
button, and ambient music that round the tour out into something that feels finished
rather than assembled.
