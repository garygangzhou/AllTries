
using System;
using System.DirectoryServices;
using Microsoft.VisualBasic.CompilerServices;

namespace GZ.ActiveDirectoryLibrary.GroupManagement
{   
    public sealed class GroupManager : ManagerBase
    {
        public const string INVALID_GROUPNAME_CHRS = @"/\[]:;|=,+*?<>""";
        private const string FIND_SEARCH_EXCEPTION_MESSAGE = "There is no such object on the server. (Exception from HRESULT: 0x80072030)";

        private GroupManager()
        {
        }

        internal GroupManager(string domainControllerAddress, string domainName, string userName, string password)
        {
            DomainControllerAddress = domainControllerAddress;
            DomainControllerUserName = userName;
            DomainControllerPassword = password;
            DomainName = domainName;
        }

        public ADGroup GetGroup(string groupGUID)
        {
            DirectoryEntry de;
            ADGroup adGrp;
            de = GetDirectoryEntry(groupGUID);
            if (de is object)
            {
                adGrp = new ADGroup(Conversions.ToString(de.Properties["name"].Value), de.Path, de.Guid);
                if ((Conversions.ToInteger(de.Properties["groupType"].Value) & (int)ADGroupTypes.Security) == (int)ADGroupTypes.Security)
                {
                    adGrp.Type = ADGroupTypes.Security;
                }
                else
                {
                    adGrp.Type = ADGroupTypes.Distribution;
                }

                if ((Conversions.ToInteger(de.Properties["groupType"].Value) & (int)ADGroupScopes.DomainLocal) == (int)ADGroupScopes.DomainLocal)
                {
                    adGrp.Scope = ADGroupScopes.DomainLocal;
                }
                else if ((Conversions.ToInteger(de.Properties["groupType"].Value) & (int)ADGroupScopes.Global) == (int)ADGroupScopes.Global)
                {
                    adGrp.Scope = ADGroupScopes.Global;
                }
                else if ((Conversions.ToInteger(de.Properties["groupType"].Value) & (int)ADGroupScopes.Universal) == (int)ADGroupScopes.Universal)
                {
                    adGrp.Scope = ADGroupScopes.Universal;
                }
                else
                {
                    adGrp.Scope = ADGroupScopes.Local;
                }

                if (de.Properties.Contains("isCriticalSystemObject") == true)
                {
                    if ((de.Properties["isCriticalSystemObject"].Value.ToString().ToLower() ?? "") == "true")
                    {
                        adGrp.IsCriticalSystemObject = true;
                    }
                }

                return adGrp;
            }
            else
            {
                return null;
            }
        }

        public bool GroupExists(string groupName)
        {
            DirectoryEntry de;
            de = GetDirectoryEntry();
            var deSearch = new DirectorySearcher(de);
            deSearch.Filter = string.Format("(&(objectClass={0})(CN={1}))", SCHEMA_CLASS_GROUP, EscapeInvalidLDAPSearchCharacters(groupName));
            deSearch.PropertiesToLoad.Add("name");
            var sr = deSearch.FindOne();
            if (sr is object)
            {
                return true;
            }
            else
            {
                return false;
            }
        }      

        public void AddGroup(string parentGUID, string groupName, ADGroupTypes groupType, ADGroupScopes groupScope)
        {
            DirectoryEntry de;
            DirectoryEntry deGroup;
            if (IsValidGroupName(groupName) == true)
            {
                if (!string.IsNullOrEmpty(parentGUID))
                {
                    de = GetDirectoryEntry(parentGUID);
                }
                else
                {
                    de = GetDirectoryEntry();
                }

                if (de is object)
                {
                    deGroup = de.Children.Add("CN=" + EscapeInvalidLDAPCharacters(groupName), SCHEMA_CLASS_GROUP);
                    deGroup.Properties["sAMAccountName"].Value = groupName;
                    deGroup.Properties["groupType"].Value = (int)groupType | (int)groupScope;
                    deGroup.CommitChanges();
                }
            }
            else
            {
                throw new ArgumentException(string.Format("Group name contains one or more invalid characters. Invalid characters in a group name are {0}", INVALID_GROUPNAME_CHRS));
            }
        }

        public void RenameGroup(string oldGroupGUID, string newGroupName)
        {
            DirectoryEntry de;
            if (IsValidGroupName(newGroupName) == true)
            {
                de = GetDirectoryEntry(oldGroupGUID);
                if (de is object)
                {
                    de.Rename("CN=" + EscapeInvalidLDAPCharacters(newGroupName));
                    de.CommitChanges();
                    de.Properties["sAMAccountName"].Value = newGroupName;
                    de.CommitChanges();
                }
            }
            else
            {
                throw new ArgumentException(string.Format("Group name contains one or more invalid characters. Invalid characters in a group name are {0}", INVALID_GROUPNAME_CHRS));
            }
        }

        public void DeleteGroup(string groupGUID)
        {
            DirectoryEntry de;
            de = GetDirectoryEntry(groupGUID);
            if (de is object)
            {
                de.DeleteTree();
                de.CommitChanges();
            }
        }

        private bool IsValidGroupName(string groupName)
        {
            if (groupName.IndexOfAny(INVALID_GROUPNAME_CHRS.ToCharArray()) != -1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
