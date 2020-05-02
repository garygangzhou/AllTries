
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;
using GZ.ActiveDirectoryLibrary.Exceptions;

namespace GZ.ActiveDirectoryLibrary.UserManagement
{

    public sealed class AccountManager : ManagerBase
    {
        private const string CHANGE_PASSWORD_GUID = "{AB721A53-1E2F-11D0-9819-00AA0040529B}";

        private AccountManager()
        {
        }

        internal AccountManager(string domainControllerAddress, string domainName, string userName, string password)

        {
            DomainControllerAddress = domainControllerAddress;
            DomainControllerUserName = userName;
            DomainControllerPassword = password;
            DomainName = domainName;
        }

        private SearchResult SearchGUID(string userGuid)
        {
            var rootde = GetDirectoryEntry();
            var deSearch = new DirectorySearcher(rootde);
            deSearch.Filter = string.Format("(&(objectClass=user)(objectGUID={0}))", Utility.GuidToOctetString(userGuid));
            return deSearch.FindOne();
        }

        private string EscapeLogonChrs(string logon)
        {
            foreach (char c in Constants.LOGON_ESCAPE_CHRS.ToCharArray())
                logon = logon.Replace(Convert.ToString(c), @"\" + Convert.ToString(c));
            return logon;
        }      

        private void SetAccountOptions(Account user, DirectoryEntry de)
        {
            _ = int.TryParse(de.Properties["userAccountControl"].Value == null ? string.Empty : de.Properties["userAccountControl"].Value.ToString(), out int exp);

            // 'Normail Account
            // exp = exp Or &H200

            // UF_DONT_EXPIRE_PASSWD 0x10000
            if (user.AccountOptionPasswordNeverExpire == true)
            {
                exp = exp | Constants.ACCOUNT_OPTION_PWD_NEVER_EXPIRES;
            }
            else
            {
                exp = exp & ~Constants.ACCOUNT_OPTION_PWD_NEVER_EXPIRES;
            }

            if (user.AccountOptionAccountDisabled == true)
            {
                // UF_ACCOUNTDISABLE 0x0002
                exp = exp | Constants.ACCOUNT_OPTION_ACCOUNT_DISABLED;
            }
            else
            {
                exp = exp & ~Constants.ACCOUNT_OPTION_ACCOUNT_DISABLED;
            }

            if (user.AccountOptionPasswordReversibleEncryption == true)
            {
                exp = exp | Constants.ACCOUNT_OPTION_PWD_REVERSIBLE_ENCRYPTION;
            }
            else
            {
                exp = exp & ~Constants.ACCOUNT_OPTION_PWD_REVERSIBLE_ENCRYPTION;
            }

            if (user.AccountOptionAccountLocked == true)
            {
                exp = exp | Constants.ACCOUNT_OPTION_ACCOUNT_LOCKOUT;
            }
            else
            {
                exp = exp & ~Constants.ACCOUNT_OPTION_ACCOUNT_LOCKOUT;
            }

            if (user.AccountOptionUserMustChangePasswordNextLogon == true)
            {
                de.Properties["PwdLastSet"].Value = 0;
            }
            else
            {
                de.Properties["PwdLastSet"].Value = -1;
            }

            de.Properties["userAccountControl"].Value = exp;
            de.CommitChanges();

            // Commented out this code because it throws an exception
            // SetUserCannotChangePassword(user.AccountOptionUserCannotChangePassword, de)
        }

        private void SetUserCannotChangePassword(bool cannotChangePwd, DirectoryEntry userde)
        {
            // Code from http://www.eggheadcafe.com/conversation.aspx?messageid=29773590&threadid=29773576

            bool fEveryone;
            bool fSelf;
            fEveryone = false;
            fSelf = false;
            var securityType = new SecurityIdentifier((byte[])userde.Properties["objectSid"].Value, 0);
            var theAccess = userde.ObjectSecurity.GetAccessRules(true, true, securityType.GetType());
            foreach (ActiveDirectoryAccessRule accessRule in theAccess)
            {
                if (accessRule.ObjectType == new Guid(CHANGE_PASSWORD_GUID))
                {

                    // Everyone or NT Authority\Self
                    if ((accessRule.IdentityReference.Value ?? "") == "S-1-1-0" | (accessRule.IdentityReference.Value ?? "") == "S-1-5-10")
                    {
                        ActiveDirectoryAccessRule newRule;
                        if (cannotChangePwd)
                        {
                            newRule = new ActiveDirectoryAccessRule(accessRule.IdentityReference, accessRule.ActiveDirectoryRights, System.Security.AccessControl.AccessControlType.Deny, accessRule.ObjectType, accessRule.InheritanceType, accessRule.InheritedObjectType);
                        }
                        else
                        {
                            newRule = new ActiveDirectoryAccessRule(accessRule.IdentityReference, accessRule.ActiveDirectoryRights, System.Security.AccessControl.AccessControlType.Allow, accessRule.ObjectType, accessRule.InheritanceType, accessRule.InheritedObjectType);
                        }

                        userde.ObjectSecurity.RemoveAccessRuleSpecific(accessRule);
                        userde.ObjectSecurity.AddAccessRule(newRule);
                    }
                }
            }

            userde.CommitChanges();
        }

        private void AddUserToGroup(DirectoryEntry de, DirectoryEntry deUser, string groupName)
        {
            SearchResultCollection results = null;
            try
            {
                var deSearch = new DirectorySearcher(de);
                deSearch.Filter = "(&(objectClass=group)(cn=" + EscapeInvalidLDAPSearchCharacters(groupName) + "))";
                results = deSearch.FindAll();
                bool isGroupMember = false;
                if (results.Count < 1)
                {
                    throw new ApplicationException("The Active Directory group does not exist.");
                }

                var group = results[0].GetDirectoryEntry();

                // First we'll invoke the ADSI IsMember method to check  
                // if the user is already a member of this group
                // 
                bool isMember = Convert.ToBoolean(group.Invoke("IsMember", new object[] { deUser.Path }));
                if (isMember == true)
                {
                }
                else
                {
                    // Add the user to the group by invoking the Add method
                    try
                    {
                        group.Invoke("Add", new object[] { deUser.Path });
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException == default && ex.InnerException.Message.IndexOf("The object already exists") == -1)
                        {
                            throw;
                        }
                    }
                }

                // 
                // Cleanup our allocated objects
                // 
                if (!(group == default))
                {
                    group.Dispose();
                }
            }
            finally
            {
                if (!(results == default))
                {
                    results.Dispose();
                }
            }
        }

        private void RemoveUserFromGroup(DirectoryEntry deGroup, DirectoryEntry deUser)
        {
            // First we'll invoke the ADSI IsMember method to check  
            // if the user is already a member of this group
            // 
            bool isMember = Convert.ToBoolean(deGroup.Invoke("IsMember", new object[] { deUser.Path }));
            if (isMember == true)
            {
                // 
                // Remove the user from the group by invoking the remove method
                // 
                deGroup.Invoke("Remove", new object[] { deUser.Path });
            }
        }

        private string BuildUserDisplayName(string firstName, string middleInitial, string lastName)
        {
            if (!string.IsNullOrEmpty(middleInitial.Trim()))
            {
                return (firstName + " " + middleInitial + ". " + lastName).Trim ();
            }
            else
            {
                return (firstName + " " + lastName).Trim();
            }
        }

        private long GetMaxPasswordAgeForDomain()
        {
            DirectoryEntry de;
            de = GetDirectoryEntry();
            var deSearch = new DirectorySearcher(de);
            deSearch.Filter = "objectclass=Domain";
            SearchResultCollection results = null;
            try
            {
                results = deSearch.FindAll();
                if (results.Count > 0)
                {                    
                    return Math.Abs(Convert.ToInt64(results[0].Properties["maxPwdAge"][0]));
                }
                else
                {
                    return 0;
                }
            }
            finally
            {
                if (!(results == default))
                {
                    results.Dispose();
                }
            }
        }

        private bool GetUserCannotChangePassword(DirectoryEntry userde)
        {

            // Code from http://www.eggheadcafe.com/conversation.aspx?messageid=29773590&threadid=29773576
            bool fEveryone;
            bool fSelf;
            fEveryone = false;
            fSelf = false;
            var securityType = new SecurityIdentifier((byte[])userde.Properties["objectSid"].Value, 0);
            var theAccess = userde.ObjectSecurity.GetAccessRules(true, true, securityType.GetType());
            foreach (ActiveDirectoryAccessRule accessRule in theAccess)
            {
                if (accessRule.ObjectType == new Guid(CHANGE_PASSWORD_GUID))
                {

                    // Everyone
                    if ((accessRule.IdentityReference.Value ?? "") == "S-1-1-0" && accessRule.AccessControlType == System.Security.AccessControl.AccessControlType.Deny)
                    {
                        fEveryone = true;
                    }

                    // NT Authority\Self
                    if ((accessRule.IdentityReference.Value ?? "") == "S-1-5-10" && accessRule.AccessControlType == System.Security.AccessControl.AccessControlType.Deny)
                    {
                        fSelf = true;
                    }
                }
            }

            if (fSelf && fEveryone)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private Account GetADAccount(string filter)
        {
            DirectoryEntry de;
            var user = new Account();
            var propertiesToLoad = new[] { "objectGUID", "sAMAccountName", "userPrincipalName", "distinguishedName", "givenName", "sn",
                "initials", "displayName", "description", "physicalDeliveryOfficeName", "telephoneNumber", "otherTelephone", "mail",
                "wWWHomePage", "url", "streetAddress", "postOfficeBox", "l", "st", "postalCode", "co", "c", "ou", "homePhone",
                "otherHomePhone", "pager", "otherPager", "mobile", "otherMobile", "facsimileTelephoneNumber", "otherFacsimileTelephoneNumber",
                "ipPhone", "otherIpPhone", "info", "title", "department", "company", "manager", "userAccountControl",
                "msDS-User-Account-Control-Computed", "MemberOf", "whenCreated", "creator", "pwdLastSet" };

            de = GetDirectoryEntry();
            var deSearch = new DirectorySearcher(de);
            deSearch.Filter = filter;
            deSearch.PropertiesToLoad.AddRange(propertiesToLoad);
            var result = deSearch.FindOne();
            if (!(result == default))
            {
                user.Guid = result.GetDirectoryEntry().Guid.ToString();
                user.LogonName = GetADPropertyValue("sAMAccountName", result.Properties);
                user.UPN = GetADPropertyValue("userPrincipalName", result.Properties);
                user.FirstName = GetADPropertyValue("givenName", result.Properties);
                user.LastName = GetADPropertyValue("sn", result.Properties);
                user.MiddleInitial = GetADPropertyValue("initials", result.Properties);
                user.Phone = GetADPropertyValue("telephoneNumber", result.Properties);
                user.PhoneOther = GetADPropertyMultiValue("otherTelephone", result.Properties);
                user.Email = GetADPropertyValue("mail", result.Properties);
                user.Description = GetADPropertyValue("description", result.Properties);
                user.Office = GetADPropertyValue("physicalDeliveryOfficeName", result.Properties);
                user.WebPage = GetADPropertyValue("wWWHomePage", result.Properties);
                user.WebPageOther = GetADPropertyMultiValue("url", result.Properties);
                user.DisplayName = GetADPropertyValue("displayName", result.Properties);
                user.StreetName = GetADPropertyValue("streetAddress", result.Properties);
                user.PostOfficeBox = GetADPropertyValue("postOfficeBox", result.Properties);
                user.City = GetADPropertyValue("l", result.Properties);
                user.Province = GetADPropertyValue("st", result.Properties);
                user.PostalCode = GetADPropertyValue("postalCode", result.Properties);
                user.Country = GetADPropertyValue("co", result.Properties);
                user.CountryAbbr = GetADPropertyValue("c", result.Properties);
                user.HomePhone = GetADPropertyValue("homePhone", result.Properties);
                user.HomePhoneOther = GetADPropertyMultiValue("otherHomePhone", result.Properties);
                user.Pager = GetADPropertyValue("pager", result.Properties);
                user.PagerOther = GetADPropertyMultiValue("otherPager", result.Properties);
                user.Mobile = GetADPropertyValue("mobile", result.Properties);
                user.MobileOther = GetADPropertyMultiValue("otherMobile", result.Properties);
                user.Fax = GetADPropertyValue("facsimileTelephoneNumber", result.Properties);
                user.FaxOther = GetADPropertyMultiValue("otherFacsimileTelephoneNumber", result.Properties);
                user.IpPhone = GetADPropertyValue("ipPhone", result.Properties);
                user.IpPhoneOther = GetADPropertyMultiValue("otherIpPhone", result.Properties);
                user.Note = GetADPropertyValue("info", result.Properties);
                user.Title = GetADPropertyValue("title", result.Properties);
                user.Department = GetADPropertyValue("department", result.Properties);
                user.Company = GetADPropertyValue("company", result.Properties);
                user.Manager = GetADPropertyValue("manager", result.Properties);

                string regExp = "CN=([^,]+),(.*)";
                string dn = GetADPropertyValue("distinguishedName", result.Properties);

                dn = dn.Replace(@"\,", " ");
                var match = Regex.Match(dn, regExp, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    user.OrganizationalUnit = match.Groups[2].Value;
                }

                // retrieve the manager's display name.
                if (!string.IsNullOrEmpty(user.Manager.Trim()))
                {
                    string managername = user.Manager.Replace(@"\,", " ");
                    match = Regex.Match(managername, regExp, RegexOptions.IgnoreCase);
                    string managerOU = "";
                    if (match.Success)
                    {
                        user.ManagerName = match.Groups[1].Value;
                        managerOU = match.Groups[2].Value;
                    }

                    var managerDe = GetDirectoryEntry(managerOU, true);
                    var managerProperties = new[] { "objectGUID", "displayName" };

                    var managerDeSearch = new DirectorySearcher(managerDe);
                    managerDeSearch.Filter = string.Format("(&(objectClass=user)(cn={0}))", user.ManagerName);
                    managerDeSearch.PropertiesToLoad.AddRange(managerProperties);
                    var managerResult = managerDeSearch.FindOne();
                    if (!(managerResult == default))
                    {
                        user.ManagerName = GetADPropertyValue("displayName", managerResult.Properties);
                    }
                }

                string whenCreated = GetADPropertyValue("whenCreated", result.Properties);
                if (!string.IsNullOrEmpty(whenCreated))
                {
                    user.CreatedOn = Convert.ToDateTime(whenCreated).ToLocalTime();
                }

                user.CreatedBy = GetADPropertyValue("creator", result.Properties);

                user.PasswordLastChanged = Utility.ConvertFromADDateFormat(long.Parse(GetADPropertyValue("pwdLastSet", result.Properties)));
                /* 
                #error Cannot convert AssignmentStatementSyntax - see comment for details
                                Cannot convert AssignmentStatementSyntax, System.Collections.Generic.KeyNotFoundException: The given key was not present in the dictionary.
                                   at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
                                   at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<CreateLocals>d__7.MoveNext()
                                --- End of stack trace from previous location where exception was thrown ---
                                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                                   at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<AddLocalVariables>d__6.MoveNext()
                                --- End of stack trace from previous location where exception was thrown ---
                                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                                   at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<DefaultVisitInnerAsync>d__3.MoveNext()

                                Input:
                                                user.PasswordLastChanged = Global.CCO.ActiveDirectoryLibrary.Utility.ConvertFromADDateFormat(CLng(MyBase.GetADPropertyValue("pwdLastSet", result.Properties)))

                */

                user.PhoneOther = GetADPropertyMultiValue("otherTelephone", result.Properties);
                if (result.Properties.Contains("memberOf") == true)
                {
                    user.MemberOf = Utility.GetGroupNameFromDN(Utility.ConvertMultiValuedToStringArray(result.Properties["memberOf"]));
                }

                user.CreatedBy = GetOwner(result.GetDirectoryEntry());
                // account options
                int exp = Convert.ToInt32(result.Properties["userAccountControl"][0]);

                // Use "msDS-User-Account-Control-Computed" property instead of "userAccountControl" to determine locked accounts
                // The "msDS-User-Account-Control-Computed" is only available in Windows 2003 or later                
                int expComputed = -1;
                if (result.Properties.Contains("msDS-User-Account-Control-Computed"))
                {
                    expComputed = Convert.ToInt32(result.Properties["msDS-User-Account-Control-Computed"][0]);
                }

                // UF_DONT_EXPIRE_PASSWD 0x10000
                if ((exp & Constants.ACCOUNT_OPTION_PWD_NEVER_EXPIRES) == Constants.ACCOUNT_OPTION_PWD_NEVER_EXPIRES)
                {
                    user.AccountOptionPasswordNeverExpire = true;
                }

                if ((exp & Constants.ACCOUNT_OPTION_ACCOUNT_DISABLED) == Constants.ACCOUNT_OPTION_ACCOUNT_DISABLED)
                {
                    user.AccountOptionAccountDisabled = true;
                }

                if ((exp & Constants.ACCOUNT_OPTION_PWD_REVERSIBLE_ENCRYPTION) == Constants.ACCOUNT_OPTION_PWD_REVERSIBLE_ENCRYPTION)
                {
                    user.AccountOptionPasswordReversibleEncryption = true;
                }

                // account locked
                user.AccountOptionAccountLocked = false;
                if (expComputed != -1)
                {
                    if ((expComputed & Constants.ACCOUNT_OPTION_ACCOUNT_LOCKOUT) == Constants.ACCOUNT_OPTION_ACCOUNT_LOCKOUT)
                    {
                        user.AccountOptionAccountLocked = true;
                    }
                }

                if (!(result.Properties["pwdLastSet"] == default) && Convert.ToInt64(result.Properties["pwdLastSet"][0]) == (long)0)
                {
                    user.AccountOptionUserMustChangePasswordNextLogon = true;
                }

                user.AccountOptionUserCannotChangePassword = GetUserCannotChangePassword(result.GetDirectoryEntry());
                return user;
            }

            return null;
        }

        private void AddUpdateUser(Account user, DirectoryEntry userDe)
        {            
            SetADProperty("sAMAccountName", user.LogonName, userDe.Properties);            
            SetADProperty("userPrincipalName", user.UPN, userDe.Properties);
            SetADProperty("givenName", user.FirstName, userDe.Properties);            
            SetADProperty("sn", user.LastName, userDe.Properties);            
            SetADProperty("initials", user.MiddleInitial, userDe.Properties);            
            SetADProperty("displayName", user.DisplayName, userDe.Properties);            
            SetADProperty("description", user.Description, userDe.Properties);            
            SetADProperty("telephoneNumber", user.Phone, userDe.Properties);            
            SetMultiValueADProperty("otherTelephone", user.PhoneOther, userDe.Properties);           
            SetADProperty("mail", user.Email, userDe.Properties);            
            SetADProperty("physicalDeliveryOfficeName", user.Office, userDe.Properties);            
            SetADProperty("wWWHomePage", user.WebPage, userDe.Properties);            
            SetMultiValueADProperty("url", user.WebPageOther, userDe.Properties);            
            SetADProperty("streetAddress", user.StreetName, userDe.Properties);            
            SetADProperty("postOfficeBox", user.PostOfficeBox, userDe.Properties);            
            SetADProperty("l", user.City, userDe.Properties);            
            SetADProperty("st", user.Province, userDe.Properties);            
            SetADProperty("postalCode", user.PostalCode, userDe.Properties);            
            SetADProperty("co", user.Country, userDe.Properties);            
            SetADProperty("c", user.CountryAbbr, userDe.Properties);           
            SetADProperty("homePhone", user.HomePhone, userDe.Properties);            
            SetMultiValueADProperty("otherHomePhone", user.HomePhoneOther, userDe.Properties);            
            SetADProperty("pager", user.Pager, userDe.Properties);            
            SetMultiValueADProperty("otherPager", user.PagerOther, userDe.Properties);            
            SetADProperty("mobile", user.Mobile, userDe.Properties);            
            SetMultiValueADProperty("otherMobile", user.MobileOther, userDe.Properties);            
            SetADProperty("facsimileTelephoneNumber", user.Fax, userDe.Properties);            
            SetMultiValueADProperty("otherFacsimileTelephoneNumber", user.FaxOther, userDe.Properties);            
            SetADProperty("ipPhone", user.IpPhone, userDe.Properties);            
            SetMultiValueADProperty("otherIpPhone", user.IpPhoneOther, userDe.Properties);            
            SetADProperty("info", user.Note, userDe.Properties);            
            SetADProperty("title", user.Title, userDe.Properties);            
            SetADProperty("department", user.Department, userDe.Properties);           
            SetADProperty("company", user.Company, userDe.Properties);
            if (user.UpdateUserRole == Account.UpdateUserRoleType.DomainAdmin)
            {                
                SetADProperty("manager", user.Manager, userDe.Properties);
            }

            userDe.CommitChanges();

            // set Account Options
            SetAccountOptions(user, userDe);

            // We should be careful to only add/delete what we need to. This
            // means not deleting and re-creating everything.

            if (user.MemberOf != null)
            {
                IEnumerable<string> oldGroups = Utility.GetGroupNameFromDN(Utility.ConvertMultiValuedToStringArray(userDe.Properties["memberOf"]));
                var groupsToRemove = oldGroups.Except(user.MemberOf);
                var groupsToAdd = user.MemberOf.Except(oldGroups);
                foreach (string gName in groupsToRemove)
                {
                    // get directory entry from groups path
                    var deGroup = GetDEtoRemoveUserFromGroup(gName);
                    try
                    {
                        RemoveUserFromGroup(deGroup, userDe);
                    }
                    catch (Exception ex)
                    {
                        // throw exception only if this is not domain user group. This is the default primary group
                        if (Constants.DOMAIN_USER_GROUP != (gName ?? ""))
                        {
                            throw;
                        }
                    }
                }

                // add new group membership to ad.
                var deRoot = GetDirectoryEntry();
                using (deRoot)
                    foreach (string groupName in groupsToAdd)
                    {
                        if (!string.IsNullOrEmpty(groupName))
                        {
                            AddUserToGroup(deRoot, userDe, groupName);
                        }
                    }
            }
        }

        public bool AuthenticateUser(string userName, string password)
        {
            string ldapPath = "LDAP://" + DomainControllerAddress;
            string completeName = DomainName + @"\" + userName;
            var de = new DirectoryEntry(ldapPath, completeName, password);

            // Set AutheticationType to use Kerberos (Sealing)
            de.AuthenticationType = AuthenticationTypes.Sealing & AuthenticationTypes.Sealing;
            try
            {
                // bind to NativeAdsObject to force authentication
                var obj = de.NativeObject;
                var searcher = new DirectorySearcher(de);
                searcher.Filter = "(&(objectClass=User)(SAMAccountName=" + EscapeInvalidLDAPSearchCharacters(userName) + "))";
                var result = searcher.FindOne();
                if (result == default)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return false;
        }

        public bool ResetPassword(string userGuid, string newPassword)
        {
            try
            {
                var result = SearchGUID(userGuid);
                if (!(result == default))
                {
                    var de = result.GetDirectoryEntry();
                    bool forceSet = de.IsForcePasswordSet();
                    de.Invoke("SetPassword", new object[] { newPassword });
                    if (forceSet)
                    {
                        de.RefreshCache();
                        de.ForcePasswordSet(true);
                        de.CommitChanges();
                    }
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return false;
        }

        public bool ChangePassword(string userGuid, string oldPassword, string newPassword)
        {
            try
            {
                var result = SearchGUID(userGuid);
                if (!(result == default))
                {
                    result.GetDirectoryEntry().Invoke("ChangePassword", new object[] { oldPassword, newPassword });
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return false;
        }

        public List<Account> SearchAccountList(SearchAccountCriteria criteria)
        {
            SearchResultCollection results = null;
            try
            {
                string sFilter = "";
                string logonNameFilter = "";
                string firstNameFilter = "";
                string lastNameFilter = "";
                DirectoryEntry de;
                if (!string.IsNullOrEmpty(criteria.OU))
                {
                    try
                    {
                        de = GetDirectoryEntry(criteria.OU, true);
                    }
                    catch (Exception ex)
                    {
                        de = GetDirectoryEntry();
                    }
                }
                else
                {
                    de = GetDirectoryEntry();
                }

                var users = new List<Account>();
                var deSearch = new DirectorySearcher(de);

                // do validation

                if (!string.IsNullOrEmpty(criteria.LogonName.Trim()))
                {
                    logonNameFilter = string.Format("(sAMAccountName={0})", EscapeInvalidLDAPWildCharSearch(criteria.LogonName));
                }

                if (!string.IsNullOrEmpty(criteria.FirstName.Trim()))
                {
                    firstNameFilter = string.Format("(givenName={0})", EscapeInvalidLDAPWildCharSearch(criteria.FirstName));
                }

                if (!string.IsNullOrEmpty(criteria.LastName.Trim()))
                {
                    lastNameFilter = string.Format("(sn={0})", EscapeInvalidLDAPWildCharSearch(criteria.LastName));
                }

                sFilter = string.Format("(&(objectClass=user)(objectCategory=person){0}{1}{2})", logonNameFilter, firstNameFilter, lastNameFilter);
                deSearch.Filter = sFilter;

                // sort by User Name
                var deSort = new SortOption("sAMAccountName", SortDirection.Ascending);
                deSearch.Sort = deSort;

                // set Properties to load
                var propertiesToLoad = new[] { "sAMAccountName", "givenName", "sn", "initials", "displayName", "distinguishedName" };

                deSearch.PropertiesToLoad.AddRange(propertiesToLoad);

                // set page size
                deSearch.PageSize = 1000;
                results = deSearch.FindAll();
                if (results.Count > 0)
                {
                    foreach (SearchResult result in results)
                    {
                        var user = new Account();

                        user.LogonName = GetADPropertyValue("sAMAccountName", result.Properties);
                        user.FirstName = GetADPropertyValue("givenName", result.Properties);
                        user.LastName = GetADPropertyValue("sn", result.Properties);
                        user.MiddleInitial = GetADPropertyValue("initials", result.Properties);
                        user.DisplayName = GetADPropertyValue("displayName", result.Properties);
                        user.DistinguishedName = GetADPropertyValue("distinguishedName", result.Properties);
                        users.Add(user);
                    }
                }

                return users;
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }
            finally
            {
                // required to avoid memory leaks
                if (!(results == default))
                {
                    results.Dispose();
                }
            }

            return null;
        }

        public List<Account> SearchAccount(SearchAccountCriteria criteria, List<Guid> guids)
        {
            var results = default(SearchResultCollection);
            try
            {
                string sFilter = "";
                string guidFilter = "";
                string logonNameFilter = "";
                string firstNameFilter = "";
                string lastNameFilter = "";
                string diabledAccountFilter = "";
                string passwordExpiredFilter = "";
                string accountExistsInADFilter = "";
                DirectoryEntry de;
                if (!string.IsNullOrEmpty(criteria.OU))
                {
                    try
                    {
                        de = GetDirectoryEntry(criteria.OU, true);
                    }
                    catch (Exception ex)
                    {
                        de = GetDirectoryEntry();
                    }
                }
                else
                {
                    de = GetDirectoryEntry();
                }

                var users = new List<Account>();
                var deSearch = new DirectorySearcher(de);
                if (guids != null)
                {
                    if (!string.IsNullOrWhiteSpace(criteria.GUID))
                    {
                        criteria.GUID = criteria.GUID.Trim();
                        if (!Utility.IsValidGUID(criteria.GUID))
                        {
                            return users;
                        }

                        guidFilter = string.Format("(objectGUID={0})", Utility.GuidToOctetString(criteria.GUID));
                    }
                }
                else if (guids.Count < 1)
                {
                    // If we did an application search, but there are no results,
                    // return nothing
                    return users;
                }
                else
                {
                    // (|(objectGUID=a)(objectGUID=b)
                    guidFilter = "(|(objectGUID=" + guids.Select(g => Utility.GuidToOctetString(g.ToString())).Aggregate((a, b) => a + ")(objectGUID=" + b) + "))";


                }

                if (!string.IsNullOrEmpty(criteria.LogonName.Trim()))
                {
                    logonNameFilter = string.Format("(sAMAccountName={0})", EscapeInvalidLDAPWildCharSearch(criteria.LogonName));
                }

                if (!string.IsNullOrEmpty(criteria.FirstName.Trim()))
                {
                    firstNameFilter = string.Format("(givenName={0})", EscapeInvalidLDAPWildCharSearch(criteria.FirstName));
                }

                if (!string.IsNullOrEmpty(criteria.LastName.Trim()))
                {
                    lastNameFilter = string.Format("(sn={0})", EscapeInvalidLDAPWildCharSearch(criteria.LastName));
                }

                if (criteria.DisabledAccount == true)
                {
                    diabledAccountFilter = "(userAccountControl:1.2.840.113556.1.4.803:=2)";
                }

                if (criteria.PasswordExpired == true)
                {
                    var expiryCreationDate = DateTime.Now.AddTicks(-GetMaxPasswordAgeForDomain());
                    long expiryTicks = expiryCreationDate.ToFileTime();

                    // Any of the following could signify an expired password, so use "or"
                    passwordExpiredFilter = "(|" + "(userAccountControl:1.2.840.113556.1.4.803:=8388608)" + "(pwdlastSet=0)" + string.Format("(pwdLastSet<={0})", expiryTicks) + ")";
                }

                sFilter = string.Format("(&(objectClass=user)(objectCategory=person){0}{1}{2}{3}{4}{5})", guidFilter, logonNameFilter, firstNameFilter, lastNameFilter, diabledAccountFilter, passwordExpiredFilter);
                deSearch.Filter = sFilter;

                // sort by User Name
                var deSort = new SortOption("sAMAccountName", SortDirection.Ascending);
                deSearch.Sort = deSort;

                // set Properties to load
                var propertiesToLoad = new[] { "objectGUID", "sAMAccountName", "userPrincipalName", "givenName", "sn", "initials",
                    "displayName", "description", "physicalDeliveryOfficeName", "telephoneNumber", "otherTelephone", "mail",
                    "wWWHomePage", "url", "streetAddress", "postOfficeBox", "l", "st", "postalCode", "co", "distinguishedName",
                    "homePhone", "otherHomePhone", "pager", "otherPager", "mobile", "otherMobile", "facsimileTelephoneNumber",
                    "otherFacsimileTelephoneNumber", "ipPhone", "otherIpPhone", "info", "title", "department", "company", "manager",
                    "userAccountControl", "msDS-User-Account-Control-Computed", "MemberOf", "whenCreated", "creator", "pwdLastSet" };

                deSearch.PropertiesToLoad.AddRange(propertiesToLoad);

                // set page size
                deSearch.PageSize = 1000;
                results = deSearch.FindAll();
                string regExp = "CN=[^,]+,(.*)";
                if (results.Count > 0)
                {

                    // get max password age to compute expiry date
                    long maxPwdAge;
                    maxPwdAge = GetMaxPasswordAgeForDomain();
                    foreach (SearchResult result in results)
                    {
                        var user = new Account();
                        user.Guid = result.GetDirectoryEntry().Guid.ToString();

                        user.LogonName = GetADPropertyValue("sAMAccountName", result.Properties);
                        user.UPN = GetADPropertyValue("userPrincipalName", result.Properties);
                        user.FirstName = GetADPropertyValue("givenName", result.Properties);

                        user.LastName = GetADPropertyValue("sn", result.Properties);
                        user.MiddleInitial = GetADPropertyValue("initials", result.Properties);
                        user.Phone = GetADPropertyValue("telephoneNumber", result.Properties);
                        user.PhoneOther = GetADPropertyMultiValue("otherTelephone", result.Properties);
                        user.Email = GetADPropertyValue("mail", result.Properties);
                        user.Description = GetADPropertyValue("description", result.Properties);
                        user.Office = GetADPropertyValue("physicalDeliveryOfficeName", result.Properties);
                        user.WebPage = GetADPropertyValue("wWWHomePage", result.Properties);
                        user.WebPageOther = GetADPropertyMultiValue("url", result.Properties);
                        user.DisplayName = GetADPropertyValue("displayName", result.Properties);
                        user.StreetName = GetADPropertyValue("streetAddress", result.Properties);
                        user.PostOfficeBox = GetADPropertyValue("postOfficeBox", result.Properties);
                        user.City = GetADPropertyValue("l", result.Properties);
                        user.Province = GetADPropertyValue("st", result.Properties);
                        user.PostalCode = GetADPropertyValue("postalCode", result.Properties);
                        user.Country = GetADPropertyValue("co", result.Properties);
                        user.HomePhone = GetADPropertyValue("homePhone", result.Properties);
                        user.HomePhoneOther = GetADPropertyMultiValue("otherHomePhone", result.Properties);
                        user.Pager = GetADPropertyValue("pager", result.Properties);
                        user.PagerOther = GetADPropertyMultiValue("otherPager", result.Properties);
                        user.Mobile = GetADPropertyValue("mobile", result.Properties);
                        user.MobileOther = GetADPropertyMultiValue("otherMobile", result.Properties);
                        user.Fax = GetADPropertyValue("facsimileTelephoneNumber", result.Properties);
                        user.FaxOther = GetADPropertyMultiValue("otherFacsimileTelephoneNumber", result.Properties);
                        user.IpPhone = GetADPropertyValue("ipPhone", result.Properties);
                        user.IpPhoneOther = GetADPropertyMultiValue("otherIpPhone", result.Properties);
                        user.Note = GetADPropertyValue("info", result.Properties);
                        user.Title = GetADPropertyValue("title", result.Properties);
                        user.Department = GetADPropertyValue("department", result.Properties);
                        user.Company = GetADPropertyValue("company", result.Properties);
                        user.Manager = GetADPropertyValue("manager", result.Properties);
                        string dn = GetADPropertyValue("distinguishedName", result.Properties);
                        dn = dn.Replace(@"\,", " ");
                        var match = Regex.Match(dn, regExp, RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            user.OrganizationalUnit = match.Groups[1].Value;
                        }

                        string whenCreated = GetADPropertyValue("whenCreated", result.Properties);
                        if (!string.IsNullOrEmpty(whenCreated))
                        {
                            user.CreatedOn = Convert.ToDateTime(whenCreated).ToLocalTime();
                        }

                        user.CreatedBy = GetOwner(result.GetDirectoryEntry());
                        ;

                        // account options
                        int exp = Convert.ToInt32(result.Properties["userAccountControl"][0]);

                        // URS-18: ISAAC-Users account are getting locked in AD but their account in UMA shows unlocked. 
                        // 
                        // Use "msDS-User-Account-Control-Computed" property instead of "userAccountControl" to determine locked accounts
                        // The "msDS-User-Account-Control-Computed" is only available in Windows 2003 or later                
                        int expComputed = -1;
                        if (result.Properties.Contains("msDS-User-Account-Control-Computed"))
                        {
                            expComputed = Convert.ToInt32(result.Properties["msDS-User-Account-Control-Computed"][0]);
                        }

                        // UF_DONT_EXPIRE_PASSWD 0x10000
                        if ((exp & Constants.ACCOUNT_OPTION_PWD_NEVER_EXPIRES) == Constants.ACCOUNT_OPTION_PWD_NEVER_EXPIRES)
                        {
                            user.AccountOptionPasswordNeverExpire = true;
                        }

                        // Account state
                        if ((exp & Constants.ACCOUNT_OPTION_ACCOUNT_DISABLED) == Constants.ACCOUNT_OPTION_ACCOUNT_DISABLED)
                        {
                            user.AccountOptionAccountDisabled = true;
                        }
                        else
                        {
                            user.AccountOptionAccountDisabled = false;
                        }

                        // Account state (locked)
                        user.AccountOptionAccountLocked = false;
                        if (expComputed != -1)
                        {
                            if ((expComputed & Constants.ACCOUNT_OPTION_ACCOUNT_LOCKOUT) == Constants.ACCOUNT_OPTION_ACCOUNT_LOCKOUT)
                            {
                                user.AccountOptionAccountLocked = true;
                            }
                        }

                        if (user.AccountOptionPasswordNeverExpire == false && !user.IsForcePasswordSet)
                        {
                            user.PasswordExpiryDate = user.PasswordLastChanged.AddTicks(maxPwdAge);
                        }

                        users.Add(user);
                    }
                }

                return users;
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }
            finally
            {
                // required to avoid memory leaks
                if (!(results == default))
                {
                    results.Dispose();
                }
            }

            return null;
        }

        public Account GetAccountByCommonName(string firstName, string init, string lastName)
        {
            try
            {
                return GetADAccount(string.Format("(&(objectClass=user)(cn={0}))", BuildUserDisplayName(firstName, init, lastName)));
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return null;
        }

        public Account GetAccount(string userName)
        {
            try
            {
                return GetADAccount(string.Format("(&(objectClass=user)(sAMAccountName={0}))", userName));
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return null;
        }

        public Account GetAccountByGuid(string guid)
        {
            try
            {
                return GetADAccount(string.Format("(objectGUID={0})", Utility.GuidToOctetString(guid)));
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return null;
        }

        public Account GetAccountByDistinguishedName(string distinguishedName)
        {
            try
            {
                return GetADAccount(string.Format("(CN={0})", distinguishedName));
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return null;
        }

        public Account GetAccountByUserPrincipalName(string upn)
        {
            try
            {
                return GetADAccount(string.Format("(&(objectClass=user)(userPrincipalName={0}))", upn));
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return null;
        }

        public bool AddAccountWithSuffix(Account user, string containerFullPath, string suffix)
        {
            string cn = EscapeInvalidLDAPCharacters(BuildUserDisplayName(user.FirstName, user.MiddleInitial, user.LastName) + suffix);
            return AddAccount(user, containerFullPath, cn);
        }

        public bool AddAccount(Account user, string containerFullPath)
        {
            string cn = EscapeInvalidLDAPCharacters(BuildUserDisplayName(user.FirstName, user.MiddleInitial, user.LastName));
            return AddAccount(user, containerFullPath, cn);
        }

        public bool AddAccount(Account user, string containerFullPath, string cn)
        {
            if (!PathExists(containerFullPath))
            {
                return false;
            }

            try
            {
                var de = GetDirectoryEntry(containerFullPath, true);
                var userDe = de.Children.Add("CN=" + cn, "user");
                
                SetADProperty("sAMAccountName", EscapeLogonChrs(user.LogonName), userDe.Properties);                
                SetADProperty("userPrincipalName", EscapeLogonChrs(user.LogonName), userDe.Properties);                
                SetADProperty("givenName", user.FirstName, userDe.Properties);                
                SetADProperty("sn", user.LastName, userDe.Properties);
                de.CommitChanges();
                userDe.CommitChanges();
                user.Guid = userDe.Guid.ToString();
                if (!string.IsNullOrEmpty(user.LogonPassword))
                {
                    ResetPassword(user.Guid, user.LogonPassword);
                }

                // CCR03650: This must happen after ResetPassword(), or else
                // AccountOptionUserMustChangePasswordNextLogon is overwritten.
                AddUpdateUser(user, userDe);
            }
            catch (Exception ex)
            {
                // failed to create user, delete the user and return a failure message
                Guid parsed;
                if (Guid.TryParse(user.Guid, out parsed))
                {
                    DeleteUser(user.Guid);
                }

                ActiveDirectoryException.ReThrowException(ex);
            }

            return true;
        }

        public bool UpdateAccount(Account user)
        {
            try
            {
                var result = SearchGUID(user.Guid);
                if (!(result == default))
                {
                    var userDe = result.GetDirectoryEntry();
                    AddUpdateUser(user, userDe);
                }
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return default;
        }

        public bool DeleteUser(string userGuid)
        {
            try
            {
                var result = SearchGUID(userGuid);
                if (!(result == default))
                {
                    result.GetDirectoryEntry().DeleteTree();
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return default;
        }

        public bool UnlockAccount(string userGuid)
        {
            try
            {
                var result = SearchGUID(userGuid);
                if (!(result == default))
                {
                    var deUser = result.GetDirectoryEntry();
                    int exp = Convert.ToInt32(deUser.Properties["userAccountControl"][0]);
                    exp = exp & ~Constants.ACCOUNT_OPTION_ACCOUNT_LOCKOUT;
                    deUser.Properties["userAccountControl"].Value = exp;
                    deUser.CommitChanges();
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return true;
        }

        public bool EnableAccount(string userGuid)
        {
            try
            {
                var result = SearchGUID(userGuid);
                if (!(result == default))
                {
                    var deUser = result.GetDirectoryEntry();
                    int exp = Convert.ToInt32(deUser.Properties["userAccountControl"][0]);

                    // enable account
                    exp = exp & ~Constants.ACCOUNT_OPTION_ACCOUNT_DISABLED;
                    deUser.Properties["userAccountControl"].Value = exp;
                    deUser.CommitChanges();
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return true;
        }

        public bool DisableAccount(string userGuid)
        {
            try
            {
                var result = SearchGUID(userGuid);
                if (!(result == default))
                {
                    var deUser = result.GetDirectoryEntry();
                    int exp = Convert.ToInt32(deUser.Properties["userAccountControl"][0]);

                    // disable account
                    exp = exp | Constants.ACCOUNT_OPTION_ACCOUNT_DISABLED;
                    deUser.Properties["userAccountControl"].Value = exp;
                    deUser.CommitChanges();
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return true;
        }

        public bool IsUserInGroup(string userGuid, string groupName)
        {
            try
            {
                var result = SearchGUID(userGuid);
                if (!(result == default))
                {
                    var userDe = result.GetDirectoryEntry();
                    var userGroups = Utility.GetGroupNameFromDN(Utility.ConvertMultiValuedToStringArray(userDe.Properties["memberOf"]));
                    if (!(userGroups == default))
                    {
                        foreach (string group in userGroups)
                        {
                            if ((group != null) && (group.ToLower() ?? "") == (groupName.ToLower() ?? ""))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return false;
        }

        /// <summary>
        /// Gets all available UPN suffixes from the domain controller.
        /// https://social.microsoft.com/Forums/en-US/7183c252-3b57-4f32-8cea-c2622bb82d14/how-to-get-list-all-upn-suffixes-in-c-code?forum=crm
        /// https://social.msdn.microsoft.com/forums/vstudio/en-US/d3e4cc17-9391-4d55-8416-4c952a01f762/get-upn-suffix-list-from-active-directory
        /// </summary>       
        public List<string> GetUpnSuffixes()
        {
            DirectoryEntry de;
            DirectoryEntry dd;
            DirectoryEntry dp;
            var upnSuffixes = new List<string>();
            SearchResultCollection results = null;
            string namingContext = "";
            string configContext = "";
            string domainName = "";
            string canonicalName = "";
            try
            {

                // Get naming contexts
                de = GetDirectoryEntry("RootDSE", true);
                de.RefreshCache();
                namingContext = de.Properties["defaultNamingContext"].Value.ToString();
                configContext = de.Properties["configurationNamingContext"].Value.ToString();

                // Get current domain name
                dd = new DirectoryEntry("LDAP://" + namingContext);
                domainName = dd.Properties["name"].Value.ToString();
                dp = new DirectoryEntry("LDAP://CN=Partitions," + configContext);

                // Add the default UPN suffix (domain name)
                // http://blogs.msmvps.com/richardsiddaway/2012/01/12/finding-upn-suffixes/
                dd.Invoke("GetInfoEx", new object[] { "canonicalName" }, 0);
                canonicalName = dd.InvokeGet("canonicalName").ToString();
                canonicalName = canonicalName.Replace("/", ""); // Strip the final "/"
                upnSuffixes.Add(canonicalName);

                // Get additional UPN suffixes
                var dpSearch = new DirectorySearcher(dp);
                dpSearch.PropertiesToLoad.Add("uPNSuffixes");
                results = dpSearch.FindAll();
                foreach (SearchResult result in results)
                {
                    foreach (string propertyName in result.Properties.PropertyNames)
                    {
                        if ((propertyName.ToLower() ?? "") == "upnsuffixes")
                        {
                            foreach (object retEntry in result.Properties[propertyName])
                                upnSuffixes.Add(retEntry.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }
            finally
            {
                if (!(results == default))
                {
                    results.Dispose();
                }
            }

            return upnSuffixes;
        }


        public List<string> GetGroups()
        {
            // Connect to the AD + Get Diectory
            DirectoryEntry de;
            var adGroups = new List<string>();
            var results = default(SearchResultCollection);
            try
            {
                de = GetDirectoryEntry();
                var deSearch = new DirectorySearcher(de);
                deSearch.Filter = "(objectClass=group)";
                var deSort = new SortOption("Name", SortDirection.Ascending);
                deSearch.Sort = deSort;
                results = deSearch.FindAll();
                foreach (SearchResult result in results)
                {
                    string tempName = result.GetDirectoryEntry().Name.ToString();
                    if (tempName.IndexOf("CN=") > 0)
                    {
                        adGroups.Add(tempName.Substring(3));
                    }
                    else
                    {
                        adGroups.Add(tempName);
                    }
                }
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }
            finally
            {
                if (!(results == default))
                {
                    results.Dispose();
                }
            }

            return adGroups;
        }

        public bool AddUserToGroup(string userGuid, string groupName)
        {
            DirectoryEntry de;
            DirectoryEntry userDe;
            SearchResult result;
            try
            {
                de = GetDirectoryEntry();
                var deSearch = new DirectorySearcher(de);
                deSearch.Filter = string.Format("(&(objectClass=user)(objectGUID={0}))", Utility.GuidToOctetString(userGuid));
                result = deSearch.FindOne();
                if (!(result == default))
                {
                    userDe = result.GetDirectoryEntry();
                    AddUserToGroup(de, userDe, groupName);
                }
            }
            catch (Exception ex)
            {
                ActiveDirectoryException.ReThrowException(ex);
            }

            return default;
        }

        public bool RemoveUserFromGroup(string userGuid, string groupName)
        {
            DirectoryEntry de;
            DirectoryEntry userDe;
            SearchResult result;
            SearchResultCollection results;
            DirectoryEntry deGroup;
            try
            {
                de = GetDirectoryEntry();
                var deSearch = new DirectorySearcher(de);
                deSearch.Filter = string.Format("(&(objectClass=user)(objectGUID={0}))", Utility.GuidToOctetString(userGuid));
                result = deSearch.FindOne();
                if (!(result == default))
                {
                    userDe = result.GetDirectoryEntry();
                    deSearch.Filter = "(&(objectClass=group) (cn=" + EscapeInvalidLDAPSearchCharacters(groupName) + "))";
                    results = deSearch.FindAll();
                    if (results.Count > 0)
                    {
                        deGroup = results[0].GetDirectoryEntry();
                        RemoveUserFromGroup(deGroup, userDe);
                    }
                }
            }
            catch (Exception ex)
            {
                // throw exception only if this is not domain user group. This is the default primary group
                if (Constants.DOMAIN_USER_GROUP != (groupName ?? ""))
                {
                    throw;
                }
            }

            return default;
        }

        public bool GroupExists(string groupName)
        {
            DirectoryEntry de;
            SearchResult result;
            try
            {
                de = GetDirectoryEntry();
                var deSearch = new DirectorySearcher(de);
                deSearch.Filter = "(&(objectClass=group) (cn=" + EscapeInvalidLDAPSearchCharacters(groupName) + "))";
                result = deSearch.FindOne();
                if (!(result == default))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
            }

            return false;
        }

        public void MoveUserTo(string userGUID, string moveToNewParentGUID)
        {
            DirectoryEntry de;
            DirectoryEntry pDe;
            de = GetDirectoryEntry(userGUID);
            if (de is object)
            {
                pDe = GetDirectoryEntry(moveToNewParentGUID);
                de.MoveTo(pDe);
                de.CommitChanges();
            }
        }
    }
}