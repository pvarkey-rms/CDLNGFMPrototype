#ifndef UTILFUNCTION_H
#define UTILFUNCTION_H

#define DELETEARRAY(ARG) {if(ARG) {delete [] ARG; ARG = 0;}}
#define DELETEOBJECT(ARG) {if(ARG) {delete ARG; ARG = 0;}}

struct TwoInt
{
	int i1;
	int i2;
};

struct ThreeInt
{
	int i1;
	int i2;
	int i3;
};

double MAXD(double a, double b);
double MIND(double a, double b);

int findIndexByBinary(int idx, int size, int* pValue);

void sortIntArrayInc(int size, int* pV);
//sort an array of TwoInt by i2
void sortTwoIntArrayInc(int size, TwoInt* pV);
//sort an array of ThreeInt by i2; increasing
void sort3IntBy2(int size, ThreeInt* pV);

//.sm file
struct Record
{
	int _ExposureIdx;
	int _nodeIdx;
	double _exposureValue;
};
//sort _nodeIdx first, then _ExposureIdx
void sortRecordArray(int size, Record* pV);

void setItemSmaller(double& result, double v);
void setItemLarger(double& result, double v);

///////////////////////////////////////////////////////////////
//functions for calculating losses for basic node types


#endif