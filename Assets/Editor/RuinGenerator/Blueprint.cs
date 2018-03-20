using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class Blueprint
{
    private static System.Random rng = new System.Random();
    private const int MAX_MUTATE_SEARCH_ITERS = 20;

    private static GameObject GenerateBrickXAxis()
    {
        GameObject o = GameObject.CreatePrimitive(PrimitiveType.Cube);
        o.transform.localScale += new Vector3(o.transform.localScale.x, 0, 0);
        o.AddComponent(typeof(Rigidbody));
        return o;
    }

    public Block[][][] blocks;

    public GameObject Instantiate()
    {
        GameObject brick_x_axis = GenerateBrickXAxis();
        Blueprint structure = this;
        GameObject container = new GameObject();
        List<GameObject> blocks = new List<GameObject>();
        for (int i = 0; i < structure.blocks.Length; i++)
        {
            for (int j = 0; j < structure.blocks[0].Length; j++)
            {
                for (int k = 0; k < structure.blocks[0][0].Length; k++)
                {
                    switch (structure.blocks[i][j][k].type)
                    {
                        case BlockType.BRICK_X_AXIS:
                            blocks.Add(Object.Instantiate(brick_x_axis, new Vector3(i + 0.5f, j + 0.5f, k), new Quaternion()));
                            break;
                        case BlockType.BRICK_Y_AXIS:
                            blocks.Add(Object.Instantiate(brick_x_axis, new Vector3(i, j + 1.0f, k), Quaternion.Euler(0, 0, 90)));
                            break;
                        case BlockType.BRICK_Z_AXIS:
                            blocks.Add(Object.Instantiate(brick_x_axis, new Vector3(i, j + 0.5f, k + 0.5f), Quaternion.Euler(0, 90, 0)));
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        foreach (GameObject g in blocks)
        {
            // g.GetComponent<Rigidbody>().useGravity = false;
            g.transform.parent = container.transform;
        }
        Object.DestroyImmediate(brick_x_axis);
        return container;
    }

    private Block[][][] CreateEmptyBlocks(int x_dim, int y_dim, int z_dim)
    {
        Block[][][] new_blocks = new Block[x_dim][][];
        for (int x = 0; x < x_dim; x++)
        {
            new_blocks[x] = new Block[y_dim][];
            for (int y = 0; y < y_dim; y++)
            {
                new_blocks[x][y] = new Block[z_dim];
                for (int z = 0; z < z_dim; z++)
                {
                    new_blocks[x][y][z] = new Block(BlockType.EMPTY);
                }
            }
        }
        return new_blocks;
    }

    public Blueprint(int x_dim, int y_dim, int z_dim)
    {
        blocks = CreateEmptyBlocks(x_dim, y_dim, z_dim);
    }

    public Blueprint(Blueprint parent)
    {
        blocks = CreateEmptyBlocks(parent.blocks.Length, parent.blocks[0].Length, parent.blocks[0][0].Length);
        for (int x = 0; x < parent.blocks.Length; x++)
        {
            for (int y = 0; y < parent.blocks[0].Length; y++)
            {
                for (int z = 0; z < parent.blocks[0][0].Length; z++)
                {
                    blocks[x][y][z] = new Block(parent.blocks[x][y][z]);
                }
            }
        }
    }

    public void CopyInto(Blueprint dest)
    {
        for (int x = 0; x < this.blocks.Length; x++)
        {
            for (int y = 0; y < this.blocks[0].Length; y++)
            {
                for (int z = 0; z < this.blocks[0][0].Length; z++)
                {
                    dest.blocks[x][y][z] = new Block(this.blocks[x][y][z]);
                }
            }
        }
    }

    public void Clear()
    {
        for (int x = 0; x < this.blocks.Length; x++)
        {
            for (int y = 0; y < this.blocks[0].Length; y++)
            {
                for (int z = 0; z < this.blocks[0][0].Length; z++)
                {
                    this.blocks[x][y][z] = new Block(BlockType.EMPTY);
                }
            }
        }
    }

    public void Randomize()
    {
        this.Clear();
        for (int y = 0; y < this.blocks[0].Length; y++)
        {
            for (int x = 0; x < this.blocks.Length; x++)
            {
                for (int z = 0; z < this.blocks[0][0].Length; z++)
                {
                    BlockType b = this.FindValidBlock(new BlockCoord(x, y, z));
                    this.ApplyBlock(b, new BlockCoord(x, y, z));
                }
            }
        }
    }

    public void ApplyBlock(BlockType b, BlockCoord coord)
    {
        int x = coord.x;
        int y = coord.y;
        int z = coord.z;
        switch (b)
        {
            case BlockType.BRICK_X_AXIS:
                this.blocks[x][y][z].type = BlockType.BRICK_X_AXIS;
                this.blocks[x + 1][y][z].type = BlockType.BRICK_EXT;
                break;
            case BlockType.BRICK_Y_AXIS:
                this.blocks[x][y][z].type = BlockType.BRICK_Y_AXIS;
                this.blocks[x][y + 1][z].type = BlockType.BRICK_EXT;
                break;
            case BlockType.BRICK_Z_AXIS:
                this.blocks[x][y][z].type = BlockType.BRICK_Z_AXIS;
                this.blocks[x][y][z + 1].type = BlockType.BRICK_EXT;
                break;
            case BlockType.EMPTY:
                this.DestroyBlock(new BlockCoord(x, y, z));
                break;
            default:
                break;
        }
    }


    // TODO: CLEAN
    public void DestroyBlock(BlockCoord pos)
    {
        BlockCoord coord = this.FindSourceCoord(pos);
        int x = coord.x;
        int y = coord.y;
        int z = coord.z;
        switch (this.blocks[x][y][z].type)
        {
            case BlockType.BRICK_X_AXIS:
                this.blocks[x][y][z].type = BlockType.EMPTY;
                this.blocks[x + 1][y][z].type = BlockType.EMPTY;
                if (y + 1 < this.blocks[0].Length &&
                    !this.BlockIsSupported(new BlockCoord(x, y + 1, z)))
                {
                    this.DestroyBlock(new BlockCoord(x, y + 1, z));
                }
                if (y + 1 < this.blocks[0].Length &&
                    !this.BlockIsSupported(new BlockCoord(x + 1, y + 1, z)))
                {
                    this.DestroyBlock(new BlockCoord(x + 1, y + 1, z));
                }
                break;
            case BlockType.BRICK_Y_AXIS:
                this.blocks[x][y][z].type = BlockType.EMPTY;
                this.blocks[x][y + 1][z].type = BlockType.EMPTY;
                if (y + 2 < this.blocks[0].Length &&
                    !this.BlockIsSupported(new BlockCoord(x, y + 2, z)))
                {
                    this.DestroyBlock(new BlockCoord(x, y + 2, z));
                }
                break;
            case BlockType.BRICK_Z_AXIS:
                this.blocks[x][y][z].type = BlockType.EMPTY;
                this.blocks[x][y][z + 1].type = BlockType.EMPTY;
                if (y + 1 < this.blocks[0].Length &&
                    !this.BlockIsSupported(new BlockCoord(x, y + 1, z)))
                {
                    this.DestroyBlock(new BlockCoord(x, y + 1, z));
                }
                if (y + 1 < this.blocks[0].Length &&
                    !this.BlockIsSupported(new BlockCoord(x, y + 1, z + 1)))
                {
                    this.DestroyBlock(new BlockCoord(x, y + 1, z + 1));
                }
                break;
            default:
                break;
        }
    }

    // TODO: CLEAN
    public BlockType FindValidBlock(BlockCoord coord)
    {
        int x = coord.x;
        int y = coord.y;
        int z = coord.z;
        List<BlockType> possible = new List<BlockType>();
        possible.Add(BlockType.EMPTY);
        // TODO: move this logic into the block class and iterate through all blocks
        if (this.blocks[x][y][z].type != BlockType.EMPTY)
        {
            return this.blocks[x][y][z].type;
        }
        foreach (BlockType b in System.Enum.GetValues(typeof(BlockType)).Cast<BlockType>())
        {
            if (this.IsValidPlacement(b, coord))
            {
                possible.Add(b);
            }
        }
        return possible[rng.Next(0, possible.Count)];
    }

    public void Mutate()
    {
        double r = rng.NextDouble();

        if (r < 0.2)
        {
            DeleteRandomBlock();
        }
        else
        {
            AddRandomBlock();
        }
    }

    // Different Private Mutation Functions
    private void AddRandomBlock()
    {
        for (int i = 0; i < MAX_MUTATE_SEARCH_ITERS; i++)
        {
            int x = rng.Next(0, this.blocks.Length);
            int y = rng.Next(0, this.blocks[0].Length);
            int z = rng.Next(0, this.blocks[0][0].Length);
            BlockCoord coord = new BlockCoord(x, y, z);
            coord = this.FindSourceCoord(coord);
            x = coord.x;
            y = coord.y;
            z = coord.z;

            if (this.blocks[x][y][z].type == BlockType.EMPTY)
            {
                BlockType possible = this.FindValidBlock(new BlockCoord(x, y, z));
                if (possible != BlockType.EMPTY)
                {
                    this.ApplyBlock(possible, new BlockCoord(x, y, z));
                    break;
                }
            }
        }
    }

    private void DeleteRandomBlock()
    {
        for (int i = 0; i < MAX_MUTATE_SEARCH_ITERS; i++)
        {
            int x = rng.Next(0, this.blocks.Length);
            int y = rng.Next(0, this.blocks[0].Length);
            int z = rng.Next(0, this.blocks[0][0].Length);
            BlockCoord coord = new BlockCoord(x, y, z);
            coord = this.FindSourceCoord(coord);
            x = coord.x;
            y = coord.y;
            z = coord.z;

            if (this.blocks[x][y][z].type != BlockType.EMPTY)
            {
                this.DestroyBlock(coord);
                break;
            }
        }
    }


    public BlockCoord FindSourceCoord(BlockCoord coord)
    {
        BlockCoord new_coord = new BlockCoord(coord);
        if (blocks[coord.x][coord.y][coord.z].type == BlockType.BRICK_EXT)
        {
            if (coord.x > 0 && this.blocks[coord.x - 1][coord.y][coord.z].type == BlockType.BRICK_X_AXIS)
            {
                new_coord.x--;
            }
            else if (coord.y > 0 && this.blocks[coord.x][coord.y - 1][coord.z].type == BlockType.BRICK_Y_AXIS)
            {
                new_coord.y--;
            }
            else if (coord.z > 0)
            {
                new_coord.z--;
            }
        }
        return new_coord;
    }

    public bool IsValidPlacement(BlockType b, BlockCoord coord)
    {
        if (new Block(b).IsValidCoord(this, coord))
        {
            return BlockIsSupported(b, coord);
        }
        return false;
    }

    public bool BlockIsSupported(BlockCoord coord)
    {
        return BlockIsSupported(this.blocks[coord.x][coord.y][coord.z].type, coord);
    }

    public bool BlockIsSupported(BlockType b, BlockCoord coord)
    {
        coord = this.FindSourceCoord(coord);
        int x = coord.x;
        int y = coord.y;
        int z = coord.z;
        // On the ground
        if (y == 0)
        {
            return true;
        }
        // Central block could be supported
        if (this.blocks[x][y - 1][z].type != BlockType.EMPTY)
        {
            return true;
        }
        // Each special shape
        switch (b)
        {
            case BlockType.BRICK_X_AXIS:
                return this.blocks[x + 1][y - 1][z].type != BlockType.EMPTY;
            case BlockType.BRICK_Z_AXIS:
                return this.blocks[x][y - 1][z + 1].type != BlockType.EMPTY;
            default:
                return false;
        }
    }

    // NOTE: Returns false if out of bounds
    private bool SpaceIsFilled(int x, int y, int z)
    {
        bool isValid = true;
        isValid &= x < this.blocks.Length;
        isValid &= y < this.blocks[0].Length;
        isValid &= z < this.blocks[0][0].Length;
        isValid &= x >= 0;
        isValid &= y >= 0;
        isValid &= z >= 0;
        return isValid && this.blocks[x][y][z].type != BlockType.EMPTY;
    }

    public bool BlockIsSupported2(BlockType b, BlockCoord coord)
    {
        coord = this.FindSourceCoord(coord);
        int x = coord.x;
        int y = coord.y;
        int z = coord.z;
        // On the ground
        if (y == 0)
        {
            return true;
        }

        bool main_strong = false;
        bool ext_strong = false;
        bool main_weak = false;
        bool ext_weak = false;
        bool ext_top = false;
        bool main_top = false;
        // Central block could be supported
        if (this.SpaceIsFilled(x, y - 1, z))
        {
            main_strong = true;
            main_top = this.SpaceIsFilled(x, y + 1, z);
        }
        // Each special shape
        switch (b)
        {
            case BlockType.BRICK_X_AXIS:
                ext_strong = this.SpaceIsFilled(x + 1, y - 1, z);
                ext_weak |= this.SpaceIsFilled(x + 2, y, z);
                main_weak |= this.SpaceIsFilled(x - 1, y, z);
                ext_top = this.SpaceIsFilled(x + 1, y + 1, z);
                break;
            case BlockType.BRICK_Z_AXIS:
                ext_strong = this.SpaceIsFilled(x, y - 1, z + 1);
                ext_weak |= this.SpaceIsFilled(x, y, z + 2);
                main_weak |= this.SpaceIsFilled(x, y, z - 1);
                ext_top = this.SpaceIsFilled(x, y + 1, z + 1);
                break;
            case BlockType.BRICK_Y_AXIS:
                main_top = true;
                break;
            default:
                ext_strong = false;
                ext_weak = false;
                main_weak = false;
                break;
        }
        return (main_strong && ext_strong) || 
               (main_strong && ext_weak) || (ext_strong && main_weak) || 
               (main_strong && main_top) || (ext_strong && ext_top);
    }
}