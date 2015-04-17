%{
    /* If running in nodejs on server, require underscore.js */
    if (typeof require !== 'undefined') {
       _ = require('underscore');
    }
    function prependChild(node, child){
      node.splice(0,0,child); 
      return node;
    }
    function appendChild(node, child){
      node.push(child); 
      return node;
    }
	function printOut( nodes ) {
      //var retVal =  JSON.stringify( nodes,0, 4 );
      //console.log(retVal);
    }

	// set of known currencies (slightly general set implementation in http://stackoverflow.com/a/7958422)
	var knownCurrencies = Object.create(null);

	var coverIndex = 1;
	var deductibleIndex = 0;
	var sublimitIndex = 0;

	var hasDefaults = "False";
	var productParams = {};

	// deprecated (obsoleted) decls
	//| ident_name IS expression { $$ = { Key: $1, Value: _.findWhere($3, {Key: 'Amount'}).Value }; }

	//| vector_ident_name IS vector { $$ = { Key: $1, Value: $3 }; }

	//| vector_ident_name IS vector_default { $$ = { Key: $1, Value: $3 }; }

    //| ident_name IS phrase  { $$ = { Key: $1, Value: $3.replace(/\{|\}/g, '').trim() }; }

	//| expression-is-expression  { $$ = $1; }
	
%}

 
%%
cdl : 
	/* empty */ {return printOut('Contract is empty');} 
	| contract { return $$; } 
	| product { return $$; };

/* A CDL contract consists of the keyword 'Contract' followed by declarations, covers, terms and sections */
contract : 
    CONTRACT declaration_part cover_part section_part sublimit_part deductible_part
    {  $$ = {Name: 'Contract', Declarations: $2, Covers: $3, Sections:$4, Sublimits: $5, Deductibles:$6  }; }
    ;

/* A CDL product consists of the keyword 'Product' followed by declarations, covers, terms and sections */
product : 
    PRODUCT product_name product_declaration_part cover_part section_part sublimit_part deductible_part
	{  
       var decls = _.filter( $3, 
		function(kv) 
			{
				return ("Key" in kv) && kv.Key.indexOf('Parameters') == -1;
			}
		);
       reqparam =  _.findWhere($3, {Key: 'RequiredParameters'});
       optparam = _.findWhere($3, {Key: 'OptionalParameters'});
       decls.push($2);
	   var doesHaveDefaultAfterParse =  hasDefaults;
	   hasDefaults = "False";
	   productParams = {};
       $$ = {
                Key: 'Product',
				HasDefaults: doesHaveDefaultAfterParse, 
                RequiredParameters : (reqparam)?reqparam.Value:'',
				OptionalParameters : (optparam)?optparam.Value:'',
                Declarations: decls, 
                Covers: $4, 
				Sections: $5,
                Sublimits: $6, 
                Deductibles: $7
             } ; 
    };

product_name :
    ident_name        { $$ = {'ProductName': $1}; }
    ;

/*  Every contract has a list of declarations, introduced by the keyword 'Declarations' */
declaration_part :  
    DECLARATIONS declarations { $$ =  $2 }
    ;

/*  Every product has a list of declarations, introduced by the keyword 'Declarations' */
product_declaration_part  :  
    DECLARATIONS product_declarations  { $$ =  _.flatten($2); }
    ;

/* Every contract has a list of covers, introduced by the keyword 'Covers'. */
/* Covers specify which claims get paid, and for how much */
cover_part :  
    /* empty */		    { $$ = []; }
    | COVERS covers	    { $$ =  $2; }
    ;


/* A contract has a list of terms, which consist of sublimits followed by deductibles. */
/* Terms provide 'haircuts' which reduce the subject seen by the covers. */
/* Sublimits, if present, are introduced by the keyword 'Sublimits' */
sublimit_part : 
    /* empty */            { $$ = []; }
    | SUBLIMITS sublimits  { sublimitIndex = 0; $$ = $2; }
    ;

/* A contract has a list of terms, which consist of sublimits followed by deductibles. */
/* Terms provide 'haircuts' which reduce the subject seen by the covers. */
/* Deductibles, if present, are introduced by the keyword 'Deductibles' */
deductible_part : 
    /* empty */                { $$ = []; }
    | DEDUCTIBLES deductibles  { deductibleIndex = 0; $$ = $2; }
    ;

/* */
/* Declarations */
/* */
declarations : 
    declaration                { $$ = $1; }
    | declarations declaration 
		{
			if (_.has($2, "DeclaredBinding")) {
				if (!_.has($1, "DeclaredBindings")) {
					$1['DeclaredBindings']=[];
				}
				$1['DeclaredBindings'].push($2['DeclaredBinding'])
			}
			else {
				$$ = _.extend($1, $2);
			}
		}
    ;

declaration : 
    SUBJECT IS contract_subject { 
        $$ = $3; 
    }

	| adjustment_option			{ $$ = $1; }
   
    | INCEPTION IS date         { $$ = {'Inception' : $3}; }

	| INCEPTION IS phrase  { $$ = { 'Inception' : $3.replace(/\{|\}/g, '').trim() }; }

	| INCEPTION IS expression  { $$ = { 'Inception' : $3['Value'] }; }
   
    | EXPIRATION IS date        { $$ = {'Expiration' :$3}; }

	| EXPIRATION IS phrase  { $$ = {'Expiration' : $3.replace(/\{|\}/g, '').trim() }; }

	| EXPIRATION IS expression  { $$ = {'Expiration' : $3['Value'] }; }
	   
    | CURRENCY IS known_currency_unit  { $$ = {'Currency' : $3}; }

	| CURRENCY IS phrase  { $$ = {'Currency' : $3.replace(/\{|\}/g, '').trim() }; }
   
    | CLAIMSADJUSTMENTOPTIONS ARE LPAREN adjustment_options RPAREN { $$ = {'Claims Adjustment Options' : $4}; }

	| EXCLUSIONS ARE LPAREN exclusions RPAREN  { $$ = {'Exclusions' : $4}; } // { $$ = _.flatten($4); }

	| KNOWNCURRENCIES ARE LBRACE knownCurrencies RBRACE
		{
			currencyList = $3.trim().split(",");
			for (var i = 0; i < currencyList.length; i++) 
			{
				knownCurrencies[currencyList[i].trim()] = true;
			}
			//$$ = { Key: 'KnownCurrencies', Value: $3.replace(/\{|\}/g, '').trim().split(",") }; 
			$$ = { 'KnownCurrencies' : Object.keys(knownCurrencies) }; 
		}
   
    | ATTACHMENTBASIS IS attachment_basis { $$ = $3; } 
   
    | OCCURRENCES ARE LPAREN hours_clauses RPAREN { $$ = {'Hours Clauses' : $4}; }
   
    | USING versioned_ref { $$ = {'UsingRef' : $2}; }
   
    | PRODUCT IS versioned_ref { $$ = {'ProductRef' : $3}; }
   
    | TYPE IS ident_name { $$ = {'ContractType' : $3}; }

	| TYPE IS INSURANCE { $$ = {'ContractType' : $3}; }

	| TYPE IS REINSURANCE { $$ = {'ContractType' : $3}; }

	| TYPE IS phrase { $$ = {'ContractType' : $3.replace(/\{|\}/g, '').trim() }; }

	| RISK IS EACH CONTRACT   { $$ = {'Risk' : $4}; }

	| RISK IS EACH SECTION   { $$ = {'Risk' : 'Cover'}; }

	| RISK IS EACH LOCATION  { $$ = {'Risk' : $4}; }

	| RISK IS EACH ident_name  { $$ = {'Risk' : $4}; }

	| RISK IS phrase  { $$ = {'Risk' : $3.replace(/\{|\}/g, '').trim() }; }
	
	| declaration-lvalue-expression IS declaration-rvalue-expression  
		{ 
			if ($1 in productParams) 
				hasDefaults = 'True'; 
			$$ = { DeclaredBinding: {Key: $1, Value: $3} }; 
		}
		
	| NAME IS ident_name	{ $$ = {'Name' : $3 }; }

	| NAME IS phrase		{ $$ = {'Name' : $3.replace(/\{|\}/g, '').trim() }; }

	| covers_are_part		{ $$ = $1; }

    ;


declaration-lvalue-expression :
	ident_name						{ $$ = $1; }
	| vector_ident_name				{ $$ = $1; }
	| function_call					
		{ 
			$$ = {'ExpressionType': 'FunctionInvocation<double>', 
			'FunctionName' : $1['FunctionName'], 
			'FunctionParameterValueType': $1['FunctionParameterValueType'],
			'Value': $1['Parameters']};
		}
	;

declaration-rvalue-expression : 
    expression						
		{
			$$ = $1; 
		}
	| phrase						{ $$ = $1.replace(/\{|\}/g, '').trim(); }
	| vector_default				{ $$ = $1; }
	| vector						{ $$ = $1; }
	| percentage					{ $$ = $1; }
    ;

/* Product declarations can include a list of parameters */
product_declarations : 
    product_declaration                { $$ = [$1]; }
    | product_declarations product_declaration { $$ = prependChild($1, $2); }
    ;

product_declaration : 
    optional_or_required PARAMETERS ARE LPAREN special_names RPAREN  { $$ = {Key: $1+'Parameters', Value: $5}; }
    | declaration
    ;

optional_or_required : OPTIONAL | REQUIRED
    ;

/*  */
/* Contract subject - every contract has a subject, which defines its universe of claims */
/* */

/* Contract subject is a position */
contract_subject : 
    position
    ;

/* A position may specify a filter as in Acme { policy is ABC and location is 1234 } */
position: 
    unfiltered_position filter 
    ;

unfiltered_position : 
    named_position  cause_of_loss_constraint   { $$ = _.extend({ 'GrossPosition' : $1 }, $2); }
    | net_position     
    | primary_position
    ;

/* Named positions may look like Acme.Gross or BU1,BU2 */
named_position : 
    ident_names
    ;

/* Net positions specify inuring relationships such as "BU1, BU2 net of PerRisk, Fac" */
net_position :
    named_position NET OF named_position { $$ = { 'GrossPosition' :  $1 , 'CededPosition' : $4 }; }
    ;

/* Primary positions may look like "Loss, Damage to Accounts" */
primary_position : 
    outcomes TO named_position cause_of_loss_constraint  
		{ $$ = _.extend({'ExposureTypes' : $1 , 'Schedule' : $3 }, $4); }
    ;

outcomes : 
    outcome					   { $$ = $1; }	
    | outcomes COMMA outcome   { $$ = $1 + ', ' + $3; }
    ;
outcome : 
    LOSS
    | DAMAGE
    | HAZARD
    ;

/* Filters are optional.  An example filter is { Account is ABC45-321/2013 } */
filter : 
    /* empty */ { $$ = { 'Filter' : '' }; }
    | phrase    { $$ = { 'Filter' : $1.replace( /[}{]/g, '').trim() }; }
    ;

/*  Claims adjustment options affect how terms are interpreted */
adjustment_options : 
    adjustment_option								{ $$ = $1; }
    | adjustment_options COMMA adjustment_option	{ $$ = _.extend($1, $3); }
    ;
adjustment_option : 
    DEDUCTIBLESARE ABSORBABLE				{ $$ = {'Claims Adjustment Deductibles' : 'Absorbable'}; }
	| DEDUCTIBLESARE NOT ABSORBABLE		{ $$ = {'Claims Adjustment Deductibles' : 'Not Absorbable'}; }
    | SUBLIMITSARE NET OF DEDUCTIBLE		{ $$ = {'Claims Adjustment Sublimits' : 'Net Of Deductible'}; }
	| SUBLIMITSARE GROUNDUP				{ $$ = {'Claims Adjustment Sublimits' : 'GroundUp'}; }
    ;

exclusions : 
    /*exclusion						{ $$ = ($1)?[$1]:[]; }
    | exclusions COMMA exclusion	{ $$ = prependChild($1, $3); }
	*/
	exclusion						{ $$ = $1; }
    | exclusions COMMA exclusion	{ $$ = $1 + ', ' + $3; }
	;

exclusion :
	ident_name BY cause_of_loss		{ $$ = $1 + ' by ' + $3; }
	//{ $$ = [ {Key: 'Exclusion', Value: $1 + ' by ' + $3} ]; }
	;

/* Attachment basis specifies a time filter on which losses to consider. */
attachment_basis : 
    loss_occurrence_basis               {$$ = {'AttachmentBasis' : 'Loss Occurring'}; }
    | risk_attachment_basis				{$$ = {'AttachmentBasis' : 'Risk Attaching'}; }   
    //| risk_attachment_basis AND loss_occurrence_basis { $$ = $3.concat( $1 ); }
	//| loss_occurrence_basis AND risk_attachment_basis { $$ = $3.concat( $1 ); }
    ;

/* Default basis is loss occurring from inception to expiration */
loss_occurrence_basis : 
    LOSS OCCURRING optional_date_range   { $$ = [ {Key : 'LossOccurrenceBasis', Value : 'Loss Occurring During'}, {Key : 'LossOccurrenceValue', Value : $3} ]; }
    ;

risk_attachment_basis :
    RISK ATTACHING optional_date_range { $$ = [ {Key : 'RiskAttachmentBasis', Value : 'Risk Attaching During'}, {Key : 'RiskAttachmentValue', Value : $3} ];  }
    | INFORCE POLICIES optional_date_range  { $$ = [ {Key : 'RiskAttachmentBasis', Value : 'In-force Policies During'}, {Key : 'RiskAttachmentValue', Value : $3} ]; }
    | POLICIES ISSUED optional_date_range   { $$ = [ {Key : 'RiskAttachmentBasis', Value : 'Policies Issued During'}, {Key : 'RiskAttachmentValue', Value : $3} ]; }
    ;

/* Hours clauses define maximum duration of a loss occurrence */
hours_clauses : 
    hours_clause                           { $$ = [ $1 ]; }
    | hours_clauses AND hours_clause       { $$ = prependChild( $1, $3 ); }
    ;

hours_clause : 
    duration cause_of_loss_constraint onlyonce { $$ = _.extend($1, $2, $3); }
    ;

onlyonce :
	/* empty */ { $$ = { 'OnlyOnce' : 'False' }; }
	| ONLYONCE		{ $$ = { 'OnlyOnce' : 'True' }; }
	;

/* References to a Product or a set of Using definitions must include a version to ensure immutability. */
versioned_ref : 
    ident_name version                           { $$ = "" + $1 + $2; }
    ;

/* */
/*  Covers */
/* */

/* If there is more than one cover, each must be named */
covers : 
    cover				{ $$ = [_.extend($1, {"Index" : 1})]; }
    | named_covers		{ coverIndex = 1; $$ = $1; }
	| ASPER SECTIONS	{ $$ = []; }
    ;

named_covers : 
    named_cover        
		{
			coverIndex++; 
			$$ = [_.extend($1, {"Index" : coverIndex-1})]; 
		}
    | named_covers named_cover  
		{
			coverIndex++;  
			$$ = appendChild($1, _.extend($2, { "Index" : coverIndex-1})); 
		}
    ;

named_cover : 
    label cover   { $$ = _.extend($2, {"Label" : $1}); }
    ;

/* A cover has a participation, a payout, an attachment and may have a constraint on subject */
cover : 
    participation payout attachment cover_subject_constraint { $$ = _.extend({'Participation' : $1}, $2, $3, $4); }
    ;

/* Participation is required on all covers */
participation : 
    ratio SHARE  
    ;

/* Payout - if empty, cover will pay all losses it sees (above the attachment) */
/*    note: OF is inserted to make contract more readable, e.g., 100% SHARE of 1M */
payout : 
    /* empty */ { $$ = {}; }
    | OF payout_spec time_basis  
		{ 
			$$ = _.extend(
							{ 'LimitTimeBasis' : $3['TimeBasis'] }, 
							{ 'LimitReinstatements' : $3['NumberReinstatements'] }, 
							{ LimitSpecification: $2 }
						); 
		}
    ;

payout_spec : 
    expression		{ $$ = _.extend($1, { PAY: 'False' }) ; }
    | PAY expression   { $$ = _.extend($2, { PAY : 'True' }) ; }
    ;

/* Attachment - Cover attaches if cover subject exceeds the attachment. */
/*    If empty, cover will always attach. */
/*    If cover attaches, subject is reduced by value of attachment, unless it is a franchise. */
attachment : 
    /* empty */  { $$ = {}; }
    | XS expression franchise time_basis  
	{  
		//attachmentAmount =  _.findWhere($2, {Key: 'Amount'});
		//attachmentCurrency =  _.findWhere($2, {Key: 'Currency'});
		//attachmentFunction =  _.findWhere($2, {Key: 'Function'});
		$$ = _.extend(
						{	
							//'AttachmentAmount' : (attachmentAmount)?attachmentAmount.Value:'' , 
							//'AttachmentCurrency' : (attachmentCurrency)?attachmentCurrency.Value:'' , 
							//'AttachmentFunction' : (attachmentFunction)?attachmentFunction.Value:'' 
							'AttachmentTimeBasis' : $4['TimeBasis'] 
						}, 
						_.extend({AttachmentSpecification: $2}, $3)
					); 
	}
    ;

/* Covers can derive subject from child covers and sections or by constraining the contract subject */
cover_subject_constraint : 
    derived_subject	filter resolution			{ $$ = _.extend({DerivedSubject: $1}, $2, $3); }
    | subject_constraint resolution     { $$ = _.extend($1,$2); }
    ;

/* Derived subject can reference other covers or sections by name */
derived_subject : 
    ON vector			// { $$ = [ 'Function', {}, 'Sum', $2 ]; }		
		{ $$ = {'ExpressionType': 'FunctionInvocation<double>', 
			'FunctionName' : 'SUM', 
			'FunctionParameterValueType': 'SymbolicExpression',
			'Value': $2.replace(/^\[/,'').replace(/\]$/,'')}; }
	| ON ident_names				// { $$ = { 'ChildCoverFunction' : 'Sum' , 'ChildCovers' : $2 }; }              // { $$ = [ 'Function', {}, 'Sum', $2 ]; }
		{	Parameters = $2.trim().split(",");
			JSONValue = Parameters.map(function(x) { return _.extend({ExpressionType: 'SimpleExpression<SymbolicValue>'},{ValueType: 'SymbolicValue'},{Value:x.trim()}); }
									);
			$$ = {'ExpressionType': 'FunctionInvocation<double>', 
			'FunctionName' : 'SUM', 
			'FunctionParameterValueType': 'SimpleExpression<SymbolicValue>',
			'Value': JSONValue}; }
	| ON function_call	
		{ 
			$$ = {'ExpressionType': 'FunctionInvocation<double>', 
			'FunctionName' : $2['FunctionName'], 
			'FunctionParameterValueType': $2['FunctionParameterValueType'],
			'Value': $2['Parameters']}; }
		}
	//{ $$ = [{ Key : 'ChildCoverFunction', Value : $2 },{ Key : 'ChildCovers', Value : $4 }]; }  // { $$ = [ 'Function', {},  $2, $4 ]; }
    //| ON ident_name LPAREN ident_names RPAREN	{ $$ = [{ Key : 'ChildCoverFunction', Value : $2 },{ Key : 'ChildCovers', Value : $4 }]; }  // { $$ = [ 'Function', {},  $2, $4 ]; }
    ;

/* */
/*  Terms */
/* */

/* Terms may be sublimits or deductibles. Every term may have a time_basis, constraint on subject and resolution */
term_modifiers : 
	time_basis subject_constraint resolution  { $$ = _.extend($1, $2, $3); }
    ;

/* Sublimits */
/* A sublimit has an amount and may have a time basis, subject constraint,  and resolution */
sublimits : 
    sublimit									{ $$= [$1]; }
    | sublimits sublimit                        { $$ = appendChild($1, $2); }
    ;

sublimit : 
	unlabelled_sublimit					{ $$ = $1; }
	| label unlabelled_sublimit		{ $$ = _.extend({'Label' : $1}, $2); }
    ;

unlabelled_sublimit : expression term_modifiers            
		{
			sublimitAmount =  _.findWhere($1, {Key: 'Amount'});
			sublimitCurrency =  _.findWhere($1, {Key: 'Currency'});
			sublimitUnit =  _.findWhere($1, {Key: 'Unit'});
			sublimitFunction =  _.findWhere($1, {Key: 'Function'});
			triggerOrCLT = _.findWhere($1, {Key: 'TriggerOrCLT'});
			sublimitIndex++;
			$$ =  _.extend(	((triggerOrCLT)?{'TriggerOrCLT' : triggerOrCLT.Value }:{}),
							((sublimitUnit)?
								((sublimitUnit != '')?
									{'WaitingPeriod' : ((sublimitAmount)?sublimitAmount.Value:'')}:
									{'SublimitAmount' : ((sublimitAmount)?sublimitAmount.Value:'')}):
								{'SublimitAmount' : ((sublimitAmount)?sublimitAmount.Value:'')}),
							{ 'SublimitCurrency' : ((sublimitCurrency)?sublimitCurrency.Value:'') },
							{ 'SublimitFunction' : ((sublimitFunction)?sublimitFunction.Value:'') },		
							$1,$2);
		} 
    ;

/* Deductibles */
/* A deductible has an amount, may be franchise, may interact with other deductibles. */
/* As with sublimits, it may have a time basis, subject constraint, and resolution */
deductibles : 
    deductible               					{ $$ = [$1]; }
    | deductibles deductible                    { $$ = appendChild($1, $2); }
    ;

deductible :
	unlabelled_deductible				{ $$ = $1; }
	| label unlabelled_deductible		{ $$ = _.extend({'Label' : $1}, $2); }
	;

unlabelled_deductible : expression franchise interaction term_modifiers 
		{
			deductibleUnit =  _.findWhere($1, {Key: 'Unit'});
			triggerOrCLT = _.findWhere($1, {Key: 'TriggerOrCLT'});
			deductibleIndex++;
			$$ = _.extend({ 'Index' : deductibleIndex }, $1, $2, $3, $4); 
		} ;

/* Default interaction is min deductible. Could instead be Max deductible or single largest deductible */
interaction : 
    /* empty */  { $$ = { 'Interaction' : 'MIN' }; }
    | MIN        { $$ = { 'Interaction' : 'MIN' } }
	| MAX        { $$ = { 'Interaction' : 'MAX' } }
	| MAXIMUM        { $$ = { 'Interaction' : 'MAX' } }
    | SINGLELARGEST { $$ = { 'Interaction' : 'SINGLELARGEST' } }
    ;

/* Sections, if present, are introduced by the keyword 'Sections' */
section_part : 
    /* empty */				{ $$ = []; }
    |  sections				{ $$ = $1; }
	|  SECTIONS sections	{ $$ = $2; }
	;

/* */
/* Sections */
/* */
sections : 
    section						{ $$ = [$1]; }
    | sections section			{ $$ = appendChild($1, $2); }
	;

/* A section has a name, and consists of declarations and covers. It does not contain sections or terms. */
/* A section is basically a named subcontract: It is executed within the context of the enclosing contract  */
/* after the declarations and terms, and before the covers.  The payout of a section is determined by  */
/* its covers, and can be referenced by other covers using its section name. */
section :  
	SECTION label section_declaration_part cover_part { $$ = _.extend({'SectionName' : $2}, $3, {Covers: $4}); }
	;

section_declaration_part :
	/* empty */					{ $$ = {}; }
	| DECLARATIONS section_declarations { $$ =  $2; }
    ;	

section_declarations : 
	/* empty */					{ $$ = {}; }
    | declarations               { $$ = $1; }
    ;

covers_are_part :
	COVERNAMESARE LPAREN ident_names RPAREN  { $$ = {'CoverNames' : $3 }; }
	;


/* Expressions to compute term (and cover term) amounts */

vector_value_expressions :
    vector_value_expression						
		{
			literalAmount =  _.findWhere($1, {Key: 'LiteralAmount'});
			amount = _.findWhere($1, {Key: 'Amount'});
			$$ = '' + ((literalAmount)?literalAmount.Value:((amount)?amount.Value:'')); 
		}
    | vector_value_expressions COMMA vector_value_expression
		{ 
			literalAmount =  _.findWhere($3, {Key: 'LiteralAmount'});
			amount = _.findWhere($3, {Key: 'Amount'});
			$$ = $1 + ',' + ((literalAmount)?literalAmount.Value:((amount)?amount.Value:'')); 
		}
	;

vector_value_expression :
	simple_expression						{ $$ = [{Key: 'Amount', Value: $1}]; }
    | parenthesized_arithmetic_expression	{ $$ = [{Key: 'Amount', Value: $1}]; }
	| special_function						{ $$ = _.flatten($1); }
    | function_call				
		{ 
			functionName =  _.findWhere($1, {Key: 'FunctionName'});
			functionParams =  _.findWhere($1, {Key: 'Parameters'});
			$$ = [	{ Key: 'Function', Value : (functionName)?functionName.Value:'' }, 
					{ Key: 'Amount', Value : (functionName)
											 ? (functionParams)
											   ? functionName.Value + '(' + functionParams.Value + ')'
											   : functionName.Value + '()'
											 : (functionParams)
											   ? functionParams.Value
											   : '' 
					} 
				]; 
		}
    | money								{ $$ = $$ = [{Key: 'LiteralAmount', Value: $1}]; }
	| percentage						{ $$ = $$ = [{Key: 'LiteralAmount', Value: $1}]; }
	;

ratio : 
	simple_expression
		{  
			$$ =  { ExpressionType: 'SimpleExpression<' + $1['ValueType'] + '>', 
			ValueType: $1['ValueType'], Value: $1['Value'] };
		}
	| parenthesized_arithmetic_expression	{ $$ = $1 }
	| function_call							{ $$ = $1 }
	| percentage
		{  
			$$ =  $1;
		}
	;

percentage : 
    simple_expression PERCENT						
		{  
			$$ =  { ExpressionType: 'Percentage<' + $1['ValueType'] + '>', 
			ValueType: $1['ValueType'], Value: $1['Value'] };
		}
	| parenthesized_arithmetic_expression PERCENT	{ $$ = "" + $1 + "%" }
	| function_call PERCENT	{ $$ = "" + $1 + "%" }
	;

simple_expression : 
    number						{ $$ = { ValueType: 'NumericValue', Value: $1 }; }
    | ident_name				{ $$ = { ValueType: 'SymbolicValue', Value: $1 }; }
	| vector_ident_name			{ $$ = $1 }
	| SUBJECT					{ $$ = { ValueType: 'SymbolicValue', Value: $1 }; }
    ;

parenthesized_arithmetic_expression :
	LPAREN arithmetic_expression RPAREN						{ $$ = $2; }
	;

parenthesized_arithmetic_expressions : 
    parenthesized_arithmetic_expression						{ $$ = $1; }
    | parenthesized_arithmetic_expressions COMMA parenthesized_arithmetic_expression	{ $$ = $1 + ', ' + $3; }
	;

arithmetic_expression :
	arithmetic_expression PLUS arithmetic_term 		
		{ 
			$$ = 
				{
					LeftOperandExpression : $1,
					Operator : 'PLUS',
					RightOperandTerm : $3
				}				
		}
	| arithmetic_expression MINUS arithmetic_term	
		{ 
			$$ = 
				{
					LeftOperandExpression : $1,
					Operator : 'MINUS',
					RightOperandTerm : $3
				}				
		}
	| arithmetic_term 		
		{ 
			$$ = 
				{
					RightOperandTerm : $1
				}				
		}
	;

arithmetic_term :
	arithmetic_term TIMES arithmetic_factor					
		{ 
			$$ = 
				{
					LeftOperandTerm : $1,
					Operator : 'MULTIPLY',
					RightOperandFactor : $3
				}				
		}
	| arithmetic_term SLASH arithmetic_factor	
		{ 
			$$ = 
				{
					LeftOperandTerm : $1,
					Operator : 'DIVIDE',
					RightOperandFactor : $3
				}				
		}
	| arithmetic_factor		
		{ 
			$$ = 
				{
					RightOperandFactor : $1
				}				
		}
	| MINUS arithmetic_factor
		{ 
			$$ = 
				{
					LeftOperandTerm :
						{
							ExpressionType: 'SimpleExpression<NumericValue>', 
							ValueType: 'NumericValue', 
							Value: -1
						},
					Operator : 'MULTIPLY',
					RightOperandFactor : $2
				}				
		}
	;

arithmetic_factor :
    expression	{ $$ = $1; }
	;

expression : 
	simple_expression
		{  
			$$ =  
				{
					ExpressionType: 'SimpleExpression<' + $1['ValueType'] + '>', 
					ValueType: $1['ValueType'], 
					Value: $1['Value']
				};
		}
    | parenthesized_arithmetic_expression
		{ 
			$$ = 
				{
					ExpressionType: 'ArithmeticExpression', 
					ValueType: 'Value', 
					Value: $1
				}; 
		}
	| special_function						
		{ 
			$$ = 
				{
					ExpressionType: $1['ExpressionType'], 
					ValueType: 'Value', 
					Value: $1['Value'],
					FunctionParameterValueType: $1['FunctionParameterValueType'],
					FunctionName: $1['FunctionName']
				}; 
		}
    | function_call				
		{ 
			$$ = {'ExpressionType': 'FunctionInvocation<double>', 
			'FunctionName' : $1['FunctionName'], 
			'FunctionParameterValueType': $1['FunctionParameterValueType'],
			'ValueType': $1['ValueType'], 
			'Value': $1['Parameters']}; }
		}
    | money								{ $$ = $1; }
	| clt_function						{ $$ = $1; }
	| special_arithmetic_factor			{ $$ = $1; }
	;

expressions : 
    expression						
		{
			//literalAmount =  _.findWhere($1, {Key: 'LiteralAmount'});
			//amount = _.findWhere($1, {Key: 'Amount'});
			//$$ = '' + ((literalAmount)?literalAmount.Value:((amount)?amount.Value:'')); 
			$$ = [$1];
		}
    | expressions COMMA expression	
		{ 
			//literalAmount =  _.findWhere($3, {Key: 'LiteralAmount'});
			//amount = _.findWhere($3, {Key: 'Amount'});
			//$$ = $1 + ', ' + ((literalAmount)?literalAmount.Value:((amount)?amount.Value:'')); 
			$$ = appendChild($1, $3); 
		}
	;


clt_function :
	simple_expression CLTBEGIN ident_name RPAREN
		{
			$$ = [ 
					{Key: 'Function', Value : 'CLT(' + $3 + ')' },
					{Key: 'Amount', Value: $1 },
					{Key: 'TriggerOrCLT', Value: $3 },
				 ]; 
		}
	| money CLTBEGIN ident_name RPAREN
		{
			amount =  _.findWhere($1, {Key: 'Amount'});
			amountValue = (amount)?amount.Value:'';
			currency =  _.findWhere($1, {Key: 'Currency'});
			currencyValue = (currency)?currency.Value:'';
			$$ = [ 
					{Key: 'Function', Value : 'CLT(' + $3 + ')' },
					{Key: 'Amount', Value: amountValue },
					{Key: 'Currency', Value: currencyValue },
					{Key: 'TriggerOrCLT', Value: $3 },
				 ]; 
		}
	|  percentage risk_size scope CLTBEGIN ident_name RPAREN
		{
			amount =  _.findWhere($1, {Key: 'Amount'});
			amountValue = (amount)?amount.Value:'';
			currency =  _.findWhere($1, {Key: 'Currency'});
			currencyValue = (currency)?currency.Value:'';
			$$ = [ 
					{Key: 'Function', Value : 'CLT(' + $5 + ')' },
					{Key : 'Amount', Value : '%' + ' of ' + $2.Value + ' ' + $3.Value },
					{Key : 'LiteralAmount', Value : $1.slice(0,-1) + '%' + ' ' + $2.Value + ' ' + $3.Value },
					{Key: 'TriggerOrCLT', Value: $5 }
				 ]; 
		}
	;

/* Functions may be builtin or user defined. User defined functions are brought in via a USING declaration */

function_call : 
	//ident_name LPAREN expressions RPAREN		{ $$ = [ {Key: 'Function', Value: $1.slice(0,-1)}, {Key: 'Parameters', Value: $3 } ]; }
	FUNCBEGIN expressions RPAREN // { $$ = [ {Key: 'Function', Value: $1.slice(0,-1)}, {Key: 'Parameters', Value: $2 } ]; }
		{
			$$ = {'ExpressionType': 'FunctionInvocation<double>', 
			'FunctionName' : $1.slice(0,-1), 
			'FunctionParameterValueType': 'IExpression<Value>',
			'ValueType': 'Value',
			'Parameters': $2 };
		}
	//| special_function_name LPAREN expressions RPAREN	{ $$ = [ {'Function' : $1}, {'Parameters' : $3 } ]; }
	;

/* All function names end with a left paren */
special_function_name : 
    MIN               { $$ = 'Min'; }
    | MAX             { $$ = 'Max'; }
    | SUM             { $$ = 'Sum'; }
    ;

special_function : 
    percentage risk_size scope  
		{ $$ = {'ExpressionType': 'FunctionInvocation<Value>', 
			'FunctionName' : $2['Value'], 
			'FunctionParameterValueType': 'IExpression<Value>',
			'Value': [$1, $3] }; }
    | duration                  
		{ 
			durationAmount =  $1['Duration'];
			durationAmountValue = (durationAmount)?durationAmount.Value:'';
			durationTimeUnit =  $1['DurationTimeUnit'];
			durationTimeUnitValue = (durationTimeUnit)?durationTimeUnit.Value:'';
			$$ = {'ExpressionType': 'FunctionInvocation<Value>', 
			'FunctionName' : 'WaitingPeriod', 
			'FunctionParameterValueType': 'IExpression<Value>',
			'Value': [{ExpressionType: 'NumericExpression', ValueType: 'NumericValue', Value: durationAmount}, 
					  {ExpressionType: 'SymbolicExpression', ValueType: 'SymbolicValue', Value: durationTimeUnit}] }
		}
		/*
	| simple_expression CLTBEGIN ident_name RPAREN
		{
			$$ = [ 
					{Key: 'Function', Value : 'CLT(' + $3 + ')' },
					{Key: 'Amount', Value: $1 },
					{Key: 'TriggerOrCLT', Value: $3 },
				 ]; 
		}
	| money CLTBEGIN ident_name RPAREN
		{
			amount =  _.findWhere($1, {Key: 'Amount'});
			amountValue = (amount)?amount.Value:'';
			currency =  _.findWhere($1, {Key: 'Currency'});
			currencyValue = (currency)?currency.Value:'';
			$$ = [ 
					{Key: 'Function', Value : 'CLT(' + $3 + ')' },
					{Key: 'Amount', Value: amountValue },
					{Key: 'Currency', Value: currencyValue },
					{Key: 'TriggerOrCLT', Value: $3 },
				 ]; 
		}
		*/
	;

risk_size : 
    REPLACEMENTCOST				{ $$ = {ExpressionType: 'SymbolicExpression', ValueType: 'SymbolicValue', Value: 'RCV'}; }
    | TOTALSUMINSURED			{ $$ = {ExpressionType: 'SymbolicExpression', ValueType: 'SymbolicValue', Value: 'TSI'}; }
    | CASHVALUE					{ $$ = {ExpressionType: 'SymbolicExpression', ValueType: 'SymbolicValue', Value: 'CashValue'}; }
	| LOSS						{ $$ = {ExpressionType: 'SymbolicExpression', ValueType: 'SymbolicValue', Value: 'Subject'}; }
	;

scope : 
    /* empty */                 { $$ = {ExpressionType: 'SymbolicExpression', ValueType: 'SymbolicValue', Value: 'Affected'}; }
    | AFFECTED                  { $$ = {ExpressionType: 'SymbolicExpression', ValueType: 'SymbolicValue', Value: 'Affected'}; }
    | COVERED                   { $$ = {ExpressionType: 'SymbolicExpression', ValueType: 'SymbolicValue', Value: 'Covered'}; }
    ;

/* All terms are per occurrence by default */
time_basis :  
    /* empty */				{ $$ = {'TimeBasis' : 'Occurrence', 'NumberReinstatements' : -1 }; }
    | reinstatement_phrase	{ $$ = {'TimeBasis' : 'Occurrence', 'NumberReinstatements' : $1['NumberReinstatements'] }; }
    | AGGREGATE				{ $$ = {'TimeBasis' : 'Aggregate', 'NumberReinstatements' : -1 }; }
	| PEROCCURRENCE			{ $$ = {'TimeBasis' : 'Occurrence', 'NumberReinstatements' : -1 }; }
    | PEROCCURRENCE reinstatement_phrase { $$ = {'TimeBasis' : 'Occurrence', 'NumberReinstatements' : $2['NumberReinstatements'] }; }
    ;

reinstatement_phrase :
    WITH NUMBERWITHOPTIONALMULTIPLIER REINSTATEMENTS	{ $$ = {'NumberReinstatements' : $2 }; }
	;


/* Does term apply to each risk separately?  By default, no. */
resolution :  
    /* empty */  { $$ = {'PerRisk' : 'False'}; } 
    | PERRISK    { $$ = {'PerRisk' : 'True'}; } 
    ;

/* Deductibles and attachment can be standard or franchise. */
franchise :  
    /* empty */  { $$ = {'IsFranchise' : 'False' }; }  
	| STANDARD  { $$ = {'IsFranchise' : 'False' }; }
    | FRANCHISE  { $$ = {'IsFranchise' : 'True' }; }
    ;

/* Subject constraints - used on terms and covers as constraints against contract subject */
subject_constraint : 
	outcome_type_constraint schedule_constraint cause_of_loss_constraint filter  { $$ = _.extend($1,$2,$3,$4); } ;

outcome_types : 
    outcome_type						{ $$ = $1; }
    | outcome_types COMMA outcome_type  { $$ = $1 + ', ' + $3; }
    ;

outcome_type : 
    ident_name				{ $$ = $1; }
	| vector_ident_name		{ $$ = $1; }
    | outcome				{ $$ = $1; }
	;

outcome_type_constraint : 
    /* empty */					{ $$ = { 'ExposureTypes' : '' } ; } 
    |  FOR outcome_types        { $$ = { 'ExposureTypes' : $2 } ; } 
    ;

schedules : 
    schedule							{ $$ = $1; }
    | schedules COMMA schedule			{ $$ = $1 + ', ' + $3; }
	;

schedule : 
    ident_name								{ $$ = $1; }
	| vector_ident_name						{ $$ = $1; }
	;

schedule_constraint : 
    /* empty */				   { $$ = { 'Schedule' : '' } ; } 
    | TO schedules             { $$ = { 'Schedule' : $2 } ; } 
    ;

causes_of_loss : 
    cause_of_loss                        { $$ = $1; }       
    | causes_of_loss COMMA cause_of_loss { $$ = $1 + ', ' + $3; }
    ;

cause_of_loss : 
	ident_name								{ $$ = $1; }
	| vector_ident_name								{ $$ = $1; }
	;

cause_of_loss_constraint : 
    /* empty */                  { $$ = { 'CausesOfLoss' : '' }; } 
    | BY causes_of_loss          { $$ = { 'CausesOfLoss' : $2 }; } 
    ;

/* Money */
money : 
	simple_expression known_currency_unit
		{ $$ = 
			{ 
				ValueType: 'MoneyValue<'+$1['ValueType']+'>',
				ExpressionType: 'Money<'+$1['ValueType']+'>',
				MonetaryExpressionValueType: $1['ValueType'],
				MonetaryExpressionType: 'SimpleExpression<' + $1['ValueType'] + '>', 
				Value: $1['Value'],
				Currency: $2
			};
		}
	| parenthesized_arithmetic_expression known_currency_unit	{ $$ = [ { Key: 'Amount', Value: $1 }, { Key: 'Currency', Value: $2 }];}
	| function_call known_currency_unit						{ $$ = [ { Key: 'Amount', Value: $1 }, { Key: 'Currency', Value: $2 }];}
	//| special_function known_currency_unit						{ $$ = [ { Key: 'Amount', Value: $1 }, { Key: 'Currency', Value: $2 }];}
	;

known_currency_unit :
/*
	KNOWNCURRENCY
	;
*/	AED
| AFA
| AFN
| ALL
| AMD
| ANG
| AOA
| AON
| ARS
| ATS
| AUD
| AZM
| BAM
| BDT
| BEF
| BGN
| BHD
| BIF
| BMD
| BND
| BOB
| BRL
| BTN
| BWP
| BYR
| BZD
| CAD
| CDF
| CHF
| CLF
| CLP
| CNY
| COP
| CRC
| CVE
| CYP
| CZK
| DEM
| DJF
| DKK
| DZD
| EEK
| EGP
| ERN
| ETB
| EUR
| FJD
| FKP
| FRF
| GBP
| GEL
| GHC
| GIP
| GMD
| GNF
| GRD
| GTQ
| GYD
| HKD
| HNL
| HRK
| HUF
| IDR
| IEP
| ILS
| INR
| IQD
| IRR
| ISK
| ITL
| JMD
| JOD
| JPY
| KES
| KGS
| KHR
| KMF
| KPW
| KRW
| KWD
| KZT
| LAK
| LBP
| LKR
| LRD
| LSL
| LTL
| LUF
| LVL
| LYD
| MAD
| MDL
| MGA
| MKD
| MMK
| MNT
| MRO
| MTL
| MUR
| MVR
| MWK
| MXN
| MXP
| MYR
| MZM
| NAD
| NGN
| NIO
| NLG
| NOK
| NPR
| NZD
| OMR
| PAB
| PEN
| PGK
| PHP
| PKR
| PLN
| PTE
| PYG
| QAR
| RON
| RUB
| RWF
| SAR
| SBD
| SCR
| SDD
| SEK
| SGD
| SHP
| SIT
| SKK
| SLL
| SOS
| SRD
| STD
| SVC
| SYP
| SZL
| THB
| TJS
| TMM
| TND
| TOP
| TRL
| TRY
| TWD
| TZS
| UAH
| UGX
| USD
| UYU
| UZS
| VEB
| VEF
| VND
| VUV
| WST
| XAF
| XOF
| XPF
| YER
| ZAR
| ZMK
| ZWD
| AOR
| AWG
| AZN
| BBD
| BGL
| BOV
| BSD
| CHE
| CHW
| COU
| CUP
| DOP
| GHS
| HTG
| KYD
| MGF
| MOP
| MXV
| MZN
| ROL
| RSD
| SDG
| SRG
| SSP
| TTD
| UD1
| UD2
| UD3
| UD4
| UD5
| US
| USN
| USS
| UYI
| UYP
| XAG
| XAU
| XBA
| XBB
| XBC
| XBD
| XCD
| XDR
| XPD
| XPT
| XSU
| XTS
| XUA
| XUF
| XXX
| YUN
| ZWL
;

/* Time */
optional_date_range :
    /* empty */             { $$ = ''; }
    | FROM date UNTIL date  { $$ = "" + 'From ' + $2 + ' To ' + $4; }
    | UNTIL date            { $$ = "" + 'To ' + $2; }
    ;

date : 
     number month year { $$ = "" + $1 + ' ' + $2 + ' ' + $3; } 
     ;

duration : 
    number time-unit  { $$ = {Duration : $1, DurationTimeUnit : $2['Value']}; }
	;

time-unit : 
    HOUR				{ $$ = {ExpressionType: 'SymbolicExpression', ValueType: 'SymbolicValue', Value: 'Hours'}; }
    | DAY				{ $$ = {ExpressionType: 'SymbolicExpression', ValueType: 'SymbolicValue', Value: 'Days'}; }
    | WEEK				{ $$ = {ExpressionType: 'SymbolicExpression', ValueType: 'SymbolicValue', Value: 'Weeks'}; }
    | MONTH				{ $$ = {ExpressionType: 'SymbolicExpression', ValueType: 'SymbolicValue', Value: 'Months'}; }
    | YEAR				{ $$ = {ExpressionType: 'SymbolicExpression', ValueType: 'SymbolicValue', Value: 'Years'}; }
	;

month : 
    JAN
    | FEB
    | MAR
    | APR
    | MAY
    | JUN
    | JUL
    | AUG
	| SEP
	| OCT
    | NOV
    | DEC ;

day-of-month : 
    number ;

year : 
    number ;

/* Certain lists (currently PARAMETERS) may include keywords. */
special_name : 
	/* empty */
    | ident_name
	| vector_ident_name
	| NAME
    | TYPE
    | SHARE
    | SUBJECT
    | INCEPTION
    | EXPIRATION
    | CURRENCY
	| ATTACHMENTBASIS 
	| RISK
	;

special_names : 
    special_name							{ productParams[$1] = true; $$ = ($1)?[{Key: $1, IsVector:  /\[\]/.test($1)?1:0}]:[]; }
    | special_names COMMA special_name		{ productParams[$3] = true; $$ = prependChild($1, {Key: $3, IsVector:  /\[\]/.test($3)?1:0}); }
    ;

special_arithmetic_factor :
	INCEPTION
    | EXPIRATION
	| TOTALSUMINSURED
	;

label :
	ident_name COLON						{ $$ = $1; } // { $$ = $1.slice(0,-1); }
	| vector_ident_name COLON				{ $$ = $1; } 
	;

vector_ident_name :
	ident_name LSQUARE RSQUARE				{ $$ = $1+"[]"; }
	;

vector :
	LSQUARE vector_value_expressions RSQUARE				{ $$ = "[" + $2 + "]"; }
	;

vector_default :
	LSQUARE vector_value_expressions COMMA ELLIPSIS RSQUARE	{ $$ = "[" + $2 + ",...]"; }
	;

/* Un-roll (i.e. horizontally expand) with a vector's list of values */
vector_dereference :
	ident_name LSQUARE TIMES RSQUARE		{ $$ = $1+"[*]"; }
	;
	

ident_names : 
    ident_name								{ $$ = $1; }
	| vector_ident_name						{ $$ = $1; }
	| vector_dereference					{ $$ = $1; }
    | ident_names COMMA ident_name			{ $$ = $1 + ', ' + $3; }
	| ident_names COMMA vector_ident_name	{ $$ = $1 + ', ' + $3; }
	| ident_names COMMA vector_dereference	{ $$ = $1 + ', ' + $3; }
    ;



/* Low level input */
ident_name : 
    IDENT ;

user-defined-function : 
    ident_name ;

number : 
	NUMBERWITHOPTIONALMULTIPLIER	
		{ 
			multiplier = 1;
			number = $1.match(/^\d+(\.\d+)?/)[0];
			if($1.toLowerCase().match(/k|thousand|m|million|b|billion/))
			{
				multiplierSymbol = $1.toLowerCase().match(/k|thousand|m|million|b|billion/)[0];
				switch(multiplierSymbol.trim())
				{
					case "k":
					case "thousand":
						multiplier = 1000;
						break;
					case "m":
					case "million":
						multiplier = 1000000;
						break;
					case "b":
					case "billion":
						multiplier = 1000000000;
						break;
					case "trillion":
						multiplier = 1000000000000;
						break;
				};
			}
			$$ = number * multiplier; 
		}
    /*
	INTEGER							{ $$ = $1; }
    | FLOAT							{ $$ = $1; }
	| INTEGER multiplier			{ $$ = $1 * $2; }
	| FLOAT multiplier				{ $$ = $1 * $2; }
	*/
	;

/*
multiplier : 
	MULTIPLIER		
		{ 
			multiplier = 1;
			switch($1.toLowerCase().trim())
			{
				case "k":
				case "thousand":
					multiplier = 1000;
					break;
				case "m":
				case "million":
					multiplier = 1000000;
					break;
				case "b":
				case "billion":
					multiplier = 1000000000;
					break;
			};
			$$ = multiplier; 
		}
	;
*/

version : 
    phrase ;

phrase :
	PHRASE;

/*
phrase : 
    LBRACE phraselistitems RBRACE ;

knownCurrencies :
	phraselistitems
	;

phraselistitems :
	phraselistitem
	| phraselistitems COMMA phraselistitem
	;

phraselistitem :
	PHRASELISTITEM
	;
*/