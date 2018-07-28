///////////////////////////////////////////////////////////////////////
// LocalNavControl.xaml.cs - Control GUI for Local Navigation        //
// ver 1.0                                                           //
// Jim Fawcett, CSE687 - Object Oriented Design, Spring 2018         //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package provides a WPF-based control GUI for Project4Demo.  It's 
 * responsibilities are to:
 * - Provide a display of directory contents of the local client.
 * - It provides a subdirectory list and a filelist for the selected directory.
 * - You can navigate into subdirectories by double-clicking on subdirectory
 *   or the parent directory, indicated by the name "..".
 *   
 * Required Files:
 * ---------------
 * LocalNavControl.xaml, LocalNavControl.xaml.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 22 Apr 2018
 * - first release
 */
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
using MsgPassingCommunication;

namespace WpfApp1
{
  public partial class LocalNavControl : UserControl
  {
    internal Stack<string> pathStack_ = new Stack<string>();
    internal string localStorageRoot_;
        string descript_="";
        string userName_="";
        string fileName_="";
        string category_ = "";
        //string tempFile_="";
        string dependencyFiles_ = "";
       // List<string> selectedFiles_ = new List<string>();
        internal CsEndPoint navEndPoint_;
        internal CsEndPoint serverEndPoint_;


        public LocalNavControl()
    {
      InitializeComponent();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
      DirList.Items.Clear();
      string path = localStorageRoot_ + pathStack_.Peek();
      string[] dirs = System.IO.Directory.GetDirectories(path);
      foreach (string dir in dirs)
      {
        if (dir != "." && dir != "..")
        {
          string itemDir = System.IO.Path.GetFileName(dir);
          DirList.Items.Add(itemDir);
        }
      }
      DirList.Items.Insert(0, "..");

      FileList.Items.Clear();
      string[] files = System.IO.Directory.GetFiles(path);
      foreach (string file in files)
      {
        string itemFile = System.IO.Path.GetFileName(file);
        FileList.Items.Add(itemFile);
      }
    }

    internal void refreshDisplay()
    {
      Refresh_Click(this, null);
    }
    //----< strip off name of first part of path >---------------------

    public string removeFirstDir(string path)
    {
      string modifiedPath = path;
      int pos = path.IndexOf("/");
      modifiedPath = path.Substring(pos + 1, path.Length - pos - 1);
      return modifiedPath;
    }

    internal void clearDirs()
    {
      DirList.Items.Clear();
    }
    //----< function dispatched by child thread to main thread >-------

    internal void addDir(string dir)
    {
      DirList.Items.Add(dir);
    }
    //----< function dispatched by child thread to main thread >-------

    internal void insertParent()
    {
      DirList.Items.Insert(0, "..");
    }
    //----< function dispatched by child thread to main thread >-------

    internal void clearFiles()
    {
      FileList.Items.Clear();
    }
    //----< function dispatched by child thread to main thread >-------

    internal void addFile(string file)
    {
      FileList.Items.Add(file);
    }
    //----< show file text on MouseDoubleClick >-----------------------

    private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      MainWindow win = (MainWindow)Window.GetWindow(this);

      string fileName = (string)FileList.SelectedItem;
      string srcFile = localStorageRoot_ + "/" + pathStack_.Peek() + "/" + fileName;
      srcFile = System.IO.Path.GetFullPath(srcFile);
      string dstFile = win.sendFilesPath + "/" + fileName;
      System.IO.File.Copy(srcFile, dstFile, true);

      //serverEndPoint = new CsEndPoint();
      //serverEndPoint.machineAddress = "localhost";
      //serverEndPoint.port = 8080;
      CsMessage msg = new CsMessage();
      msg.add("to", CsEndPoint.toString(serverEndPoint_));
      msg.add("from", CsEndPoint.toString(navEndPoint_));
      msg.add("sendingFile", fileName);
      msg.add("path", pathStack_.Peek());
      msg.add("fileName", fileName);
      win.translater.postMessage(msg);
            //win.translater.se

    }
    //----< respond to mouse double-click on dir name >----------------

    private void DirList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      MainWindow win = (MainWindow)Window.GetWindow(this);

      // build path for selected dir
      string selectedDir = (string)DirList.SelectedItem;
      string path;
      if (selectedDir == "..")
      {
        if (pathStack_.Count > 1)  // don't pop off "LocalStorage"
          pathStack_.Pop();
        else
          return;
      }
      else
      {
        path = pathStack_.Peek() + "/" + selectedDir;
        pathStack_.Push(path);
      }
      // display path in Dir TextBlock
      // May use statement below later:
      // PathTextBlock.Text = win.removeFirstDir("LocalStorage/" + pathStack_.Peek());
      PathTextBlock.Text = "LocalStorage" + pathStack_.Peek();

      refreshDisplay();
    }

        private void confirm_descrip_Click(object sender, RoutedEventArgs e)
        {
            MainWindow win = (MainWindow)Window.GetWindow(this);

            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint_));
            msg.add("from", CsEndPoint.toString(navEndPoint_));
            msg.add("command", "descripFolder");
            descript_ = description.Text;
            msg.add("descripFolder", descript_);
            msg.show();
            win.translater.postMessage(msg);
        }

        //private void localFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    //selectedFiles_.Clear();
        //    ListBoxItem lbi = ((sender as ListBox).SelectedItem as ListBoxItem);
        //    foreach (var files in FileList.SelectedItems)
        //    {
                
        //        tempFile_ =  files.ToString();
        //    }
        //}

        private void selectFile_Click(object sender, RoutedEventArgs e)
        {
            //if (selectedFiles_.Count() == 1)
            //{
            //    fileName_ = selectedFiles_[0];
            //    localNotification.Items.Add("confirm select " + fileName_ +" as check in file");
            //}
            MainWindow win = (MainWindow)Window.GetWindow(this);
            fileName_ = (string)FileList.SelectedItem;
        
            string srcFile = localStorageRoot_ + "/" + pathStack_.Peek() + "/" + fileName_;
            srcFile = System.IO.Path.GetFullPath(srcFile);
            string dstFile = win.sendFilesPath + "/" + fileName_;
            System.IO.File.Copy(srcFile, dstFile, true);

            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint_));
            msg.add("from", CsEndPoint.toString(navEndPoint_));
            msg.add("command", "preCheckingin");
            msg.add("sendingFile", fileName_);    
            msg.show();
            win.translater.postMessage(msg);

            localNotification.Items.Add("confirm select " + fileName_ + " as check in file");
        }
        private void selectDependency_Click(object sender, RoutedEventArgs e)
        {
            //dependencyFiles_ = selectedFiles_;
            //string tempStr = "";
            //foreach (var item in selectedFiles_) {
            //    tempStr += item + ", ";
            //}
            //localNotification.Items.Add("confirm select dependency files : " + tempStr);
            MainWindow win = (MainWindow)Window.GetWindow(this);

            string fileName = (string)FileList.SelectedItem;
            string srcFile = localStorageRoot_ + "/" + pathStack_.Peek() + "/" + fileName;
            srcFile = System.IO.Path.GetFullPath(srcFile);
            string dstFile = win.sendFilesPath + "/" + fileName;
            System.IO.File.Copy(srcFile, dstFile, true);

            dependencyFiles_ += fileName + "/";

            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint_));
            msg.add("from", CsEndPoint.toString(navEndPoint_));
            msg.add("command", "preCheckingin");
            msg.add("sendingFile", fileName);
            msg.show();
            win.translater.postMessage(msg);
            localNotification.Items.Add("add " + (string)FileList.SelectedItem + " to dependency files" );

        }
        private void checkin_Click(object sender, RoutedEventArgs e)
        {
            MainWindow win = (MainWindow)Window.GetWindow(this);
 
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint_));
            msg.add("from", CsEndPoint.toString(navEndPoint_));
            msg.add("command", "checkin");
            msg.add("checkinFile", fileName_);
            msg.add("userName", userName_);
            msg.add("descrip", descript_);
            msg.add("category", category_);
            //string tempDependency = "";
            //foreach (var item in dependencyFiles_) {
            //    tempDependency += item + ",";
            //}
            msg.add("dependencyFiles", dependencyFiles_);
            msg.show();
            win.translater.postMessage(msg);

            fileName_ = "";
            userName_ = "";
            descript_ = "";
            category_ = "";
            dependencyFiles_ = "";
        }
        private void confirm_UserName_Click(object sender, RoutedEventArgs e)
        {
            userName_ = userName.Text;
            localNotification.Items.Add("confirm user name");
        }

        private void confirm_Cate_Click(object sender, RoutedEventArgs e)
        {
            category_ += category.Text + "/";
            localNotification.Items.Add("confirm category");
        }
    }
}
