using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Generator : MonoBehaviour
{
    // ------ Public variables / parameters ------
    public GameObject brick_x_axis;
    public int pop_size = 3;
    public int num_rounds = 5;
    public int building_dimension_x = 10;
    public int building_dimension_y = 10;
    public int building_dimension_z = 10;
    public const int MAX_MUTATE_SEARCH_ITERS = 20;

    // ----------- Private variables -------------
    private Blueprint[] population;
    private static System.Random rng = new System.Random();

    // -------- Internal classes & enums ---------

    // The type of a block
    enum BlockType { EMPTY = 0, BRICK_Z_AXIS, BRICK_X_AXIS, BRICK_Y_AXIS, BRICK_EXT };

    // A block in a blueprint
    class Block
    {
        public BlockType type;

        public Block(Block parent)
        {
            type = parent.type;
        }

        public Block(BlockType block_type)
        {
            type = block_type;
        }

        public bool IsValidPlacement(Blueprint blueprint, BlockCoord coord)
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
                        blueprint.blocks[x + 1][y][z].type == BlockType.EMPTY &&
                        // On the floor or has support
                        (y == 0 ||
                            (blueprint.blocks[x][y - 1][z].type != BlockType.EMPTY ||
                            blueprint.blocks[x + 1][y - 1][z].type != BlockType.EMPTY));

                case BlockType.BRICK_Z_AXIS:
                    return
                        // Bounds check
                        z + 1 < blueprint.blocks[0][0].Length &&
                        // Empty space to the left
                        blueprint.blocks[x][y][z + 1].type == BlockType.EMPTY &&
                        // On the floor or has support
                        (y == 0 ||
                            (blueprint.blocks[x][y - 1][z].type != BlockType.EMPTY ||
                            blueprint.blocks[x][y - 1][z + 1].type != BlockType.EMPTY));

                case BlockType.BRICK_Y_AXIS:
                    return
                        // Bounds check
                        y + 1 < blueprint.blocks[0].Length &&
                        // Empty space up
                        blueprint.blocks[x][y + 1][z].type == BlockType.EMPTY &&
                        // On the floor or has support
                        (y == 0 ||
                            (blueprint.blocks[x][y - 1][z].type != BlockType.EMPTY));

                default:
                    return false;
            }
        }
    }

    // A coordinate in a blueprint
    class BlockCoord
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

    // All the information needed to build a building.
    class Blueprint
    {
        public Block[][][] blocks;

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
                if ((new Block(b)).IsValidPlacement(this, coord))
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

        public bool BlockIsSupported(BlockCoord coord)
        {
            coord = this.FindSourceCoord(coord);
            int x = coord.x;
            int y = coord.y;
            int z = coord.z;
            if (this.blocks[x][y - 1][z].type != BlockType.EMPTY)
            {
                return true;
            }
            switch (this.blocks[x][y][z].type)
            {
                case BlockType.BRICK_X_AXIS:
                    return this.blocks[x + 1][y - 1][z].type != BlockType.EMPTY;
                case BlockType.BRICK_Z_AXIS:
                    return this.blocks[x][y - 1][z + 1].type != BlockType.EMPTY;
                default:
                    return false;
            }
        }

    }

    // ---------- Unity Functions ----------
    // Use this for initialization
    void Start()
    {
        InitPopulation();
        // for round in num_rounds or until #points > goal points
        StartCoroutine("RunGenerator");
    }

    // Update is called once per frame
    void Update()
    {

    }

    // ------ Core Functionality ------

    Blueprint[] CreateEmptyPopulation()
    {
        Blueprint[] pop = new Blueprint[pop_size];
        for (int i = 0; i < pop.Length; i++)
        {
            pop[i] = new Blueprint(building_dimension_x, building_dimension_y, building_dimension_z);
        }
        return pop;
    }

    void InitPopulation()
    {
        population = CreateEmptyPopulation();
        for (int i = 0; i < population.Length; i++)
        {
            population[i].Mutate();
        }
    }

    int CompareBlueprints(Blueprint a, Blueprint b)
    {
        return CalculateScore(a).CompareTo(CalculateScore(b));
    }

    // TODO: CLEAN
    IEnumerator RunGenerator()
    {
        int[] final_scores = new int[population.Length];
        for (int round = 0; round < num_rounds; round++)
        {
            int total_points = 0;
            int[] scores = new int[population.Length];

            System.Array.Sort(population, (a, b) => CompareBlueprints(b, a));

            // Record scores for the current generation
            for (int i = 0; i < population.Length; i++)
            {
                scores[i] = CalculateScore(population[i]);
                total_points += scores[i];
            }

            print(string.Format("First 2 scores: {0}, {1}",scores[0], scores[1]));

            // TODO: Factor out this normalization logic
            double[] cdf = new double[population.Length];
            if (total_points == 0)
            {
                for (int i = 0; i < population.Length; i++)
                {
                    cdf[i] = 1.0 / population.Length * (i + 1);
                }
            }
            else
            {
                cdf[0] = ((double)scores[0]) / total_points;
                for (int i = 1; i < population.Length; i++)
                {
                    cdf[i] = ((double)scores[i]) / total_points + cdf[i - 1];
                }
            }
            print(string.Format("cdf is {0} {1} {2} {3}", cdf.Select(x => x.ToString()).ToArray()));


            // Setup the next generation
            // TODO: Expose these through public vars
            int num_elite = 2;
            int num_babies = pop_size;
            int num_survivors = pop_size - num_babies;
            Blueprint[] next_generation = CreateEmptyPopulation();

            // Select elite
            for (int i = 0; i < num_elite; i++)
            {
                population[i].CopyInto(next_generation[i]);
            }

            // Select survivors
            for (int i = num_elite; i < num_survivors + num_elite; i++)
            {
                population[WeightedRandomIndex(cdf)].CopyInto(next_generation[i]);
            }

            // Select breeding bases
            for (int i = num_survivors + num_elite; i < next_generation.Length; i++)
            {
                population[WeightedRandomIndex(cdf)].CopyInto(next_generation[i]);
                // Generate mutations
                next_generation[i].Mutate();
            }
            population = next_generation;
            print(string.Format("finished gen, best score {0}", scores.Max()));
            final_scores = scores;
        }

        // Display the final results
        for (int i = 0; i < population.Length; i++)
        {
            if (final_scores[i] == final_scores.Max())
            {
                var old_scale = Time.timeScale;
                Time.timeScale = 1;
                List<GameObject> blocks = CreateStructure(population[i]);
                // Wait to calculate physics based heuristics
                yield return new WaitForSeconds(5);
                // Destroy Structure
                DestroyBlocks(blocks);
                Time.timeScale = old_scale;
                yield return new WaitForSeconds(1); ;
            }           
        }
    }


    // ----------- Scoring --------------
    int CalculateScore(Blueprint blueprint)
    {
        int total = 0;
        total += CalcStaticSturdyScore(blueprint);
        // TODO: Calculate more scores/heuristics here
        return total;
    }

    int CalcStaticSturdyScore(Blueprint blueprint)
    {
        int score = 0;
        for (int x = 0; x < blueprint.blocks.Length; x++)
        {
            for (int z = 0; z < blueprint.blocks[0][0].Length; z++)
            {
                int volume_under_a_roof = 0;
                bool found_roof = false;
                for (int y = blueprint.blocks[0].Length - 1; y >= 0; y--)
                {
                    if (!found_roof && blueprint.blocks[x][y][z].type != BlockType.EMPTY)
                    {
                        found_roof = true;
                    }
                    else if (found_roof && blueprint.blocks[x][y][z].type == BlockType.EMPTY)
                    {
                        volume_under_a_roof++;
                    }
                }
                score += volume_under_a_roof;
            }
        }
        return score;
    }

    // --------- Helpers for creating Unity GameObjects ------------
    List<GameObject> CreateStructure(Blueprint structure)
    {
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
                            blocks.Add(Instantiate(brick_x_axis, new Vector3(i + 0.5f, j + 0.5f, k), new Quaternion()));
                            break;
                        case BlockType.BRICK_Y_AXIS:
                            blocks.Add(Instantiate(brick_x_axis, new Vector3(i, j + 1.0f, k), Quaternion.Euler(0, 0, 90)));
                            break;
                        case BlockType.BRICK_Z_AXIS:
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

    // --------- Math Helpers ---------
    int WeightedRandomIndex(double[] cdf)
    {
        double val = rng.NextDouble();
        int low = 0;
        int high = cdf.Length - 1;

        for (int i = 0; i < cdf.Length; i++)
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
}
