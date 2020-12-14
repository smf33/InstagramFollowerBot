using System;
using Microsoft.Extensions.Logging;

namespace InstagramFollowerBot
{
    public class Program
    {
        private static int Main(string[] args)
        {
            ConsoleLogger logger = new ConsoleLogger();

            try
            {
                using FollowerBot bot = new FollowerBot(args, logger);
                try
                {
                    bot.Run();
                }
                catch
                {
                    bot.DebugDump();
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(default, ex, "## ENDED IN ERROR : {0}", ex.GetBaseException().Message);
                return -1;
            }

            return 0;
        }

        // Utility classes should not have public constructors
        protected Program()
        {
        }
    }
}