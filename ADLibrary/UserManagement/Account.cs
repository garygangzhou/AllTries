
using System;
using System.Collections.Generic;

namespace GZ.ActiveDirectoryLibrary.UserManagement
{  
    [Serializable()]
    public class Account
    {
        public bool IsForcePasswordSet
        {
            get
            {
                return Utility.ConvertFromADDateFormat(0) == PasswordLastChanged;
            }
        }

        public UpdateUserRoleType UpdateUserRole  { get; set; }

        public string DistinguishedName { get; set; } // AD: distinguishedName

        public string Manager { get; set; } // AD: manager

        public string ManagerName { get; set; }

        public string Office { get; set; }  // AD: physicalDeliveryOfficeName

        public string WebPage { get; set; }  // ad:wWWHomePage

        public string WebPageOther { get; set; }  // ad: url

        public string Note{ get; set; } // AD:info

        public string Title{ get; set; }  // AD: title

        public string Department { get; set; } // AD:department

        public string Company{ get; set; }// AD: company

        public string HomePhone { get; set; } // AD:homePhone

        public string HomePhoneOther { get; set; }   // AD: otherHomePhone

        public string Pager { get; set; }  // AD: pager

        public string PagerOther { get; set; } // AD: otherPager

        public string Mobile { get; set; }  // AD:mobile

        public string MobileOther  { get; set; }   // AD: otherMobile

        public string Fax { get; set; } // AD: facsimileTelephoneNumber

        public string FaxOther { get; set; } // AD: otherFacsimileTelephoneNumber

        public string IpPhone { get; set; } // AD:ipPhone

        public string IpPhoneOther { get; set; } // AD: otherIpPhone

        public string StreetName { get; set; } // AD:streetAddress

        public string PostOfficeBox { get; set; }  // AD: postOfficeBox

        public string City { get; set; }  // AD: l

        public string Province { get; set; }  // AD: st

        public string PostalCode{ get; set; }  // AD:postalCode

        public string Country { get; set; } // AD: co

        public string CountryAbbr { get; set; } // AD: c

        public string CreatedBy  { get; set; }

        public string DisplayName { get; set; } // AD: displayName

        public bool AccountOptionAccountLocked { get; set; }

        public string AccountLockStatus
        {
            get
            {
                if (AccountOptionAccountLocked == true)
                {
                    return "Yes";
                }
                else
                {
                    return "No";
                }
            }           
        }
       
        public string AccountEnabled
        {
            get
            {
                if (AccountOptionAccountDisabled == true)
                {
                    return "No";
                }
                else
                {
                    return "Yes";
                }
            }          
        }
        
        public DateTime PasswordExpiryDate { get; set; }

        public DateTime PasswordLastChanged { get; set; }

        public DateTime CreatedOn { get; set; }

        public string LogonName { get; set; }  // AD: sAMAccountName OR userPrincipalName

        public string UPN { get; set; }  // AD: userPrincipalName

        public string UpnRoot
        {
            get
            {
                var segments = UPN.Split('@');
                if (segments.Length > 0)
                {
                    return segments[0];
                }

                return "";
            }
        }

        public string UpnSuffix
        {
            get
            {
                var segments = UPN.Split('@');
                if (segments.Length > 1)
                {
                    return segments[segments.Length - 1];
                }

                return "";
            }
        }
       
        public string LogonPassword { get; set; }  // AD: userPassword

        public string FirstName { get; set; }  // AD: givenName

        public string LastName  { get; set; }  // AD: sn

        public string MiddleInitial { get; set; }  // AD: initials

        public string OrganizationalUnit { get; set; } // AD: OU

        public string Phone { get; set; }  // AD: telephoneNumber

        public string PhoneOther { get; set; } // AD: otherTelephone

        public string Email { get; set; }  // AD: mail

        public string Description { get; set; }  // AD: description

        public bool AccountOptionUserMustChangePasswordNextLogon { get; set; }// AD: pwdLastSet

        public bool AccountOptionUserCannotChangePassword { get; set; }  // AD: userAccountControl (ADS_UF_PASSWD_CANT_CHANGE flag)

        public bool AccountOptionPasswordNeverExpire { get; set; }   // AD: userAccountControl (ADS_UF_DONT_EXPIRE_PASSWD)     

        public bool AccountOptionPasswordReversibleEncryption { get; set; } // AD: userAccountControl (ADS_UF_ENCRYPTED_TEXT_PASSWORD_ALLOWED)

        public bool AccountOptionAccountDisabled { get; set; }
        
        public string[] MemberOf { get; set; } // AD: memberOf

        public string Guid { get; set; } // AD: objectGUID

        public enum UpdateUserRoleType
        {
            SiteAdmin = 1,
            DomainAdmin = 2
        }
    }

    public class AccountComparer : IEqualityComparer<Account>
    {        

        public bool Equals(Account x, Account y)
        {
            return (x.Guid ?? "") == (y.Guid ?? "");
        }

        public int GetHashCode(Account account)
        {
            return account.Guid.GetHashCode();
        }
    }
}