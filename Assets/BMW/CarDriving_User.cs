using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Assets.Car{
	
	[RequireComponent(typeof (CarDriving))]
	public class CarDriving_User : MonoBehaviour
	{
		private CarDriving m_Car; // the car controller we want to use


		private void Awake()
		{
			// get the car controller
			m_Car = GetComponent<CarDriving>();
		}


		private void FixedUpdate()
		{
			// pass the input to the car!
			float h = CrossPlatformInputManager.GetAxis("Horizontal");
			float v = CrossPlatformInputManager.GetAxis("Vertical");
			#if !MOBILE_INPUT
			float handbrake = CrossPlatformInputManager.GetAxis("Jump");
            //m_Car.Move(h, v, v, handbrake);


            if (h != 0f)
                m_Car.OverrideSteering(h);

            if (v < 0f)
                m_Car.OverrideBraking(v);

            if (v == 0f && h == 0f)
                m_Car.ReleaseOverride();
#else
			m_Car.Move(h, v, v, 0f);
#endif
        }
	}
}
