// Generated by CoffeeScript 1.7.1
var bodyOf, declarationText, getFormalParams, getParamValues, isProduct, isProductInstance, isWhitespace, productFile, root, vectorParams,
  __indexOf = [].indexOf || function(item) { for (var i = 0, l = this.length; i < l; i++) { if (i in this && this[i] === item) return i; } return -1; };

root = typeof exports !== "undefined" && exports !== null ? exports : this;

productFile = function(text) {
  var matches, pattern;
  pattern = /\sproduct\s+is\s+(.*)/i;
  matches = pattern.exec(text);
  return (matches != null ? matches[1] : void 0) || (function() {
    throw 'Expected product filename';
  })();
};

isProduct = function(cdl) {
  return /^\s*product\s*/i.test(cdl);
};

declarationText = function(text) {
  var matches, pattern;
  pattern = /declarations([\s|\S]*)covers/i;
  matches = pattern.exec(text);
  matches || (matches = pattern.exec(text + "\ncovers"));
  return (matches != null ? matches[1] : void 0) || '';
};

isProductInstance = function(text) {
  var pattern;
  pattern = /^contract\s+declarations[\s|\S]+product\s+is\s+/i;
  return pattern.test(text);
};

getFormalParams = function(text) {
  var arr, matches, pattern;
  arr = [];
  pattern = /parameters\s+are\s+\((.*)\)/gi;
  matches = pattern.exec(text);
  if (!(matches != null ? matches[1] : void 0)) {
    return arr;
  }
  while (matches != null) {
    arr = arr.concat(matches[1].split(/,/));
    matches = pattern.exec(text);
  }
  return _.map(arr, function(e) {
    return e.trim();
  });
};

vectorParams = function(params) {
  return params.filter(function(p) {
    return /\[\]/.test(p);
  });
};

getParamValues = function(formal, text, exclusions) {
  var lines, pattern;
  lines = text.replace(/\/\/(.*)/, "").split('\n');
  pattern = /(.*)( is | are )(.*)/i;
  return _.reduce(lines, function(accum, line) {
    var matches, name;
    matches = pattern.exec(line);
    if (!(matches != null ? matches[3] : void 0)) {
      return accum;
    }
    name = matches[1].trim();
    if (__indexOf.call(formal, name) < 0) {
      return accum;
    }
    if (exclusions != null) {
      if (__indexOf.call(exclusions, name) >= 0) {
        return accum;
      }
    }
    name = name.replace("\[\]", "\[*\]");
    accum.push([name, matches[3].replace(/\/\/.*/, "").trim()]);
    return accum;
  }, []);
};

bodyOf = function(text) {
  var matches, pattern;
  pattern = /[\s]+(covers[\s]+[\s|\S]*)/im;
  matches = pattern.exec(text);
  return (matches != null ? matches[1] : void 0) || (function() {
    throw "Expected 'Covers'";
  })();
};

root.merge = function(texts) {
  var actualParams, body, decls, defaultProductParams, formalParams, instanceParam, instanceParamNames, instanceParams, template, _i, _len;
  template = bodyOf(texts.product);
  formalParams = getFormalParams(texts.product);
  instanceParams = getParamValues(formalParams, declarationText(texts.instance));
  instanceParamNames = [];
  for (_i = 0, _len = instanceParams.length; _i < _len; _i++) {
    instanceParam = instanceParams[_i];
    instanceParamNames.push(instanceParam[0].replace("\[*\]", "\[\]"));
  }
  defaultProductParams = getParamValues(formalParams, declarationText(texts.product), instanceParamNames);
  actualParams = instanceParams.concat(defaultProductParams);
  body = _.reduce(actualParams, (function(t, k) {
    return t.replace(_.first(k) + ' ', _.last(k) + ' ');
  }), template);
  decls = declarationText(texts.instance);
  return {
    cdl: "Contract\n  Declarations" + decls + body,
    params: actualParams
  };
};

isWhitespace = function(str) {
  return /^\s+$/.test(str);
};
