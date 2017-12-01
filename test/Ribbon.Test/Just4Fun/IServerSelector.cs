using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ribbon.Test.Just4Fun
{
    /// <summary>
    /// 服务器选择器
    /// </summary>
    public interface IServerSelector
    {
        /// <summary>
        /// 选择一个服务器
        /// </summary>
        /// <param name="servers">全部的服务器</param>
        /// <returns></returns>
        IServer Select(IEnumerable<IServer> servers);
    }
}
