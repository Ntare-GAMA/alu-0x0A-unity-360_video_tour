# Runtime Architecture

The Main Menu delegates scene loading to `MenuSceneLoader`. Each tour scene
owns one navigator that receives serialized panorama references from the
Inspector. The navigator guards transitions, fades the screen, swaps the
active panorama, and fades back in.

Info panels are intentionally separate from navigation so their content can
change without changing the tour state machine.