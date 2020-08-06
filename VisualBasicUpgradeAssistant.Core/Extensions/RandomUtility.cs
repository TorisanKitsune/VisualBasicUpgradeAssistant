using System;

namespace VisualBasicUpgradeAssistant.Core.Extensions
{
    public static class RandomUtility
    {
        public static DateTime GetRandomDateTime(DateTime? startDataTime = null, DateTime? endDataTime = null)
        {
            return new RandomDateTime(startDataTime, endDataTime).Next();
        }

        public static Random GetUniqueRandom()
        {
            return new Random(Guid.NewGuid().GetHashCode());
        }
    }
}
