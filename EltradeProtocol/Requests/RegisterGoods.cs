using System;
using System.Globalization;

namespace EltradeProtocol.Requests
{
    public class RegisterGoods : EltradeFiscalDeviceRequestPackage
    {
        public RegisterGoods(string articleName, string articleDescription, char taxType, decimal unitPrice) :
                        this(articleName, articleDescription, taxType, unitPrice, 1, false, 0, "", false, 0, ""){ }

        public RegisterGoods(string articleName, string articleDescription, char taxType, decimal unitPrice, decimal quantity) :
                        this(articleName, articleDescription, taxType, unitPrice, quantity, false, 0, "", false, 0, "") { }

        public RegisterGoods(string articleName, string articleDescription, char taxType, decimal unitPrice, decimal quantity, bool IsTS, decimal TS, string TSDESC) :
                        this(articleName, articleDescription, taxType, unitPrice, quantity, IsTS, TS, TSDESC, false, 0, "") { }


        public RegisterGoods(string articleName, string articleDescription, char taxType, decimal unitPrice, decimal quantity = 1, bool IsTS = false,
                            decimal TS = 0, string TSDESC = "", bool IsPO = false,  decimal PRORIG = 0, string PRDESC = "") : base(0x31)
        {
            AppendData(Truncate(articleName, 64));
            AppendData(LineFeed);
            AppendData(Truncate(articleDescription, 24));
            AppendData(Tab);
            AppendData(taxType.ToString());
            AppendData(Math.Round(unitPrice, 0).ToString(CultureInfo.InvariantCulture));

            if (quantity != 1)
                AppendData($"*{Math.Round(quantity, 2).ToString(CultureInfo.InvariantCulture)}");

            if (IsTS)
            {
                AppendData($";{Math.Round(TS, 0).ToString(CultureInfo.InvariantCulture)}");
                AppendData($",{TSDESC}");
            }

            if (IsPO)
            {
                AppendData(Tab);
                AppendData($"{Math.Round(PRORIG, 0).ToString(CultureInfo.InvariantCulture)}");
                AppendData($",{PRDESC}");
            }

        }
    }
}
