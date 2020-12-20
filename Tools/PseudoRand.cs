using System;

namespace IFB
{
    internal static class PseudoRandom
    {
        private static readonly Random pseudoRand = new Random(); // this pseudorandom number generator is safe here.

        internal static int Next(int minIncluded, int maxincluded)
        {
            return pseudoRand.Next(minIncluded, maxincluded + 1);
        }
    }
}