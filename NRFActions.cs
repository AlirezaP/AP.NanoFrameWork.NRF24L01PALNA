using AP.NanoFrameWork.NRF24L01PALNA.Models;
using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Diagnostics;
using System.Threading;

namespace AP.NanoFrameWork.NRF24L01PALNA
{
    public class NRFActions
    {
        public delegate void ACKRecivedHandler(object source, AckEventArgs e);
        public event ACKRecivedHandler AckRecivedEventHandler;

        public enum PAState { Min = 0x2F, Low = 0x2D, High = 0x2B, Max = 0x29 }
        public enum DataRate { d1Mbps = 0xd7, d2Mbps = 0xdf, d250K = 0xf7 }
        public enum Register
        {
            R_REGISTER = 0x00,
            W_REGISTER = 0x20,
            R_RX_PAYLOAD = 0x61,
            W_TX_PAYLOAD = 0xA0,
            FLUSH_TX = 0xE1,
            FLUSH_RX = 0xE2,
            REUSE_TX_PL = 0xE3,
            R_RX_PL_WID = 0x60,
            W_ACK_PAYLOAD = 0xA8,
            W_TX_PAYLOAD_NO_ACK = 0xB0,
            NOP = 0xFF,

            PWR_UP = 0x1,
            PRIM_RX = 0x0,


            REGISTER_MASK = 0x1F,
            ACTIVATE = 0x50,






            //Memory Map

            NRF_CONFIG = 0x00,
            EN_AA = 0x01,
            EN_RXADDR = 0x02,
            SETUP_AW = 0x03,
            SETUP_RETR = 0x04,
            RF_CH = 0x05,
            RF_SETUP = 0x06,
            NRF_STATUS = 0x07,
            OBSERVE_TX = 0x08,
            CD = 0x09,
            RX_ADDR_P0 = 0x0A,
            RX_ADDR_P1 = 0x0B,
            RX_ADDR_P2 = 0x0C,
            RX_ADDR_P3 = 0x0D,
            RX_ADDR_P4 = 0x0E,
            RX_ADDR_P5 = 0x0F,
            TX_ADDR = 0x10,
            RX_PW_P0 = 0x11,
            RX_PW_P1 = 0x12,
            RX_PW_P2 = 0x13,
            RX_PW_P3 = 0x14,
            RX_PW_P4 = 0x15,
            RX_PW_P5 = 0x16,
            FIFO_STATUS = 0x17,
            DYNPD = 0x1C,
            FEATURE = 0x1D,



            CONFIG = 0x00,// 'Config' register address 

            RD_RX_PLOAD = 0x61,
            WR_TX_PLOAD = 0xA0,

            WR_TX_PLOAD_NO_ACT = 0xB0
        }


        public ushort RF_DR_LOW = 5;
        public ushort RF_DR_HIGH = 3;
        public ushort RF_PWR_LOW = 1;
        public ushort RF_PWR_HIGH = 2;

        public ushort ARD = 4;
        public uint EN_CRC = 3;
        public uint CRCO = 2;
        public uint RX_DR = 6;
        public uint TX_DS = 5;
        public uint MAX_RT = 4;



        private byte RX_PLOAD_WIDTH = 32;
        private byte TX_PLOAD_WIDTH = 32;



        private byte[] TX_ADDR_New = new byte[5]
{
          0x70,0x12,0x12,0x00,0x01
};



        PinValue csnHigth = PinValue.High;// PinValue.High;
        PinValue csnLow = PinValue.Low;// PinValue.Low;

        PinValue ceHigth = PinValue.High;// PinValue.High;
        PinValue ceLow = PinValue.Low;// PinValue.Low;


        private SpiDevice _spi;
        private GpioPin _csn;
        private GpioPin _ce;


        public NRFActions(SpiDevice spi, GpioPin csn, GpioPin ce)
        {
            _spi = spi;
            _csn = csn;
            _ce = ce;

            ce.Write(PinValue.Low);
            csn.Write(PinValue.High);
        }


        //.........................................................

        public byte SPIRead(byte reg)
        {
            BeginCommand();

            SpanByte readBuffer = new byte[1];
            SpanByte readBuffer2 = new byte[1];

            _spi.TransferFullDuplex(new byte[] { reg }, readBuffer);
            _spi.TransferFullDuplex(new byte[] { 0 }, readBuffer2);


            EndCommand();

            return readBuffer2[0];
        }

        public byte SPIRW(byte register)
        {
            SpanByte readBuffer = new byte[1];

            _spi.TransferFullDuplex(new byte[] { register }, readBuffer);

            return readBuffer[0];
        }

        public byte SPIWriteRegister(byte register, byte data)
        {
            BeginCommand();

            SpanByte readBuffer = new byte[1];
            SpanByte readBuffer2 = new byte[1];

            _spi.TransferFullDuplex(new byte[] { register }, readBuffer);
            _spi.TransferFullDuplex(new byte[] { data }, readBuffer2);


            EndCommand();

            return readBuffer[0];
        }

        public byte SPIWriteBuffer(byte reg, byte[] pBuf)
        {

            SpanByte readBuffer = new byte[pBuf.Length];

            BeginCommand();

            var status = SPIRW(reg);
            _spi.TransferFullDuplex(pBuf, readBuffer);


            EndCommand();

            return (status); // 
        }

        public byte[] SPIReadBuffer(byte reg, int len)
        {

            byte[] pBuf = new byte[len];

            BeginCommand();

            var status = SPIRW(reg);
            _spi.TransferFullDuplex(new byte[] { 0 }, pBuf);

            EndCommand();


            return (pBuf);
        }

        //.........................................................

        public byte[] ReciveData()
        {


            byte[] rx_buf = null;

            byte stateResult = SPIRead((byte)(Register.NRF_STATUS));

            if ((stateResult & BV(RX_DR)) > 0)
            {
                rx_buf = new byte[TX_PLOAD_WIDTH];
                //_ce.Write(ceLow);

                rx_buf = SPIReadBuffer((byte)Register.RD_RX_PLOAD, TX_PLOAD_WIDTH);
                var r = SPIWriteRegister((byte)(Register.FLUSH_RX), 0);

            }


            SPIWriteRegister((byte)(Register.W_REGISTER | Register.NRF_STATUS), stateResult);// clear RX_DR or TX_DS or MAX_RT interrupt flag


            return rx_buf;

        }

        public void SendData(string txt)
        {
            var buteData = System.Text.Encoding.UTF8.GetBytes(txt);

            if (buteData.Length > 32)
            {
                Debug.WriteLine("More than 32 byte Must be Coded and ......");
                return;
            }

            SendData(buteData);
        }

        public void SendData(byte[] data)
        {

            byte[] tx_buf = new byte[32];

            Array.Copy(data, 0, tx_buf, 0, data.Length);

            Thread.Sleep(100);

            byte sstatus = SPIRead((byte)(Register.NRF_STATUS));


            Debug.WriteLine($"NRF_STATUS: {sstatus.ToString()}");

            SPIWriteBuffer((byte)Register.WR_TX_PLOAD, tx_buf);

            if ((sstatus & BV(TX_DS)) > 0)
            {
                SPIWriteRegister((byte)(Register.FLUSH_TX), 0);
                AckRecivedEventHandler?.Invoke(this, new AckEventArgs(true));
                // SPI_Write_Buf(WR_TX_PLOAD, tx_buf); // Writes data to TX payload 
            }

            if ((byte)(sstatus & BV(MAX_RT)) > 0)
            {
                //SPI_Write_Buf(WR_TX_PLOAD, tx_buf); // Writes data to TX payload 
                SPIWriteRegister((byte)(Register.FLUSH_TX), 0);
            }

            Thread.Sleep(1000);

            SPIWriteRegister((byte)(Register.W_REGISTER | Register.NRF_STATUS), sstatus);


        }

        //.........................................................

        public void InitialTXMode(PAState pa, DataRate rate)
        {
            Thread.Sleep(500);
            TX_ADDR_New = new byte[5] { 0x70, 0x12, 0x12, 0x00, 0x01 };
            Thread.Sleep(500);
            InitialTXMode(pa, rate, TX_ADDR_New, 0x40);
        }

        public void InitialTXMode(PAState pa, DataRate rate, byte[] txAddress, byte rfChannel = 0x40)
        {
            if (txAddress == null || txAddress.Length > 5)
            {
                throw new Exception("Invalid txAddress!");
            }

            byte RF_SETUP_Value = (byte)(((byte)(pa)) & ((byte)(rate)));

            //initial io 
            _ce.Write(ceLow);
            _csn.Write(csnHigth);

            Thread.Sleep(100);


            _ce.Write(ceLow);
            Thread.Sleep(100);



            SPIWriteBuffer((byte)(Register.W_REGISTER | Register.TX_ADDR), txAddress);
            SPIWriteBuffer((byte)(Register.W_REGISTER | Register.RX_ADDR_P0), txAddress);


            SPIWriteRegister((byte)(Register.W_REGISTER | Register.EN_AA), 0x01);
            SPIWriteRegister((byte)(Register.W_REGISTER | Register.EN_RXADDR), 0x01); // Enable Pipe0 


            SPIWriteRegister((byte)(Register.W_REGISTER | Register.SETUP_AW), 0x03); // Setup address width=5 bytes 

            SPIWriteRegister((byte)(Register.W_REGISTER | Register.SETUP_RETR), 0x1a); // 500us + 86us, 10 retrans... 
            SPIWriteRegister((byte)(Register.W_REGISTER | Register.RF_CH), rfChannel);

            //SPI_RW_Reg((byte)(W_REGISTER | RF_SETUP), 0x00);
            SPIWriteRegister((byte)(Register.W_REGISTER | Register.RF_SETUP), RF_SETUP_Value);

            SPIWriteRegister((byte)(Register.W_REGISTER | Register.RX_PW_P0), RX_PLOAD_WIDTH);

            SPIWriteRegister((byte)(Register.W_REGISTER | Register.CONFIG), 0x0e); // Set PWR_UP bit, enable CRC(2 bytes) & Prim: RX.RX_DR enabled..

            SPIWriteRegister((byte)(Register.FLUSH_RX), 0);
            SPIWriteRegister((byte)(Register.FLUSH_TX), 0);


            //SPI_RW_Reg((byte)(Register.NRF_STATUS), (byte)(BV(RX_DR) | BV(TX_DS) | BV(MAX_RT)));


            _ce.Write(ceHigth);

            Thread.Sleep(100);
        }

        public void InitialRXMode(PAState pa, DataRate rate)
        {
            TX_ADDR_New = new byte[5] { 0x70, 0x12, 0x12, 0x00, 0x01 };

            InitialRXMode(pa, rate, TX_ADDR_New, 0x40);
        }
        public void InitialRXMode(PAState pa, DataRate rate, byte[] txAddress, byte rfChannel = 0x40)
        {
            if (txAddress == null || txAddress.Length > 5)
            {
                throw new Exception("Invalid txAddress!");
            }

            byte RF_SETUP_Value = (byte)(((byte)(pa)) & ((byte)(rate)));

            //initial io 
            _ce.Write(ceLow);
            _csn.Write(csnHigth);

            Thread.Sleep(100);


            _ce.Write(ceLow);

            Thread.Sleep(100);



            SPIWriteBuffer((byte)(Register.W_REGISTER | Register.RX_ADDR_P0), txAddress);

            SPIWriteRegister((byte)(Register.W_REGISTER | Register.EN_AA), 0x01);
            SPIWriteRegister((byte)(Register.W_REGISTER | Register.EN_RXADDR), 0x01); // Enable Pipe0 


            SPIWriteRegister((byte)(Register.W_REGISTER | Register.SETUP_AW), 0x03); // Setup address width=5 bytes 

            //SPI_RW_Reg((byte)(W_REGISTER | SETUP_RETR), 0x1a); // 500us + 86us, 10 retrans... 

            SPIWriteRegister((byte)(Register.W_REGISTER | Register.RF_CH), rfChannel);


            SPIWriteRegister((byte)(Register.W_REGISTER | Register.RF_SETUP), RF_SETUP_Value);



            SPIWriteRegister((byte)(Register.W_REGISTER | Register.RX_PW_P0), RX_PLOAD_WIDTH);

            //Here 0x0e or 0x0f
            SPIWriteRegister((byte)(Register.W_REGISTER | Register.CONFIG), 0x0f); // Set PWR_UP bit, enable CRC(2 bytes) & Prim: RX.RX_DR enabled..
                                                                                   //SPI_RW_Reg((byte)(W_REGISTER | EN_AA), 0x01);
                                                                                   // SPI_RW_Reg((byte)(W_REGISTER | EN_RXADDR), 0x01); // Enable Pipe0 



            SPIWriteRegister((byte)(Register.FLUSH_RX), 0);
            SPIWriteRegister((byte)(Register.FLUSH_TX), 0);


            SPIWriteRegister((byte)(Register.W_REGISTER | Register.NRF_STATUS), (byte)(BV(RX_DR) | BV(TX_DS) | BV(MAX_RT)));
            //var r = SPI_RW_Reg((byte)(FLUSH_RX), 0);
            //var r2 = SPI_RW_Reg((byte)(FLUSH_TX), 0);


            _ce.Write(ceHigth);// CE = 1; // 
            Thread.Sleep(100);
        }

        //.........................................................

        public void BeginCommand()
        {
            _csn.Write(csnLow);
            Thread.Sleep(100);
        }

        public void EndCommand()
        {
            _csn.Write(csnHigth);
            Thread.Sleep(100);
        }

        //.........................................................

        private uint Pow(uint num, uint pow)
        {
            uint result = 1;

            if (pow > 0)
            {
                for (int i = 1; i <= pow; ++i)
                {
                    result *= num;
                }
            }
            else if (pow < 0)
            {
                for (int i = -1; i >= pow; --i)
                {
                    result /= num;
                }
            }

            return result;
        }

        private byte BV(uint bit)
        {
            return (byte)(Pow(2, bit));
        }


    }
}

