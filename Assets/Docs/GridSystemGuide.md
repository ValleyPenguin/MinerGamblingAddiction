# Grid System Guide

This project uses a simple pixel grid rule:

```text
32 pixels = 1 Unity world unit = 1 grid cell
```

That means a `32 x 32` sprite imported with `Pixels Per Unit` set to `32` will naturally fill one grid cell when its Transform scale is `1, 1, 1`.

## Current Scene Setup

The `Allan` scene has a `Grid System` object with a `GridSystem` component.

Default values:

- Canvas Width: `640`
- Canvas Height: `360`
- Pixels Per Unit: `32`
- Columns: `20`
- Rows: `11`
- Cell Pixel Size: `32`

The playable grid is `20 x 11` cells:

```text
20 cells * 32 pixels = 640 pixels wide
11 cells * 32 pixels = 352 pixels tall
```

The game canvas is `640 x 360`, so there are 4 pixels of extra camera space above and below the grid.

## Importing Sprites

For any object that should fit one grid cell, such as the player or an ore:

1. Make the image `32 x 32` pixels.
2. Select the sprite in Unity.
3. Set `Pixels Per Unit` to `32`.
4. Keep the object Transform scale at `1, 1, 1`.

If an ore prefab is assigned to the grid system, `Fit Ore Prefab To Cell` can automatically scale the prefab to one cell. For player art, it is cleaner to import the sprite correctly at `32 PPU` and keep the player scale at `1`.

## How The Grid Is Made In Code

The grid is made by `GridSystem.cs`. It does not use Unity's Tilemap yet. Instead, it creates simple preview GameObjects:

- one parent object called `Generated Grid Preview`
- one `LineRenderer` for each vertical grid line
- one `LineRenderer` for each horizontal grid line
- one ore GameObject for each spawned ore

The important flow is:

```csharp
Regenerate()
ClearPreview()
FrameCamera()
CreatePreviewRoot()
DrawGrid()
SpawnOres()
```

### Regenerate

`Regenerate()` is the main entry point. It runs when the component wakes up, when it is enabled, when inspector values change, and when you use the `Regenerate Grid` context menu.

It does three main things:

1. Deletes the old generated preview.
2. Frames the camera.
3. Rebuilds the grid lines and ore objects.

This is why changing values like `Columns`, `Rows`, or `Ore Spawn Chance` in the inspector updates the scene preview.

### Cell Size

The code uses this property:

```csharp
public float CellWorldSize => (float)cellPixelSize / pixelsPerUnit;
```

With our default setup:

```text
cellPixelSize = 32
pixelsPerUnit = 32
CellWorldSize = 1
```

So one grid cell is one Unity world unit.

If we changed the cell size to `64` pixels while keeping `pixelsPerUnit` at `32`, each grid cell would become `2` world units.

### Grid Origin

`GridOrigin()` decides where the bottom-left corner of the grid is.

If `centerGridOnThisObject` is enabled, the grid is centered around the `Grid System` object's position. For a `20 x 11` grid, the grid extends equally left/right and up/down from that object.

If it is disabled, the `Grid System` object's position becomes the bottom-left corner instead.

Most other methods depend on `GridOrigin()`.

### Cell Centers

Objects are centered in grid boxes with `GridToWorld()`.

The grid lines mark the cell edges. A cell coordinate like `(0, 0)` means "the first cell", not "the first grid line". To place an object in the middle of that cell, the code starts at the bottom-left corner of the grid and then moves by half a cell.

This is the important part:

```csharp
origin.x + (cell.x + 0.5f) * cellSize
origin.y + (cell.y + 0.5f) * cellSize
```

The `+ 0.5f` is what centers the object inside the grid box.

For example, with the default setup:

```text
CellWorldSize = 1
cell = (0, 0)
```

The center of that cell is:

```text
x = origin.x + 0.5
y = origin.y + 0.5
```

Cell `(1, 0)` is one cell to the right:

```text
x = origin.x + 1.5
y = origin.y + 0.5
```

This is why spawned ores land inside cells instead of sitting on grid intersections.

### Drawing The Lines

`DrawGrid()` calculates:

```csharp
float cellSize = CellWorldSize;
float width = columns * cellSize;
float height = rows * cellSize;
```

Then it loops through every column edge:

```csharp
for (int x = 0; x <= columns; x++)
```

For each `x`, it creates a vertical line from the bottom of the grid to the top.

Then it loops through every row edge:

```csharp
for (int y = 0; y <= rows; y++)
```

For each `y`, it creates a horizontal line from the left side of the grid to the right side.

The reason the loops use `<=` is that a grid with `20` columns needs `21` vertical lines: one line at the left edge, one at the right edge, and the lines between cells.

### AddLine

`AddLine()` creates a new GameObject and adds a `LineRenderer`.

The `LineRenderer` receives two positions:

```csharp
line.SetPosition(0, start);
line.SetPosition(1, end);
```

It also sets the visual width from pixels:

```csharp
line.startWidth = gridLinePixels / pixelsPerUnit;
line.endWidth = gridLinePixels / pixelsPerUnit;
```

So a `1` pixel grid line becomes `1 / 32` world units wide.

### Ores

`SpawnOres()` creates a shuffled list of every grid cell. It then walks through that list and randomly decides whether each cell gets an ore.

The shuffle matters because it avoids always checking cells in the same bottom-left to top-right order.

The ore settings control the result:

- `Ore Spawn Chance`: chance for each checked cell to spawn ore
- `Maximum Ores`: hard cap on how many ores can appear
- `Random Seed`: makes the random layout repeatable

If `Ore Prefab` is assigned, the system instantiates that prefab. If no prefab is assigned, it creates a simple colored square with a `SpriteRenderer`.

After an ore is created, `AddOre()` places it with:

```csharp
ore.transform.position = GridToWorld(cell);
```

So ore placement uses the same cell-center math described above. The ore does not decide its own world position. It only receives a grid coordinate, and `GridSystem` converts that coordinate to the exact center of the matching grid box.

### Fitting Ores To A Cell

`FitToCell()` looks for a `SpriteRenderer` on the ore object. If it finds one, it checks the sprite's world size and scales the GameObject so the sprite's largest side fits exactly inside one cell.

This is useful while prototyping, but for final pixel art the cleaner setup is:

```text
Sprite image size: 32 x 32
Pixels Per Unit: 32
Transform scale: 1, 1, 1
```

With those settings, the prefab already fits a cell naturally.

### Editor Preview Objects

The generated grid and ore objects are marked with:

```csharp
HideFlags.DontSaveInEditor
```

That means they can be visible in the editor, but they are not meant to be saved as permanent scene objects. The scene should save the `Grid System` settings, not hundreds of generated line and ore objects.

## Important GridSystem Properties

```csharp
grid.Columns
grid.Rows
grid.CellPixelSize
grid.CellWorldSize
```

With the default settings, `CellWorldSize` is `1`.

Use `CellWorldSize` instead of hardcoding `1` if you want your code to keep working when the grid settings change.

## Useful GridSystem Methods

### GridToWorld

```csharp
Vector3 worldPosition = grid.GridToWorld(new Vector2Int(3, 5));
```

This converts a grid cell coordinate into the world position at the center of that cell.

### TryWorldToGrid

```csharp
if (grid.TryWorldToGrid(transform.position, out Vector2Int cell))
{
    Debug.Log(cell);
}
```

This converts a world position back into a grid cell. It returns `false` if the position is outside the grid.

## Grid Coordinates

Grid coordinates use `Vector2Int`.

```text
(0, 0) is the bottom-left cell.
(columns - 1, rows - 1) is the top-right cell.
```

For the default `20 x 11` grid:

```text
Bottom-left: (0, 0)
Top-right:   (19, 10)
```

## Recommended Next Step: Store Cell Contents

Right now, `GridSystem` draws the grid and spawns visual ore previews. For real gameplay, add a separate data layer instead of using scene objects as the source of truth.

A simple starting point:

```csharp
public enum CellContent
{
    Empty,
    Dirt,
    Ore
}

private CellContent[,] cells;
```

Then initialize it using the grid size:

```csharp
cells = new CellContent[grid.Columns, grid.Rows];
```

When the player moves or mines, check `cells[x, y]`. The visual GameObjects should display what the data says, not the other way around.

## Good Pattern For This Game

Use this split:

- `GridSystem`: grid size, conversion between grid cells and world positions, editor preview.
- Player movement script: input, player cell position, movement rules.
- `MineableGrid` or `GridContents`: what each cell contains.
- `OreView` or similar visual code: shows sprites/prefabs for the data.

This keeps the project easy to grow. The player should not need to know how ore visuals are drawn, and the ore system should not need to know which keys move the player.
