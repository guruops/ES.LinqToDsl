using System;

namespace GuruOps.ES.LinqToDsl.DAL.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TemplatePropertyAttribute : Attribute
    {
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public bool InternalOnly { get; internal set; }
        public bool TemplateProperty { get; internal set; }
        public bool QuestionProperty { get; internal set; }

        public TemplatePropertyAttribute(string name = "", string description = "", bool internalOnly = false, bool templateProperty = true, bool questionProperty = true)
        {
            Name = name;
            Description = description;
            InternalOnly = internalOnly;
            TemplateProperty = templateProperty;
            QuestionProperty = questionProperty;
        }
    }
}