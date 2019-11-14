﻿
import System.Collections.Generic;


private var wheelRadius : float = 0.4;
var suspensionRange : float = 0.1;
var suspensionDamper : float = 50;
var suspensionSpringFront : float = 18500;
var suspensionSpringRear : float = 9000;

public var brakeLights : Material;

var dragMultiplier : Vector3 = new Vector3(2, 5, 1);

var throttle : float = 0; 
private var steer : float = 0;
private var handbrake : boolean = false;

var centerOfMass : Transform;

var frontWheels : Transform[];
var rearWheels : Transform[];

private var wheels : Wheel[];
wheels = new Wheel[frontWheels.Length + rearWheels.Length];

private var wfc : WheelFrictionCurve;

var topSpeed : float = 160;
var numberOfGears : int = 5;

var maximumTurn : int = 15;
var minimumTurn : int = 10;

var resetTime : float = 5.0;
private var resetTimer : float = 0.0;

private var engineForceValues : float[];
private var gearSpeeds : float[];

private var currentGear : int;
private var currentEnginePower : float = 0.0;

private var handbrakeXDragFactor : float = 0.5;
private var initialDragMultiplierX : float = 10.0;
private var handbrakeTime : float = 0.0;
private var handbrakeTimer : float = 1.0;

//private var skidmarks : Skidmarks = null;
//private var skidSmoke : ParticleEmitter = null;
//var skidmarkTime : float[];

//private var sound : SoundController = null;
//sound = transform.GetComponent(SoundController);

private var canSteer : boolean;
private var canDrive : boolean;


canDrive=true;

//NPAI CODE
var AIThrottle=0f;
var AISteer=0f;
private var AIDriving=true;
private var propSteerConst=10f;
private var propThrottleConst=4;
private var ThrottleConst=10;
private var noCollisionsThrottleProp=0.1;
public var targetVelocity=40f;
private var distThresh=[3f,5f,10f,20f,100000000000f];
private var targetSpeeds=[35f,35f,40f,40f,targetVelocity];
private var minSpeed=35f;
var relativeVelocity: Vector3;

private var NPAIDriving=true;
private var cyclePath=false;//else destroy when path is finished
public var checkPointNum=0;
public  var checkPoints: Vector3[];
function applyNPAIDriving(){
	var forward = transform.TransformDirection(Vector3.forward);
	var nextPos=checkPoints[checkPointNum]-transform.position;
	if(Vector3.Dot(forward,nextPos)<0){
		++checkPointNum;
		if(checkPointNum>=checkPoints.Length){
			checkPointNum=0;
			if(!cyclePath){
				Destroy(gameObject);
			}
		}
		//Debug.Log(checkPointNum);
		nextPos=checkPoints[checkPointNum]-transform.position;
	}
	AISteer=0;
	AIThrottle=noCollisionsThrottleProp*(targetVelocity-Vector3.Magnitude(relativeVelocity));
	
	var left = transform.TransformDirection(Vector3.left);
	AISteer=-Vector3.Dot(left,nextPos)/10f;
	AIThrottle=Mathf.Min(1,AIThrottle);
	AIThrottle=Mathf.Max(-1,AIThrottle);
	//AISteer=minDSteerLeft+minDSteerRight;
	AISteer=Mathf.Min(1,AISteer);
	AISteer=Mathf.Max(-1,AISteer);
	
	
}





/*
class Wheel
{
	var collider : WheelCollider;
	var wheelGraphic : Transform;
	var tireGraphic : Transform;
	var driveWheel : boolean = false;
	var steerWheel : boolean = false;
	var lastSkidmark : int = -1;
	var lastEmitPosition : Vector3 = Vector3.zero;
	var lastEmitTime : float = Time.time;
	var wheelVelo : Vector3 = Vector3.zero;
	var groundSpeed : Vector3 = Vector3.zero;
}


*/

function Start()
{	
	
	
	accelerationTimer = Time.time;
	
	SetupWheelColliders();
	
	SetupCenterOfMass();
	
	topSpeed = Convert_Miles_Per_Hour_To_Meters_Per_Second(topSpeed);
	
	SetupGears();

	
	initialDragMultiplierX = dragMultiplier.x;
	Physics.gravity = new Vector3(0, -2*9.8F, 0);
	
}

function Update()
{		
	
	relativeVelocity = transform.InverseTransformDirection(GetComponent.<Rigidbody>().velocity);
	
	
	GetInput();
	
	Check_If_Car_Is_Flipped();
	
	UpdateWheelGraphics(relativeVelocity);
	
	UpdateGear(relativeVelocity);
}

function FixedUpdate()
{	
	// The rigidbody velocity is always given in world space, but in order to work in local space of the car model we need to transform it first.
	var relativeVelocity : Vector3 = transform.InverseTransformDirection(GetComponent.<Rigidbody>().velocity);
	
	CalculateState();	
	
	UpdateFriction(relativeVelocity);
	
	UpdateDrag(relativeVelocity);
	
	CalculateEnginePower(relativeVelocity);
	
	ApplyThrottle(canDrive, relativeVelocity);
	
	ApplySteering(canSteer, relativeVelocity);
}

/**************************************************/
/* Functions called from Start()                  */
/**************************************************/

function SetupWheelColliders()
{
	
	SetupWheelFrictionCurve();
	
	var wheelCount : int = 0;
	
	for (var t : Transform in frontWheels)
	{
		wheels[wheelCount] = SetupWheel(t, true);
		wheelCount++;
	}
	
	for (var t : Transform in rearWheels)
	{
		wheels[wheelCount] = SetupWheel(t, false);
		wheelCount++;
	}
	//Debug.Log(wheelCount);
}

function SetupWheelFrictionCurve()
{
	wfc = new WheelFrictionCurve();
	
	wfc.extremumSlip = 1;
	wfc.extremumValue = 50;
	wfc.asymptoteSlip = 2;
	wfc.asymptoteValue = 25;
	wfc.stiffness = 1;
	
	
}

function SetupWheel(wheelTransform : Transform, isFrontWheel : boolean)
{
	var go : GameObject = new GameObject(wheelTransform.name + " Collider");
	go.transform.position = wheelTransform.position;
	go.transform.parent = transform;
	go.transform.rotation = wheelTransform.rotation;
		
	var wc : WheelCollider = go.AddComponent(typeof(WheelCollider)) as WheelCollider;
	wc.suspensionDistance = suspensionRange;
	var js : JointSpring = wc.suspensionSpring;
	
	if (isFrontWheel)
		js.spring = suspensionSpringFront;
	else
		js.spring = suspensionSpringRear;
		
	js.damper = suspensionDamper;
	wc.suspensionSpring = js;
		
	wheel = new Wheel(); 
	wheel.collider = wc;
	wc.sidewaysFriction = wfc;
	wheel.wheelGraphic = wheelTransform;
	
	//Debug.Log(wheelTransform.GetComponentsInChildren(Transform).Length);
	//Debug.Log(wheelTransform.name);
	wheel.tireGraphic = wheelTransform.GetComponentsInChildren(Transform)[1];
	
	wheelRadius = wheel.tireGraphic.GetComponent.<Renderer>().bounds.size.y / 2;	
	wheel.collider.radius = wheelRadius;
	
	if (isFrontWheel)
	{
		wheel.steerWheel = true;
		
		go = new GameObject(wheelTransform.name + " Steer Column");
		go.transform.position = wheelTransform.position;
		go.transform.rotation = wheelTransform.rotation;
		go.transform.parent = transform;
		wheelTransform.parent = go.transform;
	}
	else
		wheel.driveWheel = true;
		
	return wheel;
}

function SetupCenterOfMass()
{
	if(centerOfMass != null)
		GetComponent.<Rigidbody>().centerOfMass = centerOfMass.localPosition;
}

function SetupGears()
{
	engineForceValues = new float[numberOfGears];
	gearSpeeds = new float[numberOfGears];
	
	var tempTopSpeed : float = topSpeed;
		
	for(var i = 0; i < numberOfGears; i++)
	{
		if(i > 0)
			gearSpeeds[i] = tempTopSpeed / 4 + gearSpeeds[i-1];
		else
			gearSpeeds[i] = tempTopSpeed / 4;
		
		tempTopSpeed -= tempTopSpeed / 4;
	}
	
	var engineFactor : float = topSpeed / gearSpeeds[gearSpeeds.Length - 1];
	
	for(i = 0; i < numberOfGears; i++)
	{
		var maxLinearDrag : float = gearSpeeds[i] * gearSpeeds[i];// * dragMultiplier.z;
		engineForceValues[i] = maxLinearDrag * engineFactor;
	}
}



/**************************************************/
/* Functions called from Update()                 */
/**************************************************/

private var okayToStart=false;

function startCar(){
	okayToStart=true;

}


function GetInput()
{
	if(okayToStart){
		applyNPAIDriving();
	}
	
	throttle = AIThrottle;
	steer = AISteer;
	
	//Debug.Log(throttle);
	//Debug.Log(steer);
	
	
	CheckHandbrake();
}

function CheckHandbrake()
{
	if(Input.GetKey("space"))
	{
		if(!handbrake)
		{
			handbrake = true;
			handbrakeTime = Time.time;
			dragMultiplier.x = initialDragMultiplierX * handbrakeXDragFactor;
		}
	}
	else if(handbrake)
	{
		handbrake = false;
		StartCoroutine(StopHandbraking(Mathf.Min(5, Time.time - handbrakeTime)));
	}
}

function StopHandbraking(seconds : float)
{
	var diff : float = initialDragMultiplierX - dragMultiplier.x;
	handbrakeTimer = 1;
	
	// Get the x value of the dragMultiplier back to its initial value in the specified time.
	while(dragMultiplier.x < initialDragMultiplierX && !handbrake)
	{
		dragMultiplier.x += diff * (Time.deltaTime / seconds);
		handbrakeTimer -= Time.deltaTime / seconds;
		yield;
	}
	
	dragMultiplier.x = initialDragMultiplierX;
	handbrakeTimer = 0;
}

function Check_If_Car_Is_Flipped()
{
	if(transform.localEulerAngles.z > 80 && transform.localEulerAngles.z < 280)
		resetTimer += Time.deltaTime;
	else
		resetTimer = 0;
	
	if(resetTimer > resetTime)
		FlipCar();
}

function FlipCar()
{
	transform.rotation = Quaternion.LookRotation(transform.forward);
	transform.position += Vector3.up * 0.5;
	GetComponent.<Rigidbody>().velocity = Vector3.zero;
	GetComponent.<Rigidbody>().angularVelocity = Vector3.zero;
	resetTimer = 0;
	currentEnginePower = 0;
}

var wheelCount : float;
function UpdateWheelGraphics(relativeVelocity : Vector3)
{
	return;
	wheelCount = -1;
	
	for(var w : Wheel in wheels)
	{
		wheelCount++;
		var wheel : WheelCollider = w.collider;
		var wh : WheelHit = new WheelHit();
		
		// First we get the velocity at the point where the wheel meets the ground, if the wheel is touching the ground
		if(wheel.GetGroundHit(wh))
		{
			w.wheelGraphic.localPosition = wheel.transform.up * (wheelRadius + wheel.transform.InverseTransformPoint(wh.point).y);
			w.wheelVelo = GetComponent.<Rigidbody>().GetPointVelocity(wh.point);
			w.groundSpeed = w.wheelGraphic.InverseTransformDirection(w.wheelVelo);
			
			
		}
		else
		{
			// If the wheel is not touching the ground we set the position of the wheel graphics to
			// the wheel's transform position + the range of the suspension.
			w.wheelGraphic.position = wheel.transform.position + (-wheel.transform.up * suspensionRange);
			if(w.steerWheel)
				w.wheelVelo *= 0.9;
			else
				w.wheelVelo *= 0.9 * (1 - throttle);
			
			/*if(skidmarks)
			{
				w.lastSkidmark = -1;
				sound.Skid(false, 0);
			}*/
		}
		// If the wheel is a steer wheel we apply two rotations:
		// *Rotation around the Steer Column (visualizes the steer direction)
		// *Rotation that visualizes the speed
		if(w.steerWheel)
		{
			var ea : Vector3 = w.wheelGraphic.parent.localEulerAngles;
			ea.y = steer * maximumTurn;
			w.wheelGraphic.parent.localEulerAngles = ea;
			w.tireGraphic.Rotate(Vector3.right * (w.groundSpeed.z / wheelRadius) * Time.deltaTime * Mathf.Rad2Deg);
		}
		else if(!handbrake && w.driveWheel)
		{
			// If the wheel is a drive wheel it only gets the rotation that visualizes speed.
			// If we are hand braking we don't rotate it.
			w.tireGraphic.Rotate(Vector3.right * (w.groundSpeed.z / wheelRadius) * Time.deltaTime * Mathf.Rad2Deg);
		}
	}
}

function UpdateGear(relativeVelocity : Vector3)
{
	currentGear = 0;
	for(var i = 0; i < numberOfGears - 1; i++)
	{
		if(relativeVelocity.z > gearSpeeds[i])
			currentGear = i + 1;
	}
}

/**************************************************/
/* Functions called from FixedUpdate()            */
/**************************************************/

function UpdateDrag(relativeVelocity : Vector3)
{
	var relativeDrag : Vector3 = new Vector3(	-relativeVelocity.x * Mathf.Abs(relativeVelocity.x), 
												-relativeVelocity.y * Mathf.Abs(relativeVelocity.y), 
												-relativeVelocity.z * Mathf.Abs(relativeVelocity.z) );
	
	var drag = Vector3.Scale(dragMultiplier, relativeDrag);
		
	if(initialDragMultiplierX > dragMultiplier.x) // Handbrake code
	{			
		drag.x /= (relativeVelocity.magnitude / (topSpeed / ( 1 + 2 * handbrakeXDragFactor ) ) );
		drag.z *= (1 + Mathf.Abs(Vector3.Dot(GetComponent.<Rigidbody>().velocity.normalized, transform.forward)));
		drag += GetComponent.<Rigidbody>().velocity * Mathf.Clamp01(GetComponent.<Rigidbody>().velocity.magnitude / topSpeed);
	}
	else // No handbrake
	{
		drag.x *= topSpeed / relativeVelocity.magnitude;
	}
	
	if(Mathf.Abs(relativeVelocity.x) < 5 && !handbrake)
		drag.x = -relativeVelocity.x * dragMultiplier.x;
		

	GetComponent.<Rigidbody>().AddForce(transform.TransformDirection(drag) * GetComponent.<Rigidbody>().mass * Time.deltaTime);
}

function UpdateFriction(relativeVelocity : Vector3)
{
	var sqrVel : float = relativeVelocity.x * relativeVelocity.x;
	
	// Add extra sideways friction based on the car's turning velocity to avoid slipping
	wfc.extremumValue = Mathf.Clamp(300 - sqrVel, 0, 300);
	wfc.asymptoteValue = Mathf.Clamp(150 - (sqrVel / 2), 0, 150);
		
	for(var w : Wheel in wheels)
	{
		w.collider.sidewaysFriction = wfc;
		w.collider.forwardFriction = wfc;
	}
}

function CalculateEnginePower(relativeVelocity : Vector3)
{
	if(throttle == 0)
	{
		currentEnginePower -= Time.deltaTime * 200;
	}
	else if( HaveTheSameSign(relativeVelocity.z, throttle) )
	{
		normPower = (currentEnginePower / engineForceValues[engineForceValues.Length - 1]) * 2;
		currentEnginePower += Time.deltaTime * 200 * EvaluateNormPower(normPower);
	}
	else
	{
		currentEnginePower -= Time.deltaTime * 300;
	}
	
	if(currentGear == 0)
		currentEnginePower = Mathf.Clamp(currentEnginePower, 0, engineForceValues[0]);
	else
		currentEnginePower = Mathf.Clamp(currentEnginePower, engineForceValues[currentGear - 1], engineForceValues[currentGear]);
}

function CalculateState()
{
	canDrive = false;
	canSteer = false;
	
	for(var w : Wheel in wheels)
	{
		if(w.collider.isGrounded)
		{
			if(w.steerWheel)
				canSteer = true;
			if(w.driveWheel)
				canDrive = true;
		}
	}
}

function ApplyThrottle(canDrive : boolean, relativeVelocity : Vector3)
{
	
	if(true)
	{
	
		var throttleForce : float = 0;
		var brakeForce : float = 0;
		
		if (HaveTheSameSign(relativeVelocity.z, throttle))
		{
			if (!handbrake)
				throttleForce = Mathf.Sign(throttle) * currentEnginePower * GetComponent.<Rigidbody>().mass;
		}
		else
			brakeForce = Mathf.Sign(throttle) * engineForceValues[0] * GetComponent.<Rigidbody>().mass;
		//Debug.Log(transform.forward * Time.deltaTime * (throttleForce + brakeForce));
		GetComponent.<Rigidbody>().AddForce(transform.forward * Time.deltaTime * (throttleForce + brakeForce));
	}
}

function ApplySteering(canSteer : boolean, relativeVelocity : Vector3)
{
	if(true)
	{
		var turnRadius : float = 3.0 / Mathf.Sin((90 - (steer * 30)) * Mathf.Deg2Rad);
		var minMaxTurn : float = EvaluateSpeedToTurn(GetComponent.<Rigidbody>().velocity.magnitude);
		var turnSpeed : float = Mathf.Clamp(relativeVelocity.z / turnRadius, -minMaxTurn / 10, minMaxTurn / 10);
		
		transform.RotateAround(	transform.position + transform.right * turnRadius * steer, 
								transform.up, 
								turnSpeed * Mathf.Rad2Deg * Time.deltaTime * steer);
		
		var debugStartPoint = transform.position + transform.right * turnRadius * steer;
		var debugEndPoint = debugStartPoint + Vector3.up * 5;
		
		//Debug.DrawLine(debugStartPoint, debugEndPoint, Color.red);
		
		if(initialDragMultiplierX > dragMultiplier.x) // Handbrake
		{
			var rotationDirection : float = Mathf.Sign(steer); // rotationDirection is -1 or 1 by default, depending on steering
			if(steer == 0)
			{
				if(GetComponent.<Rigidbody>().angularVelocity.y < 1) // If we are not steering and we are handbraking and not rotating fast, we apply a random rotationDirection
					rotationDirection = Random.Range(-1.0, 1.0);
				else
					rotationDirection = GetComponent.<Rigidbody>().angularVelocity.y; // If we are rotating fast we are applying that rotation to the car
			}
			// -- Finally we apply this rotation around a point between the cars front wheels.
			transform.RotateAround( transform.TransformPoint( (	frontWheels[0].localPosition + frontWheels[1].localPosition) * 0.5), 
																transform.up, 
																GetComponent.<Rigidbody>().velocity.magnitude * Mathf.Clamp01(1 - GetComponent.<Rigidbody>().velocity.magnitude / topSpeed) * rotationDirection * Time.deltaTime * 2);
		}
	}
}

/**************************************************/
/*               Utility Functions                */
/**************************************************/

function Convert_Miles_Per_Hour_To_Meters_Per_Second(value : float) : float
{
	return value * 0.44704;
}

function Convert_Meters_Per_Second_To_Miles_Per_Hour(value : float) : float
{
	return value * 2.23693629;	
}

function HaveTheSameSign(first : float, second : float) : boolean
{
	if (Mathf.Sign(first) == Mathf.Sign(second))
		return true;
	else
		return false;
}

function EvaluateSpeedToTurn(speed : float)
{
	if(speed > topSpeed / 2)
		return minimumTurn;
	
	var speedIndex : float = 1 - (speed / (topSpeed / 2));
	return minimumTurn + speedIndex * (maximumTurn - minimumTurn);
}

function EvaluateNormPower(normPower : float)
{
	if(normPower < 1)
		return 10 - normPower * 9;
	else
		return 1.9 - normPower * 0.9;
}

function GetGearState()
{
	var relativeVelocity : Vector3 = transform.InverseTransformDirection(GetComponent.<Rigidbody>().velocity);
	var lowLimit : float = (currentGear == 0 ? 0 : gearSpeeds[currentGear-1]);
	return (relativeVelocity.z - lowLimit) / (gearSpeeds[currentGear - lowLimit]) * (1 - currentGear * 0.1) + currentGear * 0.1;
}




































///////////////SEMI-DUPLICATED CODE

/*

function createLaserSensor(numberOfBeams:int,degrees:float,dist:float,relPos:Vector3,direction:Vector3){
	var forward=transform.TransformDirection(Vector3.forward);
	newSensor = Instantiate (basicLaserSensor, transform.position+relPos , transform.rotation);
	newSensor.transform.Rotate(direction);
	var lsscript=newSensor.GetComponent("LaserSensor");
	lsscript.setPrams(numberOfBeams,degrees,dist);
	newSensor.transform.parent = transform; 
}

function createLaserSensorByAngle(numberOfBeams:int,degrees:float,dist:float,angles:Vector2,direction:Vector3){//0 for laser,  for cone
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
				var newSensorAdd = Instantiate (basicLaserSensor, hit.point+backDir*0.1 , transform.rotation);
				newSensorAdd.transform.Rotate(direction);
				var lsscript=newSensorAdd.GetComponent("LaserSensor");
				lsscript.setPrams(numberOfBeams,degrees,dist);
				newSensorAdd.transform.parent = transform; 
				Debug.DrawLine(pointOnSphere-translation,hit.point-translation,Color.green,0);
				laserSensorList.Add(newSensorAdd);
			
		}
	}else{
	Debug.Log("Couldn't place sensor");
	//Debug.DrawLine(pointOnSphere-translation,targetPoint-translation,Color.green,0);
	}
	
	
		
	
	transform.position=currentp;
	//rigidbody.isKinematic=false;
	//Debug.Log(pointOnSphere);
	//////
	
}
*/

/*function createConeSensorByAngle(dist:float,theta:float,angles:Vector2,direction:Vector3){
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
				var lsscript=newSensorAdd.GetComponent("coneSensor");
				lsscript.setPrams(theta,dist);
				newSensorAdd.transform.parent = transform; 
				//Debug.DrawLine(pointOnSphere-translation,hit.point-translation,Color.green,100000);
				coneSensorList.Add(newSensorAdd);
			
		}
	}else{
	Debug.Log("Couldn't place sensor");
	//Debug.DrawLine(pointOnSphere-translation,targetPoint-translation,Color.green,0);
	}
	
	
		
	
	transform.position=currentp;
	//rigidbody.isKinematic=false;
	//Debug.Log(pointOnSphere);

	//////
	
}*/











