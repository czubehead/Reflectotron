using System;
using System.Linq;

namespace Reflex
{
    /// <summary>
    ///     Extension methods for <see cref="Reflectotron" />
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        ///     basically a ToString
        /// </summary>
        /// <param name="mod"></param>
        /// <returns></returns>
        public static string Str(this Reflectotron.EKeyWords mod)
        {
            return mod.ToString().ToLower();
        }

        /// <summary>
        ///     Check whether the type has a public parameterless constructor
        /// </summary>
        /// <param name="type">type to check</param>
        /// <returns></returns>
        public static bool HasParamlessConstructor(this Type type)
        {
            return type.GetConstructors().Any(constructor => !constructor.GetParameters().Any());
        }
    }
}