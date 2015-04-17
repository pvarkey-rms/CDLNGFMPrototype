#ifndef AGGREGARE_H
#define AGGREGARE_H

#include "Contract.h"

struct PerRiskResolusion
{
	//the max of nNode of all contracts in this portfolio
	int _maxNode;
	//number of nodes of the portfolio before resolution
	int _nOrigNode;
	//size = _nOrigNode
	//_pIsPerriskNode[i] indicates whether the node i is for resolution or not
	bool* _pIsPerriskNode;
	
	//number of contracts in the portfolio
	int _nContract;
	int* _pNodeStartIdx;
	Contract* _pContract;

	PerRiskResolusion();
	~PerRiskResolusion();
};

struct PerRiskData
{
	int _nExposure;
//	int* _pExposureID;
	double* _pExposureLoss;
	PerRiskData();
	~PerRiskData();
	void addItem(int index, double loss);
};

class Aggragator
{
public:
	Aggragator();
	~Aggragator();

	void readInputData(int nContract, int* pNodeStartIdx, char* smFile, char* ixFile, PerRiskResolusion* perriskResolution);
	void run(int nInputs, LossesWithPerilId* inputs, Losses* pNodeLoss, int nContract, int* pNodeStartIdx
#ifdef WITHALLOCATION
		, int* pNumExpoA, int** pExpoIdxA , int* pExpoIdx2ContractIdx
#endif
#ifdef PECENTAFFECTED
		, double* pNodeVIT
#endif
		);
	void getExposureInfo(int nInputs, LossesWithPerilId* inputs, double* pExposureLoss, int* pExpoIdx2NodeIdx);
	int getNumNode() { return _nNode; }
//	int getNodeIdx(LossesWithPerilId loss) { return _ppIdxeIdx2nIdx[loss.perilId][loss.exposureId]; }

	int* getExpoIdx2LocIdx() { return _pExpoIdx2LocIdx; }
	int getNumAllLocation() { return _nAllLoc; }

	int setValidFlag(int size, bool* pIsValidExpo, LossesWithPerilId* inputs);

	int getNumExposure() { return _maxExposureId; }
	int* getExpo2NodeIdx() { return _pNodeIdx; }
	double* getExposureValue() { return _pExposureV; }

#ifdef MULTILAVELCOVER
	bool _hasPerRiskTreaty;
	PerRiskData* getPerRiskTreatyData() { return _pPerRiskData; }
	PerRiskData* _pPerRiskData;
#endif

private:
	int _maxExposureId;
//	int* _pContractIdx;
	int* _pPerilId;
	int* _pNodeIdx;
	double* _pExposureV;

//	int PERIL_SIZE;
	int _nNode;

	int  _nAllLoc;
	void readSmFile(int nContract, int* pNodeStartIdx, char* smFile, PerRiskResolusion* perriskResolution);

	//for aggregating exposures to locations
	int* _pExpoIdx2LocIdx;
	void readIxFile(char* ixFile);

#ifdef PERRISK
	void adjustPortfolioForResolution(PerRiskResolusion* perriskResolution);
	int getAllExposureUnderTheNode(int& expoIdx, ThreeInt* pInput, int nodeIdx, int& expoStartIdx);
	void saveContractForSolution(int contractIdx, int nNode, int* pNumNodeC, int** pNodeIdxC, int** pNumExpoN, int** pExpoStartIdxN,
		int* pTmpNodeIdx, int* pTmpNumExpoN, int* pTmpExpoStartIdxN);
	void findAllExpoForAllPerriskNode(PerRiskResolusion* perriskResolution, ThreeInt* pInput,
		int* pNumNodeC, int** pNodeIdxC, int** pNumExpoN, int** pExpoStartIdxN);
	void modifyTreeForPerriskNode(PerRiskResolusion* perriskResolution, ThreeInt* pInput,
		int* pNumNodeC, int** pNodeIdxC, int** pNumExpoN, int** pExpoStartIdxN);
#endif
};

#endif
