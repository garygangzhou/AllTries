
using System;

namespace GZ.ActiveDirectoryLibrary.GroupManagement
{
    /// <summary>
    /// ADGroup class inherit ADOBject. This class represents a group
    /// </summary>
    /// <remarks></remarks>
    public class ADGroup : ADObject
    {
        /// <summary>
        /// present a group
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        /// <param name="groupGuid"></param>
        public ADGroup(string name, string path, Guid groupGuid) : base(ADObjectTypes.Group, name, path, groupGuid)
        {
        }

        public ADGroupScopes Scope { get; set; }
        public ADGroupTypes Type { get; set; }

    }
}
