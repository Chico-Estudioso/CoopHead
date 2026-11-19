using System;
using System.Collections.Generic;
using System.Reflection;

namespace Coophead.Plugin.GameIntegration
{
    /// <summary>
    /// Lectura por reflexión de miembros del juego. Coophead no compila contra Assembly-CSharp: todos los nombres
    /// se resuelven en tiempo de ejecución y, si no existen en la versión instalada, la función se desactiva.
    /// </summary>
    internal static class GameReflection
    {
        private static readonly Dictionary<Type, Dictionary<string, MemberInfo>> Cache = new Dictionary<Type, Dictionary<string, MemberInfo>>();

        /// <summary>Lee un campo o propiedad (público o privado, también heredado). Null si no existe.</summary>
        public static object ReadMember(object instance, string name)
        {
            if (instance == null)
            {
                return null;
            }

            Type type = instance.GetType();
            Dictionary<string, MemberInfo> members;
            if (!Cache.TryGetValue(type, out members))
            {
                members = new Dictionary<string, MemberInfo>();
                Cache[type] = members;
            }

            MemberInfo member;
            if (!members.TryGetValue(name, out member))
            {
                member = Find(type, name);
                members[name] = member;
            }

            var field = member as FieldInfo;
            if (field != null)
            {
                return field.GetValue(instance);
            }

            var property = member as PropertyInfo;
            return property != null ? property.GetValue(instance, null) : null;
        }

        /// <summary>Campo o propiedad de instancia (público o privado) en el tipo o sus bases, sin avisos en el log.</summary>
        private static MemberInfo Find(Type type, string name)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, Flags);
                if (field != null)
                {
                    return field;
                }

                PropertyInfo property = current.GetProperty(name, Flags);
                if (property != null && property.GetIndexParameters().Length == 0 && property.CanRead)
                {
                    return property;
                }
            }

            return null;
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
