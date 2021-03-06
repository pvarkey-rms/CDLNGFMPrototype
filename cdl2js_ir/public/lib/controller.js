// Generated by CoffeeScript 1.7.1
$(function() {
  var merged_view, parse, parseProductInstance, parseSourceAndVectorExpand, printOut, read, vectorExpand, vectorExpandUnit, view;
  merged_view = {};
  $("#inputCDL").on("change", function() {
    var ext, filename;
    filename = this.files[0].name;
    ext = filename.substring(filename.length - 3, filename.length);
    ext = ext.toLowerCase();
    if (ext !== 'cdl') {
      return alert('Please select a .cdl file!');
    } else {
      return read(filename, function(data, status, xhr) {
        return view(data);
      });
    }
  });
  $("#examples").on("change", function() {
    var file;
    file = this.options[this.selectedIndex].value;
    return read(file, function(data, status, xhr) {
      return view(data);
    });
  });
  $("#model").on("blur", function() {
    return view(this.value);
  });
  view = function(cdl) {
    var fname;
    $("#model").val(cdl);
    $("#view").val('');
    $("#ast").val('');
    $("#ast").removeClass("bad").removeClass("good");
    if (!isProductInstance(cdl)) {
      $("#model").addClass('source');
      $("#view").removeClass('source');
      return;
    }
    fname = productFile(cdl);
    read(fname, function(productCdl, status, xhr) {
      var decls, formalParams, instanceParams, vParams;
      merged_view = merge({
        instance: cdl,
        product: productCdl
      });
      formalParams = getFormalParams(productCdl);
      decls = declarationText(cdl);
      instanceParams = getParamValues(formalParams, decls);
      vParams = vectorParams(formalParams);
      return $("#view").val(merged_view.cdl);
    });
    $("#model").removeClass('source');
    return $("#view").addClass('source');
  };
  vectorExpand = function(productCdlAst, aParams) {
    var aParam, appendToProductCdlAst, cover, coverIndexInProduct, deductible, deductibleIndexInProduct, defaultParamValue, expandedCovers, expandedDeductibles, expandedSublimits, expandedValue, i, key, paramValues, sublimit, sublimitIndexInProduct, vExpansion, val, _i, _j, _k, _l, _len, _len1, _len2, _len3, _m, _ref, _ref1, _ref2, _ref3;
    if (!aParams) {
      return productCdlAst;
    }
    vExpansion = {};
    for (_i = 0, _len = aParams.length; _i < _len; _i++) {
      aParam = aParams[_i];
      if (/\[\*\]/.test(aParam[0])) {
        paramValues = aParam[1].replace(/\[/, "").replace(/\]/, "").trim().split(',');
        defaultParamValue = paramValues[0].trim();
        for (i = _j = 0, _ref = paramValues.length - 1; _j <= _ref; i = _j += 1) {
          if (!vExpansion[i]) {
            vExpansion[i] = {};
          }
          expandedValue = paramValues[i] != null ? paramValues[i].trim() : defaultParamValue;
          if (/\.\.\./.test(expandedValue)) {
            expandedValue = defaultParamValue;
          }
          vExpansion[i][aParam[0].replace(/\*/, "").trim()] = expandedValue;
        }
      }
    }
    appendToProductCdlAst = {};
    appendToProductCdlAst['Covers'] = [];
    expandedCovers = [];
    coverIndexInProduct = 1;
    _ref1 = productCdlAst['Covers'];
    for (_k = 0, _len1 = _ref1.length; _k < _len1; _k++) {
      cover = _ref1[_k];
      cover['IndexInProduct'] = coverIndexInProduct;
      for (key in cover) {
        val = cover[key];
        if (/\[\]/.test(val)) {
          expandedCovers.push(cover);
          appendToProductCdlAst['Covers'] = appendToProductCdlAst['Covers'].concat(vectorExpandUnit(cover, vExpansion));
          break;
        }
      }
      coverIndexInProduct++;
    }
    productCdlAst['Covers'] = _.difference(productCdlAst['Covers'], expandedCovers);
    productCdlAst['Covers'] = productCdlAst['Covers'].concat(appendToProductCdlAst['Covers']);
    appendToProductCdlAst['Deductibles'] = [];
    expandedDeductibles = [];
    deductibleIndexInProduct = 1;
    _ref2 = productCdlAst['Deductibles'];
    for (_l = 0, _len2 = _ref2.length; _l < _len2; _l++) {
      deductible = _ref2[_l];
      deductible['IndexInProduct'] = deductibleIndexInProduct;
      for (key in deductible) {
        val = deductible[key];
        if (/\[\]/.test(val)) {
          expandedDeductibles.push(deductible);
          appendToProductCdlAst['Deductibles'] = appendToProductCdlAst['Deductibles'].concat(vectorExpandUnit(deductible, vExpansion));
          break;
        }
      }
      deductibleIndexInProduct++;
    }
    productCdlAst['Deductibles'] = _.difference(productCdlAst['Deductibles'], expandedDeductibles);
    productCdlAst['Deductibles'] = productCdlAst['Deductibles'].concat(appendToProductCdlAst['Deductibles']);
    appendToProductCdlAst['Sublimits'] = [];
    expandedSublimits = [];
    sublimitIndexInProduct = 1;
    _ref3 = productCdlAst['Sublimits'];
    for (_m = 0, _len3 = _ref3.length; _m < _len3; _m++) {
      sublimit = _ref3[_m];
      sublimit['IndexInProduct'] = sublimitIndexInProduct;
      for (key in sublimit) {
        val = sublimit[key];
        if (/\[\]/.test(val)) {
          expandedSublimits.push(sublimit);
          appendToProductCdlAst['Sublimits'] = appendToProductCdlAst['Sublimits'].concat(vectorExpandUnit(sublimit, vExpansion));
          break;
        }
      }
      sublimitIndexInProduct++;
    }
    productCdlAst['Sublimits'] = _.difference(productCdlAst['Sublimits'], expandedSublimits);
    productCdlAst['Sublimits'] = productCdlAst['Sublimits'].concat(appendToProductCdlAst['Sublimits']);
    return productCdlAst;
  };
  vectorExpandUnit = function(unit, vExpansion) {
    var expandedValue, i, key, unitclone, units, val, _i, _ref;
    units = [];
    for (i = _i = 0, _ref = Object.keys(vExpansion).length - 1; _i <= _ref; i = _i += 1) {
      unitclone = JSON.parse(JSON.stringify(unit));
      for (key in unit) {
        val = unit[key];
        if (/\[\]/.test(val)) {
          expandedValue = vExpansion[i][val.replace(/\].*/, "\]")];
          if (expandedValue == null) {
            expandedValue = vExpansion[0][val.replace(/\].*/, "\]")];
          }
          unitclone[key] = expandedValue;
        }
      }
      units.push(unitclone);
    }
    return units;
  };
  printOut = function(str) {
    return $("#ast").val(str);
  };
  parseSourceAndVectorExpand = function(source) {
    var ast;
    ast = grammarAst.parse(source);
    ast = vectorExpand(ast, merged_view.params);
    return ast;
  };
  parseProductInstance = function(productCDL, instanceCDL) {
    var ast, merged;
    merged = merge({
      instance: instanceCDL,
      product: productCDL
    });
    ast = grammarAst.parse(merged.cdl);
    ast = vectorExpand(ast, merged.params);
    return ast;
  };
  parse = function() {
    var ast, e, source;
    printOut("Parsing...");
    source = $(".source").val();
    try {
      $("#ast").removeClass("bad").addClass('good');
      ast = parseSourceAndVectorExpand(source);
      return printOut(JSON.stringify(ast, 0, 4));
    } catch (_error) {
      e = _error;
      $("#ast").removeClass("good").addClass('bad');
      return printOut(e.message || e);
    }
  };
  $("#parse").on("click", parse);
  read = function(name, success_callback) {
    return $.get("./examples/" + name, success_callback);
  };
  return $(document).ajaxError(function(event, request, settings) {
    return printOut("Error reading contract: " + decodeURI(request.responseText));
  });
});
