#ifndef CONTRACT_H
#define CONTRACT_H


#include "RuntimeInterface.h"
#include "utilFunc.h"
#include "Cover.h"

#ifdef OVERLAP
struct LevelInfo
{
	int _nLevel;
	int* _pLevelNodeNum;
	int** _pLevelNodeIdx;
	LevelInfo();
	LevelInfo(LevelInfo* p);
	~LevelInfo();
};
#endif

enum ContractType
{
	CONTRACT_REGULAR,
	CONTRACT_STEPPOLICY,
	CONTRACT_PERRISKTREATY
};

class Contract
{
public:
	Contract();
	~Contract();

	void readInput(FILE* pFile, int sizeI
#ifdef PERRISK
		, int& globalIdx, bool* pIsPerriskNode
#endif
		);

#ifdef FOR_QA_TEST
	void setglobalNodeIndex(int nodeStartIdx);
	void writeNodeLoss(FILE* pf, char* line, double* pRA);
#endif

	void adjustByExchangeRate(int nCurrency, Losses* exchangeRate);

	void ClearMemory(int nObjCreated);

	void copyDataForMultiThread(Contract& contract);
	int getNumNode() { return _nNode; }
//	int getCurrencyIdx() { return _currencyIdx; }
	
#ifdef MULTILAVELCOVER
	void allocMemForPerRiskTreaty(int nExposure);
#endif

	void setAffectedNode(int nAffectedLeaf, int nodeStartIdx, Losses* pLoss
#ifdef WITHALLOCATION
	, double* pNodeLoss
#endif
#ifdef OVERLAP
		, int* pIntTmp, bool* pBoolTmp, bool* pIsSet
#endif
#ifdef PECENTAFFECTED
			, double* pNodeVIT, double* pNodeTotalTIV
#endif
			);

#ifdef PECENTAFFECTED
	void convertAffectedPecent2Amount(double* pNodeTotalTIV);
#endif

//	void calcLoss(int nAffectedLeaf, int nodeStartIdx, int lossStartIdx, Losses* pLoss, double& recovery);
//	void calcLoss(int nAffectedLeaf, int nodeStartIdx, int lossStartIdx, Losses* pLoss, double& recovery,
	//	int nExposure, int* pExpoIdx, int* pExpoIdx2NodeIdx, double* pExposureLoss, double* pPayoutA,
		//double* pRA, double* pPA, double* pDirectLoss, double* pTmpD, double* pTmpR);
	void calcLoss(int nAffectedLeaf, int nodeStartIdx, Losses* pLoss, double& recovery
#ifdef WITHALLOCATION
		, int nExposure, int* pExpoIdx, int* pExpoIdx2NodeIdx, double* pExposureLoss, double* pPayoutA,
		double* pRA, double* pPA, double* pDirectLoss, double* pTmpD, double* pTmpR
#endif
#ifdef OVERLAP
		, int* pIntTmp, bool* pBoolTmp, bool* pIsSet
#endif
#ifdef PECENTAFFECTED
			, double* pNodeVIT, double* pNodeTotalTIV
#endif
#ifdef MULTILAVELCOVER
#ifndef WITHALLOCATION
			, int nExposure, double* pExposureLoss
#endif
#endif
	);
	
#ifdef WITHALLOCATION
	void allocationGR(int nodeStartIdx, int nExposure, int* pExpoIdx, int* pExpoIdx2NodeIdx, double* pExposureLoss,
		double* pPayoutA, double* pRA, double* pPA, double* pDirectLoss, double* pTmpD, double* pTmpR);
#endif

	void allocatioForDQ(int nExposure, int* pExpoIdx, int* pExpoIdx2NodeIdx, double* pExposureLoss, double* pPayoutA,
		int nodeStartIdx, double* pDirectLoss, double* pRA);

	bool isActive(int time);

#ifdef PERRISK
	//add new nodes to the contract for perrisk nodes
	int addNewNode(int nodeStartIdx, int nAffectedNode, int* pNodeIdx, int* pNumExpo, int* pExpoStartIdx, ThreeInt* pInput);
#endif

	int convertCoveredPecentToAmount(int nodeStartIdx, int nAllExposure, TwoInt* pExpoIdxNodeIdx, double* pValue, double* pCoverValue
#ifdef OVERLAP
				, bool* pIsSet
#endif
				);

private:
#ifdef OVERLAP
	LevelInfo* _pLevelInfo;
	int* _pNodeLevel;
#endif

#ifdef MULTILAVELCOVER
	int _contractType;
	int _nRootCover;
	int* _pRootCoverIdx;
#ifdef WITHALLOCATION
	//_pSetsIndex[i] is the cover index which realize the max of min function for cover i
	int* _pSetsIndex;
#endif
#endif

	char* _contractName;

	int   _nCover;
	Cover* _pCover;

	int   _nNode;
	Node* _pNode;

	int  _nRoot;
	int* _pRootIdx;
//	double* _pNodeLoss;

	int _currencyId;

	int _startTime;
	int _endTime;

	void calcContractRecovery(double& recovery);

	void resetRootNode();

	void reInitial(
#ifdef WITHALLOCATION
	double* pDirectLoss
#endif
	);

#ifdef WITHALLOCATION
	void payoutAloc(double* pRA, double* pPA, double* pDirectRA, double* pDirectPA);
#endif
};

//void setContracts(Contract* pContract);

#endif
