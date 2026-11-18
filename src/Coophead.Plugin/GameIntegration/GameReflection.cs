using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Coophead.Plugin.GameIntegration
{
    /// <summary>
    /// Lectura por reflexión de miembros del juego. Coophead no compila contra Assembly-CSharp: todos los nombres
    /// se resuelven en tiempo de ejecución y, si no existen en la versión instalada, la función se desactiva.
    /// </summary>
    internal static class GameReflection
    {
        private static readonly Dictionary<string, MemberInfo> Cache = new Dictionary<string, MemberInfo>();

        /// <summary>Lee un campo o propiedad (público o privado, también heredado). Null si no existe.</summary>
        public static object ReadMember(object instance, string name)
        {
            if (instance == null)
            {
                return null;
            }

            Type type = instance.GetType();
            string key = type.FullName + "::" + name;
            MemberInfo member;
            if (!Cache.TryGetValue(key, out member))
            {
                member = (MemberInfo)AccessTools.Field(type, name) ?? AccessTools.Property(type, name);
                Cache[key] = member;
            }

            var field = member as FieldInfo;
            if (field != null)
            {
                return field.GetValue(instance);
            }

            var property = member as PropertyInfo;
            return property != null ? property.GetValue(instance, null) : null;
        }

        /// <summary>
        /// Valor numérico de un enum PlayerId del juego, leído de un miembro. -1 si no se puede leer.
        /// </summary>
        public static int ReadPlayerId(object instance, string memberName)
        {
            object value = ReadMember(instance, memberName);
            if (value == null)
            {
                return -1;
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}
