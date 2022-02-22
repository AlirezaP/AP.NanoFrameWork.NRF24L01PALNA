using nanoFramework.Hardware.Esp32;
using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Diagnostics;
using System.Threading;

namespace AP.NanoFrameWork.NRF24L01PALNA
{
    public class Program
    {
        public static void Main()
        {
            Debug.WriteLine("Hello from nanoFramework!");

            Thread.Sleep(2000);
            var gpioController = new GpioController();


            var p1 = Configuration.GetFunctionPin(DeviceFunction.SPI1_CLOCK);

            if (p1 != 18)
            {

                Configuration.SetPinFunction(18, DeviceFunction.SPI1_CLOCK);
                Configuration.SetPinFunction(19, DeviceFunction.SPI1_MISO);
                Configuration.SetPinFunction(23, DeviceFunction.SPI1_MOSI);

            }

            var irq = gpioController.OpenPin(17);
            irq.SetPinMode(PinMode.InputPullUp);
            irq.ValueChanged += Irq_ValueChanged;

            var ce = gpioController.OpenPin(16);
            ce.SetPinMode(PinMode.Output);

            //  var csn = gpioController.OpenPin(15, PinMode.Output);
            var csn = gpioController.OpenPin(5, PinMode.Output);


            Thread.Sleep(200);


            SpiDevice spiDevice;
            SpiConnectionSettings connectionSettings;


            SpiBusInfo spiBusInfo = SpiDevice.GetBusInfo(1);
            Debug.WriteLine($"{nameof(spiBusInfo.ChipSelectLineCount)}: {spiBusInfo.ChipSelectLineCount}");
            Debug.WriteLine($"{nameof(spiBusInfo.MaxClockFrequency)}: {spiBusInfo.MaxClockFrequency}");
            Debug.WriteLine($"{nameof(spiBusInfo.MinClockFrequency)}: {spiBusInfo.MinClockFrequency}");
            Debug.WriteLine($"{nameof(spiBusInfo.SupportedDataBitLengths)}: ");

            foreach (var data in spiBusInfo.SupportedDataBitLengths)
            {
                Debug.WriteLine($"  {data}");
            }

         
            connectionSettings = new SpiConnectionSettings(1, -1);
            connectionSettings.ClockFrequency = 4_000_000;
            connectionSettings.DataBitLength = 8;
            connectionSettings.DataFlow = DataFlow.MsbFirst;
            connectionSettings.Mode = SpiMode.Mode0;


            // Then you create your SPI device by passing your settings
            spiDevice = SpiDevice.Create(connectionSettings);


            Thread.Sleep(50);


            NRFActions nrf24 = new NRFActions(spiDevice, csn, ce);



            ////TX
            ////..................

            nrf24.InitialTXMode(NRFActions.PAState.Min, NRFActions.DataRate.d1Mbps);


            Thread.Sleep(100);


            while (true)
            {
                nrf24.SendData("Hi My Name Is Alireza Paridar.)");
                Thread.Sleep(1000);
            }

            ///RX
            ////..................
            
            //nrf24.InitialRXMode(NRFActions.PAState.Min, NRFActions.DataRate.d1Mbps);


            //while (true)
            //{
            //    Thread.Sleep(1000);

            //    var buf = nrf24.ReciveData();

            //    if (buf == null || buf.Length <= 0)
            //    {
            //        continue;
            //    }

            //    string res = "";
            //    for (int i = 0; i < buf.Length; i++)
            //    {
            //        res += $"{buf[i].ToString()},";
            //    }

            //    Debug.WriteLine(res);
            //    Debug.WriteLine("");

            //    string txt = System.Text.Encoding.UTF8.GetString(buf, 0, buf.Length);
            //    Debug.WriteLine(txt);
            //    Debug.WriteLine("------------------");

            //}


            Thread.Sleep(Timeout.Infinite);


        }

        private static void Irq_ValueChanged(object sender, PinValueChangedEventArgs e)
        {
            Debug.WriteLine("IRQ: " + ((int)e.ChangeType).ToString());
        }


    }
}
