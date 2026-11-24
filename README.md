# Gtag-style map test (offline)

Open this folder in Unity Hub with **Unity 6 (6000.0 LTS)** (add the Android Build Support module if you want a Quest build).

1. First open: Unity downloads the XR packages and creates `Assets/Scenes/Main.unity` by itself.
2. Press Play. The scene builds itself at runtime: map, lighting, gorilla rig, menu. Nothing to drag into the scene.
   - No headset: right-mouse-drag = look, WASD = move, Space = jump, Shift = fly, Q/E = platforms, Tab = menu (aim at a button + left click).
3. Quest build (local): menu **Gtag Test > Configure for Quest (Android)** (also enables the Oculus XR loader), then File > Build Settings > Build.
   Or headless: `Unity -batchmode -quit -projectPath . -executeMethod BuildSetup.BuildAndroid` -> `Builds/GtagMapTest.apk`.

## Unity Build Automation (Cloud Build) -> APK
1. Push this folder's *contents* to the root of a Git repo (or set "Project subdirectory" in the build target). `.gitignore` is included.
2. Create a build target: platform **Android**, Unity version **6000.0.23f1** (or the closest 6000.0 LTS offered).
3. Advanced settings -> **Pre-export method name**: `BuildSetup.PreExport`
   (creates the scene, applies IL2CPP/ARM64/Vulkan/Linear color space, assigns the Oculus loader).
4. Build. Download the APK and sideload it to the Quest (`adb install GtagMapTest.apk`).
   No keystore is needed for sideloading; add one in the target's credentials for a Store/release build.

## In VR
- Left X: open/close the **Main Menu**. It sits on your left hand and turns to face you. Poke a button with your right hand + right trigger.
  - **Player**: name and color. **Settings**: turn speed, monster on/off + speed, mic mute, reconnect, reset board.
  - **Mods** and **Moderator** are locked: press them (or "Owner Code"), type the owner code on the keypad. That also gives you the gold **OWNER** title above your head for everyone else.
  - Mods: Speed, Gravity, Long Arms (1.5x/2x/3x), Fly, Noclip, Platforms, Invincible, Freeze Monster, Flashlight, Darkness, Random Spot, Save/Load Pos, Reset Mods.
  - Moderator: pick a player, then Kick, Bring To Me, Go To Player, Freeze 10s, Send To Stump.
- **Monsters**: Timmy near the campground, the spider in the caves (see Monsters). It chases only when it sees you or you get close, and gives up a few seconds after losing you. If it catches you: "YOU DIED", back to the stump.
- **Leaderboard**: on the map's scoreboard, readable from both sides. Longest time alive, with name + color.
- **Online**: everybody joins ONE public lobby automatically ("gtag-public-lobby", up to 12 players). You see the others, with name tags, and hear them with positional voice chat. The status line at the bottom of the menu shows what is happening (Online: 3 in lobby | Voice: on).

## One-time setup for online + voice (Unity Gaming Services)
1. Open the project in the Editor once: Edit > Project Settings > Services, sign in and **link it to your Unity project**. Commit the changed ProjectSettings so Cloud Build uses the same project.
2. In the Unity Dashboard for that project, make sure **Multiplayer (Relay + Lobby)** and **Vivox** are enabled.
3. Build as usual (BuildSetup.PreExport does the rest). Quest asks for the microphone permission the first time.
If it says "Offline: ..." or "Voice: ..." at the bottom of the menu, that text is the reason.

## Security note
The owner code (123) is checked inside the app, so it keeps casual players out but is NOT real security: anyone who unpacks the APK can find it. Moderator commands are checked by the host with the same code. For real protection use a server check (e.g. Unity Cloud Code) or an allow-list of your player ID.

## Player model: the rigged gorilla (gtag-player-model-rigged.zip)
The player (and every other player online) is the rigged gorilla: 30 bones (torso, neck, head, and shoulder / upper arm / forearm / hand / fingers on both sides), skinned at runtime from `Assets/Resources/Rig/player.bytes` (made by `python Tools/pack_assets.py player gorilla.glb`; textures `player_secondary.png`, `player_face.png`).
- The **body + head** hangs under your headset and turns with it.
- The **arms are not attached to the body**: each whole arm is lifted off the torso and placed so its hand bone sits on your controller and **turns exactly like the controller** (online too: the controller rotations are sent with the pose). Long Arms still works.
- In your own view you see the headless body and just the hands (an arm trailing back through your face would block the view); others and the mirror see everything.
- Your color setting tints the fur (the first material).
- Finger bones are in the skeleton but are not animated yet (no finger curling).
If the hands sit on the controller the wrong way round, change `HandFix` in `MonkeModel.cs` (MonkeSkin).
If `player.bytes` is missing, the old 5-part monke.obj (`Rig/monke_mesh.txt`) is used instead.

## Hit sounds
When a hand touches something it plays a sound (louder for faster hits), picked from what that triangle of the map is made of:
`hit_concrete.mp3` (stone, concrete, metal and anything not listed), `hit_wood.mp3` (bark, tree rings, planks, shingles) and `hit_grass.mp3` (grass/moss ground, leaves, bushes, pine branches), in `Assets/Resources/Audio`.
Which texture counts as wood or grass is the `WOOD_IMAGES` / `GRASS_IMAGES` list at the top of `Tools/pack_assets.py` (then re-run `pack_assets.py map`). Code: `HandSounds.cs`.

## Monsters (two of them)
- **Forest: Timmy** (`Resources/Creatures/timmy.bytes`, from timmy-red.zip). Starts around the campground, wanders the map, never enters the caves.
- **Caves: the spider** (`Resources/Creatures/spider.bytes`, from gth-spider.zip - its gorilla body mesh with your two textures). Lives in the caves and never leaves them.
Both are single meshes (no bones): they bob, sway and lean with their steps. **Detection is smaller:** top of `Monster.cs` - Timmy `ForestSightRange` 20 m (was 45) / `ForestCloseRange` 6 m (was 12), spider 14 m / 5 m. Close range no longer works from another floor.
Timmy/Freeze/Monster on-off in the menu apply to both. Models: `Tools/pack_assets.py timmy|spider`.

## Mirror in the hat room
The mirror stands in the hat room (the white busts), exactly where the map keeps its own mirror (`mirror_fixed`), against the wall and facing into the room, so it is out of the way of the exits. It replaces the map's own mirror, which has no picture in Unity. It is a real mirror (a second camera at your reflected eye, so it follows your head) and it is where you see your whole monke body. It only renders when you are within 12 m and not facing away. Settings > **Mirror** turns it off completely if you need the frames back (Quest).

## Fixed: movement after dying to the monster
Two causes, both fixed:
1. `Player.cs` wiped its swing history on every teleport but not the running average of it, so the leftover "ghost" speed could never be subtracted again and launched you the next time a hand touched something. `InitializeValues()` now clears all of it (also fixes Random Spot, Load Pos, Long Arms and noclip changes).
2. You could still swing, fly and slide during the YOU DIED screen. `Player.locked` now freezes you from the moment you are caught until you are back at the stump and fully reset.

## Player rig (your Blender model)
Unity Cloud has no Blender, so convert once on a PC with Blender:
`blender -b _Originals/Gorilla_IK_Rig.blend -P Tools/export_rig_fbx.py` -> writes `Assets/Resources/Rig/GorillaRig.fbx`.
The game loads that FBX automatically. Until it exists, a built-in gorilla (body + stretchy arms) is used.


## New map (the_entire_gtag_map.glb) and size
The whole map (Sketchfab "The Entire GTAG Map", by Frogggy, CC-BY-4.0 - keep the credit) is packed into `Assets/Resources/Map/map.bytes` (500k triangles, textures shrunk to 512 px in `Map/Textures`) by `Tools/pack_assets.py map the_entire_gtag_map.glb`. No importer or extra package is needed. The whole project zip is about 10 MB.
- You **start in the cosmetics room** next to the in-map computer. The leaderboard is on the forest scoreboard (campground). You never start in the forest.
- Sky sphere and far scenery are visual only (no collision). Leaves/bushes with transparency are drawn unlit.
- Chunks named `cave_*` are the caves, `forest_*` the forest; the monsters use those names to stay in their area.
- The old FBX map was removed.

## Files
- `Assets/Scripts/GorillaLocomotion/` - your Player.cs + Surface.cs (Player.cs has three small additions: `handMoveScale`, `locked` and the state reset)
- `Assets/Scripts/GtagBootstrap.cs` - builds the scene, loads the map, spawns the rig with the original prefab's tuning values
- `Assets/Editor/BuildSetup.cs` - Quest/XR build configuration used by the menu, the batch build and Cloud Build
- `Assets/Scripts/XRRigInput.cs` - headset/controller input (+ desktop fallback)
- `Assets/Scripts/MainMenu.cs` - combined main menu (mods + settings + player)
- `Assets/Scripts/OnlineManager.cs`, `RemoteAvatar.cs` - public lobby, voice, other players, moderator commands
- `Assets/Scripts/Monster.cs`, `Leaderboard.cs`, `PlayerRigModel.cs`, `PlayerProfile.cs` - monster behaviour, board, your body, saved settings
- `Assets/Scripts/MonkeModel.cs` (mesh loader + arm skinning, `MonkeSkin`), `CorruptedMonke.cs` (monster body + hand-walking), `StumpMirror.cs` (mirror)
- `Tools/export_rig_fbx.py` - Blender -> FBX export for the rig
- `Assets/Resources/Map/` - map FBX + textures (30 textures in your zip; the FBX references a few more that weren't included, those materials stay plain)
- `_Originals/` - your original unitypackage (outside Assets, ignored by Unity)

Poison_Menu.dll is intentionally not in the project: it hooks into the real game's code and can't load in a standalone Unity project.
