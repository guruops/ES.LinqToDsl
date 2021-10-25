using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace GuruOps.ES.LinqToDsl.DAL.Utilities
{
    public static class EnumUtilities
    {
        public static List<T> GetList<T>() where T : struct
        {
            return Enum.GetValues(typeof(T))
                    .Cast<T>().ToList();
        }

        public static T? GetEnum<T>(int value) where T : struct
        {
            List<T> list = GetList<T>();
            bool filter(T c) => c.GetHashCode() == value;
            if (!list.Any(filter))
                return null;

            return list.FirstOrDefault(filter);
        }

        public static T? GetEnum<T>(string label) where T : struct
        {
            List<T> list = GetList<T>();
            bool filter(T c) => c.ToString().Trim().ToLower() == label?.Trim().ToLower();
            if (!list.Any(filter))
                return null;

            return list.FirstOrDefault(filter);
        }

        public static T? GetEnumByDescription<T>(string description) where T : struct
        {
            List<T> list = GetList<T>();
            bool filter(T c) => DescriptionAttr(c).Trim().ToLower() == description?.Trim().ToLower();
            if (!list.Any(filter))
                return null;

            return list.FirstOrDefault(filter);
        }

        public static T? GetEnumByAttribute<T, TAttribute>(string displayName, string property)
            where T : struct
            where TAttribute : Attribute
        {
            List<T> list = GetList<T>();
            bool filter(T c) => GetAttributeValue<T, TAttribute>(c, property).Trim().ToLower() == displayName?.Trim().ToLower();
            if (!list.Any(filter))
                return null;

            return list.FirstOrDefault(filter);
        }

        public static string DescriptionAttr<T>(T source)
        {
            FieldInfo fi = source.GetType().GetField(source.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
                return attributes[0].Description;
            else
                return source.ToString();
        }

        public static string GetAttributeValue<T, TAttribute>(T source, string property)
            where T : struct
            where TAttribute : Attribute
        {
            FieldInfo fi = source.GetType().GetField(source.ToString());

            TAttribute[] attributes = (TAttribute[])fi.GetCustomAttributes(typeof(TAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                var propertyInfo = attributes[0].GetType().GetProperty(property);
                if (propertyInfo != null)
                    return propertyInfo.GetValue(attributes[0])?.ToString();
            }

            return source.ToString();
        }

        public static T GetEnumAttributeOfType<T>(this Enum enumValue) where T : Attribute
        {
            Type type = enumValue.GetType();
            MemberInfo[] memInfo = type.GetMember(enumValue.ToString());
            object attr = memInfo[0].GetCustomAttributes(typeof(T), false).FirstOrDefault();
            return attr != null
                ? (T)attr
                : throw new Exception($"Type {nameof(enumValue)} does not have attribute {nameof(T)}.");
        }

        public static int Calculate<T>(List<T> enums) where T : struct
        {
            return (enums ?? new List<T>()).Distinct().Sum(c => c.GetHashCode());
        }

        public static List<T> GetEnums<T>(int value) where T : struct
        {
            List<T> result = new List<T>();

            foreach (T permission in GetList<T>().OrderByDescending(c => c.GetHashCode()))
            {
                if (permission.GetHashCode() > 0 && value / permission.GetHashCode() == 1)
                {
                    result.Add(permission);
                    value = value % permission.GetHashCode();
                }
            }

            return result;
        }
    }
}