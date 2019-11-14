using UnityEngine;
using System.Collections;
using System.Linq;

public class SensorConfigChamp
{

    public readonly SensorConfigGenome Genome;
    public readonly float Fitness;

    public SensorConfigChamp(SensorConfigGenome genome, float fitenss)
    {
        Genome = genome;
        Fitness = fitenss;
    }

    public override string ToString()
    {
        string retrn = Genome.Genome.Aggregate("", (current, f) => current + (f + " "));
        return retrn + " fit: " + Fitness;
    }
}
