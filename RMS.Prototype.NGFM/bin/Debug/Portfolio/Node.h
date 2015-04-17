# pragma once

#include "stdio.h"
#include "constant.h"

class Node;
typedef void(*pt2NodeFunc)(Node*);

#ifdef OVERLAP
struct Bucket
{
	int _nNode;
	int* _pNodeIdx;
	int _nAffectedNode;
	int* _pAffectedNodeIdx;
#ifdef WITHALLOCATION
	double _directLoss;
#endif

	Bucket();
	~Bucket();
	bool setAffectedNode(Node* pNode
#ifdef WITHALLOCATION
		, int nodeIdx, double* pDirectLoss
#endif
		);
	void calcLoss(Node* currNode, Node* pNode, int* pNodeLevel);
};

struct OverlapInfo
{
	int _maxLossBucketIdx;
	double _D;
	double _X;
	double _R;
	int     _nBucket;
	Bucket* _pBucket;
	int _nAffectedBucket;
	int* _pAffectedBuckedIdx;

	int  _nParent;
	int* _pParentIdx;

	OverlapInfo();
	OverlapInfo(int nBucket);
	OverlapInfo(OverlapInfo* p);
	~OverlapInfo();

	void calcLoss(Node* currNode, Node* pNode, int* pNodeLevel);
	void setAffectedBucketNodes(Node* pNode
#ifdef WITHALLOCATION
		, int nodeIdx, double* pDirectLoss
#endif
		);
};
#endif

struct NodeData;
struct NodePercentData
{
	double _GSL;
	double _MIND;
	double _MINDA;
	double _MAXD;
	double _NSL;

	NodePercentData();
	void adjustTerm(NodeData* pData, double value);
};

struct NodeData
{
	int  _calcType;
	int  _parentIdx;

	double _GSL;
	double _MIND;
	double _MINDA;
	double _MAXD;
	double _NSL;

	NodePercentData* _pPercentDataC;
	NodePercentData* _pPercentDataA;
#ifdef PECENTAFFECTED
	NodePercentData* _pOriginalData;
	void saveOriginalTerm();
	void adjustTermByPecentAffected(double value);
#endif
	void adjustTermByPecentCover(double value);

	NodeData();
//	NodeData(NodeData* p);
	void adjustByExchangeRate(double rate);
	void setItem(int key, double v);
	void resetValues();
};

struct CDLFuncData
{
	bool   _calculated;
	Node* _relatedNode;
	double _threshold;
	int  _relatedNodeIdx;

	CDLFuncData();
	~CDLFuncData() {};
};

class Node
{
public:
	static void payoutAlloc(double P, int nChild, int* pChildIdx, double* pRA, double* pPA, Node* pNode,
		int index, double* pDirectRA, double* pDirectPA);

	Node();
	~Node();
	//delete _pData when deleting the last Calculation (portfolio) object
	void ClearMemory(int nObjCreated);

	void calcTreatyLoss(double inLoss);

	//functions called by Calculation::initialize
	void CreateData();
	void readOverlapInput(FILE* pFile, int sizeI, Node* pNode);
	void readNonOverlapInput(FILE* pFile, int sizeI, Node* pNode);
#ifdef PERRISK
	void copyForPerrisk(Node& node);
	void setDataForResolution(int parentIdx, Node& n);
	void resetDataForResolution(int nMoreChild
#ifdef OVERLAP
	, int* pMoreChildIdx
#endif
		);
#endif
	void adjustByExchangeRate(double rate);
	void copyForMultiThread(Node& node, Node* pNode);
	//end of functions called by Calculation::initialize

	//called for each subevent loss calculation
	//reset values
	void reInitial();

	//find all affected nodes and set ground up loss for those nodes
	void setSubjectLoss(int currIdx, double loss, Node* pNode
#ifdef PECENTAFFECTED
						  , double TIV, double* pNodeTotalTIV
#endif
		);
#ifdef OVERLAP
	void setGuLossWithOverlap(double loss, Node* pNode, bool* pGroundUpLossIsSet
#ifdef PECENTAFFECTED
						  , double TIV, double* pNodeTotalTIV
#endif
		);
#endif

#ifdef FOR_QA_TEST
	int _nodeIdx;
	void writeNodeLoss(FILE* pf, char* line, double R);
#endif

#ifdef IMPORTFUNCTION
	CDLFuncData* _pCDLFuncData;
#endif

	double _S;
	double _D;
	double _X;

	NodeData* _pData;

	bool _isAffected;
	pt2NodeFunc _pt2Func;

#ifdef OVERLAP
	OverlapInfo* _pOverlapInfo;
	void setAffectedChild(Node* pNode);
	void calcLossOverlap(Node* pNode, int* pNodeLevel);
	void resetExposure2NodeIdx(int nodeStartIdx, Node* pNode, bool* pIsInAllocationTree, int nExposure, int* pExpoIdx, int* pExpoIdx2NodeIdx);
	void setInAllocationFlag(Node* pNode, bool* pIsInAllocationTree);
	int findParentInAllocationTree(bool* pIsInAllocationTree, Node* pNode);
#endif

	void calcLoss(Node* pNode);

	void calc_D_with_MIND();
	void update_D_with_MIND();

	void calc_D_with_MAXD();
	void update_D_with_MAXD();

	void calc_X_with_NSL();
	void update_X_with_NSL();

	void calc_D_with_MINDA();
	void update_D_with_MINDA();

	void calc_X_with_GSL();
	void update_X_with_GSL();

	void allocation(double DA, double RA, Node* pNode, int index, double* pDirectLoss, double* pDirectRA, double* pTmpD, double* pTmpR
#ifdef OVERLAP
					  , int nExposure, int* pExpoIdx, int* pExpoIdx2NodeIdx
#endif
		);
	void payoutAloc(double P, int index, double* pRA, double* pPA, Node* pNode, double* pDirectRA, double* pDirectPA);
	bool isLeaf() { return _nChild == 0 ? true : false; }
	
	void allocatioForDQ(double RA, Node* pNode, int index, double* pDirectLoss, double* pRA);

	double getR();

	void addExpoValue(int index, double* pCoverValue, double value, Node* pNode
#ifdef OVERLAP
			, bool* pIsSet
#endif
			);
	void convertCoveredPecentToAmount(double value);
	void convertAffectedPecent2Amount(double value);

private:
	void readCommonInfo(FILE* pFile, int sizeI, Node* pNode);
	void setItem(int key, double v);
	void setCalcFuntionType(FILE* pFile, Node* pNode);

	void setFuncPointer();
	void setFuncForNonLeafNode();
	void setFuncForLeafNode();

	void addSubjectLoss(double loss, Node* pNode
#ifdef PECENTAFFECTED
						  , int currIdx, double TIV, double* pNodeTotalTIV
#endif
		);
	void setLossWithChildIdx(int currIdx, double loss, int childIdx, Node* pNode
#ifdef PECENTAFFECTED
						  , double TIV, double* pNodeTotalTIV
#endif
		);

	void recoverableAloc(double factor, double& DA, double& RA);

	int  _nChild;
	int* _pChildIdx;
#ifdef OVERLAP
	//set affected node in one bucket
	int setOneBucket(int index, Node* pNode);
#endif

	//for testing code
	bool checkRA(int nodeIdx, double* pRA, double* pDirectRA, Node* pNode, int contractIdx);
};

