
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;
using GZ.ActiveDirectoryLibrary.Exceptions;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

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

        /// <summary>
        /// Escapes invalid logon Character
        /// </summary>
        /// <param name="logon"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private string EscapeLogonChrs(string logon)
        {
            foreach (char c in Constants.LOGON_ESCAPE_CHRS.ToCharArray())
                logon = logon.Replace(Conversions.ToString(c), @"\" + Conversions.ToString(c));
            return logon;
        }

        /// <summary>
        /// Helper function to set Account options atrributes
        /// </summary>
        /// <param name="user"></param>
        /// <param name="de"></param>
        /// <remarks></remarks>
        private void SetAccountOptions(Account user, DirectoryEntry de)
        {
            int exp = Conversions.ToInteger(de.Properties["userAccountControl"].Value);

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

        /// <summary>
        /// Set the user cannot change password attribute on adirectory entry object
        /// </summary>
        /// <param name="cannotChangePwd"></param>
        /// <param name="userde"></param>
        /// <remarks></remarks>
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
                return Strings.Trim(firstName + " " + middleInitial + ". " + lastName);
            }
            else
            {
                return Strings.Trim(firstName + " " + lastName);
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
                    return Math.Abs(Conversions.ToLong(results[0].Properties["maxPwdAge"][0]));
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
                user.Guid = result.GetDirectoryEntry().Guid.ToString();
                var argproperties = result.Properties;
                user.LogonName = GetADPropertyValue("sAMAccountName", ref argproperties);
                var argproperties1 = result.Properties;
                user.UPN = GetADPropertyValue("userPrincipalName", ref argproperties1);
                var argproperties2 = result.Properties;
                user.FirstName = GetADPropertyValue("givenName", ref argproperties2);
                var argproperties3 = result.Properties;
                user.LastName = GetADPropertyValue("sn", ref argproperties3);
                var argproperties4 = result.Properties;
                user.MiddleInitial = GetADPropertyValue("initials", ref argproperties4);
                var argproperties5 = result.Properties;
                user.Phone = GetADPropertyValue("telephoneNumber", ref argproperties5);
                var argproperties6 = result.Properties;
                user.PhoneOther = GetADPropertyMultiValue("otherTelephone", ref argproperties6);
                var argproperties7 = result.Properties;
                user.Email = GetADPropertyValue("mail", ref argproperties7);
                var argproperties8 = result.Properties;
                user.Description = GetADPropertyValue("description", ref argproperties8);
                var argproperties9 = result.Properties;
                user.Office = GetADPropertyValue("physicalDeliveryOfficeName", ref argproperties9);
                var argproperties10 = result.Properties;
                user.WebPage = GetADPropertyValue("wWWHomePage", ref argproperties10);
                var argproperties11 = result.Properties;
                user.WebPageOther = GetADPropertyMultiValue("url", ref argproperties11);
                var argproperties12 = result.Properties;
                user.DisplayName = GetADPropertyValue("displayName", ref argproperties12);
                var argproperties13 = result.Properties;
                user.StreetName = GetADPropertyValue("streetAddress", ref argproperties13);
                var argproperties14 = result.Properties;
                user.PostOfficeBox = GetADPropertyValue("postOfficeBox", ref argproperties14);
                var argproperties15 = result.Properties;
                user.City = GetADPropertyValue("l", ref argproperties15);
                var argproperties16 = result.Properties;
                user.Province = GetADPropertyValue("st", ref argproperties16);
                var argproperties17 = result.Properties;
                user.PostalCode = GetADPropertyValue("postalCode", ref argproperties17);
                var argproperties18 = result.Properties;
                user.Country = GetADPropertyValue("co", ref argproperties18);
                var argproperties19 = result.Properties;
                user.CountryAbbr = GetADPropertyValue("c", ref argproperties19);
                var argproperties20 = result.Properties;
                user.HomePhone = GetADPropertyValue("homePhone", ref argproperties20);
                var argproperties21 = result.Properties;
                user.HomePhoneOther = GetADPropertyMultiValue("otherHomePhone", ref argproperties21);
                var argproperties22 = result.Properties;
                user.Pager = GetADPropertyValue("pager", ref argproperties22);
                var argproperties23 = result.Properties;
                user.PagerOther = GetADPropertyMultiValue("otherPager", ref argproperties23);
                var argproperties24 = result.Properties;
                user.Mobile = GetADPropertyValue("mobile", ref argproperties24);
                var argproperties25 = result.Properties;
                user.MobileOther = GetADPropertyMultiValue("otherMobile", ref argproperties25);
                var argproperties26 = result.Properties;
                user.Fax = GetADPropertyValue("facsimileTelephoneNumber", ref argproperties26);
                var argproperties27 = result.Properties;
                user.FaxOther = GetADPropertyMultiValue("otherFacsimileTelephoneNumber", ref argproperties27);
                var argproperties28 = result.Properties;
                user.IpPhone = GetADPropertyValue("ipPhone", ref argproperties28);
                var argproperties29 = result.Properties;
                user.IpPhoneOther = GetADPropertyMultiValue("otherIpPhone", ref argproperties29);
                var argproperties30 = result.Properties;
                user.Note = GetADPropertyValue("info", ref argproperties30);
                var argproperties31 = result.Properties;
                user.Title = GetADPropertyValue("title", ref argproperties31);
                var argproperties32 = result.Properties;
                user.Department = GetADPropertyValue("department", ref argproperties32);
                var argproperties33 = result.Properties;
                user.Company = GetADPropertyValue("company", ref argproperties33);
                var argproperties34 = result.Properties;
                user.Manager = GetADPropertyValue("manager", ref argproperties34);
                string regExp = "CN=([^,]+),(.*)";
                var argproperties35 = result.Properties;
                string dn = GetADPropertyValue("distinguishedName", ref argproperties35);
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
                        var argproperties36 = managerResult.Properties;
                        user.ManagerName = GetADPropertyValue("displayName", ref argproperties36);
                    }
                }

                var argproperties37 = result.Properties;
                string whenCreated = GetADPropertyValue("whenCreated", ref argproperties37);
                if (!string.IsNullOrEmpty(whenCreated))
                {
                    user.CreatedOn = Conversions.ToDate(whenCreated).ToLocalTime();
                }

                var argproperties38 = result.Properties;
                user.CreatedBy = GetADPropertyValue("creator", ref argproperties38);
                ;

                var argproperties39 = result.Properties;
                user.PasswordLastChanged = Utility.ConvertFromADDateFormat(long.Parse(GetADPropertyValue("pwdLastSet", ref argproperties39)));
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
                                                user.PasswordLastChanged = Global.GZ.ActiveDirectoryLibrary.Utility.ConvertFromADDateFormat(CLng(MyBase.GetADPropertyValue("pwdLastSet", result.Properties)))

                */
                var argproperties40 = result.Properties;
                user.PhoneOther = GetADPropertyMultiValue("otherTelephone", ref argproperties40);
                if (result.Properties.Contains("memberOf") == true)
                {
                    user.MemberOf = Utility.GetGroupNameFromDN(Utility.ConvertMultiValuedToStringArray(result.Properties["memberOf"]));
                }

                user.CreatedBy = GetOwner(result.GetDirectoryEntry());
                // account options
                int exp = Conversions.ToInteger(result.Properties["userAccountControl"][0]);

                // URS-18: ISAAC-Users account are getting locked in AD but their account in UMA shows unlocked. 
                // 
                // Use "msDS-User-Account-Control-Computed" property instead of "userAccountControl" to determine locked accounts
                // The "msDS-User-Account-Control-Computed" is only available in Windows 2003 or later                
                int expComputed = -1;
                if (result.Properties.Contains("msDS-User-Account-Control-Computed"))
                {
                    expComputed = Conversions.ToInteger(result.Properties["msDS-User-Account-Control-Computed"][0]);
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

                if (!(result.Properties["pwdLastSet"] == default) && Conversions.ToLong(result.Properties["pwdLastSet"][0]) == (long)0)
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
            var argproperties = userDe.Properties;
            SetADProperty("sAMAccountName", user.LogonName, ref argproperties);
            var argproperties1 = userDe.Properties;
            SetADProperty("userPrincipalName", user.UPN, ref argproperties1);
            var argproperties2 = userDe.Properties;
            SetADProperty("givenName", user.FirstName, ref argproperties2);
            var argproperties3 = userDe.Properties;
            SetADProperty("sn", user.LastName, ref argproperties3);
            var argproperties4 = userDe.Properties;
            SetADProperty("initials", user.MiddleInitial, ref argproperties4);
            var argproperties5 = userDe.Properties;
            SetADProperty("displayName", user.DisplayName, ref argproperties5);
            var argproperties6 = userDe.Properties;
            SetADProperty("description", user.Description, ref argproperties6);
            var argproperties7 = userDe.Properties;
            SetADProperty("telephoneNumber", user.Phone, ref argproperties7);
            var argproperties8 = userDe.Properties;
            SetMultiValueADProperty("otherTelephone", user.PhoneOther, ref argproperties8);
            var argproperties9 = userDe.Properties;
            SetADProperty("mail", user.Email, ref argproperties9);
            var argproperties10 = userDe.Properties;
            SetADProperty("physicalDeliveryOfficeName", user.Office, ref argproperties10);
            var argproperties11 = userDe.Properties;
            SetADProperty("wWWHomePage", user.WebPage, ref argproperties11);
            var argproperties12 = userDe.Properties;
            SetMultiValueADProperty("url", user.WebPageOther, ref argproperties12);
            var argproperties13 = userDe.Properties;
            SetADProperty("streetAddress", user.StreetName, ref argproperties13);
            var argproperties14 = userDe.Properties;
            SetADProperty("postOfficeBox", user.PostOfficeBox, ref argproperties14);
            var argproperties15 = userDe.Properties;
            SetADProperty("l", user.City, ref argproperties15);
            var argproperties16 = userDe.Properties;
            SetADProperty("st", user.Province, ref argproperties16);
            var argproperties17 = userDe.Properties;
            SetADProperty("postalCode", user.PostalCode, ref argproperties17);
            var argproperties18 = userDe.Properties;
            SetADProperty("co", user.Country, ref argproperties18);
            var argproperties19 = userDe.Properties;
            SetADProperty("c", user.CountryAbbr, ref argproperties19);
            var argproperties20 = userDe.Properties;
            SetADProperty("homePhone", user.HomePhone, ref argproperties20);
            var argproperties21 = userDe.Properties;
            SetMultiValueADProperty("otherHomePhone", user.HomePhoneOther, ref argproperties21);
            var argproperties22 = userDe.Properties;
            SetADProperty("pager", user.Pager, ref argproperties22);
            var argproperties23 = userDe.Properties;
            SetMultiValueADProperty("otherPager", user.PagerOther, ref argproperties23);
            var argproperties24 = userDe.Properties;
            SetADProperty("mobile", user.Mobile, ref argproperties24);
            var argproperties25 = userDe.Properties;
            SetMultiValueADProperty("otherMobile", user.MobileOther, ref argproperties25);
            var argproperties26 = userDe.Properties;
            SetADProperty("facsimileTelephoneNumber", user.Fax, ref argproperties26);
            var argproperties27 = userDe.Properties;
            SetMultiValueADProperty("otherFacsimileTelephoneNumber", user.FaxOther, ref argproperties27);
            var argproperties28 = userDe.Properties;
            SetADProperty("ipPhone", user.IpPhone, ref argproperties28);
            var argproperties29 = userDe.Properties;
            SetMultiValueADProperty("otherIpPhone", user.IpPhoneOther, ref argproperties29);
            var argproperties30 = userDe.Properties;
            SetADProperty("info", user.Note, ref argproperties30);
            var argproperties31 = userDe.Properties;
            SetADProperty("title", user.Title, ref argproperties31);
            var argproperties32 = userDe.Properties;
            SetADProperty("department", user.Department, ref argproperties32);
            var argproperties33 = userDe.Properties;
            SetADProperty("company", user.Company, ref argproperties33);
            if (user.UpdateUserRole == Account.UpdateUserRoleType.DomainAdmin)
            {
                var argproperties34 = userDe.Properties;
                SetADProperty("manager", user.Manager, ref argproperties34);
            }

            userDe.CommitChanges();

            // set Account Options
            SetAccountOptions(user, userDe);

            // We should be careful to only add/delete what we need to. This
            // means not deleting and re-creating everything.

            if (!Information.IsNothing(user.MemberOf))
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
        /// <summary>
        /// Returns true if authentication is successful
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <remarks></remarks>
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
        /// <summary>
        /// Resets user password
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        /// <remarks></remarks>
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
        /// <summary>
        /// Changes user password
        /// </summary>
        /// <param name="userGuid"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        /// <remarks></remarks>
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

        /// <summary>
        /// Searches user based on search criteria parameter
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns>List of Account objects</returns>
        /// <remarks></remarks>
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
                        var argproperties = result.Properties;
                        user.LogonName = GetADPropertyValue("sAMAccountName", ref argproperties);
                        var argproperties1 = result.Properties;
                        user.FirstName = GetADPropertyValue("givenName", ref argproperties1);
                        var argproperties2 = result.Properties;
                        user.LastName = GetADPropertyValue("sn", ref argproperties2);
                        var argproperties3 = result.Properties;
                        user.MiddleInitial = GetADPropertyValue("initials", ref argproperties3);
                        var argproperties4 = result.Properties;
                        user.DisplayName = GetADPropertyValue("displayName", ref argproperties4);
                        var argproperties5 = result.Properties;
                        user.DistinguishedName = GetADPropertyValue("distinguishedName", ref argproperties5);
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


        /// <summary>
        /// Searches user based on search criteria parameter
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns>List of Account objects</returns>
        /// <remarks></remarks>
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
                if (Information.IsNothing(guids))
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
                var propertiesToLoad = new[] { "objectGUID", "sAMAccountName", "userPrincipalName", "givenName", "sn", "initials", "displayName", "description", "physicalDeliveryOfficeName", "telephoneNumber", "otherTelephone", "mail", "wWWHomePage", "url", "streetAddress", "postOfficeBox", "l", "st", "postalCode", "co", "distinguishedName", "homePhone", "otherHomePhone", "pager", "otherPager", "mobile", "otherMobile", "facsimileTelephoneNumber", "otherFacsimileTelephoneNumber", "ipPhone", "otherIpPhone", "info", "title", "department", "company", "manager", "userAccountControl", "msDS-User-Account-Control-Computed", "MemberOf", "whenCreated", "creator", "pwdLastSet" };









































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
                        var argproperties = result.Properties;
                        user.LogonName = GetADPropertyValue("sAMAccountName", ref argproperties);
                        var argproperties1 = result.Properties;
                        user.UPN = GetADPropertyValue("userPrincipalName", ref argproperties1);
                        var argproperties2 = result.Properties;
                        user.FirstName = GetADPropertyValue("givenName", ref argproperties2);
                        var argproperties3 = result.Properties;
                        user.LastName = GetADPropertyValue("sn", ref argproperties3);
                        var argproperties4 = result.Properties;
                        user.MiddleInitial = GetADPropertyValue("initials", ref argproperties4);
                        var argproperties5 = result.Properties;
                        user.Phone = GetADPropertyValue("telephoneNumber", ref argproperties5);
                        var argproperties6 = result.Properties;
                        user.PhoneOther = GetADPropertyMultiValue("otherTelephone", ref argproperties6);
                        var argproperties7 = result.Properties;
                        user.Email = GetADPropertyValue("mail", ref argproperties7);
                        var argproperties8 = result.Properties;
                        user.Description = GetADPropertyValue("description", ref argproperties8);
                        var argproperties9 = result.Properties;
                        user.Office = GetADPropertyValue("physicalDeliveryOfficeName", ref argproperties9);
                        var argproperties10 = result.Properties;
                        user.WebPage = GetADPropertyValue("wWWHomePage", ref argproperties10);
                        var argproperties11 = result.Properties;
                        user.WebPageOther = GetADPropertyMultiValue("url", ref argproperties11);
                        var argproperties12 = result.Properties;
                        user.DisplayName = GetADPropertyValue("displayName", ref argproperties12);
                        var argproperties13 = result.Properties;
                        user.StreetName = GetADPropertyValue("streetAddress", ref argproperties13);
                        var argproperties14 = result.Properties;
                        user.PostOfficeBox = GetADPropertyValue("postOfficeBox", ref argproperties14);
                        var argproperties15 = result.Properties;
                        user.City = GetADPropertyValue("l", ref argproperties15);
                        var argproperties16 = result.Properties;
                        user.Province = GetADPropertyValue("st", ref argproperties16);
                        var argproperties17 = result.Properties;
                        user.PostalCode = GetADPropertyValue("postalCode", ref argproperties17);
                        var argproperties18 = result.Properties;
                        user.Country = GetADPropertyValue("co", ref argproperties18);
                        var argproperties19 = result.Properties;
                        user.HomePhone = GetADPropertyValue("homePhone", ref argproperties19);
                        var argproperties20 = result.Properties;
                        user.HomePhoneOther = GetADPropertyMultiValue("otherHomePhone", ref argproperties20);
                        var argproperties21 = result.Properties;
                        user.Pager = GetADPropertyValue("pager", ref argproperties21);
                        var argproperties22 = result.Properties;
                        user.PagerOther = GetADPropertyMultiValue("otherPager", ref argproperties22);
                        var argproperties23 = result.Properties;
                        user.Mobile = GetADPropertyValue("mobile", ref argproperties23);
                        var argproperties24 = result.Properties;
                        user.MobileOther = GetADPropertyMultiValue("otherMobile", ref argproperties24);
                        var argproperties25 = result.Properties;
                        user.Fax = GetADPropertyValue("facsimileTelephoneNumber", ref argproperties25);
                        var argproperties26 = result.Properties;
                        user.FaxOther = GetADPropertyMultiValue("otherFacsimileTelephoneNumber", ref argproperties26);
                        var argproperties27 = result.Properties;
                        user.IpPhone = GetADPropertyValue("ipPhone", ref argproperties27);
                        var argproperties28 = result.Properties;
                        user.IpPhoneOther = GetADPropertyMultiValue("otherIpPhone", ref argproperties28);
                        var argproperties29 = result.Properties;
                        user.Note = GetADPropertyValue("info", ref argproperties29);
                        var argproperties30 = result.Properties;
                        user.Title = GetADPropertyValue("title", ref argproperties30);
                        var argproperties31 = result.Properties;
                        user.Department = GetADPropertyValue("department", ref argproperties31);
                        var argproperties32 = result.Properties;
                        user.Company = GetADPropertyValue("company", ref argproperties32);
                        var argproperties33 = result.Properties;
                        user.Manager = GetADPropertyValue("manager", ref argproperties33);
                        var argproperties34 = result.Properties;
                        string dn = GetADPropertyValue("distinguishedName", ref argproperties34);
                        dn = dn.Replace(@"\,", " ");
                        var match = Regex.Match(dn, regExp, RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            user.OrganizationalUnit = match.Groups[1].Value;
                        }

                        var argproperties35 = result.Properties;
                        string whenCreated = GetADPropertyValue("whenCreated", ref argproperties35);
                        if (!string.IsNullOrEmpty(whenCreated))
                        {
                            user.CreatedOn = Conversions.ToDate(whenCreated).ToLocalTime();
                        }

                        user.CreatedBy = GetOwner(result.GetDirectoryEntry());
                        ;

                        // account options
                        int exp = Conversions.ToInteger(result.Properties["userAccountControl"][0]);

                        // URS-18: ISAAC-Users account are getting locked in AD but their account in UMA shows unlocked. 
                        // 
                        // Use "msDS-User-Account-Control-Computed" property instead of "userAccountControl" to determine locked accounts
                        // The "msDS-User-Account-Control-Computed" is only available in Windows 2003 or later                
                        int expComputed = -1;
                        if (result.Properties.Contains("msDS-User-Account-Control-Computed"))
                        {
                            expComputed = Conversions.ToInteger(result.Properties["msDS-User-Account-Control-Computed"][0]);
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

        /// <summary>
        /// Gets account from name
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="init"></param>
        /// <param name="lastName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
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
        /// <summary>
        /// Gets Account by username
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
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
        /// <summary>
        /// Gets Account from guid
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        /// <remarks></remarks>
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
        /// <summary>
        /// Gets Account from the user's fully distinguished name
        /// </summary>
        /// <param name="distinguishedName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
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

        /// <summary>
        /// Add user account
        /// </summary>
        /// <param name="user"></param>
        /// <param name="containerFullPath"></param>
        /// <returns></returns>
        /// <remarks></remarks>
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
                var argproperties = userDe.Properties;
                SetADProperty("sAMAccountName", EscapeLogonChrs(user.LogonName), ref argproperties);
                var argproperties1 = userDe.Properties;
                SetADProperty("userPrincipalName", EscapeLogonChrs(user.LogonName), ref argproperties1);
                var argproperties2 = userDe.Properties;
                SetADProperty("givenName", user.FirstName, ref argproperties2);
                var argproperties3 = userDe.Properties;
                SetADProperty("sn", user.LastName, ref argproperties3);
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
        /// <summary>
        /// Update user account
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <remarks></remarks>
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

        /// <summary>
        /// Deletes user account
        /// </summary>
        /// <param name="userGuid"></param>
        /// <returns></returns>
        /// <remarks></remarks>
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
        /// <summary>
        /// Unlocks user account
        /// </summary>
        /// <param name="userGuid"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool UnlockAccount(string userGuid)
        {
            try
            {
                var result = SearchGUID(userGuid);
                if (!(result == default))
                {
                    var deUser = result.GetDirectoryEntry();
                    int exp = Conversions.ToInteger(deUser.Properties["userAccountControl"][0]);
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
        /// <summary>
        /// Enables user account
        /// </summary>
        /// <param name="userGuid"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool EnableAccount(string userGuid)
        {
            try
            {
                var result = SearchGUID(userGuid);
                if (!(result == default))
                {
                    var deUser = result.GetDirectoryEntry();
                    int exp = Conversions.ToInteger(deUser.Properties["userAccountControl"][0]);

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
        /// <summary>
        /// Disables user account
        /// </summary>
        /// <param name="userGuid"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool DisableAccount(string userGuid)
        {
            try
            {
                var result = SearchGUID(userGuid);
                if (!(result == default))
                {
                    var deUser = result.GetDirectoryEntry();
                    int exp = Conversions.ToInteger(deUser.Properties["userAccountControl"][0]);

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

        /// <summary>
        /// Determines if User is a member of the group
        /// </summary>
        /// <param name="userGuid"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
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
                            if (!Information.IsNothing(group) && (group.ToLower() ?? "") == (groupName.ToLower() ?? ""))
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
        /// </summary>
        /// <returns>A list of UPN suffixes.</returns>
        /// <remarks>
        /// https://social.microsoft.com/Forums/en-US/7183c252-3b57-4f32-8cea-c2622bb82d14/how-to-get-list-all-upn-suffixes-in-c-code?forum=crm
        /// https://social.msdn.microsoft.com/forums/vstudio/en-US/d3e4cc17-9391-4d55-8416-4c952a01f762/get-upn-suffix-list-from-active-directory
        /// </remarks>
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


        /// <summary>
        /// Returns all the groups in AD
        /// </summary>
        /// <returns>Array of Group distinguished name</returns>
        /// <remarks></remarks>
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
                    if (Strings.InStr(1, tempName, "CN=") > 0)
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
        /// <summary>
        /// Adds user to group
        /// </summary>
        /// <param name="userGuid">guid of user to add</param>
        /// <param name="groupName">name of the group</param>
        /// <returns></returns>
        /// <remarks></remarks>
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
        /// <summary>
        /// Removes User From Group
        /// </summary>
        /// <param name="userGuid">guid of the user</param>
        /// <param name="groupName">name of the group</param>
        /// <returns></returns>
        /// <remarks></remarks>
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
        /// <summary>
        /// Determines if group already exist in AD
        /// </summary>
        /// <param name="groupName">Name of the Group</param>
        /// <returns></returns>
        /// <remarks></remarks>
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