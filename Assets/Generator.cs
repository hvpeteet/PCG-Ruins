using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum Block { EMPTY = 0, BRICK_Z_AXIS, BRICK_X_AXIS, BRICK_Y_AXIS, BRICK_EXT };

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

public class Generator : MonoBehaviour
{
    public GameObject brick_x_axis;
    private Block[][][][] population;
    private System.Random rng = new System.Random();

    // Use this for initialization
    void Start()
    {
        InitPopulation();
        // for round in num_rounds or until #points > goal points
        StartCoroutine("RunGenerator");
    }

    public int pop_size = 3;
    public int num_rounds = 5;
    public int building_dimension_x = 10;
    public int building_dimension_y = 10;
    public int building_dimension_z = 10;


    void InitPopulation()
    {
        population = CreateEmptyPopulation();
        for (int i = 0; i < population.Length; i++)
        {
            GenRandomStructure(population[i]);
        }
    }

    // TODO: make generation check for support (block/ground under the new one)
    void GenRandomStructure(Block[][][] space)
    {
        for (int x = 0; x < space.Length; x++)
        {
            for (int y = 0; y < space[0].Length; y++)
            {
                for (int z = 0; z < space[0][0].Length; z++)
                {
                    space[x][y][z] = Block.EMPTY;
                }
            }
        }
        for (int y = 0; y < space[0].Length; y++)
        {
            for (int x = 0; x < space.Length; x++)
            {
                for (int z = 0; z < space[0][0].Length; z++)
                {
                    Block b = FindValidBlock(space, x, y, z);
                    // TODO: Change how likely this is to happen (50% ??)
                    ApplyBlock(space, b, x, y, z);
                }
            }
        }
    }

    IEnumerator RunGenerator()
    {
        for (int round = 0; round < num_rounds; round++)
        {
            int total_points = 0;
            int[] scores = new int[population.Length];
            for (int i = 0; i < population.Length; i++)
            {
                // --------- Setup -------------
                //List<GameObject> blocks = CreateStructure(population[i]);
                List<GameObject> blocks = new List<GameObject>();
                // Wait to calculate physics based heuristics
                //yield return new WaitForSeconds(15);
                // -------- Analysis -----------
                // Pause simulation for analysis
                var old_scale = Time.timeScale;
                Time.timeScale = 0;
                // Calculate & Record Score
                scores[i] = CalculateScore(population[i], blocks);
                total_points += scores[i];
                // Restore the simulation to its old speed.
                Time.timeScale = old_scale;
                // Destroy Structure
                //DestroyBlocks(blocks);

                //yield return new WaitForSeconds(5);
            }

            // Setup the next generation
            int num_babies = pop_size / 4;
            int num_survivors = pop_size - num_babies;
            double[] cdf = new double[population.Length];
            for (int i = 0; i < population.Length; i++)
            {
                cdf[i] = ((double)scores[i]) / total_points;
            }
            Block[][][][] next_generation = CreateEmptyPopulation();
            // Select survivors
            for (int i = 0; i < num_survivors; i++)
            {
                CopyBlueprint(population[WeightedRandomIndex(cdf)], next_generation[i]);
            }
            // Select breeding bases
            for (int i = num_survivors; i < next_generation.Length; i++)
            {
                CopyBlueprint(population[WeightedRandomIndex(cdf)], next_generation[i]);
                // Generate mutations
                Mutate(next_generation[i]);
            }
            population = next_generation;
            print("finished gen");
        }
        for (int i = 0; i < population.Length; i++)
        {
            // --------- Setup -------------
            List<GameObject> blocks = CreateStructure(population[i]);
            // Wait to calculate physics based heuristics
            yield return new WaitForSeconds(5);
            // Destroy Structure
            DestroyBlocks(blocks);

            yield return new WaitForSeconds(1);
        }
    }

    void CopyBlueprint(Block[][][] source, Block[][][] dest)
    {
        for(int x = 0; x < source.Length; x++)
        {
            for(int y = 0; y < source[0].Length; y++)
            {
                for(int z = 0; z < source[0][0].Length; z++)
                {
                    dest[x][y][z] = source[x][y][z];
                }
            }
        }
    }

    BlockCoord FindSourceCoord(Block[][][] blueprint, BlockCoord coord)
    {
        BlockCoord new_coord = new BlockCoord(coord);
        if (blueprint[coord.x][coord.y][coord.z] == Block.BRICK_EXT)
        {
            if (coord.x > 0 && blueprint[coord.x - 1][coord.y][coord.z] == Block.BRICK_X_AXIS)
            {
                new_coord.x--;
            }
            else if (coord.y > 0 && blueprint[coord.x][coord.y - 1][coord.z] == Block.BRICK_Y_AXIS)
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

    void Mutate(Block[][][] blueprint)
    {
        int x = rng.Next(0, blueprint.Length);
        int y = rng.Next(0, blueprint[0].Length);
        int z = rng.Next(0, blueprint[0][0].Length);
        BlockCoord coord = new BlockCoord(x, y, z);
        coord = FindSourceCoord(blueprint, coord);
        x = coord.x;
        y = coord.y;
        z = coord.z;
        switch (blueprint[x][y][z])
        {
            case Block.EMPTY:
                Block b = FindValidBlock(blueprint, x, y, z);
                ApplyBlock(blueprint, b, x, y, z);
                break;
            default:
                DestroyBlock(blueprint, x, y, z);
                break;
        }
    }

    bool BlockIsSupported(Block[][][] blueprint, int x, int y, int z)
    {
        BlockCoord coord = new BlockCoord(x, y, z);
        coord = FindSourceCoord(blueprint, coord);
        x = coord.x;
        y = coord.y;
        z = coord.z;
        if (blueprint[x][y - 1][z] != Block.EMPTY)
        {
            return true;
        }
        switch (blueprint[x][y][z])
        {
            case Block.BRICK_X_AXIS:
                return blueprint[x + 1][y - 1][z] != Block.EMPTY;
            case Block.BRICK_Z_AXIS:
                return blueprint[x][y - 1][z + 1] != Block.EMPTY;
            default:
                return false;
        }
    }

    void DestroyBlock(Block[][][] blueprint, int x, int y, int z)
    {
        BlockCoord coord = new BlockCoord(x, y, z);
        coord = FindSourceCoord(blueprint, coord);
        x = coord.x;
        y = coord.y;
        z = coord.z;
        switch (blueprint[x][y][z])
        {
            case Block.BRICK_X_AXIS:
                blueprint[x][y][z] = Block.EMPTY;
                blueprint[x + 1][y][z] = Block.EMPTY;
                if (y + 1 < blueprint[0].Length && !BlockIsSupported(blueprint, x, y + 1, z))
                {
                    DestroyBlock(blueprint, x, y + 1, z);
                }
                if (y + 1 < blueprint[0].Length && !BlockIsSupported(blueprint, x + 1, y + 1, z))
                {
                    DestroyBlock(blueprint, x + 1, y + 1, z);
                }
                break;
            case Block.BRICK_Y_AXIS:
                blueprint[x][y][z] = Block.EMPTY;
                blueprint[x][y + 1][z] = Block.EMPTY;
                if (y + 2 < blueprint[0].Length && !BlockIsSupported(blueprint, x, y + 2, z))
                {
                    DestroyBlock(blueprint, x, y + 2, z);
                }
                break;
            case Block.BRICK_Z_AXIS:
                blueprint[x][y][z] = Block.EMPTY;
                blueprint[x][y][z + 1] = Block.EMPTY;
                if (y + 1 < blueprint[0].Length && !BlockIsSupported(blueprint, x, y + 1, z))
                {
                    DestroyBlock(blueprint, x, y + 1, z);
                }
                if (y + 1 < blueprint[0].Length && !BlockIsSupported(blueprint, x, y + 1, z + 1))
                {
                    DestroyBlock(blueprint, x, y + 1, z + 1);
                }
                break;
            default:
                break;
        }
    }

    Block FindValidBlock(Block[][][] blueprint, int x, int y, int z)
    {
        List<Block> possible = new List<Block>();
        possible.Add(Block.EMPTY);
        if (blueprint[x][y][z] != Block.EMPTY)
        {
            return blueprint[x][y][z];
        }
        if (x + 1 < building_dimension_x &&
            blueprint[x + 1][y][z] == Block.EMPTY &&
            (y == 0 ||
            (blueprint[x][y - 1][z] != Block.EMPTY || blueprint[x + 1][y - 1][z] != Block.EMPTY)))
        {
            possible.Add(Block.BRICK_X_AXIS);
        }
        if (y + 1 < building_dimension_y &&
            blueprint[x][y + 1][z] == Block.EMPTY &&
            (y == 0 ||
            (blueprint[x][y - 1][z] != Block.EMPTY)))
        {
            possible.Add(Block.BRICK_Y_AXIS);
        }
        if (z + 1 < building_dimension_z &&
            blueprint[x][y][z + 1] == Block.EMPTY &&
            (y == 0 ||
            (blueprint[x][y - 1][z] != Block.EMPTY || blueprint[x][y - 1][z + 1] != Block.EMPTY)))
        {
            possible.Add(Block.BRICK_Z_AXIS);
        }
        return possible[rng.Next(0, possible.Count)];
    }

    void ApplyBlock(Block[][][] blueprint, Block b, int x, int y, int z)
    {
        switch (b)
        {
            case Block.BRICK_X_AXIS:
                blueprint[x][y][z] = Block.BRICK_X_AXIS;
                blueprint[x + 1][y][z] = Block.BRICK_EXT;
                break;
            case Block.BRICK_Y_AXIS:
                blueprint[x][y][z] = Block.BRICK_Y_AXIS;
                blueprint[x][y + 1][z] = Block.BRICK_EXT;
                break;
            case Block.BRICK_Z_AXIS:
                blueprint[x][y][z] = Block.BRICK_Z_AXIS;
                blueprint[x][y][z + 1] = Block.BRICK_EXT;
                break;
            case Block.EMPTY:
                DestroyBlock(blueprint, x, y, z);
                break;
            default:
                break;
        }
    }

    int WeightedRandomIndex(double[] cdf)
    {
        double val = rng.NextDouble();
        int low = 0;
        int high = cdf.Length - 1;

        for (int i = 0; i < high; i++)
        {
            int mid = (low + high) / 2;
            if (cdf[mid] < val)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
            if (low == high - 1)
            {
                break;
            }
        }
        return high;

    }

    Block[][][][] CreateEmptyPopulation()
    {
        Block[][][][] empty_pop = new Block[pop_size][][][];
        for (int i = 0; i < pop_size; i++)
        {
            empty_pop[i] = new Block[building_dimension_x][][];
            for (int x = 0; x < building_dimension_x; x++)
            {
                empty_pop[i][x] = new Block[building_dimension_y][];
                for (int y = 0; y < building_dimension_y; y++)
                {
                    empty_pop[i][x][y] = new Block[building_dimension_z];
                }
            }
        }
        return empty_pop;
    }

    int CalculateScore(Block[][][] blueprint, List<GameObject> structure)
    {
        int total = 0;
        total += CalcStaticSturdyScore(blueprint);
        // TODO: Calculate more scores/heuristics here
        return total;
    }

    int CalcStaticSturdyScore(Block[][][] blueprint)
    {
        int score = 0;
        for (int x = 0; x < blueprint.Length; x++)
        {
            for (int z = 0; z < blueprint[0][0].Length; z++)
            {
                int volume_under_a_roof = 0;
                bool found_roof = false;
                for (int y = blueprint[0].Length - 1; y >= 0; y--)
                {
                    if (!found_roof && blueprint[x][y][z] != Block.EMPTY)
                    {
                        found_roof = true;
                    }
                    else if (found_roof && blueprint[x][y][z] == Block.EMPTY)
                    {
                        volume_under_a_roof++;
                    }
                }
                score += volume_under_a_roof;
            }
        }
        return score;
    }

    List<GameObject> CreateStructure(Block[][][] structure)
    {
        List<GameObject> blocks = new List<GameObject>();
        for (int i = 0; i < structure.Length; i++)
        {
            for (int j = 0; j < structure[0].Length; j++)
            {
                for (int k = 0; k < structure[0][0].Length; k++)
                {
                    switch (structure[i][j][k])
                    {
                        case Block.BRICK_X_AXIS:
                            blocks.Add(Instantiate(brick_x_axis, new Vector3(i + 0.5f, j + 0.5f, k), new Quaternion()));
                            break;
                        case Block.BRICK_Y_AXIS:
                            blocks.Add(Instantiate(brick_x_axis, new Vector3(i, j + 1.0f, k), Quaternion.Euler(0, 0, 90)));
                            break;
                        case Block.BRICK_Z_AXIS:
                            blocks.Add(Instantiate(brick_x_axis, new Vector3(i, j + 0.5f, k + 0.5f), Quaternion.Euler(0, 90, 0)));
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        return blocks;
    }

    void DestroyBlocks(List<GameObject> blocks)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            Destroy(blocks[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
