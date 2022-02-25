

using System;

namespace AP.NanoFrameWork.NRF24L01PALNA.Models
{
    public class AckEventArgs : EventArgs
    {
        private bool AckRecvived;

        public AckEventArgs(bool ackRecvived)
        {
            AckRecvived = ackRecvived;
        }

        public bool HasAck()
        {
            return AckRecvived;
        }
    }
}
