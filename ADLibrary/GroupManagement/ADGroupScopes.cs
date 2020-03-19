
namespace GZ.ActiveDirectoryLibrary.GroupManagement
{
    public enum ADGroupScopes
    {
        None = 0,
        DomainLocal = 4, // ActiveDs.ADS_GROUP_TYPE_ENUM.ADS_GROUP_TYPE_DOMAIN_LOCAL_GROUP
        Local = 4, // ActiveDs.ADS_GROUP_TYPE_ENUM.ADS_GROUP_TYPE_LOCAL_GROUP
        Global = 2, // ActiveDs.ADS_GROUP_TYPE_ENUM.ADS_GROUP_TYPE_GLOBAL_GROUP
        Universal = 8 // ActiveDs.ADS_GROUP_TYPE_ENUM.ADS_GROUP_TYPE_UNIVERSAL_GROUP
    }
}