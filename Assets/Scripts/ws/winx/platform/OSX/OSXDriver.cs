//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17929
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using System;
using ws.winx.devices;
//using UnityEngine;


namespace ws.winx.platform.osx
{

	using Carbon;
	using CFAllocatorRef = System.IntPtr;
	using CFDictionaryRef = System.IntPtr;
	using CFArrayRef = System.IntPtr;
	using CFIndex = System.IntPtr;
	using CFRunLoop = System.IntPtr;
	using CFString = System.IntPtr;
	using CFStringRef = System.IntPtr; // Here used interchangeably with the CFString
	using CFTypeRef = System.IntPtr;
	using IOHIDDeviceRef = System.IntPtr;
	using IOHIDElementRef = System.IntPtr;
	using IOHIDManagerRef = System.IntPtr;
	using IOHIDValueRef = System.IntPtr;
	using IOOptionBits = System.IntPtr;
	using IOReturn = System.IntPtr;
	using NativeMethods=OSXHIDInterface.NativeMethods;
	using IOHIDElementType=OSXHIDInterface.IOHIDElementType;
	using HIDUsageGD=OSXHIDInterface.HIDUsageGD;
	using HIDPage=OSXHIDInterface.HIDPage;




	sealed class OSXDriver: IJoystickDriver
		{

#region Fields

		readonly CFRunLoop RunLoop = CF.CFRunLoopGetMain();
		readonly CFString InputLoopMode = CF.RunLoopModeDefault;
		NativeMethods.IOHIDValueCallback HandleDeviceValueReceived;
		IHIDInterface _hidInterface;
		
	
		
        #endregion




#region Constructor
        public OSXDriver()
				{
				
					HandleDeviceValueReceived = DeviceValueReceived;

				}
        #endregion


#region Private Members




        /// <summary>
		/// Devices the value received.
		/// </summary>
		/// <param name="context">Context.</param>
		/// <param name="res">Res.</param>
		/// <param name="sender">Sender.</param>
		/// <param name="valRef">Value reference.</param>
		void DeviceValueReceived(IntPtr context, IOReturn res, IntPtr sender, IOHIDValueRef valRef)
		{
			IOHIDElementRef element = NativeMethods.IOHIDValueGetElement(valRef);
			uint uid=NativeMethods.IOHIDElementGetCookie(element);
			int value;
			OSXHIDInterface.IOHIDElementType  type = NativeMethods.IOHIDElementGetType(element);

			IDevice stick=_hidInterface.Devices[context];

			if (NativeMethods.IOHIDValueGetLength(valRef) > 4) {
				// Workaround for a strange crash that occurs with PS3 controller; was getting lengths of 39 (!)
				return;
			}

			value=NativeMethods.IOHIDValueGetIntegerValue(valRef);

			//AXIS
			if(type==OSXHIDInterface.IOHIDElementType.kIOHIDElementTypeInput_Misc
			   || type==OSXHIDInterface.IOHIDElementType.kIOHIDElementTypeInput_Axis)
			{
				int numAxes=stick.Axis.Count;
				AxisDetails axisDetails;






				for (int axisIndex = 0; axisIndex < numAxes; axisIndex++) {

					axisDetails=stick.Axis[axisIndex] as AxisDetails;

					if ( axisDetails.uid== uid) {
						//check hatch
						//Check if POV element.
						if((NativeMethods.IOHIDElementGetUsage(element) & (uint)OSXHIDInterface.HIDUsageGD.Hatswitch)!=0)
						{
									//Workaround for POV hat switches that do not have null states.
									if(!axisDetails.isNullable)
									{
										value = value < axisDetails.min ? axisDetails.max - axisDetails.min + 1 : value - 1;
									}


							int outX;
							int outY;

							hatValueToXY(value,axisDetails.max - axisDetails.min,out outX,out outY);

							stick.Axis[JoystickAxis.AxisPovX].value=outX;
							stick.Axis[JoystickAxis.AxisPovY].value=outY;

									
						
						}else{
							//Sanity check.
							if(value < axisDetails.min)
							{
								value = axisDetails.min;
							}
							if(value > axisDetails.max)
							{
								value = axisDetails.max;
							}

							//Calculate the -1 to 1 float from the min and max possible values.
							axisDetails.value=(value - axisDetails.min) / (float)(axisDetails.max - axisDetails.min) * 2.0f - 1.0f;

						}



						return;
					}

				}//end for

			//BUTTONS
			}else if(type==OSXHIDInterface.IOHIDElementType.kIOHIDElementTypeInput_Button){

				int numButtons=stick.Buttons.Count;

				for (int buttonIndex = 0; buttonIndex < numButtons; buttonIndex++) {
					if ( stick.Buttons[buttonIndex].uid== uid) {

						stick.Buttons[buttonIndex].value=value;


						
						return;
					}
				}
			}

		}



//					   0                 
//					   |
//				4______|______1
//					   |
//					   |
//					   2



		//				  7    0     1            
		//				   \   |   /
		//				6 _____|______2
		// 					  /|\
		//					/  |  \
		//				   5   4    3


		/// <summary>
		/// Hats the value to X.
		/// </summary>
		/// <param name="value">Value.</param>
		/// <param name="range">Range.</param>
		/// <param name="outX">Out x.</param>
		/// <param name="outY">Out y.</param>
		 void hatValueToXY(int value, int range,out int outX,out int outY) {

				outX = outY = 0;
				int rangeHalf=range>>1;
				int rangeQuat=range>>2;
				
				if (value > 0 && value < rangeHalf) {
					outX = 1;
					
				} else if (value > range / 2) {
					outX = -1;					
				} 
				
				if (value > rangeQuat * 3 || value < rangeQuat) {
					outY = 1;
					
				} else if (value > rangeQuat && value < rangeQuat * 3) {
					outY = 1;					
				} 

		}




        #endregion








#region IJoystickDriver implementation

         public void Update(IDevice joystick)
		//public void Update (IDevice<IAxisDetails, IButtonDetails, IDeviceExtension> joystick)
		{
            throw new Exception("OSX Default driver is meant to auto update on callback");
		}

		public IDevice ResolveDevice(IHIDDeviceInfo info)
		//public IDevice<IAxisDetails,IButtonDetails,IDeviceExtension> ResolveDevice(IHIDDeviceInfo info)
		{
			_hidInterface=info.hidInterface;

			// The device is not normally available in the InputValueCallback (HandleDeviceValueReceived), so we include
			// the device identifier as the context variable, so we can identify it and figure out the device later.
			OSXHIDInterface.NativeMethods.IOHIDDeviceRegisterInputValueCallback(info.deviceHandle,HandleDeviceValueReceived, info.deviceHandle);


			OSXHIDInterface.NativeMethods.IOHIDDeviceScheduleWithRunLoop(info.deviceHandle, RunLoop, InputLoopMode);

			return CreateDevice(info);
		}




		//public IDevice<IAxisDetails,IButtonDetails,IDeviceExtension> CreateDevice(IHIDDeviceInfo info){
		public IDevice CreateDevice(IHIDDeviceInfo info){

			IntPtr device=info.deviceHandle;
			//JoystickDevice<IAxisDetails,IButtonDetails,IDeviceExtension> joystick;
			JoystickDevice joystick;
			int axisIndex=0;
			int buttonIndex=0;

			CFArray elements=new CFArray();
			IOHIDElementRef element;
			IOHIDElementType type;

			//copy all matched 
			elements.Ref=NativeMethods.IOHIDDeviceCopyMatchingElements(device, IntPtr.Zero,IOOptionBits.Zero );
			
			int numButtons=0;
			int numAxis=0;
			int numElements=elements.Count;
			
							for (int elementIndex = 0; elementIndex < numElements; elementIndex++){
								element = (IOHIDElementRef) elements[elementIndex];
								type = NativeMethods.IOHIDElementGetType(element);
			
			
								// All of the axis elements I've ever detected have been kIOHIDElementTypeInput_Misc. kIOHIDElementTypeInput_Axis is only included for good faith...
								if (type == IOHIDElementType.kIOHIDElementTypeInput_Misc ||
								    type == IOHIDElementType.kIOHIDElementTypeInput_Axis) {
									numAxis++;
									
								} else if (type == IOHIDElementType.kIOHIDElementTypeInput_Button) {
									numButtons++;
								}
			
						}


			joystick=new JoystickDevice(info.index,info.PID,info.PID,numAxis,numButtons,this);
			
			AxisDetails axisDetails;

			for (int elementIndex = 0; elementIndex < elements.Count; elementIndex++){
				element = (IOHIDElementRef) elements[elementIndex];
				type = NativeMethods.IOHIDElementGetType(element);
				
				

				
				// All of the axis elements I've ever detected have been kIOHIDElementTypeInput_Misc. kIOHIDElementTypeInput_Axis is only included for good faith...
				if (type == IOHIDElementType.kIOHIDElementTypeInput_Misc ||
				    type == IOHIDElementType.kIOHIDElementTypeInput_Axis) {
					
					
					axisDetails=new AxisDetails();
					axisDetails.uid=NativeMethods.IOHIDElementGetCookie(element);
					axisDetails.min=NativeMethods.IOHIDElementGetLogicalMin(element);
					axisDetails.max=NativeMethods.IOHIDElementGetLogicalMax(element);
					axisDetails.isNullable=NativeMethods.IOHIDElementHasNullState(element);
					axisDetails.isHat=false;
					
					
					if(NativeMethods.IOHIDElementGetUsage(element)==(uint)HIDUsageGD.Hatswitch){
						
						
						axisDetails.isHat=true;
						
						//if prevous axis was Hat =>X next is Y
						if(NativeMethods.IOHIDElementGetUsage((IOHIDElementRef) elements[elementIndex-1])==(uint)HIDUsageGD.Hatswitch){
							
							
							joystick.Axis[JoystickAxis.AxisPovY]=axisDetails;
							joystick.numPOV++;
							
						}else{
							joystick.Axis[JoystickAxis.AxisPovX]=axisDetails;
						}
						
						
					}else{
						
						
						
						joystick.Axis[(JoystickAxis)axisIndex]=axisDetails;
						
					}
					axisIndex++;
					
					
				} else if (type == IOHIDElementType.kIOHIDElementTypeInput_Button) {
					
					joystick.Buttons[buttonIndex]=new ButtonDetails(NativeMethods.IOHIDElementGetCookie(element));  
					buttonIndex++;
					
				}
				
			}









						joystick.Extension=new OSXDefaultExtension();
//			JoystickDevice<AxisDetails,ButtonDetails,OSXDefaultExtension> joystick;
//			joystick=new JoystickDevice<AxisDetails,ButtonDetails,OSXDefaultExtension>(id,axes,buttons);
//			joystick.Extension=new OSXDefaultExtension();







             return joystick;
			//return (IDevice<IAxisDetails,IButtonDetails,IDeviceExtension>)joystick;
			//return joystick as IDevice<AxisDetails,ButtonDetails,OSXDefaultExtension>;
		}
		


#region ButtonDetails
		public sealed class ButtonDetails:IButtonDetails{
			
#region Fields
			
			float _value;
			uint _uid;
			JoystickButtonState _buttonState;

#region IDeviceDetails implementation


			public uint uid {
				get {
					return _uid;
				}
				set {
					_uid=value;
				}
			}




			public JoystickButtonState buttonState{
				get{return _buttonState; }
			}



			public float value{
				get{
					return _value;
					//return (_buttonState==JoystickButtonState.Hold || _buttonState==JoystickButtonState.Down);
				}
				set{

					_value=value;
					//if pressed==TRUE
					//TODO check the code with triggers
					if (value>0) {
						if (_buttonState == JoystickButtonState.None 
						    || _buttonState == JoystickButtonState.Up) {
							
								_buttonState = JoystickButtonState.Down;
							
							
							
						} else {
							//if (buttonState == JoystickButtonState.Down)
							 _buttonState = JoystickButtonState.Hold;
							
						}
						
						
					} else { //
						if (_buttonState == JoystickButtonState.Down
						    || _buttonState == JoystickButtonState.Hold) {
							_buttonState = JoystickButtonState.Up;
						} else {//if(buttonState==JoystickButtonState.Up){
							_buttonState = JoystickButtonState.None; 
						}
						
					}
				}
			}
            #endregion
            #endregion

#region Constructor
			public ButtonDetails(uint uid=0){this.uid=uid; }
            #endregion
			
			
			
			
			
			
		}
		
        #endregion
		
#region AxisDetails
		public sealed class AxisDetails:IAxisDetails{

#region Fields
			float _value;
			int _uid;
			int _min;
			int _max;
			JoystickButtonState _buttonState;
			bool _isNullable;
			bool _isHat;

#region IAxisDetails implementation



			public bool isTrigger {
				get {
					throw new NotImplementedException ();
				}
				set {
					throw new NotImplementedException ();
				}
			}




			public int min {
				get {
					return _min;
				}
				set {
					_min=value;
				}
			}


			public int max {
				get {
					return _max;
				}
				set {
					_max=value;
				}
			}


			public bool isNullable {
				get {
					return _isNullable;
				}
				set {
					_isNullable=value;
				}
			}


			public bool isHat {
				get {
					return _isHat;
				}
				set {
					_isHat=value;
				}
			}


            #endregion


#region IDeviceDetails implementation


			public uint uid {
				get {
					throw new NotImplementedException ();
				}
				set {
					throw new NotImplementedException ();
				}
			}


            #endregion

			public JoystickButtonState buttonState{
				get{return _buttonState; }
			}
			public float value{
				get{ return _value;}
				set{ 
					
					if (value == 0) {
						if (_buttonState == JoystickButtonState.Down
						    || _buttonState == JoystickButtonState.Hold){
							
							//axis float value isn't yet update so it have value before getting 0
							if (_value > 0)//0 come after positive values
								_buttonState = JoystickButtonState.PosToUp;
							else
								_buttonState = JoystickButtonState.NegToUp;
							
						}else {//if(buttonState==JoystickButtonState.Up){
							_buttonState = JoystickButtonState.None; 
						}
						
						
					} else { 
						if (_buttonState == JoystickButtonState.None 
						    || _buttonState == JoystickButtonState.Up) {
							
							_buttonState = JoystickButtonState.Down;
							
						} else {
							_buttonState = JoystickButtonState.Hold;
						}
						
						
					}
					
					_value=value;
					
					
					
				}//set
			}

            #endregion
			
		}
		
        #endregion







		public sealed class OSXDefaultExtension:IDeviceExtension{
		}




		}
}

        #endregion
#endif