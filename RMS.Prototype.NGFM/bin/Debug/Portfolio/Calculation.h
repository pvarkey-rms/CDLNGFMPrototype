#ifndef CALCULATIN_H
#define CALCULATIN_H

#ifndef DLLEXPORT
#define DLLEXPORT __declspec( dllexport )
#endif

#include "Aggregate.h"

#ifdef __cplusplus 
extern "C" { // export the C interface
#endif
DLLEXPORT public void initialize(char* irFile, char* smFile, char* ixFile, long long prevObj, long long* newObj);
DLLEXPORT public void Pay(long long object, CalculationInterface* payInterface);
DLLEXPORT public void ClearPortfolio(long long* object);
#ifdef __cplusplus 
}
#endif

class Calculation
{
public:
	Calculation();
    ~Calculation();

	void initialize(char* irFile, char* smFile, char* ixFile, Calculation* prevObj);
	void calcLoss(CalculationInterface* payInterface);
	void ClearMemory();

	int getNumOfContract() { return _nContract; }

private:
	//array of size 1, holding the number of objects of this class created
	//multiple objects will share the same data,
	//and the shared data will be cleared only when the number of objects is reduced to 0
	int* _nObjCreated;

	//thread index, is set in initialize, and will be used for debug
	int _threadIdx;
	//subevent index for this object, is set in calcLoss, and will be used for debug
	int _subeventIdx;

	//the max of nNode of all contracts in this portfolio
	int _maxNode;
//	int _maxChild;

	int  _nContract;
	int* _pNodeStartIdx;
    Contract* _pContract;
	
	int   _nAffectedContract;
	int*  _pAffectedContractIdx;
	int*  _pNumAffectedLeaf;

	void Initial();
	void buildPortfolio(char* irFile);
	void readInputFromBinFile(FILE* pFile);
	
	//for currency convert --- not used yet
	int _nCurrency;
	Losses* _exchangeRate;
	void adjustByExchangeRate();

	//for accumulating ground up losses from exposures to nodes
	Aggragator* _pAggragator;
	void CreateAggregator(char* smFile, char* ixFile);
#ifdef MULTILAVELCOVER
	void allocMemForPerRiskTreaty();
#endif

	//the sum of numbers of nodes over all contract in this portfolio
	int _nAllNode;
	//array of size _nAllNode,
	//_pNodeLoss[i].loss is the loss accumulated from input exposure losses
	//_pNodeLoss[i].idx is the index in the global node array of the whole portfolio
	Losses* _pNodeLoss;

	//for allocation
	enum AllocateType
	{
		NO_ALLOCATION,
		EXPOSURE_ALLOCATION,
		LOCATION_ALLOCATION,
		DQ_ALLOCATION
	};
	int _allocationIdx;
#ifdef WITHALLOCATION
	int _nAllLoc;
	bool*   _pIsAffectedLoc;
	double* _pLocLoss;//location recovery accumulated from exposure recovery
	double* _pDirectLoss;//the same value as _pNodeLoss.loss 
	double* _pRA;//allocated recovery for each node
	double* _pPA;//allocated payout for each node
	double* _pTmpD;//allocated deductible for each node
	double* _pTmpR;//temparary allocated deductible for each node
	double* _pExposureLoss;//exposure loss for each subevent
	int* _pExpoIdx2NodeIdx;//mapp exposure index to nodex index for each subevent
	int* _pNumExpoA;//number of input loss for each contract
	int** _pExpoIdxA;//indices of input loss for each contract
#endif

	void SetAffectedContract(int nInputs, LossesWithPerilId* inputs, int time
#ifdef PECENTAFFECTED
		, double* pNodeVIT
#endif
#ifdef FOR_QA_TEST
		, FILE* pf, char* line
#endif
		);

#ifdef OVERLAP
	int*  _pIntTmp;
	bool* _pBoolTmp;
#endif

	PerRiskResolusion* _perriskResolution;
};

#endif
