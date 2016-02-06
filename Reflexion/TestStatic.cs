using System;

namespace Reflex
{
    internal static class TestStatic
    {
        [Obsolete]
        public static int GetInt(this int i)
        {
            return 0;
        }
    }
}