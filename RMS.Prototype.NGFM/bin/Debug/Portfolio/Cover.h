#ifndef COVER_H
#define COVER_H

#include "Node.h"

class Cover;
typedef void(*pt2CoverFunc)(Cover*);

struct CoverPecentData
{
	double _share;
	double _limit;
	double _attach;
	CoverPecentData();
};

struct CoverData
{
	double _share;
	double _limit;
	double _attach;
	
	int _nChild;
	int* _pChildIdx;

	CoverPecentData* _pPecentCover;
	CoverPecentData* _pPecentAffected;

#ifdef MULTILAVELCOVER
	double _pay;
#ifdef PECENTAFFECTED
	double _payp;
#endif
#endif

	CoverData();
	void clearMemory();
	void adjustByExchangeRate(double rate);
};

#ifdef MULTILAVELCOVER
enum SetSubjectType
{
	CalcSubjectMax = 11,
	CalcSubjectMin,
	CalcSubjectSum,
};
enum PayType
{
	PayMin = 21,
	PayMax,
	PayConstant,
};
#endif

class Cover
{
public:
//	static int getCoverItemType(char* str);
	Cover();
	~Cover();

#ifdef MULTILAVELCOVER
	bool _isPerContract;
	int _nContract;//the size of _pLoss
	double* _pLoss;
	void calcPerRiskTreatyLeafLoss(int nExposure, double* pExposureLoss, Node& node);
	double calcPerRiskTreatyRootLoss(Cover* pCover);
#ifdef WITHALLOCATION
	void allocMemForPerRiskTreaty(int nExposure);
	void allocateLossToChildCover(Cover* pCover, double* pAllcLoss);
	void allocateLossToExposure(double loss, int nExposure, double* pExposureLoss, int* pExposureIdx, double* pPayoutA);
#endif
#endif

	double calcLoss(Node* pNode);

#ifdef MULTILAVELCOVER
	double calcRootPayout(Node* pNode, Cover* pCover
#ifdef WITHALLOCATION
		, int index, int* pSetsIndex
#endif
		);

#ifdef WITHALLOCATION
	void payoutAlocForNonLeafRoot(Cover* pCover, double* pRA, double* pPA, Node* pNode, double* pDirectRA, double* pDirectPA
				, int index, int* pSetsIndex
		);
#endif			

#ifdef PECENTAFFECTED
	double convertAffectedPecent2Amount(Cover* pCover, double* pNodeTotalTIV);
#endif
#endif

	void calcLoss0();
	void calcLoss1();
	void calcLoss2();
	void calcLoss3();
	int getCalcType();
	void setFuncP();

	void adjustByExchangeRate(double rate);
	void setChild(int nChild, int* pChildIdx);
	void setItem(int key, FILE* pf);

	void CreateData();
	void linkData(Cover& cover);
	void ClearMemory(int nObjCreated);

	void payoutAloc(double* pRA, double* pPA, Node* pNode, double* pDirectRA, double* pDirectPA);

	void convertCoveredPecentToAmount(double* pValue);

private:
	CoverData* _pData;
	pt2CoverFunc _pt2Func;
	double _S;
	double _R;

	void calcLeafPayout(Node* pNode);
	void calcLeafLoss(Node* pNode);

	void setItemValue(int key, double v);
	void lossAllocation(double loss, double* pRA, double* pPA, Node* pNode, double* pDirectRA, double* pDirectPA);

#ifdef MULTILAVELCOVER
	int _setsType;
	int _payType;
	double* _pS;

	void calcBasic();
	void payF();
	void payStepPolicy(
#ifdef WITHALLOCATION
	int& index
#endif
		);
	void calcPayout(Node* pNode, Cover* pCover
#ifdef WITHALLOCATION
		, int index, int* pSetsIndex
#endif
		);
	void payoutAlocForStepPolicy(double loss, Cover* pCover, double* pRA, double* pPA, Node* pNode, double* pDirectRA, double* pDirectPA
#ifdef WITHALLOCATION
				, int index, int* pSetsIndex
#endif			
		);
#endif
};

#endif