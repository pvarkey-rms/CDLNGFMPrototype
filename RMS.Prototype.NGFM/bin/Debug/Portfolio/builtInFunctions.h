# pragma once
/*
* Header file with signatures for functions mimicking Risk Link
* @CDL Team
*/

namespace cdl{
	namespace Shared{
		namespace FunctionLib{
			extern double ContentLossTrigger(double Deductible, double Building, double Threshold, double Contents);
			//ArrayMax, ArrayMin, ArraySum assume size>=1
			extern int ArrayMax(double Arr[], int size);
			extern int ArrayMin(double Arr[], int size);
			extern double ArraySum(double Arr[], int size);
		}
	}
}
