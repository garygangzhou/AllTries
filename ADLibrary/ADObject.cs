
using System;

namespace GZ.ActiveDirectoryLibrary
{
    /// <summary>
    /// An active directory object
    /// </summary>
    public class ADObject
    {
     
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="path"></param>
        /// <param name="guid"></param>
        public ADObject(ADObjectTypes type, string name, string path, Guid guid)
        {
            Type = type;
            Name = name;
            Path = path;
            GUID = guid;
        }

        public ADObjectTypes Type { get; set; }

        public string Name { get; set; }      

        public string Path { get; set; }        

        public Guid GUID { get; set; }
        
        public bool IsCriticalSystemObject { get; set; }       

        public bool ShowInAdvancedViewOnly { get; set; }        
       
    }
}