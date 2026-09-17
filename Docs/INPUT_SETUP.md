# Input Setup

World-space canvases require the tracked-device UI input module and a tracked
device raycaster. The controller action map must provide a UI click action for
the trigger to invoke Unity button callbacks.

When replacing the XR rig, verify these three pieces together; changing only
the controller prefab can leave an apparently functional but unclickable UI.