#ifndef CHECKOUT_H
#define CHECKOUT_H
/////////////////////////////////////////////////////////////////////
// Checkout.h - Implements checkout functions for repo		       //
// ver 1.0                                                         //
// WeitianDing, CSE687 - Object Oriented Design, Spring 2018		   //
// Language:    C++												   //
// Platform:    desktop, Win10									   //  
/////////////////////////////////////////////////////////////////////
/*
* Package Operations:
* -------------------
* It defines following functions:
*	- moveNewestFile : check the newest file in version management 
*	and copy that file from repo to client wiout version number extension
*	- getDependency : get all dependency files
*
* Required Files:
* ---------------
* Version.h ; FileSystem.h
* PayLoad.h ; DbCore.h
*
* Maintenance History:
* --------------------
* ver 1.0 : 8 March 2018
* - first release
*/


#include "../CppNoSqlDb/DbCore/DbCore.h"
#include "../FileSystem-Windows/FileSystemDemo/FileSystem.h"
#include "../Version/Version.h"
#include "../CppNoSqlDb/PayLoad/PayLoad.h"
#include<regex>
#include<set>
#include<queue>
#include<vector>

using namespace FileSystem;
using namespace std;
using namespace VersionSystem;
using namespace NoSqlDb;

template<typename T>
class Checkout {
public:
	//check the newest file in version management and copy that file from repo to client wiout version number extension
	string moveNewestFile(const string& fileName);
	//get all dependency files
	set<string> getDependency(const string& fileName);

	//vector<string> displayCate(string fileName);
	//constructor
	Checkout(DbCore<T> & dbRepo);
private:
	DbCore<T>  *db_repo;
	//repo path
	string repoPath = "./../Storage/";
	//client path
	string clientDownloadPath = "./../ServerPrototype/SendFiles/";
};


template<typename T>
Checkout<T>::Checkout(DbCore<T> & dbRepo) {
	//constructor, point to repo db
	this->db_repo = &dbRepo;
}




//template<typename T>
//vector<string> displayCate(string fileName) {
//	vector<string> result;
//	if (!(*db_repo).contains(fileName)) {
//		cout << "  " << fileName << " doesn't exist in db" << endl;
//	}
//	else {
//		result = (*db_repo)[fileName].payLoad().categories();
//	}
//
//
//	return result;
//
//
//}



template<typename T>
string Checkout<T>::moveNewestFile(const string& fileName) {
	cout << endl;
	cout << "  looking for " << fileName << " newest version" << endl;
	//get filename without extension first
	string pureName;
	string extension = FileSystem::Path::getExt(fileName);
	regex e("[0-9]+?");
	bool match = regex_match(extension, e);
	if (!match) {
		//first version, no numeric at the end
		//ex.  datatime.h
		pureName = fileName;
	}
	else {
		//with numeric
		//ex. datatime.h.2
		size_t lastDot = fileName.find_last_of(".");
		pureName = fileName.substr(0, lastDot);
	}
	//use pureName to check newest version
	string newestName = Version::getFileName_Version(pureName);
	cout << "  checking out " << newestName << endl;
	string filePath = (*db_repo)[newestName].payLoad().value();
	cout << "  from " << filePath << " to " << clientDownloadPath + pureName;
	//copy that file from repo to client
	if (FileSystem::File::copy(filePath, clientDownloadPath + pureName, false)) {
		cout << " successfully" << endl;
		getDependency(newestName);
	}
	else {
		cout << " fails" << endl;
	}
	return pureName;
}


template<typename T> 
set<string>Checkout<T>::getDependency(const string& fileName) {
	set<string> result;//the result gonna be returned
	queue<string> childQ;
	set<string> childS;//make sure no duplicate files
	childQ.push(fileName);
	childS.insert(fileName);
	//use bfs to get all dependency files
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
	return result;
}


#endif