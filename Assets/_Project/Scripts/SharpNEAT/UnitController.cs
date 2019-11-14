using UnityEngine;
using SharpNeat.Phenomes;
using System.Collections.Generic;

public abstract class UnitController : MonoBehaviour {
    public abstract void Activate(IBlackBox box);

	public abstract bool Stopped {
		get;
	}
	public abstract Dictionary<string,List<float>> SensorInputs {
		get;
	}

    public abstract void Stop();

    public abstract float GetFitness();

    public abstract float GetNovelty();

    public abstract void SetNovelty(float novelty);

    public abstract int GetNumberOfInputsIntoNeat();

    public abstract int GetNumberOfOutputsNeededFromNeat();

    public abstract void SetWaypoints(GameObject [] waypoints);

    public abstract void SetBehaviourCharacterisation(BehaviourCharacterisationType type);

    public abstract float[] GetBehaviourCharacterisation();

    public abstract void SetRunType(RunType type);
}
