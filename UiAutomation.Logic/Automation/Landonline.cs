using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;
//using System.Windows.Forms;
using UiAutomation.Logic.Workflows;
//using WFICALib;
using System.Linq;
using UiAutomation.Contract.Models;

namespace UiAutomation.Logic.Automation
{
    public class LandonlineAutomator : CitrixAutomator, ILandonlineAutomator
    {
        public static string CitrixOutputDirectoryTemp = @"C:\LandonlineOutputDirectory\";

        private static string _citrixClientPath = ConfigurationManager.AppSettings.Get("CitrixClientPath"); // IMPORTANT this file path cannot contain spaces
        private static string _downloadFileLocation = ConfigurationManager.AppSettings.Get("DownloadFileLocation");
        private static string _chromePath = ConfigurationManager.AppSettings.Get("ChromePath");
        private static string _landOnlineUrl = @"http://www.linz.govt.nz/land/landonline";
        
        private static int _maxTimeExpectedForCitrixFirstLoad = 30000; // Initial load of Citrix can take a bit longer than usual. It wouldn't usually be this long, but we need to allow for it.
        private static int _maxTimeExpectedForCitrixReload = 15000;
        
        /// <summary>
        /// Max number of searches to run per batch
        /// </summary>
        private static int _pageSize
        {
            get
            {
                var size = ConfigurationManager.AppSettings.Get("PageSize");
                return !string.IsNullOrEmpty(size) ? Convert.ToInt32(size) : 1;
            }
        }

        /// <summary>
        /// Try everything possible to make sure Citrix is running
        /// </summary>
        public override bool EnsureCitrixRunning(string autoOrderBulkId, string screenshotDirectoryPath)
        {
            var test = _pageSize;

            bool isRunning = false; ;
            Exception firstAttemptException = null;
            try
            {
                isRunning = EnsureCitrixRunningInner(autoOrderBulkId, screenshotDirectoryPath, attempt: 0);
            }
            catch (Exception e)
            {
                // If there is already an active robot thread, don't kill it
                if (e.Message == "Can only have one active robot thread.")
                {
                    throw (e);
                }

                firstAttemptException = e;

                // It failed, but give it another chance. Kill all related processes then try again from scratch.
                //Threading.KillRobotThread();
                KillCitrixProcesses(); // all of them, not just the robots
                EndBrowserProcesses(ProcessHelper.CloseProcessType.Kill);
                //GC.Collect();
                try
                {
                    isRunning = EnsureCitrixRunningInner(autoOrderBulkId, screenshotDirectoryPath, attempt: 1);
                }
                catch (Exception e1)
                {
                    // I think this is likely to be an out of memory issue, causing the robot to not be able to start
                    //var availableMemory = new ComputerInfo().AvailablePhysicalMemory;

                    //var mem = GC.GetTotalMemory();

                    // TODO should we be throwing the first exception or the second? Could add one as an innerException to
                    // the other, but would that make sense (and that one might already have an innerException, anyway)?
                    throw e1;
                }
            }

            // If the second attempt failed without throwing an exception, throw the exception from the first attempt
            // (Doesn't even make sense that this would happen, but handle it just in case)
            if (!isRunning && firstAttemptException != null)
            {
                throw firstAttemptException;
            }

            return isRunning;
        }
        
        private bool EnsureCitrixRunningInner(string autoOrderBulkId, string screenshotDirectoryPath, int attempt = 0)
        {
            // Check if the .ica file exists. This file is deleted every time Citrix is launched, but a copy is created as
            // a .txt file to help avoid the need to re-download the file. If the .ica file doesn't exist but the .txt file
            // does, recreate the .ica file.
            var fileExists = File.Exists(_citrixClientPath);
            if (!fileExists && File.Exists($"{_citrixClientPath}.txt"))
            {
                File.Copy($"{_citrixClientPath}.txt", _citrixClientPath);
                fileExists = true;
            }

            // If the .ica file exists already, check whether Landonline is already running
            if (fileExists && CheckCitrixAvailable(screenshotDirectoryPath, 0, autoOrderBulkId))
            {
                return true;
            }

            // If the file exists, but Citrix is not already running, attempt to run it
            // (Note that this may be attempting to run an old, expired file, which may not work)
            if (fileExists && RunCitrixClient() && CheckCitrixAvailable(screenshotDirectoryPath, _maxTimeExpectedForCitrixReload, autoOrderBulkId))
            {
                return true;
            }

            // The file didn't already exist, so attempt to download it. If download is successful, attempt to run it and confirm
            // that it is available
            fileExists = DownloadCitrixClient(screenshotDirectoryPath, autoOrderBulkId);
            return fileExists && RunCitrixClient() && CheckCitrixAvailable(screenshotDirectoryPath, _maxTimeExpectedForCitrixFirstLoad, autoOrderBulkId);
        }

        private static bool CheckCitrixAvailable(string screenshotDirectoryPath, int initialTimeoutDelayAllowed, string autoOrderBulkId)
        {
            var response = new CheckCitrixAvailabilityWorkflow().Execute(new CheckCitrixAvailabilityWorkflowRequest(screenshotDirectoryPath, autoOrderBulkId, initialTimeoutDelayAllowed));
            return response.Success;
        }

        private void EndBrowserProcesses(ProcessHelper.CloseProcessType closeProcessType)
        {
            ProcessHelper.EndProcessesByName(new [] { "chrome" }, closeProcessType);
        }

        private bool DownloadCitrixClient(string screenshotDirectoryPath, string autoOrderBulkId)
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
                throw new ApplicationException($"Unable to execute process for {startInfo.FileName}");
            }

            // Wait for 5 seconds while the webpage loads...
            Console.WriteLine($"Waiting for {_landOnlineUrl} to load...");
            int maxTimeExpectedForUrlToLoad = 10000;

            var response = new ChromeDownloadCitrixWorkflow().Execute(new DownloadCitrixRequest(screenshotDirectoryPath, autoOrderBulkId, maxTimeExpectedForUrlToLoad));

            // Wait for the file to be downloaded, then move it to the expected location
            var success = MoveCitrixClientFile();

            EndBrowserProcesses(ProcessHelper.CloseProcessType.CloseMainWindow);

            return success;
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
                throw e;
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

        public void ExecuteTitleSearch(LINZTitleSearchBatch externalRequest)
        {
            // _pageSize = 4; // TODO this paging is not tested properly yet
            var pagesProcessed = 0;
            var page = externalRequest.TitleSearchRequests.Skip(_pageSize * pagesProcessed++).Take(_pageSize);
            while (page.Any())
            {
                var request = new LINZTitleSearchWorkflowRequest(page.ToArray(), externalRequest.ScreenshotDirectoryPath, externalRequest.AutoOrderBulkId);
                new LINZTitleSearchWorkflow().Execute(request);

                page = externalRequest.TitleSearchRequests.Skip(_pageSize * pagesProcessed++).Take(_pageSize);
            }
        }
        
    }
    
}
