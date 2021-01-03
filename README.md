# Spliny 

## A simple 3D spline solution for Unity 

#### by [Rodrigo Pinheiro][github]

### About

Spliny is a simple, decoupled, and ready to use solution for 3D splines in the Unity Engine,
the splines work around the `PathCreator` behaviour which generates and loads the path in editor.

Spliny offers a way to serialize paths into Unity's `ScriptableObjects` which can be used
for a data-driven system that needs to load different fixed paths at runtime. This was
exactly why spliny was made!

### How to use
* Add a `PathCreator.cs` behaviour script to the object that will manage the spline

![splineMonobehaviour][behaviour]

* You can now shape, save and load paths to your liking

* You can create new behaviours using the created paths like the one in the `Examples` folder `MoveAlongPath.cs`. Here the lerp amount goes from `[0, 1]` and moves the object by that
value percentage along the curve. Both the curve and the lerp amount can and should be set programmatically.

![movePathBehaviour][moveBehaviour]

* Enjoy!

### About Updates

I don't plan on updating this asset regurarly, if I have the need for extra behaviour,
feel like adding some personalization options or I find a major bug while using it I can
update it. Other than that I still want to add at least a custom window to change the
curve visualization parameters like colors and gizmo sizes.

### Using it in Projects

Feel free to use this asset, the code is simple and someone might learn something from
peaking. If you do release something using this asset I appreciate referring me in the
credits. Thanks!

[behaviour]: Images/behaviour.png
[moveBehaviour]: Images/move.png
[github]: https://github.com/RodrigoPrinheiro