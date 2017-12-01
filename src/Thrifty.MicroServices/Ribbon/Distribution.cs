using System;

namespace Thrifty.MicroServices.Ribbon
{
    class Distribution
    {
        private long _valuesCount = 0L;
        private double _sum = 0;
        private double _sumSquare = 0;
        private double _min = 0;
        private double _max = 0;
        public virtual void NoteValue(double value)
        {
            _valuesCount++;
            _sum += value;
            _sumSquare += value * value;
            if (_valuesCount == 1)
            {
                _min = value;
                _max = value;
            }
            if (value < _min)
            {
                _min = value;
            }
            if (value > _max)
            {
                _max = value;
            }
        }
        public virtual void Clear()
        {
            _valuesCount = 0L;
            _sum = 0;
            _sumSquare = 0;
            _min = 0;
            _max = 0;
        }

        public long ValuesCount { get { return _valuesCount; } }
        public double Average { get { return _valuesCount < 1 ? 0 : _sum / _valuesCount; } }
        public double Variance
        {
            get
            {
                if (_valuesCount < 2 || _sum == 0) return 0;
                var average = Average;
                return (_sumSquare / _valuesCount) - average * average;
            }
        }
        public double StdDev { get { return Math.Sqrt(Variance); } }
        public double Minimum { get { return _min; } }
        public double Maximum { get { return _max; } }
        public override string ToString() => $"{{Distribution:N={ValuesCount}: {Minimum}..{Average}..{Maximum}}}";
    }
}
