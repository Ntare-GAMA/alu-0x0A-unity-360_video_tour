# Scene Lifecycle

Menu requests are ignored while a scene fade is running. Tour requests are
ignored while a panorama transition is running. A valid transition fades to
black, swaps the active location, and restores visibility before accepting the
next request.

This ordering keeps rapid trigger presses from leaving multiple panorama
objects active or interrupting a fade halfway through.