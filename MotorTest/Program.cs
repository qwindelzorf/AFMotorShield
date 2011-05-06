using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using AFMotorShield;

namespace MotorTest
{
    public class Program
    {
        public static void Main()
        {
            DCMotor leftWheel = new DCMotor(MotorHeaders.M4);
            DCMotor rightWheel = new DCMotor(MotorHeaders.M3);

            leftWheel.SetSpeed(0);
            rightWheel.SetSpeed(0);

            leftWheel.Run(DCMotor.MotorDirection.Forward);
            rightWheel.Run(DCMotor.MotorDirection.Forward);

            leftWheel.SetSpeed(90);
            rightWheel.SetSpeed(90);

            while (true)
            {
                // spin left
                leftWheel.Run(DCMotor.MotorDirection.Reverse);
                rightWheel.Run(DCMotor.MotorDirection.Forward);

                Thread.Sleep(1000);

                // spin right
                leftWheel.Run(DCMotor.MotorDirection.Forward);
                rightWheel.Run(DCMotor.MotorDirection.Reverse);

                Thread.Sleep(1000);
            }
        }
    }
}
