using System;

namespace GuruOps.ES.LinqToDsl.DAL.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InternalOnlyAttribute : Attribute
    {
        public bool InternalOnly { get; internal set; }

        public InternalOnlyAttribute(bool internalOnly = true)
        {
            InternalOnly = internalOnly;
        }
    }
}