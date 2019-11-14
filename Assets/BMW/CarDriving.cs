using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Assets.Car
{

	internal enum CarDriveType
	{
		FrontWheelDrive,
		RearWheelDrive,
		FourWheelDrive
	}

	public class CarDriving : MonoBehaviour
	{
		[SerializeField] private CarDriveType m_CarDriveType = CarDriveType.FourWheelDrive;
		[SerializeField] private Vector3 m_CentreOfMassOffset;
		[SerializeField] private float m_MaximumSteerAngle;
		[SerializeField] private float m_BrakeTorque;
		[SerializeField] private float m_Topspeed = 200;
		[SerializeField] private static int NoOfGears = 6;
		[SerializeField] private float m_RevRangeBoundary = 1f;
		[SerializeField] private float m_SlipLimit;
		[SerializeField] private float m_MaxHandbrakeTorque;
		[SerializeField] private float m_ReverseTorque;
		[SerializeField] private float m_FullTorqueOverAllWheels;

		[Range (0, 1)] [SerializeField] private float m_SteerHelper;
		// 0 is raw physics , 1 the car will grip in the direction it is facing
		[Range (0, 1)] [SerializeField] private float m_TractionControl;
		// 0 is no traction control, 1 is full interference

		[SerializeField] private float m_Downforce = 100f;


        private float m_CurrentTorque;
		private float m_SteerAngle;
		private Rigidbody m_Rigidbody;
		private int m_GearNum;
		private float m_GearFactor;
		private float m_OldRotation;
        private Boolean overrideControl = false;

		public WheelCollider WheelColFR;
		public WheelCollider WheelColFL;
		public WheelCollider WheelColRR;
		public WheelCollider WheelColRL;

		public bool Skidding { get; private set; }

		public float BrakeInput { get; private set; }

		public float CurrentSteerAngle{ get { return m_SteerAngle; } }

		public float CurrentSpeed{ get { return m_Rigidbody.velocity.magnitude * 2.23693629f; } }

		public float MaxSpeed{ get { return m_Topspeed; } }

		public float Revs { get; private set; }

		public float AccelInput { get; private set; }

		private void GearChanging ()
		{
			float f = Mathf.Abs (CurrentSpeed / MaxSpeed);
			float upgearlimit = (1 / (float)NoOfGears) * (m_GearNum + 1);
			float downgearlimit = (1 / (float)NoOfGears) * m_GearNum;

			if (m_GearNum > 0 && f < downgearlimit) {
				m_GearNum--;
			}

			if (f > upgearlimit && (m_GearNum < (NoOfGears - 1))) {
				m_GearNum++;
			}
		}

		// simple function to add a curved bias towards 1 for a value in the 0-1 range
		private static float CurveFactor (float factor)
		{
			return 1 - (1 - factor) * (1 - factor);
		}


		// unclamped version of Lerp, to allow value to exceed the from-to range
		private static float ULerp (float from, float to, float value)
		{
			return (1.0f - value) * from + value * to;
		}


		private void CalculateGearFactor ()
		{
			float f = (1 / (float)NoOfGears);
			// gear factor is a normalised representation of the current speed within the current gear's range of speeds.
			// We smooth towards the 'target' gear factor, so that revs don't instantly snap up or down when changing gear.
			var targetGearFactor = Mathf.InverseLerp (f * m_GearNum, f * (m_GearNum + 1), Mathf.Abs (CurrentSpeed / MaxSpeed));
			m_GearFactor = Mathf.Lerp (m_GearFactor, targetGearFactor, Time.deltaTime * 5f);
		}

		private void CalculateRevs ()
		{
			// calculate engine revs (for display / sound)
			// (this is done in retrospect - revs are not used in force/power calculations)
			CalculateGearFactor ();
			var gearNumFactor = m_GearNum / (float)NoOfGears;
			var revsRangeMin = ULerp (0f, m_RevRangeBoundary, CurveFactor (gearNumFactor));
			var revsRangeMax = ULerp (m_RevRangeBoundary, 1f, gearNumFactor);
			Revs = ULerp (revsRangeMin, revsRangeMax, m_GearFactor);
		}

        public void OverrideSteering(float steering)
        {
            overrideControl = true;
            Steer(steering);
        }

        public void OverrideBraking(float brake)
        {
            overrideControl = true;
            DetermineAccelerationAndBraking(0, brake);
        }

        public void ReleaseOverride()
        {
            if(overrideControl)
                overrideControl = false;
        }

        private void Steer(float steering)
        {
            steering = Mathf.Clamp(steering, -1, 1);

            //Set the steer on the front wheels.
            //Assuming that wheels 0 and 1 are the front wheels.
            m_SteerAngle = steering * m_MaximumSteerAngle;
            WheelColFL.steerAngle = m_SteerAngle;
            WheelColFR.steerAngle = m_SteerAngle;

            SteerHelper();
        }

        private void DetermineAccelerationAndBraking(float acceleration, float footbrake)
        {
            if (overrideControl)
                acceleration = 0;
 
            ApplyDrive(acceleration, footbrake);
        }

        public void Move (float steering, float accel, float footbrake, float handbrake)
		{
			//clamp input values
			AccelInput = accel = Mathf.Clamp (accel, 0, 1);
			BrakeInput = footbrake = -1 * Mathf.Clamp (footbrake, -1, 0);
			handbrake = Mathf.Clamp (handbrake, 0, 1);

            if (!overrideControl)
                Steer(steering);
            
            DetermineAccelerationAndBraking(accel, footbrake);
			CapSpeed ();

			//Set the handbrake.
			if (handbrake > 0f) {
				var hbTorque = handbrake * m_MaxHandbrakeTorque;
				WheelColRL.brakeTorque = hbTorque;
				WheelColRR.brakeTorque = hbTorque;
			}


			CalculateRevs ();
			GearChanging ();

			AddDownForce ();
			TractionControl ();
		}


		private void CapSpeed ()
		{
			float speed = m_Rigidbody.velocity.magnitude;
			speed *= 3.6f;
			if (speed > m_Topspeed)
				m_Rigidbody.velocity = (m_Topspeed / 3.6f) * m_Rigidbody.velocity.normalized;

//			Debug.Log ("Current Speed: " + speed);
		}

		private void ApplyDrive (float accel, float footbrake)
		{
			List<WheelCollider> affectedWheels = new List<WheelCollider>();

			switch (m_CarDriveType) {
			case CarDriveType.FourWheelDrive:
				affectedWheels.Add (WheelColFL);
				affectedWheels.Add (WheelColFR);
				affectedWheels.Add (WheelColRL);
				affectedWheels.Add (WheelColRR);
				break;

			case CarDriveType.FrontWheelDrive:
				affectedWheels.Add (WheelColFL);
				affectedWheels.Add (WheelColFR);
				break;
				
			case CarDriveType.RearWheelDrive:
				affectedWheels.Add (WheelColRL);
				affectedWheels.Add (WheelColRR);
				break;
			}

			float thrustTorque = accel * (m_CurrentTorque / 4f);
			foreach (WheelCollider currentWheel in affectedWheels) {
				currentWheel.motorTorque = thrustTorque;
			}

			if (CurrentSpeed > 5 && Vector3.Angle (transform.forward, m_Rigidbody.velocity) < 50f) {
				WheelColRL.brakeTorque = WheelColRR.brakeTorque =
					WheelColFL.brakeTorque = WheelColFR.brakeTorque = m_BrakeTorque * footbrake;
			} else if (footbrake > 0) {
				WheelColRL.brakeTorque = WheelColRR.brakeTorque =
				WheelColFL.brakeTorque = WheelColFR.brakeTorque = 0f;

				float reverseTorque = -m_ReverseTorque * footbrake;
				foreach (WheelCollider currentWheel in affectedWheels) {
					currentWheel.motorTorque = reverseTorque;
				}
			}
		}


		private void SteerHelper ()
		{
			// this if is needed to avoid gimbal lock problems that will make the car suddenly shift direction
			if (Mathf.Abs (m_OldRotation - transform.eulerAngles.y) < 10f) {
				var turnadjust = (transform.eulerAngles.y - m_OldRotation) * m_SteerHelper;
				Quaternion velRotation = Quaternion.AngleAxis (turnadjust, Vector3.up);
				m_Rigidbody.velocity = velRotation * m_Rigidbody.velocity;
			}
			m_OldRotation = transform.eulerAngles.y;
		}

		// this is used to add more grip in relation to speed
		private void AddDownForce ()
		{
			WheelColFR.attachedRigidbody.AddForce (-transform.up * m_Downforce *
			WheelColFR.attachedRigidbody.velocity.magnitude);
		}

		// crude traction control that reduces the power to wheel if the car is wheel spinning too much
		private void TractionControl ()
		{
			WheelHit wheelHit;
			switch (m_CarDriveType) {
			case CarDriveType.FourWheelDrive:
				WheelColFL.GetGroundHit (out wheelHit);
				AdjustTorque (wheelHit.forwardSlip);

				WheelColFR.GetGroundHit (out wheelHit);
				AdjustTorque (wheelHit.forwardSlip);

				WheelColRL.GetGroundHit (out wheelHit);
				AdjustTorque (wheelHit.forwardSlip);

				WheelColRR.GetGroundHit (out wheelHit);
				AdjustTorque (wheelHit.forwardSlip);

				break;

			case CarDriveType.RearWheelDrive:
				WheelColRL.GetGroundHit (out wheelHit);
				AdjustTorque (wheelHit.forwardSlip);

				WheelColRR.GetGroundHit (out wheelHit);
				AdjustTorque (wheelHit.forwardSlip);
				break;

			case CarDriveType.FrontWheelDrive:
				WheelColFL.GetGroundHit (out wheelHit);
				AdjustTorque (wheelHit.forwardSlip);

				WheelColFR.GetGroundHit (out wheelHit);
				AdjustTorque (wheelHit.forwardSlip);
				break;
			}
		}


		private void AdjustTorque (float forwardSlip)
		{
			if (forwardSlip >= m_SlipLimit && m_CurrentTorque >= 0) {
				m_CurrentTorque -= 10 * m_TractionControl;
			} else {
				m_CurrentTorque += 10 * m_TractionControl;
				if (m_CurrentTorque > m_FullTorqueOverAllWheels) {
					m_CurrentTorque = m_FullTorqueOverAllWheels;
				}
			}
		}
	
		//code to prevent car from veering
		[HideInInspector] public Vector3 _currentRotation;
		private Boolean _colliding;
		private Boolean _initilization = true;

		void Awake()
		{
			m_Rigidbody = GetComponent<Rigidbody> ();
			m_MaxHandbrakeTorque = float.MaxValue;
			m_CurrentTorque = m_FullTorqueOverAllWheels - (m_TractionControl * m_FullTorqueOverAllWheels);

			// _currentRotation is defined elsewhere in the class
			_currentRotation = new Vector3 (transform.localEulerAngles.x,
			                                transform.localEulerAngles.y, transform.localEulerAngles.z);
		}

		void OnCollisionEnter(Collision collision)
		{
			_colliding = true;
		}

		void OnCollisionExit(Collision collision)
		{
			_colliding = false;
		}
		
		void LateUpdate()
		{
			// whenever the car is intended to be going straight we adjust localEulerAngle; if the user steers we do not interfere but adjust the rotation angle to allow for correct further calculation
			
			// _steer is defined elsewhere and
			// is taken from the user in FixedUpdate()
			// as follows: _steer = Input.GetAxis("Horizontal"); 
			if (m_SteerAngle == 0 || _colliding || _initilization)
			{
			    float y = _currentRotation.y;
			    if (transform != null)
			        transform.localEulerAngles = new Vector3 (transform.localEulerAngles.x, y, transform.localEulerAngles.z);
				_initilization = false;
			}
			else
				_currentRotation.y = transform.localEulerAngles.y;
		}
	}
}