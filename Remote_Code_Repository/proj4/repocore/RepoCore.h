#pragma once


#include "../CppNoSqlDb/DbCore/DbCore.h"
//#include "../CppNoSqlDb/PayLoad/PayLoad.h"
#include "../Checkin/Checkin.h"
#include "../Checkout/Checkout.h"
#include "../CppNoSqlDb/XmlDocument/XmlDocument/XmlDocument.h"
#include "../CppNoSqlDb/XmlDocument/XmlElement/XmlElement.h"
#include "../FileSystem-Windows/FileSystemDemo/FileSystem.h"
#include "../CppNoSqlDb/DateTime/DateTime.h"
#include "../Version/Version.h"
//#include"../CppNoSqlDb/Query/Query.h"
#include <string>
#include <vector>
#include <set>


using namespace std;
using namespace NoSqlDb;

template<typename T>
class RepoCore {
public:
	RepoCore(DbCore<T> & dbRepo);
	bool RepoCheckin(string filePath,  string descrip, string userName, vector<string> cate,vector<string> dependencyFilesPath=vector<string>() );
	
	set<string> RepoCheckout(string fileName);

	string closeCheckin(string userName,string fileName);

	void showRepoDB();
	void showPayLoad();

	vector<string> displayCate(string fileName);

	set<string> getQueryResult(string fileName, string version, string cate, string dependency);

private:
	DbCore<T>  *db_repo;
	//Checkin<PayLoad> checkin = Checkin<PayLoad>(dbRepo);
	Checkin<T> *checkin;
	Checkout<T> *checkout;
	/*Query<T> *q;*/

};

template<typename T>
RepoCore<T>::RepoCore(DbCore<T> & dbRepo) {
	this->db_repo = &dbRepo;
	checkin = new Checkin<T>(dbRepo);
	checkout = new Checkout<T>(dbRepo);
	//q = new Query<T>(dbRepo);
	//temp.showRepoDB();
	//this->checkin = &temp;
	//checkin = &temp;
	//Checkin<T> asd = Checkin<T>(dbRepo);
}

template<typename T>
string RepoCore<T>::closeCheckin(string userName, string fileName) {
	return checkin->closeCheckin(userName,fileName);
}

template<typename T>
void RepoCore<T>::showPayLoad() {
	checkin->showPayLoad();
}

template<typename T>
void RepoCore<T>::showRepoDB() {
	checkin->showRepoDB();
}


template<typename T>
bool RepoCore<T>::RepoCheckin(string filePath, string descrip, string userName,vector<string> cate, vector<string> dependencyFilesPath ) {
	//this->db_repo = &dbRepo;
	if (checkin->checkAllFiles(filePath, dependencyFilesPath)) {
		checkin->checkinFiles(filePath, descrip, userName, cate,dependencyFilesPath);
	}
	else {
		return false;
	}
	return true;
}


template<typename T>
set<string> RepoCore<T>::RepoCheckout(string fileName) {
	if (!(*db_repo).contains(fileName)) {
		cout << "  " << fileName << " doesn't exist in db" << endl;
		return set<string>();
	}
	set<string> allFiles = checkout->getDependency(fileName);
	cout << "  All required files of "<<fileName<< " are : ";
	for (string item : allFiles) {
		cout << item << " ";
	}
	cout << endl;
	set<string> fileNames;
	for (auto item : allFiles) {
		string temp = checkout->moveNewestFile(item);
		fileNames.insert(temp);
	}
	return fileNames;
}

template<typename T>
vector<string> RepoCore<T>::displayCate(string fileName) {
	vector<string> result;
	if (!(*db_repo).contains(fileName)) {
		cout << "  " << fileName << " doesn't exist in db" << endl;
	}
	else {
		result = (*db_repo)[fileName].payLoad().categories();
	}


	return result;


}

//template<typename T>
//set<string> RepoCore<T>::getQueryResult(string fileName, string version, string cate, string dependency) {
//	Conditions<T> conds;
//	conds.categories(cate);
//	conds.children(dependency);
//	conds.version(version);
//	conds.name(fileName);
//	q->select(conds).show();
//	return q->select(conds).getResult();
//	
//}