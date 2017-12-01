using System;
using Thrift;

namespace Thrifty.Samples.Common
{
    [ThriftService("service")]
    public interface IService
    {
        //twoway
        [ThriftMethod]
        Entity[] AllEntities();
        [ThriftMethod]
        Entity ChangeDay(Entity input, DayOfWeek day);
        [ThriftMethod]
        Entity ChangeStringValue(Entity input, string value);
        [ThriftMethod]
        Entity ChangeBoolValue(Entity input, bool value);
        [ThriftMethod]
        Entity ChangeByteNumber(Entity input, byte number);
        [ThriftMethod]
        Entity ChangeShortNumber(Entity input, short number);
        [ThriftMethod]
        Entity ChangeIntNumber(Entity input, int number);
        [ThriftMethod]
        Entity ChangeLongNumber(Entity input, long number);
        [ThriftMethod]
        Entity ChangeDoubleNumber(Entity input, double number);
        [ThriftMethod]
        Entity ChangeDateTime(Entity input, DateTime value);

        [ThriftMethod]
        int GetCount(Entity[] entities);
        [ThriftMethod]
        int GetMatchedDay(DayOfWeek day, Entity[] entities);
        [ThriftMethod]
        int GetMatchedStringValue(string value, Entity[] entities);
        [ThriftMethod]
        int GetMatchedBoolValue(bool value, Entity[] entities);
        [ThriftMethod]
        int GetMatchedByteNumber(byte number, Entity[] entities);
        [ThriftMethod]
        int GetMatchedShortNumber(short number, Entity[] entities);
        [ThriftMethod]
        int GetMatchedIntNumber(int number, Entity[] entities);
        [ThriftMethod]
        int GetMatchedLongNumber(long number, Entity[] entities);
        [ThriftMethod]
        int GetMatchedDoubleNumber(double number, Entity[] entities);

        //oneway
        [ThriftMethod(OneWay = true)]
        void Serialize(Entity[] entities);

        [ThriftMethod(OneWay = true)]
        void SerializeMatches(DayOfWeek day, Entity[] entities);

        [ThriftMethod(OneWay = true)]
        void SerializeMatchesStringValue(string value, Entity[] entities);

        [ThriftMethod(OneWay = true)]
        void SerializeMatchesBoolValue(bool value, Entity[] entities);

        [ThriftMethod(OneWay = true)]
        void SerializeMatchesByteNumber(byte number, Entity[] entities);

        [ThriftMethod(OneWay = true)]
        void SerializeMatchesShortNumber(short number, Entity[] entities);
        [ThriftMethod(OneWay = true)]
        void SerializeMatchesIntNumber(int number, Entity[] entities);
        [ThriftMethod(OneWay = true)]
        void SerializeMatchesLongNumber(long number, Entity[] entities);
        [ThriftMethod(OneWay = true)]
        void SerializeMatchesDoubleNumber(double number, Entity[] entities);

        [ThriftMethod(OneWay = true)]
        void ThrowException();
        [ThriftMethod]
        // [ThriftException(1231, "exception", typeof(MyException))]
        int ThrowExceptionWithReturnValue(bool isThrow);

        [ThriftMethod]
        Entity ReturnMyself(Entity e);
    }
}