using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
//using System.Windows.Forms;
using UiAutomation.Logic.Workflows;
//using WFICALib;
using UiAutomation.Logic.RequestsResponses;
using System.Linq;
using System.Collections.Generic;

namespace UiAutomation.Logic.Automation
{
    public static class Landonline
    {
        private static string _citrixClientPath = @"C:\Git\Citrix\launch.ica"; // IMPORTANT this file path cannot contain spaces
        private static string _downloadFileLocation = @"C:\Users\amber.weightman\Downloads\launch.ica";
        private static string _chromePath = @"C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe";
        private static string _landOnlineUrl = @"http://www.linz.govt.nz/land/landonline";

        private static string _user = @"INFOTRACK\amber.weightman";

        private static int _maxTimeExpectedForCitrixFirstLoad = 30000; // Initial load of Citrix can take a bit longer than usual. It wouldn't usually be this long, but we need to allow for it.
        private static int _maxTimeExpectedForCitrixReload = 15000;

        private static int _pageSize = 20; // Max number of searches to run per batch

        public static bool EnsureCitrixRunning()
        {
            var fileExists = File.Exists(_citrixClientPath);
            if (!fileExists && File.Exists($"{_citrixClientPath}.txt"))
            {
                File.Copy($"{_citrixClientPath}.txt", _citrixClientPath);
                fileExists = true;
            }

            // If the .ica file exists already, check whether Landonline is already running
            if (fileExists && CheckCitrixAvailable(0))
            {
                return true;
            }
            
            // If the file exists, attempt to run it
            // (Note that this may be attempting to run an old, expired file, which may not work)
            if (fileExists && RunCitrixClient() && CheckCitrixAvailable(_maxTimeExpectedForCitrixReload))
            {
                return true;
            }

            fileExists = DownloadCitrixClient();
            return fileExists && RunCitrixClient() && CheckCitrixAvailable(_maxTimeExpectedForCitrixFirstLoad);
        }

        private static bool CheckCitrixAvailable(int initialTimeoutDelayAllowed)
        {
            var response = new CheckCitrixAvailabilityWorkflow().Execute(new WorkflowRequest(initialTimeoutDelayAllowed));
            return response.Success;
        }

        private static bool DownloadCitrixClient()
        {
            // We need to re-download the file, so nuke it if it already exists in downloads
            if (File.Exists(_downloadFileLocation))
                File.Delete(_downloadFileLocation);

            // Also, we wouldn't be downloading if this file was ok, so delete it too
            DeleteCitrixFileIfExisting();

            ProcessStartInfo startInfo = new ProcessStartInfo(_chromePath)
            {
                WindowStyle = ProcessWindowStyle.Normal,
                Arguments = _landOnlineUrl
            };

            if (!ExecuteProcess(startInfo))
            {
                throw new ApplicationException("Unable to execute process");
            }

            // Wait for 5 seconds while the webpage loads...
            Console.WriteLine($"Waiting for {_landOnlineUrl} to load...");
            int maxTimeExpectedForUrlToLoad = 10000;
            //Thread.Sleep(5000);

            var response = new ChromeDownloadCitrixWorkflow().Execute(new DownloadCitrixRequest(maxTimeExpectedForUrlToLoad));

            // Wait for the file to be downloaded, then move it to the expected location
            return MoveCitrixClientFile();
        }

        private static bool RunCitrixClient()
        {
            var processStartInfo = new ProcessStartInfo(_citrixClientPath);
            //var processStartInfo2 = new ProcessStartInfo(@"C:\Program Files (x86)\Citrix\ICA Client\wfica32.exe", _citrixClientPath);
            return ExecuteProcess(processStartInfo);

            // This particular process takes some time to start up
            //Console.WriteLine("Waiting for Citrix client to load...");
            //Thread.Sleep(10000); // give it time to open
            //Console.WriteLine("Citrix client has probably loaded by now\n");
            //return true;
        }

        private static bool ExecuteProcess(ProcessStartInfo processStartInfo)
        {
            Process process;
            try
            {
                process = Process.Start(processStartInfo);
                //var process = Process.Start(_citrixClientPath);

                //var err = process?.StandardError.ReadToEnd();
                //Console.WriteLine(err);
            }
            catch (Exception e)
            {
                return false; // TODO should probably not be concealing this
            }

            if (process != null && !process.HasExited)
            {
                //var err = process.StandardError;
                //var str = err.ReadToEnd();
                //Console.WriteLine(str);
                process?.WaitForInputIdle(10000); // this happens almost immediately - the citrix window still isn't ready for us yet
            }
            return true; // Errors are still not being caught successfully
        }

        private static void DeleteCitrixFileIfExisting()
        {
            if (File.Exists(_citrixClientPath))
            {
                File.Delete(_citrixClientPath);
            }
            if (File.Exists($"{_citrixClientPath}.txt"))
            {
                File.Delete($"{_citrixClientPath}.txt");
            }
        }

        private static bool MoveCitrixClientFile()
        {
            var fileChecks = 0;
            while (fileChecks++ < 20 && !File.Exists(_downloadFileLocation))
            {
                Console.WriteLine("Checking for download file... not yet found...");
                Thread.Sleep(2000);
            }

            if (!File.Exists(_downloadFileLocation))
            {
                throw new FileNotFoundException(_downloadFileLocation);
            }

            DeleteCitrixFileIfExisting();

            File.Move(_downloadFileLocation, _citrixClientPath);
            Console.WriteLine("File {0} was found and moved to {1}.", _downloadFileLocation, _citrixClientPath);

            // Create a copy, because the original .ica file will be deleted by Citrix when the session ends
            File.Copy(_citrixClientPath, $"{_citrixClientPath}.txt");

            // Edit the .ica file
            //using (StreamWriter w = File.AppendText(_citrixClientPath))
            //{
            //    w.WriteLine("RemoveICAFile=no"); // the file will be deleted anyway - this doesn't work
            //}

            return true;
        }

        #region run citrix client better

        public static AutoResetEvent onLogonResetEvent = null;

        static void ica_OnLogon()
        {
            Console.WriteLine("OnLogon event was Handled!");
            onLogonResetEvent.Set();
        }

        //private static bool RunCitrixClientBetterBetter()
        //{
        //    if (!File.Exists(_citrixClientPath))
        //    {
        //        return false;
        //    }

        //    var ica = new ICAClientClass
        //    {
        //        // WFClient
        //        CPMAllowed = true,
        //        ClientName = "LANDO-JCUNNING-muvin",
        //        TransportReconnectEnabled = true,
        //        VSLAllowed = true,
        //        VirtualCOMPortEmulation = false,

        //        // Landonline
        //        Address = ";40;STA475DE9C8ABDA;6C0EE48D2E8CADFEE6D1EB93BFB52A57",
        //        AutoLogonAllowed = true,
        //        BrowserProtocol = "HTTPonTCP",
        //        ClientAudio = false,
        //        DesiredColor = ICAColorDepth.Color16Bit,
        //        DesiredHRes = 800,
        //        DesiredVRes = 600,
        //        DoNotUseDefaultCSL = true,
        //        Domain = @"\112B42FF24CDE9B0",
        //        EncryptionLevelSession = "EncRC5-128",
        //        HttpBrowserAddress = "!",
        //        InitialProgram = "#Landonline",
        //        LocHTTPBrowserAddress = "!",
        //        LongCommandLine = "09A7EC444BA0BA0C344391A9C34B4219",
        //        SSLCiphers = "all",
        //        SSLEnable = true,
        //        SSLProxyHost = "secure1.landonline.govt.nz:443",
        //        SessionSharingKey = "4-rc5-128-none-LANDONLINE-jcunningham004-LANDONLINE",
        //        TWIMode = true,
        //        TransportDriver = "TCP/IP",
        //        WinstationDriver = "ICA 3.0",
        //    };

        //    ica.TCPBrowserAddress = "!";
        //    ica.BrowserTimeout = 30000;
        //    ica.ICASOCKSTimeout = 30000;
        //    ica.SessionCacheTimeout = 30000;
        //    ica.SessionExitTimeout = 30000;



        //    onLogonResetEvent = new AutoResetEvent(false);
        //    ica.LoadIcaFile(_citrixClientPath);
        //    ica.ICAFile = _citrixClientPath;

        //    ica.Launch = true;

        //    // Register for the OnLogon event
        //    ica.OnLogon += new _IICAClientEvents_OnLogonEventHandler(ica_OnLogon);

        //    // Launch/Connect to the session
        //    ica.Connect();

        //    //var box = new MessageBox(5);
        //    Console.WriteLine("tst......");
        //    MessageBox.Show("tst");

        //    if (onLogonResetEvent.WaitOne(new TimeSpan(0, 0, 15)))
        //        Console.WriteLine("Session Logged on sucessfully! And OnLogon Event was Fired!");
        //    else
        //        Console.WriteLine("OnLogon event was NOT Fired! Logging off ...");

        //    // Do we have access to the client simulation APIs?
        //    if (ica.Session == null)
        //        Console.WriteLine("ICA.Session object is NULL! :(");


        //    Console.WriteLine("\nPress any key to log off");
        //    Console.Read();
        //    Console.WriteLine("Logging off Session");
        //    ica.Logoff();
        //    ica.Disconnect();
        //    return ica.Connected;
        //}

        //private static bool RunCitrixClientBetter()
        //{
        //    if (!File.Exists(_citrixClientPath))
        //    {
        //        return false;
        //    }

        //    Console.WriteLine("beginning");

        //    var ica = new ICAClientClass();
        //    onLogonResetEvent = new AutoResetEvent(false);

        //    // Launch published Notepad if you comment this line, and uncommented
        //    // the one above you will launch a desktop session
        //    ica.Application = "";
        //    //ica.InitialProgram = "#Notepad";

        //    // Launch a new session
        //    ica.Launch = true;

        //    //ica.ICAFile = _citrixClientPath;
        //    ica.LoadIcaFile(_citrixClientPath);

        //    // Set Server address
        //    //ica.Address = "10.8.X.X";
        //    //ica.Address = "localhost";
        //    //ica.Address = "10.200.21.98";

        //    // No Password property is exposed (for security)
        //    // but can explicitly specify it by using the ICA File "password" property
        //    //ica.Username = "amber.weightman";
        //    //ica.Domain = "INFOTRACK";
        //    //ica.SetProp("Password", "xxxx");

        //    // Let's have a "pretty" session
        //    ica.DesiredColor = ICAColorDepth.Color24Bit;

        //    // Reseach the output mode you want, depending on what your trying
        //    // to attempt to automate. The "Client Simulation APIs" are only available under certain modes
        //    // (i.e. things like sending keys to the session, enumerating windows, etc.)
        //    ica.OutputMode = OutputMode.OutputModeNormal;

        //    // Height and Width
        //    ica.DesiredHRes = 1024;
        //    ica.DesiredVRes = 786;



        //    // Register for the OnLogon event
        //    ica.OnLogon += new _IICAClientEvents_OnLogonEventHandler(ica_OnLogon);

        //    // Launch/Connect to the session
        //    ica.Connect();

        //    //var box = new MessageBox(5);
        //    Console.WriteLine("tst......");
        //    MessageBox.Show("tst");

        //    if (onLogonResetEvent.WaitOne(new TimeSpan(0, 0, 15)))
        //        Console.WriteLine("Session Logged on sucessfully! And OnLogon Event was Fired!");
        //    else
        //        Console.WriteLine("OnLogon event was NOT Fired! Logging off ...");

        //    // Do we have access to the client simulation APIs?
        //    if (ica.Session == null)
        //        Console.WriteLine("ICA.Session object is NULL! :(");


        //    Console.WriteLine("\nPress any key to log off");
        //    Console.Read();
        //    Console.WriteLine("Logging off Session");
        //    ica.Logoff();
        //    ica.Disconnect();
        //    return ica.Connected;

        //    // The below code (the first bit) used to work. Don't know if it's necessary, but in theory it should give
        //    // more control over the citrix client (if nothing else, it enables checking when the connection is established). 
        //    // Don't know why it stopped working.
        //    /*var ica = new ICAClient
        //    {
        //        ICAFile = _citrixClientPath,
        //        Application = "",
        //        Launch = true,
        //        OutputMode = OutputMode.OutputModeNormal,
        //        DesiredHRes = 1024,
        //        DesiredVRes = 786,

        //        //SSLEnable = true,
        //        //SSLNoCACerts = 1 //?
        //        //VSLAllowed = true, //?
        //        //UpdatesAllowed = true, //?
        //        //CDMAllowed = true, //?
        //        //COMAllowed = true, //?
        //        //CPMAllowed = true, //?

        //        Address = "10.8.X.X"
        //    };

        //    ica.Connect();

        //    var connectionChecks = 0;
        //    do
        //    {
        //        if (ica.Connected)
        //            return true;
        //        Thread.Sleep(1000);
        //    } while (connectionChecks++ < 10); // give it 10 seconds - it does take a little time to connect

        //    if (!ica.Connected)
        //    {
        //        throw new ApplicationException("Unable to connect to Citrix client.");
        //    }

        //    Console.WriteLine($"Citrix client connected.");
        //    return ica.Connected;*/

        //    /*
        //    ICAClientClass ica = new ICAClientClass();

        //    // Launch published Notepad if you comment this line, and uncommented
        //    // the one above you will launch a desktop session
        //    // ica.Application = "";
        //    //ica.ICAFile = _citrixClientPath;
        //    //ica.LoadIcaFile(_citrixClientPath);
        //    ica.InitialProgram = "#Notepad";



        //    // Launch a new session
        //    ica.Launch = true;

        //    // Set Server address
        //    ica.Address = "10.8.X.X";

        //    // No Password property is exposed (for security)
        //    // but can explicitly specify it by using the ICA File "password" property
        //    ica.Username = "johncat";
        //    ica.Domain = "xxxx";
        //    ica.SetProp("Password", "xxxx");

        //    // Let's have a "pretty" session
        //    ica.DesiredColor = ICAColorDepth.Color24Bit;

        //    // Reseach the output mode you want, depending on what your trying
        //    // to attempt to automate. The "Client Simulation APIs" are only available under certain modes
        //    // (i.e. things like sending keys to the session, enumerating windows, etc.)
        //    ica.OutputMode = OutputMode.OutputModeNormal;

        //    // Height and Width
        //    ica.DesiredHRes = 1024;
        //    ica.DesiredVRes = 786;

        //    // Launch/Connect to the session
        //    ica.Connect();
        //    ica.LoadIcaFile(_citrixClientPath);

        //    //Console.WriteLine("\nPress any key to log off");
        //    //Console.Read();

        //    // Logoff our session
        //    //Console.WriteLine("Logging off Session");
        //    var connected = ica.Connected;
        //    //ica.Logoff();
        //    return connected;*/

        //    // MIGHT BE almost helpful
        //    // http://discussions.citrix.com/topic/304327-launch-published-application-using-ica-client-object-in-vb-net/
        //}

        #endregion

        public static void ExecuteTitleSearch(TitleSearchRequest[] titleSearchRequests)
        {
            var pagesProcessed = 0;
            var page = titleSearchRequests.Skip(_pageSize * pagesProcessed++).Take(_pageSize);
            while (page.Any())
            {
                var request = new LINZTitleSearchWorkflowRequest(page.ToArray());
                new LINZTitleSearchWorkflow().Execute(request);

                page = titleSearchRequests.Skip(_pageSize * pagesProcessed++).Take(_pageSize);
            }
        }
    }

    public class TitleSearchRequest
    {
        public string TitleReference { get; set; }

        public LINZTitleSearchType Type { get; set; }

        public string OrderId { get; set; }

        public string OutputFilePath { get; set; }

        public bool Success { get; set; }

        public List<SearchResult> SearchResults { get; set; }

        public List<string> Errors { get; set; }

        public List<string> Warnings { get; set; }
    }
}
