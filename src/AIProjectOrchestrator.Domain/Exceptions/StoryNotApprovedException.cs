using System;

namespace AIProjectOrchestrator.Domain.Exceptions
{
    public class StoryNotApprovedException : Exception
    {
        public StoryNotApprovedException() : base("The story is not approved yet.")
        {
        }

        public StoryNotApprovedException(string message) : base(message)
        {
        }

        public StoryNotApprovedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}