using System;

namespace AP.NanoFrameWork.NRF24L01PALNA.Models
{
   public class TransfertsResponseModel
    {
        public string Message { get; set; }
        public ushort Status { get; set; }
        public ushort[] ReadBufferus { get; set; }
    }
}
