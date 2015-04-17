#include "utilFunc.h"
#include <stdlib.h>


double MAXD(double a, double b)
{
	return a>b ? a : b;
}

double MIND(double a, double b)
{
	return a<b ? a : b;
}

//assume that idx >= pValue[0] and idx < pValue[size]
//return index such that idx >= pValue[index] and idx < pValue[index+1]
int findIndexByBinary(int idx, int size, int* pValue)
{
	if ( idx < pValue[1] )
		return 0;
	int low = 0;
	int high = size;
	int mid = ( high - low ) / 2;
	while ( pValue[mid] != idx )
	{
		if ( idx > pValue[mid] )
		{
			low = mid;
			mid = ( high + low ) / 2;
		}
		else
		{
			high = mid;
			mid = ( high + low ) / 2;
		}
		if ( low == mid )
			return low;
	}
	return mid;
}

int compIntInc (const void *a, const void *b)
{
	if (*(int *)a < *(int *)b) return -1;
	if (*(int *)a > *(int *)b) return 1;
	return 0;
}

void sortIntArrayInc(int size, int* pV)
{
	qsort(pV, size, sizeof(int), compIntInc);
}

int compTwoIntInc (const void *a, const void *b)
{
	TwoInt* d1 = (TwoInt *)a;
	TwoInt* d2 = (TwoInt *)b;
	if (d1->i2 < d2->i2) return -1;
	if (d1->i2 > d2->i2) return 1;
	return 0;
}

void sortTwoIntArrayInc(int size, TwoInt* pV)
{
	qsort(pV, size, sizeof(TwoInt), compTwoIntInc);
}

int comp3IntIncBy2 (const void *a, const void *b)
{
	ThreeInt* d1 = (ThreeInt *)a;
	ThreeInt* d2 = (ThreeInt *)b;
	if (d1->i2 < d2->i2) return -1;
	if (d1->i2 > d2->i2) return 1;
	return 0;
}

void sort3IntBy2(int size, ThreeInt* pV)
{
	qsort(pV, size, sizeof(ThreeInt), comp3IntIncBy2);
}

int compRecordInc (const void *a, const void *b)
{
	Record* d1 = (Record *)a;
	Record* d2 = (Record *)b;
	if (d1->_nodeIdx < d2->_nodeIdx) return -1;
	if (d1->_nodeIdx > d2->_nodeIdx) return 1;
	if (d1->_ExposureIdx < d2->_ExposureIdx) return -1;
	if (d1->_ExposureIdx > d2->_ExposureIdx) return 1;
	return 0;
}

void sortRecordArray(int size, Record* pV)
{
	qsort(pV, size, sizeof(Record), compRecordInc);
}

void setItemSmaller(double& result, double v)
{
	if ( result == -1 )
		result = v;
	else if ( result > v )
		result = v;
}

void setItemLarger(double& result, double v)
{
	if ( result == -1 )
		result = v;
	else if ( result < v )
		result = v;
}

