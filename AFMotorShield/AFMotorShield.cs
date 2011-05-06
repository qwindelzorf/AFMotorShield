using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System.Threading;

//#define MICROSTEPS_8

namespace AFMotorShield
{ 
    /// <summary>
    /// A class containing all of the pin definitions for the Adafruit Motor Shield
    /// </summary>
    public abstract class MotorShield
    {
        private readonly OutputPort motorLatch = new OutputPort(Pins.GPIO_PIN_D12, false);
        private readonly OutputPort motorClock = new OutputPort(Pins.GPIO_PIN_D4, false);
        private readonly OutputPort motorEnable = new OutputPort(Pins.GPIO_PIN_D7, false);
        private readonly OutputPort motorData = new OutputPort(Pins.GPIO_PIN_D8, false);

        /// <summary>
        /// Bits indicating where motor enable lines are connected on the latch
        /// </summary>
        internal enum MotorBits
        {
            Motor1_A = 2,
            Motor1_B = 3,
            Motor2_A = 1,
            Motor2_B = 4,
            Motor3_A = 5,
            Motor3_B = 6,
            Motor4_A = 0,
            Motor4_B = 7,
        }

        /// <summary>
        /// Connections for the PWM pins used by the motor shield
        /// </summary>
        internal class PwmPins
        {
            public static Cpu.Pin pwm0A = Pins.GPIO_PIN_D6;   // M4
            public static Cpu.Pin pwm0B = Pins.GPIO_PIN_D5;   // M3
            public static Cpu.Pin pwm1A = Pins.GPIO_PIN_D9;   // Servo 2
            public static Cpu.Pin pwm1B = Pins.GPIO_PIN_D10;  // Servo 1
            //public static Cpu.Pin pwm2A = Pins.GPIO_PIN_D11;  // M1
            //public static Cpu.Pin pwm2B = Pins.GPIO_PIN_D3;   // M2
        }

        internal byte latchState = 0;

        /// <summary>
        /// Sets the state of the latch on the motor shield
        /// </summary>
        /// <param name="latchState">A byte representing the new pin state on the latch</param>
        internal void latch_tx(byte latchState) 
        {
            //LATCH_PORT &= ~_BV(LATCH);
            motorLatch.Write(false);

            //SER_PORT &= ~_BV(SER);
            motorData.Write(false);

            for (int i=0; i<8; i++) 
            {
                //CLK_PORT &= ~_BV(CLK);
                motorClock.Write(false);

                int mask = (1 << (7 - i));
                if ((latchState & mask) != 0) 
                {
                    //SER_PORT |= _BV(SER);
                    motorData.Write(true);
                } 
                else 
                {
                    //SER_PORT &= ~_BV(SER);
                    motorData.Write(false);
                }
                //CLK_PORT |= _BV(CLK);
                motorClock.Write(true);
            }
            //LATCH_PORT |= _BV(LATCH);
            motorLatch.Write(true);
        }
    }

    /// <summary>
    /// Headers for the motors on the AdaFruit Motor Shield
    /// </summary>
    public enum MotorHeaders
    {
        //M1,
        //M2,
        M3,
        M4
    }

    public class DCMotor : MotorShield
    {
        private static PWM pwm;
        private static byte motorBitA, motorBitB;

        public enum MotorDirection
        {
            Release,
            Forward,
            Reverse,
        }

        /// <summary>
        /// A DC Motor controller
        /// </summary>
        /// <param name="header">The header to which the motor is connected</param>
        /// <param name="frequency">The PWM frequency (in Hz) at which to drive the motor. Defaults to 10kHz.</param>
        public DCMotor(MotorHeaders header, uint frequency = 10000)
        {
            switch (header)
            {
                /*
                case MotorHeaders.M1:
                    motorBitA = (int)MotorBits.Motor1_A;
                    motorBitB = (int)MotorBits.Motor1_B;
                    pwm = new PWM(PwmPins.pwm2A);
                    break;
                case MotorHeaders.M2:
                    motorBitA = (int)MotorBits.Motor2_A;
                    motorBitB = (int)MotorBits.Motor2_B;
                    pwm = new PWM(PwmPins.pwm2B);
                    break;
                */
                case MotorHeaders.M3:
                    motorBitA = (int)MotorBits.Motor3_A;
                    motorBitB = (int)MotorBits.Motor3_B;
                    pwm = new PWM(PwmPins.pwm0B);
                    break;
                case MotorHeaders.M4:
                    motorBitA = (int)MotorBits.Motor4_A;
                    motorBitB = (int)MotorBits.Motor4_B;
                    pwm = new PWM(PwmPins.pwm0A);
                    break;
                default:
                    throw new InvalidOperationException("Invalid motor header specified");
            }

            latchState &= (byte)(~(1 << motorBitA) & ~(1 << motorBitB)); 
            latch_tx(latchState); // Set both motor pins low

            pwm.SetPulse(1000000/frequency, 0); // Set PWM frequency, but 0% duty cycle
        }

        /// <summary>
        /// Set the motor direction
        /// </summary>
        /// <param name="dir">Direction the motor should run</param>
        public void Run(MotorDirection dir)
        {            
            switch (dir)
            {
                case MotorDirection.Release:
                    latchState &= (byte)(~(1 << motorBitA));
                    latchState &= (byte)(~(1 << motorBitB));
                    break;
                case MotorDirection.Forward:
                    latchState |= (byte)(1 << motorBitA);
                    latchState &= (byte)(~(1 << motorBitB));
                    break;
                case MotorDirection.Reverse:
                    latchState &= (byte)(~(1 << motorBitA));
                    latchState |= (byte)(1 << motorBitB);
                    break;
                default:
                    throw new InvalidOperationException("Invalid motor direction specified");
            }

            latch_tx((byte)latchState);
        }

        /// <summary>
        /// Set the speed of the DC motor
        /// </summary>
        /// <param name="speed">The percentage of full speed at which to run (0-100)</param>
        public void SetSpeed(uint speed)
        {
            if (speed > 100)
                speed = 100;

            pwm.SetDutyCycle(speed);
        }
    }

    public class Stepper : MotorShield
    {
#if MICROSTEPS_8
        private static byte[] microstepCurve = {0, 50, 98, 142, 180, 212, 236, 250, 255};
#else
        private static byte[] microstepCurve = { 0, 25, 50, 74, 98, 120, 141, 162, 180, 197, 212, 225, 236, 244, 250, 253, 255 };
#endif

        /// <summary>
        /// Steps per revolution
        /// </summary>
        public readonly uint StepsPerRevolution;

        /// <summary>
        /// The port to which the stepper is connected
        /// </summary>
        public readonly StepperPorts stepperPort;

        private uint usPerStep, stepCounter;
        private uint currentStep = 0;
        private PWM coilA, coilB;
        private uint microsteps;

        public enum StepperPorts
        {
            //M1_M2,
            M3_M4,
        }

        public enum StepType
        {
            /// <summary>
            /// Single Coil activation
            /// </summary>
            Single,
            /// <summary>
            /// Double Coil activation
            /// </summary>
            /// <remarks>Higher torque than Single</remarks>
            Double,
            /// <summary>
            /// Alternating between Single and Double.
            /// </summary>
            /// <remarks>Twice the resolution, but half the speed</remarks>
            Interleave,
            /// <summary>
            /// 8x or 16x Microstepping
            /// </summary>
            /// <remarks>8x (or 16x) higher step resolution, but 1/8 (or 1/16) the torque and speed</remarks>
            Microstep,
        }

        public enum MotorDirection
        {
            Forward,
            Backward,
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="stepsPerRev">The number of steps per complete revolution of the output shaft</param>
        /// <param name="port">The port to which the stepper is connected</param>
        public Stepper(uint stepsPerRev, StepperPorts port)
        {
            StepsPerRevolution = stepsPerRev;
            stepperPort = port;
            currentStep = 0;
            microsteps = (uint)microstepCurve.Length;

            int latchState = 0;
            switch (stepperPort)
            {
                /*
                case StepperPorts.M1_M2:
                    // Turn off all motor pins
                    latchState &= ~(1 << (int)MotorBits.Motor1_A) & 
                                  ~(1 << (int)MotorBits.Motor1_B) & 
                                  ~(1 << (int)MotorBits.Motor2_A) &
                                  ~(1 << (int)MotorBits.Motor2_B);

                    coilA = new PWM(PwmPins.pwm2A);
                    coilB = new PWM(PwmPins.pwm2B);
                    break;
                 */
                case StepperPorts.M3_M4:
                    // turn off all motor pins
                    latchState &= ~(1 << (int)MotorBits.Motor3_A) & 
                                  ~(1 << (int)MotorBits.Motor3_B) & 
                                  ~(1 << (int)MotorBits.Motor4_A) &
                                  ~(1 << (int)MotorBits.Motor4_B);

                    coilA = new PWM(PwmPins.pwm0B);
                    coilB = new PWM(PwmPins.pwm0A);
                    break;
                default:
                    throw new InvalidOperationException("Invalid motor header specified");
            }

            latch_tx((byte)latchState); // Enable channels
            coilA.SetPulse(1000000 / 64000, 0); // 64KHz microstep pwm
            coilB.SetPulse(1000000 / 64000, 0); // 64KHz microstep pwm
        }

        /// <summary>
        /// Move the stepper a specific number of steps
        /// </summary>
        /// <param name="steps">How many steps to move</param>
        /// <param name="dir">The direction in which to rotate</param>
        /// <param name="style">The type of stepping to perform</param>
        public void Step(uint steps, MotorDirection dir, StepType style = StepType.Single)
        {
            uint uspers = usPerStep;
            uint ret = 0;

            if (style == StepType.Interleave) 
            {
                uspers /= 2;
            }
            else if (style == StepType.Microstep) 
            {
                uspers /= microsteps;
                steps *= microsteps;
            }

            while (steps-- > 0)
            {
                ret = OneStep(dir, style);
                Thread.Sleep((int)uspers / 1000); // in ms
                stepCounter += (uspers % 1000);
                if (stepCounter >= 1000)
                {
                    Thread.Sleep(1);
                    stepCounter -= 1000;
                }
            }

            if (style == StepType.Microstep)
            {
                while ((ret != 0) && (ret != microsteps))
                {
                    ret = OneStep(dir, style);
                    Thread.Sleep((int)uspers / 1000); // in ms
                    stepCounter += (uspers % 1000);
                    if (stepCounter >= 1000)
                    {
                        Thread.Sleep(1);
                        stepCounter -= 1000;
                    }
                }
            }

        }

        /// <summary>
        /// Sets the stepper speed
        /// </summary>
        /// <param name="rpm">The speed in revolutions per minute</param>
        public void SetSpeed(uint rpm)
        {
            usPerStep = 60000000 / (StepsPerRevolution * rpm);
            stepCounter = 0;
        }

        /// <summary>
        /// Move the stepper one step
        /// </summary>
        /// <param name="dir">The direction in which to move</param>
        /// <param name="style">The type of stepping to use</param>
        /// <returns>Current step count</returns>
        public uint OneStep(MotorDirection dir, StepType style = StepType.Single)
        {
            byte a, b, c, d;
            byte ocrb, ocra;

            ocra = ocrb = 255;
            switch (stepperPort)
            {
                /*
                case StepperPorts.M1_M2;
                    a = (1<<(int)MotorBits.Motor1_A);
                    b = (1<<(int)MotorBits.Motor2_A);
                    c = (1<<(int)MotorBits.Motor1_B);
                    d = (1<<(int)MotorBits.Motor2_B);
                    break;
                */
                case StepperPorts.M3_M4:
                    a = (1<<(int)MotorBits.Motor3_A);
                    b = (1<<(int)MotorBits.Motor4_A);
                    c = (1<<(int)MotorBits.Motor3_B);
                    d = (1<<(int)MotorBits.Motor4_B);
                    break;
                default:
                    return 0;
            }

            // next determine what sort of stepping procedure we're up to
            if (style == StepType.Single) 
            {
                if ((currentStep/(microsteps/2)) % 2 == 0) // we're at an odd step, weird. Shouldn't happen, but just in case...
                {
                    currentStep += (dir == MotorDirection.Forward) ? (uint)(microsteps / 2) : (uint)(-microsteps / 2);
                } 
                else // go to the next even step
                {
                    currentStep += (dir == MotorDirection.Forward) ? (uint)(microsteps) : (uint)(-microsteps);
                }
            } 
            
            else if (style == StepType.Double) 
            {
                if ((currentStep/(microsteps/2) % 2) != 0) // we're at an even step, weird.  Just in case...
                {
                    currentStep += (dir == MotorDirection.Forward) ? (uint)(microsteps / 2) : (uint)(-microsteps / 2);
                } 
                else  // go to the next odd step
                {
                    currentStep += (dir == MotorDirection.Forward) ? (uint)(microsteps) : (uint)(-microsteps);
                }
            } 
            
            else if (style == StepType.Interleave)
            {
                currentStep += (dir == MotorDirection.Forward) ? (uint)(microsteps) : (uint)(-microsteps);
            } 

            if (style == StepType.Microstep) 
            {
                if (dir == MotorDirection.Forward) 
                {
                    currentStep++;
                } 
                else 
                {
                    // BACKWARDS
                    currentStep--;
                }

                currentStep += microsteps*4;
                currentStep %= microsteps*4;

                ocra = ocrb = 0;
                if ( (currentStep >= 0) && (currentStep < microsteps)) 
                {
                    ocra = microstepCurve[microsteps - currentStep];
                    ocrb = microstepCurve[currentStep];
                } 
                else if  ( (currentStep >= microsteps) && (currentStep < microsteps*2)) 
                {
                    ocra = microstepCurve[currentStep - microsteps];
                    ocrb = microstepCurve[microsteps*2 - currentStep];
                } 
                else if  ( (currentStep >= microsteps*2) && (currentStep < microsteps*3)) 
                {
                    ocra = microstepCurve[microsteps*3 - currentStep];
                    ocrb = microstepCurve[currentStep - microsteps*2];
                } 
                else if  ( (currentStep >= microsteps*3) && (currentStep < microsteps*4)) 
                {
                    ocra = microstepCurve[currentStep - microsteps*3];
                    ocrb = microstepCurve[microsteps*4 - currentStep];
                }
            }

            currentStep += microsteps*4;
            currentStep %= microsteps*4;

            coilA.SetDutyCycle(ocra);
            coilB.SetDutyCycle(ocrb);

            // release all
            latchState &= (byte)(~a & ~b & ~c & ~d); // all motor pins to 0

            //Serial.println(step, DEC);
            if (style == StepType.Microstep) 
            {
                if ((currentStep >= 0) && (currentStep < microsteps))
                    latchState |= (byte)(a | b);
                if ((currentStep >= microsteps) && (currentStep < microsteps*2))
                    latchState |= (byte)(b | c);
                if ((currentStep >= microsteps*2) && (currentStep < microsteps*3))
                    latchState |= (byte)(c | d);
                if ((currentStep >= microsteps*3) && (currentStep < microsteps*4))
                    latchState |= (byte)(d | a);
            } 
            else 
            {
                switch (currentStep/(microsteps/2)) 
                {
                    case 0:
                        latchState |= (byte)(a); // energize coil 1 only
                    break;
                    case 1:
                        latchState |= (byte)(a | b); // energize coil 1+2
                    break;
                    case 2:
                        latchState |= (byte)(b); // energize coil 2 only
                    break;
                    case 3:
                        latchState |= (byte)(b | c); // energize coil 2+3
                    break;
                    case 4:
                        latchState |= (byte)(c); // energize coil 3 only
                    break; 
                    case 5:
                        latchState |= (byte)(c | d); // energize coil 3+4
                    break;
                    case 6:
                        latchState |= (byte)(d); // energize coil 4 only
                    break;
                    case 7:
                        latchState |= (byte)(d | a); // energize coil 1+4
                    break;
                }
            }

 
            latch_tx(latchState);
            return currentStep;
        }

        /// <summary>
        /// Releases the motor, allowing it to spin freely
        /// </summary>
        public void Release()
        {
            int latchState = 0;

            switch (stepperPort)
            {
                /*
                case StepperPorts.M1_M2:
                    // Turn off all motor pins
                    latchState &= ~(1 << (int)MotorBits.Motor1_A) &
                                  ~(1 << (int)MotorBits.Motor1_B) &
                                  ~(1 << (int)MotorBits.Motor2_A) &
                                  ~(1 << (int)MotorBits.Motor2_B);
                    break;
                */
                case StepperPorts.M3_M4:
                    // turn off all motor pins
                    latchState &= ~(1 << (int)MotorBits.Motor3_A) &
                                  ~(1 << (int)MotorBits.Motor3_B) &
                                  ~(1 << (int)MotorBits.Motor4_A) &
                                  ~(1 << (int)MotorBits.Motor4_B);
                    break;
                default:
                    throw new InvalidOperationException("Invalid motor header specified");
            }

            latch_tx((byte)latchState); // disable channels

            // Ste speed to 0
            coilA.SetDutyCycle(0);
            coilB.SetDutyCycle(0);

        }
    }

    public class Servo : MotorShield
    {
        // Not yet implemented
    }
}
