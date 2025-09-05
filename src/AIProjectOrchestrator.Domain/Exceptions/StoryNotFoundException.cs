using System;

namespace AIProjectOrchestrator.Domain.Exceptions
{
    public class StoryNotFoundException : Exception
    {
        public StoryNotFoundException() : base("The story was not found.")
        {
        }

        public StoryNotFoundException(string message) : base(message)
        {
        }

        public StoryNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}