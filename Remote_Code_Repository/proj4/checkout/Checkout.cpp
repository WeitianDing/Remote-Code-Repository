#include "stdafx.h"
#include "Checkout.h"



//test stub
#ifdef TEST_CHECKOUT
int main()
{
	DbCore<PayLoad> testDB_Repo;
	Checkout<PayLoad> test(testDB_Repo);
	//move a file doesn't exist in version management
	test.moveNewestFile("DbCore.h");
	test.moveNewestFile("test.h");
    return 0;
}
#endif
