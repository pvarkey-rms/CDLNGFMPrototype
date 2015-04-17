#include "utilFunc.h"
#include "Node.h"
#include "string.h"
#include <cmath>

#ifdef OVERLAP
//////////////////////////////////////////////////////////////////////////////////////////
//Bucket
Bucket::Bucket() :
_nNode(0),
_pNodeIdx(0),
_nAffectedNode(0),
_pAffectedNodeIdx(0)
#ifdef WITHALLOCATION
, _directLoss(0)
#endif
{
}

Bucket::~Bucket()
{
	DELETEARRAY(_pNodeIdx);
	DELETEARRAY(_pAffectedNodeIdx);
}

bool Bucket::setAffectedNode(Node* pNode
#ifdef WITHALLOCATION
		, int nodeIdx, double* pDirectLoss
#endif
							 )
{
	_nAffectedNode = 0;
	if ( _nNode == 0 )
		return true;
	int i;
	for (i=0; i<_nNode; i++)
	{
		if ( pNode[_pNodeIdx[i]]._isAffected )
		{
			_pAffectedNodeIdx[_nAffectedNode++] = _pNodeIdx[i];
		}
	}
	if ( _nAffectedNode > 0 )
	{
#ifdef WITHALLOCATION
		_directLoss = pNode[nodeIdx]._S;
		for (i=0; i<_nAffectedNode; i++)
		{
			_directLoss -= pDirectLoss[_pAffectedNodeIdx[i]];
		}
#endif
		return true;
	}
	return false;
}

void Bucket::calcLoss(Node* currNode, Node* pNode, int* pNodeLevel)
{
	currNode->_D = currNode->_X = 0;
	for (int i=0; i<_nAffectedNode; i++)
	{
		if ( pNode[_pAffectedNodeIdx[i]]._isAffected )
		{
			pNode[_pAffectedNodeIdx[i]].calcLossOverlap(pNode, pNodeLevel);
			pNode[_pAffectedNodeIdx[i]]._isAffected = false;
		}
		currNode->_D += pNode[_pAffectedNodeIdx[i]]._D;
		currNode->_X += pNode[_pAffectedNodeIdx[i]]._X;
	}
	currNode->_pt2Func(currNode);
}
//////////////////////////////////////////////////////////////////////////////////////////
//OverlapInfo
OverlapInfo::OverlapInfo() :
_maxLossBucketIdx(0),
_D(0),
_X(0),
_R(0),
_nBucket(1),
_nAffectedBucket(0),
_nParent(0),
_pParentIdx(0)
{
	_pBucket = new Bucket[1];
	_pAffectedBuckedIdx = new int[1];
}

OverlapInfo::OverlapInfo(int nBucket) :
_maxLossBucketIdx(0),
_D(0),
_X(0),
_R(0),
_nBucket(nBucket),
_nAffectedBucket(0),
_nParent(0),
_pParentIdx(0)
{
	_pBucket = new Bucket[nBucket];
	_pAffectedBuckedIdx = new int[nBucket];
}

OverlapInfo::OverlapInfo(OverlapInfo* p) :
_maxLossBucketIdx(0),
_D(0),
_X(0),
_R(0),
_nBucket(p->_nBucket),
_nParent(p->_nParent),
_nAffectedBucket(0),
_pParentIdx(0)
{
	int i, j, nNode;
	_pBucket = new Bucket[_nBucket];
	_pAffectedBuckedIdx = new int[_nBucket];
	for (i=0; i<_nBucket; i++)
	{
		nNode = _pBucket[i]._nNode = p->_pBucket[i]._nNode;
		if ( nNode > 0 )
		{
			_pBucket[i]._pNodeIdx = new int[nNode];
			_pBucket[i]._pAffectedNodeIdx = new int[nNode];
			for (j=0; j<nNode; j++)
				_pBucket[i]._pNodeIdx[j] = p->_pBucket[i]._pNodeIdx[j];
		}
	}
	if ( _nParent > 0 )
	{
		_pParentIdx = new int[_nParent];
		for (i=0; i<_nParent; i++)
			_pParentIdx[i] = p->_pParentIdx[i];
	}
}

OverlapInfo::~OverlapInfo()
{
	DELETEARRAY(_pBucket);
	DELETEARRAY(_pParentIdx);
	DELETEARRAY(_pAffectedBuckedIdx);
}

void OverlapInfo::setAffectedBucketNodes(Node* pNode
#ifdef WITHALLOCATION
		, int nodeIdx, double* pDirectLoss
#endif
										 )
{
	_nAffectedBucket = 0;
	for (int i=0; i<_nBucket; i++)
	{
		if ( _pBucket[i].setAffectedNode(pNode
#ifdef WITHALLOCATION
		, nodeIdx, pDirectLoss
#endif
			) )
		{
			_pAffectedBuckedIdx[_nAffectedBucket++] = i;
		}
	}
	if ( _nAffectedBucket == 0 )
		_pAffectedBuckedIdx[_nAffectedBucket++] = 0;
}

void OverlapInfo::calcLoss(Node* currNode, Node* pNode, int* pNodeLevel)
{
	_maxLossBucketIdx = _pAffectedBuckedIdx[0];
	_pBucket[_pAffectedBuckedIdx[0]].calcLoss(currNode, pNode, pNodeLevel);
	if ( _nAffectedBucket > 1 )
	{
		_R = currNode->getR();
		_D = currNode->_D;
		_X = currNode->_X;
		double R;
		int i, j, idxCurr, idxSaved;
		for (i=1; i<_nAffectedBucket; i++)
		{
			_pBucket[_pAffectedBuckedIdx[i]].calcLoss(currNode, pNode, pNodeLevel);
			R = currNode->getR();
			if ( R > _R )
			{
				_maxLossBucketIdx = _pAffectedBuckedIdx[i];
				_R = R;
				_D = currNode->_D;
				_X = currNode->_X;
			}
			else if ( _R == R )
			{
				if ( _pBucket[_pAffectedBuckedIdx[i]]._nAffectedNode < _pBucket[_maxLossBucketIdx]._nAffectedNode )
				{
					_maxLossBucketIdx = _pAffectedBuckedIdx[i];
					_R = R;
					_D = currNode->_D;
					_X = currNode->_X;
				}
				else if ( _pBucket[_pAffectedBuckedIdx[i]]._nAffectedNode == _pBucket[_maxLossBucketIdx]._nAffectedNode )
				{
					double scoreCurr = 0;
					double scoreSaved = 0;
					for (j=0; j<_pBucket[_pAffectedBuckedIdx[i]]._nAffectedNode; j++)
					{
						idxCurr = _pBucket[_pAffectedBuckedIdx[i]]._pAffectedNodeIdx[j];
						scoreCurr += pNode[idxCurr].getR() * pNodeLevel[idxCurr];
						idxSaved = _pBucket[_maxLossBucketIdx]._pAffectedNodeIdx[j];
						scoreSaved += pNode[idxSaved].getR() * pNodeLevel[idxSaved];
					}
					if ( scoreCurr > scoreSaved )
					{
						_maxLossBucketIdx = _pAffectedBuckedIdx[i];
						_R = R;
						_D = currNode->_D;
						_X = currNode->_X;
					}
					else if ( scoreCurr == scoreSaved )
					{
						idxCurr = idxSaved = 0;
						for (j=0; j<_pBucket[_pAffectedBuckedIdx[i]]._nAffectedNode; j++)
						{
							idxCurr += _pBucket[_pAffectedBuckedIdx[i]]._pAffectedNodeIdx[j];
							idxSaved += _pBucket[_maxLossBucketIdx]._pAffectedNodeIdx[j];
						}
						if ( idxCurr > idxSaved )
						{
							_maxLossBucketIdx = _pAffectedBuckedIdx[i];
							_R = R;
							_D = currNode->_D;
							_X = currNode->_X;
						}
					}
				}
			}
		}
		currNode->_D = _D;
		currNode->_X = _X;
	}
}
#endif

///////////////////////////////////////////////////////////////////////////
//NodePercentData
NodePercentData::NodePercentData() :
_GSL(-1),
_MIND(-1),
_MINDA(-1),
_MAXD(-1),
_NSL(-1)
{
}

void NodePercentData::adjustTerm(NodeData* pData, double value)
{
	if ( _GSL != -1 )
	{
		setItemSmaller(pData->_GSL, _GSL*value);
	}
	if ( _MIND != -1 )
	{
		setItemLarger(pData->_MIND, _MIND*value);
	}
	if ( _MINDA != -1 )
	{
		setItemLarger(pData->_MINDA, _MINDA*value);
	}
	if ( _MAXD != -1 )
	{
		setItemSmaller(pData->_MAXD, _MAXD*value);
	}
	if ( _NSL != -1 )
	{
		setItemSmaller(pData->_NSL, _NSL*value);
	}
}
//////////////////////////////////////////////////////////////////////////////////////////
//NodeData
NodeData::NodeData() :
_calcType(-1),
_parentIdx(-1),
_GSL(-1),
_MIND(-1),
_MINDA(-1),
_MAXD(-1),
_NSL(-1),
#ifdef PECENTAFFECTED
_pOriginalData(0),
#endif
_pPercentDataC(0),
_pPercentDataA(0)
{
}

#ifdef PECENTAFFECTED
void NodeData::saveOriginalTerm()
{
	_pOriginalData = new NodePercentData();
	_pOriginalData->_GSL = _GSL;
	_pOriginalData->_MIND = _MIND;
	_pOriginalData->_MINDA = _MINDA;
	_pOriginalData->_MAXD = _MAXD;
	_pOriginalData->_NSL = _NSL;
}
#endif

/*
NodeData::NodeData(NodeData* p) :
_calcType(p->_calcType),
_parentIdx(p->_parentIdx),
_GSL(p->_GSL),
_MIND(p->_MIND),
_MINDA(p->_MINDA),
_MAXD(p->_MAXD),
_NSL(p->_NSL),
_MINDP(p->_MINDP),
_MAXDP(p->_MAXDP),
_MINDPA(p->_MINDPA)
{
}
*/
void NodeData::adjustByExchangeRate(double rate)
{
	if ( _GSL != -1 )
		_GSL *= rate;
	if ( _MIND != -1 )
		_MIND *= rate;
	if ( _MINDA != -1 )
		_MINDA *= rate;
	if ( _MAXD != -1 )
		_MAXD *= rate;
	if ( _NSL != -1 )
		_NSL *= rate;
}

void NodeData::setItem(int key, double v)
{
	switch( key )
	{
	case NODE_GSL:
		_GSL = v;
		break;
	case NODE_GSLPC:
		if ( _pPercentDataC == 0 )
			_pPercentDataC = new NodePercentData();
		_pPercentDataC->_GSL = v;
		break;
	case NODE_GSLPA:
		if ( _pPercentDataA == 0 )
			_pPercentDataA = new NodePercentData();
		_pPercentDataA->_GSL = v;
		break;
	case NODE_MIND:
		_MIND = v;
		break;
	case NODE_MINDPC:
		if ( _pPercentDataC == 0 )
			_pPercentDataC = new NodePercentData();
		_pPercentDataC->_MIND = v;
		break;
	case NODE_MINDPA:
		if ( _pPercentDataA == 0 )
			_pPercentDataA = new NodePercentData();
		_pPercentDataA->_MIND = v;
		break;
	case NODE_MINDA:
		_MINDA = v;
		break;
	case NODE_MINDAPC:
		if ( _pPercentDataC == 0 )
			_pPercentDataC = new NodePercentData();
		_pPercentDataC->_MINDA = v;
		break;
	case NODE_MINDAPA:
		if ( _pPercentDataA == 0 )
			_pPercentDataA = new NodePercentData();
		_pPercentDataA->_MINDA = v;
		break;
	case NODE_MAXD:
		_MAXD = v;
		break;
	case NODE_MAXDPC:
		if ( _pPercentDataC == 0 )
			_pPercentDataC = new NodePercentData();
		_pPercentDataC->_MAXD = v;
		break;
	case NODE_MAXDPA:
		if ( _pPercentDataA == 0 )
			_pPercentDataA = new NodePercentData();
		_pPercentDataA->_MAXD = v;
		break;
	case NODE_NSL:
		_NSL = v;
		break;
	case NODE_NSLPC:
		if ( _pPercentDataC == 0 )
			_pPercentDataC = new NodePercentData();
		_pPercentDataC->_NSL = v;
		break;
	case NODE_NSLPA:
		if ( _pPercentDataA == 0 )
			_pPercentDataA = new NodePercentData();
		_pPercentDataA->_NSL = v;
		break;
	}
}

void NodeData::resetValues()
{
	_GSL = _MIND = _MINDA = _MAXD = _NSL = -1;
	_pPercentDataC = _pPercentDataA = 0;
	_calcType = 0;
}

void NodeData::adjustTermByPecentCover(double value)
{
	_pPercentDataC->adjustTerm(this, value);
	DELETEOBJECT(_pPercentDataC);
}

#ifdef PECENTAFFECTED
void NodeData::adjustTermByPecentAffected(double value)
{
	_GSL = _pOriginalData->_GSL;
	_MIND = _pOriginalData->_MIND;
	_MINDA = _pOriginalData->_MINDA;
	_MAXD = _pOriginalData->_MAXD;
	_NSL = _pOriginalData->_NSL;
	_pPercentDataA->adjustTerm(this, value);
}
#endif
//////////////////////////////////////////////////////////////////////////////
//CDLFuncData
CDLFuncData::CDLFuncData() :
_calculated(false),
_relatedNode(0),
_threshold(-1),
_relatedNodeIdx(-1)
{
}

//////////////////////////////////////////////////////////////////////////////
//node calcLoss
Node::Node() :
#ifdef FOR_QA_TEST
_nodeIdx(0),
#endif
#ifdef IMPORTFUNCTION
_pCDLFuncData(0),
#endif
#ifdef OVERLAP
_pOverlapInfo(0),
#endif
_S(0),
_D(0),
_X(0),
_isAffected(false),
_pData(0),
_nChild(0),
_pChildIdx(0)
{
}

Node::~Node()
{
}

void Node::ClearMemory(int nObjCreated)
{
	if ( nObjCreated == 0 )
		DELETEOBJECT(_pData);

#ifdef IMPORTFUNCTION
	DELETEOBJECT(_pCDLFuncData);
#endif

#ifdef OVERLAP
	if ( _pOverlapInfo )
	{
		DELETEOBJECT(_pOverlapInfo);
	}
	else
	{
		DELETEARRAY(_pChildIdx);
	}
#else
	DELETEARRAY(_pChildIdx);
#endif
}

void Node::calcTreatyLoss(double inLoss)
{
	_S = inLoss;
	_D = _X = 0;
	_pt2Func(this);
}

void Node::reInitial()
{
	_S = 0;//_D, _X will be set at the beginning of the function calcLoss
	_isAffected = false;

#ifdef IMPORTFUNCTION
	if ( _pCDLFuncData )
	{
		_D = 0;
		_pCDLFuncData->_calculated = false;
	}
#endif
}

void Node::adjustByExchangeRate(double rate)
{
	_pData->adjustByExchangeRate(rate);
}

void Node::readCommonInfo(FILE* pFile, int sizeI, Node* pNode)
{
	int key;
	double v;
	//read calculation function type
	setCalcFuntionType(pFile, pNode);
	setFuncPointer();

	//read the key for item in the node
	fread(&key, sizeI, 1, pFile);
	//key == NODE_ISPERRISK is the last field for each node
	while ( key != NODE_ISPERRISK )
	{//key is one of all term types
		fread(&v, sizeof(double), 1, pFile);
		setItem(key, v);
		fread(&key, sizeI, 1, pFile);
	}
#ifdef PECENTAFFECTED
	_pData->saveOriginalTerm();
#endif
//	fread(&key, sizeI, 1, pFile);
	//key is NODE_ISPERRISK new
}

#ifdef OVERLAP
void Node::readOverlapInput(FILE* pFile, int sizeI, Node* pNode)
{
	int i, j, nBucket, nNode;
	fread(&nBucket, sizeI, 1, pFile);
	_pOverlapInfo = new OverlapInfo(nBucket);
	for (i=0; i<nBucket; i++)
	{
		fread(&nNode, sizeI, 1, pFile);
		if ( nNode > 0 )
		{
			_pOverlapInfo->_pBucket[i]._pNodeIdx = new int[nNode];
			_pOverlapInfo->_pBucket[i]._pAffectedNodeIdx = new int[nNode];
			_pOverlapInfo->_pBucket[i]._nNode = nNode;
			for (j=0; j<nNode; j++)
				fread(&(_pOverlapInfo->_pBucket[i]._pNodeIdx[j]), sizeI, 1, pFile);
		}
	}
	//read number of parent
	fread(&nNode, sizeI, 1, pFile);
	if ( nNode == 1 )
	{
		fread(&(_pData->_parentIdx), sizeI, 1, pFile);
	}
	else
	{
		_pOverlapInfo->_nParent = nNode;
		_pOverlapInfo->_pParentIdx = new int[nNode];
		for (i=0; i<nNode; i++)
		{
			fread(&(_pOverlapInfo->_pParentIdx[i]), sizeI, 1, pFile);
		}
	}
	//read child indices
	fread(&_nChild, sizeI, 1, pFile);
	readCommonInfo(pFile, sizeI, pNode);
}
#endif

void Node::readNonOverlapInput(FILE* pFile, int sizeI, Node* pNode)
{
	//read the parent index
	fread(&(_pData->_parentIdx), sizeI, 1, pFile);
	fread(&_nChild, sizeI, 1, pFile);
	if ( _nChild > 0 )
		_pChildIdx = new int[_nChild];
	readCommonInfo(pFile, sizeI, pNode);
}

void Node::setFuncPointer()
{
	if ( _pData->_calcType < NUM_BASIC_CALC_TYPE && _nChild == 0 )
		setFuncForLeafNode();
	else
		setFuncForNonLeafNode();
}

void Node::addSubjectLoss(double S, Node* pNode
#ifdef PECENTAFFECTED
						  , int currIdx, double TIV, double* pNodeTotalTIV
#endif
						  )
{
	_S += S;
#ifdef PECENTAFFECTED
	pNodeTotalTIV[currIdx] += TIV;
#endif

	if ( _pData->_parentIdx != -1 )
	{
		pNode[_pData->_parentIdx].addSubjectLoss(S, pNode
#ifdef PECENTAFFECTED
			, _pData->_parentIdx, TIV, pNodeTotalTIV
#endif
);
	}
}

void Node::setLossWithChildIdx(int currIdx, double loss, int childIdx, Node* pNode
#ifdef PECENTAFFECTED
						  , double TIV, double* pNodeTotalTIV
#endif
							   )
{
#ifdef PECENTAFFECTED
	pNodeTotalTIV[currIdx] += TIV;
#endif

	if ( _isAffected )
	{
		_S += loss;
		_pChildIdx[_nChild++] = childIdx;
		if ( _pData->_parentIdx != -1 )
		{
			pNode[_pData->_parentIdx].addSubjectLoss(loss, pNode
#ifdef PECENTAFFECTED
				, _pData->_parentIdx, TIV, pNodeTotalTIV
#endif
				);
		}
	}
	else
	{
		_S = loss;
		_pChildIdx[0] = childIdx;
		_nChild = 1;
		if ( _pData->_parentIdx != -1 )
		{
			pNode[_pData->_parentIdx].setLossWithChildIdx(_pData->_parentIdx, loss, currIdx, pNode
#ifdef PECENTAFFECTED
				, TIV, pNodeTotalTIV
#endif
				);
		}
		_isAffected = true;
	}
}

void Node::setSubjectLoss(int currIdx, double loss, Node* pNode
#ifdef PECENTAFFECTED
						  , double TIV, double* pNodeTotalTIV
#endif
						  )
{
	if ( _isAffected )
		addSubjectLoss(loss, pNode
#ifdef PECENTAFFECTED
		, currIdx, TIV, pNodeTotalTIV
#endif		
		);
	else
	{
		_S = loss;
		_isAffected = true;
		_nChild = 0;
#ifdef PECENTAFFECTED
		pNodeTotalTIV[currIdx] += TIV;
#endif
		if ( _pData->_parentIdx != -1 )
			pNode[_pData->_parentIdx].setLossWithChildIdx(_pData->_parentIdx, loss, currIdx, pNode
#ifdef PECENTAFFECTED
		, TIV, pNodeTotalTIV
#endif
			);
	}
}

#ifdef OVERLAP
void Node::setGuLossWithOverlap(double loss, Node* pNode, bool* pGroundUpLossIsSet
#ifdef PECENTAFFECTED
						  , double TIV, double* pNodeTotalTIV
#endif
								)
{
	_isAffected = true;
	
	int i, idx, nParent = _pOverlapInfo->_nParent;
	if ( nParent == 0 )
	{
		idx = _pData->_parentIdx;
		if ( idx != -1 )
		{
			if ( pGroundUpLossIsSet[idx] == false )
			{	
				pNode[idx]._S += loss;
				pNode[idx].setGuLossWithOverlap(loss, pNode, pGroundUpLossIsSet
#ifdef PECENTAFFECTED
					, TIV, pNodeTotalTIV
#endif
					);
				pGroundUpLossIsSet[idx] = true;
#ifdef PECENTAFFECTED
				pNodeTotalTIV[idx] += TIV;
#endif
			}
		}
	}
	else
	{
		for (i=0; i<nParent; i++)
		{
			idx = _pOverlapInfo->_pParentIdx[i];
			if ( idx != -1 )
			{
				if ( pGroundUpLossIsSet[idx] == false )
				{	
					pNode[idx]._S += loss;
					pNode[idx].setGuLossWithOverlap(loss, pNode, pGroundUpLossIsSet
	#ifdef PECENTAFFECTED
							  , TIV, pNodeTotalTIV
	#endif
						);
					pGroundUpLossIsSet[idx] = true;
	#ifdef PECENTAFFECTED
					pNodeTotalTIV[idx] += TIV;
	#endif
				}
			}
		}
	}
}
#endif

#ifdef PECENTAFFECTED
void Node::convertAffectedPecent2Amount(double value)
{
	if ( !_isAffected || _pData->_pPercentDataA==0 || value<ONECENT )
		return;

	_pData->adjustTermByPecentAffected(value);
}
#endif

#ifdef PERRISK
void Node::copyForPerrisk(Node& node)
{
	_pData = node._pData;
	node._pData = 0;
	_nChild = node._nChild;
	_pChildIdx = node._pChildIdx;
	node._pChildIdx = 0;
	_pt2Func = node._pt2Func;

#ifdef FOR_QA_TEST
	_nodeIdx = node._nodeIdx;
#endif

#ifdef IMPORTFUNCTION
	_pCDLFuncData = node._pCDLFuncData;
	node._pCDLFuncData = 0;
#endif

#ifdef OVERLAP
	_pOverlapInfo = node._pOverlapInfo;
	node._pOverlapInfo = 0;
#endif
}

//for new created node
void Node::setDataForResolution(int parentIdx, Node& n)
{
	_pData = new NodeData(n._pData);
	_pData->_parentIdx = parentIdx;
	_pt2Func = n._pt2Func;
#ifdef OVERLAP
	if ( n._pOverlapInfo )
	{
		_pOverlapInfo = new OverlapInfo();
	}
#endif
}

//for per risk node
void Node::resetDataForResolution(int nMoreChild
#ifdef OVERLAP
	, int* pMoreChildIdx
#endif
	)
{
	//reset child information
	if ( _nChild > 0 )
	{
		delete [] _pChildIdx;
		_nChild += nMoreChild;
	}
	else
		_nChild = nMoreChild;
	//reset _pData
	_pData->resetValues();
	setFuncForNonLeafNode();
#ifdef OVERLAP
	if ( _pOverlapInfo )
	{
		int i, j, k, nNode, *pTmp;
		for (i=0; i<_pOverlapInfo->_nBucket; i++)
		{
			nNode = _pOverlapInfo->_pBucket[i]._nNode;
			_pOverlapInfo->_pBucket[i]._nNode += nMoreChild;
			pTmp = _pOverlapInfo->_pBucket[i]._pNodeIdx;
			_pOverlapInfo->_pBucket[i]._pNodeIdx = new int[_pOverlapInfo->_pBucket[i]._nNode];
			for (j=0; j<nNode; j++)
				_pOverlapInfo->_pBucket[i]._pNodeIdx[j] = pTmp[j];
			for (k=0; k<nMoreChild; k++,j++)
				_pOverlapInfo->_pBucket[i]._pNodeIdx[j] = pMoreChildIdx[k];
			delete [] pTmp;
		}
	}
	else
	{
		_pChildIdx = new int[_nChild];
	}
#else
	_pChildIdx = new int[_nChild];
#endif
}
#endif

void Node::calcLoss(Node* pNode)
{
#ifdef IMPORTFUNCTION
	if ( _pCDLFuncData && _pCDLFuncData->_calculated )
		return;
#endif
	_D = _X = 0;
	for (int i=0; i<_nChild; i++)
	{
		pNode[_pChildIdx[i]].calcLoss(pNode);
		pNode[_pChildIdx[i]]._isAffected = false;
		_D += pNode[_pChildIdx[i]]._D;
		_X += pNode[_pChildIdx[i]]._X;
	}
#ifdef IMPORTFUNCTION
	if ( _pCDLFuncData && _pCDLFuncData->_relatedNode )
		_pCDLFuncData->_relatedNode->calcLoss(pNode);
#endif
	_pt2Func(this);
#ifdef IMPORTFUNCTION
	if ( _pCDLFuncData )
		_pCDLFuncData->_calculated = true;
#endif
}

void Node::CreateData()
{
	_pData = new NodeData();
}

void Node::setItem(int key, double v)
{
	_pData->setItem(key, v);
}

void Node::setCalcFuntionType(FILE* pFile, Node* pNode)
{
	fread(&(_pData->_calcType), sizeof(int), 1, pFile);
#ifdef IMPORTFUNCTION
	if ( _pData->_calcType == CLT_FUNCTION_TYPE )
	{
		int isPecentThrehold = 0;
		if ( !_pCDLFuncData )
			_pCDLFuncData = new CDLFuncData();
		fread(&(_pCDLFuncData->_threshold), sizeof(double), 1, pFile);
		fread(&(_pCDLFuncData->_relatedNodeIdx), sizeof(int), 1, pFile);
//		fread(&(isPecentThrehold), sizeof(int), 1, pFile);
		_pCDLFuncData->_relatedNode = &(pNode[_pCDLFuncData->_relatedNodeIdx]);
		if ( _pCDLFuncData->_relatedNode->_pCDLFuncData == 0 )
		{
			_pCDLFuncData->_relatedNode->_pCDLFuncData = new CDLFuncData();
			_pCDLFuncData->_relatedNode->_pCDLFuncData->_relatedNodeIdx = -1;
		}
	}
#endif
}

#ifdef OVERLAP
int Node::setOneBucket(int index, Node* pNode)
{
	int nNode = _pOverlapInfo->_pBucket[index]._nNode;
	int* pNodeIdx = _pOverlapInfo->_pBucket[index]._pNodeIdx;
	int j, nAffectedNode = 0;
	for (j=0; j<nNode; j++)
	{
		if ( pNode[pNodeIdx[j]]._isAffected )
		{
			_pOverlapInfo->_pBucket[index]._pAffectedNodeIdx[nAffectedNode] = pNodeIdx[j];
			nAffectedNode++;
		}
	}
	return nAffectedNode;
}

void Node::setAffectedChild(Node* pNode)
{
	int k;
	int nNode = setOneBucket(0, pNode);
	if ( nNode > 0 )
	{
		_pOverlapInfo->_pBucket[0]._nAffectedNode = nNode;
		for (k=0; k<nNode; k++)
			_S += pNode[_pOverlapInfo->_pBucket[0]._pAffectedNodeIdx[k]]._S;
		_isAffected = true;
	
		for (k=1; k<_pOverlapInfo->_nBucket; k++)
			_pOverlapInfo->_pBucket[k]._nAffectedNode = setOneBucket(k, pNode);
	}
}

void Node::calcLossOverlap(Node* pNode, int* pNodeLevel)
{
#ifdef IMPORTFUNCTION
	if ( _pCDLFuncData && _pCDLFuncData->_calculated )
		return;
#endif

#ifdef IMPORTFUNCTION
	if ( _pCDLFuncData && _pCDLFuncData->_relatedNode )
		_pCDLFuncData->_relatedNode->calcLoss(pNode);
#endif

	_pOverlapInfo->calcLoss(this, pNode, pNodeLevel);

#ifdef IMPORTFUNCTION
		if ( _pCDLFuncData )
			_pCDLFuncData->_calculated = true;
#endif
}
#endif

void Node::copyForMultiThread(Node& node, Node* pNode)
{
	_pData = node._pData;
	_nChild = node._nChild;

#ifdef FOR_QA_TEST
	_nodeIdx = node._nodeIdx;
#endif

#ifdef IMPORTFUNCTION
	if ( node._pCDLFuncData )
	{
		_pCDLFuncData = new CDLFuncData();
		_pCDLFuncData->_threshold = node._pCDLFuncData->_threshold;
		_pCDLFuncData->_relatedNodeIdx = node._pCDLFuncData->_relatedNodeIdx;
		if ( _pCDLFuncData->_relatedNodeIdx != -1 )
		{
			_pCDLFuncData->_relatedNode = &(pNode[_pCDLFuncData->_relatedNodeIdx]);
		}
	}
#endif

#ifdef OVERLAP
	if ( node._pOverlapInfo )
		_pOverlapInfo = new OverlapInfo(node._pOverlapInfo);
#endif

	if (_nChild > 0)
	{
#ifdef OVERLAP
		if ( node._pOverlapInfo == 0 )
			_pChildIdx = new int[_nChild];
#else
		_pChildIdx = new int[_nChild];
#endif
		setFuncForNonLeafNode();
	}
	else
	{
#ifdef IMPORTFUNCTION
		if ( _pData->_calcType >= NUM_BASIC_CALC_TYPE )
			setFuncForNonLeafNode();
		else
#endif
		setFuncForLeafNode();
	}

	_isAffected = false;
}

void Node::calc_D_with_MIND()
{
	_D = MIND(_S, _pData->_MIND);
}

void Node::update_D_with_MIND()
{
	double a = MIND(_S, _pData->_MIND);
	if ( a > _D ) _D = a;
}

void Node::calc_D_with_MAXD()
{
	_D = MIND(_S, _pData->_MAXD);
}

void Node::update_D_with_MAXD()
{
	double a = MIND(_S, _pData->_MAXD);
	if ( a < _D ) _D = a;
}

void Node::calc_X_with_NSL()
{
	double n = _S - _D;
	if( n > _pData->_NSL ) _X = n - _pData->_NSL;
}

void Node::update_X_with_NSL()
{
	double n = _S - _D;
	if( n > _pData->_NSL )
	{ 
		n -= _pData->_NSL;
		if ( _X < n ) _X = n;
	}
}

void Node::calc_D_with_MINDA()
{
	double d1 = MIND(_S, _pData->_MINDA);
//	_D = MIND(d1-_X, _S-_X);
	_D = MAXD(0, d1-_X);
}

void Node::update_D_with_MINDA()
{
	double d1 = MIND(_S, _pData->_MINDA);
	double d2 = MAXD(_D, d1 - _X);
	_D = MIND(d2, _S-_X);
}

void Node::calc_X_with_GSL()
{
	if ( _S > _pData->_GSL )
		_X = _S - _pData->_GSL;
	_X = MIND(_X, _S-_D);
}

void Node::update_X_with_GSL()
{
	double a = _S - _pData->_GSL;
	if ( a > 0 )
	{
		if ( a > _X )
			_X = a;
		_X = MIND(_X, _S-_D);
	}
}

#ifdef FOR_QA_TEST
void Node::writeNodeLoss(FILE* pf, char* line, double R)
{
	if ( _S < ONECENT )
		return;
	int size;
	if ( R > ONECENT )
		size = sprintf_s(line, 1020, "%d,%f,%f,%f,%f\n", _nodeIdx, _S, _D, _X, R);
	else
		size = sprintf_s(line, 1020, "%d,%f,%f,%f\n", _nodeIdx, _S, _D, _X);
	fwrite(line, size, sizeof(char), pf);
}
#endif
/////////////////////////
bool Node::checkRA(int nodeIdx, double* pRA, double* pDirectRA, Node* pNode, int contractIdx)
{
	if ( _nChild <= 0 )
		return true;

#ifdef OVERLAP
	int nChildTmp = _nChild;
	int* pChildIdxTmp = _pChildIdx;
	_nChild = _pOverlapInfo->_pBucket[_pOverlapInfo->_maxLossBucketIdx]._nAffectedNode;
	_pChildIdx = _pOverlapInfo->_pBucket[_pOverlapInfo->_maxLossBucketIdx]._pAffectedNodeIdx;
#endif
	int i;
	double v = pDirectRA[nodeIdx];
	for (i=0; i<_nChild; i++)
		v += pRA[_pChildIdx[i]];
	if ( fabs(pRA[nodeIdx]-v) > ONECENT )
	{
		printf("contract(%d) node(%d) RA (%f) is not the same as the sum (%f) of its child RA\n", contractIdx, nodeIdx, pRA[nodeIdx], v);
		return false;
	}

	for (i=0; i<_nChild; i++)
	{
		if ( !pNode[_pChildIdx[i]].checkRA(_pChildIdx[i], pRA, pDirectRA, pNode, contractIdx) )
			return false;
	}
#ifdef OVERLAP
	_nChild = nChildTmp;
	_pChildIdx = pChildIdxTmp;
#endif
	return true;
}

double Node::getR()
{
	return _S - _X - _D;
}

void Node::addExpoValue(int index, double* pCoverValue, double value, Node* pNode
#ifdef OVERLAP
			, bool* pIsSet
#endif
			)
{
#ifdef OVERLAP
	if ( pIsSet[index] == false )
#endif
	pCoverValue[index] += value;
#ifdef OVERLAP
	if (_pOverlapInfo)
	{
		for (int i=0; i<_pOverlapInfo->_nParent; i++)
		{
			if ( _pOverlapInfo->_pParentIdx[i] != -1 )
			{
				pNode[_pOverlapInfo->_pParentIdx[i]].addExpoValue(_pOverlapInfo->_pParentIdx[i], pCoverValue, value, pNode, pIsSet);
			}
		}
	}
	else
	{
		if ( _pData->_parentIdx != -1 )
		{
			pNode[_pData->_parentIdx].addExpoValue(_pData->_parentIdx, pCoverValue, value, pNode, pIsSet);
		}
	}
#else
	if ( _pData->_parentIdx != -1 )
	{
		pNode[_pData->_parentIdx].addExpoValue(_pData->_parentIdx, pCoverValue, value, pNode);
	}
#endif
}

void Node::convertCoveredPecentToAmount(double value)
{
	if ( _pData->_pPercentDataC==0 || value<ONECENT )
		return;

	_pData->adjustTermByPecentCover(value);
}

void Node::recoverableAloc(double factor, double& DA, double& RA)
{
	DA = _D * factor;
	RA = _S - _X - DA;
}

//allocate recovery for its children
void Node::allocation(double DA, double RA, Node* pNode, int index, double* pDirectLoss, double* pDirectRA, double* pTmpD, double* pTmpR
#ifdef OVERLAP
					  , int nExposure, int* pExpoIdx, int* pExpoIdx2NodeIdx
#endif
					  )
{
	if ( _nChild == 0 || RA < ONECENT )
		return;

	int i, idx;
	
#ifdef OVERLAP
	int nChildTmp;
	if ( _pOverlapInfo )
	{
		nChildTmp = _nChild;
		_nChild = _pOverlapInfo->_pBucket[_pOverlapInfo->_maxLossBucketIdx]._nAffectedNode;
		_pChildIdx = _pOverlapInfo->_pBucket[_pOverlapInfo->_maxLossBucketIdx]._pAffectedNodeIdx;
#ifdef WITHALLOCATION
		pDirectLoss[index] = _pOverlapInfo->_pBucket[_pOverlapInfo->_maxLossBucketIdx]._directLoss;
#endif
	}
#endif	

	double factor, delta_D = DA;
	for (i=0; i<_nChild; i++)
		delta_D -= pNode[_pChildIdx[i]]._D;
	//calculate allocated deductible and temparary allocated recoverable
	if ( fabs(delta_D) >= ONECENT )
	{
		if ( delta_D > 0 )
		{
			double sum_child_R = pDirectLoss[index];//ground up loss from outside directly
			for (i=0; i<_nChild; i++)
			{
				idx = _pChildIdx[i];
				pTmpR[idx] = pNode[idx].getR();
				sum_child_R += pTmpR[idx];
			}

			factor = 1.0 - delta_D / sum_child_R;
			for (i=0; i<_nChild; i++)
			{
				idx = _pChildIdx[i];
				pTmpR[idx] *= factor;
				pTmpD[idx] = pNode[idx]._S - pNode[idx]._X - pTmpR[idx];
			}
			pDirectRA[index] = pDirectLoss[index] * factor;
		}
		else
		{
			factor = 1.0 + delta_D / (DA - delta_D);
			for (i=0; i<_nChild; i++)
			{
				idx = _pChildIdx[i];
				pNode[idx].recoverableAloc(factor, pTmpD[idx], pTmpR[idx]);
			}
			pDirectRA[index] = pDirectLoss[index];
		}
	}
	else
	{
		for (i=0; i<_nChild; i++)
		{
			idx = _pChildIdx[i];
			pTmpD[idx] =  pNode[idx]._D;
			pTmpR[idx] = pNode[idx].getR();
		}
		pDirectRA[index] = pDirectLoss[index];
	}

	//calculate allocated recoverable
	double sum_child_adjustR = pDirectRA[index];
	for (i=0; i<_nChild; i++)
		sum_child_adjustR += pTmpR[_pChildIdx[i]];
	factor = RA / sum_child_adjustR;
	for (i=0; i<_nChild; i++)
	{
		idx = _pChildIdx[i];
		pTmpR[idx] *= factor;
		pNode[idx].allocation(pTmpD[idx], pTmpR[idx], pNode, idx, pDirectLoss, pDirectRA, pTmpD, pTmpR
#ifdef OVERLAP
					  , nExposure, pExpoIdx, pExpoIdx2NodeIdx
#endif
			);
	}
	pDirectRA[index] *= factor;
#ifdef OVERLAP
	if ( _pOverlapInfo )
	{
		_nChild = nChildTmp;
	}
#endif
}

#ifdef OVERLAP
void Node::setInAllocationFlag(Node* pNode, bool* pIsInAllocationTree)
{
	int i, nNode = _pOverlapInfo->_pBucket[_pOverlapInfo->_maxLossBucketIdx]._nAffectedNode;
	int* pNodeIdx = _pOverlapInfo->_pBucket[_pOverlapInfo->_maxLossBucketIdx]._pAffectedNodeIdx;
	for (i=0; i<nNode; i++)
	{
		pIsInAllocationTree[pNodeIdx[i]] = true;
		pNode[pNodeIdx[i]].setInAllocationFlag(pNode, pIsInAllocationTree);
	}
}

void Node::resetExposure2NodeIdx(int nodeStartIdx, Node* pNode, bool* pIsInAllocationTree, int nExposure, int* pExpoIdx, int* pExpoIdx2NodeIdx)
{
	int i, nodeIdx, newNodeIdx, nNode = _pOverlapInfo->_pBucket[_pOverlapInfo->_maxLossBucketIdx]._nAffectedNode;
	int* pNodeIdx = _pOverlapInfo->_pBucket[_pOverlapInfo->_maxLossBucketIdx]._pAffectedNodeIdx;
	for (i=0; i<nNode; i++)
	{
		pIsInAllocationTree[pNodeIdx[i]] = true;
		pNode[pNodeIdx[i]].setInAllocationFlag(pNode, pIsInAllocationTree);
	}

	for (i=0; i<nExposure; i++)
	{
		nodeIdx = pExpoIdx2NodeIdx[pExpoIdx[i]] - nodeStartIdx;
		if ( pIsInAllocationTree[pExpoIdx2NodeIdx[nodeIdx]] == false )
		{
			//find its first parent (including indirect parent) which is in the allocation tree
			newNodeIdx = findParentInAllocationTree(pIsInAllocationTree, pNode);
			if ( newNodeIdx == -1 )
			{
				printf("internel error to find the first parent which is in the allocation tree for the node (%d)", nodeIdx);
				return;
			}
			//reset the node index for this input loss
			pExpoIdx2NodeIdx[pExpoIdx[i]] = newNodeIdx + nodeStartIdx;
		}
	}
}

int Node::findParentInAllocationTree(bool* pIsInAllocationTree, Node* pNode)
{
	int i, indexFound = -1;
	int idx, nParent = _pOverlapInfo->_nParent;
	if ( nParent == 0 )
	{
		idx = _pData->_parentIdx;
		if (idx == -1)
			return -1;
		if ( pIsInAllocationTree[idx] )
			return idx;
		return indexFound = pNode[idx].findParentInAllocationTree(pIsInAllocationTree, pNode);
	}
	
	for (i=0; i<nParent; i++)
	{
		idx = _pOverlapInfo->_pParentIdx[i];
		if (idx != -1)
		{
			if ( pIsInAllocationTree[idx] )
				return idx;
			indexFound = pNode[idx].findParentInAllocationTree(pIsInAllocationTree, pNode);
		}
	}
	return indexFound;
}
#endif

void Node::allocatioForDQ(double RA, Node* pNode, int index, double* pDirectLoss, double* pRA)
{
	if ( _nChild == 0 || RA < ONECENT )
		return;

#ifdef OVERLAP
	int nChildTmp;
	if ( _pOverlapInfo )
	{
		nChildTmp = _nChild;
		_nChild = _pOverlapInfo->_pBucket[_pOverlapInfo->_maxLossBucketIdx]._nAffectedNode;
		_pChildIdx = _pOverlapInfo->_pBucket[_pOverlapInfo->_maxLossBucketIdx]._pAffectedNodeIdx;
	}
#endif	

	int i, idx;
	double factor, totalChildLoss = pDirectLoss[index];;
	for (i=0; i<_nChild; i++)
		totalChildLoss += pNode[_pChildIdx[i]]._S;
	
	factor = RA / totalChildLoss;
	for (i=0; i<_nChild; i++)
	{
		idx = _pChildIdx[i];
		pRA[idx] = factor * pNode[idx]._S;
		pNode[idx].allocatioForDQ(pRA[idx], pNode, idx, pDirectLoss, pRA);
	}
	pRA[index] = pDirectLoss[index] * factor;
#ifdef OVERLAP
	if ( _pOverlapInfo )
	{
		_nChild = nChildTmp;
	}
#endif
}

void Node::payoutAlloc(double P, int nChild, int* pChildIdx, double* pRA, double* pPA, Node* pNode,
	int index, double* pDirectRA, double* pDirectPA)
{
	if ( nChild == 0 || P < ONECENT )
		return;

	int i, idx;
	double sum_RecoverableA = 0;
	for (i = 0; i<nChild; i++)
	{
		sum_RecoverableA += pRA[pChildIdx[i]];
	}
	if ( index != -1 )
		sum_RecoverableA += pDirectRA[index];

	double P1, factor = P / sum_RecoverableA;
	for (i = 0; i<nChild; i++)
	{
		idx = pChildIdx[i];
		P1 = pRA[idx] * factor;
		pPA[idx] += P1;
		pNode[idx].payoutAloc(P1, idx, pRA, pPA, pNode, pDirectRA, pDirectPA);
	}
	if ( index != -1 )
	{
		pDirectPA[index] += pDirectRA[index] * factor;
	}
}

void Node::payoutAloc(double P, int index, double* pRA, double* pPA, Node* pNode, double* pDirectRA, double* pDirectPA)
{
#ifdef OVERLAP
	if ( _pOverlapInfo )
		payoutAlloc(P, _pOverlapInfo->_pBucket[_pOverlapInfo->_maxLossBucketIdx]._nAffectedNode,
			_pOverlapInfo->_pBucket[_pOverlapInfo->_maxLossBucketIdx]._pAffectedNodeIdx,pRA, pPA, pNode, index, pDirectRA, pDirectPA);
	else
		payoutAlloc(P, _nChild, _pChildIdx, pRA, pPA, pNode, index, pDirectRA, pDirectPA);
#else
	payoutAlloc(P, _nChild, _pChildIdx, pRA, pPA, pNode, index, pDirectRA, pDirectPA);
#endif	
}

