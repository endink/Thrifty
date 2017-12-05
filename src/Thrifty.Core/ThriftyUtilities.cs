using DotNetty.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Thrift.Transport;

namespace Thrifty
{
    public static class ThriftyUtilities
    {

        public static String ForDebugString(this IByteBuffer byteBuffer)
        {
            return String.Format($"buffer rb: {byteBuffer.ReadableBytes}, rix: {byteBuffer.ReaderIndex}");
        }

        public static ThriftyTransportException.ExceptionType ToThriftyError(this TTransportException.ExceptionType thriftType)
        {
            switch (thriftType)
            {
                case TTransportException.ExceptionType.AlreadyOpen:
                    return ThriftyTransportException.ExceptionType.AlreadyOpen;
                case TTransportException.ExceptionType.EndOfFile:
                    return ThriftyTransportException.ExceptionType.EndOfFile;
                case TTransportException.ExceptionType.Interrupted:
                    return ThriftyTransportException.ExceptionType.Interrupted;
                case TTransportException.ExceptionType.NotOpen:
                    return ThriftyTransportException.ExceptionType.NotOpen;
                case TTransportException.ExceptionType.TimedOut:
                    return ThriftyTransportException.ExceptionType.TimedOut;
                case TTransportException.ExceptionType.Unknown:
                default:
                    return ThriftyTransportException.ExceptionType.Unknown;
            }
        }

        public static String GetHost(this EndPoint endPoint)
        {
            if (endPoint is DnsEndPoint dp)
            {
                return dp.Host;
            }
            if (endPoint is IPEndPoint ip)
            {
                return ip.Address.ToString();
            }
            throw new ArgumentException($"cant get host from a {endPoint.GetType().Name}.");
        }

        /// <summary>
        /// 抛出无法处理的异常，例如堆栈溢出、算术溢出等）。
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static void ThrowIfNecessary(this Exception exception)
        {
            if (exception is OutOfMemoryException || exception is OverflowException || exception is InvalidCastException)
            {
                throw new Exception(exception.StackTrace, exception);
            }
        }

        public static TValue RemoveAndGet<TKey, TValue>(this IDictionary<TKey, TValue> instance, TKey key)
        {
            TValue result;
            if (instance.TryGetValue(key, out result))
            {
                if (instance.Remove(key))
                {
                    return result;
                }
            }
            return default(TValue);
        }

        public static int Hash(params Object[] a)
        {
            return Hash((IEnumerable<Object>)a);
        }

        public static int Hash(IEnumerable<Object> a)
        {
            if (a == null)
                return 0;

            int result = 1;

            foreach (Object element in a)
                result = 31 * result + (element == null ? 0 : element.GetHashCode());

            return result;
        }

        public static object GetDefaultValue(this Type type)
        {
            Guard.ArgumentNotNull(type, nameof(type));
            
            Expression<Func<object>> e = Expression.Lambda<Func<object>>(
                Expression.Convert(
                    Expression.Default(type), typeof(object))
            );

            // Compile and return the value.
            return e.Compile()();
        }

        public static IEnumerable<IPAddress> GetLocalIPV4Addresses()
        {
            var addresses = from item in NetworkInterface.GetAllNetworkInterfaces()
                            where item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                                  || item.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                            from ipc in item.GetIPProperties().UnicastAddresses
                            let address = ipc.Address
                            where address.AddressFamily == AddressFamily.InterNetwork
                            select address;
            return addresses.ToArray();
        }
    }
}
