using System;
using System.Runtime.Serialization;

namespace InstagramFollowerBot
{
    public class FollowerBotException : Exception
    {
        public FollowerBotException()
        {
        }

        public FollowerBotException(string message) : base(message)
        {
        }

        public FollowerBotException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FollowerBotException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}