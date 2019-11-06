using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace YawVR
{
    /*Communication between the game and the simulator:
        (Commands can be created easily by calling static functions on Commands class)

        UDP messages:
        Every udp command is ascii encoded string sent as byte array

        TCP messages:
        Every tcp command begins with the byte identifier of the given command, 
        followed by the command parameters.
        Integer and float parameters are converted into 4 bytes (sent in big endian format), 
        string parameters are converted into byte array with ASCII encoding.
        (CommandIds class contains the tcp command ids)
    */

    //MARK: - Command ID-s
    public static class CommandIds
    {
        public const byte CHECK_IN = 0x30;
        public const byte START = 0xA1;
        public const byte STOP = 0xA2;
        public const byte EXIT = 0xA3;
        public const byte RESET_PORTS = 0x01;
        public const byte SET_SIMU_INPUT_PORT = 0x10;
        public const byte SET_GAME_INPUT_PORT = 0x11;
        public const byte SET_GAME_IP_ADDRESS = 0xA4;
        public const byte SET_OUTPUT_PORT = 0x12;
        public const byte SET_YAW_PID = 0x99;
        public const byte SET_PITCH_PID = 0x9A;
        public const byte SET_ROLL_PID = 0x9B;
        public const byte SET_GAME_MODE = 0x80;
        public const byte GET_GAME_PARAMS = 0x81;
        public const byte SET_POWER = 0x30;
        public const byte SET_TILT_LIMITS = 0x40;
        public const byte SET_YAW_LIMIT = 0x70;
        public const byte SET_YAW_LIMIT_SPEED = 0x71;
        public const byte SET_LED_STRIP_COLOR = 0xB0;
        public const byte SET_LED_STRIP_MODE = 0xB1;
        public const byte CHECK_IN_ANS = 0x31;
        public const byte ERROR = 0xA5;
        public const byte SERVER_PID_PARAMS = 0xFF;

    };


    public static class Commands
    {
        //MARK: - CALLS FROM GAME TO SIMULATOR
        //UDP
        public static byte[] DEVICE_DISCOVERY = Encoding.ASCII.GetBytes("YAW_CALLING");
          
        //example: "Y[000.00]P[359.99]R[180.00]"; - there is no 360.00, just 000.00
        public static byte[] SET_POSITION(float yaw, float pitch, float roll)
        {
            var message = "Y[" + FormatRotation(yaw) + "]P[" + FormatRotation(pitch) + "]R[" + FormatRotation(roll) + "]";
            return Encoding.ASCII.GetBytes(message);
        }

        //TCP

        public static byte[] CHECK_IN(int udpListeningPort, string gameName)
        {
            List<byte> message = new List<byte>();
            message.AddRange(IntToByteArray(udpListeningPort));
            message.AddRange(Encoding.ASCII.GetBytes(gameName));
            return AddByteToArray(message.ToArray(), CommandIds.CHECK_IN);
        }

        public static byte[] START = { CommandIds.START };

        public static byte[] STOP = { CommandIds.STOP };

        public static byte[] EXIT = { CommandIds.EXIT };

        public static byte[] SET_TILT_LIMITS(int pitchFrontMax, int pitchBackMax, int rollMax)
        {
            List<byte> message = new List<byte>();
            message.AddRange(IntToByteArray(pitchFrontMax));
            message.AddRange(IntToByteArray(pitchBackMax));
            message.AddRange(IntToByteArray(rollMax));
            return AddByteToArray(message.ToArray(), CommandIds.SET_TILT_LIMITS);
        }

        public static byte[] SET_YAW_LIMIT(int yawMax) 
        {
            var message = IntToByteArray(yawMax);
            return AddByteToArray(message, CommandIds.SET_YAW_LIMIT);
        }


        //MARK: - Helper functions
        private static byte[] AddByteToArray(byte[] bArray, byte newByte)
        {
            byte[] newArray = new byte[bArray.Length + 1];
            bArray.CopyTo(newArray, 1);
            newArray[0] = newByte;
            return newArray;
        }

        private static string FormatRotation(float f)
        {
            float i = (float)((int)(f * 100)) / (float)100.0;
            while (i < 0) i += 360;
            while (i >= 360) i -= 360;
            string s = i.ToString();
            if (i < 10)
            {
                s = "00" + s;
            }
            else if (i < 100)
            {
                s = "0" + s;
            }
            if (s.Length == 5) s = s + "0";
            if (s.Length == 4) s = s + "00";
            if (s.Length == 3) s = s + ".00";
            return s;
        }

        private static byte[] IntToByteArray(int intValue)
        {
            byte[] intBytes = BitConverter.GetBytes(intValue);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);
            return intBytes;
        }

        private static byte[] FloatToByteArray(float floatValue)
        {
            byte[] floatBytes = BitConverter.GetBytes(floatValue);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(floatBytes);
            return floatBytes;
        }

        public static int ByteArrayToInt(byte[] intBytes, int startIndex)
        {
            byte[] intArray = new byte[4] { intBytes[startIndex], intBytes[startIndex + 1], intBytes[startIndex + 2], intBytes[startIndex + 3] };
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(intArray);
            }

            int integer = BitConverter.ToInt32(intArray, 0);
            return integer;
        }

        public static float ByteArrayToFloat(byte[] floatBytes, int startIndex)
        {
            byte[] floatArray = new byte[4] { floatBytes[startIndex], floatBytes[startIndex + 1], floatBytes[startIndex + 2], floatBytes[startIndex + 3] };
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(floatArray);
            }
            float floatNumber = BitConverter.ToSingle(floatArray, 0);
            return floatNumber;
        }
    }
}

