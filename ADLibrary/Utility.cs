
using System;
using System.DirectoryServices;
using System.Text;
using System.Text.RegularExpressions;

namespace GZ.ActiveDirectoryLibrary
{
    internal class Utility
    {
        public static string ConvertMultiValuedToString(ResultPropertyValueCollection pValues)
        {
            var r = new StringBuilder();
            foreach (string p in pValues)
            {
                if (string.IsNullOrEmpty(r.ToString()))
                {
                    r.Append(p);
                }
                else
                {
                    r.Append(Constants.MULTI_VALUED_SEPARATOR);
                    r.Append(p);
                }
            }

            return r.ToString();
        }
 
        public static string[] GetGroupNameFromDN(string[] dn)
        {
            const string _regExpression = @"CN=['\\\#\w\d\s\-\&amp;@\(\)]+,";
            var _returnValue = new string[dn.Length];

            // dn = "CN=MyGroup,CN=Users,DC=cco,DC=com"
            Regex re;
            re = new Regex(_regExpression, RegexOptions.IgnoreCase & RegexOptions.Singleline);
            for (int i = 0, loopTo = dn.Length - 1; i <= loopTo; i++)
            {
                // get the first match
                var m = re.Match(dn[i]);
                if (m.Success == true)
                {
                    _returnValue[i] = m.Value.Substring(3, m.Length - 4);
                }
            }

            return _returnValue;
        }

        public static string[] ConvertMultiValuedToStringArray(ResultPropertyValueCollection pValues)
        {
            if (!(pValues == default))
            {
                var r = new string[pValues.Count];
                for (int i = 0, loopTo = pValues.Count - 1; i <= loopTo; i++)
                    r[i] = Convert.ToString(pValues[i]);
                return r;
            }

            return null;
        }

        public static string[] ConvertMultiValuedToStringArray(PropertyValueCollection pValues)
        {
            if (!(pValues == default))
            {
                var r = new string[pValues.Count];
                for (int i = 0, loopTo = pValues.Count - 1; i <= loopTo; i++)
                    r[i] = Convert.ToString(pValues[i]);
                return r;
            }

            return null;
        }

        public static bool IsValidGUID(string guid)
        {
            try
            {
                var g = new Guid(guid);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public static string GuidToOctetString(string objectGuid)
        {
            var guid = new Guid(objectGuid);
            var byteGuid = guid.ToByteArray();
            string queryGuid = "";
            foreach (byte b in byteGuid)
                queryGuid = queryGuid + @"\" + b.ToString("x2");
            return queryGuid;
        }

        public static string GuidOctetStringToADFormat(string objectGuid)
        {
            var queryGuid = new StringBuilder();
            for (int i = 0, loopTo = objectGuid.Length - 1; i <= loopTo; i += 2)
                queryGuid.Append(@"\").Append(objectGuid.Substring(i, 2));
            return queryGuid.ToString();
        }

        public static DateTime ConvertFromADDateFormat(long dt)
        {
            if (dt < 0)
            {
                return DateTime.MinValue;
            }

            return DateTime.FromFileTime(dt);
            // Return DateTime.FromFileTimeUtc(dt)
        }

        public static string ConvertSidToOctetString(string sid)
        {
            int iterator;
            StringBuilder builder;
            string slash = @"\";
            string formatCode = "x2";
            var data = sid.ToCharArray();
            builder = new StringBuilder(data.Length * 2);
            var loopTo = data.Length - 1;
            for (iterator = 0; iterator <= loopTo; iterator++)
                // builder.Append(slash)
                builder.AppendFormat("{0:x2}", data[iterator]);
            return builder.ToString();
        }
    }
}
