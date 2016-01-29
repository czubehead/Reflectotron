namespace Reflex
{
    public static class Extensions
    {
        /// <summary>
        /// basically a ToString
        /// </summary>
        /// <param name="mod"></param>
        /// <returns></returns>
        public static string Str(this Reflectotron.EKeyWords mod)
        {
            switch (mod)
            {
                default:
                    return mod.ToString().ToLower();
            }
        }
    }
}
