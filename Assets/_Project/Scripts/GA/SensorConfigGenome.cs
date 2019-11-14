using System.Linq;
using Random = UnityEngine.Random;

public class SensorConfigGenome
{
    public SensorConfigProperties[] Genome { get; private set; }
    public SensorDimensions dimensions { get; private set; }

    public SensorConfigGenome(SensorConfigProperties[] genome)
    {
        Genome = genome;
    }

    public SensorConfigGenome(SensorConfigGenome genome_to_copy)
    {
        Genome = genome_to_copy.Genome.Clone() as SensorConfigProperties[];
    }

    public SensorConfigGenome(SensorDimensions dim)
    {
        dimensions = dim;
        SensorType type;
        //Make a random genome of size 10
        SensorConfigProperties[] genome = new SensorConfigProperties[10];

        for (var i = 0; i < genome.Length; i++) {

            //Todo: cleanup - possibly create a parameter specifying
            //proportion of each type of sensor.
            if (Random.Range(0f, 1f) > 0.5f)
                type = SensorType.Laser;
            else
                type = SensorType.Radar;

            genome[i] = new SensorConfigProperties(dimensions, type);
        }

        Genome = genome;
    }

    public override bool Equals(object other)
    {
        var other_genome = other as SensorConfigGenome;

        var equal = true;
        for (var i = 0; i < Genome.Length; i++)
        {
            //Check equal if still equal
            if (equal)
                equal = Genome[i].Equals(other_genome.Genome[i]);
            else
                break;                //No need to continue if not equal
        }

        return equal;
    }

    public override string ToString()
    {
        return Genome.Aggregate("", (current, f) => current + (f + " "));
    }
}