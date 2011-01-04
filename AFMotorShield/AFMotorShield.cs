using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

//#define MICROSTEPS_8

namespace AFMotorShield
{

    public class DCMotor
    {
        private enum PinNames
        {
            MOTORLATCH = Pins.GPIO_PIN_D12,
            MOTORCLK = Pins.GPIO_PIN_D4,
            MOTORENABLE = Pins.GPIO_PIN_D7,
            MOTORDATA = Pins.GPIO_PIN_D8,
        }

        private enum MotorPins
        {
            Motor1_A = Pins.GPIO_PIN_D2,
            Motor1_B = Pins.GPIO_PIN_D3,
            Motor2_A = Pins.GPIO_PIN_D1,
            Motor2_B = Pins.GPIO_PIN_D4,
            Motor3_A = Pins.GPIO_PIN_D0,
            Motor3_B = Pins.GPIO_PIN_D6,
            Motor4_A = Pins.GPIO_PIN_D5,
            Motor4_B = Pins.GPIO_PIN_D7,
        }

        public enum MotorHeader
        {
            M1,
            M2,
            M3,
            M4
        }

        private static PWM pwm;
        private static OutputPort motorPinA, motorPinB;

        /// <summary>
        /// A DC Motor handler
        /// </summary>
        /// <param name="header">Which header the motor is connected to</param>
        /// <param name="freq">The PWM frequency (in Hz) at which to drive the motor</param>
        public DCMotor(MotorHeader header, uint freq)
        {

            switch (header)
            {
                case MotorHeader.M1:
                    motorPinA = new OutputPort(MotorPins.Motor1_A, false);
                    motorPinB = new OutputPort(MotorPins.Motor1_B, false);
                    pwm = new PWM(MotorPins.Motor1_A);
                    break;
                case MotorHeader.M2:
                    motorPinA = new OutputPort(MotorPins.Motor2_A, false);
                    motorPinB = new OutputPort(MotorPins.Motor2_B, false);
                    pwm = new PWM(MotorPins.Motor2_A);
                    break;
                case MotorHeader.M3:
                    motorPinA = new OutputPort(MotorPins.Motor3_A, false);
                    motorPinB = new OutputPort(MotorPins.Motor3_B, false);
                    pwm = new PWM(MotorPins.Motor3_A);
                    break;
                case MotorHeader.M4:
                    motorPinA = new OutputPort(MotorPins.Motor4_A, false);
                    motorPinB = new OutputPort(MotorPins.Motor4_B, false);
                    pwm = new PWM(MotorPins.Motor4_A);
                    break;
                default:
                    throw new InvalidOperationException("Invalid motor header specified");
                    break;
            }

            pwm.SetPulse(1000000/freq, 0); // Set PWM frequency
        }
        public void run(uint var);
        public void setSpeed(uint speed);
    }

    public class StepperMotor
    {
#if MICROSTEPS_8
        private static uint[] microstepcurve = {0, 50, 98, 142, 180, 212, 236, 250, 255};
#else
        private static uint[] microstepcurve = { 0, 25, 50, 74, 98, 120, 141, 162, 180, 197, 212, 225, 236, 244, 250, 253, 255 };
#endif

        /// <summary>
        /// Steps per revolution
        /// </summary>
        public uint revsteps;
        public uint steppernum;
        public uint usperstep, steppingcounter;
        private uint currentstep;

        public StepperMotor(uint stepsPerRev, uint port);
        public void step(uint steps, bool dir, uint style = 0);
        public void setSpeed(uint speed);
        public uint onestep(bool dir, uint style);
        public void release();
    }
}
