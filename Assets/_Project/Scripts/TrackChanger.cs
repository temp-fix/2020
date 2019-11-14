using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrackChanger : MonoBehaviour {

	public GameObject[] tracks;
	private int currentTrackIndex;

	public GameObject CurrentTrack
	{
		get { return tracks[currentTrackIndex]; }
	}

    public int CurrentTrackIndex
    {
        get { return currentTrackIndex; }
    }

    public void resetObstacleSpawnsForCurrentTrack()
    {
        Transform forwardObstacle = CurrentTrack.transform.Find("obstacle_spawn/obstacle");
        Transform reverseObstacle = CurrentTrack.transform.Find("obstacle_spawn_rev/obstacle");

        if (forwardObstacle != null)
            forwardObstacle.gameObject.SetActive(false);

        if (reverseObstacle != null)
            reverseObstacle.gameObject.SetActive(false);
    }

    public GameObject getReverseStart()
    {
        foreach (Transform child in CurrentTrack.transform)
        {
            if (child.name == "rev_start")
                return child.gameObject;
        }
        return null;
    }

    public GameObject [] getAllStartingPositions()
    {
        List<GameObject> starts = new List<GameObject>();
        foreach (Transform child in CurrentTrack.transform)
        {
            if(child.name.Contains("start_"))
            {
                starts.Add(child.gameObject);
            }
        }

        return starts.ToArray();
    }

    public void disableAllTracks() {
		foreach (GameObject track in tracks) {
			track.SetActive(false);
		}
	}

	public void nextTrack() {
		currentTrackIndex++;
		if(currentTrackIndex > tracks.Length -1) {
			currentTrackIndex = 0;
		}
		disableAllTracks();
		tracks[currentTrackIndex].SetActive(true);
	}

	public void previousTrack() {
		currentTrackIndex--;
		if(currentTrackIndex < 0) {
			currentTrackIndex = tracks.Length - 1;
		}
		
		disableAllTracks();
		tracks[currentTrackIndex].SetActive(true);
	}

	public GameObject [] getTargetsForCurrentTrack(bool reverse) {
		List<GameObject> targetsForTrack = new List<GameObject>();

		foreach(Transform child in tracks [currentTrackIndex].transform) {
			if (child.name == "targets") {
				foreach(Transform target in child) {
					targetsForTrack.Add(target.gameObject);
				}
			}
		}

        if(reverse)
            targetsForTrack.Reverse();

		return targetsForTrack.ToArray();
	}

    public GameObject [] getSpecificTargetsForCurrentTrack(int targetGroup)
    {
        List<GameObject> targetsForTrack = new List<GameObject>();

        foreach (Transform child in tracks[currentTrackIndex].transform)
        {
            if (child.name == "targets_" + targetGroup)
            {
                foreach (Transform target in child)
                {
                    targetsForTrack.Add(target.gameObject);
                }
            }
        }

        return targetsForTrack.ToArray();
    }

    public void SetObstacleLayerToObstacles()
    {
        foreach (GameObject track in tracks)
        {
            foreach (Transform child in track.transform)
            {
                if (child.name == "obstacles")
                {
                    foreach (Transform obstacle in child)
                    {
                        obstacle.gameObject.layer = LayerMask.NameToLayer("Obstacles");
                    }
                }
            }
        }
    }

	// Use this for initialization
	void Awake () {
		if (tracks.Length > 0) {
			currentTrackIndex = 0;
			disableAllTracks ();
			tracks [currentTrackIndex].SetActive (true);
		}
	}
}
