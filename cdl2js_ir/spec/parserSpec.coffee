# spec/parserSpec.js
basedir = '../'
# Controller = require basedir + 'public/lib/controller.js'
Product = require basedir + 'public/lib/product.js'

describe 'cdl2js parser', ->

    it 'parse CDL', ->
        merged_view = {}
        Product.merge(product: "Product Simple 
  Declarations
      Type is Insurance
      Required Parameters are ( PolicyNum, Subject, Inception, Expiration, WindSublim, BlanketLim )
      Optional Parameters are ( Insured )
      WindSublim is 0
      BlanketLim is 0
  Covers 
    100% share of BlanketLim 
  Sublimits
    WindSublim  by Wind", instance: "Contract 
  Declarations
      Product is Simple { ver 0.1-b }
      Subject is Loss to Schedule
      Insured is Acme
      PolicyNum is P-12456A.2014
      Inception is 5 Jun 2014
      Expiration is 4 Jun 2015
      WindSublim is 10000
      BlanketLim is 10000000")
        expect(merged_view).toEqual {}