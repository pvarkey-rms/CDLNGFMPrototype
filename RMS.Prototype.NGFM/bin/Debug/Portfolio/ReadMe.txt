========================================================================
    DLL : Calculation
========================================================================

The following files are created by code generation :
Calculation0.cpp
    initialize the portfolio :
	(1) set number of contracts in the portfolio
SetContracts.cpp
    static functions of loss calculation for non basic type nodes
	link the calculation function pointer of those nodes the above functions




A. file with dll interface functions
==================================================================================================
file name : Calculation.[h,cpp]
consists of all dll interface functions :
(1) void initialize(char* inputFile, long long* object)
    this function will be called only once before loss calculation loop
    create the Calculation object
	read input data from the file "inputFile",
	initialize the portfolio with the data just read in
	set function pointer for the loss calculation for each node and each cover
(2) void Pay(long long object, int nInputs, Losses* inputs, int *nOutputs, Losses *outputs)
    this function will be called in a loop of loss calculation for subevents
==================================================================================================




B. files with all internel structures and functions
==================================================================================================
file name : BasicCalc.cpp
    functions for calculating losses for basic type node
==================================================================================================
file name : Contract.[h,cpp]
==================================================================================================
file name : Cover.[h,cpp]
==================================================================================================
file name : fileIO.[h,cpp]
    functions to read data from .csv file
==================================================================================================
file name : Node.[h,cpp]
==================================================================================================
file name : utilFunc.[h,cpp]
==================================================================================================
