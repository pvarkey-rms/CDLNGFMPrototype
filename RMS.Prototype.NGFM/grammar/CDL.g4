grammar CDL;

@parser::members
{
	protected const int EOF = Eof;
}

@lexer::members
{
	protected const int EOF = Eof;
	protected const int HIDDEN = Hidden;
}

cdl : contract | product;
// A CDL contract consists of the keyword ‘Contract’ followed by declarations, covers, terms and sections
contract : CONTRACT declarationPart coverPart sectionPart sublimitPart deductablePart;
// A CDL product consists of the keyword ‘Product’ followed by product declarations, covers, terms and sections
product : PRODUCT productName productDeclarationPart coverPart sectionPart sublimitPart deductablePart;
//  A product name is an identifier
productName  :  identName | phrase;
//  Every contract has a list of declarations, introduced by the keyword ‘Declarations’
declarationPart  :  DECLARATIONS declarations;
//  Every product has a list of declarations, introduced by the keyword ‘Declarations’
productDeclarationPart  :  DECLARATIONS productDeclarations;
// Every contract has an optional list of covers, introduced by the keyword ‘Covers’.
// Covers specify which claims get paid, and for how much
coverPart :  /* empty */ | COVERS covers;

// A contract has an optional list of terms, which consist of sublimits followed by deductibles.
// Terms provide ‘haircuts’ which reduce the subject seen by the covers. 
// Terms can be sublimits or deductibles. 
// Sublimits, if present, are introduced by the keyword ‘Sublimits’
sublimitPart : /* empty */  | SUBLIMITS sublimits;
// Deductibles, if present, are introduced by the keyword ‘Deductibles’
deductablePart : /* empty */  | DEDUCTIBLES deductibles;
// Sections, if present, are introduced by the keyword ‘Sections’
sectionPart : /* empty */   |  SECTIONS sections;

//
// Declarations
//
declarations : declaration | declarations declaration;

declaration : 
      SUBJECT IS contractSubject #subjectDeclaration
	  | INCEPTION IS date #inceptionDeclDate
      | INCEPTION IS phrase #inceptionDeclPhrase
	  | INCEPTION IS expression #inceptionDeclExp
      | EXPIRATION IS date #expirationDeclDate
      | EXPIRATION IS phrase #expirationDeclPhrase
	  | EXPIRATION IS expression #expirationDeclExp
      | CURRENCY IS (currencyUnit | phrase)  #currencyDeclaration
      | CLAIMSADJUSTMENTOPTIONS ARE LPAREN adjustmentOptions RPAREN #claimAdjustmentOptionsDeclaration
      | ATTACHMENTBASIS IS attachmentBasis #attachmentBasisDeclaration
      | OCCURRENCES ARE LPAREN hoursClauses RPAREN #occurencesDeclaration
	  | EXCLUSIONS ARE LPAREN exclusions RPAREN #exclusionsDeclaration
      | USING versionedRef #usingDeclaration
      | PRODUCT IS versionedRef #productDecl
      | TYPE IS identName #typeDeclaration
      | TYPE IS phrase #typeDeclaration2
      | RISK IS EACH CONTRACT #riskPerContractDeclaration
      | RISK IS EACH SECTION  #riskPerSectionDeclaration
      | RISK IS EACH LOCATION #riskPerLocationDeclaration
      | RISK IS EACH identName #riskDeclaration
      | NAME IS identName #nameDeclaration
      | NAME IS phrase #nameDeclaration2
	  | IDENT '(' paramList ')' IS expression #functionDeclaration
      | vectorIdentName IS vector #vectorDeclaration
      | vectorIdentName IS vectorDefault #vectorDefaultDeclaration
      | IDENT IS expression #genericDeclaration
      | IDENT IS phrase #genericDeclaration2
	  ;  

// Product declarations can include a list of parameters
productDeclarations : productDeclaration (productDeclaration)*;

productDeclaration : OPTIONAL PARAMETERS ARE specialNameList #optionalParamsProductDecl
                    |REQUIRED PARAMETERS ARE specialNameList #requiredParamsProductDecl
                    | declaration #otherProductDecl
					;

optionalOrRequired : OPTIONAL | REQUIRED;

//
// Contract subject – every contract has a subject, which defines its universe of claims
//

// Contract subject is a position
contractSubject : position;

// A position may specify a filter as in Acme { policy is ABC and location is 1234 }
position: unfilteredPosition filter;
// A position can be a named position, or a net position or a primary position
unfilteredPosition : 
namedPosition causeOfLossConstraint
| netPosition 
| primaryPosition
;

// Named positions may look like Acme.Gross or BU1,BU2
namedPosition : identNames;

// Net positions specify inuring relationships such as “BU1, BU2 net of PerRisk, Fac”
netPosition : namedPosition NET OF namedPosition;

// Primary positions may look like “Loss, Damage to Accounts”
primaryPosition : outcomes TO namedPosition causeOfLossConstraint;
outcomes : outcome | outcomes COMMA outcome;
outcome : LOSS | DAMAGE | HAZARD;

// Filters are optional.  An example filter is { Account is ABC45_321/2013 }
filter : /* empty */ 
           | phrase
		   ;

//  Claims adjustment options affect how terms are interpreted
adjustmentOptions : 
  adjustmentOption 
| adjustmentOptions COMMA adjustmentOption
;

adjustmentOption : 
DEDUCTIBLES ARE ABSORBABLE 
| DEDUCTIBLES ARE NOT ABSORBABLE
| SUBLIMITS ARE NET OF DEDUCTIBLE
| SUBLIMITS ARE GROUNDUP
;

//  Exclusions
exclusions : 
exclusion 
| exclusions COMMA exclusion;
exclusion : 
identName BY causeOfLoss;

// Attachment basis specifies a time filter on which losses to consider.
attachmentBasis : 
lossOccurrenceBasis 
| riskAttachmentBasis
| riskAttachmentBasis AND lossOccurrenceBasis
| lossOccurrenceBasis AND riskAttachmentBasis;

// Default basis is loss occurring from inception to expiration
lossOccurrenceBasis : LOSS OCCURRING optionalDateRange;

riskAttachmentBasis :
     RISK ATTACHING optionalDateRange
   | INFORCE POLICIES optionalDateRange
   | POLICIES ISSUED optionalDateRange;

// Hours clauses define maximum duration of a loss occurrence
hoursClauses : hoursClause | hoursClauses AND hoursClause;
hoursClause : duration causeOfLossConstraint filter;

versionedRef : identName version | phrase version;

//
//  Covers
//

// If there is more than one cover, each must be named (using a label)
covers : cover | namedCovers;
namedCovers : (namedCover)+;
namedCover : label cover;

// A cover has a participation, a payout, an attachment and may have a constraint on subject
cover : ratio SHARE payout attachment coverSubjectConstraint;

// Payout – if empty, cover will pay all losses it sees (above the attachment)
//    note: OF is inserted to make contract more readable, e.g., 100% SHARE of 1M
payout : /* empty */ 
         | OF payoutSpec timeBasis;
payoutSpec : expression | PAY expression;

// Attachment – Cover attaches if cover subject exceeds the attachment.
//    If empty, cover will always attach.
//    If cover attaches, subject is reduced by value of attachment, unless it is a franchise.
attachment: /* empty */ 
         | XS expression franchise timeBasis;

// Covers can derive subject from child covers and sections or by constraining the contract subject
coverSubjectConstraint : derivedSubject | derivedSubject  filter | subjectConstraint resolution;
// Derived subject can reference other covers or sections by name
derivedSubject : ON vector | ON identNames | ON functionCall;

//
//  Terms
//

// Terms may be sublimits or deductibles. Every term may have a timeBasis, constraint on subject and resolution
termModifiers : timeBasis subjectConstraint resolution;

// Sublimits 
sublimits : sublimit | sublimits sublimit;
// A sublimit can be labeled or unlabelled
sublimit : unlabelledSublimit | labeledSublimit;
// A sublimit has an amount and may have a time basis, subject constraint,  and resolution 
labeledSublimit: label unlabelledSublimit; 
unlabelledSublimit : expression termModifiers;

// Deductibles
deductibles : deductible | deductibles deductible;
// As with sublimits, they may be labelled
deductible : unlabelledDeductible | labeledDeductible;
labeledDeductible : label unlabelledDeductible;
// A deductible has an amount, may be franchise, may interact with other deductibles.
// As with sublimits, it may have a time basis, subject constraint, and resolution
unlabelledDeductible : expression franchise interaction termModifiers;
// Default interaction is min deductible. Could instead be Max deductible or single largest deductible
interaction : /* empty */ | MIN | MAX | MAXIMUM | SINGLELARGEST;

//
// Sections
//
sections : section | sections section;
// A section has a name, and consists of declarations and covers. It does not contain sections or terms.
// A section is basically a named subcontract: It is executed within the context of the enclosing contract 
// after the declarations and terms, and before the covers.  The payout of a section is determined by 
// its covers, and can be referenced by other covers using its section name.
section : SECTION label DECLARATIONS section_Declarartions coversArePart;

section_Declarartions:  
/* empty */
| declarations;

coversArePart: COVERNAMES ARE LPAREN identNames RPAREN;

// Expressions to compute term (and cover term) amounts 
ratio : simpleExpression | parenthesizedArithmeticExpression| functionCall | percentage;
percentage : simpleExpression PERCENT | parenthesizedArithmeticExpression PERCENT | functionCall PERCENT;

fullExpression: expression EOF;

expression : arithmeticExpression
             | vector
             | moneyExpression
			 | dateExpression
             | functionCall 
			 | specialFunction
			 | cltFunction
			 ; 

expressions : expression (COMMA expression)*;

riskSizeExpression : percentage riskSize scope;

specialFunction : riskSizeExpression #riskSizeExp
                  | duration #durationExp
				  ;

cltFunction : simpleExpression CLT '(' IDENT ')'
			  | riskSizeExpression CLT '(' IDENT ')'
			  ;

riskSize : REPLACEMENTCOST | TOTALSUMINSURED | CASHVALUE; 
scope : /* empty */ | AFFECTED | COVERED;

specialFunctionName : MIN | MAX | SUM;

// Functions may be builtin or user defined. User defined functions are brought in via a USING declaration
functionCall : IDENT '(' expressions ')' | specialFunctionName '(' expressions ')'; 

// Arithmetic expressions
parenthesizedArithmeticExpressions : parenthesizedArithmeticExpression | parenthesizedArithmeticExpressions COMMA parenthesizedArithmeticExpression;
parenthesizedArithmeticExpression : LPAREN arithmeticExpression RPAREN;

arithmeticExpression : arithmeticExpression PLUS arithmeticTerm    #plusArithmeticExpression
                   	  | arithmeticExpression MINUS arithmeticTerm  #minusArithmeticExpression
					  | arithmeticTerm                             #arithmeticTermExpression
					  ;
arithmeticTerm : 
arithmeticTerm TIMES arithmeticFactor  #multiplyExp
	| arithmeticTerm SLASH arithmeticFactor #divideExp
	| arithmeticFactor #arithmeticFactorExp
	| MINUS arithmeticFactor #unaryMinusExp
	;

arithmeticFactor :specialFunction
    | simpleExpression								
	| parenthesizedArithmeticExpression
	| functionCall	
	| specialArithmeticFactor
	| percentage
	;

specialArithmeticFactor : 
INCEPTION | EXPIRATION;

simpleExpression : identName             #identifierExpression
				   | vectorIdentName     #vectorIdentifierExpression
                   | moneyExpression     #moneyExp
	         	   | dateExpression      #dateExp
				   | INTEGER multiplier? #integerConstantExpression
                   | FLOAT multiplier?   #floatConstantExpression                 
				   ;

// All terms are per occurrence by default
timeBasis :  /* empty */
              | PEROCCURRENCE
              | AGGREGATE
			  ;

// Does term apply to each risk separately?  By default, no.
resolution :  /* empty */
              | PERRISK
			  ;

// Deductibles and attachment can be standard or franchise.
franchise :  /* empty */
              | FRANCHISE
			  ;

// Subject constraints – used on terms and covers as constraints against contract subject
subjectConstraint : 
outcomeTypeConstraint scheduleConstraint causeOfLossConstraint filter;

causeOfLossConstraint : /* empty */ | BY causeOfLoss (COMMA causeOfLoss)*;
causeOfLoss : identName | vectorIdentName;

outcomeTypes : outcomeType | outcomeTypes COMMA outcomeType;
outcomeType : identName | vectorIdentName | outcome;
outcomeTypeConstraint : /* empty */ | FOR outcomeTypes;

schedules : schedule (COMMA schedule)*;
schedule : identName | vectorIdentName;
scheduleConstraint : /* empty */ | TO schedules;

moneyExpression : number multiplier? currencyUnit;

numberWithMultiplier : number multiplier;

multiplier : 'MILLION'| 'M'| 'K' | 'THOUSAND' | 'BILLION' | 'B'; 

currencyUnit : AED | AFA | AFN | ALL | AMD | ANG | AOA | AON | ARS | ATS | AUD | AZM | BAM | BDT | BEF | BGN | BHD | BIF | BMD | BND | BOB | BRL | BTN | BWP | BYR | BZD | CAD | CDF | CHF | CLF | CLP | CNY | COP | CRC | CVE | CYP | CZK | DEM | DJF | DKK | DZD | EEK | EGP | ERN | ETB | EUR | FJD | FKP | FRF | GBP | GEL | GHC | GIP | GMD | GNF | GRD | GTQ | GYD | HKD | HNL | HRK | HUF | IDR | IEP | ILS | INR | IQD | IRR | ISK | ITL | JMD | JOD | JPY | KES | KGS | KHR | KMF | KPW | KRW | KWD | KZT | LAK | LBP | LKR | LRD | LSL | LTL | LUF | LVL | LYD | MAD | MDL | MGA | MKD | MMK | MNT | MRO | MTL | MUR | MVR | MWK | MXN | MXP | MYR | MZM | NAD | NGN | NIO | NLG | NOK | NPR | NZD | OMR | PAB | PEN | PGK | PHP | PKR | PLN | PTE | PYG | QAR | RON | RUB | RWF | SAR | SBD | SCR | SDD | SEK | SGD | SHP | SIT | SKK | SLL | SOS | SRD | STD | SVC | SYP | SZL | THB | TJS | TMM | TND | TOP | TRL | TRY | TWD | TZS | UAH | UGX | USD | UYU | UZS | VEB | VEF | VND | VUV | WST | XAF | XOF | XPF | YER | ZAR | ZMK | ZWD | AOR | AWG | AZN | BBD | BGL | BOV | BSD | CHE | CHW | COU | CUP | DOP | GHS | HTG | KYD | MGF | MOP | MXV | MZN | ROL | RSD | SDG | SRG | SSP | TTD | UD1 | UD2 | UD3 | UD4 | UD5 | US | USN | USS | UYI | UYP | XAG | XAU | XBA | XBB | XBC | XBD | XCD | XDR | XPD | XPT | XSU | XTS | XUA | XUF | XXX | YUN | ZWL;

// Time 
optionalDateRange :
    /* empty */
    | FROM date UNTIL date
    | UNTIL date
	;

dateExpression: date | duration;
date : identName #dateVar 
       | dayOfMonth month year #dateConst
	   ;
duration : number timeUnit;
timeUnit : HOUR | DAY | DAYS | WEEK | MONTH | YEAR;
month : JAN | FEB | MAR | APR | MAY | JUN | JUL | AUG | SEP | OCT | NOV | DEC;
dayOfMonth : INTEGER;
year : INTEGER;
// Certain lists (currently PARAMETERS) may include keywords.
specialName : identName | vectorIdentName | NAME | TYPE | SHARE | SUBJECT | INCEPTION | EXPIRATION | CURRENCY;
specialNameList : LPAREN specialNames RPAREN;
specialNames : specialName | specialNames COMMA specialName;

paramList  :
           | param (COMMA param)*;
param : identName;
identNames : identifier (COMMA identifier)*;
identifier : identName           #scalarIdentifier
             | vectorIdentName   #vectorIdentifier
			 | vectorDereference #vectorDerefIdentifier
			 ; 

// Vectors
vectorIdentName : identName LSQUARE RSQUARE;
vector: LSQUARE expressions RSQUARE;
vectorDefault : LSQUARE expressions COMMA ELLIPSIS RSQUARE;
vectorDereference : identName LSQUARE TIMES RSQUARE;

// Low level input
identName : SUBJECT
	  | INCEPTION
      | EXPIRATION
      | CURRENCY
      | CLAIMSADJUSTMENTOPTIONS
      | ATTACHMENTBASIS
      | OCCURRENCES
      | PRODUCT
      | TYPE
      | RISK
      | NAME
	  | IDENT;

label : identName COLON #scalarLabel
        | vectorIdentName COLON #vectorLabel
		;
number : INTEGER | FLOAT;
phrase : PHRASE;
version : PHRASE;

/*
 * Lexer Rules
 */

SLASH : '/';
LPAREN : '(';
RPAREN : ')';
LSQUARE : '[';
RSQUARE : ']';
PERCENT : '%';
UNDERSCORE : '_';
SEMICOLON : ';';
COLON : ':';
PLUS : '+';
MINUS : '-';
TIMES : '*';
ELLIPSIS : '...';
DOT : '.';
COMMA : ',';
AND : 'AND';
FOR : 'FOR';
ACV: 'ACV';
PERRISK : 'PER RISK';
PEROCCURRENCE : 'PER OCCURRENCE';
GROUNDUP : 'GROUND UP';
SINGLELARGEST : 'SINGLE LARGEST';
CONTRACT : 'CONTRACT';
PRODUCT  : 'PRODUCT';
LOCATION : 'LOCATION';
NAME : 'NAME';
MIN : 'MIN';
MAX : 'MAX';
SUM : 'SUM';
CLT : 'CLT';
MAXIMUM : 'MAXIMUM';
DECLARATIONS : 'DECLARATIONS';
OPTIONAL: 'OPTIONAL';
REQUIRED: 'REQUIRED';
COVERS : 'COVERS';
SUBLIMITS : 'SUBLIMITS';
DEDUCTIBLES : 'DEDUCTIBLES';
EXCLUSIONS : 'EXCLUSIONS';
SECTIONS : 'SECTIONS';
SUBJECT : 'SUBJECT';
INCEPTION : 'INCEPTION';
EXPIRATION : 'EXPIRATION';
CURRENCY : 'CURRENCY';
CLAIMSADJUSTMENTOPTIONS : 'CLAIMS ADJUSTMENT OPTIONS';
ATTACHMENTBASIS : 'ATTACHMENT BASIS';
OCCURRENCES : 'OCCURRENCES';
OCCURRENCE : 'OCCURRENCE';
USING : 'USING';
SECTION : 'SECTION';
SHARE : 'SHARE';
PARAMETERS : 'PARAMETERS';
LOSS : 'LOSS';
DAMAGE : 'DAMAGE';
HAZARD : 'HAZARD';
NOT : 'NOT';
ABSORBABLE : 'ABSORBABLE';
NET : 'NET';
DEDUCTIBLE : 'DEDUCTIBLE';
RISK : 'RISK';
ATTACHING : 'ATTACHING';
INFORCE : 'INFORCE';
POLICIES : 'POLICIES';
ISSUED : 'ISSUED';
OCCURRING : 'OCCURRING';
PAY : 'PAY';
XS : 'XS';
FRANCHISE : 'FRANCHISE';
REPLACEMENTCOST : 'REPLACEMENT COST' | 'RCV';
ACTUALCASHVALUE : 'ACTUAL CASHVALUE';
TOTALSUMINSURED : 'TOTAL SUM INSURED';
AFFECTED : 'AFFECTED';
CASHVALUE : 'CASH VALUE';
COVERED : 'COVERED';
AGGREGATE : 'AGGREGATE';
TYPE : 'TYPE';
HOUR : 'HOUR';
HOURS : 'HOURS';
DAY : 'DAY';
DAYS : 'DAYS';
WEEK : 'WEEK';
WEEKS : 'WEEKS';
MONTH : 'MONTH';
MONTHS : 'MONTHS';
YEAR : 'YEAR';
YEARS : 'YEARS';
IS : 'IS';
ARE : 'ARE';
ON : 'ON';
OF : 'OF';
PER : 'PER';
BY : 'BY';
TO : 'TO';
EACH : 'EACH';
FROM : 'FROM';
UNTIL : 'UNTIL';
COVERNAMES : 'COVERNAMES';
AED : 'AED';
AFA : 'AFA';
AFN : 'AFN';
ALL : 'ALL';
AMD : 'AMD';
ANG : 'ANG';
AOA : 'AOA';
AON : 'AON';
ARS : 'ARS';
ATS : 'ATS';
AUD : 'AUD';
AZM : 'AZM';
BAM : 'BAM';
BDT : 'BDT';
BEF : 'BEF';
BGN : 'BGN';
BHD : 'BHD';
BIF : 'BIF';
BMD : 'BMD';
BND : 'BND';
BOB : 'BOB';
BRL : 'BRL';
BTN : 'BTN';
BWP : 'BWP';
BYR : 'BYR';
BZD : 'BZD';
CAD : 'CAD';
CDF : 'CDF';
CHF : 'CHF';
CLF : 'CLF';
CLP : 'CLP';
CNY : 'CNY';
COP : 'COP';
CRC : 'CRC';
CVE : 'CVE';
CYP : 'CYP';
CZK : 'CZK';
DEM : 'DEM';
DJF : 'DJF';
DKK : 'DKK';
DZD : 'DZD';
EEK : 'EEK';
EGP : 'EGP';
ERN : 'ERN';
ETB : 'ETB';
EUR : 'EUR';
FJD : 'FJD';
FKP : 'FKP';
FRF : 'FRF';
GBP : 'GBP';
GEL : 'GEL';
GHC : 'GHC';
GIP : 'GIP';
GMD : 'GMD';
GNF : 'GNF';
GRD : 'GRD';
GTQ : 'GTQ';
GYD : 'GYD';
HKD : 'HKD';
HNL : 'HNL';
HRK : 'HRK';
HUF : 'HUF';
IDR : 'IDR';
IEP : 'IEP';
ILS : 'ILS';
INR : 'INR';
IQD : 'IQD';
IRR : 'IRR';
ISK : 'ISK';
ITL : 'ITL';
JMD : 'JMD';
JOD : 'JOD';
JPY : 'JPY';
KES : 'KES';
KGS : 'KGS';
KHR : 'KHR';
KMF : 'KMF';
KPW : 'KPW';
KRW : 'KRW';
KWD : 'KWD';
KZT : 'KZT';
LAK : 'LAK';
LBP : 'LBP';
LKR : 'LKR';
LRD : 'LRD';
LSL : 'LSL';
LTL : 'LTL';
LUF : 'LUF';
LVL : 'LVL';
LYD : 'LYD';
MAD : 'MAD';
MDL : 'MDL';
MGA : 'MGA';
MKD : 'MKD';
MMK : 'MMK';
MNT : 'MNT';
MRO : 'MRO';
MTL : 'MTL';
MUR : 'MUR';
MVR : 'MVR';
MWK : 'MWK';
MXN : 'MXN';
MXP : 'MXP';
MYR : 'MYR';
MZM : 'MZM';
NAD : 'NAD';
NGN : 'NGN';
NIO : 'NIO';
NLG : 'NLG';
NOK : 'NOK';
NPR : 'NPR';
NZD : 'NZD';
OMR : 'OMR';
PAB : 'PAB';
PEN : 'PEN';
PGK : 'PGK';
PHP : 'PHP';
PKR : 'PKR';
PLN : 'PLN';
PTE : 'PTE';
PYG : 'PYG';
QAR : 'QAR';
RON : 'RON';
RUB : 'RUB';
RWF : 'RWF';
SAR : 'SAR';
SBD : 'SBD';
SCR : 'SCR';
SDD : 'SDD';
SEK : 'SEK';
SGD : 'SGD';
SHP : 'SHP';
SIT : 'SIT';
SKK : 'SKK';
SLL : 'SLL';
SOS : 'SOS';
SRD : 'SRD';
STD : 'STD';
SVC : 'SVC';
SYP : 'SYP';
SZL : 'SZL';
THB : 'THB';
TJS : 'TJS';
TMM : 'TMM';
TND : 'TND';
TOP : 'TOP';
TRL : 'TRL';
TRY : 'TRY';
TWD : 'TWD';
TZS : 'TZS';
UAH : 'UAH';
UGX : 'UGX';
USD : 'USD';
UYU : 'UYU';
UZS : 'UZS';
VEB : 'VEB';
VEF : 'VEF';
VND : 'VND';
VUV : 'VUV';
WST : 'WST';
XAF : 'XAF';
XOF : 'XOF';
XPF : 'XPF';
YER : 'YER';
ZAR : 'ZAR';
ZMK : 'ZMK';
ZWD : 'ZWD';
AOR : 'AOR';
AWG : 'AWG';
AZN : 'AZN';
BBD : 'BBD';
BGL : 'BGL';
BOV : 'BOV';
BSD : 'BSD';
CHE : 'CHE';
CHW : 'CHW';
COU : 'COU';
CUP : 'CUP';
DOP : 'DOP';
GHS : 'GHS';
HTG : 'HTG';
KYD : 'KYD';
MGF : 'MGF';
MOP : 'MOP';
MXV : 'MXV';
MZN : 'MZN';
ROL : 'ROL';
RSD : 'RSD';
SDG : 'SDG';
SRG : 'SRG';
SSP : 'SSP';
TTD : 'TTD';
UD1 : 'UD1';
UD2 : 'UD2';
UD3 : 'UD3';
UD4 : 'UD4';
UD5 : 'UD5';
US : 'US';
USN : 'USN';
USS : 'USS';
UYI : 'UYI';
UYP : 'UYP';
XAG : 'XAG';
XAU : 'XAU';
XBA : 'XBA';
XBB : 'XBB';
XBC : 'XBC';
XBD : 'XBD';
XCD : 'XCD';
XDR : 'XDR';
XPD : 'XPD';
XPT : 'XPT';
XSU : 'XSU';
XTS : 'XTS';
XUA : 'XUA';
XUF : 'XUF';
XXX : 'XXX';
YUN : 'YUN';
ZWL : 'ZWL';
JAN : 'JAN'; 
FEB : 'FEB'; 
MAR : 'MAR';
APR : 'APR'; 
MAY : 'MAY'; 
JUN : 'JUN'; 
JUL : 'JUL'; 
AUG : 'AUG'; 
SEP : 'SEP'; 
OCT : 'OCT'; 
NOV : 'NOV';  
DEC : 'DEC';
FLOAT   : DIGIT* '.' DIGIT+;
INTEGER : DIGIT+;
COMMENT	: SLASH SLASH [^\n\r]* -> skip;
IDENT   : ALPHA (ALPHA | DIGIT | UNDERSCORE | DOT | MINUS)*;
PHRASE	: '{' (~[{])* '}';
WS : [ \t\r\n]+ -> channel(HIDDEN);
DIGIT : [0-9];
ALPHA : [a-zA-ZÀ-ÖØ-öø-ÿ];

