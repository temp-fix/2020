using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public static class SensorConfigGAOperators
{
    //Replacment operators
    public static SensorConfigGenome[] Replacement_Elitist(int elite_perfect, GameObject[] pop)
    {
        // Get the phenomes from the game objects
        var phenomes = new SensorConfigIndividual[pop.Length];
        for (var indi = 0; indi < pop.Length; indi++)
        {
            phenomes[indi] = pop[indi].GetComponent<SensorConfigIndividual>();
        }

        // The elite percent ust be between 0 and 100
        Assert.IsTrue(elite_perfect >= 0 && elite_perfect <= 100);

        // Calc the no of fittest indivudals that must go into the next pop unchanged
        int no_of_fittest = (elite_perfect * pop.Length) / 100;

        var fittest = new SensorConfigGenome[no_of_fittest];
        //Get the fittest genomes from the sorted phenomes array
        for (var i = 0; i < no_of_fittest; i++)
        {
            fittest[i] = phenomes[i].Genes;
        }

        return fittest;
    }

    //TODO: do Replacement_SteadyState

    //Selection operators
    public static SensorConfigIndividual Selection_Random(GameObject[] pop)
    {
        int rand_pop_idx = Random.Range(0, pop.Length); //upper bound is non-inclusive
        return pop[rand_pop_idx].GetComponent<SensorConfigIndividual>();
    }

    public static SensorConfigIndividual Selection_FitnessProportionate(GameObject[] pop)
    {
        float fitness_total = pop.Sum(individual => individual.GetComponent<SensorConfigIndividual>().Fitness);

        float random = Random.Range(0f, fitness_total);
        foreach (GameObject individual in pop)
        {
            var indi = individual.GetComponent<SensorConfigIndividual>();
            random -= indi.Fitness;

            if (random < 0)
            {
                return indi;
            }
        }

        return pop[0].GetComponent<SensorConfigIndividual>(); //This should never happen though
    }

    public static SensorConfigIndividual Selection_Tournament(int tournament_size, float probabality_of_selection,
        GameObject[] pop)
    {
        var tournament = new SensorConfigIndividual[tournament_size];

        // Randomly choose k = tournament_size number of individuals from the pop
        for (var i = 0; i < tournament_size; i++)
        {
            tournament[i] = Selection_Random(pop);
        }

        // Sort the array on fitness, best on top
        Array.Sort(tournament, (A, B) => B.Fitness.CompareTo(A.Fitness));

        // Choose the rth best inididual from the tournament with probabilty p
        // where r is the round number. If no selection occured, r increases.
        var r = 0;
        do
        {
            float prob = probabality_of_selection;
            if (Random.value <= prob)
            {
                return tournament[r];
            }

            r++;
        } while (r < tournament_size);

        //If no individual was selected, return the fittest
        return tournament[0];
    }

    //Recombination operators
    public static SensorConfigGenome Recomb_OnePointCrossover(SensorConfigIndividual parent_A,
        SensorConfigIndividual parent_B)
    {
        SensorConfigGenome parent_A_gene = parent_A.Genes;
        SensorConfigGenome parent_B_gene = parent_B.Genes;

        Assert.AreEqual(parent_A_gene.Genome.Length, parent_B_gene.Genome.Length);

        int rand_crossover_point = Random.Range(0, parent_A_gene.Genome.Length); //upper non-inclusive

        var genome = new SensorConfigProperties[parent_A_gene.Genome.Length];

        // Get the genes from A until crossover point
        for (var i = 0; i < rand_crossover_point; i++)
        {
            genome[i] = parent_A_gene.Genome[i];
        }

        // Get the genes from B until the end of genome
        for (int i = rand_crossover_point; i < parent_A_gene.Genome.Length; i++)
        {
            genome[i] = parent_B_gene.Genome[i];
        }

        return new SensorConfigGenome(genome);
    }

    public static SensorConfigGenome Recomb_Uniform(SensorConfigIndividual parent_A,
        SensorConfigIndividual parent_B, float prob_fitter_genes_being_chosen)
    {
        SensorConfigGenome parent_A_gene;
        SensorConfigGenome parent_B_gene;

        //A must be the fitter
        if (parent_A.Fitness < parent_B.Fitness)
        {
            parent_A_gene = parent_B.Genes;
            parent_B_gene = parent_A.Genes;
        }
        else
        {
            parent_A_gene = parent_A.Genes;
            parent_B_gene = parent_B.Genes;
        }

        Assert.AreEqual(parent_A_gene.Genome.Length, parent_B_gene.Genome.Length);

        var genome = new SensorConfigProperties[parent_A_gene.Genome.Length];

        for (var i = 0; i < parent_A_gene.Genome.Length; i++)
        {
            if (Random.value < prob_fitter_genes_being_chosen)
            {
                //Get the gene from parentA
                genome[i] = parent_A_gene.Genome[i];
            }
            else
            {
                //Get the gene from parentB
                genome[i] = parent_B_gene.Genome[i];
            }
        }

        return new SensorConfigGenome(genome);
    }

    public static SensorConfigGenome Recomb_Local(SensorConfigIndividual parent_A,
        SensorConfigIndividual parent_B)
    {
        SensorConfigGenome parent_A_genes = parent_A.Genes;
        SensorConfigGenome parent_B_genes = parent_B.Genes;


        Assert.AreEqual(parent_A_genes.Genome.Length, parent_B_genes.Genome.Length);

        var genome = new SensorConfigProperties[parent_A_genes.Genome.Length];

        for (var i = 0; i < parent_A_genes.Genome.Length; i++)
        {
            float alpha_vert = Random.value;
            float vert = alpha_vert * parent_A_genes.Genome[i].angles.x + (1 - alpha_vert) * parent_B_genes.Genome[i].angles.x;

            float alpha_hori = Random.value;
            float hori = alpha_hori * parent_A_genes.Genome[i].angles.y + (1 - alpha_hori) * parent_B_genes.Genome[i].angles.y;


            float alpha_sensorVert = Random.value;
            float sensorVert = alpha_sensorVert * parent_A_genes.Genome[i].direction.x + (1 - alpha_sensorVert) * parent_B_genes.Genome[i].direction.x;

            float alpha_sensorHori = Random.value;
            float sensorHori = alpha_sensorHori * parent_A_genes.Genome[i].direction.y + (1 - alpha_sensorHori) * parent_B_genes.Genome[i].direction.y;

            float alpha_range = Random.value;
            float range = alpha_range * parent_A_genes.Genome[i].Range + (1 - alpha_range) * parent_B_genes.Genome[i].Range;

            float alpha_fov = Random.value;
            float fov = alpha_fov * parent_A_genes.Genome[i].FOV + (1 - alpha_fov) * parent_B_genes.Genome[i].FOV;

            vert = ClipValues(vert, -30, 30);
            hori = ClipValues(hori, 0, 360);
            sensorVert = ClipValues(sensorVert, -30, 30);
            sensorHori = ClipValues(sensorHori, -30, 30);
            range = ClipValues(range, 5, 50);
            fov = ClipValues(fov, 5, 25);

            if (Random.value > 0.5)
                genome[i] = new SensorConfigProperties(new Vector2(vert, hori), new Vector3(sensorVert, sensorHori), range, fov, parent_A_genes.Genome[i].sensorType);
            else
                genome[i] = new SensorConfigProperties(new Vector2(vert, hori), new Vector3(sensorVert, sensorHori), range, fov, parent_B_genes.Genome[i].sensorType);
        }

        return new SensorConfigGenome(genome);
    }

    public static SensorConfigGenome Recomb_FitnessProportinate(SensorConfigIndividual parent_A,
        SensorConfigIndividual parent_B)
    {
        SensorConfigGenome parent_A_gene = parent_A.Genes;
        SensorConfigGenome parent_B_gene = parent_B.Genes;

        //The probabilty of A being selected is based the two forumlas:
        //First one finds the norm'd, relative to A, differance of fitness.
        //This function should tend to 1 as the differnace between the two gets large
        //It is dF = (fitness_A - fitness_B) / (fitness_A + fitness_B)

        //Using that in a modified Sigmoid function, we get:
        // P = 1/( 1 + 2^(-7 * dF) )

        //This may seem arb, but I played with the consts. until the function worked well. (i plotted it)

        float delta_fitness = (parent_A.Fitness - parent_B.Fitness) / (parent_A.Fitness + parent_B.Fitness);
        float gene_selection_probabailty_for_parentA = 1 / (1 + Mathf.Pow(2, -7 * delta_fitness));

        Assert.AreEqual(parent_A_gene.Genome.Length, parent_B_gene.Genome.Length);

        var genome = new SensorConfigProperties[parent_A_gene.Genome.Length];

        for (var i = 0; i < parent_A_gene.Genome.Length; i++)
        {
            if (Random.value < gene_selection_probabailty_for_parentA)
            {
                //Get the gene from parentA
                genome[i] = parent_A_gene.Genome[i];
            }
            else
            {
                //Get the gene from parentB
                genome[i] = parent_B_gene.Genome[i];
            }
        }

        return new SensorConfigGenome(genome);
    }


    //Mutation operators
    public static SensorConfigGenome Mutation_Random(float mutation_probabilty,
        SensorConfigGenome genome_to_mutate, SensorDimensions dimensions)
    {
        var mutant = new SensorConfigGenome(genome_to_mutate);

        if (Random.value < mutation_probabilty)
            for (var gene = 0; gene < mutant.Genome.Length; gene++)
                mutant.Genome[gene] = new SensorConfigProperties(genome_to_mutate.dimensions, genome_to_mutate.Genome[gene].sensorType);

        return mutant;
    }

    public static SensorConfigGenome Mutation_Breeder(float mutation_range_percent, float mutation_precision,
        SensorConfigGenome genome_to_mutate, SensorDimensions dimensions)
    {
        //check out http://www.geatbx.com/docu/algindex-04.html for more info

        //mutation prob per gene, on average one gene will mutate
        float mutation_rate = 1.0f / genome_to_mutate.Genome.Length;

        //Make a copy of the genome
        var mutation = new SensorConfigGenome(genome_to_mutate);

        for (var gene = 0; gene < mutation.Genome.Length; gene++)
        {
            //Will the gene mutate?
            if (Random.value <= mutation_rate)
            {
                //vert_step_size range is [-30,30]
                float vert_step_size = Random.Range(-1.0f, 1.0f) * (mutation_range_percent / 100) * 30 * //60 is the domain
                                       Mathf.Pow(2, (-1 * Random.value * mutation_precision));

                float vert = mutation.Genome[gene].angles.x;
                if (dimensions == SensorDimensions.ThreeD)
                {
                    //Apply the change to vert
                    vert += vert_step_size;
                    vert = ClipValues(vert, -30, 30);
                }

                //hori_step_size range is [0,360]
                float hori_step_size = Random.Range(0.0f, 1.0f) * (mutation_range_percent / 100) * 360 * //360 is the domain
                                       Mathf.Pow(2, (-1 * Random.value * mutation_precision));

                float hori = mutation.Genome[gene].angles.y;

                //Apply the change to hori
                hori += hori_step_size;
                hori = ClipValues(hori, 0, 360);



                //sensor_vert_step_size range is [-30,30]
                float sensor_vert_step_size = Random.Range(-1.0f, 1.0f) * (mutation_range_percent / 100) * 30 * //60 is the domain
                                       Mathf.Pow(2, (-1 * Random.value * mutation_precision));

                float sensorVert = mutation.Genome[gene].direction.x;
                //Apply the change to vert
                sensorVert += sensor_vert_step_size;
                sensorVert = ClipValues(sensorVert, -30, 30);


                //sensor_vert_step_size range is [-30,30]
                float sensor_hori_step_size = Random.Range(-1.0f, 1.0f) * (mutation_range_percent / 100) * 30 * //60 is the domain
                                       Mathf.Pow(2, (-1 * Random.value * mutation_precision));

                float sensorHori = mutation.Genome[gene].direction.y;

                //Apply the change to hori
                sensorHori += sensor_hori_step_size;
                sensorHori = ClipValues(sensorHori, -30, 30);


                //sensor_range_step_size range is [5,50]
                float sensor_range_step_size = Random.Range(0.1f, 1.0f) * (mutation_range_percent / 100) * 50 *
                                       Mathf.Pow(2, (-1 * Random.value * mutation_precision));

                float range = mutation.Genome[gene].Range;

                //Apply the change to range
                range += sensor_range_step_size;
                range = ClipValues(range, 5, 50);

                //sensor_range_step_size range is [5,25]
                float sensor_fov_step_size = Random.Range(0.2f, 1.0f) * (mutation_range_percent / 100) * 25 *
                                       Mathf.Pow(2, (-1 * Random.value * mutation_precision));

                float fov = mutation.Genome[gene].Range;

                //Apply the change to range
                fov += sensor_fov_step_size;
                fov = ClipValues(fov, 5, 25);

                //Set as new gene
                mutation.Genome[gene] = new SensorConfigProperties(new Vector2(vert, hori), new Vector3(sensorVert, sensorHori), range, fov, genome_to_mutate.Genome[gene].sensorType);
            }
        }

        return mutation;
    }

    public static SensorConfigGenome Mutation_Gaussian(SensorConfigGenome genome_to_mutate, SensorDimensions dimensions,
        float prob = 1,
        float mutation_precision = 10f)
    {
        //mutation prob per gene, on average one gene will mutate
        float mutation_rate = prob * (1.0f / genome_to_mutate.Genome.Length);

        float vert_std_dev = 60.0f / mutation_precision; //a tenth of the range for the vert values
        float hori_std_dev = 360.0f / mutation_precision; //a tenth of the range of the hori values
        float sensor_std_dev = vert_std_dev;

        float range_std_dev = 45.0f / mutation_precision;
        float fov_std_dev = 20.0f / mutation_precision;

        //Make a copy of the genome
        var mutation = new SensorConfigGenome(genome_to_mutate);

        for (var gene = 0; gene < mutation.Genome.Length; gene++)
        {
            //Will the gene mutate?
            if (Random.value <= mutation_rate)
            {
                float vert = mutation.Genome[gene].angles.x;
                if (dimensions == SensorDimensions.ThreeD)
                {
                    //Apply the change to vert
                    vert += NextGaussian(0, vert_std_dev);
                    vert = ClipValues(vert, -30, 30);
                }

                float hori = mutation.Genome[gene].angles.y;

                //Apply the change to hori
                hori += NextGaussian(0, hori_std_dev);
                hori = ClipValues(hori, 0, 360);

                float sensorVert = mutation.Genome[gene].direction.x;
                //Apply the change to vert
                sensorVert += NextGaussian(0, sensor_std_dev);
                sensorVert = ClipValues(vert, -30, 30);

                float sensorHori = mutation.Genome[gene].direction.y;
                sensorHori += NextGaussian(0, sensor_std_dev);
                sensorHori = ClipValues(hori, -30, 30);

                float range = mutation.Genome[gene].Range;
                range += NextGaussian(0, range_std_dev);
                range = ClipValues(range, 5, 50);

                float fov = mutation.Genome[gene].FOV;
                fov += NextGaussian(0, fov_std_dev);
                fov = ClipValues(range, 5, 50);


                //Set as new gene
                mutation.Genome[gene] = new SensorConfigProperties(new Vector2(vert, hori), new Vector3(sensorVert, sensorHori), range, fov, genome_to_mutate.Genome[gene].sensorType);
            }
        }

        return mutation;
    }

    private static float NextGaussian(float mean = 0, float std_dev = 1)
    {
        float u1 = Random.value;
        float u2 = Random.value;

        double Box_Mueller_trans = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                   Math.Sin(2.0 * Math.PI * u2);

        float rand_normal = mean + std_dev * (float)Box_Mueller_trans;

        return rand_normal;
    }


    private static float ClipValues(float value, float min, float max)
    {
        if (value > max)
            value = max;
        else if (value < min)
            value = min;
        return value;
    }

}