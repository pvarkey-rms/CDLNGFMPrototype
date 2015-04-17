#
# Pattern matching routines to create merged view of CDL product instance.
#

  root = exports ? this

  # Return product file name and version from cdl text
  productFile = (text) ->
    pattern = /\sproduct\s+is\s+(.*)/i
    matches = pattern.exec text
    matches?[1] or throw 'Expected product filename'

  # return true if a product instance
  isProduct = (cdl) ->
    /^\s*product\s*/i.test cdl

  # Return all declarations in text as a single string
  declarationText = (text) ->
    pattern = /declarations([\s|\S]*)covers/i
    matches = pattern.exec( text )
    # if no match, append 'covers' and try again
    matches or= pattern.exec( text + "\ncovers")
    matches?[1] or ''

  # Return true if text is CDL of a product instance
  isProductInstance = (text) ->
    pattern = /^contract\s+declarations[\s|\S]+product\s+is\s+/i
    pattern.test text

  # Return list of parameter names specified in product definition
  getFormalParams = (text) ->
    arr = []
    pattern = /parameters\s+are\s+\((.*)\)/gi
    matches = pattern.exec text
    return arr unless matches?[1]
    while matches?
      arr = arr.concat(matches[1].split(/,/))
      matches = pattern.exec text
    _.map arr, (e) -> e.trim()    # remove spaces before and after each param

  # Get list of vector parameters from list of parameters
  vectorParams = (params) ->
    params.filter (p) -> /\[\]/.test(p)

  # Return list of [name, value] found in text for each name in formal params (unless name is in exclusion list)
  getParamValues = (formal, text, exclusions ) ->
    lines = text.replace(/\/\/(.*)/,"").split('\n')
    pattern = /(.*)( is | are )(.*)/i
    _.reduce(
      lines
      , (accum, line) ->
          matches = pattern.exec line
          return accum unless matches?[3]
          name = matches[1].trim()
          return accum unless name in formal
          if exclusions?
            return accum unless name not in exclusions
          # replace vector idents with dereferencing symbol
          name = name.replace("\[\]","\[*\]")
          accum.push [ name,matches[3].replace(/\/\/.*/,"").trim() ]
          accum
      , [] )

  # Return CDL text starting from Covers keyword
  bodyOf = (text) ->
    # pattern = /^(\s*covers\s*[\s|\S]*)$/im
    pattern = /[\s]+(covers[\s]+[\s|\S]*)/im
    matches = pattern.exec text
    matches?[1] or throw "Expected 'Covers'"

  # Replace parameter names in Product terms & conditions
  # with values specified in product instance.
  root.merge = ( texts ) ->

    # Set template to product text starting from 'Covers'
    template = bodyOf texts.product

    # formal is the list of parameter names defined by the product
    formalParams = getFormalParams texts.product

    # instanceParams is the list of [name, value] pairs where name is a formal param
    instanceParams = getParamValues( formalParams, declarationText texts.instance )

    # build list of instance param names as exclusion list for defaultProductParams
    instanceParamNames =  []
    for instanceParam in instanceParams
        instanceParamNames.push instanceParam[0].replace("\[*\]", "\[\]");

    # default params from product
    defaultProductParams = getParamValues( formalParams, declarationText(texts.product), instanceParamNames )
	
    # merge all params
    actualParams = instanceParams.concat defaultProductParams

    # Replace each occurrence of a param name with its value, throughout template
    body = _.reduce(
      actualParams
      , ((t,k) -> t.replace _.first(k) + ' ', _.last(k) + ' ')
      , template )

    # Set decls to product decls - {ProductName, Parameters} + instance decls
    decls = declarationText texts.instance

    # Return 'Contract' + decls + template
    {
      cdl : "Contract\n  Declarations#{decls}#{body}"
      params: actualParams
    }

  isWhitespace = (str) ->
    /^\s+$/.test str

