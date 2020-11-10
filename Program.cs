using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace K30Co2Measure
{
    class Program
    {
        #region "Library"

        /// <summary> =============================================================================
        /// Import「Linux C Library」
        /// </summary> ============================================================================
        // open Systm call
        [DllImport("libc", EntryPoint = "open")]
        extern static int I2CBusOpen(string BusPath, int O_RDW);

        // ioctrl Systm call
        [DllImport("libc", EntryPoint = "ioctl")]
        extern static int I2CIoctl(int FileDescriptor, uint I2C_SLAVE, int SlaveAdress);

        // write Systm call
        [DllImport("libc", EntryPoint = "write")]
        extern static int I2CWrite(int FileDescriptor, byte[] SetBuffer, int BufferLength);

        // read Systm call
        [DllImport("libc", EntryPoint = "read")]
        extern static int I2CRead(int FileDescriptor, byte[] ReadBuffer, int BufferLength);

        // close Systm call
        [DllImport("libc", EntryPoint = "close")]
        extern static int I2CClose(int FileDescriptor);

        #endregion

        #region "Constant"

        /// <summary> =============================================================================
        /// Constant
        /// </summary> ============================================================================
        // Default slave ID of「K30」Co2 sensor
        const int K30Addresses = 0x68;

        // Data to send
        static byte COMMAND_NUMBER_AND_BYTES_TO_READ = 0x22;  // Command
        static byte ADDRESS_RANGE_START = 0x00;　　　　　　   // RAM Address
        static byte READ_MEMORY_LOCATIONS_HI_BYTE = 0x08;     // RAM Address
        static byte CHECKSUM_AS_SUM_OF_BYTE = 0x2A;           // Sum of byte 2nd, 3rd and 4th.

        // O_RDWR
        const int O_RDWR = 0x0002;

        // I2C_SLAVE
        const int I2C_SLAVE = 0x0703;

        #endregion

        #region "Program"

        /// <summary> =============================================================================
        /// CO2 Measure
        /// </summary> ============================================================================
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            try
            {
                int ret;
                byte[] set_value = new byte[]
                {   COMMAND_NUMBER_AND_BYTES_TO_READ,
                    ADDRESS_RANGE_START,
                    READ_MEMORY_LOCATIONS_HI_BYTE,
                    CHECKSUM_AS_SUM_OF_BYTE
                };

                ret = I2CWriteAsync(K30Addresses, set_value);
                Console.WriteLine("CO2：{0}ppm", ret);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary> =============================================================================
        /// Detect the value of the CO2 sensor via the I2C bus
        /// </summary> ============================================================================
        /// <param name="SlaveAdress"></param>
        /// <param name="SetValue"></param>
        /// <returns></returns>
        static int I2CWriteAsync(int SlaveAdress, byte[] SetValue)
        {
            int ret = 0;

            // Open the I2C Bus.
            string dev = string.Format("/dev/i2c-{0}", 1);
            int FileDescriptor = I2CBusOpen(dev, O_RDWR);
            if (FileDescriptor < 0)
            {
                return -96;
            }

            // Set FileDescriptor to I2C slave address.
            ret = I2CIoctl(FileDescriptor, I2C_SLAVE, SlaveAdress);
            if (ret < 0)
            {
                I2CClose(FileDescriptor);
                return -97;
            }

            // Data writing
            ret = I2CWrite(FileDescriptor, SetValue, SetValue.Length);
            if (ret < 0)
            {
                I2CClose(FileDescriptor);
                return -98;
            }

            Thread.Sleep(25);

            // Create a buffer for read data
            byte[] readBuffer = new byte[5];

            // Data reading
            ret = I2CRead(FileDescriptor, readBuffer, readBuffer.Length);

            // Close the File Descriptor.
            I2CClose(FileDescriptor);

            if (ret < 0)
            {
                Console.WriteLine("Error:{0}", readBuffer[0]);
                return -99;
            }

            // The 2:nd and 3:rd byte will contain CO2 value hi byte and CO2 value low byte.
            int co2Val = (readBuffer[1] * 256) + readBuffer[2];

            // The 4:th byte contains checksum
            byte receivedCheckSum = readBuffer[3];

            // calculated as sum of byte 1st, 2nd and 3rd.
            byte[] checkBuf = new byte[] { readBuffer[0], readBuffer[1], readBuffer[2] };

            // Verify the checksum is NG.
            if (CheckSum(checkBuf, checkBuf.Length, receivedCheckSum) == false)
            {
                //todo：
            }

            return co2Val;
        }

        /// <summary> =============================================================================
        /// Verify checksum
        /// </summary> ============================================================================
        /// <param name="receivedBuf"></param>
        /// <param name="bufCount"></param>
        /// <param name="receivedCheckSum"></param>
        /// <returns></returns>
        static bool CheckSum(byte[] receivedBuf, int bufCount, byte receivedCheckSum)
        {
            byte sum = 0;

            for (int i = 0; i < bufCount; i++)
            {
                sum += receivedBuf[i];
            }

            return receivedCheckSum == sum;
        }

        #endregion
    }
}

