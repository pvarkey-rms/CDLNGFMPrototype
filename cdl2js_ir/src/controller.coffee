# UI controller for CDL editor prototype
# --------------------------------------

# The controller coordinates four text areas:
# `Model` always shows text of selected CDL file.
# `View` shows the merged CDL if `Model` represents a product instance
# (see `merge` routine, below).
# `AST` shows the  result of parsing contents of the `Source` window.
# `Designer` shows an HTML form for the sample product definition, filled per the product instance.
#
# `Source` identifies either `View` or `Model` (depending on whether `Model` is a product instance.)

$ ->

  merged_view = {}

  # Populate `Model` from selected example CDL.
  $("#inputCDL").on "change", -> 
   filename = @files[0].name
   ext = filename.substring(filename.length-3,filename.length);
   ext = ext.toLowerCase();
   if ext isnt 'cdl'
    alert('Please select a .cdl file!');
   else
    read filename, (data, status, xhr) -> view( data )

  $("#examples").on "change", ->
    file = @options[@selectedIndex].value
    read file, (data, status, xhr) ->
      view( data )

  $("#model").on "blur", () ->
    view @value

  view = (cdl) ->

    # Display the `Model`
    $("#model").val( cdl )
    $("#view").val('')
    $("#ast").val('')
    $("#ast").removeClass("bad").removeClass("good")

    # DISABLE 4 lines below
    # visible = isProduct cdl
    # $("#designer").toggle visible
    # if visible
    #  _.each getFormalParams(cdl), (p) -> $(".#{p}").val('')


    unless isProductInstance cdl
    # It's not a product instance, set `Source` to `Model`
      $("#model").addClass('source')
      $("#view").removeClass('source')
      return

    # It is a product instance ..
    fname = productFile( cdl )
    # .. so read the Product definition
    read fname, ( productCdl, status, xhr ) ->
    # .. and display the merged CDL in `View`
      merged_view = merge(
        instance: cdl
        product: productCdl
        )
      formalParams = getFormalParams productCdl
      decls = declarationText cdl
      instanceParams = getParamValues( formalParams, decls )
      vParams = vectorParams formalParams
      #printOut ("PRODUCT:\n" + productCdl + "\n\nFORMAL PARAMS:\n" + formalParams + "\n\nDECLS:\n" + decls + "\n\nACTUAL:\n" + instanceParams + "\n\nVECTOR PARAMS:\n" + vParams)
      $("#view").val( merged_view.cdl )
      # DISABLE 3 lines below : Display params in designer view
      #_.each merged_view.params, (p) ->
      #  $(".#{_.first(p)}").val(_.last(p))
      #$("#designer").toggle(true);

    # ... and set `Source` to `View`
    $("#model").removeClass('source')
    $("#view").addClass('source')

  # process JSON representation of parse ast and vertically expand vectors for covers and terms
  vectorExpand = ( productCdlAst, aParams ) ->
    return productCdlAst unless aParams
    # iterate through actual params and build two-dimensional array of [index][key] --> value of vector with name key at specified index
    vExpansion = {}
    for aParam in aParams
        if /\[\*\]/.test(aParam[0])
          paramValues = aParam[1].replace(/\[/,"").replace(/\]/,"").trim().split(',')
          defaultParamValue = paramValues[0].trim()
          for i in [0..paramValues.length-1] by 1
              vExpansion[i] = {} unless vExpansion[i]
              expandedValue = if paramValues[i]? then paramValues[i].trim() else defaultParamValue
              if /\.\.\./.test(expandedValue)
                 expandedValue = defaultParamValue
              vExpansion[i][aParam[0].replace(/\*/,"").trim()] = expandedValue
    appendToProductCdlAst = {}
    
    appendToProductCdlAst['Covers'] = []
    expandedCovers = []
    coverIndexInProduct = 1
    for cover in productCdlAst['Covers']
        cover['IndexInProduct'] = coverIndexInProduct
        for key, val of cover
            if /\[\]/.test(val)
              expandedCovers.push cover
              appendToProductCdlAst['Covers'] = appendToProductCdlAst['Covers'].concat vectorExpandUnit(cover, vExpansion)
              break
        coverIndexInProduct++
    productCdlAst['Covers'] = _.difference(productCdlAst['Covers'], expandedCovers)
    productCdlAst['Covers'] = productCdlAst['Covers'].concat appendToProductCdlAst['Covers']
    
    appendToProductCdlAst['Deductibles'] = []
    expandedDeductibles = []
    deductibleIndexInProduct = 1
    for deductible in productCdlAst['Deductibles']
        deductible['IndexInProduct'] = deductibleIndexInProduct
        for key, val of deductible
            if /\[\]/.test(val)
              expandedDeductibles.push deductible
              appendToProductCdlAst['Deductibles'] = appendToProductCdlAst['Deductibles'].concat vectorExpandUnit(deductible, vExpansion)
              break
        deductibleIndexInProduct++
    productCdlAst['Deductibles'] = _.difference(productCdlAst['Deductibles'], expandedDeductibles)
    productCdlAst['Deductibles'] = productCdlAst['Deductibles'].concat appendToProductCdlAst['Deductibles']
    
    appendToProductCdlAst['Sublimits'] = []
    expandedSublimits = []
    sublimitIndexInProduct = 1
    for sublimit in productCdlAst['Sublimits']
        sublimit['IndexInProduct'] = sublimitIndexInProduct
        for key, val of sublimit
            if /\[\]/.test(val)
              expandedSublimits.push sublimit
              appendToProductCdlAst['Sublimits'] = appendToProductCdlAst['Sublimits'].concat vectorExpandUnit(sublimit, vExpansion)
              break
        sublimitIndexInProduct++
    productCdlAst['Sublimits'] = _.difference(productCdlAst['Sublimits'], expandedSublimits)
    productCdlAst['Sublimits'] = productCdlAst['Sublimits'].concat appendToProductCdlAst['Sublimits']
    
    productCdlAst

  vectorExpandUnit = ( unit , vExpansion ) ->
    units = []
    for i in [0..Object.keys(vExpansion).length-1] by 1
        unitclone = JSON.parse(JSON.stringify(unit))
        #for unitclone_kvp in unitclone
            #if /\[\]/.test(unitclone_kvp['Value'])
        for key, val of unit
            if /\[\]/.test(val)
              expandedValue = vExpansion[i][val.replace(/\].*/,"\]")]
              if not expandedValue?
                 expandedValue = vExpansion[0][val.replace(/\].*/,"\]")]
              unitclone[key] = expandedValue
        units.push unitclone
    units

  # Utility to write text to `AST`.
  printOut = (str) -> $("#ast").val(str)

  parseSourceAndVectorExpand = (source) ->
    ast = grammarAst.parse(source)
    ast = vectorExpand(ast, merged_view.params)
    ast

  parseProductInstance = (productCDL, instanceCDL) ->
    merged = merge(
        instance: instanceCDL
        product: productCDL
        )
    ast = grammarAst.parse(merged.cdl)
    ast = vectorExpand(ast, merged.params)
    ast

  # Parse `Source`, write to `AST`.
  parse = ->
    printOut "Parsing..."
    source = $(".source").val()
    try
      $("#ast").removeClass("bad").addClass('good')
      ast = parseSourceAndVectorExpand(source)
      printOut(JSON.stringify(ast,0,4))
    catch e
      $("#ast").removeClass("good").addClass('bad')
      printOut(e.message || e)

  # `AST` is generated when the `Parse` button is pushed.
  $("#parse").on "click", parse

  # Wrap Ajax GET, set Ajax error routine
  read = (name, success_callback) ->
    $.get( "./examples/" + name, success_callback )
  $(document).ajaxError( (event, request, settings ) ->
    printOut "Error reading contract: " + decodeURI(request.responseText) )
