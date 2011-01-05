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
            OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);
            DCMotor dc = new DCMotor(MotorShield.MotorHeaders.M1);   // A DC motor on header M1
            dc.SetSpeed(0);
            dc.Run(DCMotor.MotorDirection.Forward); // Disable the motor

            int speed = 0;
            int increment = 1;

            while (true)
            {
                dc.SetSpeed((uint)speed);
                
                speed += increment;
                if(speed > 100)
                {
                    speed = 100;
                    increment = -1;
                }
                else if (speed < 0)
                {
                    speed = 0;
                    increment = 1;
                }

                led.Write((increment > 0) ? true : false);
                Debug.Print(speed + "\n");

                Thread.Sleep(50);
            }
        }

    }
}
