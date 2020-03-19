﻿
using System;

namespace GZ.ActiveDirectoryLibrary.OrganizationUnitManagement
{
    public class ADOrganizationalUnit : ADObject
    {
        public ADOrganizationalUnit(string name, string path, Guid ouGuid) : base(ADObjectTypes.OU, name, path, ouGuid)
        {
        }
    }
}