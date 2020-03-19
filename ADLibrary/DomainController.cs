
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text.RegularExpressions;

namespace GZ.ActiveDirectoryLibrary
{
    public class DomainController : ManagerBase
    {
        private UserManagement.AccountManager _userManager;
        private GroupManagement.GroupManager _groupManager;
        private OrganizationUnitManagement.OrganizationUnitManager _organizationUnitManager;

        private DomainController()
        {}

        public DomainController(string domainControllerAddress, string domainName, string userName, string password)

        {
            DomainControllerAddress = domainControllerAddress;
            DomainControllerUserName = userName;
            DomainControllerPassword = password;
            DomainName = domainName;
        }

        public UserManagement.AccountManager AccountManager
        {
            get
            {
                if (_userManager == default)
                {
                    _userManager = new UserManagement.AccountManager(DomainControllerAddress, DomainName, DomainControllerUserName, DomainControllerPassword);
                }

                return _userManager;
            }
        }
        
        public GroupManagement.GroupManager GroupManager
        {
            get
            {
                if (_groupManager == default)
                {
                    _groupManager = new GroupManagement.GroupManager(DomainControllerAddress, DomainName, DomainControllerUserName, DomainControllerPassword);
                }

                return _groupManager;
            }
        }
  
        public OrganizationUnitManagement.OrganizationUnitManager OrganizationalUnitManager
        {
            get
            {
                if (_organizationUnitManager == default)
                {
                    _organizationUnitManager = new OrganizationUnitManagement.OrganizationUnitManager(DomainControllerAddress, DomainName, DomainControllerUserName, DomainControllerPassword);
                }

                return _organizationUnitManager;
            }
        }

        public List<ADObject> SearchGroupAndOU(string name, bool groups, bool ou)
        {
            var de = GetDirectoryEntry();
            var deSearch = new DirectorySearcher(de);
            if (groups == true && ou == false)
            {
                deSearch.Filter = string.Format("(&(objectClass={0})(CN={1}))", SCHEMA_CLASS_GROUP, EscapeInvalidLDAPWildCharSearch(name));
            }
            else if (groups == false && ou == true)
            {
                deSearch.Filter = string.Format("(&(objectClass={0})(OU={1}))", SCHEMA_CLASS_OU, EscapeInvalidLDAPWildCharSearch(name));
            }
            else
            {
                deSearch.Filter = string.Format("(&(|(objectClass={0})(objectClass={1}))(|(CN={2})(OU={2})))", SCHEMA_CLASS_GROUP, SCHEMA_CLASS_OU, EscapeInvalidLDAPWildCharSearch(name));
            }

            var deSort = new SortOption("Name", SortDirection.Ascending);
            deSearch.Sort = deSort;
            deSearch.PageSize = 1000;
            var results = deSearch.FindAll();
            var children = new List<ADObject>();
            string rootPath = de.Path + "/";
            string regExp = "(CN|OU)=(.*)";
            Match match = null;
            string oname = "";
            if (results.Count > 0)
            {
                foreach (SearchResult result in results)
                {
                    var rde = result.GetDirectoryEntry();
                    string relativePath = rde.Path.Replace(rootPath, "");
                    match = Regex.Match(rde.Name, regExp, RegexOptions.IgnoreCase);
                    if (match.Success == true)
                    {
                        oname = match.Groups[2].Value;
                    }
                    else
                    {
                        oname = rde.Name;
                    }

                    oname = UnEscapeInvalidLDAPCharacters(oname);
                    relativePath = UnEscapeInvalidLDAPCharacters(relativePath);
                    var switchExpr = rde.SchemaClassName;
                    switch (switchExpr)
                    {
                        case SCHEMA_CLASS_GROUP:
                            {
                                children.Add(new ADObject(ADObjectTypes.Group, oname, relativePath, rde.Guid));
                                break;
                            }

                        case SCHEMA_CLASS_OU:
                            {
                                children.Add(new ADObject(ADObjectTypes.OU, oname, relativePath, rde.Guid));
                                break;
                            }
                    }
                }
            }

            children.Sort(new Comparer.ADObjectComparer());
            // children.Reverse()
            return children;
        }
    }
}