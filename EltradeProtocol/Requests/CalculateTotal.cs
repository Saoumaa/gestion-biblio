using System;
using System.Globalization;

namespace EltradeProtocol.Requests
{
    public class CalculateTotal : EltradeFiscalDeviceRequestPackage
    {
        public CalculateTotal() : base(0x35){}

        public CalculateTotal(string paymentType, decimal amount) : base(0x35)
        {
            AppendData(paymentType);
            AppendData((Math.Round(amount, 0)).ToString(CultureInfo.InvariantCulture));
        }

    }
}
