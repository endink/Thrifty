using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using Thrifty.Samples.Common;
using Thrift;

namespace Thrifty.Samples
{
    internal class ServiceImpl : IService
    {
        public Entity[] AllEntities() => new[] {
            new Entity
            {
                Day = DayOfWeek.Monday,
                BoolValue = false,
                ByteNumber = 56,
                ShortNumber = 5467,
                IntNumber = 123456,
                LongNumber = 123456123456,
                DoubleNumber = 123.456,
                StringValue = "abc",
                Guid = Guid.NewGuid(),
                GuidArray = new []{Guid.NewGuid(), Guid.NewGuid()}
            }, new Entity
            {
                Day = DayOfWeek.Friday,
                BoolValue = false,
                ByteNumber = 56,
                ShortNumber = 5466,
                IntNumber = 123457,
                LongNumber = 123456123457,
                DoubleNumber = 123.457,
                StringValue = "abd",
                Guid = Guid.NewGuid(),
                GuidArray = new []{Guid.NewGuid(), Guid.NewGuid()}
            },new Entity
            {
                Day = DayOfWeek.Friday,
                BoolValue = true,
                ByteNumber = 56,
                ShortNumber = 5466,
                IntNumber = 123457,
                LongNumber = 123456123457,
                DoubleNumber = 123.457,
                StringValue = "abe",
                Guid = Guid.NewGuid(),
                GuidArray = new []{Guid.NewGuid(), Guid.NewGuid()}
            },new Entity
            {
                Day = DayOfWeek.Sunday,
                BoolValue = false,
                ByteNumber = 56,
                ShortNumber = 5467,
                IntNumber = 123456,
                LongNumber = 123456123456,
                DoubleNumber = 123.456,
                StringValue = "abf",
                Guid = Guid.NewGuid(),
                GuidArray = new []{Guid.NewGuid(), Guid.NewGuid()}
            },new Entity
            {
                Day = DayOfWeek.Sunday,
                BoolValue = false,
                ByteNumber = 56,
                ShortNumber = 5466,
                IntNumber = 123457,
                LongNumber = 123456123457,
                DoubleNumber = 123.457,
                StringValue = "abh",
                Guid = Guid.NewGuid(),
                GuidArray = new []{Guid.NewGuid(), Guid.NewGuid()}
            },new Entity
            {
                Day = DayOfWeek.Tuesday,
                BoolValue = false,
                ByteNumber = 56,
                ShortNumber = 5467,
                IntNumber = 123456,
                LongNumber = 123456123456,
                DoubleNumber = 123.456,
                StringValue = "abg",
                Guid = Guid.NewGuid(),
                GuidArray = new []{Guid.NewGuid(), Guid.NewGuid()}
            }
        };

        private static T Change<T>(T input, Action<T> action)
        {
            action(input);
            return input;
        }

        public Entity ChangeDay(Entity input, DayOfWeek day) => Change(input, s => s.Day = day);
        public Entity ChangeStringValue(Entity input, string value) => Change(input, s => s.StringValue = value);
        public Entity ChangeBoolValue(Entity input, bool value) => Change(input, s => s.BoolValue = value);
        public Entity ChangeByteNumber(Entity input, byte number) => Change(input, s => s.ByteNumber = number);
        public Entity ChangeShortNumber(Entity input, short number) => Change(input, s => s.ShortNumber = number);
        public Entity ChangeIntNumber(Entity input, int number) => Change(input, s => s.IntNumber = number);
        public Entity ChangeLongNumber(Entity input, long number) => Change(input, s => s.LongNumber = number);
        public Entity ChangeDoubleNumber(Entity input, double number) => Change(input, s => s.DoubleNumber = number);
        public Entity ChangeDateTime(Entity input, DateTime value) => Change(input, s => s.Now = value);

        public int GetCount(Entity[] entities) => entities?.Length ?? 0;
        public int GetMatchedDay(DayOfWeek day, Entity[] entities) => entities?.Count(x => x.Day == day) ?? 0;
        public int GetMatchedStringValue(string value, Entity[] entities) => entities?.Count(x => x.StringValue == value) ?? 0;
        public int GetMatchedBoolValue(bool value, Entity[] entities) => entities?.Count(x => x.BoolValue == value) ?? 0;
        public int GetMatchedByteNumber(byte number, Entity[] entities) => entities?.Count(x => x.ByteNumber == number) ?? 0;
        public int GetMatchedShortNumber(short number, Entity[] entities) => entities?.Count(x => x.ShortNumber == number) ?? 0;
        public int GetMatchedIntNumber(int number, Entity[] entities) => entities?.Count(x => x.IntNumber == number) ?? 0;
        public int GetMatchedLongNumber(long number, Entity[] entities) => entities?.Count(x => x.LongNumber == number) ?? 0;
        public int GetMatchedDoubleNumber(double number, Entity[] entities) => entities?.Count(x => x.DoubleNumber == number) ?? 0;

        private static void SerializeToFile(IEnumerable<Entity> entities)
        {
            if (entities == null) return;
            //Thread.Sleep(1000 * 0);
        }

        public void Serialize(Entity[] entities) => SerializeToFile(entities);

        public void SerializeMatches(DayOfWeek day, Entity[] entities) => SerializeToFile(entities?.Where(x => x.Day == day));
        public void SerializeMatchesStringValue(string value, Entity[] entities) => SerializeToFile(entities?.Where(x => x.StringValue == value));
        public void SerializeMatchesBoolValue(bool value, Entity[] entities) => SerializeToFile(entities?.Where(x => x.BoolValue == value));
        public void SerializeMatchesByteNumber(byte number, Entity[] entities) => SerializeToFile(entities?.Where(x => x.ByteNumber == number));
        public void SerializeMatchesShortNumber(short number, Entity[] entities) => SerializeToFile(entities?.Where(x => x.ShortNumber == number));
        public void SerializeMatchesIntNumber(int number, Entity[] entities) => SerializeToFile(entities?.Where(x => x.IntNumber == number));
        public void SerializeMatchesLongNumber(long number, Entity[] entities) => SerializeToFile(entities?.Where(x => x.LongNumber == number));
        public void SerializeMatchesDoubleNumber(double number, Entity[] entities) => SerializeToFile(entities?.Where(x => x.DoubleNumber == number));


        public void ThrowException() => throw new MyException("ThrowException");
        public int ThrowExceptionWithReturnValue(bool isThrow) => isThrow ? throw new MyException("ThrowExceptionWithReturnValue") : 123456;

        public Entity ReturnMyself(Entity e) => e;
    }
}
