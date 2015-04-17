/*
* Implementation of functions mimicking Risk Link
* @CDL Team
*/
#include<iostream>
#include "builtInFunctions.h"

namespace cdl{
	namespace Shared{
		namespace FunctionLib{
			double Min(double x, double y){
			  if( x < y ){
				return x;
			  }
			  return y;
			}

			double MinL(double x1, double x2, double y1, double y2){
			  if( x1 < x2 ){
				return y1;
			  }
			  return y2;
			}

			double ContentLossTrigger(double Deductible, double Building, double Threshold, double Contents){
			  return MinL(Deductible, Building, Min(Contents, Threshold), Contents);
			}

			//Assumes size>=1
			int ArrayMax(double Arr[], int size){
				double maxArr = Arr[0];
				int maxInd = 0;
				for(int i=1; i<size; ++i){
					if(maxArr < Arr[i]){
						maxArr = Arr[i];
						maxInd = i;
					}
				}
				return maxInd;
			}
			
			//Assumes size>=1
			int ArrayMin(double Arr[], int size){
				double minArr = Arr[0];
				int minInd = 0;
				for(int i=1; i<size; ++i){
					if(minArr > Arr[i]){
						minArr = Arr[i];
						minInd = i;
					}
				}
				return minInd;
			}
			
			//Assumes size>=1
			double ArraySum(double Arr[], int size){
				double sum = 0;
				for(int i=0; i<size; ++i){
					sum += Arr[i];
				}
				return sum;
			}
		}
	}
}