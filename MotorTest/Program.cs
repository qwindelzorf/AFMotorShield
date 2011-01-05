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
            DCMotor dc = new DCMotor(MotorShield.MotorHeaders.M1);   // A DC motor on header M1
            dc.Run(DCMotor.MotorDirection.Release); // Disable the motor
            dc.SetSpeed(50); // 50% speed
        }

    }
}
