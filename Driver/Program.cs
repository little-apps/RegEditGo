using System;

namespace Driver
{
    static class Program
    {   
        private const string RegKey = "HKEY_LOCAL_MACHINE\\Software\\Microsoft";
        private const string ValueName = RegEditGo.RegEditGo.DefaultValueName;

        static void Main(string[] args)
        {
            if (!Privileges.SetPrivilege("SeDebugPrivilege", true))
                throw new Exception("Unable to enable privilege");

            try
            {
                using (var regEditGo = new RegEditGo.RegEditGo(RegKey, ValueName))
                {
                    regEditGo.GoTo();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to navigate to registry key or value: {ex.Message}");
            }
            
            Privileges.SetPrivilege("SeDebugPrivilege", false);

        }

        
    }
}
