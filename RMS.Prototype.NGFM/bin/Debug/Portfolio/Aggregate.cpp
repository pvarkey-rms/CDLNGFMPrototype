#include "Aggregate.h"
#include <iostream>
#include <fstream>

using namespace std;

PerRiskData::PerRiskData() :
_nExposure(-1),
//_pExposureID(0),
_pExposureLoss(0)
{
}

PerRiskData::~PerRiskData()
{
//	DELETEARRAY(_pExposureID);
	DELETEARRAY(_pExposureLoss);
}

void PerRiskData::addItem(int index, double loss)
{
//	_pExposureID[_nExposure] = index;
	_pExposureLoss[_nExposure] = loss;
	_nExposure++;
}
//////////////////////////////////////////////////////////////////////////////////////////////
//Stemper
Aggragator::Aggragator() :
//PERIL_SIZE(11),
#ifdef MULTILAVELCOVER
_hasPerRiskTreaty(false),
_pPerRiskData(0),
#endif
_pNodeIdx(0),
_pExposureV(0),
//_pContractIdx(0),
_pPerilId(0),
_maxExposureId(0),
_nNode(0),
_pExpoIdx2LocIdx(0),
_nAllLoc(0)
{
}

Aggragator::~Aggragator()
{
	DELETEARRAY(_pNodeIdx);
	DELETEARRAY(_pExposureV);
	DELETEARRAY(_pPerilId);
//	DELETEARRAY(_pContractIdx);
	DELETEARRAY(_pExpoIdx2LocIdx);
#ifdef MULTILAVELCOVER
	DELETEARRAY(_pPerRiskData);
#endif
}

void Aggragator::readInputData(int nContract, int* pNodeStartIdx, char* smFile, char* ixFile, PerRiskResolusion* perriskResolution)
{
	readSmFile(nContract, pNodeStartIdx, smFile, perriskResolution);
	
#ifdef WITHALLOCATION
	readIxFile(ixFile);
#endif

#ifdef PERRISK
#ifndef WITHALLOCATION
	readIxFile(ixFile);
#endif
	adjustPortfolioForResolution(perriskResolution);
#ifndef WITHALLOCATION
	DELETEARRAY(_pExpoIdx2LocIdx);
#endif
#endif
}

void Aggragator::readSmFile(int nContract, int* pNodeStartIdx, char* smFile, PerRiskResolusion* perriskResolution)
{
	ifstream inputStream;
	inputStream.open(smFile, ios::in|ios::binary);
	if ( !inputStream.is_open() )
	{
		printf("Could not open file (%s) to read", smFile);
		return;
	}

	int* pNumPeril = new int[nContract];
	int** pPerilId = new int*[nContract];
	int** pNumExposure = new int*[nContract];
	int*** ppIdxeIdx2nIdx = new int**[nContract];
	double*** pExposureValue = new double**[nContract];
	
	int  contractIdx, nPeril, peril_id, nExposure;
	int i, j, k, size, maxExposureID, maxNodeId = 0;
	int maxExposureSize = 0;
	Record* pRecord;
	for (i=0; i<nContract; i++)
	{
		pNumPeril[i] = 0;
		pPerilId[i] = 0;
		pNumExposure[i] = 0;
		ppIdxeIdx2nIdx[i] = 0;
		pExposureValue[i] = 0;
	}

	bool hasPerriskNode = false;
	if ( perriskResolution )
	{
		hasPerriskNode = true;
	}
	
#ifdef MULTILAVELCOVER
	_pPerRiskData = new PerRiskData[nContract];
#endif

	while ( !inputStream.eof()) 
    {
		istream& status = inputStream.read((char*)&contractIdx, sizeof(int));
		if ( status.fail() ) 
			break;
		if ( contractIdx >= nContract )
		{
			//pass those data which will not be used
			inputStream.read((char*)&nPeril, sizeof(int));
			if ( nPeril > 0 )
			{
				for (j=0; j<nPeril; j++)
				{
					inputStream.read((char*)&peril_id, sizeof(int));
					inputStream.read((char*)&nExposure, sizeof(int));
					if ( nExposure > 0 )
					{
						try {
							pRecord = new Record[nExposure];
						}
						catch (...) {
							printf("Stamper failed to allocate buffer of requested size %d", nExposure);
							return;
						}
						inputStream.read((char*)pRecord, sizeof(Record)*nExposure);
						delete [] pRecord;
					}
				}
			}
		}
		else
		{
			inputStream.read((char*)&nPeril, sizeof(int));
			if ( nPeril > 0 )
			{
				pNumPeril[contractIdx] = nPeril;
				pPerilId[contractIdx] = new int[nPeril];
				pNumExposure[contractIdx] = new int[nPeril];
				ppIdxeIdx2nIdx[contractIdx] = new int*[nPeril];
				pExposureValue[contractIdx] = new double*[nPeril];

				for (j=0; j<nPeril; j++)
				{
					inputStream.read((char*)&peril_id, sizeof(int));
					inputStream.read((char*)&nExposure, sizeof(int));
					
					pPerilId[contractIdx][j] = peril_id;
					if ( nExposure > 0 )
					{
						try {
							pRecord = new Record[nExposure];
						}
						catch (...) {
							printf("Stamper failed to allocate buffer of requested size %d", nExposure);
							return;
						}

						maxExposureID = 0;
						inputStream.read((char*)pRecord, sizeof(Record)*nExposure);

						if ( peril_id > 0 && peril_id < PERIL_SIZE )
						{
							for (i = 0; i < nExposure; i++)
							{
								if (maxExposureID < pRecord[i]._ExposureIdx)
									maxExposureID = pRecord[i]._ExposureIdx;
								if (maxNodeId < pRecord[i]._nodeIdx+pNodeStartIdx[contractIdx])
									maxNodeId = pRecord[i]._nodeIdx+pNodeStartIdx[contractIdx];
							}
							size = maxExposureID + 1;
							if ( maxExposureSize < size )
								maxExposureSize = size;
							int* indexInOutArray = new int[size];
							double* exposureValue = new double[size];

							for (i=0; i<size; i++)
							{
								indexInOutArray[i] = -1;
							}
							for (i = 0; i < nExposure; i++)
							{
								indexInOutArray[pRecord[i]._ExposureIdx] = pRecord[i]._nodeIdx;
								exposureValue[pRecord[i]._ExposureIdx] = pRecord[i]._exposureValue;
							}
							
							pNumExposure[contractIdx][j] = size;
							ppIdxeIdx2nIdx[contractIdx][j] = indexInOutArray;
							pExposureValue[contractIdx][j] = exposureValue;
						}
#ifdef MULTILAVELCOVER
						else if ( peril_id == -1 )
						{//per risk treaty
							if ( nPeril > 1 )
							{
								printf("Per risk treaty has more than one peril ID (%d)", nPeril);
								return;
							}
							_hasPerRiskTreaty = true;
							_pPerRiskData[contractIdx]._nExposure = nExposure;
//							_pPerRiskData[contractIdx]._pExposureID = new int[nExposure];
#ifndef WITHALLOCATION
							_pPerRiskData[contractIdx]._pExposureLoss = new double[nExposure];
#endif
							for (i = 0; i < nExposure; i++)
							{
								if (maxExposureID < pRecord[i]._ExposureIdx)
									maxExposureID = pRecord[i]._ExposureIdx;
							}
							size = maxExposureID + 1;
							if ( maxExposureSize < size )
								maxExposureSize = size;
							int* indexInOutArray = new int[size];
							double* exposureValue = new double[size];

							for (i=0; i<size; i++)
							{
								indexInOutArray[i] = -1;
							}
							for (i = 0; i < nExposure; i++)
							{
								indexInOutArray[pRecord[i]._ExposureIdx] = 0;
								exposureValue[pRecord[i]._ExposureIdx] = pRecord[i]._exposureValue;
							}
							
							pNumExposure[contractIdx][j] = size;
							ppIdxeIdx2nIdx[contractIdx][j] = indexInOutArray;
							pExposureValue[contractIdx][j] = exposureValue;
						}
#endif
						else
						{
							//reset
							pNumPeril[contractIdx]--;
							if ( pNumPeril[contractIdx] == 0 )
							{
								delete [] pPerilId[contractIdx];
								delete [] pNumExposure[contractIdx];
								delete [] ppIdxeIdx2nIdx[contractIdx];
								delete [] pExposureValue[contractIdx];
							}
						}
						delete [] pRecord;
					}
					else
					{
						pNumExposure[contractIdx][j] = 0;
					}
				}
			}
		}
    }
	
    inputStream.close();
	
	//set _nNode and _maxExposureId
	_nNode = maxNodeId + 1;
	_maxExposureId = 0;
	for (i=0; i<nContract; i++)
	{
		for (j=0; j<pNumPeril[i]; j++)
		{
			if ( _maxExposureId < pNumExposure[i][j] )
				_maxExposureId = pNumExposure[i][j];
		}
	}
	//
	_pNodeIdx = new int[_maxExposureId];
	_pExposureV = new double[_maxExposureId];
	_pPerilId = new int[_maxExposureId];
//	_pContractIdx = new int[_maxExposureId];
	for (i=0; i<_maxExposureId; i++)
	{
//		_pContractIdx[i] = -1;
		_pPerilId[i] = _pNodeIdx[i] = -1;
		_pExposureV[i] = 0;
	}
	for (i=0; i<nContract; i++)
	{
		if ( pNumPeril[i] > 0 )
		{
			for (j=0; j<pNumPeril[i]; j++)
			{
				if ( pNumExposure[i][j] > 0 )
				{
					for (k=0; k<pNumExposure[i][j]; k++)
					{
						if ( ppIdxeIdx2nIdx[i][j][k] > -1 )
						{
							_pNodeIdx[k] = ppIdxeIdx2nIdx[i][j][k] + pNodeStartIdx[i];
							_pExposureV[k] = pExposureValue[i][j][k];
							_pPerilId[k] = pPerilId[i][j];
	//						_pContractIdx[k] = i;
						}
					}
					delete [] ppIdxeIdx2nIdx[i][j];
					delete [] pExposureValue[i][j];
				}
			}
			delete [] pPerilId[i];
			delete [] pNumExposure[i];
			delete [] ppIdxeIdx2nIdx[i];
			delete [] pExposureValue[i];
		}
	}
	delete [] pNumPeril;
	delete [] pPerilId;
	delete [] pNumExposure;
	delete [] ppIdxeIdx2nIdx;
	delete [] pExposureValue;
}

void Aggragator::readIxFile(char* ixFile)
{
	ifstream inputStream;
	inputStream.open(ixFile, ios::in|ios::binary);
	if ( !inputStream.is_open() )
	{
		printf("Could not open file (%s) to read", ixFile);
		return;
	}

	int i, nExposure = 0;
	inputStream.read((char*)&nExposure, sizeof(int));
	if ( nExposure == 0 )
		return;
    int* pExpoIndex = new int[nExposure];
	int* pLocIdx = new int[nExposure];
	int maxExposureID = 0;
	double dummyD = 0;
	int dummyI = 0;
	_nAllLoc = 0;
	for (i=0; i<nExposure; i++)
	{
		inputStream.read((char*)&(pExpoIndex[i]), sizeof(int));
		inputStream.read((char*)&dummyD, sizeof(double));
		inputStream.read((char*)&dummyI, sizeof(int));
		inputStream.read((char*)&(pLocIdx[i]), sizeof(int));
		if ( maxExposureID < pExpoIndex[i] )
			maxExposureID = pExpoIndex[i];
		if ( _nAllLoc < pLocIdx[i] )
			_nAllLoc = pLocIdx[i];
    }
	_nAllLoc++;
    inputStream.close();
	_pExpoIdx2LocIdx = new int[maxExposureID+1];
	for (i=0; i<=maxExposureID; i++)
	{
		_pExpoIdx2LocIdx[i] = -1;
	}
	for (i=0; i<nExposure; i++)
	{
		_pExpoIdx2LocIdx[pExpoIndex[i]] = pLocIdx[i];
	}
	delete [] pExpoIndex;
	delete [] pLocIdx;
}

int Aggragator::setValidFlag(int size, bool* pIsValidExpo, LossesWithPerilId* inputs)
{
	int i, idx, nodeIdx, nInvalidExpo = 0;
	for (i=0; i<size; i++)
	{
		idx = inputs[i].id;
		if ( idx>=_maxExposureId || _pExposureV[idx]==0 )
		{
			pIsValidExpo[i] = false;
			nInvalidExpo++;
		}
		else
		{
			nodeIdx = _pNodeIdx[idx];
			if ( nodeIdx > -1 )
			{
				if ( inputs[i].perilId != _pPerilId[idx] )
				{
					pIsValidExpo[i] = false;
					nInvalidExpo++;
				}
				else
					pIsValidExpo[i] = true;
			}
			else
			{
				pIsValidExpo[i] = false;
				nInvalidExpo++;
			}
		}
	}
	return nInvalidExpo;
}

//assume the inputs is in the order of contract index, for example :
//let ii is the contract index of the node corresponding to input exposure loss inputs[i]
//and jj is the contract index of the node corresponding to input exposure loss inputs[j]
//then ii <= jj if i < j
//otherwise need sort pNodeLoss with pNodeLoss::idx and need more work to handle the relation between inputs and pNodeLoss for allocation
void Aggragator::run(int nInputs, LossesWithPerilId* inputs, Losses* pNodeLoss, int nContract, int* pNodeStartIdx
#ifdef WITHALLOCATION
	, int* pNumExpoA, int** pExpoIdxA , int* pExpoIdx2ContractIdx
#endif
#ifdef PECENTAFFECTED
		, double* pNodeVIT
#endif
	)
{
	int i, idx, nodeIdx, contractIdx;
#ifdef MULTILAVELCOVER
	if ( _hasPerRiskTreaty )
	{
		for (i=0; i<nContract; i++)
		{
			if ( _pPerRiskData[i]._nExposure > -1 )
			{
				_pPerRiskData[i]._nExposure = 0;
			}
		}
	}
#endif

#ifdef WITHALLOCATION
	for (i = 0; i < nContract; i++)
		pNumExpoA[i] = 0;
//pExpoIdx2NodeIdx used as a temprary array to hold contract index for each input exposure loss in this function
	for (i = 0; i < nInputs; i++)
	{
		idx = inputs[i].id;
		nodeIdx = _pNodeIdx[idx];
		if (nodeIdx != -1)
		{
			contractIdx = findIndexByBinary(nodeIdx, nContract, pNodeStartIdx);
			pNodeLoss[nodeIdx].loss += inputs[i].lossRatio * _pExposureV[idx];

#ifdef PECENTAFFECTED
			pNodeVIT[nodeIdx] += _pExposureV[idx];
#endif

			pNumExpoA[contractIdx]++;
			pExpoIdx2ContractIdx[i] = contractIdx;
		}
	}

	for (i = 0; i < nContract; i++)
	{
		if ( pNumExpoA[i] > 0 )
		{
			pExpoIdxA[i] = new int[pNumExpoA[i]];
			pNumExpoA[i] = 0;
		}
	}
	for (i = 0; i < nInputs; i++)
	{
		pExpoIdxA[pExpoIdx2ContractIdx[i]][pNumExpoA[pExpoIdx2ContractIdx[i]]++] = i;
	}
#else
	for (i = 0; i < nInputs; i++)
	{
		idx = inputs[i].id;
		nodeIdx = _pNodeIdx[idx];
		if (nodeIdx != -1)
		{
			pNodeLoss[nodeIdx].loss += inputs[i].lossRatio * _pExposureV[idx];

#ifdef MULTILAVELCOVER
			contractIdx = findIndexByBinary(nodeIdx, nContract, pNodeStartIdx);
			if ( _hasPerRiskTreaty && _pPerRiskData[contractIdx]._nExposure > -1 )
			{
				_pPerRiskData[contractIdx].addItem(idx, inputs[i].lossRatio * _pExposureV[idx]);
			}
#endif								

#ifdef PECENTAFFECTED
			pNodeVIT[nodeIdx] += _pExposureV[idx];
#endif
		}
	}
#endif
}

void Aggragator::getExposureInfo(int nInputs, LossesWithPerilId* inputs, double* pExposureLoss, int* pExpoIdx2NodeIdx)
{
	int i, idx, nodeIdx;
	for (i = 0; i < nInputs; i++)
	{
		idx = inputs[i].id;
		nodeIdx = _pNodeIdx[idx];
		pExposureLoss[i] = inputs[i].lossRatio * _pExposureV[idx];
		pExpoIdx2NodeIdx[i] = nodeIdx;
	}
}

#ifdef PERRISK
void Aggragator::adjustPortfolioForResolution(PerRiskResolusion* perriskResolution)
{
	int i;
	
	ThreeInt* pInput = new ThreeInt[_maxExposureId];
	for (i=0; i<_maxExposureId; i++)
	{
		pInput[i].i1 = i;
		pInput[i].i2 = _pNodeIdx[i];
		pInput[i].i3 = _pExpoIdx2LocIdx[i];
	}
	sort3IntBy2(_maxExposureId, pInput);

	int nContract = perriskResolution->_nContract;

	//pNumNodeC[i] is the number of perrisk nodes in contract i
	int* pNumNodeC = new int[nContract];
	//pNodeIdxC[i][j] is the nodeIdx of the j-th perrisk node of contract i in the array of all nodes
	int** pNodeIdxC = new int*[nContract];
	//pNumExpoN[i][j] is the number of exposures under the perrisk node j of contract i
	int** pNumExpoN = new int*[nContract];
	//pExpoStartIdxN[i][j] is the start index of contract i node pNumExpoN[i][j] in the array pInput
	int** pExpoStartIdxN = new int*[nContract];
	for (i=0; i<nContract; i++)
		pNumNodeC[i] = 0;

	//find all exposures under each perrisk node
	findAllExpoForAllPerriskNode(perriskResolution, pInput, pNumNodeC, pNodeIdxC, pNumExpoN, pExpoStartIdxN);
	
	//find all location nodes under each perrisk node and add those nodes to the tree
	modifyTreeForPerriskNode(perriskResolution, pInput, pNumNodeC, pNodeIdxC, pNumExpoN, pExpoStartIdxN);

	delete [] pInput;
	delete [] pNumNodeC;
	delete [] pNodeIdxC;
	delete [] pNumExpoN;
	delete [] pExpoStartIdxN;
}

void Aggragator::modifyTreeForPerriskNode(PerRiskResolusion* perriskResolution, ThreeInt* pInput,
	int* pNumNodeC, int** pNodeIdxC, int** pNumExpoN, int** pExpoStartIdxN)
{
	int i, j, k, nNodeAdded, expoIdx;
	
	int nContract = perriskResolution->_nContract;
	int* pNodeStartIdx = perriskResolution->_pNodeStartIdx;
	Contract* pContract = perriskResolution->_pContract;

	int nAllAddedNode = 0;
	for (i=0; i<nContract; i++)
	{
		if ( pNumNodeC[i] > 0 )
		{
			//find location node under all perrisk nodes in this contract, and add those nodes to the node tree of this contract
			nNodeAdded = pContract[i].addNewNode(pNodeStartIdx[i], pNumNodeC[i], pNodeIdxC[i], pNumExpoN[i], pExpoStartIdxN[i], pInput);
			nAllAddedNode += nNodeAdded;
			k = i + 1;
			//modify node index for exposures link to the nodes in all contract[t] with t > i
			if ( k < nContract )
			{
				for (j=0; j<_maxExposureId; j++)
				{
					if ( _pNodeIdx[j] >=  pNodeStartIdx[k] )
						_pNodeIdx[j] += nNodeAdded;
				}
			}
			//modify node index for exposures link to the nodes added contract[i]
			for (j=0; j<pNumNodeC[i]; j++)
			{
				expoIdx = pExpoStartIdxN[i][j];
				for (k=0; k<pNumExpoN[i][j]; k++,expoIdx++)
				{
					_pNodeIdx[pInput[expoIdx].i1] = pInput[expoIdx].i2 + pNodeStartIdx[i];
				}
			}
			//modify the index for all perrisk nodes in contract k with k > i
			for (k=i+1; k<nContract; k++)
			{
				if ( pNumNodeC[k] > 0 )
				{
					for (j=0; j<pNumNodeC[k]; j++)
						pNodeIdxC[k][j] += nNodeAdded;
				}
			}
			//modify the start node index for all contract k with k > i
			for (k=i+1; k<=nContract; k++)
				pNodeStartIdx[k] += nNodeAdded;
			delete [] pNodeIdxC[i];
			delete [] pNumExpoN[i];
			delete [] pExpoStartIdxN[i];
		}
	}
	_nNode += nAllAddedNode;
}

void Aggragator::findAllExpoForAllPerriskNode(PerRiskResolusion* perriskResolution, ThreeInt* pInput,
	int* pNumNodeC, int** pNodeIdxC, int** pNumExpoN, int** pExpoStartIdxN)
{
	int nContract = perriskResolution->_nContract;
	int* pNodeStartIdx = perriskResolution->_pNodeStartIdx;

	int maxNode = perriskResolution->_maxNode;
	int nOrigNode = perriskResolution->_nOrigNode;
	bool* pIsPerriskNode = perriskResolution->_pIsPerriskNode;

	int i, contractIdx = 1;
	int currContractIdx = 0;
	int nNode = 0;
	int expoIdx = 0;
	int* pTmpNodeIdx = new int[nOrigNode];
	int* pTmpNumExpoN = new int[maxNode];
	int* pTmpExpoStartIdxN = new int[maxNode];

	for (i=0; i<nOrigNode; i++)
	{
		if ( pIsPerriskNode[i] )
		{
			while (pNodeStartIdx[contractIdx] <= i)
			{
				saveContractForSolution(contractIdx, nNode, pNumNodeC, pNodeIdxC, pNumExpoN, pExpoStartIdxN, pTmpNodeIdx, pTmpNumExpoN, pTmpExpoStartIdxN);
				nNode = 0;
				contractIdx++;
			}
			if ( currContractIdx != contractIdx-1 )
			{
//				saveContractForSolution(contractIdx, nNode, pNumNodeC, pNodeIdxC, pNumExpoN, pExpoStartIdxN, pTmpNodeIdx, pTmpNumExpoN, pTmpExpoStartIdxN);
				pTmpNodeIdx[0] = i;
				nNode = 1;
				pTmpNumExpoN[0] = getAllExposureUnderTheNode(expoIdx, pInput, i, pTmpExpoStartIdxN[0]);
				if ( pTmpNumExpoN[0] == 0 )
					nNode = 0;
			}
			else
			{
				pTmpNodeIdx[nNode] = i;
				pTmpNumExpoN[nNode] = getAllExposureUnderTheNode(expoIdx, pInput, i, pTmpExpoStartIdxN[nNode]);
				if ( pTmpNumExpoN[nNode] > 0 )
					nNode++;
			}
		}
	}
	saveContractForSolution(contractIdx, nNode, pNumNodeC, pNodeIdxC, pNumExpoN, pExpoStartIdxN, pTmpNodeIdx, pTmpNumExpoN, pTmpExpoStartIdxN);

	delete [] pTmpNodeIdx;
	delete [] pTmpNumExpoN;
	delete [] pTmpExpoStartIdxN;
}

void Aggragator::saveContractForSolution(int contractIdx, int nNode, int* pNumNodeC, int** pNodeIdxC, int** pNumExpoN, int** pExpoStartIdxN,
	int* pTmpNodeIdx, int* pTmpNumExpoN, int* pTmpExpoStartIdxN)
{
	int currContractIdx = contractIdx - 1;
	pNumNodeC[currContractIdx] = nNode;
	if ( nNode > 0 )
	{
		pNodeIdxC[currContractIdx] = new int[nNode];
		pNumExpoN[currContractIdx] = new int[nNode];
		pExpoStartIdxN[currContractIdx] = new int[nNode];
		for (int j=0; j<nNode; j++)
		{
			pNodeIdxC[currContractIdx][j] = pTmpNodeIdx[j];
			pNumExpoN[currContractIdx][j] = pTmpNumExpoN[j];
			pExpoStartIdxN[currContractIdx][j] = pTmpExpoStartIdxN[j];
		}
	}
}

int Aggragator::getAllExposureUnderTheNode(int& expoIdx, ThreeInt* pInput, int nodeIdx, int& expoStartIdx)
{
	int nExposure = 0;
	int idx = expoIdx;
	while (idx<_maxExposureId && pInput[idx].i2<nodeIdx)
		idx++;
	if ( idx == _maxExposureId )
	{
		//input error
		printf("perrisk node(%d) doex not have any exposure belongs to", nodeIdx);
		expoIdx = idx;
		return 0;
	}
	if ( pInput[idx].i2 == nodeIdx )
	{
		expoStartIdx = idx;
		nExposure = 1;
		idx++;
		while (idx<_maxExposureId && pInput[idx].i2==nodeIdx)
		{
			idx++;
			nExposure++;
		}
	}
	expoIdx = idx;
	return nExposure;
}
#endif
/////////////////////////////////////////////////////////////////////////////////////////
//Resolution
PerRiskResolusion::PerRiskResolusion() :
_maxNode(0),
_nOrigNode(0),
_pIsPerriskNode(0),
_nContract(0),
_pNodeStartIdx(0),
_pContract(0)
{
}

PerRiskResolusion::~PerRiskResolusion()
{
	DELETEARRAY(_pIsPerriskNode);
}