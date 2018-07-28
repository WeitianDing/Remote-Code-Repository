#pragma once
/////////////////////////////////////////////////////////////////////
// Version.h - Implements Version management for Repo		       //
// ver 1.0                                                         //
// WeitianDing, CSE687 - Object Oriented Design, Spring 2018	   //
// Language:    C++												   //
// Platform:    desktop, Win10									   //  
/////////////////////////////////////////////////////////////////////
/*
* Package Operations:
* -------------------
* It defines following functions:
*	- getVersion : get the map versionStore
*	- add : insert new file name to version management and update the version automatically
*	- change : change the version of the file name
*	- getFileName_Version : bind pure file name with version number extension
*	- showVersion : show version system
*
* Required Files:
* ---------------
* StringUtilities.h

* Maintenance History:
* --------------------
* ver 1.0 : 8 March 2018
* - first release
*/

#include "../CppCommWithFileXfer/Utilities/Utilities.h"
#include <map>

using namespace std;

namespace VersionSystem {
	class Version {
	private:
		static map<string, int> versionStore;
	public:
		//get the map versionStore
		static map<string, int> getVersion();

		//insert new file name to version management and update the version automatically
		static void add(const string& filename);

		//change the version of the file name
		static void change(const string& filename, const int& version);

		//bind pure file name with version number extension
		static string getFileName_Version(const string& fileName);

		//show version system
		static void showVersion();
	};

}