using System;
using System.Collections.Generic;
using System.Linq;

namespace GuruOps.ES.LinqToDsl.DAL.Exceptions
{
    [Serializable]
    public class BadRequestException : Exception
    {
        public BadRequestException() { }

        public BadRequestException(List<string> messages) : this(messages?.ToArray())
        {
        }

        public BadRequestException(params string[] messages)
            : base(string.Join(",", (messages?.ToList() ?? new List<string>()).Where(c => !string.IsNullOrWhiteSpace(c)).ToArray()))
        {
        }
    }
}