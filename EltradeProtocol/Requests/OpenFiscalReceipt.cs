namespace EltradeProtocol.Requests
{
    public class OpenFiscalReceipt : EltradeFiscalDeviceRequestPackage
    {

        //public OpenFiscalReceipt(string operatorName, string usn) : base(0x90, $"{operatorName},{usn}") { }

        public OpenFiscalReceipt(string OPID, string OPNOM, string IFU, string TAXA, string TAXB, string TAXC, string TAXD,
                             string VT_RT) :
                            this(OPID, OPNOM, IFU, TAXA, TAXB, TAXC, TAXD, VT_RT, "", "", "", "") { }
        public OpenFiscalReceipt(string OPID, string OPNOM, string IFU, string TAXA, string TAXB, string TAXC, string TAXD,
                                 string VT_RT, string CIFU, string CNOM, string CEX) :
                                this(OPID, OPNOM, IFU, TAXA, TAXB, TAXC, TAXD, VT_RT, "", CIFU, CNOM, CEX) { }

        public OpenFiscalReceipt(string OPID, string OPNOM, string IFU, string TAXA, string TAXB, string TAXC, string TAXD,
                                 string VT_RT, string RN) :
                                this(OPID, OPNOM, IFU, TAXA, TAXB, TAXC, TAXD, VT_RT, RN, "", "", "") { }

        public OpenFiscalReceipt(string OPID, string OPNOM, string IFU, string TAXA, string TAXB, string TAXC, string TAXD,
                                 string VT_RT, string RN, string CIFU, string CNOM, string CEX) : base(0xc0)
        {
            AppendData(OPID);
            AppendData($",{Truncate(OPNOM, 32)}");
            AppendData($",{IFU}");
            AppendData($",{TAXA}");
            AppendData($",{TAXB}");
            AppendData($",{TAXC}");
            AppendData($",{TAXD}");
            AppendData($",{VT_RT}");

            if (VT_RT != "FV" || VT_RT != "CV" || VT_RT != "EV" || VT_RT != "EC" )
            {
                AppendData($",{RN}");
            }

            if (CIFU != "")
            {
                AppendData($",{CIFU}");
                AppendData($",{CNOM}");
            }

            if (CEX != "")
                AppendData($",{CEX}");
        }
    }
}
