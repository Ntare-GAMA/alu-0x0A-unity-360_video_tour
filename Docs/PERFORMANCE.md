# Performance Notes

Keep only the current panorama active during a tour transition. This limits
video decoding, mesh rendering, and UI work on the Quest while preserving the
simple scene wiring used by the project.

Prefer testing texture and video changes on the target headset. Editor frame
rates are not a reliable proxy for Android XR performance.