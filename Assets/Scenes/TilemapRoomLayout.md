# Binding of Isaac Style Room Layout
## Tilemap Specifications (32x32 pixels per tile)

### Identified Tiles from Sprite Sheet

#### Floor Tiles (Row 1 - Bottom Section)
- Tile 0: Plain floor with skulls
- Tile 1: Floor variant with skull pattern
- Tile 2: Floor variant (darker)
- Tile 3: Floor variant

#### Wall Tiles (Rows 2-3 - Bottom Section)
**Top Wall / Corners:**
- Tile 4: Top-left corner
- Tile 5: Top wall (horizontal)
- Tile 6: Top-right corner
- Tile 7: Top wall variant

**Side Walls:**
- Tile 8: Left wall (vertical)
- Tile 9: Wall intersection/center
- Tile 10: Right wall (vertical)
- Tile 11: Door frame top

**Bottom Wall / Corners:**
- Tile 12: Bottom-left corner
- Tile 13: Bottom wall (horizontal)
- Tile 14: Bottom-right corner
- Tile 15: Door frame side

### Standard Room Layout (13x9 tiles)

```
Room Dimensions: 13 tiles wide × 9 tiles tall = 416x288 pixels

Legend:
  C = Corner
  W = Wall
  D = Door opening
  F = Floor
  . = Empty/void

Layout Map:
```

```
C W W W W D W W W W W W C
W F F F F F F F F F F F W
W F F F F F F F F F F F W
D F F F F F F F F F F F D
W F F F F F F F F F F F W
W F F F F F F F F F F F W
W F F F F F F F F F F F W
W F F F F F F F F F F F W
C W W W W D W W W W W W C
```

### Tile Index Map (Using sprite sheet indices)

Assuming tiles are numbered left-to-right, top-to-bottom starting from the tilemap section:

```
 4  5  5  5  5  -  5  5  5  5  5  5  6
 8  0  0  0  0  0  0  0  0  0  0  0 10
 8  1  1  1  1  1  1  1  1  1  1  1 10
 -  0  0  0  0  0  0  0  0  0  0  0  -
 8  1  1  1  1  1  1  1  1  1  1  1 10
 8  0  0  0  0  0  0  0  0  0  0  0 10
 8  1  1  1  1  1  1  1  1  1  1  1 10
 8  0  0  0  0  0  0  0  0  0  0  0 10
12 13 13 13 13  - 13 13 13 13 13 13 14
```

### Door Positions
- **Top Door:** Column 6 (center), Row 0
- **Left Door:** Column 0, Row 3 (middle)
- **Right Door:** Column 12, Row 3 (middle)
- **Bottom Door:** Column 6 (center), Row 8

### Implementation Notes

1. **Corner Tiles:**
   - Top-Left: Tile 4
   - Top-Right: Tile 6
   - Bottom-Left: Tile 12
   - Bottom-Right: Tile 14

2. **Wall Tiles:**
   - Top/Bottom Horizontal: Tile 5, 13
   - Left/Right Vertical: Tile 8, 10

3. **Floor Pattern:**
   - Alternate between tile 0 and 1 for visual variety
   - Use checkered or row-based pattern

4. **Doors:**
   - Leave gaps in walls (marked as `-`)
   - Can add door frame tiles (11, 15) around openings

### Unity Tilemap Setup

1. Create a Tilemap Grid with Cell Size: 32x32
2. Import sprite sheet with Pixels Per Unit: 32
3. Slice sprites using Grid By Cell Size: 32x32
4. Create Tile Palette from sliced sprites
5. Paint room using the layout above

### Variations

**Smaller Room (9x7):**
```
C W W D W W W W C
W F F F F F F F W
W F F F F F F F W
D F F F F F F F D
W F F F F F F F W
W F F F F F F F W
C W W D W W W W C
```

**Larger Boss Room (17x13):**
```
C W W W W W W W D W W W W W W W C
W F F F F F F F F F F F F F F F W
W F F F F F F F F F F F F F F F W
W F F F F F F F F F F F F F F F W
W F F F F F F F F F F F F F F F W
W F F F F F F F F F F F F F F F W
D F F F F F F F F F F F F F F F D
W F F F F F F F F F F F F F F F W
W F F F F F F F F F F F F F F F W
W F F F F F F F F F F F F F F F W
W F F F F F F F F F F F F F F F W
W F F F F F F F F F F F F F F W
C W W W W W W W D W W W W W W W C
```
