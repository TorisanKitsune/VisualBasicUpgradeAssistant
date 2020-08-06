using System;

namespace VisualBasicUpgradeAssistant.Core.Extensions
{
    internal class RandomDateTime
    {
        private readonly DateTime _start;

        private readonly DateTime _end;

        private readonly Random _gen;

        private readonly Int32 _range;

        public RandomDateTime(DateTime? startDateTime, DateTime? endDateTime)
        {
            _start = startDateTime ?? DateTime.Now.AddYears(-20);
            _end = endDateTime ?? DateTime.Now;
            _gen = RandomUtility.GetUniqueRandom();
            _range = (_end - _start).Days;
        }

        public DateTime Next()
        {
            return _start.AddDays(_gen.Next(_range)).AddHours(_gen.Next(0, 24)).AddMinutes(_gen.Next(0, 60))
                .AddSeconds(_gen.Next(0, 60));
        }
    }
}
