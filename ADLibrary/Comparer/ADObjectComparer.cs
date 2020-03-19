

using System.Collections.Generic;

namespace GZ.ActiveDirectoryLibrary.Comparer
{
    /// <summary>
    /// Implements IComparer(Of ADObject) interface
    /// </summary>
    /// <remarks></remarks>
    public class ADObjectComparer : IComparer<ADObject>
    {
        public int Compare(ADObject x, ADObject y)
        {

            // Compare by object type first and then by name
            // ORDER = Users, Groups, OU (reversed by the method that is calling this)
            // If same type than compare by name
            if ((x.Type == ADObjectTypes.BuiltInDomain || x.Type == ADObjectTypes.Container || x.Type == ADObjectTypes.OU) && (y.Type == ADObjectTypes.BuiltInDomain || y.Type == ADObjectTypes.Container || y.Type == ADObjectTypes.OU))
            {
                return string.Compare(x.Name, y.Name);
            }

            if (x.Type == y.Type)
            {
                return string.Compare(x.Name, y.Name);
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
                    return string.Compare(x.Name, y.Name);
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
                    return string.Compare(x.Name, y.Name);
                }
            }

            return default;
        }
    }
}