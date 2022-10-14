﻿using System;
using System.Collections.Generic;
using System.Threading;
using EltradeProtocol.Requests;

namespace EltradeProtocol
{
    // mynkow
    public class EltradeTransaction : IDisposable
    {
        private readonly Queue<RequestCallback> requests = new Queue<RequestCallback>();
        private EltradeFiscalDeviceDriver driver;

        public EltradeTransaction(EltradeFiscalDeviceDriver driver)
        {
            if (driver is null) throw new ArgumentNullException(nameof(driver));

            this.driver = driver;
        }

        public EltradeTransaction Enqueue(EltradeFiscalDeviceRequestPackage request)
        {
            return Enqueue(request, null);
        }

        public EltradeTransaction Enqueue(EltradeFiscalDeviceRequestPackage request, Func<EltradeFiscalDeviceResponsePackage, bool> then)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            if (then is null)
                requests.Enqueue(new RequestCallback(request));
            else
                requests.Enqueue(new RequestCallback(request, then));

            return this;
        }

        public void Commit()
        {
            while (requests.Count != 0)
            {
                var request = requests.Dequeue();
                EltradeFiscalDeviceResponsePackage response = null;
                var success = false;
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        response = driver.Send(request.Request);
                        if (response is null)
                            continue;

                        success = true;
                        break;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(10);
                    }
                }

                if (success == false)
                {
                    requests.Clear();
                    return;
                }

                if (request.Then is null)
                    continue;

                var @continue = request.Then(response);
                if (@continue == false)
                    requests.Clear();
            }
        }

        public void Dispose()
        {
            driver?.Dispose();
            driver = null;
        }

        private class RequestCallback
        {
            public RequestCallback(EltradeFiscalDeviceRequestPackage request)
            {
                Request = request ?? throw new ArgumentNullException(nameof(request));
            }

            public RequestCallback(EltradeFiscalDeviceRequestPackage request, Func<EltradeFiscalDeviceResponsePackage, bool> then)
            {
                Request = request ?? throw new ArgumentNullException(nameof(request));
                Then = then;
            }

            public EltradeFiscalDeviceRequestPackage Request { get; }
            public Func<EltradeFiscalDeviceResponsePackage, bool> Then { get; }
        }
    }
}
