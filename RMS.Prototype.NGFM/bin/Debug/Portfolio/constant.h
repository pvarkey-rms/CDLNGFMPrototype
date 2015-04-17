#ifndef CONSTANT_H
#define CONSTANT_H

#define DELETEARRAY(ARG) {if(ARG) {delete [] ARG; ARG = 0;}}
#define DELETEOBJECT(ARG) {if(ARG) {delete ARG; ARG = 0;}}

struct InOut
{
	int    _i;
	double _v;
};

struct Losses
{
    unsigned long long exposureId;
    double loss;
};

struct AccumulatedLosses
{
    unsigned long long exposureId;
    double exposureValue;
    double loss;
};

#endif
