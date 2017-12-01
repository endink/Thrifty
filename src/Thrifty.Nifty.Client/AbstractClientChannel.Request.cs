using DotNetty.Common.Utilities;
using Thrifty.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Client
{
    partial class AbstractClientChannel
    {

        /**
         * Bundles the details of a client request that has started, but for which a response hasn't
         * yet been received (or in the one-way case, the send operation hasn't completed yet).
         */
        private class Request
        {

            public Request(IListener listener)
            {
                this.Listener = listener;
            }


            public IListener Listener { get; }
            public ITimeout SendTimeout { get; set; }
            public ITimeout ReceiveTimeout { get; set; }

            public ITimeout ReadTimeout { get; set; }
        }
    }
}
