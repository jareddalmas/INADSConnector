using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.IO;
using System.Text.RegularExpressions;

namespace GetPPPIP
{
    class Program
    {
        public static string psCMUserName;
        public static string psCMPassword;
        public static string psInadsUsername;
        public static string psInadsPassword;
        public static string psInadsPhoneNumber;

        //
        //  run using the following command:  appname InadsUsername InadsPassword InadsPhoneNumber CMUsername CMPassword
        //

        static void Main(string[] args)
        {
            psInadsUsername = args[0];      //Command Argument INADS Username
            psInadsPassword = args[1];      //Command Argument INADS Remote Password
            psInadsPhoneNumber = args[2];   //Command Argument INADS Phone Number
            psCMUserName = args[3];         //Command Argument INADS Communication Manager Username
            psCMPassword = args[4];         //Command Argument INADS Communication Manager Password
            string sServerIp = "";
            //string sa_test = GetPPPIP.Properties.Resources.avaya;
            
            sServerIp = fGetServerIP();
            if (sServerIp != "")
            {
                sStartProgram(sServerIp);
                return;
            }

            fChangeFile();
            fDialConnection(args);


            
            while (sServerIp == "")
            {
                System.Threading.Thread.Sleep(5000);
                sServerIp = fGetServerIP();
            }
        
            sStartProgram(sServerIp);
            
            
            
        }

        //
        //  This function runs the tutty.exe executable connecting to the sServerIp with the psCMUserName and psCMPassword.
        //
        private static void sStartProgram(string sServerIp)
        {
            Process tutty = new Process();
            tutty.StartInfo.FileName = "tutty.exe";
            tutty.StartInfo.Arguments = "-load \"tutty defaults\" " + sServerIp + " -l " + psCMUserName + " -pw " + '"' + psCMPassword + "\" -D 4000";
            try
            {
                tutty.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
           
        }
        //
        //  This function gets the IP address of the connection named "Generic"
        //
        private static string fGetServerIP()
        {
            string sServerIP = "";
            bool bConnectionFound = false;
            string[] saAddress;

            

            var vNics = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var nic in vNics)
            {
                Console.WriteLine(nic.Name.ToString());
            
                if (nic.Name.ToString().Contains("Generic"))
                {
                    var ipProps = nic.GetIPProperties();
                    foreach (UnicastIPAddressInformation info in ipProps.UnicastAddresses)
                    {
                        saAddress = info.Address.ToString().Split('.');
                        int iServerLastOctet = (int.Parse(saAddress[3]) - 1);
                        sServerIP = saAddress[0] + '.' + saAddress[1] + '.' + saAddress[2] + '.' + iServerLastOctet.ToString();
                        Console.WriteLine(sServerIP);
                        bConnectionFound = true;
                        break;
                    }
                }
                
                if (bConnectionFound == true)
                {
                    break;
                }
                
            }
            if (bConnectionFound == true)
            {
                return sServerIP;
            }
            else
            {
                return "";
            }
        }

        //
        //  This function runs rasphone with the arguments -dx sConnectionName.  It will bring up a dialog box
        //  which the user will click connect.
        //
        private static void fDialConnection(string[] args)
        {
            
            string sConnectionName = "Generic";
            Process process_rasphone = new Process();
            process_rasphone.StartInfo.FileName = "rasphone.exe";
            process_rasphone.StartInfo.Arguments = "-dx \"" + sConnectionName + '"';
            process_rasphone.Start();
        }

        private static void fChangeFile()
        {
            string sConnectionPassword = psInadsPassword;
            string sPhoneNumber = psInadsPhoneNumber;
            fChangeScpFile(psInadsUsername,sConnectionPassword);
            fChangePbkFile(sConnectionPassword, sPhoneNumber);
            
        }

        //
        //  This code makes changes to the rasphone.pbk file, the default phonebook for the system.
        //  it looks for a connection named [Generic] in that connection it changes the phone number
        //  to  PhoneNumber=9,1(InadsNumber) e.g.(PhoneNumber=9,1315-555-1212) it also adds 
        //  Name=C:\Windows\system32\ras\avaya.scp and Script=1  to the [switch] section which
        //  sets the path of the login script and enables using the login script.
        //
        //  
        private static void fChangePbkFile(string sConnectionPassword, string sPhoneNumber)
        {
            int iSwitchLines = 0;
            bool bGroupFound = false;
            bool bSwitchSecionFound = false;
            string sAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string sUserName = System.Security.Principal.WindowsIdentity.GetCurrent().User.ToString();
            StringBuilder newFile = new StringBuilder();
            string sTemp = "";
            string sFileName = sAppDataDir + @"\Microsoft\Network\Connections\Pbk\rasphone.pbk";
            string sNewFileName = sAppDataDir + @"\Microsoft\Network\Connections\Pbk\rasphone.pbk";
            string[] saFile = File.ReadAllLines(sFileName);
            int i = 0;
            //
            //  This loop goes through the rasphone.pbk file looking to see if there's a connection named Generic
            //  if it does find one it changes the phone number, adds the connection script, and tells it to run 
            //  the connection script.
            //
            while (i < saFile.Count())
            {
                string sLine = saFile[i];
                if (sLine.Equals("[Generic]") || bGroupFound == true)
                {
                    if (sLine.Contains("[Generic]"))
                    {
                        bGroupFound = true;
                        newFile.Append(sLine + "\r\n");
                    }
                    else if (sLine.Contains("PhoneNumber="))
                    {
                        //                        sTemp = "PhoneNumber=9,1" + sPhoneNumber;
                        //sTemp = sLine.Replace("9,1", "9,1" + sPhoneNumber);
                        newFile.Append("PhoneNumber=9,1" + sPhoneNumber + "\r\n");

                    }
                    else if (sLine.Contains("DEVICE=switch"))
                    {
                        bSwitchSecionFound = true;
                        newFile.Append(sLine + "\r\n");
                        if (saFile[i + 1].Contains(@"Name=C:\Windows\system32\ras\avaya.scp"))
                        {
                            // do nothing
                            newFile.Append(@"Name=C:\Windows\system32\ras\avaya.scp" + "\r\n");
                            i++;
                        }
                        else
                        {
                            newFile.Append(@"Name=C:\Windows\system32\ras\avaya.scp" + "\r\n");
                        }
                        if (saFile[i + 1].Contains(@"Script=1"))
                        {
                            // do nothing
                            newFile.Append("Script=1" + "\r\n");
                            i++;
                        }
                        else
                        {
                            newFile.Append("Script=1" + "\r\n");
                        }

                        bGroupFound = false;
                        //newFile.Append(@"Name=C:\Windows\system32\ras\avaya.scp" + "\r\n");
                    }
                    else
                    {
                        newFile.Append(sLine + "\r\n");
                    }
                }
                else
                {
                    newFile.Append(sLine + "\r\n");
                }

                i++;
            }
            //
            //  If the [Generic] connection doesn't exist, it creates a new connection named [Generic] based on the 
            //  generic_connection text file in Properties.
            //

            if (bGroupFound == false)
            {
                int j = 0;
                string[] saNewConnection = Regex.Split(GetPPPIP.Properties.Resources.generic_connection,"\r\n");
                List<string> ls_file = new List<string>();
                //ls_file.AddRange(Regex.Split("\r\n", GetPPPIP.Properties.Resources.generic_connection));
                while (j < saNewConnection.Count())
                {
                    if (saNewConnection[j].Contains("PhoneNumber="))
                    {
                        //                        sTemp = "PhoneNumber=9,1" + sPhoneNumber;
                        //sTemp = sLine.Replace("9,1", "9,1" + sPhoneNumber);
                        newFile.Append("PhoneNumber=9,1" + sPhoneNumber + "\r\n");
                    }
                    else
                    {
                        newFile.Append(saNewConnection[j] + "\r\n");
                    }
                    j++;
                }
            }
            File.WriteAllText(sNewFileName, newFile.ToString()); 
        }


        //
        //  This code makes changes to the rasphone.pbk file, the default phonebook for the system.
        //  it looks for a connection named [Generic] in that connection it changes the phone number
        //  to  PhoneNumber=9,1(InadsNumber) e.g.(PhoneNumber=9,1315-555-1212) it also adds 
        //  Name=C:\Windows\system32\ras\avaya.scp and Script=1  to the [switch] section which
        //  sets the path of the login script and enables using the login script.
        //
        //  Old Code
        /*
        private static void fChangePbkFile(string sConnectionPassword, string sPhoneNumber)
        {
            int iSwitchLines = 0;
            bool bGroupFound = false;
            bool bSwitchSecionFound = false;
            string sAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string sUserName = System.Security.Principal.WindowsIdentity.GetCurrent().User.ToString();
            StringBuilder newFile = new StringBuilder();
            string sTemp = "";
            string sFileName = sAppDataDir + @"\Microsoft\Network\Connections\Pbk\rasphone.pbk";
            string sNewFileName = sAppDataDir + @"\Microsoft\Network\Connections\Pbk\rasphone.pbk";
            string[] saFile = File.ReadAllLines(sFileName);
            int i = 0;
            while (i < saFile.Count())
            {
                string sLine = saFile[i];
                if (sLine.Equals("[Generic]") || bGroupFound == true)
                {
                    if (sLine.Contains("[Generic]"))
                    {
                        bGroupFound = true;
                        newFile.Append(sLine + "\r\n");
                    }
                    else if (sLine.Contains("PhoneNumber="))
                    {
                        //                        sTemp = "PhoneNumber=9,1" + sPhoneNumber;
                        //sTemp = sLine.Replace("9,1", "9,1" + sPhoneNumber);
                        newFile.Append("PhoneNumber=9,1" + sPhoneNumber + "\r\n");

                    }
                    else if (sLine.Contains("DEVICE=switch"))
                    {
                        bSwitchSecionFound = true;
                        newFile.Append(sLine + "\r\n");
                        if (saFile[i + 1].Contains(@"Name=C:\Windows\system32\ras\avaya.scp"))
                        {
                            // do nothing
                            newFile.Append(@"Name=C:\Windows\system32\ras\avaya.scp" + "\r\n");
                            i++;
                        }
                        else
                        {
                            newFile.Append(@"Name=C:\Windows\system32\ras\avaya.scp" + "\r\n");
                        }
                        if (saFile[i + 1].Contains(@"Script=1"))
                        {
                            // do nothing
                            newFile.Append("Script=1" + "\r\n");
                            i++;
                        }
                        else
                        {
                            newFile.Append("Script=1" + "\r\n");
                        }

                        bGroupFound = false;
                        //newFile.Append(@"Name=C:\Windows\system32\ras\avaya.scp" + "\r\n");
                    }
                    else
                    {
                        newFile.Append(sLine + "\r\n");
                    }
                }
                else
                {
                    newFile.Append(sLine + "\r\n");
                }

                i++;
            }
            File.WriteAllText(sNewFileName, newFile.ToString());
        }
         * */
        

       
        
        /*      Old Code
        private static void fChangeScpFile(string psInadsUsername, string sConnectionPassword)
        {
            bool bGroupFound = false;
            string sAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string sUserName = System.Security.Principal.WindowsIdentity.GetCurrent().User.ToString();
            StringBuilder newFile = new StringBuilder();
            string sTemp = "";
           
            string sFileName = @"C:\Windows\SysWOW64\ras\avaya.scp";
            string sNewFileName = @"C:\Windows\SysWOW64\ras\avaya.scp";

            //string[] saFile = StreamReader
            string[] saFile = File.ReadAllLines( sFileName);
            foreach (string sLine in saFile)
            {
                
                if (sLine.Contains("string sUserID"))
                {
                    sTemp = "string sUserID = " + '"' + psInadsUsername + '"';
                    //sTemp = sLine.Replace("9,1", "9,1" + sPhoneNumber);
                    newFile.Append(sTemp + "\r\n");
                    //bGroupFound = false;
                }
                else if (sLine.Contains("string sPassword"))
                {
                    sTemp = "string sPassword = " + '"' + psInadsPassword + '"' ;
                    //sTemp = sLine.Replace("9,1", "9,1" + sPhoneNumber);
                    newFile.Append(sTemp + "\r\n");
                    bGroupFound = false;
                }
                else
                {
                    newFile.Append(sLine + "\r\n");
                }
            
                

            }
            File.WriteAllText(sNewFileName, newFile.ToString());
        }
         * */


        /// <summary>
        /// 
        ///  This function modifies the avaya scp file located at "C:\Windows\System32\ras\avaya.scp"
        ///  changing the username and password for the INADS login.
        ///

        /// </summary>
        /// <param name="psInadsUsername"></param>
        /// <param name="sConnectionPassword"></param>
        /// 


        private static void fChangeScpFile(string psInadsUsername, string sConnectionPassword)
        {
            bool bGroupFound = false;
            string sAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string sUserName = System.Security.Principal.WindowsIdentity.GetCurrent().User.ToString();
            StringBuilder newFile = new StringBuilder();
            string sTemp = "";
            string sNewFileName = "";
            
            //  This populates the string array with lines from the avaya.scp resource file.
            string[] sa_AvayaScpText = Regex.Split(GetPPPIP.Properties.Resources.avaya, "\r\n");
            
            
            if (Environment.Is64BitOperatingSystem)
            {
                sNewFileName = @"C:\Windows\SysWOW64\ras\avaya.scp";
            }
            else
            {
                sNewFileName = @"C:\Windows\System32\ras\avaya.scp";
            }

            foreach (string sLine in sa_AvayaScpText)
            {

                if (sLine.Contains("string sUserID"))
                {
                    sTemp = "string sUserID = " + '"' + psInadsUsername + '"';
                    newFile.Append(sTemp + "\r\n");
                }
                else if (sLine.Contains("string sPassword"))
                {
                    sTemp = "string sPassword = " + '"' + psInadsPassword + '"';
                    newFile.Append(sTemp + "\r\n");
                    bGroupFound = false;
                }
                else
                {
                    newFile.Append(sLine + "\r\n");
                }



            }
            File.WriteAllText(sNewFileName, newFile.ToString());
            
        }
    }
}
