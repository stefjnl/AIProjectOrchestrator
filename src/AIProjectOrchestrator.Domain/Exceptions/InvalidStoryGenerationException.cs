using System;

namespace AIProjectOrchestrator.Domain.Exceptions
{
    public class InvalidStoryGenerationException : Exception
    {
        public InvalidStoryGenerationException() : base("The story generation is invalid.")
        {
        }

        public InvalidStoryGenerationException(string message) : base(message)
        {
        }

        public InvalidStoryGenerationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}