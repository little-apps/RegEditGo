using System;

namespace Driver
{
    static class Program
    {   
        private const string RegKey = "HKEY_LOCAL_MACHINE\\Software\\Microsoft";
        private const string ValueName = "";

        static void Main(string[] args)
        {
            if (!Privileges.SetPrivilege("SeDebugPrivilege", true))
                throw new Exception("Unable to enable privilege");

            using (var regEditGo = new RegEditGo.RegEditGo(RegKey, ValueName))
            {
                regEditGo.GoTo();
            }

            Privileges.SetPrivilege("SeDebugPrivilege", false);

        }

        
    }
}
