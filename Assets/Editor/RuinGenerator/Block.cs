using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The type of a block
public enum BlockType { EMPTY = 0, BRICK_Z_AXIS, BRICK_X_AXIS, BRICK_Y_AXIS, BRICK_EXT };

// A coordinate in a blueprint
public class BlockCoord
{
    public int x, y, z;

    public BlockCoord(int x_coord, int y_coord, int z_coord)
    {
        x = x_coord;
        y = y_coord;
        z = z_coord;
    }

    public BlockCoord(BlockCoord parent)
    {
        x = parent.x;
        y = parent.y;
        z = parent.z;
    }
}

public class Block
{
    // A block in a blueprint
    public BlockType type;

    public Block(Block parent)
    {
        type = parent.type;
    }

    public Block(BlockType block_type)
    {
        type = block_type;
    }

    // Checks if a block is inside bounds and is not intersecting any other blocks.
    public bool IsValidCoord(Blueprint blueprint, BlockCoord coord)
    {
        int x = coord.x;
        int y = coord.y;
        int z = coord.z;
        switch (this.type)
        {
            case BlockType.EMPTY:
                return true;

            // ------ Basic Bricks -------
            case BlockType.BRICK_X_AXIS:
                return
                    // Bounds check
                    x + 1 < blueprint.blocks.Length &&
                    // Empty space to the left
                    blueprint.blocks[x + 1][y][z].type == BlockType.EMPTY;

            case BlockType.BRICK_Z_AXIS:
                return
                    // Bounds check
                    z + 1 < blueprint.blocks[0][0].Length &&
                    // Empty space to the left
                    blueprint.blocks[x][y][z + 1].type == BlockType.EMPTY;

            case BlockType.BRICK_Y_AXIS:
                return
                    // Bounds check
                    y + 1 < blueprint.blocks[0].Length &&
                    // Empty space up
                    blueprint.blocks[x][y + 1][z].type == BlockType.EMPTY;

            default:
                return false;
        }
    }
}
