using Nest;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Linq.Nest
{
    public static class FieldFactory
    {
        public static Field Create(MemberExpression node, bool isToLower = false)
        {
            EnsureSupportableMember(node);
            node = ClearExpressionIfItNullable(node);
            var fieldName = string.Join(".", node.ToString().Split('.').Skip(1)).ToCamelCase();
            if ((!isToLower && (IsStringProperty(node) || IsEnumerableString(node) || IsStringEnum(node)))
                || (isToLower && node.Member.Name.ToLower().Contains("email")))
            {
                fieldName = $"{fieldName}.keyword";
            }
            return new Field(fieldName);
        }

        private static string ToCamelCase(this string name)
        {
            var results = new List<string>();
            foreach (var property in name.Split('.'))
            {
                var firstLetter = property.Substring(0, 1).ToLower();
                results.Add($"{firstLetter}{property.Substring(1)}");
            }
            return string.Join('.', results);
        }

        private static MemberExpression ClearExpressionIfItNullable(MemberExpression node)
        {
            var memberType = node.Member.DeclaringType;
            if (memberType != null &&
                memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                node.Member.Name == "Value")
            {
                node = (MemberExpression)node.Expression;
            }
            return node;
        }

        private static void EnsureSupportableMember(MemberExpression expression)
        {
            if (expression.Member.DeclaringType.Assembly == typeof(DateTime).Assembly)
            {
                var memberType = expression.Member.DeclaringType;
                if (memberType != null &&
                    (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                {
                    memberType = memberType.GetGenericArguments()[0];
                }
                if (memberType == typeof(DayOfWeek) || memberType == typeof(DateTime))
                {
                    return;
                }
                throw new NotSupportedException($"Most likely you are trying to access primitive's member. We haven't support it yet. \n Expression is {expression}.");
            }

        }

        private static bool IsStringProperty(MemberExpression node)
        {
            var result = node.Type == typeof(string);
            return result;
        }

        private static bool IsEnumerableString(MemberExpression node)
        {
            //ToDo: compare with list<string> for now
            var result = node.Type == typeof(List<string>);
            return result;
        }

        private static bool IsStringEnum(MemberExpression node)
        {
            var propertyType = (node.Member as PropertyInfo)
                .PropertyType;
            var attribute = (JsonConverterAttribute)Attribute.GetCustomAttribute(
                propertyType,
                typeof(JsonConverterAttribute));
            if (attribute != null && attribute.ConverterType == typeof(StringEnumConverter))
            {
                return true;
            }
            return false;
        }

    }
}
