# AP.NanoFrameWork.NRF24L01PALNA
NanoFrameWork Library to Control NRF24L01PALNA

(https://www.hackster.io/Alirezap/nanoframework-and-nrf24l01-pa-lna-dc510e)

```
            connectionSettings = new SpiConnectionSettings(1, -1);
            connectionSettings.ClockFrequency = 4_000_000;
            connectionSettings.DataBitLength = 8;
            connectionSettings.DataFlow = DataFlow.MsbFirst;
            connectionSettings.Mode = SpiMode.Mode0;


            // Then you create your SPI device by passing your settings
            spiDevice = SpiDevice.Create(connectionSettings);


            Thread.Sleep(50);


            NRFActions nrf24 = new NRFActions(spiDevice, csn, ce);
            
            
```

Tx:

```          
            nrf24.InitialTXMode(NRFActions.PAState.Min, NRFActions.DataRate.d1Mbps);


            Thread.Sleep(100);


            while (true)
            {
                nrf24.SendData("Hi My Name Is Alireza Paridar.)");
                Thread.Sleep(1000);
            }
```

RX:
```

            nrf24.InitialRXMode(NRFActions.PAState.Min, NRFActions.DataRate.d1Mbps);


            while (true)
            {
                Thread.Sleep(1000);

                var buf = nrf24.ReciveData();

                if (buf == null || buf.Length <= 0)
                {
                    continue;
                }

                string res = "";
                for (int i = 0; i < buf.Length; i++)
                {
                    res += $"{buf[i].ToString()},";
                }

                Debug.WriteLine(res);
                Debug.WriteLine("");

                string txt = System.Text.Encoding.UTF8.GetString(buf, 0, buf.Length);
                Debug.WriteLine(txt);
                Debug.WriteLine("------------------");

            }
```
