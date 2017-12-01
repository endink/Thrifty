using System;

namespace Thrifty.MicroServices.Ribbon
{
    public interface IPing
    {
        bool IsAlive(Server server);
    }
}
