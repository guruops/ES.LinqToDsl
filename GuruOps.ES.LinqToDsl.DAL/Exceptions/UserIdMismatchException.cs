using System;

namespace GuruOps.ES.LinqToDsl.DAL.Exceptions
{
    [Serializable]
    public class UserIdMismatchException : Exception
    {
        public UserIdMismatchException() { }

        public UserIdMismatchException(string message) : base(message) { }
    }
}