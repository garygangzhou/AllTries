
using System;
using System.DirectoryServices;
using Microsoft.VisualBasic.CompilerServices;

namespace GZ.ActiveDirectoryLibrary.OrganizationUnitManagement
{
    public sealed class OrganizationUnitManager : ManagerBase
    {
        private const string FIND_SEARCH_EXCEPTION_MESSAGE = "There is no such object on the server. (Exception from HRESULT: 0x80072030)";

        private OrganizationUnitManager()
        {
        }

        internal OrganizationUnitManager(string domainControllerAddress, string domainName, string userName, string password)

        {
            DomainControllerAddress = domainControllerAddress;
            DomainControllerUserName = userName;
            DomainControllerPassword = password;
            DomainName = domainName;
        }

        public ADOrganizationalUnit GetOrganizationalUnit(string organizationalUnitGUID)
        {
            DirectoryEntry de;
            ADOrganizationalUnit adOU;
            de = GetDirectoryEntry(organizationalUnitGUID);
            if (de is object)
            {
                adOU = new ADOrganizationalUnit(Conversions.ToString(de.Properties["name"].Value), de.Path, de.Guid);
                if (de.Properties.Contains("isCriticalSystemObject") == true)
                {
                    if ((de.Properties["isCriticalSystemObject"].Value.ToString().ToLower() ?? "") == "true")
                    {
                        adOU.IsCriticalSystemObject = true;
                    }
                }

                return adOU;
            }
            else
            {
                return null;
            }
        }

        public bool OrganizationalUnitExists(string parentGUID, string organizationalUnitName)
        {
            DirectoryEntry de;
            var deOU = default(DirectoryEntry);
            if (!string.IsNullOrEmpty(parentGUID))
            {
                de = GetDirectoryEntry(parentGUID);
            }
            else
            {
                de = GetDirectoryEntry();
            }

            try
            {
                deOU = de.Children.Find("OU=" + EscapeInvalidLDAPCharacters(organizationalUnitName), SCHEMA_CLASS_OU);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains(FIND_SEARCH_EXCEPTION_MESSAGE) == true)
                {
                    return false;
                }
            }

            if (deOU is object)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsDuplicateInContainer(string organizationalUnitGuid, string ouNameToFind)
        {
            DirectoryEntry de = null;
            DirectoryEntry deOU;
            deOU = GetDirectoryEntry(organizationalUnitGuid);
            if (deOU.Parent is object)
            {
                try
                {
                    de = deOU.Parent.Children.Find("OU=" + EscapeInvalidLDAPCharacters(ouNameToFind), SCHEMA_CLASS_OU);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains(FIND_SEARCH_EXCEPTION_MESSAGE) == true)
                    {
                        return false;
                    }
                }
            }

            if (de is object)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void AddOrganizationalUnit(string parentGUID, string ouName)
        {
            DirectoryEntry de;
            DirectoryEntry deOU;
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
                deOU = de.Children.Add("OU=" + EscapeInvalidLDAPCharacters(ouName), SCHEMA_CLASS_OU);
                deOU.CommitChanges();
            }
        }

        public void RenameOrganizationalUnit(string oldOrganizationalUnitGUID, string newOrganizationalUnitName)
        {
            DirectoryEntry de;
            de = GetDirectoryEntry(oldOrganizationalUnitGUID);
            if (de is object)
            {
                de.Rename("OU=" + EscapeInvalidLDAPCharacters(newOrganizationalUnitName));
                de.CommitChanges();
            }
        }

        public void DeleteOrganizationalUnit(string organizationalUnitGUID)
        {
            DirectoryEntry de;
            de = GetDirectoryEntry(organizationalUnitGUID);
            if (de is object)
            {
                de.DeleteTree();
                de.CommitChanges();
            }
        }
    }
}