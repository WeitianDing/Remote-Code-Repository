///////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs - GUI for Project3HelpWPF                      //
// ver 2.0                                                           //
// Jim Fawcett, CSE687 - Object Oriented Design, Spring 2018         //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package provides a WPF-based GUI for Project3HelpWPF demo.  It's 
 * responsibilities are to:
 * - Provide a display of directory contents of a remote ServerPrototype.
 * - It provides a subdirectory list and a filelist for the selected directory.
 * - You can navigate into subdirectories by double-clicking on subdirectory
 *   or the parent directory, indicated by the name "..".
 *   
 * Required Files:
 * ---------------
 * Mainwindow.xaml, MainWindow.xaml.cs
 * Translater.dll
 * 
 * Maintenance History:
 * --------------------
 * ver 2.0 : 22 Apr 2018
 * - added tabbed display
 * - moved remote file view to RemoteNavControl
 * - migrated some methods from MainWindow to RemoteNavControl
 * - added local file view
 * - added NoSqlDb with very small demo as server starts up
 * ver 1.0 : 30 Mar 2018
 * - first release
 * - Several early prototypes were discussed in class. Those are all superceded
 *   by this package.
 */

// Translater has to be statically linked with CommLibWrapper
// - loader can't find Translater.dll dependent CommLibWrapper.dll
// - that can be fixed with a load failure event handler
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.IO;
using MsgPassingCommunication;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Console.Title = "Project4Demo GUI Console";
        }

        private Stack<string> pathStack_ = new Stack<string>();
        internal Translater translater;
        internal CsEndPoint endPoint_;
        internal CsEndPoint serverEndPoint_;
        private Thread rcvThrd = null;
        private Dictionary<string, Action<CsMessage>> dispatcher_
          = new Dictionary<string, Action<CsMessage>>();
        internal string saveFilesPath;
        internal string sendFilesPath;
        //the path of folder in repo which named the same as package description
        internal string descripFolderPath = "";
        string cateQ_ = "";
        string fileNameQ_ = "";
        string dependencyQ_ = "";
        string versionQ_ = "";

        //----< process incoming messages on child thread >----------------

        private void processMessages()
        {
            ThreadStart thrdProc = () =>
            {
                while (true)
                {
                    CsMessage msg = translater.getMessage();
                    try
                    {
                        string msgId = msg.value("command");
                        Console.Write("\n  client getting message \"{0}\"", msgId);
                        if (dispatcher_.ContainsKey(msgId))
                            dispatcher_[msgId].Invoke(msg);
                    }
                    catch (Exception ex)
                    {
                        Console.Write("\n  {0}", ex.Message);
                        msg.show();
                    }
                }
            };
            rcvThrd = new Thread(thrdProc);
            rcvThrd.IsBackground = true;
            rcvThrd.Start();
        }
        //----< add client processing for message with key >---------------

        private void addClientProc(string key, Action<CsMessage> clientProc)
        {
            dispatcher_[key] = clientProc;
        }
        ////----< load getDirs processing into dispatcher dictionary >-------

        private void DispatcherLoadGetDirs()
        {
            Action<CsMessage> getDirs = (CsMessage rcvMsg) =>
            {
                Action clrDirs = () =>
          {
            //NavLocal.clearDirs();
            NavRemote.clearDirs();
              };
                Dispatcher.Invoke(clrDirs, new Object[] { });
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("dir"))
                    {
                        Action<string> doDir = (string dir) =>
                  {
                      NavRemote.addDir(dir);
                  };
                        Dispatcher.Invoke(doDir, new Object[] { enumer.Current.Value });
                    }
                }
                Action insertUp = () =>
          {
                  NavRemote.insertParent();
              };
                Dispatcher.Invoke(insertUp, new Object[] { });
            };
            addClientProc("getDirs", getDirs);
        }
        //----< load getFiles processing into dispatcher dictionary >------

        private void DispatcherLoadGetFiles()
        {
            Action<CsMessage> getFiles = (CsMessage rcvMsg) =>
            {
                Action clrFiles = () =>
          {
                  NavRemote.clearFiles();
              };
                Dispatcher.Invoke(clrFiles, new Object[] { });
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("file"))
                    {
                        Action<string> doFile = (string file) =>
                  {
                      NavRemote.addFile(file);
                  };
                        Dispatcher.Invoke(doFile, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("getFiles", getFiles);
        }
        //----< load getFiles processing into dispatcher dictionary >------

        private void DispatcherLoadSendFile()
        {
            Action<CsMessage> sendFile = (CsMessage rcvMsg) =>
            {
                Console.Write("\n  processing incoming file");
                string fileName = "";
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("sendingFile"))
                    {
                        fileName = enumer.Current.Value;
                        break;
                    }
                }
                if (fileName.Length > 0)
                {
                    Action<string> act = (string fileNm) => { showFile(fileNm); };
                    Dispatcher.Invoke(act, new object[] { fileName });
                }
            };
            addClientProc("sendFile", sendFile);
        }

        private void DispatcherReply_descrip()
        {
            Action<CsMessage> Reply_descrip = (CsMessage rcvMsg) =>
            {
                Console.Write("\n  Received Msg is : ");
                rcvMsg.show();
                string tempNotification;
                //if there is package description
                string path = rcvMsg.value("descripFolderPath");
                descripFolderPath = path;
                if (rcvMsg.value("createFolder") == "true")
                {
                    //string path = System.IO.Path.Combine(RepoStoragePath, rcvMsg.value("descripFolder"));

                    //descripFolderPath = path;
                    Console.WriteLine("  Successfully creat subfolder named as package description in RepoStorage, path : " + path);
                    tempNotification = " create subfolder in repo successfully";
                }
                //if package description is null
                else if (rcvMsg.value("createFolder") == "root")
                {
                    //descripFolderPath = RepoStoragePath;
                    Console.WriteLine("  No description for package, going to check in files to RepoStorage root folder");
                    tempNotification = " No description for package, going to check in files to RepoStorage root folder";
                }
                //if create folder fails
                else
                {
                    Console.WriteLine("  Fail to creat subfolder named as package description in RepoStorage");
                    tempNotification = " fail to create subfolder in repo";
                }
                //use STA thread
                Action createFolderNotification = () =>
                {
                    NavLocal.localNotification.Items.Add(tempNotification);
                };
                Dispatcher.Invoke(createFolderNotification, new Object[] { });
                //test check in next
                //if (rcvMsg.contains("demo"))
                //{
                //    if (rcvMsg.value("demo") == "next_testCheckin")
                //    {
                //        testCheckin();
                //    }
                //}
            };
            addClientProc("reply_descrip", Reply_descrip);
        }

        private void DispatcherReply_preChecking()
        {
            Action<CsMessage> Reply_preChecking = (CsMessage rcvMsg) =>
            {
                Console.Write("\n  Received Reply_preChecking Msg is : ");
                rcvMsg.show();
                Console.WriteLine("  Repo received " + rcvMsg.value("fileName"));
                Action createFolderNotification = () =>
                {
                    NavLocal.localNotification.Items.Add("Repo received " + rcvMsg.value("fileName"));
                };
                Dispatcher.Invoke(createFolderNotification, new Object[] { });
            };

            addClientProc("reply_preChecking", Reply_preChecking);
        }


        private void DispatcherReply_checkin()
        {
            Action<CsMessage> Reply_checkin = (CsMessage rcvMsg) =>
            {

                Console.Write("\n  Received Reply_checkin Msg is : ");
                rcvMsg.show();
                //counter to make sure only show msg once

                Console.WriteLine("\n  Check In Successfully,please check the RepoStorage folder");
                //Console.WriteLine("  File path in ClientStorage is : " + rcvMsg.value("checkinFile"));
                // test check out next
                if (rcvMsg.contains("demo"))
                {
                    if (rcvMsg.value("demo") == "next_testCheckout")
                    {
                        //testCheckout();
                    }
                }
                //use STA thread
                Action checkinNotification = () =>
                {
                    NavLocal.localNotification.Items.Add(" Check In Successfully");
                };
                Dispatcher.Invoke(checkinNotification, new Object[] { });

            };
            addClientProc("reply_checkin", Reply_checkin);
        }



        private void DispatcherReply_closeCheckin()
        {
            Action<CsMessage> Reply_closeCheckin = (CsMessage rcvMsg) =>
            {

                Console.Write("\n  Received Reply_closeCheckin Msg is : ");
                rcvMsg.show();
                //counter to make sure only show msg once

                Console.WriteLine("\n  Repo DB tried to close check in, result is " + rcvMsg.value("closeResult"));
                //Console.WriteLine("  File path in ClientStorage is : " + rcvMsg.value("checkinFile"));
                // test check out next
                if (rcvMsg.contains("demo"))
                {
                    if (rcvMsg.value("demo") == "next_testCheckout")
                    {
                        //testCheckout();
                    }
                }
                //use STA thread
                Action closeCheckinNotification = () =>
                {
                    NavRemote.close_result.Items.Add(rcvMsg.value("closeResult"));
                    NavRemote.remoteNotification.Items.Add("  Try to close " + rcvMsg.value("fileName") + ". Please check details on console");
                };
                Dispatcher.Invoke(closeCheckinNotification, new Object[] { });

            };
            addClientProc("reply_closeCheckin", Reply_closeCheckin);
        }

        private void DispatcherReply_checkoutSending()
        {
            Action<CsMessage> Reply_checkoutSending = (CsMessage rcvMsg) =>
            {

                Console.Write("\n  Received Reply_checkoutSending Msg is : ");
                rcvMsg.show();
                //counter to make sure only show msg once

                Console.WriteLine("\n  Client received " + rcvMsg.value("sendingFile")+". Moving it to LocalStorage");
                //Console.WriteLine("  File path in ClientStorage is : " + rcvMsg.value("checkinFile"));
                // test check out next
                if (rcvMsg.contains("demo"))
                {
                    if (rcvMsg.value("demo") == "next_testCheckout")
                    {
                        //testCheckout();
                    }
                }
                //use STA thread
                Action closeCheckinNotification = () =>
                {
                    NavRemote.remoteNotification.Items.Add("  Received " + rcvMsg.value("sendingFile") + ".Moving it to LocalStorage.");
                };
                Dispatcher.Invoke(closeCheckinNotification, new Object[] { });

            };
            addClientProc("reply_checkoutSending", Reply_checkoutSending);
        }

        private void DispatcherReply_checkoutFinished()
        {
            Action<CsMessage> Reply_checkoutFinished = (CsMessage rcvMsg) =>
            {

                Console.Write("\n  Received Reply_checkoutFinished Msg is : ");
                rcvMsg.show();
                //counter to make sure only show msg once

                Console.WriteLine("\n  Received " + rcvMsg.value("fileName") + " and all related files. Check them in LocalStorage.");
                //Console.WriteLine("  File path in ClientStorage is : " + rcvMsg.value("checkinFile"));
                // test check out next
                if (rcvMsg.contains("demo"))
                {
                    if (rcvMsg.value("demo") == "next_testCheckout")
                    {
                        //testCheckout();
                    }
                }
                //use STA thread
                Action closeCheckinNotification = () =>
                {
                    NavRemote.remoteNotification.Items.Add("  Received " + rcvMsg.value("fileName") + " and all related files. Check them in LocalStorage.");
                };
                Dispatcher.Invoke(closeCheckinNotification, new Object[] { });

            };
            addClientProc("reply_checkoutFinished", Reply_checkoutFinished);
        }




        private void DispatcherReply_displayCate()
        {
            Action<CsMessage> Reply_displayCate = (CsMessage rcvMsg) =>
            {

                Console.Write("\n  Received Reply_displayCate Msg is : ");
                rcvMsg.show();
                //counter to make sure only show msg once

               // Console.WriteLine("\n  Received " + rcvMsg.value("fileName") + " and all related files. .");
                //Console.WriteLine("  File path in ClientStorage is : " + rcvMsg.value("checkinFile"));
                // test check out next
                if (rcvMsg.contains("demo"))
                {
                    if (rcvMsg.value("demo") == "next_testCheckout")
                    {
                        //testCheckout();
                    }
                }
                //use STA thread
                Action closeCheckinNotification = () =>
                {
                   
                    string[] words = rcvMsg.value("fileName").Split('/');
                    NavRemote.cateList.Items.Add("Categories are: ");
                    foreach (var word in words)
                    {
                        NavRemote.cateList.Items.Add(word);
                    }
 
                };
                Dispatcher.Invoke(closeCheckinNotification, new Object[] { });

            };
            addClientProc("reply_displayCate", Reply_displayCate);
        }


        private void DispatcherReply_query()
        {
            Action<CsMessage> Reply_query = (CsMessage rcvMsg) =>
            {

                Console.Write("\n  Received Reply_query Msg is : ");
                rcvMsg.show();
                //counter to make sure only show msg once

                // Console.WriteLine("\n  Received " + rcvMsg.value("fileName") + " and all related files. .");
                //Console.WriteLine("  File path in ClientStorage is : " + rcvMsg.value("checkinFile"));
                // test check out next
                if (rcvMsg.contains("demo"))
                {
                    if (rcvMsg.value("demo") == "next_testCheckout")
                    {
                        //testCheckout();
                    }
                }
                //use STA thread
                Action closeCheckinNotification = () =>
                {

                    string[] words = rcvMsg.value("fileName").Split('/');
                    show_Query.Items.Add("Query result is: ");
                    foreach (var word in words)
                    {
                        show_Query.Items.Add(word);
                    }

                };
                Dispatcher.Invoke(closeCheckinNotification, new Object[] { });

            };
            addClientProc("reply_query", Reply_query);
        }


        //----< load all dispatcher processing >---------------------------

        private void loadDispatcher()
        {
            DispatcherLoadGetDirs();
            DispatcherLoadGetFiles();
            DispatcherLoadSendFile();
            DispatcherReply_descrip();
            DispatcherReply_preChecking();
            DispatcherReply_checkin();
            DispatcherReply_closeCheckin();
            DispatcherReply_checkoutSending();
            DispatcherReply_checkoutFinished();
            DispatcherReply_displayCate();
            DispatcherReply_query();
        }
        //----< start Comm, fill window display with dirs and files >------

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // start Comm
            endPoint_ = new CsEndPoint();
            endPoint_.machineAddress = "localhost";
            endPoint_.port = 8082;
            //set remote window and local window comm's endpoint
            NavRemote.navEndPoint_ = endPoint_;
            NavLocal.navEndPoint_ = endPoint_;

            translater = new Translater();
            translater.listen(endPoint_);

            // start processing messages
            processMessages();

            // load dispatcher
            loadDispatcher();

            serverEndPoint_ = new CsEndPoint();
            serverEndPoint_.machineAddress = "localhost";
            serverEndPoint_.port = 8080;
            //set remote window and local window comm's server endpoint
            NavRemote.serverEndPoint_ = serverEndPoint_;
            NavLocal.serverEndPoint_ = serverEndPoint_;

            pathStack_.Push("../Storage");

            NavRemote.PathTextBlock.Text = "Storage";
            NavRemote.pathStack_.Push("../Storage");

            NavLocal.PathTextBlock.Text = "LocalStorage";
            NavLocal.pathStack_.Push("");
            NavLocal.localStorageRoot_ = "../../../../LocalStorage";
            saveFilesPath = translater.setSaveFilePath("../../../SaveFiles");
            sendFilesPath = translater.setSendFilePath("../../../SendFiles");

            NavLocal.refreshDisplay();
            NavRemote.refreshDisplay();
            test1();
        }
        //----< strip off name of first part of path >---------------------

        public string removeFirstDir(string path)
        {
            string modifiedPath = path;
            int pos = path.IndexOf("/");
            modifiedPath = path.Substring(pos + 1, path.Length - pos - 1);
            return modifiedPath;
        }
        //----< show file text >-------------------------------------------

        private void showFile(string fileName)
        {
            Paragraph paragraph = new Paragraph();
            string fileSpec = saveFilesPath + "\\" + fileName;
            string fileText = File.ReadAllText(fileSpec);
            paragraph.Inlines.Add(new Run(fileText));
            CodePopupWindow popUp = new CodePopupWindow();
            popUp.codeView.Blocks.Clear();
            popUp.codeView.Blocks.Add(paragraph);
            popUp.Show();
        }
        //----< first test not completed >---------------------------------

       void test1()
        {
            Console.WriteLine("\n\n  Test check in Comm.h, depends on Comm.cpp.Folder name in Repo is the same as package description");
            Console.WriteLine("  ===================================");
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint_));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "descripFolder");
            msg.add("descripFolder", "Comm Package");
            translater.postMessage(msg);
            string fileName = "Comm.h";
            string srcFile = NavLocal.localStorageRoot_ + "/" + fileName;
            srcFile = System.IO.Path.GetFullPath(srcFile);
            string dstFile = sendFilesPath + "/" + fileName;
            System.IO.File.Copy(srcFile, dstFile, true);
            msg.remove("descripFolder");
            msg.remove("command");
            msg.add("command", "preCheckingin");
            msg.add("sendingFile", fileName);
            translater.postMessage(msg);
            NavLocal.localNotification.Items.Add("confirm select " + fileName + " as check in file");
            fileName = "Comm.cpp";
            srcFile = NavLocal.localStorageRoot_ + "/" + fileName;
            dstFile = sendFilesPath + "/" + fileName;
            System.IO.File.Copy(srcFile, dstFile, true);
            msg.remove("sendingFile");
            msg.add("sendingFile", fileName);
            translater.postMessage(msg);
            NavLocal. localNotification.Items.Add("add " + fileName + " to dependency files");
            fileName = "Comm.cpp";
            srcFile = NavLocal.localStorageRoot_ + "/" + fileName;
            dstFile = sendFilesPath + "/" + fileName;
            System.IO.File.Copy(srcFile, dstFile, true);
            CsMessage msg_commH = new CsMessage();
            msg_commH.add("to", CsEndPoint.toString(serverEndPoint_));
            msg_commH.add("from", CsEndPoint.toString(endPoint_));
            msg_commH.add("command", "checkin");
            msg_commH.add("checkinFile", "Comm.h");
            msg_commH.add("userName", "WeitianDing");
            msg_commH.add("descrip", "Comm Package");
            msg_commH.add("category", "MsgPassingCommunication/TestNameSpace/");
            msg_commH.add("dependencyFiles", "Comm.cpp/");
            msg_commH.show();
            translater.postMessage(msg_commH);
            test2();
        }

        void test2() {
            Console.WriteLine("\n\n  Test check in Comm.cpp, depends on Comm.h.Folder name in Repo is the same as package description");
            Console.WriteLine("  ===================================");
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint_));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "descripFolder");
            msg.add("descripFolder", "Comm Package");
            translater.postMessage(msg);
            string fileName = "Comm.cpp";
            string srcFile = NavLocal.localStorageRoot_ + "/" + fileName;
            srcFile = System.IO.Path.GetFullPath(srcFile);
            string dstFile = sendFilesPath + "/" + fileName;
            System.IO.File.Copy(srcFile, dstFile, true);
            msg.remove("descripFolder");
            msg.remove("command");
            msg.add("command", "preCheckingin");
            msg.add("sendingFile", fileName);
            translater.postMessage(msg);
            NavLocal.localNotification.Items.Add("confirm select " + fileName + " as check in file");
            fileName = "Comm.h";
            srcFile = NavLocal.localStorageRoot_ + "/" + fileName;
            dstFile = sendFilesPath + "/" + fileName;
            System.IO.File.Copy(srcFile, dstFile, true);
            msg.remove("sendingFile");
            msg.add("sendingFile", fileName);
            translater.postMessage(msg);
            NavLocal.localNotification.Items.Add("add " + fileName + " to dependency files");
            fileName = "Comm.cpp";
            srcFile = NavLocal.localStorageRoot_ + "/" + fileName;
            dstFile = sendFilesPath + "/" + fileName;
            System.IO.File.Copy(srcFile, dstFile, true);
            CsMessage msg_commH = new CsMessage();
            msg_commH.add("to", CsEndPoint.toString(serverEndPoint_));
            msg_commH.add("from", CsEndPoint.toString(endPoint_));
            msg_commH.add("command", "checkin");
            msg_commH.add("checkinFile", "Comm.cpp");
            msg_commH.add("userName", "WeitianDing");
            msg_commH.add("descrip", "Comm Package");
            msg_commH.add("category", "MsgPassingCommunication/TestNameSpace/");
            msg_commH.add("dependencyFiles", "Comm.h/");
            msg_commH.show();
            translater.postMessage(msg_commH);
            test3();
        }






        void test3() {
            Console.WriteLine("\n\n  Test show categories of Comm.h.Please check the result on GUI->Remote tab->Operation->display categories");
            Console.WriteLine("  =============================================");

            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint_));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "displayCate");
            msg.add("fileName", "Comm.h");
            translater.postMessage(msg);

            test4();
        }


        void test4() {
            Console.WriteLine("\n\n  Test close check in Comm.cpp with a wrong User name.  Please check the result on GUI->Remote tab->Operation->close checkin tab");
            Console.WriteLine("  =============================================");

            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint_));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "closeCheckin");
            msg.add("userName", "invalid user");
            msg.add("fileName", "Comm.cpp");
            translater.postMessage(msg);
            test5();
        }

        void test5()
        {
            Console.WriteLine("\n\n  Test close check in Comm.cpp with a valid User name.  Please check the result on GUI->Remote tab->Operation->close checkin tab");
            Console.WriteLine("  =============================================");

            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint_));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "closeCheckin");
            msg.add("userName", "WeitianDing");
            msg.add("fileName", "Comm.cpp");
            translater.postMessage(msg);
            test6();
        }

        void test6()
        {
            Console.WriteLine("\n\n  Test close check in Comm.h with a valid User name.  Please check the result on GUI->Remote tab->Operation->close checkin tab");
            Console.WriteLine("  =============================================");

            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint_));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "closeCheckin");
            msg.add("userName", "WeitianDing");
            msg.add("fileName", "Comm.h");
            translater.postMessage(msg);
            test7();
        }

        void test7()
        {
            Console.WriteLine("\n\n  Test check in Comm.h AGAIN, depends on Comm.cpp.Folder name in Repo is the same as package description");
            Console.WriteLine("  ===================================");
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint_));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "descripFolder");
            msg.add("descripFolder", "Comm Package");
            translater.postMessage(msg);
            string fileName = "Comm.h";
            string srcFile = NavLocal.localStorageRoot_ + "/" + fileName;
            srcFile = System.IO.Path.GetFullPath(srcFile);
            string dstFile = sendFilesPath + "/" + fileName;
            System.IO.File.Copy(srcFile, dstFile, true);
            msg.remove("descripFolder");
            msg.remove("command");
            msg.add("command", "preCheckingin");
            msg.add("sendingFile", fileName);
            translater.postMessage(msg);
            NavLocal.localNotification.Items.Add("confirm select " + fileName + " as check in file");
            fileName = "Comm.cpp";
            srcFile = NavLocal.localStorageRoot_ + "/" + fileName;
            dstFile = sendFilesPath + "/" + fileName;
            System.IO.File.Copy(srcFile, dstFile, true);
            msg.remove("sendingFile");
            msg.add("sendingFile", fileName);
            translater.postMessage(msg);
            NavLocal.localNotification.Items.Add("add " + fileName + " to dependency files");
            fileName = "Comm.cpp";
            srcFile = NavLocal.localStorageRoot_ + "/" + fileName;
            dstFile = sendFilesPath + "/" + fileName;
            System.IO.File.Copy(srcFile, dstFile, true);
            CsMessage msg_commH = new CsMessage();
            msg_commH.add("to", CsEndPoint.toString(serverEndPoint_));
            msg_commH.add("from", CsEndPoint.toString(endPoint_));
            msg_commH.add("command", "checkin");
            msg_commH.add("checkinFile", "Comm.h");
            msg_commH.add("userName", "WeitianDing");
            msg_commH.add("descrip", "Comm Package");
            msg_commH.add("category", "MsgPassingCommunication/TestNameSpace/");
            msg_commH.add("dependencyFiles", "Comm.cpp/");
            msg_commH.show();
            translater.postMessage(msg_commH);
            test8();
        }

        void test8()
        {
            Console.WriteLine("\n\n  Test check in Comm.cpp AGAIN, depends on Comm.h.Folder name in Repo is the same as package description");
            Console.WriteLine("  ===================================");
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint_));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "descripFolder");
            msg.add("descripFolder", "Comm Package");
            translater.postMessage(msg);
            string fileName = "Comm.cpp";
            string srcFile = NavLocal.localStorageRoot_ + "/" + fileName;
            srcFile = System.IO.Path.GetFullPath(srcFile);
            string dstFile = sendFilesPath + "/" + fileName;
            System.IO.File.Copy(srcFile, dstFile, true);
            msg.remove("descripFolder");
            msg.remove("command");
            msg.add("command", "preCheckingin");
            msg.add("sendingFile", fileName);
            translater.postMessage(msg);
            NavLocal.localNotification.Items.Add("confirm select " + fileName + " as check in file");
            fileName = "Comm.h";
            srcFile = NavLocal.localStorageRoot_ + "/" + fileName;
            dstFile = sendFilesPath + "/" + fileName;
            System.IO.File.Copy(srcFile, dstFile, true);
            msg.remove("sendingFile");
            msg.add("sendingFile", fileName);
            translater.postMessage(msg);
            NavLocal.localNotification.Items.Add("add " + fileName + " to dependency files");
            fileName = "Comm.cpp";
            srcFile = NavLocal.localStorageRoot_ + "/" + fileName;
            dstFile = sendFilesPath + "/" + fileName;
            System.IO.File.Copy(srcFile, dstFile, true);
            CsMessage msg_commH = new CsMessage();
            msg_commH.add("to", CsEndPoint.toString(serverEndPoint_));
            msg_commH.add("from", CsEndPoint.toString(endPoint_));
            msg_commH.add("command", "checkin");
            msg_commH.add("checkinFile", "Comm.cpp");
            msg_commH.add("userName", "WeitianDing");
            msg_commH.add("descrip", "Comm Package");
            msg_commH.add("category", "MsgPassingCommunication/TestNameSpace/");
            msg_commH.add("dependencyFiles", "Comm.h/");
            msg_commH.show();
            translater.postMessage(msg_commH);
            //test9();
        }

        private void confirm_fileName_Click(object sender, RoutedEventArgs e)
        {
            fileNameQ_ = fileNameQuery.Text;
            show_Query.Items.Add("confirm file name query");
        }

        private void confirm_dependency_Click(object sender, RoutedEventArgs e)
        {
            dependencyQ_ = dependencyQuery.Text;
            show_Query.Items.Add("confirm dependency query");
        }

        private void confirm_version_Click(object sender, RoutedEventArgs e)
        {
            versionQ_ = versionQuery.Text;
            show_Query.Items.Add("confirm version query");
        }

        private void confirm_Cate_Click(object sender, RoutedEventArgs e)
        {
            cateQ_ = cateQuery.Text;
            show_Query.Items.Add("confirm category query");
        }

        private void start_query_Click(object sender, RoutedEventArgs e)
        {
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint_));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "query");
            msg.add("fileName", fileNameQ_);
            msg.add("dependency", dependencyQ_);
            msg.add("version", versionQ_);
            msg.add("category", cateQ_);
            translater.postMessage(msg);
        }
    }
}
