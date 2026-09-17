# Troubleshooting

## Buttons do nothing

Check that the scene uses the tracked-device input module and that each canvas
has a tracked-device raycaster. Confirm the controller ray reaches the
world-space canvas before checking the button callback.

## A panorama does not appear

Inspect the navigator reference fields and ensure only one location object is
active at scene start. The navigator ignores missing references, so a typo in
Inspector wiring can otherwise look like an empty destination.