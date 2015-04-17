#include "utilFunc.h"
#include "Cover.h"
#include "string.h"

#ifdef MULTILAVELCOVER
#include "builtInFunctions.h"
using namespace cdl::Shared::FunctionLib;
/////////////////////////////////////////
#ifdef WITHALLOCATION
void Cover::allocMemForPerRiskTreaty(int nExposure)
{
	if ( _isPerContract )
	{
		_nContract = nExposure;
		_pLoss = new double[_nContract];
	}
}

void Cover::allocateLossToChildCover(Cover* pCover, double* pAllcLoss)
{
	if ( _R < ONECENT )
		return;
	int i;
	double factor, sum = 0;
	for (i=0; i<_pData->_nChild; i++)
	{
		sum += pCover[_pData->_pChildIdx[i]]._R;
	}
	if ( sum < ONECENT )
		return;
	factor = _R / sum;
	for (i=0; i<_pData->_nChild; i++)
	{
		pAllcLoss[_pData->_pChildIdx[i]] += factor * pCover[_pData->_pChildIdx[i]]._R;
	}
}

void Cover::allocateLossToExposure(double loss, int nExposure, double* pExposureLoss, int* pExposureIdx, double* pPayoutA)
{
	double factor, sum = 0;
	int i;
	if ( _isPerContract )
	{
		if ( _R < ONECENT )
			return;
		factor = loss / _R;
		for (i=0; i<nExposure; i++)
		{
			pPayoutA[pExposureIdx[i]] += factor * _pLoss[i];
		}
	}
	else
	{
		for (i=0; i<nExposure; i++)
		{
			sum += pExposureLoss[i];
		}
		if ( sum < ONECENT )
			return;
		factor = loss / sum;
		for (i=0; i<nExposure; i++)
		{
			pPayoutA[pExposureIdx[i]] += factor * pExposureLoss[i];
		}
	}
}
#endif

void Cover::calcPerRiskTreatyLeafLoss(int nExposure, double* pExposureLoss, Node& node)
{
	int i;
	if ( _isPerContract )
	{
		double sum = 0;
		for (i=0; i<nExposure; i++)
		{
//			node.calcTreatyLoss(pExposureLoss[i]);
			_S = pExposureLoss[i];//node._S - node._D - node._X;
			calcBasic();
#ifdef WITHALLOCATION
			_pLoss[i] = _R;
#endif
			sum += _R;
		}
		_R = sum;
	}
	else
	{
		_S = 0;
		for (i=0; i<nExposure; i++)
		{
//			node.calcTreatyLoss(pExposureLoss[i]);
			_S += pExposureLoss[i];//node._S - node._D - node._X;
		}
		calcBasic();
	}
}

double Cover::calcPerRiskTreatyRootLoss(Cover* pCover)
{
	_S = 0;
	for (int i=0; i<_pData->_nChild; i++)
	{
		_S += pCover[_pData->_pChildIdx[i]]._R;
	}
	calcBasic();
	return _R;
}

void Cover::payF()
{
	switch ( _payType )
	{
	case PayMin:
		_R = MIND(_pData->_pay, _S);
		break;
	case PayMax:
		_R = MAXD(_pData->_pay, _S);
		break;
	case PayConstant:
		_R =  _pData->_pay;
		break;
	}

	if ( _pData->_attach != -1 )
	{
		if ( _R < _pData->_attach )
			_R = 0;
	}

	_R *= _pData->_share;
}

void Cover::calcBasic()
{
	double X = 0;
	double D = 0;
	if ( _pData->_attach != -1 )
	{
		D = MIND(_S, _pData->_attach);
	}
    if ( _pData->_limit != -1 )
	{
		double a = _S - D;
		a<_pData->_limit ? X = 0 : X = a - _pData->_limit;
	}

    _R = _pData->_share * (_S - X - D);
}

void Cover::payStepPolicy(
#ifdef WITHALLOCATION
	int& index
#endif
	)
{
	switch ( _setsType )
	{
	case CalcSubjectMax:
#ifdef WITHALLOCATION
		index = ArrayMax(_pS, _pData->_nChild);
		_S = _pS[index];
#else
		_S = _pS[ArrayMax(_pS, _pData->_nChild)];
#endif
		break;
	case CalcSubjectMin:
#ifdef WITHALLOCATION
		index = ArrayMin(_pS, _pData->_nChild);
		_S = _pS[index];
#else
		_S = _pS[ArrayMin(_pS, _pData->_nChild)];
#endif
		break;
	case CalcSubjectSum:
		_S = ArraySum(_pS, _pData->_nChild);
		break;
	}

	if ( _payType == -1 )
	{
		calcBasic();
	}
	else
	{
		payF();
	}
}

void Cover::calcPayout(Node* pNode, Cover* pCover
#ifdef WITHALLOCATION
		, int index, int* pSetsIndex
#endif
					   )
{
	if ( _setsType > -1 )//non leaf cover
	{
		int i, nChild = _pData->_nChild;
		for (i=0; i<nChild; i++)
		{
			pCover[_pData->_pChildIdx[i]].calcPayout(pNode, pCover
#ifdef WITHALLOCATION
		, _pData->_pChildIdx[i], pSetsIndex
#endif
				);
			_pS[i] = pCover[_pData->_pChildIdx[i]]._R;
		}
		payStepPolicy(
#ifdef WITHALLOCATION
			pSetsIndex[index]
#endif
		);
	}
	else if ( _payType != -1 )
	{
		calcLeafLoss(pNode);
		payF();
	}
	else
	{
		calcLeafPayout(pNode);
	}
}

double Cover::calcRootPayout(Node* pNode, Cover* pCover
#ifdef WITHALLOCATION
		, int index, int* pSetsIndex
#endif
							 )
{
	calcPayout(pNode, pCover
#ifdef WITHALLOCATION
		, index, pSetsIndex
#endif
		);
	return _R;
}

#ifdef WITHALLOCATION
void Cover::payoutAlocForNonLeafRoot(Cover* pCover, double* pRA, double* pPA, Node* pNode, double* pDirectRA, double* pDirectPA, int index, int* pSetsIndex)
{
	payoutAlocForStepPolicy(_R, pCover, pRA, pPA, pNode, pDirectRA, pDirectPA
#ifdef WITHALLOCATION
				, index, pSetsIndex
#endif			
		);
}

void Cover::payoutAlocForStepPolicy(double loss, Cover* pCover, double* pRA, double* pPA,
									Node* pNode, double* pDirectRA, double* pDirectPA, int index, int* pSetsIndex)
{
	if ( loss < ONECENT )
		return;

	int i;
	double factor, sum;
	switch ( _setsType )
	{
	case CalcSubjectMax:
		pCover[pSetsIndex[index]].payoutAlocForStepPolicy(loss, pCover, pRA, pPA, pNode, pDirectRA, pDirectPA, pSetsIndex[index], pSetsIndex);
		break;
	case CalcSubjectMin:
		pCover[pSetsIndex[index]].payoutAlocForStepPolicy(loss, pCover, pRA, pPA, pNode, pDirectRA, pDirectPA, pSetsIndex[index], pSetsIndex);
		break;
	case CalcSubjectSum:
		sum = pCover[_pData->_pChildIdx[0]]._R;
		for (i=1; i<_pData->_nChild; i++)
		{
			sum += pCover[_pData->_pChildIdx[i]]._R;
		}
		if ( sum >= ONECENT )
		{
			factor = loss / sum;
			for (i=0; i<_pData->_nChild; i++)
			{
				pCover[_pData->_pChildIdx[i]].payoutAlocForStepPolicy(factor * pCover[_pData->_pChildIdx[i]]._R, pCover, pRA, pPA, pNode, pDirectRA, pDirectPA, _pData->_pChildIdx[i], pSetsIndex);
			}
		}
		break;
	default://leaf cover
		lossAllocation(loss, pRA, pPA, pNode, pDirectRA, pDirectPA);
	}
}
#endif

#ifdef PECENTAFFECTED
double Cover::convertAffectedPecent2Amount(Cover* pCover, double* pNodeTotalTIV)
{
	double tivAffected = 0;
	if ( _setsType == -1 )//leaf cover
	{
		for (int i=0; i<_pData->_nChild; i++)
		{
			tivAffected += pNodeTotalTIV[_pData->_pChildIdx[i]];
		}
	}
	else
	{
		for (int i=0; i<_pData->_nChild; i++)
		{
			tivAffected += pCover[_pData->_pChildIdx[i]].convertAffectedPecent2Amount(pCover, pNodeTotalTIV);
		}
	}
	if ( _pData->_payp != -1 )
	{
		_pData->_pay = _pData->_payp * tivAffected;
	}
	if ( _pData->_pPecentAffected )
	{
		if ( _pData->_pPecentAffected->_attach != -1 )
		{
			_pData->_attach = _pData->_pPecentAffected->_attach * tivAffected;
		}
		if ( _pData->_pPecentAffected->_limit != -1 )
		{
			_pData->_limit = _pData->_pPecentAffected->_limit * tivAffected;
		}
	}

	return tivAffected;
}
#endif
#endif
///////////////////////////////////////////////
CoverPecentData::CoverPecentData() :
_share(-1),
_limit(-1),
_attach(-1)
{
}

CoverData::CoverData() :
_nChild(0),
_pChildIdx(0),
_share(-1),
_limit(-1),
_attach(-1),
#ifdef MULTILAVELCOVER
_pay(-1),
#ifdef PECENTAFFECTED
_payp(-1),
#endif
#endif
_pPecentCover(0),
_pPecentAffected(0)
{
}

void CoverData::clearMemory()
{
	DELETEARRAY(_pChildIdx);
	DELETEOBJECT(_pPecentCover);
	DELETEOBJECT(_pPecentAffected);
}

void CoverData::adjustByExchangeRate(double rate)
{
	if ( _limit != -1 )
		_limit *= rate;
	if ( _attach != -1 )
		_attach *= rate;
}

////////////////////////////////////////////////////////////////////////////////////////
//Cover
Cover::Cover() :
_pData(0),
#ifdef MULTILAVELCOVER
_isPerContract(false),
_nContract(0),
_pLoss(0),
_setsType(-1),
_payType(-1),
_pS(0),
#endif
_S(0),
_R(0)
{
}

Cover::~Cover()
{
#ifdef MULTILAVELCOVER
	DELETEARRAY(_pS);
	DELETEARRAY(_pLoss);
#endif
}

void Cover::ClearMemory(int nObjCreated)
{
	if ( nObjCreated == 0 )
	{
		_pData->clearMemory();
		DELETEOBJECT(_pData);
	}
}

void Cover::linkData(Cover& cover)
{
	_pData = cover._pData;
}

void Cover::adjustByExchangeRate(double rate)
{
	_pData->adjustByExchangeRate(rate);
}

void Cover::calcLeafLoss(Node* pNode)
{
	double R;
	_S = 0;
	for (int i=0; i<_pData->_nChild; i++)
	{
		R = pNode[_pData->_pChildIdx[i]]._S - pNode[_pData->_pChildIdx[i]]._D - pNode[_pData->_pChildIdx[i]]._X;
		if ( R > 0 )
		{
			_S += R;
		}
	}
}

void Cover::calcLeafPayout(Node* pNode)
{
	calcLeafLoss(pNode);
	_pt2Func(this);
}

double Cover::calcLoss(Node* pNode)
{
	calcLeafPayout(pNode);
	return _R;
}

void cover_calcLoss_0(Cover* cover)
{
	cover->calcLoss0();
}
void Cover::calcLoss0()
{
	_R = _pData->_share * _S;
}

void cover_calcLoss_1(Cover* cover)
{
	cover->calcLoss1();
}
void Cover::calcLoss1()
{
	_R = _pData->_share * MIND(_S, _pData->_limit);
}

void cover_calcLoss_2(Cover* cover)
{
	cover->calcLoss2();
}
void Cover::calcLoss2()
{
	if ( _S < _pData->_attach ) _R = 0;
    else _R = _pData->_share * (_S - _pData->_attach);
}

void cover_calcLoss_3(Cover* cover)
{
	cover->calcLoss3();
}
void Cover::calcLoss3()
{
	double X, D = MIND(_S, _pData->_attach);
    double a = _S - D;
    a<_pData->_limit ? X = 0 : X = a - _pData->_limit;
    _R = _pData->_share * (_S - X - D);
}

int Cover::getCalcType()
{
	if ( _pData->_limit != -1 )
	{
		if ( _pData->_attach != -1 )
		{
			return 3;
		}
		else
		{
			return 1;
		}
	}
	else
	{
		if ( _pData->_attach != -1 )
		{
			return 2;
		}
		else
		{
			return 0;
		}
	}
}

void Cover::setFuncP()
{
#ifdef MULTILAVELCOVER
	if ( _setsType > -1 || _payType > -1 )
	{
		return;
	}
#endif

	int type = getCalcType();

	switch( type )
	{
	case 0:
		_pt2Func = &cover_calcLoss_0;
		break;
	case 1:
		_pt2Func = &cover_calcLoss_1;
		break;
	case 2:
		_pt2Func = &cover_calcLoss_2;
		break;
	case 3:
		_pt2Func = &cover_calcLoss_3;
		break;
	default:
		_pt2Func = &cover_calcLoss_0;
		break;
	}
}

void Cover::setItemValue(int key, double v)
{
	switch( key )
	{
	case COVER_SHARE:
		_pData->_share = v;
		break;
	case COVER_OCCATT:
		_pData->_attach = v;
		break;
	case COVER_OCCATTPC:
		if ( _pData->_pPecentCover == 0 )
			_pData->_pPecentCover = new CoverPecentData();
		_pData->_pPecentCover->_attach = v;
		break;
	case COVER_OCCATTPA:
		if ( _pData->_pPecentAffected == 0 )
			_pData->_pPecentAffected = new CoverPecentData();
		_pData->_pPecentAffected->_attach = v;
		break;
	case COVER_OCCLIM:
		_pData->_limit = v;
		break;
	case COVER_OCCLIMPC:
		if ( _pData->_pPecentCover == 0 )
			_pData->_pPecentCover = new CoverPecentData();
		_pData->_pPecentCover->_limit = v;
		break;
	case COVER_OCCLIMPA:
		if ( _pData->_pPecentAffected == 0 )
			_pData->_pPecentAffected = new CoverPecentData();
		_pData->_pPecentAffected->_limit = v;
		break;
#ifdef MULTILAVELCOVER
	case COVER_PAY:
		_pData->_pay = v;
		_payType = PayConstant;
		break;
	case COVER_PAYF_MAX:
		_pData->_pay = v;
		_payType = PayMax;
		break;
	case COVER_PAYF_MIN:
		_pData->_pay = v;
		_payType = PayMin;
		break;
#ifdef PECENTAFFECTED
	case COVER_PAYP:
		_pData->_payp = v;
		_payType = PayConstant;
		break;
	case COVER_PAYFP_MAX:
		_pData->_payp = v;
		_payType = PayMax;
		break;
	case COVER_PAYFP_MIN:
		_pData->_payp = v;
		_payType = PayMin;
		break;
#endif
#endif
	}
}

void Cover::setItem(int key, FILE* pf)
{
#ifdef MULTILAVELCOVER
	switch ( key )
	{
	case COVER_SETS_MAX:
		_pS = new double[_pData->_nChild];
		_setsType = CalcSubjectMax;
		return;
	case COVER_SETS_MIN:
		_pS = new double[_pData->_nChild];
		_setsType = CalcSubjectMin;
		return;
	case COVER_SETS_SUM:
		_pS = new double[_pData->_nChild];
		_setsType = CalcSubjectSum;
		return;
	}
#endif
	double v;
	fread(&v, sizeof(double), 1, pf);
	setItemValue(key, v);
}

void Cover::CreateData()
{
	_pData = new CoverData();
}

void Cover::setChild(int nChild, int* pChildIdx)
{
	_pData->_nChild = nChild;
	_pData->_pChildIdx = pChildIdx;
}

void Cover::lossAllocation(double loss, double* pRA, double* pPA, Node* pNode, double* pDirectRA, double* pDirectPA)
{
	if ( loss < ONECENT )
		return;

	int i, nChild = 0;
	int* pChildIdx = new int[_pData->_nChild];
	for (i=0; i<_pData->_nChild; i++)
	{
		if ( pNode[_pData->_pChildIdx[i]]._isAffected == true )
		{
			pChildIdx[nChild] = _pData->_pChildIdx[i];
			nChild++;
		}
	}
	Node::payoutAlloc(loss, nChild, pChildIdx, pRA, pPA, pNode, -1, pDirectRA, pDirectPA);
	delete [] pChildIdx;
}

void Cover::payoutAloc(double* pRA, double* pPA, Node* pNode, double* pDirectRA, double* pDirectPA)
{
	lossAllocation(_R, pRA, pPA, pNode, pDirectRA, pDirectPA);
}

void Cover::convertCoveredPecentToAmount(double* pValue)
{
	if ( _pData->_pPecentCover == 0 )
		return;

	double value = 0;
	for (int i=0; i<_pData->_nChild; i++)
	{
		value += pValue[_pData->_pChildIdx[i]];
	}
	if ( _pData->_pPecentCover->_limit != -1 )
	{
		setItemSmaller(_pData->_limit, _pData->_pPecentCover->_limit);
	}
	if ( _pData->_pPecentCover->_attach != -1 )
	{
		setItemLarger(_pData->_attach, _pData->_pPecentCover->_attach);
	}
	DELETEOBJECT(_pData->_pPecentCover);
}

