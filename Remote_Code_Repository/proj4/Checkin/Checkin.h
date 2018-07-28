#pragma once
/////////////////////////////////////////////////////////////////////
// Chechin.h - Implements Checkin for Repo					       //
// ver 1.0                                                         //
// WeitianDing, CSE687 - Object Oriented Design, Spring 2018	   //
// Language:    C++												   //
// Platform:    desktop, Win10									   //  
/////////////////////////////////////////////////////////////////////
/*
*Package Operations:
*provides the functionality to start a new package version by accepting files,
*appending version numbers to their filenames,
*providing dependency and category information,
*creating metadata, and storing the files in a designated location.
*
* Package Operations:
* -------------------
* It defines following functions:
*	- showRepoDB : show Repo DB and payload
*	- showPayLoad : only show payload
*	- restoreRepoDB : restore Repo DB from XML file and invoke restoreVersionSystem to update VersionSystem
*	- restoreVersionSystem : restore version system according to Repo DB
*	- getAllFiles : bfs to get all files, invoke getCurrentFiles and getCurrentDirs, return a vector of files specpath in client
*	- getCurrentFiles : bfs to get all files in current dir
*	- getCurrentDirs : bfs to get all dirs in current dir
*	- checkinFiles :  get client file paths and start checking in, invoke updateVersion
*	- updateVersion :check the state of checking in file, update repo db and copy files from client to repo(invoke insertEle_RepoDB)
*	- insertEle_RepoDB : insert element into repo db
*	- getAllDependency: select checking in files' dependency files
*	- closeCheckin : check the state in payload and try to close checking in
*
*
* Required Files:
* ---------------
* DbCore.h , DateTime.h
* DateTime.cpp, DbOperation.h
* DbOperation.cpp, PayLoad.h
* Conditions.h

* Maintenance History:
* --------------------
* ver 1.0 : 8 March 2018
* - first release
*/

#include "../CppNoSqlDb/DbCore/DbCore.h"
#include "../CppNoSqlDb/DateTime/DateTime.h"
#include "../FileSystem-Windows/FileSystemDemo/FileSystem.h"
#include "../Version/Version.h"

#include "../CppNoSqlDb/XmlDocument/XmlDocument/XmlDocument.h"
#include "../CppNoSqlDb/XmlDocument/XmlElement/XmlElement.h"
#include <string>
#include <vector>
#include <fstream>
#include <windows.h>
#include <queue>
#include <set>
#include <thread>
#include <regex>

using namespace NoSqlDb;
using namespace FileSystem;
using namespace std;
using namespace VersionSystem;


template<typename T>
class Checkin {
private:
	DbCore<T>  *db_repo;
	const string repoPath = "./../Storage/";
public:
	
	Checkin(DbCore<T> & dbRepo);
	//show Repo DB and payload
	void showRepoDB();
	//only show payload
	void showPayLoad();
	
	bool checkAllFiles(const string& filePath, const vector<string>& dependencyPaths);

	bool checkSingleFile(const string&  filePath);

	bool checkMultiFiles(const vector<string>& dependencyPaths);

	//check in files
	void checkinFiles(const string & checkinFilePath, const string& description, const string& userName, const vector<string>& cate, const vector<string>& dependency = vector<string>());
	//update version, update repo db and copy files from client to repo
	string updateVersion(const string& fileName, const string & path, const string& description, const string& userName, const vector<string>& cate, const vector<string>& dependency);

	string createVersion(const string& fileName, const string & path, const string& description, const string& userName, const vector<string>& cate, const vector<string>& dependency);
	//insert element into repo db
	void insertEle_RepoDB(const string& repoFilePath_Spec, const string& description, const string& userName, const vector<string>& cate, const vector<string>& dependency = vector<string>());
	//get all dependency files
	set<string> getAllDependency(const string& fileName);
	//try to close check in 
	string closeCheckin(const string& userName, const string& fileName);

};

template<typename T>
Checkin<T>::Checkin(DbCore<T> & dbRepo) {
	this->db_repo = &dbRepo;
}

template<typename T>
void Checkin<T>::showRepoDB() {
	//show repo db and payload
	cout << endl;
	Utilities::StringHelper::title("Show Repo DB and Payload");
	cout << endl;
	showDb(*db_repo);
	cout << endl;
	db_repo->begin()->second.payLoad().showDb(*db_repo);
	cout << endl;
}

template<typename T>
void Checkin<T>::showPayLoad() {
	//show payload
	Utilities::StringHelper::title("Show Payload");
	db_repo->begin()->second.payLoad().showDb(*db_repo);
	cout << endl;
}

template<typename T>
bool Checkin<T>::checkAllFiles(const string& filePath,const vector<string>& dependencyPaths) {
	if (!checkSingleFile( filePath))
	{
		return false;
	}
	if (!checkMultiFiles(dependencyPaths))
	{
		return false;
	}
	return true;
}

template <typename T>
bool Checkin<T>::checkSingleFile(const string& filePath) {
	FileSystem::File test(filePath);
	test.open(FileSystem::File::in);  // open as text file
	std::cout << "\n  processing \"" << filePath << "\"\n";
	cout << "  ----------------------------------" << endl;
	if (test.isGood()) {
		return true;
	}
	else {
		return false;
	}
}

template <typename T>
bool Checkin<T>::checkMultiFiles(const vector<string>& dependencyPaths) {
	for (auto path : dependencyPaths) {
		if (!checkSingleFile(path)) {
			return false;
		}
	}
	return true;
}



template<typename T>
void Checkin<T>::checkinFiles(const string & checkinFilePath, const string& description, const string& userName, const vector<string>& cate, const vector<string>& dependency) {
	try
	{
		if (!dependency.empty()) {
			for (auto filePath : dependency) {
				checkinFiles(filePath, description, userName, cate);
			}
		}
		string filePath_Repo;
		string fileName = FileSystem:: Path::getName(checkinFilePath);
		cout << "  writing to ./../Storage" << endl;
		if (VersionSystem::Version::getVersion().count(fileName) == 0) {
			filePath_Repo = createVersion(fileName, checkinFilePath, description, userName,cate, dependency);
		}
		else {
			filePath_Repo = updateVersion(fileName, checkinFilePath, description, userName, cate,dependency);
		}
		if (FileSystem::File::copy(checkinFilePath, filePath_Repo, false)) {
			cout << "  finish copyfile" << endl;
		}
		else {
			cout << "  fail to copyfile" << endl;
		}
	}
	catch (std::exception& ex)
	{
		std::cout << "\n  Exception: " << ex.what() << "\n";
	}
}


template<typename T>
string Checkin<T>::createVersion(const string& fileName, const string & path, const string& description, const string& userName, const vector<string>& cate, const vector<string>& dependency) {

	//update version map, first version
	VersionSystem::Version::add(FileSystem::Path::getName(fileName));
	string filePath_Repo = repoPath + description + "/" + fileName;
	cout << "  " + fileName + " doesn't exist in repo, moving..." << endl;
	insertEle_RepoDB(filePath_Repo, description, userName,cate, dependency);
	cout << "  " + filePath_Repo + " update to repo db, state is open" << endl;
	return filePath_Repo;
}



template<typename T>
string Checkin<T>::updateVersion(const string& fileName, const string& path, const string& description, const string& userName, const vector<string>& cate, const vector<string>& dependency) {

	string filePath_Repo;
	cout << "  " + fileName + " exists in repo, updating... " << endl;
	//get old file dependency
	vector<string> tempOld = (*db_repo)[fileName].children();
	if ((*db_repo)[VersionSystem::Version::getFileName_Version(fileName)].payLoad().getState() == 1) {
		//replace files if state is open
		cout << "  " << fileName << "  is in open state" << endl;
		filePath_Repo = repoPath + description + "/" + (VersionSystem::Version::getFileName_Version(fileName));
		cout << "  try to modify " << fileName + " in repo..." << endl;
	}
	else {
		cout << "  " + fileName + " is close/pending " << endl;
		//create new version 
		Version::add(fileName);
		filePath_Repo = repoPath + description + "/" + (VersionSystem::Version::getFileName_Version(fileName));
		cout << "  " << fileName + " copy to repo as a new version..." << endl;
	}
	//if no new dependency pass in, use the old dependency
	if (dependency.empty()) {
		insertEle_RepoDB(filePath_Repo, description, userName,cate, tempOld);
	}
	else {
		insertEle_RepoDB(filePath_Repo, description, userName, cate,dependency);
	}
	cout << "  " + filePath_Repo + " update to repo db" << endl;
	return filePath_Repo;
}

template<typename T>
void Checkin<T>::insertEle_RepoDB(const string& repoFilePath_Spec, const string& description, const string& userName, const vector<string>& cate, const vector<string>& dependency) {
	try {
		string key;
		string fileName = FileSystem::Path::getName(repoFilePath_Spec);
		string extension = FileSystem::Path::getExt(fileName);
		regex e("[0-9]+?");
		bool match = regex_match(extension, e);
		if (!match) {
			key = fileName;
		}
		else {
			size_t lastDot = fileName.find_last_of(".");
			key = fileName.substr(0, lastDot);
		}
		//insert key/value pair in repo db
		DbElement<T> temp;
		temp.name(key);
		temp.dateTime(DateTime().now());
		vector<string> dependencyFilesName;
		if (!dependency.empty()) {		
			for (auto item : dependency)
			{
				dependencyFilesName.push_back(VersionSystem::Version::getFileName_Version(FileSystem::Path::getName(item)));
			}				
		}
		temp.children(dependencyFilesName);
		temp.descrip(description);
		//set up payload
		T pl;
		//user can change state manually
		pl.value(repoFilePath_Spec);
		pl.categories() = cate;
		pl.setUserName(userName);
		temp.payLoad(pl);
		//insert the element
		(*db_repo)[fileName] = temp;
		//showRepoDB();
		cout << "  create repo db element : " << fileName << endl;
	}
	catch (std::exception& ex)
	{
		std::cout << "\n\n  -- " << ex.what() << " --" << endl;
	}
}

template<typename T>
set<string> Checkin<T>::getAllDependency(const string& fileName) {
	// the set gonna be returned
	set<string> result;
	queue<string> childQ;
	// make sure no duplicate filename
	set<string> childS;
	childQ.push(fileName);
	childS.insert(fileName);
	//use bfs to search in all dependency relationship
	while (!childQ.empty()) {
		string tempChild = childQ.front();
		childQ.pop();
		result.insert(tempChild);
		for (string subChild : (*db_repo)[tempChild].children()) {
			if (childS.find(subChild) == childS.end()) {
				childQ.push(subChild);
				childS.insert(subChild);
				result.insert(subChild);
			}
		}
	}
	result.erase(fileName);
	cout << "  dependency files of " << fileName << " are : ";
	for (string item : result) {
		cout << item << " ";
	}
	cout << endl;
	return result;
}

template<typename T>
string  Checkin<T>::closeCheckin(const string& userName, const string& fileName) {
	Utilities::StringHelper::title("try to close " + fileName);
	cout << endl;
	if (!(*db_repo).contains(fileName)) {
		cout << "  " << fileName << " doesn't exist in db" << endl;
		return fileName + " doesn't exist in db";
	}
	if ((*db_repo)[fileName].payLoad().getUserName() != userName) {
		cout << "  " << userName << " doesn't match. Close fails" << endl;
		return userName + " doesn't match";
	}
	bool flag = true;//true means good to close
    //the set of all the dependency files
	set<string> dependencyFiles = getAllDependency(fileName);
	//the list of open files 	
	vector<string> waitingList;
	//the list of pending files gonna be closed
	vector<string> pendingList;
	for (string item : dependencyFiles) {
		if ((*db_repo)[item].payLoad().getState() == 1) {
			cout << "  " << item << " state is open, cannot close checkin" << endl;
			flag = false;
			waitingList.push_back(item);
		}
		else if ((*db_repo)[item].payLoad().getState() == 2) {
			//both closed and pending are the state of close
			pendingList.push_back(item);
			cout << "  " << item << " state is pending, will be closed" << endl;
		}
	}
	cout << endl;
	if (flag == true) {
		//make pending to closed
		cout << "  check in close successfully" << endl;
		(*db_repo)[fileName].payLoad().setState(0);
		for (string item : pendingList) {
			(*db_repo)[item].payLoad().setState(0);
			cout << "    " << item <<" was pending, now closed"<< endl;
		}
		return fileName + " closed";
	}
	else {
		//cannot close check in, output all the files need to be closed manually
		(*db_repo)[fileName].payLoad().setState(2);
		cout << "  cannot close check in, open files are: " << endl;
		for (string item : waitingList) {
			cout << "    " << item << endl;
		}
		cout << "  close check in error.  " << fileName << " is pending" << endl;
		return fileName + " pending now";
	}
}








