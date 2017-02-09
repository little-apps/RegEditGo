# RegEditGo
Navigates to a Windows Registry key and (possibly) a value name in regedit.exe. Instead of just setting ``LastKey`` in ``HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Applets\Regedit``, this utilizes Windows Message to navigate like an actual user would. This project is a fork of [JumpTo RegEdit](https://www.codeproject.com/articles/20283/jumpto-regedit) created by Reto Ravasio in August 2007.

## License ##
This project is licensed under the [GNU Lesser General Public License v3](http://www.gnu.org/copyleft/lesser.html).

## Notes ##
 * Along with "regedit.exe", this program also requires administrator privileges in order to run.
 * RegEditGo attempts to get all possible access rights to the "regedit.exe" process object (using ``PROCESS_ALL_ACCESS``) . Although not required, it is recommend the "SeDebugPrivilege" (``SE_DEBUG_NAME``) is added to your program before RegEditGo is used.

### Show Your Support ###
Little Apps relies on people like you to keep our software running. If you would like to show your support for Little Software Stats, then you can [make a donation](https://www.little-apps.com/?donate) using PayPal, Payza or Bitcoins. Please note that any amount helps (even just $1). 