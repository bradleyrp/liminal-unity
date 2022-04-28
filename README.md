# Liminal-Unity

This small program can be used to view and generate landmarks or boundary conditions on complex Wavefront (`obj`) meshes using a freely rotating camera system. 

The program loads preprocessed meshes in realtime and provides an interface for the user to select (landmark or annotate) a series of vertices, and review the shortest path between them, and then write the results to a named (`JSON`) text file which includes the user-selected vertices along with the shortest paths between them. This tool facilitates fast and flexible landmarking on a complex mesh with a video-game style camera and finds applications in 3D modeling projects which require careful inspection of the mesh. 

The software depends on both pre- and post-processing and was designed to serve as a user landmarking tool supported by [ATgizmo](https://gitlab.com/natalia-trayanova/atgizmo). Please note: the author is not a video game developer! This is a very, very crude tool that accomplishes the critical goal of allowing users to flexibly landmark a mesh with a freely translating and rotating camera.

## Requirements

Input Wavefront (`obj`) files should be preprocessed so they are centered at the origin with a maximum extent of 100 units. This ensures that the camera location motion scales correctly. To preprocess an `obj` file for landmarking, use the following command from [ATgizmo](https://gitlab.com/natalia-trayanova/atgizmo):

```
atgizmo vtk-to-obj -f path/to/P13_reod_marked.obj -o path/to/P13_reod_marked.alt.obj --center --scale 100.0
```

This will produce a properly-formatted `obj` file which you can load into this landmarking software.

## Usage

To use this program, start it up and type a path to an `obj` file in the text box in the upper left. Click "LOAD" and wait for it to load. For reference, it can render a mesh with 1M points and 12M cells seamlessly on a 2017 Macbook Pro with 16GB memory and a Radeon Pro 560 4 GB or with somewhat slow responses on a 2014 macbook pro with integrated graphics. We have also tested on Linux, and find good performance on an NVidia 2060 as long as you are using the proprietary drivers.

Use the following camera controls to move the camera:

- `wasd` moves the camera left, right, forwards, and backwards
- `Space` and `LeftShift` move the camera up and down
- clicking and dragging with the mouse rotates the camera
- the arrow keys also rotate the camera
- holding `RightShift` speeds up the camera
- holding or pressing the `l` (look) key will center the mesh

Note that the "look" (`l`) key is extremely useful for rotating the mesh in the center of the screen.

To mark landmarks, use the right click button. Each time you click a landmark, you will see a magenta sphere. Each time you select a new sphere, the program will highlight the shortest path (via Dijkstra's algorithm). If you want to visualize a loop, hit the `return` key to connect to the start point. You can use the "RESET" button to clear your selections.

When you are ready to save a landmark, enter a name in the the second textbox in the upper left. When you click "SAVE" it will save a JSON file with the extension `.tagged.<name>.json` as the suffix for the `obj` file you loaded. The JSON file includes `clicked` indices which tell you which vertices were selected by the user and `route` which includes the entire route. Postprocessing can always recapitulate the shortest paths if you only want to use the user-selected `clicked` vertices.

## History

The author used the following development procedure to build this program. Note that we make use of the OBJImport module along with a `.NET` Dijkstra calculator from the nuget link below.

~~~
1. prelim
	- make a "new 3D core" project in Unity 2021.3.0f1 LTS
	- go to project settings and make sure "asset serialization" is text and meta files are text
	- start tracking in git with gitignore from: https://github.com/github/gitignore/blob/main/Unity.gitignore
	- rename the scene "liminal-viewer"
	- rename the Main Camera "camera"
	- remove the directional light
	- use the menu to go to window > render > lighting and change skybox material to none to eliminate the horizon
2. get the pathfinder
	- get dijkstra. visit https://www.nuget.org/packages/Dijkstra.NET/ and download the dijkstra.net.1.2.1.nupkg 
		change the extension to zip and find the dll
		copy the dll to the assets folder for example with: cp -av ~/worker/heart/liminal-unity-eaten/Assets/Dijkstra.NET.dll ~/worker/heart/liminal-unity/Assets/
3. get the camera fly script
	- copy in the camera_fly.cs file from the previous version of unity with cp -av ~/worker/heart/liminal-unity-eaten/Assets/camera_fly.cs ~/worker/heart/liminal-unity/Assets
	- add camera_fly.cs as a script to the camera object using "Add Component" in the inspector
	- remove the audio listener from the camera
4. add the obj reader
	- next add the obj reader with window > package manager. select "my assets" on the packages menu in the upper left and then select "Realtime OBJ Importer" and click the "import" button. skip the samples because we want to avoid any accidents with the ".unity" files (one time this overwrote our scene!) and we will bring in our own usage
	- add an importer object. click the three dots by the liminal-viewer scene in the hierarchy. use the menu to select "GameObject" then "create empty" and rename it "importer"
	- copy our modified scripts for the OBJ importer into the assets folder:
		cp -av ~/worker/heart/liminal-unity-eaten/Assets/ObjFromStream.cs ~/worker/heart/liminal-unity-eaten/Assets/ObjFromFile.cs ~/worker/heart/liminal-unity/Assets/
	- click the importer in the hierarchy, then in the inspector, click "Add Component". search for "ObjFromStream.cs" and add this as a component. also add the "ObjFromFile.cs" since we can switch between the two during testing
5. get modifications to the OBJLoader
	- we need to copy this to make a collider
		cp -av ~/worker/heart/liminal-unity-eaten/Assets/OBJImport/OBJLoader.cs ~/worker/heart/liminal-unity/Assets/OBJImport/OBJLoader.cs
6. add tags for the spheres and prebuild shaders
	- use the menu edit > project settings. search "tags" and add tags for "mark_sphere" and "route_sphere"
	- go to "always included shaders" and add "Standard (Specular setup)"
7. add a directional light
	- add a light to the camera called "light" (right click the camera, then find it on the light menu)
	- set the X to 1000
8. note that collision was abandoned. sometimes it's nice to clip and see inside
9. test. run the program with the stream loader pointed to an obj file
	- in the importer you can select or deselect the ObjFromStream.cs and ObjFromFile.cs objects
	- you have to wait a moment for it to load
	- switch back to the file loader and we will try building it
10. test build.
	- go to file > build settings. enable development build
	- then go to "player" and expand "resolution and presentation" and set the resolution to 1024x768 for the 2017 macbook
	- set fullscreen mode to "windowed" and allow the window resizing
	- build and run the program. test with an OBJ that has been mean-centered with a maximum XYZ span of 1000 units
11. packaging notes
	- use the unity hub gear icon to add extra build support, for example Mono for linux if you are building on mac
	- use file > build settings to select either linux or macos build and build to the `builds/macos/` or `builds/nix` folders. rename the folder to "liminal" so it uncompresses nicely, then zip it and change the names to something meaningful for github. then rename the folder back to `nix` or `macos`. upload to github manually
~~~

These notes are retained for posterity, however the author expects that the projected can be rebuild and developed further from the available source code.