using System;
using System.Text;
using EltradeProtocol.Requests;

namespace EltradeProtocol
{
    public static class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.GetEncoding("utf-8");

            Print();

            Console.ReadLine();
            
        }

        static void Print()
        {
            using (var transaction = EltradeFiscalDeviceDriver.GetInstance().BeginTransaction())
            {
                string response = "";
                //transaction.Enqueue(new SetDateTime());
                transaction.Enqueue(new GetPrinterDiagnosticInfo(), x =>
                {
                    Console.WriteLine("C1h");
                    Console.WriteLine(x.GetHumanReadableData());
                    response = x.GetHumanReadableData();
                    return true;
                });

                transaction.Commit();
                var responseElements = response.Split(',');
                string IFU = responseElements[1];
                string TAXA = responseElements[6];
                string TAXB = responseElements[7];
                string TAXC = responseElements[8];
                string TAXD = responseElements[9];
                
                //Command C2H
                transaction.Enqueue(new GetPrinterDiagnosticInfoServer(), x =>
                {
                    Console.WriteLine("C2h");
                    Console.WriteLine(x.GetHumanReadableData());
                    response = x.GetHumanReadableData();
                    return true;
                });

                transaction.Commit();

                //Command 2BH
                transaction.Enqueue(new GetCustomerInfo(), x =>
                {
                    Console.WriteLine("2Bh");
                    Console.WriteLine(x.GetHumanReadableData());
                    response = x.GetHumanReadableData();
                    return true;
                });

                transaction.Commit();


              
            }
        }
    }
}
