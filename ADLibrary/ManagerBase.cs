
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;

namespace GZ.ActiveDirectoryLibrary
{
    public class ManagerBase
    {
        protected const string SCHEMA_CLASS_USER = "user";
        protected const string SCHEMA_CLASS_GROUP = "group";
        protected const string SCHEMA_CLASS_OU = "organizationalUnit";
        protected const string SCHEMA_CLASS_CONTAINER = "container";
        protected const string SCHEMA_CLASS_BUILTINDOMAIN = "builtinDomain";
        protected const string SCHEMA_CLASS_DOMAINDNS = "domainDNS";
        protected const string INVALID_LDAP_CHRS = @"=,+""\<>;#";
        private const string FIND_SEARCH_EXCEPTION_MESSAGE = "There is no such object on the server. (Exception from HRESULT: 0x80072030)";

        protected ManagerBase() {}

        protected string DomainControllerAddress { get; set; }

        protected string DomainControllerUserName  { get; set; }

        protected string DomainControllerPassword  { get; set; }

        protected string DomainName { get; set; }

        protected string CompleteUserName
        {
            get
            {
                return DomainName + @"\" + DomainControllerUserName;
            }
        }

        protected string EscapeInvalidLDAPCharacters(string input)
        {
            // replace \ chr first and then look for others
            input = input.Replace(@"\", @"\\");

            // If input.StartsWith("#") = True Then
            // input = "\" & input
            // End If

            if (input.EndsWith(" ") == true)
            {
                // input = input.Replace(" ", "\ ")
                input = input.Remove(input.Length);
                input = input + @"\ ";
            }

            char[] invalidChrs;
            invalidChrs = INVALID_LDAP_CHRS.ToCharArray();
            foreach (char c in invalidChrs)
            {
                if (Conversions.ToString(c) != @"\")
                {
                    input = input.Replace(Conversions.ToString(c), string.Format(@"\{0}", c));
                }
            }

            return input;
        }

        protected string UnEscapeInvalidLDAPCharacters(string input)
        {
            // replace \ chr first and then look for others
            input = input.Replace(@"\\", @"\");
            input = input.Replace(@"\#", "#");
            input = input.Replace(@"\ ", " ");
            char[] invalidChrs;
            invalidChrs = INVALID_LDAP_CHRS.ToCharArray();
            foreach (char c in invalidChrs)
                input = input.Replace(string.Format(@"\{0}", c), Conversions.ToString(c));
            return input;
        }

         protected string EscapeInvalidLDAPSearchCharacters(string input)
        {
            // replace \ chr first and then look for others
            input = input.Replace(@"\", @"\5a");
            input = input.Replace("*", @"\2a");
            input = input.Replace("(", @"\28");
            input = input.Replace(")", @"\29");
            input = input.Replace("NUL", @"\00");
            input = input.Replace("/", @"\2f");
            input = input.Replace("#", @"\#");
            return input;
        }

        protected string UnEscapeInvalidLDAPSearchCharacters(string input)
        {
            // replace \ chr first and then look for others
            input = input.Replace(@"\5a", @"\");
            input = input.Replace(@"\2a", "*");
            input = input.Replace(@"\28", "(");
            input = input.Replace(@"\29", ")");
            input = input.Replace(@"\00", "NUL");
            input = input.Replace(@"\2f", "/");
            input = input.Replace(@"\#", "#");
            return input;
        }

        protected string EscapeInvalidLDAPWildCharSearch(string input)
        {
            // replace \ chr first and then look for others
            input = input.Replace(@"\", @"\5a");
            input = input.Replace("[*]", @"\2a");
            input = input.Replace("(", @"\28");
            input = input.Replace(")", @"\29");
            input = input.Replace("NUL", @"\00");
            input = input.Replace("/", @"\2f");
            input = input.Replace("#", @"\#");
            return input;
        }

        protected string UnEscapeInvalidLDAPWildCharSearch(string input)
        {
            // replace \ chr first and then look for others
            input = input.Replace(@"\5a", @"\");
            input = input.Replace(@"\2a", "*");
            input = input.Replace(@"\28", "(");
            input = input.Replace(@"\29", ")");
            input = input.Replace(@"\00", "NUL");
            input = input.Replace(@"\2f", "/");
            input = input.Replace(@"\#", "#");
            return input;
        }

         protected DirectoryEntry GetDirectoryEntry()
        {
            DirectoryEntry de;
            de = new DirectoryEntry("LDAP://" + DomainControllerAddress, CompleteUserName, DomainControllerPassword);
            de.AuthenticationType = AuthenticationTypes.Sealing | AuthenticationTypes.Secure;
            return de;
        }

        protected DirectoryEntry GetDirectoryEntry(string path, bool fullPath)
        {
            DirectoryEntry de;
            string concatenateChr = "";
            var adPath = new StringBuilder();
            if (path == default && path.StartsWith(concatenateChr) == true)
            {
                concatenateChr = "";
            }
            else
            {
                concatenateChr = "/";
            }

            if (fullPath == true)
            {
                adPath.Append("LDAP://");
                adPath.Append(DomainControllerAddress);
                adPath.Append(concatenateChr);
                adPath.Append(path);
            }
            else
            {
                adPath.Append(path);
            }

            de = new DirectoryEntry(adPath.ToString(), CompleteUserName, DomainControllerPassword);
            de.AuthenticationType = AuthenticationTypes.Sealing | AuthenticationTypes.Secure;
            return de;
        }

        protected DirectoryEntry GetDEtoRemoveUserFromGroup(string path)
        {
            DirectoryEntry de;
            de = new DirectoryEntry("LDAP://" + DomainControllerAddress, CompleteUserName, DomainControllerPassword);
            var dSearch = new DirectorySearcher(de);
            dSearch.Filter = "(&(objectClass=group)(CN=" + EscapeInvalidLDAPSearchCharacters(path) + "))";
            dSearch.SearchScope = SearchScope.Subtree;
            var results = dSearch.FindAll();
            DirectoryEntry dentry;
            if (results.Count < 1)
            {
                throw new ApplicationException("The Active Directory group does not exist.");
            }
            else
            {
                dentry = results[0].GetDirectoryEntry();
            }

            dentry.AuthenticationType = AuthenticationTypes.Sealing | AuthenticationTypes.Secure;
            return dentry;
        }

        public DirectoryEntry GetDirectoryEntry(string objectGUID)
        {
            DirectoryEntry de;
            de = GetDirectoryEntry();
            var deSearch = new DirectorySearcher(de);
            deSearch.Filter = string.Format("(objectGUID={0})", Utility.GuidToOctetString(objectGUID));
            var result = deSearch.FindOne();
            if (result is object)
            {
                return result.GetDirectoryEntry();
            }
            else
            {
                return null;
            }
        }

        public List<ADObject> GetChildren(string parentGUID)
        {
            DirectoryEntry deCurrentContainer;
            DirectoryEntry deRoot;
            string rootPath;
            string relativePath;
            ADObject adObject = null;
            var children = new List<ADObject>();
            deRoot = GetDirectoryEntry();
            if ((parentGUID ?? "") == (string.Empty ?? ""))
            {
                deCurrentContainer = deRoot;
            }
            else
            {
                deCurrentContainer = GetDirectoryEntry(parentGUID);
            }

            rootPath = deRoot.Path + "/";
            // If deCurrentContainer.SchemaClassName = SCHEMA_CLASS_DOMAINDNS OrElse deCurrentContainer.SchemaClassName = SCHEMA_CLASS_GROUP OrElse deCurrentContainer.SchemaClassName = SCHEMA_CLASS_OU OrElse deCurrentContainer.SchemaClassName = SCHEMA_CLASS_BUILTINDOMAIN OrElse deCurrentContainer.SchemaClassName = SCHEMA_CLASS_CONTAINER Then

            foreach (DirectoryEntry de in deCurrentContainer.Children)
            {
                relativePath = de.Path.Replace(rootPath, "");
                adObject = null;
                var switchExpr = de.SchemaClassName;
                switch (switchExpr)
                {
                    case SCHEMA_CLASS_USER:
                        {
                            adObject = new ADObject(ADObjectTypes.User, Conversions.ToString(de.Properties["name"].Value), relativePath, de.Guid);
                            break;
                        }

                    case SCHEMA_CLASS_GROUP:
                        {
                            adObject = new ADObject(ADObjectTypes.Group, Conversions.ToString(de.Properties["name"].Value), relativePath, de.Guid);
                            break;
                        }

                    case SCHEMA_CLASS_OU:
                        {
                            adObject = new ADObject(ADObjectTypes.OU, Conversions.ToString(de.Properties["name"].Value), relativePath, de.Guid);
                            break;
                        }

                    case SCHEMA_CLASS_BUILTINDOMAIN:
                        {
                            adObject = new ADObject(ADObjectTypes.BuiltInDomain, Conversions.ToString(de.Properties["name"].Value), relativePath, de.Guid);
                            break;
                        }

                    case SCHEMA_CLASS_CONTAINER:
                        {
                            adObject = new ADObject(ADObjectTypes.Container, Conversions.ToString(de.Properties["name"].Value), relativePath, de.Guid);
                            break;
                        }
                }

                if (adObject is object)
                {
                    if (de.Properties.Contains("isCriticalSystemObject") == true)
                    {
                        if ((de.Properties["isCriticalSystemObject"].Value.ToString().ToLower() ?? "") == "true")
                        {
                            adObject.IsCriticalSystemObject = true;
                        }
                    }

                    if (de.Properties.Contains("showInAdvancedViewOnly") == true)
                    {
                        if ((de.Properties["showInAdvancedViewOnly"].Value.ToString().ToLower() ?? "") == "true")
                        {
                            adObject.ShowInAdvancedViewOnly = true;
                        }
                    }

                    children.Add(adObject);
                }
            }

            children.Sort(new Comparer.ADObjectComparer());
            // children.Reverse()
            return children;
        }

        public List<ADObject> GetChildContainers(string parentGUID)
        {
            DirectoryEntry deCurrentContainer;
            DirectoryEntry deRoot;
            string rootPath;
            string relativePath;
            ADObject adObject = null;
            var children = new List<ADObject>();
            deRoot = GetDirectoryEntry();
            if ((parentGUID ?? "") == (string.Empty ?? ""))
            {
                deCurrentContainer = deRoot;
            }
            else
            {
                deCurrentContainer = GetDirectoryEntry(parentGUID);
            }

            rootPath = deRoot.Path + "/";
            // If deCurrentContainer.SchemaClassName = SCHEMA_CLASS_DOMAINDNS OrElse deCurrentContainer.SchemaClassName = SCHEMA_CLASS_GROUP OrElse deCurrentContainer.SchemaClassName = SCHEMA_CLASS_OU OrElse deCurrentContainer.SchemaClassName = SCHEMA_CLASS_BUILTINDOMAIN OrElse deCurrentContainer.SchemaClassName = SCHEMA_CLASS_CONTAINER Then

            foreach (DirectoryEntry de in deCurrentContainer.Children)
            {
                relativePath = de.Path.Replace(rootPath, "");
                adObject = null;
                var switchExpr = de.SchemaClassName;
                switch (switchExpr)
                {
                    case SCHEMA_CLASS_OU:
                        {
                            adObject = new ADObject(ADObjectTypes.OU, Conversions.ToString(de.Properties["name"].Value), relativePath, de.Guid);
                            break;
                        }

                    case SCHEMA_CLASS_BUILTINDOMAIN:
                        {
                            adObject = new ADObject(ADObjectTypes.BuiltInDomain, Conversions.ToString(de.Properties["name"].Value), relativePath, de.Guid);
                            break;
                        }

                    case SCHEMA_CLASS_CONTAINER:
                        {
                            adObject = new ADObject(ADObjectTypes.Container, Conversions.ToString(de.Properties["name"].Value), relativePath, de.Guid);
                            break;
                        }
                }

                if (adObject is object)
                {
                    if (de.Properties.Contains("isCriticalSystemObject") == true)
                    {
                        if ((de.Properties["isCriticalSystemObject"].Value.ToString().ToLower() ?? "") == "true")
                        {
                            adObject.IsCriticalSystemObject = true;
                        }
                    }

                    if (de.Properties.Contains("showInAdvancedViewOnly") == true)
                    {
                        if ((de.Properties["showInAdvancedViewOnly"].Value.ToString().ToLower() ?? "") == "true")
                        {
                            adObject.ShowInAdvancedViewOnly = true;
                        }
                    }

                    children.Add(adObject);
                }
            }

            children.Sort(new Comparer.ADObjectComparer());
            // children.Reverse()
            return children;
        }

        public ADObject RootObject
        {
            get
            {
                DirectoryEntry de;
                ADObject adObject = null;
                de = GetDirectoryEntry();
                adObject = new ADObject(ADObjectTypes.RootNode, Conversions.ToString(de.Properties["name"].Value), de.Path, de.Guid);
                if (adObject is object)
                {
                    if (de.Properties.Contains("isCriticalSystemObject") == true)
                    {
                        if ((de.Properties["isCriticalSystemObject"].Value.ToString().ToLower() ?? "") == "true")
                        {
                            adObject.IsCriticalSystemObject = true;
                        }
                    }

                    if (de.Properties.Contains("showInAdvancedViewOnly") == true)
                    {
                        if ((de.Properties["showInAdvancedViewOnly"].Value.ToString().ToLower() ?? "") == "true")
                        {
                            adObject.ShowInAdvancedViewOnly = true;
                        }
                    }
                }

                return adObject;
            }
        }

        public ADObject GetADObject(Guid guid)
        {
            DirectoryEntry de;
            ADObjectTypes objType;
            ADObject adObject;
            de = GetDirectoryEntry(guid.ToString());
            if (de is object)
            {
                var switchExpr = de.SchemaClassName;
                switch (switchExpr)
                {
                    case SCHEMA_CLASS_BUILTINDOMAIN:
                        {
                            objType = ADObjectTypes.BuiltInDomain;
                            break;
                        }

                    case SCHEMA_CLASS_CONTAINER:
                        {
                            objType = ADObjectTypes.Container;
                            break;
                        }

                    case SCHEMA_CLASS_DOMAINDNS:
                        {
                            objType = ADObjectTypes.RootNode;
                            break;
                        }

                    case SCHEMA_CLASS_GROUP:
                        {
                            objType = ADObjectTypes.Group;
                            break;
                        }

                    case SCHEMA_CLASS_OU:
                        {
                            objType = ADObjectTypes.OU;
                            break;
                        }

                    case SCHEMA_CLASS_USER:
                        {
                            objType = ADObjectTypes.User;
                            break;
                        }

                    default:
                        {
                            objType = ADObjectTypes.Unknown;
                            break;
                        }
                }

                adObject = new ADObject(objType, Conversions.ToString(de.Properties["name"].Value), de.Path, de.Guid);
                if (adObject is object)
                {
                    if (de.Properties.Contains("isCriticalSystemObject") == true)
                    {
                        if ((de.Properties["isCriticalSystemObject"].Value.ToString().ToLower() ?? "") == "true")
                        {
                            adObject.IsCriticalSystemObject = true;
                        }
                    }

                    if (de.Properties.Contains("showInAdvancedViewOnly") == true)
                    {
                        if ((de.Properties["showInAdvancedViewOnly"].Value.ToString().ToLower() ?? "") == "true")
                        {
                            adObject.ShowInAdvancedViewOnly = true;
                        }
                    }
                }

                return adObject;
            }
            else
            {
                return null;
            }
        }

        public ADObject GetADObjectParent(Guid objectGuid)
        {
            DirectoryEntry de;
            de = GetDirectoryEntry(objectGuid.ToString());
            if (de is object)
            {
                if (de.Parent is object)
                {
                    return GetADObject(de.Guid);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public string GetDistinguishedName(Guid guid)
        {
            DirectoryEntry de;
            de = GetDirectoryEntry(guid.ToString());
            return Conversions.ToString(de.Properties["distinguishedName"].Value);
        }

        public string GetDistinguishedName(string commonName)
        {
            var de = GetDirectoryEntry();
            var deSearch = new DirectorySearcher(de);
            deSearch.Filter = string.Format("(CN={0})", EscapeInvalidLDAPSearchCharacters(commonName));
            deSearch.PropertiesToLoad.Add("distinguishedName");
            var result = deSearch.FindOne();
            string path = null;
            if (!(result == default))
            {
                var argproperties = result.Properties;
                path = GetADPropertyValue("distinguishedName", ref argproperties);
            }

            return path;
        }

        public bool ObjectExistsInContainer(string parentGUID, string objectName)
        {
            DirectoryEntry de;
            if (!string.IsNullOrEmpty(parentGUID))
            {
                de = GetDirectoryEntry(parentGUID);
            }
            else
            {
                de = GetDirectoryEntry();
            }

            var deSearch = new DirectorySearcher(de);
            deSearch.Filter = string.Format("(|(CN={0})(OU={0}))", EscapeInvalidLDAPSearchCharacters(objectName));
            deSearch.PropertiesToLoad.Add("name");
            deSearch.SearchScope = SearchScope.OneLevel;
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

        public bool IsDuplicateObjectInContainer(string objectGuid, string objectNameToFind)
        {
            DirectoryEntry de;
            de = GetDirectoryEntry(objectGuid);
            if (de == default)
            {
                throw new ArgumentException("Invalid objectGuid");
            }

            if (de.Parent is object)
            {
                var deSearch = new DirectorySearcher(de.Parent);
                deSearch.Filter = string.Format("(|(CN={0})(OU={0}))", EscapeInvalidLDAPSearchCharacters(objectNameToFind));
                deSearch.PropertiesToLoad.Add("name");
                deSearch.SearchScope = SearchScope.OneLevel;
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
            else
            {
                throw new ArgumentException("object has no container.");
            }
        }

        protected string GetADPropertyValue(string key, ref ResultPropertyCollection properties)
        {
            if (properties.Contains(key) == true && properties[key].Count > 0)
            {
                return UnEscapeInvalidLDAPSearchCharacters(Conversions.ToString(properties[key][0]));
            }

            return "";
        }

        protected string GetADPropertyMultiValue(string key, ref ResultPropertyCollection properties)
        {
            string value = "";
            if (properties.Contains(key) == true)
            {
                foreach (string val in properties[key])
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        value = UnEscapeInvalidLDAPSearchCharacters(val);
                    }
                    else
                    {
                        value = value + Conversions.ToString(Constants.MULTI_VALUED_SEPARATOR) + UnEscapeInvalidLDAPSearchCharacters(val);
                    }
                }
            }

            return value;
        }

        protected string GetOwner(DirectoryEntry userde)
        {
            try
            {
                System.Security.Principal.SecurityIdentifier owner = (System.Security.Principal.SecurityIdentifier)userde.ObjectSecurity.GetOwner(Type.GetType("System.Security.Principal.SecurityIdentifier"));
                if (owner.AccountDomainSid == default)
                {
                    return "";
                }

                string sid = Utility.ConvertSidToOctetString(owner.AccountDomainSid.ToString());
                var ged = GetDirectoryEntry(sid, false);
                return ged.Username;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        protected bool PathExists(string path)
        {
            DirectoryEntry de;
            // Dim o As Object
            SearchResult result;
            try
            {
                de = GetDirectoryEntry();
                var deSearch = new DirectorySearcher(de);
                deSearch.Filter = string.Format("(distinguishedName={0})", EscapeInvalidLDAPSearchCharacters(path));
                result = deSearch.FindOne();
                if (!(result == default))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf("There is no such object on the server") != -1)
                {
                    return false;
                }
            }

            return default;
        }

        protected void SetADProperty(string key, string value, ref System.DirectoryServices.PropertyCollection properties)
        {
            if (!string.IsNullOrEmpty(value))
            {
                properties[key].Value = value;
            }
            else
            {
                properties[key].Clear();
            }
        }

        protected void SetMultiValueADProperty(string key, string values, ref System.DirectoryServices.PropertyCollection properties)
        {
            if (!string.IsNullOrEmpty(values))
            {
                var list = values.Split(Constants.MULTI_VALUED_SEPARATOR);
                Array.Reverse(list);
                properties[key].Value = list;
            }
            else
            {
                properties[key].Clear();
            }
        }
    }
}
