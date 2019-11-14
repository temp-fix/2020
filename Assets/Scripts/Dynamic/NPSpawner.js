

public var spawnObject: GameObject;
public var checkPoints: GameObject[];
public var spawnNPAI=true;
public var AStar=false;
public var spawnPosition=0;
public var targetVelocity=40;
private var g:GameObject;
public var enableSpawner=false;
// Use this for initialization
function Start () {
		if(!enableSpawner){
			return;
		}
		g=Instantiate (spawnObject, checkPoints[spawnPosition].transform.position+new Vector3(0,0.2f,0), Quaternion.identity);
		var forward=g.transform.TransformDirection(Vector3.forward);
		g.transform.LookAt(checkPoints[(spawnPosition+1)%checkPoints.Length].transform.position);
		//g.transform.Rotate(Quaternion.ToEulerAngles(Quaternion.FromToRotation(forward,to)));
		
		var carS = g.GetComponent ("Car");
		carS.checkPoints=checkPoints;
		carS.checkPointNum=(spawnPosition+1)%checkPoints.Length;
		carS.AIDriving=true;
		carS.cyclePath=true;
		carS.targetVelocity=targetVelocity;
		carS.userSteeringOverride=false;
		carS.NPAIDriving=spawnNPAI;
		carS.AStar=AStar;
		
		
}
	


function Update () {

}