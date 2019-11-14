
import System.Collections.Generic;



////////////Sensors
public var basicLaserSensor:GameObject;
public var basicConeSensor:GameObject;

var myLayerMask : LayerMask;
var laserSensorList : List.<GameObject>;
var coneSensorList: List.<GameObject>;


function applySensors(){//returns indices of cars seen
	var ret=new List.<int>();
	for(ls in laserSensorList){
		var lsscript=ls.GetComponent("LaserSensor");
		lsscript.applySensor();
		var res=lsscript.getNearestIndex();

		if(res!=-3){
			ret.Add(res);
			lsscript.detectsObstacle = true;
		}
	}
	
	for(cones in coneSensorList){
		var conesscript=cones.GetComponent("ConeSensor");
		conesscript.applySensor();
		var res2=conesscript.getNearestIndex();
		if(res2!=-3){
			ret.add(res2);
		}
	}

	return ret;
}



function getDetectingSensorsList() {
	var ret = new System.Collections.Generic.Dictionary.<int, int>();
	for(ls in laserSensorList) {
		var lsscript=ls.GetComponent("LaserSensor");
		if(lsscript.detectsObstacle) { 
			ret.Add(lsscript.index, lsscript.getNearestIndex());
		}
	}

	return ret;
}


function createLaserSensorByAngle(dist:float,angles:Vector2,direction:Vector3,index:int){
	var numberOfBeams=1;
	var degrees=1;
	
	var forward=transform.TransformDirection(Vector3.forward);
	var currentp=transform.position;
	//rigidbody.isKinematic=true;
	var translation=new Vector3(0,0,0);
	transform.position+=translation;
	var pointOnSphere=new Vector3(4*Mathf.Sin(angles.x)*Mathf.Cos(angles.y),4*Mathf.Sin(angles.y),4*Mathf.Cos(angles.x)*Mathf.Cos(angles.y));
	pointOnSphere=Quaternion.FromToRotation(new Vector3(1,0,0),forward)*pointOnSphere;
	pointOnSphere+=transform.position;
	////raycast
	
	var targetPoint=transform.position+new Vector3(0,1,0);
	var currentDir=(targetPoint-pointOnSphere)*100;
	var currentRay=new Ray(pointOnSphere,currentDir);
	var hit:RaycastHit;
	
	if(Physics.Raycast(currentRay,hit,100,myLayerMask)) {
		//Debug.Log("Touched object " + hit.transform.gameObject.name + " layer is " + hit.transform.gameObject.layer);
		if(hit.transform.gameObject.layer==8 || hit.transform.gameObject.layer==9){ //Refer to layer numbers in Project Settings > Tags and layers
			var backDir=Vector3.Normalize(-currentDir);
			
				//Debug.Log("Placed");
				//Debug.Log(hit.point);
				var newSensorAdd = Instantiate (basicLaserSensor, hit.point+backDir*0.1 , transform.rotation);
				newSensorAdd.transform.LookAt(pointOnSphere);
				newSensorAdd.transform.Rotate(direction);
				var lsscript=newSensorAdd.GetComponent("LaserSensor");
				lsscript.setPrams(numberOfBeams,degrees,dist,index);
				newSensorAdd.transform.parent = transform; 
				Debug.DrawLine(pointOnSphere-translation,hit.point-translation,Color.blue,0);
				laserSensorList.Add(newSensorAdd);
				//check if laser hits self
				lsscript.applySensor();
				var res=lsscript.getNearestIndex();
				if(res==-1){
					laserSensorList.Remove(newSensorAdd);
					GameObject.Destroy(newSensorAdd);
					//Debug.Log("Couldn't place sensor: Hits Self");
					return false;
				}
				
				return true;
		}else{
			return false;
		}
		
	}else{
	Debug.Log("Couldn't place sensor");
	return false;
	Debug.DrawLine(pointOnSphere-translation,targetPoint-translation,Color.green,0);
	}
	
	
		
	
	transform.position=currentp;
	//Debug.Log("!!");
	//rigidbody.isKinematic=false;
	//Debug.Log(pointOnSphere);
	//////
	
}

function createConeSensorByAngle(dist:float,theta:float,angles:Vector2,direction:Vector3,index:int){
	
	var forward=transform.TransformDirection(Vector3.forward);
	var currentp=transform.position;
	//rigidbody.isKinematic=true;
	var translation=new Vector3(0,0,0);
	transform.position+=translation;
	var pointOnSphere=new Vector3(4*Mathf.Sin(angles.x)*Mathf.Cos(angles.y),4*Mathf.Sin(angles.y),4*Mathf.Cos(angles.x)*Mathf.Cos(angles.y));
	pointOnSphere=Quaternion.FromToRotation(new Vector3(1,0,0),forward)*pointOnSphere;
	pointOnSphere+=transform.position;
	////raycast
	
	var targetPoint=transform.position+new Vector3(0,1,0);
	var currentDir=(targetPoint-pointOnSphere)*100;
	var currentRay=new Ray(pointOnSphere,currentDir);
	var hit:RaycastHit;
	
	if(Physics.Raycast(currentRay,hit,100,myLayerMask)) {
		//Debug.Log("Touched object " + hit.transform.gameObject.name + " layer is " + hit.transform.gameObject.layer);
		if(hit.transform.gameObject.layer==8){
			var backDir=Vector3.Normalize(-currentDir);
			
				//Debug.Log("Placed");
				//Debug.Log(hit.point);
				var newSensorAdd = Instantiate (basicConeSensor, hit.point+backDir*0.1 , transform.rotation);
				newSensorAdd.transform.Rotate(direction);
				var lsscript=newSensorAdd.GetComponent("ConeSensor");
				lsscript.setPrams(theta,dist);
				
				newSensorAdd.transform.parent = transform; 
				//Debug.DrawLine(pointOnSphere-translation,hit.point-translation,Color.green,100000);
				coneSensorList.Add(newSensorAdd);
				//////////////test if hits car
				lsscript.applySensor();
				var res=lsscript.getNearestIndex();
				if(res==-1){
					laserSensorList.Remove(newSensorAdd);
					GameObject.Destroy(newSensorAdd);
					Debug.Log("Couldn't place sensor: Hits Self");
					return false;
				}
				
				return true;
			
		}else{
			return false;
		}
		
	}else{
	Debug.Log("Couldn't place sensor");
	return false;
	//Debug.DrawLine(pointOnSphere-translation,targetPoint-translation,Color.green,0);
	}
	
	//Debug.Log("!!");
		
	
	transform.position=currentp;
	//rigidbody.isKinematic=false;
	//Debug.Log(pointOnSphere);

	//////
	
}

function Start()
{	
	laserSensorList = new List.<GameObject>();
	coneSensorList=new List.<GameObject>();
	
	
	
}

function resetLaserSensors(){
	laserSensorList = new List.<GameObject>();
	coneSensorList=new List.<GameObject>();

}

function Update()
{	

	/*
	var a1=Random.Range(0,3.14);
	var a2=Random.Range(0,3.14);
	var a3=Random.Range(0,360);
	var a4=Random.Range(0,360);
	var a5=Random.Range(0,360);
	
	//Debug.Log(a1+" "+a2);
	var res=createLaserSensorByAngle(10,new Vector2(a1,a2),Vector3(a3,a4,a5));
	if(res==true){
	Debug.Log("!!");
	}
	*/
	
}

function FixedUpdate()
{	
	
}
