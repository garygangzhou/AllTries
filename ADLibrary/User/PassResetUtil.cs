using System.DirectoryServices;
using ActiveDs;

namespace GZ.ActiveDirectoryLibrary
{
    static class DirectoryEntryExtensions
    {
        public static LargeInteger PwdLastSetLI(this  DirectoryEntry de)
        {
            return (LargeInteger)de.Properties["pwdLastSet"].Value;
        }

        public static bool IsForcePasswordSet(this  DirectoryEntry de)
        {
            {
                var withBlock = de.PwdLastSetLI();
                return 0 == withBlock.HighPart & 0 == withBlock.LowPart;
            }
        }

        public static void ForcePasswordSet(this  DirectoryEntry de, bool enabled)
        {
            var newLI = new LargeInteger();
            if (enabled)
            {
                newLI.HighPart = 0;
                newLI.LowPart = 0;
            }
            else
            {
                newLI.HighPart = -1;
                newLI.LowPart = -1;
            }

            de.Properties["pwdLastSet"].Value = newLI;
        }
    }
}