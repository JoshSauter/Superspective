using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Saving;

namespace SuperspectiveUtils {
    public static class ReflectionExt {
        public static string GetReadableTypeName(this Type type) {
            if (!type.IsGenericType)
                return type.Name;

            string typeName = type.Name;
            int backtickIndex = typeName.IndexOf('`');
            if (backtickIndex > 0)
                typeName = typeName.Substring(0, backtickIndex);

            Type[] genericArgs = type.GetGenericArguments();
            string genericArgsJoined = string.Join(", ", genericArgs.Select(GetReadableTypeName));
            return $"{typeName}<{genericArgsJoined}>";
        }
        
        // Stops when we reach SuperspectiveObject, used for implicit save/load of fields
        public static IEnumerable<Type> TraverseTypeHierarchy(this Type type) {
            for (; type != null; type = type.BaseType) {
                if (type == typeof(SuperspectiveObject)) {
                    yield return type;
                    yield break;
                }
                yield return type;
            }
        }
        
        //...
        // here are methods described in the post 
        // http://dotnetfollower.com/wordpress/2012/12/c-how-to-set-or-get-value-of-a-private-or-internal-property-through-the-reflection/
        //...

        private static FieldInfo GetFieldInfo(Type type, string fieldName) {
            FieldInfo fieldInfo;
            do {
                fieldInfo = type.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            while (fieldInfo == null && type != null);
            return fieldInfo;
        }

        public static object GetFieldValue(this object obj, string fieldName) {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Type objType = obj.GetType();
            FieldInfo fieldInfo = GetFieldInfo(objType, fieldName);
            if (fieldInfo == null)
                throw new ArgumentOutOfRangeException("fieldName",
                    string.Format("Couldn't find field {0} in type {1}", fieldName, objType.FullName));
            return fieldInfo.GetValue(obj);
        }

        public static void SetFieldValue(this object obj, string fieldName, object val) {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Type objType = obj.GetType();
            FieldInfo fieldInfo = GetFieldInfo(objType, fieldName);
            if (fieldInfo == null)
                throw new ArgumentOutOfRangeException("fieldName",
                    string.Format("Couldn't find field {0} in type {1}", fieldName, objType.FullName));
            fieldInfo.SetValue(obj, val);
        }
    }
}