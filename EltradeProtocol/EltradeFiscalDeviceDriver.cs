﻿using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using EltradeProtocol.Requests;
using log4net;

namespace EltradeProtocol
{
    public class EltradeFiscalDeviceDriver : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(EltradeFiscalDeviceDriver));

        private const int SynWaitMilliseconds = 60;
        private static readonly object mutex = new object();
        private static readonly object instanceMutex = new object();

        private static string lastWorkingPort = string.Empty;
        private static EltradeFiscalDeviceDriver instance;

        private SerialPort serialPort;
        private bool reading;
        private EltradeFiscalDeviceResponsePackage response;

        private EltradeFiscalDeviceDriver()
        {
            FindFiscalDevicePort();
        }

        public static EltradeFiscalDeviceDriver GetInstance()
        {
            if (ReferenceEquals(null, instance))
            {
                lock (instanceMutex)
                {
                    if (ReferenceEquals(null, instance))
                    {
                        instance = new EltradeFiscalDeviceDriver();
                    }
                }
            }

            return instance;
        }

        public static string IFU { get; private set; }

        public static string NIM { get; private set; }

        public static string DT { get; private set; }

        public static string TC { get; private set; }

        public static string FVC { get; private set; }

        public static string FRC { get; private set; }

        public static string TAXA { get; private set; }

        public static string TAXB { get; private set; }

        public static string TAXC { get; private set; }

        public static string TAXD { get; private set; }

        public EltradeTransaction BeginTransaction()
        {
            return new EltradeTransaction(this);
        }

        public EltradeFiscalDeviceResponsePackage Send(EltradeFiscalDeviceRequestPackage package)
        {
            if (ReferenceEquals(null, package)) throw new ArgumentNullException(nameof(package));

            response = EltradeFiscalDeviceResponsePackage.Empty;
            try
            {
                serialPort.DataReceived += Read;

                var bytes = package.Build(true);
                reading = true;
                log.Debug($"0x{package.Command.ToString("x2").ToUpper()} => {package.GetType().Name} {package.DataString}");
                serialPort.Write(bytes, 0, bytes.Length);

                var wait = 0;
                while (reading && wait++ < 500) // mynkow
                    Thread.Sleep(10); // KnowHow: https://social.msdn.microsoft.com/Forums/en-US/ce8ce1a3-64ed-4f26-b9ad-e2ff1d3be0a5/serial-port-hangs-whilst-closing?forum=Vsexpressvcs
                Console.WriteLine("Wait 500ms");
                serialPort.DataReceived -= Read;

                Thread.Sleep(500);
                Console.WriteLine("Wait 500ms");

                serialPort.DataReceived -= Read;

                if (response?.Data?.Length != 0)
                    log.Debug($"0x{response.Command.ToString("x2").ToUpper()} Response => {response.GetHumanReadableData()}");
            }
            catch (IOException ex)
            {
                log.Warn("Repopening port", ex);
                response = null;//mynkow
                Dispose();
                FindFiscalDevicePort();
            }
            catch (Exception ex)
            {
                log.Error("PAFA!", ex);
                response = null;//mynkow
                throw;
            }

            return response;
        }

        private void FindFiscalDevicePort()
        {
            byte[] bytes = new GetPrinterDiagnosticInfo().Build();

            if (CheckPortConnectivity(lastWorkingPort, bytes))
            {
                return; // We have a response and we are happy
            }
            else
            {
                foreach (var portName in SerialPort.GetPortNames())
                {
                    Console.WriteLine("COM PORT CHECKED : " + portName);
                    if (CheckPortConnectivity(portName, bytes))
                        return;  // We have a response and we are happy
                }

            }

            throw new InvalidOperationException("Unable to connect to fiscal device.");
        }

        private bool CheckPortConnectivity(string portName, byte[] bytes)
        {
            log.Debug("Start CheckPortConnectivity");
            if (string.IsNullOrEmpty(portName))
                return false;

            try
            {
                if (ReferenceEquals(null, serialPort) || serialPort.PortName != portName)
                {
                    if (serialPort?.PortName != portName)
                    {
                        log.Debug($"Disposing old port {serialPort?.PortName}");
                        serialPort?.Dispose();
                    }

                    log.Debug($"Creating serial port {portName}");
                    serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);

                    serialPort.ReadTimeout = 500;
                    serialPort.WriteTimeout = 500;
                    serialPort.Encoding = Encoding.ASCII;
                    serialPort.ErrorReceived += SerialPort_ErrorReceived;
                }

                var attempts = 0;
                while (attempts < 5)
                {
                    try
                    {
                        OpenPort();

                        serialPort.Write(bytes, 0, bytes.Length);
                        Thread.Sleep(SynWaitMilliseconds);

                        var buffer = new byte[serialPort.ReadBufferSize];
                        var readBytes = serialPort.Read(buffer, 0, serialPort.ReadBufferSize);

                        var response = new EltradeFiscalDeviceResponsePackage(buffer.Take(readBytes).ToArray()).GetHumanReadableData();
                        if (string.IsNullOrEmpty(response) == false)
                        {
                            lock (mutex)
                            {
                                lastWorkingPort = portName;
                                //Console.WriteLine(response);
                                var responseElements = response.Split(',');
                                NIM = responseElements[0];
                                IFU = responseElements[1];
                                DT = responseElements[2];
                                TC = responseElements[3];
                                FVC = responseElements[4];
                                FRC = responseElements[5];
                                TAXA = responseElements[6];
                                TAXB = responseElements[7];
                                TAXC = responseElements[8];
                                TAXD = responseElements[9];

                                log.Info($"---------- Fiscal device port {lastWorkingPort}, serial number {NIM}, fiscal number {IFU} ----------");
                            }

                            log.Debug("End CheckPortConnectivity");
                            return true;
                        }

                        attempts++;
                    }
                    catch (TimeoutException)
                    {
                        lock (mutex)
                        {
                            lastWorkingPort = string.Empty;
                            NIM = string.Empty;
                            IFU = string.Empty;
                            DT  = string.Empty;
                            TC  = string.Empty;
                            FVC = string.Empty;
                            FRC = string.Empty;
                            TAXA = string.Empty;
                            TAXB = string.Empty;
                            TAXC = string.Empty;
                            TAXD = string.Empty;

                        }

                        log.Debug("End CheckPortConnectivity. Timeout.");
                        return false;
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message, ex);
                        if (attempts >= 5)
                        {
                            log.Debug("End CheckPortConnectivity. Error.");
                            throw;
                        }

                        attempts++;
                        Thread.Sleep(200);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
            }

            log.Error($"Unable to find fiscal device on port {portName}. Check cable connection!");
            return false;
        }

        private void OpenPort()
        {
            log.Debug($"Start OpenPort. {serialPort.PortName}, {serialPort.IsOpen}");
            if (serialPort.IsOpen == false)
            {
                var attempts = 0;
                while (attempts < 5)
                {
                    try
                    {
                        serialPort.Open();
                        log.Debug($"serialPort.Open()");
                        attempts = 0;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (attempts >= 5)
                        {
                            log.Debug($"End OpenPort. Error {ex.Message}");
                            throw;
                        }

                        attempts++;
                        Thread.Sleep(200);
                    }
                }
            }

            if (serialPort.IsOpen)
            {
                log.Debug($"OpenPort. Discarding buffers");
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
            }

            log.Debug($"End OpenPort");
        }

        private void ClosePort()
        {
            log.Debug($"Start ClosePort");
            if (serialPort is null)
            {
                log.Debug($"End ClosePort. serial port is null");
                return;
            }

            if (serialPort.IsOpen)
            {
                try
                {
                    serialPort.Close();
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message, ex);
                }
            }

            log.Debug($"End ClosePort");
        }

        private void Read(object sender, SerialDataReceivedEventArgs e)
        {
            var serialPortSender = sender as SerialPort;

            var buffer = new byte[serialPortSender.ReadBufferSize];
            while (reading)
            {
                try
                {
                    buffer = new byte[serialPortSender.ReadBufferSize];
                    var readBytes = serialPortSender.Read(buffer, 0, serialPortSender.ReadBufferSize);
                    response = new EltradeFiscalDeviceResponsePackage(buffer.Take(readBytes).ToArray());
                    if (response.Printing)
                    {
                        Thread.Sleep(SynWaitMilliseconds);
                        continue;
                    }
                    else
                    {
                        reading = false;
                    }
                }
                catch (TimeoutException ex)
                {
                    log.Error(ex.Message, ex);
                    response = null;//mynkow
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message, ex);
                    response = null;//mynkow
                    reading = false;
                }
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            log.Error($"SerialPort_ErrorReceived {e.EventType}");
        }

        public void Dispose()
        {
            log.Debug($"Start Dispose");
            ClosePort();
            if (serialPort is null == false)
            {
                serialPort.ErrorReceived -= SerialPort_ErrorReceived;

                try
                {
                    serialPort.Dispose();
                }
                catch (Exception) { }

                serialPort = null;
            }

            log.Debug($"Instance is null {instance is null}");
            if (instance is null == false)
                lock (instanceMutex)
                {
                    if (instance is null == false)
                        instance = null;
                }

            log.Debug($"End Dispose");
        }

        public static PingResult Ping()
        {
            try
            {
                using (var driver = GetInstance())
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var response = driver.Send(new SetDateTime());
                        if (response?.Package.Length == 0)
                            continue;
                        else
                            return new PingResult();
                    }
                }

                return new PingResult("SetDateTime: Response package length is 0");
            }
            catch (Exception ex)
            {
                return new PingResult(ex);
            }
        }
    }

    public class PingResult
    {
        public PingResult()
        {
        }

        public PingResult(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public PingResult(Exception exception)
        {
            Exception = exception;
        }

        public bool Success { get { return ReferenceEquals(null, Exception) && string.IsNullOrEmpty(ErrorMessage); } }
        public Exception Exception { get; private set; }
        public string ErrorMessage { get; private set; }
    }
}
