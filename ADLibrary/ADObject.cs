using System;

namespace GZ.ActiveDirectoryLibrary
{  
    public class ADObject
    {        
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