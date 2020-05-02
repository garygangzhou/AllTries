using System;
using System.Collections.Generic;

namespace GZ.ActiveDirectoryLibrary.Comparer
{  
    public class ADObjectComparer : IComparer<ADObject>
    {
        public int Compare(ADObject x, ADObject y)
        {
            // Compare by object type first and then by name
            // ORDER = Users, Groups, OU (reversed by the method that is calling this)
            // If same type than compare by name
            if ((x.Type == ADObjectTypes.BuiltInDomain || x.Type == ADObjectTypes.Container || x.Type == ADObjectTypes.OU) 
                && (y.Type == ADObjectTypes.BuiltInDomain || y.Type == ADObjectTypes.Container || y.Type == ADObjectTypes.OU))
            {
                return string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
            }

            if (x.Type == y.Type)
            {
                return string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
            }

            if (x.Type == ADObjectTypes.BuiltInDomain || x.Type == ADObjectTypes.Container || x.Type == ADObjectTypes.OU)
            {
                if (y.Type == ADObjectTypes.Group || y.Type == ADObjectTypes.User)
                {
                    return -1;
                }
            }

            if (x.Type == ADObjectTypes.Group)
            {
                if (y.Type == ADObjectTypes.BuiltInDomain || y.Type == ADObjectTypes.Container || y.Type == ADObjectTypes.OU)
                {
                    return 1;
                }
                else if (y.Type == ADObjectTypes.Group)
                {
                    return string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
                }
                else
                {
                    return -1;
                }
            }

            if (x.Type == ADObjectTypes.User)
            {
                if (y.Type != ADObjectTypes.User)
                {
                    return 1;
                }
                else
                {
                    return string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
                }
            }

            return default;
        }

        public static int Compare2(ADObject x, ADObject y)
        {
            //TODO: change it to switch. need c# 8.0
    
            // Compare by object type first and then by name
            // ORDER = Users, Groups, OU (reversed by the method that is calling this)
            // If same type than compare by name
            if ((x.Type == ADObjectTypes.BuiltInDomain || x.Type == ADObjectTypes.Container || x.Type == ADObjectTypes.OU)
                && (y.Type == ADObjectTypes.BuiltInDomain || y.Type == ADObjectTypes.Container || y.Type == ADObjectTypes.OU))
            {
                return string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
            }

            if (x.Type == y.Type)
            {
                return string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
            }

            if (x.Type == ADObjectTypes.BuiltInDomain || x.Type == ADObjectTypes.Container || x.Type == ADObjectTypes.OU)
            {
                if (y.Type == ADObjectTypes.Group || y.Type == ADObjectTypes.User)
                {
                    return -1;
                }
            }

            if (x.Type == ADObjectTypes.Group)
            {
                if (y.Type == ADObjectTypes.BuiltInDomain || y.Type == ADObjectTypes.Container || y.Type == ADObjectTypes.OU)
                {
                    return 1;
                }
                else if (y.Type == ADObjectTypes.Group)
                {
                    return string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
                }
                else
                {
                    return -1;
                }
            }

            if (x.Type == ADObjectTypes.User)
            {
                if (y.Type != ADObjectTypes.User)
                {
                    return 1;
                }
                else
                {
                    return string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
                }
            }

            return default;
        }

    }
}