#include "Contract.h"
#include "stdio.h"
#include <string.h>

#ifdef OVERLAP
////////////////////////////////////////////////////////////////////////////////////////
//LevelInfo
LevelInfo::LevelInfo() :
_nLevel(0),
_pLevelNodeNum(0),
_pLevelNodeIdx(0)
{
}

LevelInfo::LevelInfo(LevelInfo* p) :
_nLevel(p->_nLevel)
{
	_pLevelNodeNum = new int[_nLevel];
	_pLevelNodeIdx = new int*[_nLevel];
	int i, j;
	for (i=0; i<_nLevel; i++)
	{
		_pLevelNodeNum[i] = p->_pLevelNodeNum[i];
		_pLevelNodeIdx[i] = new int[_pLevelNodeNum[i]];
		for (j=0; j<_pLevelNodeNum[i]; j++)
		{
			_pLevelNodeIdx[i][j] = p->_pLevelNodeIdx[i][j];
		}
	}
}

LevelInfo::~LevelInfo()
{
	if ( _nLevel > 0 )
	{
		for (int i=0; i<_nLevel; i++)
			delete [] _pLevelNodeIdx[i];
		delete [] _pLevelNodeIdx;
		delete [] _pLevelNodeNum;
		_nLevel = 0;
	}
}
#endif
////////////////////////////////////////////////////////////////
//Contract
Contract::Contract() :
_contractName(0),
#ifdef OVERLAP
_pLevelInfo(0),
_pNodeLevel(0),
#endif
#ifdef MULTILAVELCOVER
_nRootCover(0),
_pRootCoverIdx(0),
#ifdef WITHALLOCATION
_pSetsIndex(0),
#endif
#endif
_nCover(0),
_pCover(0),
_nRoot(0),
_pRootIdx(0),
_nNode(0),
_pNode(0),
_currencyId(0),
_startTime(0),
_endTime(1000000000)
{
}

Contract::~Contract()
{
}

void Contract::ClearMemory(int nObjCreated)
{
	int i;
	for (i=0; i<_nNode; i++)
	{
		_pNode[i].ClearMemory(nObjCreated);
	}
	for (i=0; i<_nCover; i++)
	{
		_pCover[i].ClearMemory(nObjCreated);
	}
	DELETEARRAY(_pCover);
	DELETEARRAY(_pNode);

	if ( nObjCreated == 0 )
	{
		DELETEARRAY(_contractName);
		DELETEARRAY(_pRootIdx);
#ifdef OVERLAP
		DELETEOBJECT(_pLevelInfo);
		DELETEARRAY(_pNodeLevel);
#endif
#ifdef MULTILAVELCOVER
		if ( _nRootCover < _nCover )
		{
			DELETEARRAY(_pRootCoverIdx);
			_nRootCover = _nCover;
#ifdef WITHALLOCATION
			DELETEARRAY(_pSetsIndex);
#endif
		}
#endif
	}
}

void Contract::copyDataForMultiThread(Contract& contract)
{
	_contractName = contract._contractName;
	_pRootIdx = contract._pRootIdx;
		
#ifdef MULTILAVELCOVER
	_nRootCover = contract._nRootCover;
	_pRootCoverIdx = contract._pRootCoverIdx;
	_contractType = contract._contractType;
#ifdef WITHALLOCATION
	contract._pSetsIndex = new int[_nCover];
#endif
#endif

	int i;
#ifdef OVERLAP
	_pLevelInfo = contract._pLevelInfo;
	_pNodeLevel = contract._pNodeLevel;
#endif
	_nCover = contract._nCover;
	_pCover = new Cover[_nCover];
	for (i=0; i<_nCover; i++)
	{
		_pCover[i].linkData(contract._pCover[i]);
	}

	_nNode = contract._nNode;
	_pNode = new Node[_nNode];
	for (i=0; i<_nNode; i++)
	{
#ifdef IMPORTFUNCTION
		if ( contract._pNode[i]._pCDLFuncData )
		{
			_pNode[i]._pCDLFuncData = new CDLFuncData();
			_pNode[i]._pCDLFuncData->_relatedNodeIdx = contract._pNode[i]._pCDLFuncData->_relatedNodeIdx;
			_pNode[i]._pCDLFuncData->_threshold = contract._pNode[i]._pCDLFuncData->_threshold;
			_pNode[i]._pCDLFuncData->_calculated = false;
			if ( _pNode[i]._pCDLFuncData->_relatedNodeIdx != -1 )
			{
				_pNode[i]._pCDLFuncData->_relatedNode = &(_pNode[_pNode[i]._pCDLFuncData->_relatedNodeIdx]);
			}
		}
#endif
		_pNode[i].copyForMultiThread(contract._pNode[i], _pNode);
	}

	_nRoot = contract._nRoot;

	//set function pointer for each cover
	for (i=0; i<_nCover; i++)
	{
		_pCover[i].setFuncP();
	}
}

#ifdef PERRISK
static bool readPerriskFlag(FILE* pFile, int sizeI)
{
	int k;
	fread(&k, sizeI, 1, pFile);
	if ( k == 1 )
		return true;
	return false;
}
#endif

void Contract::readInput(FILE* pFile, int sizeI
#ifdef PERRISK
		, int& globalIdx, bool* pIsPerriskNode
#endif
	)
{
	int j, k, nChild, key, valueI;
	int* pChildIdx;
	bool done = false;

	//read the number of charecters of the contract name
	fread(&valueI, sizeI, 1, pFile);
	_contractName = new char[valueI+1];
	//read the contract name
	fread(_contractName, 1, valueI, pFile);
	_contractName[valueI] = '\0';
	//read nCover
	fread(&_nCover, sizeI, 1, pFile);
	_pCover = new Cover[_nCover];
	for (j=0; j<_nCover; j++)
	{
		_pCover[j].CreateData();
	}
#ifdef MULTILAVELCOVER
	fread(&_nRootCover, sizeI, 1, pFile);
	if ( _nRootCover < _nCover )//means step policy contract or per risk treaty
	{
		_contractType = CONTRACT_STEPPOLICY;//will check sm file to determine if this contract is a per risk treaty
#ifdef WITHALLOCATION
		_pSetsIndex = new int[_nCover];
#endif
		_pRootCoverIdx = new int[_nRootCover];
		for (j=0; j<_nRootCover; j++)
		{
			fread(&_pRootCoverIdx[j], sizeI, 1, pFile);
		}
		int nNonLeafCover;
		fread(&nNonLeafCover, sizeI, 1, pFile);
		for (j=0; j<nNonLeafCover; j++)
		{
			//cover index
			fread(&valueI, sizeI, 1, pFile);
			//number of children
			fread(&nChild, sizeI, 1, pFile);
			pChildIdx = new int[nChild];
			for (k=0; k<nChild; k++)
			{
				fread(&(pChildIdx[k]), sizeI, 1, pFile);
			}
			_pCover[valueI].setChild(nChild, pChildIdx);
		}
	}
	else
	{
		_contractType = CONTRACT_REGULAR;
	}
#endif
	//read the key of parent for the first cover
	fread(&key, sizeI, 1, pFile);
	for (j=0; j<_nCover; j++)
	{
#ifdef MULTILAVELCOVER
		fread(&key, sizeI, 1, pFile);
		if ( key == COVER_PER_CONTRACT )
		{
			_pCover[j]._isPerContract = true;
		}
		else
		{
			_pCover[j]._isPerContract = false;
		}

		//read the number of children
		fread(&nChild, sizeI, 1, pFile);
		if ( nChild > 0 )
		{
			pChildIdx = new int[nChild];
			for (k=0; k<nChild; k++)
			{
				//read child index
				fread(&(pChildIdx[k]), sizeI, 1, pFile);
			}
			_pCover[j].setChild(nChild, pChildIdx);
		}
#else
		fread(&nChild, sizeI, 1, pFile);
		pChildIdx = new int[nChild];
		for (k=0; k<nChild; k++)
		{
			//read child index
			fread(&(pChildIdx[k]), sizeI, 1, pFile);
		}
		_pCover[j].setChild(nChild, pChildIdx);
#endif
		//read the key for item in the cover
		fread(&key, sizeI, 1, pFile);
		//key == PARENT_INDEX means a new cover, key == NODE_NUMBER means end of cover
		while ( key != PARENT_INDEX && key != NODE_NUMBER)
		{
			_pCover[j].setItem(key, pFile);
			fread(&key, sizeI, 1, pFile);
		}
		_pCover[j].setFuncP();
	}
	//read nNode
	fread(&_nNode, sizeI, 1, pFile);
	_pNode = new Node[_nNode];
	for (j=0; j<_nNode; j++)
	{
		_pNode[j].CreateData();
	}
	//read root information
	fread(&_nRoot, sizeI, 1, pFile);
	_pRootIdx = new int[_nRoot];
	for (j=0; j<_nRoot; j++)
	{
		fread(&(_pRootIdx[j]), sizeI, 1, pFile);
	}
		
#ifdef OVERLAP
	fread(&valueI, sizeI, 1, pFile);
	if ( valueI == 1 )//overlap contract
	{
		//read level information
		_pNodeLevel = new int[_nNode];
		int nLevel;
		_pLevelInfo = new LevelInfo();
		fread(&nLevel, sizeI, 1, pFile);
		_pLevelInfo->_nLevel = nLevel;
		_pLevelInfo->_pLevelNodeNum = new int[nLevel];
		_pLevelInfo->_pLevelNodeIdx = new int*[nLevel];
		for (j=0; j<nLevel; j++)
		{
			fread(&valueI, sizeI, 1, pFile);
			_pLevelInfo->_pLevelNodeNum[j] = valueI;
			_pLevelInfo->_pLevelNodeIdx[j] = new int[valueI];
			for (k=0; k<valueI; k++)
			{
				fread(&(_pLevelInfo->_pLevelNodeIdx[j][k]), sizeI, 1, pFile);
				_pNodeLevel[_pLevelInfo->_pLevelNodeIdx[j][k]] = j;
			}
		}

		//read node data
		for (j=0; j<_nNode; j++)
		{
			_pNode[j].readOverlapInput(pFile, sizeI, _pNode);
#ifdef PERRISK
			pIsPerriskNode[globalIdx++] = readPerriskFlag(pFile, sizeI);
#endif
		}
		done = true;
	}
#endif
	if ( !done )
	{
		for (j=0; j<_nNode; j++)
		{
			_pNode[j].readNonOverlapInput(pFile, sizeI, _pNode);
#ifdef PERRISK
			pIsPerriskNode[globalIdx++] = readPerriskFlag(pFile, sizeI);
#endif
		}
	}
}

#ifdef MULTILAVELCOVER
void Contract::allocMemForPerRiskTreaty(int nExposure)
{
	_contractType = CONTRACT_PERRISKTREATY;
#ifdef WITHALLOCATION
	for (int i=0; i<_nCover; i++)
	{
		_pCover[i].allocMemForPerRiskTreaty(nExposure);
	}
#endif
}
#endif

bool Contract::isActive(int time)
{
	if ( time < _startTime ) return false;
	if ( time > _endTime ) return false;
	return true;
}

#ifdef FOR_QA_TEST
void Contract::setglobalNodeIndex(int nodeStartIdx)
{
	for (int i=0; i<_nNode; i++)
	{
		_pNode[i]._nodeIdx = nodeStartIdx + i;
	}
}

void Contract::writeNodeLoss(FILE* pf, char* line, double* pRA)
{
	if ( pRA )
	{
		for (int i=0; i<_nNode; i++)
			_pNode[i].writeNodeLoss(pf, line, pRA[i]);
	}
	else
	{
		for (int i=0; i<_nNode; i++)
			_pNode[i].writeNodeLoss(pf, line, -1);
	}
}
#endif

#ifdef PERRISK
int Contract::addNewNode(int nodeStartIdx, int nAffectedNode, int* pNodeIdx, int* pNumExpo, int* pExpoStartIdx, ThreeInt* pInput)
{
	int i, j, nExposure, nAllLocToAdd = 0;
	int expoIdx, nLocToAdd, nAllExpo = 0;
	int* pNumLocToAdd = new int[nAffectedNode];
	for (i=0; i<nAffectedNode; i++)
		nAllExpo += pNumExpo[i];
	ThreeInt* pExpoLocIdx = new ThreeInt[nAllExpo];
	int nodeIdx = _nNode;
	for (i=0; i<nAffectedNode; i++)
	{
		expoIdx = pExpoStartIdx[i];
		nExposure = pNumExpo[i];
		for (j=0; j<nExposure; j++,expoIdx++)
		{
			pExpoLocIdx[j].i1 = pInput[expoIdx].i1;//exposure idx in Aggragator::_pNodeIdx
			pExpoLocIdx[j].i2 = pInput[expoIdx].i3;//location id
			pExpoLocIdx[j].i3 = expoIdx;//exposure index in pInput
		}
		sort3IntBy2(nExposure, pExpoLocIdx);
		pInput[pExpoLocIdx[0].i3].i2 = nodeIdx;
		nLocToAdd = 1;
		for (j=1; j<nExposure; j++)
		{
			if ( pExpoLocIdx[j].i2 != pExpoLocIdx[j-1].i2 )
			{
				nLocToAdd++;
				nodeIdx++;
			}
			pInput[pExpoLocIdx[j].i3].i2 = nodeIdx;
		}
		pNumLocToAdd[i] = nLocToAdd;
		nAllLocToAdd += nLocToAdd;
	}
	delete [] pExpoLocIdx;

	//add nodes to node array
//	NodeData* pNodeData;
	int index, nOrigNode = _nNode;
	Node* pNode = _pNode;
	_nNode += nAllLocToAdd;
	_pNode = new Node[_nNode];
#ifdef OVERLAP
	int k;
	int* pMoreChildIdx = 0;
	int* pNodeLevel = _pNodeLevel;
	if ( _pLevelInfo )
	{
		pMoreChildIdx = new int[_nNode];
		_pNodeLevel = new int[_nNode];
	}
#endif
	for (nodeIdx=0; nodeIdx<nOrigNode; nodeIdx++)
	{
		_pNode[nodeIdx].copyForPerrisk(pNode[nodeIdx]);
#ifdef OVERLAP
		if ( _pLevelInfo )
		{
			_pNodeLevel[nodeIdx] = pNodeLevel[nodeIdx];
		}
#endif
	}
	for (i=0; i<nAffectedNode; i++)
	{
		index = pNodeIdx[i] - nodeStartIdx;
//		pNodeData = _pNode[index]._pData;
#ifdef OVERLAP
		if ( _pLevelInfo )
		{
			if ( _pNodeLevel[index] == 0 )
				_pNodeLevel[index] = 1;
			k = 0;
		}
#endif
		for (j=0; j<pNumLocToAdd[i]; j++,nodeIdx++)
		{
#ifdef FOR_QA_TEST
			_pNode[nodeIdx]._nodeIdx = nodeIdx;
#endif

#ifdef OVERLAP
			if ( _pLevelInfo )
			{
				pMoreChildIdx[k++] = nodeIdx;
				_pNodeLevel[nodeIdx] = 0;
			}
#endif
			_pNode[nodeIdx].setDataForResolution(index, _pNode[index]);
		}

		//modify the child information for this perrisk node
		_pNode[index].resetDataForResolution(pNumLocToAdd[i]
#ifdef OVERLAP
	, pMoreChildIdx
#endif
		);

#ifdef OVERLAP
	delete [] pMoreChildIdx;
#endif
	}
	delete [] pNode;
	delete [] pNumLocToAdd;
	return nAllLocToAdd;
}
#endif

int Contract::convertCoveredPecentToAmount(int nodeStartIdx, int nAllExposure, TwoInt* pExpoIdxNodeIdx, double* pValue, double* pCoverValue
#ifdef OVERLAP
				, bool* pIsSet
#endif
				)
{
	//pValue[i] is the exposure value of exposure i
	
	int i, nExposure = 0;
	int nodeIdx, nodeEndIdx = nodeStartIdx + _nNode;
	//pCoverValue[i] will be the exposure value of node i
	for (i=0; i<_nNode; i++)
	{
		pCoverValue[i] = 0;
	}
	
	//go through the exposures link to nodes before this contract
	while ( nExposure < nAllExposure && pExpoIdxNodeIdx[nExposure].i2 < nodeStartIdx )
		nExposure++;
	//go through the exposures link to the nodes in this contract
	//set expusure value for nodes in this contract
	while ( nExposure < nAllExposure && pExpoIdxNodeIdx[nExposure].i2 < nodeEndIdx )
	{
#ifdef OVERLAP
		for (int j=0; j<_nNode; j++)
			pIsSet[j] = false;
#endif
		nodeIdx = pExpoIdxNodeIdx[nExposure].i2 - nodeStartIdx;
		_pNode[nodeIdx].addExpoValue(nodeIdx, pCoverValue, pValue[pExpoIdxNodeIdx[nExposure].i1], _pNode
#ifdef OVERLAP
			, pIsSet
#endif
			);
		nExposure++;
	}

	if ( nExposure == 0 )
		return 0;
	
	for (i=0; i<_nNode; i++,nodeIdx++)
	{
		_pNode[i].convertCoveredPecentToAmount(pCoverValue[i]);//pValue[i]);
	}

	for (i=0; i<_nCover; i++)
	{
		_pCover[i].convertCoveredPecentToAmount(pCoverValue);//pValue);
	}

	return nExposure;
}

void Contract::adjustByExchangeRate(int nCurrency, Losses* exchangeRate)
{
	int i;
	double rate = 1;
	for (i=0; i<nCurrency; i++)
	{
		if ( _currencyId == exchangeRate[i].idx )
		{
			rate = exchangeRate[i].loss;
			break;
		}
	}
	
	//adjust all terms in all covers by the currency rate
	for (i=0; i<_nCover; i++)
	{
		_pCover[i].adjustByExchangeRate(rate);
	}
	//adjust all terms in all nodes by the currency rate
	for (i=0; i<_nNode; i++)
	{
		_pNode[i].adjustByExchangeRate(rate);
	}
}

void Contract::setAffectedNode(int nAffectedLeaf, int nodeStartIdx, Losses* pLoss
#ifdef WITHALLOCATION
	, double* pNodeLoss
#endif
#ifdef OVERLAP
		, int* pParentIdx, bool* pIsSet, bool* pGroundUpLossIsSet
#endif
#ifdef PECENTAFFECTED
			, double* pInNodeVIT, double* pNodeTotalTIV
#endif
	)
{
	int i, nodeIdx;

#ifdef OVERLAP
	if ( _pLevelInfo )
	{//for overlap contract
		//set groundup loss for all nodes
		for (i=0; i<nAffectedLeaf; i++)	
		{
			for (int j=0; j<_nNode; j++)
			{
				pGroundUpLossIsSet[j] = false;
			}

			nodeIdx = pLoss[i].idx - nodeStartIdx;
#ifdef WITHALLOCATION
			pNodeLoss[nodeIdx] = pLoss[i].loss;
#endif
#ifdef PECENTAFFECTED
			pNodeTotalTIV[nodeIdx] = pInNodeVIT[i];
#endif
			_pNode[nodeIdx]._S = pLoss[i].loss;
			pGroundUpLossIsSet[nodeIdx] = true;
			_pNode[nodeIdx].setGuLossWithOverlap(pLoss[i].loss, _pNode, pGroundUpLossIsSet
#ifdef PECENTAFFECTED
			, pInNodeVIT[i], pNodeTotalTIV
#endif			
				);
		}
		//set affected bucket information for all affected ndoes
		for (i=0; i<_nNode; i++)
		{
			if ( _pNode[i]._isAffected )
			{
				_pNode[i]._pOverlapInfo->setAffectedBucketNodes(_pNode
#ifdef WITHALLOCATION
					, i, pNodeLoss
#endif
					);
			}
		}
		return;
	}
#endif

	//for non overlap contract
	for (i=0; i<nAffectedLeaf; i++)	
	{
		nodeIdx = pLoss[i].idx - nodeStartIdx;
#ifdef WITHALLOCATION
		pNodeLoss[nodeIdx] = pLoss[i].loss;
#endif
		_pNode[nodeIdx].setSubjectLoss(nodeIdx, pLoss[i].loss, _pNode
#ifdef PECENTAFFECTED
			, pInNodeVIT[i], pNodeTotalTIV
#endif			
			);
	}
}

#ifdef PECENTAFFECTED
void Contract::convertAffectedPecent2Amount(double* pNodeTotalTIV)
{
	int i;
	for (i=0; i<_nNode; i++)
	{
		_pNode[i].convertAffectedPecent2Amount(pNodeTotalTIV[i]);
	}
#ifdef MULTILAVELCOVER
	if ( _nRootCover < _nCover )
	{
		for (i=0; i<_nRootCover; i++)
		{
			_pCover[_pRootCoverIdx[i]].convertAffectedPecent2Amount(_pCover, pNodeTotalTIV);
		}
	}
#endif
}
#endif

void Contract::calcContractRecovery(double& recovery)
{
	int i;
	recovery = 0;
	bool done = false;
#ifdef OVERLAP
	if ( _pLevelInfo )
	{//this is an overlapped contract
		for (i=0; i<_nRoot; i++)
		{
			if ( _pNode[_pRootIdx[i]]._isAffected )
			{
				_pNode[_pRootIdx[i]].calcLossOverlap(_pNode, _pNodeLevel);
			}
		}
		done = true;
	}
#endif
	if ( !done )
	{//for non overlap contract
		for (i=0; i<_nRoot; i++)
		{
			if ( _pNode[_pRootIdx[i]]._isAffected )
			{
				_pNode[_pRootIdx[i]].calcLoss(_pNode);
			}
		}
	}

#ifdef MULTILAVELCOVER
	if ( _nRootCover < _nCover )
	{
		for (i=0; i<_nRootCover; i++)
		{
			recovery += _pCover[_pRootCoverIdx[i]].calcRootPayout(_pNode, _pCover
#ifdef WITHALLOCATION
		, _pRootCoverIdx[i], _pSetsIndex
#endif
				);
		}
		return;
	}
#endif
	for (i=0; i<_nCover; i++)
	{
		recovery += _pCover[i].calcLoss(_pNode);
	}
}

void Contract::reInitial(
#ifdef WITHALLOCATION
	double* pDirectLoss
#endif
	)
{
	for (int i=0; i<_nNode; i++)
	{
#ifdef WITHALLOCATION
		pDirectLoss[i] = 0;
#endif
		_pNode[i].reInitial();
	}
}

void Contract::calcLoss(int nAffectedLeaf, int nodeStartIdx, Losses* pLoss, double& recovery
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
	)
{
	reInitial(
#ifdef WITHALLOCATION
	pDirectLoss
#endif
	);

#ifdef MULTILAVELCOVER
	if ( _contractType == CONTRACT_PERRISKTREATY )
	{
		int i, nLeafCover = _nCover - _nRootCover;
		int* pLeafCoverIdx = new int[nLeafCover];
		int j = 0;
		nLeafCover = 0;
		for (i=0; i<_nCover; i++)
		{
			if ( i == _pRootCoverIdx[j] )
			{
				j++;
			}
			else
			{
				pLeafCoverIdx[nLeafCover++] = i;
			}
		}
#ifdef WITHALLOCATION
		for (i=0; i<nLeafCover; i++)
		{
			_pCover[pLeafCoverIdx[i]].calcPerRiskTreatyLeafLoss(nExposure, pExposureLoss, _pNode[0]);
		}
#else
		for (i=0; i<nLeafCover; i++)
		{
			_pCover[pLeafCoverIdx[i]].calcPerRiskTreatyLeafLoss(nExposure, pExposureLoss, _pNode[0]);
		}
#endif

		for (i=0; i<_nRootCover; i++)
		{
			recovery += _pCover[_pRootCoverIdx[i]].calcPerRiskTreatyRootLoss(_pCover);
		}

#ifdef WITHALLOCATION
		double* pAllcLoss = new double[_nCover];
		for (i=0; i<_nCover; i++)
		{
			pAllcLoss[i] = 0;
		}
		for (i=0; i<_nRootCover; i++)
		{
			_pCover[_pRootCoverIdx[i]].allocateLossToChildCover(_pCover, pAllcLoss);
		}
		for (i=0; i<nLeafCover; i++)
		{
			_pCover[pLeafCoverIdx[i]].allocateLossToExposure(pAllcLoss[pLeafCoverIdx[i]], nExposure, pExposureLoss, pExpoIdx, pPayoutA);
		}
		delete [] pAllcLoss;
#endif

		delete [] pLeafCoverIdx;
		return;
	}
#endif

	setAffectedNode(nAffectedLeaf,  nodeStartIdx, pLoss
#ifdef WITHALLOCATION
			, pDirectLoss
#endif
#ifdef OVERLAP
			, pIntTmp, pBoolTmp, pIsSet
#endif
#ifdef PECENTAFFECTED
			, pNodeVIT, pNodeTotalTIV
#endif
		);

#ifdef PECENTAFFECTED
		convertAffectedPecent2Amount(pNodeTotalTIV);
#endif

		calcContractRecovery(recovery);

#ifdef WITHALLOCATION
	allocationGR(nodeStartIdx, nExposure, pExpoIdx, pExpoIdx2NodeIdx, pExposureLoss, pPayoutA, pRA, pPA, pDirectLoss, pTmpD, pTmpR);
/*
	int i;
	for (i=0; i<_nNode; i++)
		pRA[i] = pPA[i] = pTmpD[i] = pTmpR[i] = 0;
	//calculate allocated recovery for all nodes
	for (i=0; i<_nRoot; i++)
	{
		if ( _pNode[_pRootIdx[i]]._isAffected )
		{
			pTmpD[_pRootIdx[i]] = _pNode[_pRootIdx[i]]._D;
			pTmpR[_pRootIdx[i]] = _pNode[_pRootIdx[i]]._S - _pNode[_pRootIdx[i]]._X - pTmpD[_pRootIdx[i]];
			if ( pTmpR[_pRootIdx[i]] > 0 )
			{
				//pTmpD : for allocated deductible for each node
				//pTmpR : for allocated recovery for each node
				//pRA   : for allocated recovery for each node based on the direct loss 
				_pNode[_pRootIdx[i]].allocation(pTmpD[_pRootIdx[i]], pTmpR[_pRootIdx[i]], _pNode, _pRootIdx[i], pDirectLoss, pRA, pTmpD, pTmpR
#ifdef OVERLAP
					  , nExposure, pExpoIdx, pExpoIdx2NodeIdx
#endif
					);
			}
		}
	}

#ifdef OVERLAP
	//reset pExpoIdx2NodeIdx
	bool* pIsInAllocationTree = new bool[_nNode];
	for (i=0; i<_nNode; i++)
	{
		pIsInAllocationTree[i] = false;
	}
	for (i=0; i<_nRoot; i++)
	{
		if ( _pNode[_pRootIdx[i]]._isAffected )
		{
			pIsInAllocationTree[_pRootIdx[i]] = true;
			_pNode[_pRootIdx[i]].setInAllocationFlag(_pNode, pIsInAllocationTree);
			_pNode[_pRootIdx[i]].resetExposure2NodeIdx(nodeStartIdx, _pNode, pIsInAllocationTree, nExposure, pExpoIdx, pExpoIdx2NodeIdx);
		}
	}
	delete [] pIsInAllocationTree;
#endif

	//calculate payout for all nodes
	//pPA : for allocated payout for each node
	//pTmpD : for allocated payout for each node based on the direct loss
	payoutAloc(pTmpR, pPA, pRA, pTmpD);

	//allocate payout to exposures
	int expoIdx, nodeIdx;
	double factor;
	for (i=0; i<nExposure; i++)
	{
		expoIdx = pExpoIdx[i];
		nodeIdx = pExpoIdx2NodeIdx[expoIdx] - nodeStartIdx;
		if ( pDirectLoss[nodeIdx] >= ONECENT )
		{
			factor = pExposureLoss[expoIdx] / pDirectLoss[nodeIdx];
			if ( _pNode[nodeIdx].isLeaf() )
			{
				pPayoutA[expoIdx] = factor * pPA[nodeIdx];
			}
			else
			{
				pPayoutA[expoIdx] = factor * pTmpD[nodeIdx];
			}
		}
	}
*/
#endif
}

#ifdef WITHALLOCATION
void Contract::allocationGR(int nodeStartIdx, int nExposure, int* pExpoIdx, int* pExpoIdx2NodeIdx, double* pExposureLoss,
							double* pPayoutA, double* pRA, double* pPA, double* pDirectLoss, double* pTmpD, double* pTmpR)
{
	int i, nodeIdx;
	for (i=0; i<_nNode; i++)
	{
		pRA[i] = pPA[i] = pTmpD[i] = pTmpR[i] = 0;
	}
	
	//calculate allocated recovery for all nodes
	for (i=0; i<_nRoot; i++)
	{
		if ( _pNode[_pRootIdx[i]]._isAffected )
		{
			pTmpD[_pRootIdx[i]] = _pNode[_pRootIdx[i]]._D;
			pTmpR[_pRootIdx[i]] = _pNode[_pRootIdx[i]]._S - _pNode[_pRootIdx[i]]._X - pTmpD[_pRootIdx[i]];
			if ( pTmpR[_pRootIdx[i]] > 0 )
			{
				//pTmpD : for allocated deductible for each node
				//pTmpR : for allocated recovery for each node
				//pRA   : for allocated recovery for each node based on the direct loss 
				_pNode[_pRootIdx[i]].allocation(pTmpD[_pRootIdx[i]], pTmpR[_pRootIdx[i]], _pNode, _pRootIdx[i], pDirectLoss, pRA, pTmpD, pTmpR
#ifdef OVERLAP
					  , nExposure, pExpoIdx, pExpoIdx2NodeIdx
#endif
					);
			}
		}
	}

#ifdef OVERLAP
	//for exposures not in the allocation tree of this senario, reset the index for the node it points
	//reset pExpoIdx2NodeIdx
	bool* pIsInAllocationTree = new bool[_nNode];
	for (i=0; i<_nNode; i++)
	{
		pIsInAllocationTree[i] = false;
	}
	for (i=0; i<_nRoot; i++)
	{
		if ( _pNode[_pRootIdx[i]]._isAffected )
		{
			pIsInAllocationTree[_pRootIdx[i]] = true;
			_pNode[_pRootIdx[i]].setInAllocationFlag(_pNode, pIsInAllocationTree);
			_pNode[_pRootIdx[i]].resetExposure2NodeIdx(nodeStartIdx, _pNode, pIsInAllocationTree, nExposure, pExpoIdx, pExpoIdx2NodeIdx);
		}
	}
	delete [] pIsInAllocationTree;
#endif

	//calculate payout for all nodes
	//pPA : for allocated payout for each node
	//pTmpD : for allocated payout for each node based on the direct loss
	payoutAloc(pTmpR, pPA, pRA, pTmpD);

	//allocate payout to exposures
	int expoIdx;
	double factor;
	for (i=0; i<nExposure; i++)
	{
		expoIdx = pExpoIdx[i];
		nodeIdx = pExpoIdx2NodeIdx[expoIdx] - nodeStartIdx;
		if ( pDirectLoss[nodeIdx] >= ONECENT )
		{
			factor = pExposureLoss[expoIdx] / pDirectLoss[nodeIdx];
			if ( _pNode[nodeIdx].isLeaf() )
			{
				pPayoutA[expoIdx] = factor * pPA[nodeIdx];
			}
			else
			{
				pPayoutA[expoIdx] = factor * pTmpD[nodeIdx];
			}
		}
	}
}

void Contract::payoutAloc(double* pRA, double* pPA, double* pDirectRA, double* pDirectPA)
{
	int i;
	for (i=0; i<_nNode; i++)
		pDirectPA[i] = pPA[i] = 0;
#ifdef MULTILAVELCOVER
	if ( _nRootCover < _nCover )//step policy case
	{
		for (i=0; i<_nRootCover; i++)
		{
			_pCover[_pRootCoverIdx[i]].payoutAlocForNonLeafRoot(_pCover, pRA, pPA, _pNode, pDirectRA, pDirectPA
				, _pRootCoverIdx[i], _pSetsIndex
			);
		}
		return;
	}
#endif
	for (i=0; i<_nCover; i++)
	{
		_pCover[i].payoutAloc(pRA, pPA, _pNode, pDirectRA, pDirectPA);
	}
}

#endif

void Contract::allocatioForDQ(int nExposure, int* pExpoIdx, int* pExpoIdx2NodeIdx, double* pExposureLoss, double* pPayoutA,
							  int nodeStartIdx, double* pDirectLoss, double* pRA)
{
	int i;
	for (i=0; i<_nNode; i++)
		pRA[i] = 0;

	double rootLoss, effectiveLimit, tiv;
	//pTmpR : allocated loss
	//pRA   : allocated direct loss 
	for (i=0; i<_nRoot; i++)
	{
		if ( _pNode[_pRootIdx[i]]._isAffected )
		{
			effectiveLimit = _pNode[_pRootIdx[i]]._S - _pNode[_pRootIdx[i]]._X - _pNode[_pRootIdx[i]]._D;
			tiv = _pNode[_pRootIdx[i]]._S;
			rootLoss = effectiveLimit  * ( tiv - _pNode[_pRootIdx[i]]._D - effectiveLimit * 0.5 ) / tiv;
			
			if ( rootLoss > 0 )
			{
				_pNode[_pRootIdx[i]].allocatioForDQ(rootLoss, _pNode, _pRootIdx[i], pDirectLoss, pRA);
			}
		}
	}
	
	//allocate loss to exposures
	int expoIdx, nodeIdx;
	double factor;
	for (i=0; i<nExposure; i++)
	{
		expoIdx = pExpoIdx[i];
		nodeIdx = pExpoIdx2NodeIdx[expoIdx] - nodeStartIdx;
		if ( pDirectLoss[nodeIdx] >= ONECENT )
		{
			factor = pExposureLoss[expoIdx] / pDirectLoss[nodeIdx];
			pPayoutA[expoIdx] = factor * pRA[nodeIdx];
		}
	}
}

void Contract::resetRootNode()
{
	for (int i=0; i<_nRoot; i++)
	{
		if ( _pNode[_pRootIdx[i]]._isAffected )
		{
			_pNode[_pRootIdx[i]]._S = 0;
			_pNode[_pRootIdx[i]]._isAffected = false;
		}
	}
}
