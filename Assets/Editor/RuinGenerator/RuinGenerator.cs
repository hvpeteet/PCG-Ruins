using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public delegate void OnGenerationFinish(int gen_number);

public class RuinGenerator {

    // ------ Public variables / parameters ------
    public int pop_size = 100;
    // Elites are the best of the previous round 
    // that are given immortality until someone surpasses them
    public int num_elite = 0;
    // Survivors are NOT mutated from the previous round
    public int num_survivors = 0;
    public int num_rounds = 100;
    public int building_dimension_x = 10;
    public int building_dimension_y = 10;
    public int building_dimension_z = 10;
    private const int MAX_MUTATE_SEARCH_ITERS = 20;
    private System.Random rng = new System.Random();

    private Blueprint[] population;

    Blueprint[] CreateEmptyPopulation()
    {
        Blueprint[] pop = new Blueprint[pop_size];
        for (int i = 0; i < pop.Length; i++)
        {
            pop[i] = new Blueprint(building_dimension_x, building_dimension_y, building_dimension_z);
        }
        return pop;
    }

    public RuinGenerator()
    {
        this.population = CreateEmptyPopulation();
        foreach (Blueprint b in population)
        {
            b.Randomize();
        }
    }

    int CompareBlueprints(Blueprint a, Blueprint b)
    {
        return CalculateScore(a).CompareTo(CalculateScore(b));
    }

    private static void Nop(int unused)
    {
        // Does nothing
    }

    public Blueprint GenerateRuin()
    {
        return GenerateRuin(Nop);
    }

    public Blueprint GenerateRuin(OnGenerationFinish on_gen_finish)
    {
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

            Debug.Log(string.Format("First 2 scores: {0}, {1}", scores[0], scores[1]));

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
            Debug.Log(string.Format("cdf is {0} {1} {2} {3}", cdf.Select(x => x.ToString()).ToArray()));


            // Setup the next generation            
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

            // Select everyone else and mutate them
            for (int i = num_survivors + num_elite; i < next_generation.Length; i++)
            {
                population[WeightedRandomIndex(cdf)].CopyInto(next_generation[i]);
                // Generate mutations
                next_generation[i].Mutate();
            }
            population = next_generation;
            on_gen_finish(round);
            Debug.Log(string.Format("finished gen, best score {0}", scores.Max()));
        }
        System.Array.Sort(population, (a, b) => CompareBlueprints(b, a));
        return population[0];
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
