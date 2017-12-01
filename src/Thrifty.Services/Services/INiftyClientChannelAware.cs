using Thrifty.Nifty.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Services
{
    public interface INiftyClientChannelAware
    {
        INiftyClientChannel ClientChannel { get; }
    }
}
