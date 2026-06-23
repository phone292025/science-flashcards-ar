# Unity Vuforia Science Flashcards

University assignment project for education and training.

The laptop webcam detects a flashcard marker shown on a phone screen. The matching 3D planet and facts panel should appear automatically over the marker in Unity Play Mode.

## Markers

- `sun`
- `earth`
- `mars`

## Expected Result

- Sun marker -> rotating 3D Sun model and Sun facts
- Earth marker -> rotating 3D Earth model and Earth facts
- Mars marker -> rotating 3D Mars model and Mars facts
- Remove marker -> model and facts panel disappear

No click, tap, or close button is required.

## Main Files

- Scene: `Assets/Scenes/SampleScene.unity`
- Runtime setup: `Assets/Scripts/ScienceFlashcards/ScienceFlashcardsRuntimeSetup.cs`
- Tracking visibility: `Assets/Scripts/ScienceFlashcards/VuforiaTargetVisibility.cs`
- Webcam helper: `Assets/Scripts/ScienceFlashcards/VuforiaVideoBackgroundFix.cs`
- Rotation: `Assets/Scripts/RotatePlanet.cs`
- Facts panel billboard: `Assets/Scripts/ScienceFlashcards/WorldSpaceBillboard.cs`
- Planet shader: `Assets/Shaders/ScienceFlashcardsPlanetUnlit.shader`
- Vuforia database: `Assets/StreamingAssets/Vuforia/ScienceFlashcards.xml`
- Vuforia configuration: `Assets/Resources/VuforiaConfiguration.asset`

## Imported Models

- Sun: `Assets/Planets of the Solar System 3D/Prefabs/Sun Sphere.prefab`
- Earth: `Assets/Planet Earth Free/Prefabs/EarthHigh.prefab`
- Mars: `Assets/Planets of the Solar System 3D/Prefabs/Mars.prefab`

Sun and Mars use local cross-pipeline materials to avoid pink shader errors:

- `Assets/Materials/SunARMaterial.mat`
- `Assets/Materials/MarsARMaterial.mat`

The downloaded `Assets/Planets of the Solar System 3D` package was originally
made for URP Shader Graph, while this Vuforia scene uses the Built-in renderer
for stable webcam behavior. Its visible planet surfaces, Saturn ring, asteroid
material, and Sun effect materials are converted automatically to local
compatibility shaders so their Project window previews show useful colors
instead of pink. The unused package skybox remains unchanged.

## Current Status

Completed:

- Vuforia Engine `11.4.4` installed
- Vuforia license is not committed; set `VUFORIA_LICENSE_KEY` or paste your own key in Project Settings > Vuforia Engine
- Device database imported with `sun`, `earth`, and `mars`
- AR Camera created
- Runtime image targets created successfully
- Real 3D planet models added
- Rotating models and facts panels added
- Facts panels now cycle through three complete pages with an automatic swipe transition
- Brief webcam tracking gaps are tolerated for `0.75` seconds to reduce flicker
- Small marker-pose jumps are smoothed while larger intentional phone movements still follow correctly
- Pink Sun and Mars materials fixed
- Planet display diameter increased from `0.35` to `0.48`
- Sun upgraded with an animated solar surface shader
- Temporary AI cache folders cleaned from `UserSettings`; Unity Assistant may
  recreate its small drag-and-drop cache while the editor is open
- Unused astronaut sample assets, tutorial welcome files, and imported demo
  scenes removed
- Unity Build Settings now point to `Assets/Scenes/SampleScene.unity`

Play Mode logs confirm:

```text
Vuforia Started
Created SunTarget from target 'sun' with child SunModel.
Created EarthTarget from target 'earth' with child EarthModel.
Created MarsTarget from target 'mars' with child MarsModel.
```

Still being checked:

- The laptop webcam starts and tracking works, but the native Vuforia video background must be verified in the Unity `Game` tab.
- Use `Game` view for the final demo. The `Scene` tab is only the editor workspace.
- Do not lock the AR Camera transform during Play Mode. Vuforia updates that
  transform as part of marker tracking. The project now tolerates short
  tracking losses and smooths small pose jumps to reduce visible blinking.
  The planet should follow the marker smoothly rather than remain fixed to one
  screen position.

## Test Steps

1. Open `Assets/Scenes/SampleScene.unity`.
2. Wait until Unity finishes compiling.
3. Open the `Game` tab.
4. Press Play.
5. Wait 2-3 seconds for Vuforia and the webcam.
6. Show one complete marker image on the phone.
7. Keep the phone brightness high and hold it about 30-50 cm from the webcam.

The laptop webcam may log `Failed to set camera focus mode`. This usually means the webcam does not support software autofocus control and should not prevent tracking.

## GitHub Setup Notes

The Vuforia package archive is not committed because the local `.tgz` is larger
than GitHub's normal file limit. To open a fresh clone, download Vuforia Engine
for Unity from the Vuforia Developer Portal or Unity Asset Store, then add the
tarball through Unity Package Manager or place it at
`Packages/com.ptc.vuforia.engine-11.4.4.tgz`.

The Vuforia app license is also not committed. Set the `VUFORIA_LICENSE_KEY`
environment variable before opening the project, or paste your own license key
in `Edit > Project Settings > Vuforia Engine`.
