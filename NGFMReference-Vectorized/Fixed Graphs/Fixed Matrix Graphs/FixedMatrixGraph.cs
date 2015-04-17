using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGFM.Reference.MatrixHDFM;

namespace NGFMReference
{
    public abstract class FixedMatrixGraph : MatrixGraph
    {
        protected ExposureDataAdaptor expData;

        public FixedMatrixGraph(ExposureDataAdaptor _expData)
        {
            expData = _expData;
        }       
    }
}
