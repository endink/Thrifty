using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Thrifty.Nifty.Ssl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Core
{
    public interface IConnectionContext
    {
        /// <summary>
        /// 获取客户端远程调用的地址。
        /// </summary>
        /// <returns></returns>
        EndPoint RemoteAddress { get; }

        /// <summary>
        /// 返回连接的 Ssl Session，如果当前连接未使用 SSL，必须返回 null。
        /// </summary>
        /// <returns></returns>
        SslSession SslSession { get; }

        /// <summary>
        /// 获取连接中的附加属性。
        /// </summary>
        /// <param name="attributeName">属性名称。</param>
        /// <returns></returns>
        Object GetAttribute(String attributeName);

        /// <summary>
        /// 设置连接中的附加属性。
        /// </summary>
        /// <param name="attributeName">要设置的属性名称。</param>
        /// <param name="value">要设置的属性值，不能为 null。</param>
        /// <returns></returns>
       Object SetAttribute(String attributeName, Object value);

        /// <summary>
        /// 移除链接中的附加属性。
        /// </summary>
        /// <param name="attributeName">要移除的属性名称。</param>
        /// <returns>返回被移除的属性，如果属性不存在返回 null。</returns>
        Object RemoveAttribute(String attributeName);

        /// <summary>
        /// 或许连接中的附加属性。
        /// </summary>
        IEnumerable<KeyValuePair<String, Object>> Attributes { get; }
    }

    internal static class ConnectionContextExtensions
    {
        /// <summary>
        /// 获取当前通道中的 <see cref="NiftyConnectionContext"/> 上下文。
        /// 如果不存在，将抛出异常。
        /// </summary>
        /// <param name="channel">要从中获取上下文的通道。</param>
        /// <exception cref="NiftyException">如果通道（Attribute）中不存在 <see cref="NiftyConnectionContext"/> ，将抛出异常。 </exception>
        /// <returns></returns>
        public static IConnectionContext GetConnectionContext(this IChannel channel)
        {
            AttributeKey<NiftyConnectionContext> key =
                AttributeKey<NiftyConnectionContext>.ValueOf(ConnectionContextHandler.NiftyConnectionContextKey);
            IConnectionContext context = (IConnectionContext)
                    channel.Pipeline
                    .Context<ConnectionContextHandler>()
                    .GetAttribute(key).Get();
            if (context == null)
            {
                throw new NiftyException($"{nameof(IConnectionContext)} 没有被添加到 {channel.GetType().Name}。");
            }
            return context;
        }
    }
}
