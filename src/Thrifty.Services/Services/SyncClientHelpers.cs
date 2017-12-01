using DotNetty.Buffers;
using Thrifty.Nifty.Client;
using Thrifty.Nifty.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thrift;

namespace Thrifty.Services
{
    internal static class SyncClientHelpers
    {
        ///<summary>
        ///Sends a single message synchronously, and blocks until the responses is received.
        ///</summary>
        ///<remarks>
        ///NOTE: the underlying transport may be non-blocking, in which case the blocking is simulated
        ///by waits instead of using blocking network operations.
        ///</remarks>
        ///<returns>The response, stored in a <see cref="IByteBuffer"/></returns>
        ///<exception cref="Thrift.TException">
        /// if an error occurs while serializing or sending the request or while receiving or de-serializing the response
        /// </exception>
        public static IByteBuffer SendSynchronousTwoWayMessage(this IRequestChannel channel, IByteBuffer request)
        {
            request?.Retain();
            IByteBuffer responseHolder = null;
            TException exceptionHolder = null;
            using (ManualResetEventSlim latch = new ManualResetEventSlim(false))
            {
                channel.SendAsynchronousRequest(request, false, new RequestListener(
                    onRequestSent: reqs => {
                        reqs?.Release();
                    },
                    onResponseReceive: response =>
                    {
                        responseHolder = response;
                        latch.Set();
                    },
                    onChannelError: e =>
                    {
                        exceptionHolder = e;
                        latch.Set();
                    }
                    ));

                if (!latch.Wait(TimeSpan.FromMinutes(10)))
                {
                    throw new TException("wait for response/error timeout.");
                }
            }
        
            if (exceptionHolder != null)
            {
                throw exceptionHolder;
            }

            return responseHolder;
        }

        ///<summary>
        ///Sends a single message synchronously, blocking until the send is complete. Does not wait for
        ///a response.
        ///<c>
        ///NOTE: the underlying transport may be non-blocking, in which case the blocking is simulated
        ///by waits instead of using blocking network operations. 
        ///</c>
        ///</summary>
        ///<exception cref="Thrift.TException">
        ///if a network or protocol error occurs while serializing or sending the request
        ///</exception>
        public static void SendSynchronousOneWayMessage(this IRequestChannel channel, IByteBuffer request)
        {
            TException exceptionHolder = null;
            request.Retain();
            using (ManualResetEventSlim latch = new ManualResetEventSlim(false))
            {
                channel.SendAsynchronousRequest(request, true, new RequestListener(
                    onRequestSent: reqs =>
                     {
                         reqs?.Release();
                         latch.Set();
                     },
                    //onResponseReceive: response =>
                    //{
                    //    communicatingComplete?.Invoke();
                    //    latch.Set();
                    //},
                    onChannelError: e =>
                    {
                        exceptionHolder = e;
                        latch.Set();
                    }));


                if (!latch.Wait(TimeSpan.FromMinutes(10)))
                {
                    throw new TException("wait for one-way request sent timeout.");
                }
            }
            if (exceptionHolder != null)
            {
                throw exceptionHolder;
            }
        }
    }
}
