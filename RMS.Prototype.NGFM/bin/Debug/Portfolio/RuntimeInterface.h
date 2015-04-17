

# pragma once

// The interface of runtime with binder and potfolio
// If this file changes, runtime needs to be updated

struct Losses
{
    int idx;
    double loss;
};

struct LossesWithPerilId
{
    int id;
	int perilId;
	double lossRatio;
};

enum CdlOption
{
	CONTRACT_GROSS = 1,
	CONTRACT_GROUNDUP = 2,
	PORTFOLIO_GROSS = 4,
	PORTFOLIO_GROUNDUP = 8,
	RITE_GROUNDUP = 16,
	RIT_GROSS = 32,
	RITE_GROSS = 64,
	DQ = 128
};

struct CalculationInterface
{
	int cdlOption;
	int numInputLosses;
	int numRiteGross;
	int numContractGross;
	int numCurrencies;
	LossesWithPerilId* inputs;
	Losses* outContractGross;
	Losses* outRiteGross;
	Losses* currencyTable;
};

// Functions expected to be exposed - All functions are C functions
//
// Stamper.DLL 
// void Initialize(char *fileName_Map, void **object);
// void Accept(void **object, int peril, int nInputs, Losses* inputs, int *nOutputs, Losses *outputs);
//
// Calculation.dll
// void Initialize(char *fileName_IR, void **object);
// Pay(void **object, int nInputs, Losses* inputs, int *nOutputs, Losses *outputs)
//
// Any objects created on the unmanaged heap can be written into <void **object>
// This will be passed back in 


     
