namespace EltradeProtocol.Requests
{
    public class GetCustomerInfo : EltradeFiscalDeviceRequestPackage
    {
        public GetCustomerInfo() : base(0x2b, "I0") { }
    }
}
