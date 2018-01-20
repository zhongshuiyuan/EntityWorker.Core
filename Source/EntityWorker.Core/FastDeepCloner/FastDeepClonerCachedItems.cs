﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
//#if NETSTANDARD2_0 || NETSTANDARD1_3 || NETSTANDARD1_5

//#else
//using System.Reflection;
//using System.Reflection.Emit;
//#endif

namespace EntityWorker.Core.FastDeepCloner
{
    internal static class FastDeepClonerCachedItems
    {
        public delegate object ObjectActivator();
        private static readonly Dictionary<Type, Dictionary<string, IFastDeepClonerProperty>> CachedFields = new Dictionary<Type, Dictionary<string, IFastDeepClonerProperty>>();
        private static readonly Dictionary<Type, Dictionary<string, IFastDeepClonerProperty>> CachedPropertyInfo = new Dictionary<Type, Dictionary<string, IFastDeepClonerProperty>>();
        private static readonly Dictionary<Type, Type> CachedTypes = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, Func<object>> CachedConstructor = new Dictionary<Type, Func<object>>();
        private static readonly Dictionary<Type, ObjectActivator> CachedDynamicMethod = new Dictionary<Type, ObjectActivator>();


        public static void CleanCachedItems()
        {
            CachedFields.Clear();
            CachedTypes.Clear();
            CachedConstructor.Clear();
            CachedPropertyInfo.Clear();
            CachedDynamicMethod.Clear();
        }

        internal static Type GetIListType(this Type type)
        {
            if (CachedTypes.ContainsKey(type))
                return CachedTypes[type];
            if (type.IsArray)
                CachedTypes.Add(type, type.GetElementType());
            else
            {
                if (type.GenericTypeArguments.Any())
                    CachedTypes.Add(type, typeof(List<>).MakeGenericType(type.GenericTypeArguments.First()));
                else if (type.FullName.Contains("List`1"))
                    CachedTypes.Add(type, typeof(List<>).MakeGenericType(type.GetRuntimeProperty("Item").PropertyType));
                else CachedTypes.Add(type, type);
            }
            return CachedTypes[type];
        }

    

        internal static object Creator(this Type type)
        {
#if NETSTANDARD2_0 || NETSTANDARD1_3 || NETSTANDARD1_5
            if (CachedConstructor.ContainsKey(type))
                return CachedConstructor[type].Invoke();
            CachedConstructor.Add(type, Expression.Lambda<Func<object>>(Expression.New(type)).Compile());
            return CachedConstructor[type].Invoke();
#else
            if (CachedDynamicMethod.ContainsKey(type))
                return CachedDynamicMethod[type]();
            lock (CachedDynamicMethod)
            {
                var emptyConstructor = type.GetConstructor(Type.EmptyTypes);
                var dynamicMethod = new System.Reflection.Emit.DynamicMethod("CreateInstance", type, Type.EmptyTypes, true);
                System.Reflection.Emit.ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
                ilGenerator.Emit(System.Reflection.Emit.OpCodes.Nop);
                ilGenerator.Emit(System.Reflection.Emit.OpCodes.Newobj, emptyConstructor);
                ilGenerator.Emit(System.Reflection.Emit.OpCodes.Ret);
                CachedDynamicMethod.Add(type, (ObjectActivator)dynamicMethod.CreateDelegate(typeof(ObjectActivator)));

            }
            return CachedDynamicMethod[type]();
#endif
        }


        internal static bool GetField(this FieldInfo field, Dictionary<string, IFastDeepClonerProperty> properties)
        {
            if (!properties.ContainsKey(field.Name))
                properties.Add(field.Name, new FastDeepClonerProperty(field));
            return true;
        }

        internal static bool GetField(this PropertyInfo field, Dictionary<string, IFastDeepClonerProperty> properties)
        {
            if (!properties.ContainsKey(field.Name))
                properties.Add(field.Name, new FastDeepClonerProperty(field));
            return true;
        }

        internal static Dictionary<string, IFastDeepClonerProperty> GetFastDeepClonerFields(this Type primaryType)
        {
            if (!CachedFields.ContainsKey(primaryType))
            {
                var properties = new Dictionary<string, IFastDeepClonerProperty>();
                if (primaryType.GetTypeInfo().BaseType != null && primaryType.GetTypeInfo().BaseType.Name != "Object")
                {
                    primaryType.GetRuntimeFields().Where(x => x.GetField(properties)).ToList();
                    primaryType.GetTypeInfo().BaseType.GetRuntimeFields().Where(x => x.GetField(properties)).ToList();

                }
                else primaryType.GetRuntimeFields().Where(x => x.GetField(properties)).ToList();
                CachedFields.Add(primaryType, properties);
            }
            return CachedFields[primaryType];
        }


        internal static Dictionary<string, IFastDeepClonerProperty> GetFastDeepClonerProperties(this Type primaryType)
        {
            if (!CachedPropertyInfo.ContainsKey(primaryType))
            {
                var properties = new Dictionary<string, IFastDeepClonerProperty>();
                if (primaryType.GetTypeInfo().BaseType != null && primaryType.GetTypeInfo().BaseType.Name != "Object")
                {
                    primaryType.GetRuntimeProperties().Where(x => x.GetField(properties)).ToList();
                    primaryType.GetTypeInfo().BaseType.GetRuntimeProperties().Where(x => x.GetField(properties)).ToList();

                }
                else primaryType.GetRuntimeProperties().Where(x => x.GetField(properties)).ToList();
                CachedPropertyInfo.Add(primaryType, properties);
            }
            return CachedPropertyInfo[primaryType];
        }
    }
}