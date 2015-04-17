#include "utilFunc.h"
#include "Calculation.h"
#include "string.h"

#include "time.h"

static void writeInput(FILE* pf, char* line, int nInputs, LossesWithPerilId* inputs)
{
	int i, size = sprintf_s(line, 1020, "nInput = %d,\n", nInputs);
	fwrite(line, size, sizeof(char), pf);
	for (i=0; i<nInputs; i++)
	{
		size = sprintf_s(line, 1020, "%d,%d,%f,\n", inputs[i].id, inputs[i].perilId, inputs[i].lossRatio);
		fwrite(line, size, sizeof(char), pf);
	}
	size = sprintf_s(line, 1020, "\n");
	fwrite(line, size, sizeof(char), pf);
}

static void writeAffectedLoss(FILE* pf, char* line, int nAffectedNode, Losses* pLoss, int nAffectedContract, int* pAffectedContractIdx, int* pNumAffectedLeaf)
{
	int i, size = sprintf_s(line, 1020, "nAffectedNode = %d,\n", nAffectedNode);
	fwrite(line, size, sizeof(char), pf);
	for (i=0; i<nAffectedNode; i++)
	{
		size = sprintf_s(line, 1020, "%d,%f,\n", pLoss[i].idx, pLoss[i].loss);
		fwrite(line, size, sizeof(char), pf);
	}
	size = sprintf_s(line, 1020, "nAffectedContract = %d,\n", nAffectedContract);
	fwrite(line, size, sizeof(char), pf);
	for (i=0; i<nAffectedContract; i++)
	{
		size = sprintf_s(line, 1020, "contractIdx,%d,nAffectedNode,%d,\n", pAffectedContractIdx[i], pNumAffectedLeaf[i]);
		fwrite(line, size, sizeof(char), pf);
	}
	size = sprintf_s(line, 1020, "\n");
	fwrite(line, size, sizeof(char), pf);
}

//////////////////////////////////////////////////////////////////////////////////////////////
//
Calculation::Calculation() :
_nObjCreated(0),
_threadIdx(0),
_subeventIdx(1),
_maxNode(0),
_pNodeStartIdx(0),
_pContract(0),
_nAffectedContract(0),
_pAffectedContractIdx(0),
_pNumAffectedLeaf(0),
_nCurrency(0),
_exchangeRate(0),
_pAggragator(0),
_pNodeLoss(0),
#ifdef WITHALLOCATION
_nAllLoc(0),
_pIsAffectedLoc(0),
_pLocLoss(0),
_pDirectLoss(0),
_pRA(0),
_pPA(0),
_pTmpD(0),
_pTmpR(0),
_pExposureLoss(0),
_pExpoIdx2NodeIdx(0),
_pNumExpoA(0),
_pExpoIdxA(0),
#endif
#ifdef OVERLAP
_pIntTmp(0),
_pBoolTmp(0),
#endif
_perriskResolution(0)
{
	Initial();
}

Calculation::~Calculation()
{
#ifdef WITHALLOCATION
	DELETEARRAY(_pIsAffectedLoc);
	DELETEARRAY(_pLocLoss);
	DELETEARRAY(_pDirectLoss);
	DELETEARRAY(_pRA);
	DELETEARRAY(_pPA);
	DELETEARRAY(_pTmpD);
	DELETEARRAY(_pTmpR);
	DELETEARRAY(_pExposureLoss);
	DELETEARRAY(_pExpoIdx2NodeIdx);
	DELETEARRAY(_pNumExpoA);
	DELETEARRAY(_pExpoIdxA);
#endif	

#ifdef OVERLAP
	DELETEARRAY(_pIntTmp);
	DELETEARRAY(_pBoolTmp);
#endif	
	DELETEARRAY(_pAffectedContractIdx);
	DELETEARRAY(_pNumAffectedLeaf);
	DELETEARRAY(_pNodeLoss);
}

void Calculation::ClearMemory()
{
	int nObject = --_nObjCreated[0];
	if ( nObject >= 0 )
	{
		for (int i=0; i<_nContract; i++)
		{
			_pContract[i].ClearMemory(nObject);
		}
		DELETEARRAY(_pContract);
		_nContract = 0;
		
		if ( nObject == 0 )
		{
			DELETEARRAY(_pNodeStartIdx);
			DELETEARRAY(_nObjCreated);
			DELETEOBJECT(_pAggragator);
		}
	}
}

void Calculation::buildPortfolio(char* irFile)
{
	char str[1024];
	sprintf_s(str, "%sb", irFile);
	FILE* pFile = 0;
	if ( fopen_s(&pFile, str, "rb") != 0 )
	{
		printf("could not open the binary file %s\n", str);
		return;
	}

	readInputFromBinFile(pFile);
	fclose (pFile);

	_pNodeStartIdx = new int[_nContract+1];
	_nAllNode = _maxNode = 0;
	int i, nNode;
	for (i=0; i<_nContract; i++)
	{
		_pNodeStartIdx[i] = _nAllNode;
		nNode = _pContract[i].getNumNode();
		if ( _maxNode < nNode )
			_maxNode = nNode;
		_nAllNode += nNode;
	}
	_pNodeStartIdx[i] = _nAllNode;

#ifdef FOR_QA_TEST
	for (i=0; i<_nContract; i++)
	{
		_pContract[i].setglobalNodeIndex(_pNodeStartIdx[i]);
	}
#endif

//	adjustByExchangeRate();

//	setContracts(_pContract);
}

void Calculation::readInputFromBinFile(FILE* pFile)
{
	int i, sizeI = sizeof(int);

#ifdef PERRISK
	int nodeIdx = 0;
	bool* pIsPerriskNode = new bool[_nAllNode];
#endif
	for (i=0; i<_nContract; i++)
	{
		_pContract[i].readInput(pFile, sizeI
#ifdef PERRISK
		, nodeIdx, pIsPerriskNode
#endif
		);
	}

#ifdef PERRISK
		_perriskResolution = new PerRiskResolusion();
		_perriskResolution->_pIsPerriskNode = pIsPerriskNode;
#endif
}

void Calculation::adjustByExchangeRate()
{
	if ( _nCurrency == 0 )
		return;
	for (int i=0; i<_nContract; i++)
	{
		_pContract[i].adjustByExchangeRate(_nCurrency, _exchangeRate);
	}
}

#ifdef MULTILAVELCOVER
	void Calculation::allocMemForPerRiskTreaty()
	{
		PerRiskData* pPerRiskData = _pAggragator->getPerRiskTreatyData();
		for (int i=0; i<_nContract; i++)
		{
			if ( pPerRiskData[i]._nExposure != -1 )
			{
				_pContract[i].allocMemForPerRiskTreaty(pPerRiskData[i]._nExposure);
			}
		}
	}
#endif

void Calculation::initialize(char* irFile, char* smFile, char* ixFile, Calculation* prevObj)
{
	int i;
	_pContract = new Contract[_nContract];
	_pAffectedContractIdx = new int[_nContract];
	_pNumAffectedLeaf = new int[_nContract];
	
	if ( prevObj == 0 )
	{
		_nObjCreated = new int[1];
		_threadIdx = _nObjCreated[0] = 1;

		buildPortfolio(irFile);
		
		CreateAggregator(smFile, ixFile);

		i = _pAggragator->getNumNode();
		if ( i > _nAllNode )
			_nAllNode = i;
		
#ifdef PECENTCOVER
		int nAllExposure = _pAggragator->getNumExposure();
		int* pEx2NodeIdx = _pAggragator->getExpo2NodeIdx();
		double* pExpoValue = _pAggragator->getExposureValue();
		TwoInt* pExpoIdxNodeIdx = new TwoInt[nAllExposure];
		for (i=0; i<nAllExposure; i++)
		{
			pExpoIdxNodeIdx[i].i1 = i;
			pExpoIdxNodeIdx[i].i2 = pEx2NodeIdx[i];
		}
		sortTwoIntArrayInc(nAllExposure, pExpoIdxNodeIdx);
		int expoStartIdx = 0;
		double* pCoverValue = new double[_maxNode];
#ifdef OVERLAP
		bool* pIsSet = new bool[_maxNode];
#endif
		int nExposure, nExposureLeft = nAllExposure;
		for (i=0; i<_nContract; i++)
		{
			nExposure = _pContract[i].convertCoveredPecentToAmount(_pNodeStartIdx[i], nExposureLeft, pExpoIdxNodeIdx+expoStartIdx, pExpoValue, pCoverValue
#ifdef OVERLAP
				, pIsSet
#endif
				);
			expoStartIdx += nExposure;
			nExposureLeft -= nExposure;
		}
		delete [] pCoverValue;
#endif
	}
	else
	{
		_nObjCreated = prevObj->_nObjCreated;
		_nObjCreated[0]++;
		_threadIdx = _nObjCreated[0];

		for (i=0; i<_nContract; i++)
		{
			_pContract[i].copyDataForMultiThread(prevObj->_pContract[i]);
		}

		_pNodeStartIdx = prevObj->_pNodeStartIdx;
		_maxNode = prevObj->_maxNode;

//		setContracts(_pContract);

		_pAggragator = prevObj->_pAggragator;
		_nAllNode = prevObj->_nAllNode;
	}

	_pNodeLoss = new Losses[_nAllNode];
	for (i=0;i<_nAllNode; i++)
	{
		_pNodeLoss[i].idx = i;
		_pNodeLoss[i].loss = 0;
	}

#ifdef WITHALLOCATION
	_pNumExpoA = new int[_nContract];
	_pExpoIdxA = new int*[_nContract];
	for (i=0; i<_nContract; i++)
	{
		_pNumExpoA[i] = 0;
		_pExpoIdxA[i] = 0;
	}
	_pRA = new double[_maxNode];
	_pPA = new double[_maxNode];
	_pDirectLoss = new double[_maxNode];
	_nAllLoc = _pAggragator->getNumAllLocation();
	_pIsAffectedLoc = new bool[_nAllLoc];
	_pLocLoss = new double[_nAllLoc];
	_pTmpD = new double[_maxNode];
	_pTmpR = new double[_maxNode];
	for (i=0; i<_nAllLoc; i++)
	{
		_pLocLoss[i] = 0;
		_pIsAffectedLoc[i] = false;
	}
	for (i=0; i<_maxNode; i++)
	{
		_pRA[i] = _pPA[i] = _pDirectLoss[i] = _pTmpD[i] = _pTmpR[i] = 0;
	}
#endif

#ifdef OVERLAP
	_pIntTmp = new int[_maxNode];
	_pBoolTmp = new bool[_maxNode];
	for (i=0; i<_maxNode; i++)
		_pBoolTmp[i] = false;
#endif
}

void Calculation::CreateAggregator(char* smFile, char* ixFile)
{
	_pAggragator = new Aggragator();
#ifdef PERRISK
	_perriskResolution->_maxNode = _maxNode;
	_perriskResolution->_nOrigNode = _nAllNode;
	_perriskResolution->_nContract = _nContract;
	_perriskResolution->_pContract = _pContract;
	_perriskResolution->_pNodeStartIdx = _pNodeStartIdx;
#endif

	_pAggragator->readInputData(_nContract, _pNodeStartIdx, smFile, ixFile, _perriskResolution);

#ifdef MULTILAVELCOVER
	allocMemForPerRiskTreaty();
#endif

#ifdef PERRISK
	int i, nNode;
	for (i=0; i<_nContract; i++)
	{
		nNode = _pContract[i].getNumNode();
		if ( _maxNode < nNode )
			_maxNode = nNode;
	}
	DELETEOBJECT(_perriskResolution);
#endif
}

void Calculation::SetAffectedContract(int nInputs, LossesWithPerilId* inputs, int time
#ifdef PECENTAFFECTED
		, double* pNodeVIT
#endif
#ifdef FOR_QA_TEST
		, FILE* pf, char* line
#endif
	)
{
#ifdef WITHALLOCATION
	_pExposureLoss = new double[nInputs];
	_pExpoIdx2NodeIdx = new int[nInputs];
#endif

	//set groundup loss for each node (_pNodeLoss)
	//set number of exposures for each contract (_pNumExpoA) and exposure indices (_pExpoIdxA) in the whole exposure array like Aggragator::_pNodeIdx
	//set contract indeces (_pExpoIdx2NodeIdx) for each loss from inputs, _pExpoIdx2NodeIdx is used as a temperary array in this function
	//set node TIV (pNodeVIT)
	_pAggragator->run(nInputs, inputs, _pNodeLoss, _nContract, _pNodeStartIdx
#ifdef WITHALLOCATION
		, _pNumExpoA, _pExpoIdxA, _pExpoIdx2NodeIdx
#endif
#ifdef PECENTAFFECTED
		, pNodeVIT
#endif
		);
	
	//rearrange _pNodeLoss and pNodeVIT if appliable
	int i, nAffectedNode = 0;
	for (i=0; i<_nAllNode; i++)
	{
		if (_pNodeLoss[i].loss >= ONECENT)
		{
			_pNodeLoss[nAffectedNode].idx = i;
			if ( i > nAffectedNode )
			{
				_pNodeLoss[nAffectedNode].loss = _pNodeLoss[i].loss;
				_pNodeLoss[i].loss = 0;
#ifdef PECENTAFFECTED
				pNodeVIT[nAffectedNode] = pNodeVIT[i];
				pNodeVIT[i] = 0;
#endif
			}
			nAffectedNode++;
		}
	}

#ifdef WITHALLOCATION
	bool* pContractAffected = new bool[_nContract];
	for (i=0; i<_nContract; i++)
		pContractAffected[i] = false;
#endif
	//set affected contracts
	_nAffectedContract = 0;
	int contractIdx = 1;
	i = 0;
	while ( i < nAffectedNode )
	{
		if ( _pNodeLoss[i].idx < _pNodeStartIdx[contractIdx] )
		{
			i++;
			if ( _pContract[contractIdx-1].isActive(time) )
			{
				_pAffectedContractIdx[_nAffectedContract] = contractIdx - 1;
				_pNumAffectedLeaf[_nAffectedContract] = 1;
#ifdef WITHALLOCATION
				pContractAffected[contractIdx-1] = true;
#endif
			
				while ( i < nAffectedNode && _pNodeLoss[i].idx < _pNodeStartIdx[contractIdx] )
				{
					_pNumAffectedLeaf[_nAffectedContract]++;
					i++;
				}
				_nAffectedContract++;
				if ( i == nAffectedNode )
					break;
				contractIdx++;
				while ( _pNodeLoss[i].idx >= _pNodeStartIdx[contractIdx] )
					contractIdx++;
			}
			else
			{
				while ( i < nAffectedNode && _pNodeLoss[i].idx < _pNodeStartIdx[contractIdx] )
				{
					i++;
				}
				if ( i == nAffectedNode )
					break;
				contractIdx++;
				while ( _pNodeLoss[i].idx >= _pNodeStartIdx[contractIdx] )
					contractIdx++;
			}
		}
		else
		{
			contractIdx++;
			while ( _pNodeLoss[i].idx >= _pNodeStartIdx[contractIdx] )
				contractIdx++;
		}
	}
#ifdef WITHALLOCATION
	//_pExpoIdx2NodeIdx[i] is the node index in the global node array of the i-th exposure in the array of inputs
	_pAggragator->getExposureInfo(nInputs, inputs, _pExposureLoss, _pExpoIdx2NodeIdx);
	//
	for (i=0; i<_nContract; i++)
	{
		if ( _pNumExpoA[i] > 0 && pContractAffected[i] == false )
		{
			DELETEARRAY(_pExpoIdxA[i]);
		}
	}
	delete [] pContractAffected;
#endif

#ifdef FOR_QA_TEST
	if ( err == 0 )
		writeAffectedLoss(pf, line, nAffectedNode, _pNodeLoss, _nAffectedContract, _pAffectedContractIdx, _pNumAffectedLeaf);
#endif

}

void Calculation::calcLoss(CalculationInterface* payInterface)
{
	_subeventIdx++;

	int time = 0;
	int nInputs = payInterface->numInputLosses;
	LossesWithPerilId* inputs = payInterface->inputs;
	Losses* outputs = payInterface->outContractGross;
	Losses* pPayout = payInterface->outRiteGross;

	bool* pIsValidExpo = new bool[nInputs];
	int nInvalidExpo = _pAggragator->setValidFlag(nInputs, pIsValidExpo, inputs);
	int newNumInput = nInputs - nInvalidExpo;
	if ( newNumInput == 0 )
	{
		printf("There is no valid input loss.\n");
		return;
	}
	LossesWithPerilId* pInput = new LossesWithPerilId[newNumInput];
	int i, j = 0;
	for (i=0; i<nInputs; i++)
	{
		if ( pIsValidExpo[i] )
		{
			pInput[j].id = inputs[i].id;
			pInput[j].lossRatio = inputs[i].lossRatio;
			pInput[j++].perilId = 1;//not used
		}
	}
	nInputs = newNumInput;
	inputs = pInput;

	_nCurrency = payInterface->numCurrencies;
	_exchangeRate = payInterface->currencyTable;

//	bool forDataQuality = false;
	
	if ( payInterface->cdlOption & RITE_GROSS || payInterface->cdlOption & RIT_GROSS )//|| payInterface->cdlOption & DQ )
	{
#ifndef WITHALLOCATION
		_allocationIdx = NO_ALLOCATION;
		printf("This portfolio can not run with allocation, to do this, you need to tell codeGen when this cdl is created.\n");
#else
		if ( payInterface->cdlOption & RIT_GROSS )
			_allocationIdx = LOCATION_ALLOCATION;
		else //if ( payInterface->cdlOption & RITE_GROSS )
			_allocationIdx = EXPOSURE_ALLOCATION;
//		else
//			_allocationIdx = DQ_ALLOCATION;
#endif
	}
	else
	{
//		if ( payInterface->cdlOption & DQ )
//			forDataQuality = true;
		_allocationIdx = NO_ALLOCATION;
	}

#ifdef FOR_QA_TEST
	FILE* pf;
	char line[2049];
	sprintf_s(line, 2048, "C:\\nodeLoss_%d_%d.txt", _threadIdx, _subeventIdx);
	int err = fopen_s(&pf, line, "w");
	if( err != 0 )
	{
		printf("could not open the file (%s) to write out node loss information.\n", line);
	}
//	else
//	{
//		writeInput(pf, line, nInputs, inputs);;
//	}
#endif

#ifdef OVERLAP
	bool* pIsSet = new bool[_maxNode];
#endif

#ifdef PECENTAFFECTED
	double* pNodeVIT = new double[_nAllNode];
	double* pNodeTotalTIV = new double[_nAllNode];
	for (i=0; i<_nAllNode; i++)
	{
		pNodeTotalTIV[i] = pNodeVIT[i] = 0;
	}
#endif

	SetAffectedContract(nInputs, inputs, time
#ifdef PECENTAFFECTED
		, pNodeVIT
#endif
#ifdef FOR_QA_TEST
		, pf, line
#endif
		);

	if ( _nAffectedContract <= 0 )
	{
		payInterface->numContractGross = 0;
		payInterface->numRiteGross = 0;
		delete [] pIsValidExpo;
		delete [] pInput;
#ifdef OVERLAP
		delete [] pIsSet;
#endif

#ifdef PECENTAFFECTED
		delete [] pNodeVIT;
		delete [] pNodeTotalTIV;
#endif
		return;
	}

	int contractIdx, lossIndex = 0;
	int nNonZeroLoss = 0;
	double loss = 0;
#ifdef WITHALLOCATION
	double* pPayoutA = new double[nInputs];
	for (i=0; i<nInputs; i++)
		pPayoutA[i] = 0;
#endif

#ifdef MULTILAVELCOVER
	PerRiskData* pPerRiskData = _pAggragator->getPerRiskTreatyData();
#endif

	for (i=0; i<_nAffectedContract; i++)
	{
		contractIdx = _pAffectedContractIdx[i];
		_pContract[contractIdx].calcLoss(_pNumAffectedLeaf[i],  _pNodeStartIdx[contractIdx], _pNodeLoss+lossIndex, loss
#ifdef WITHALLOCATION
			,//_pRA, _pPA, _pDirectLoss, _pTmpD, _pTmpR are temprary arraies
			_pNumExpoA[contractIdx], _pExpoIdxA[contractIdx], _pExpoIdx2NodeIdx, _pExposureLoss, pPayoutA, _pRA, _pPA, _pDirectLoss, _pTmpD, _pTmpR
#endif
#ifdef OVERLAP
			, _pIntTmp, _pBoolTmp, pIsSet
#endif
#ifdef PECENTAFFECTED
			, pNodeVIT, pNodeTotalTIV
#endif
#ifdef MULTILAVELCOVER
#ifndef WITHALLOCATION
			, pPerRiskData[contractIdx]._nExposure, pPerRiskData[contractIdx]._pExposureLoss
#endif
#endif
			);

#ifdef FOR_QA_TEST
		if ( err == 0 )
		{
			_pContract[contractIdx].writeNodeLoss(pf, line, 0);
		}
#endif
		lossIndex += _pNumAffectedLeaf[i];

		if ( loss >= ONECENT )
		{
			outputs[nNonZeroLoss].idx = contractIdx;
			outputs[nNonZeroLoss].loss = loss;
			nNonZeroLoss++;
		}
	}

#ifdef OVERLAP
	delete [] pIsSet;
#endif

#ifdef PECENTAFFECTED
	delete [] pNodeVIT;
	delete [] pNodeTotalTIV;
#endif

	lossIndex = 0;
	for (i=0; i<_nAffectedContract; i++)
	{
		for (j=0; j<_pNumAffectedLeaf[i]; j++,lossIndex++)
		{
			_pNodeLoss[lossIndex].loss = 0;
		}
	}

#ifdef WITHALLOCATION
	if ( _allocationIdx == LOCATION_ALLOCATION )
	{
		//aggragate payout from exposures to locations
		int* pExpoIdx2LocIdx = _pAggragator->getExpoIdx2LocIdx();
		for (i=0; i<nInputs; i++)
		{
			_pIsAffectedLoc[pExpoIdx2LocIdx[inputs[i].id]] = true;
			_pLocLoss[pExpoIdx2LocIdx[inputs[i].id]] += pPayoutA[i];
		}
		int nPayoutLoc = 0;
		for (i=0; i<_nAllLoc; i++)
		{
			if ( _pIsAffectedLoc[i] )
			{
				pPayout[nPayoutLoc].idx = i;
				if ( _pLocLoss[i] >= ONECENT )
					pPayout[nPayoutLoc].loss = _pLocLoss[i];
				else
					pPayout[nPayoutLoc].loss = 0;
				_pLocLoss[i] = 0;
				nPayoutLoc++;
			}
		}
		payInterface->numRiteGross = nPayoutLoc;
	}
	else //if ( _allocationIdx == EXPOSURE_ALLOCATION )
	{
		nInputs = payInterface->numInputLosses;
		inputs = payInterface->inputs;
		j = 0;
		for (i=0; i<nInputs; i++)
		{
			pPayout[i].idx = inputs[i].id;
			if ( pIsValidExpo[i] )
				pPayout[i].loss = pPayoutA[j++];
			else
				pPayout[i].loss = 0;
		}
		payInterface->numRiteGross = nInputs;
	}

	for (i=0; i<_nAffectedContract; i++)
	{
		contractIdx = _pAffectedContractIdx[i];
		DELETEARRAY(_pExpoIdxA[contractIdx]);
	}
	DELETEARRAY(_pExposureLoss);
	DELETEARRAY(_pExpoIdx2NodeIdx);

	delete [] pPayoutA;
#endif
	
	delete [] pIsValidExpo;
	delete [] pInput;
	payInterface->numContractGross = nNonZeroLoss;
}

//////////////////////////////////////////////////////////////////////////////
//dll functions for outside to call
void initialize(char* irFile, char* smFile, char* ixFile, long long prevObj, long long* newObj)
{
	if ( !prevObj )
		printf("Calculation initialize based on the file (%s)\n", irFile);

	Calculation* pCalc = new Calculation();
	if ( prevObj )
	{
		Calculation* pCalcPrev = *(Calculation**) &prevObj;
		pCalc->initialize(0, 0, 0, pCalcPrev);
	}
	else
	{
		pCalc->initialize(irFile, smFile, ixFile, 0);
	}
	newObj[0] = (long long) pCalc;
}

void Pay(long long object, CalculationInterface* payInterface)
{
	if ( payInterface == 0 )
		return;
	if ( payInterface->numInputLosses <= 0 )
	{
		payInterface->numContractGross = 0;
		payInterface->numRiteGross = 0;
		return;
	}
	Calculation* pCalc = *(Calculation**) &object;
	pCalc->calcLoss(payInterface);
}

void ClearPortfolio(long long* object)
{
	Calculation* pCalc = *(Calculation**) object;
	pCalc->ClearMemory();
	delete pCalc;
	*object = 0;
}

