# CDLNGFMPrototype

This solution contains three C# implementations of HDFM.

1. Object-graph based C# HDFM (the original prototype)

  - RMS.CDLModel (represents the Contract Object Model, the parsed IR is converted into an instance of this object)
  - RMS.ContractGraphModel (represents the Contract Graph, constructed from the Contract Object Model)
  - RMS.Prototype.NGFM (the main project)
  
2. Vector-based implementation (MatrixHDFM),

  - NGFM.Reference.MatrixHDFM

    a. which also employs NGFMReferenceVectorized (the original reference)
    
      - NGFMReference-Vectorized

