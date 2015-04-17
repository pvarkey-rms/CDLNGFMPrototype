id          [a-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u00FF][a-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u00FF0-9._-]*
digit       [0-9]
sign        [+-]

//float       {sign}?{digit}+"."{digit}+
float								{digit}+"."{digit}+
//int								{sign}?{digit}+
int									{digit}+
num									{float}|{int}

//phraselistitem	[{,]\s*([^,}]*)(?=\s*[,}])


%options flex case-insensitive
 
%%
\s+																	/* ignore */
//{float}															return 'FLOAT'
//{int}																return 'INTEGER'
({float}|{int})\s*(k|thousand|m|million|b|billion|trillion)?(?![a-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u00FF0-9._-])		return 'NUMBERWITHOPTIONALMULTIPLIER'

ABSORBABLE                              return 'ABSORBABLE'
"ACTUAL"\s+"CASH"\s+"VALUE"             return 'ACTUALCASHVALUE' 
ACV                                     return 'ACTUALCASHVALUE' 
AFFECTED                                return 'AFFECTED'
AGGREGATE                               return 'AGGREGATE'
ATTACHING                               return 'ATTACHING'
"ATTACHMENT"\s+"BASIS"                  return 'ATTACHMENTBASIS'
"CLAIMS"\s+"ADJUSTMENT"\s+"OPTIONS"     return 'CLAIMSADJUSTMENTOPTIONS'
//CLT										return 'CLT'
CONTRACT                                return 'CONTRACT'
COVERED                                 return 'COVERED'
COVERS                                  return 'COVERS'
"COVERNAMES"\s+"ARE"					return 'COVERNAMESARE'
CURRENCY                                return 'CURRENCY'
DAMAGE                                  return 'DAMAGE'
DECLARATIONS                            return 'DECLARATIONS'
DEDUCTIBLE                              return 'DEDUCTIBLE'
DEDUCTIBLES                             return 'DEDUCTIBLES'
"DEDUCTIBLES"\s+"ARE"					return 'DEDUCTIBLESARE'
EXCLUSIONS								return 'EXCLUSIONS'
EXPIRATION                              return 'EXPIRATION'
FOR                                     return 'FOR'
FRANCHISE                               return 'FRANCHISE'
//"GROUND"\s+"UP"\s+"SUBLIMIT"			return 'GROUNDUPSUBLIMIT'
"GROUND"\s+"UP"							return 'GROUNDUP'
HAZARD                                  return 'HAZARD'
INCEPTION                               return 'INCEPTION'
INSURANCE								return 'INSURANCE'
"IN"[-]?"FORCE"                         return 'INFORCE'
ISSUED                                  return 'ISSUED'
KNOWNCURRENCIES							return 'KNOWNCURRENCIES'
LOSS                                    return 'LOSS'
NAME                                    return 'NAME'
NET                                     return 'NET'
OCCURRENCES                              return 'OCCURRENCES'
OCCURRENCE                              return 'OCCURRENCE'
OCCURRING                               return 'OCCURRING'
OPTIONAL                                return 'OPTIONAL'
PARAMETERS                              return 'PARAMETERS'
PAY                                     return 'PAY'
"PER"\s+"RISK"                          return 'PERRISK'
"PER"\s+"OCCURRENCE"                    return 'PEROCCURRENCE'
POLICIES                                return 'POLICIES'
PRODUCT                                 return 'PRODUCT'
"REPLACEMENT"\s+"COST"                  return 'REPLACEMENTCOST'
RCV                                     return 'REPLACEMENTCOST'
REINSURANCE								return 'REINSURANCE'
REINSTATEMENTS							return 'REINSTATEMENTS'
REQUIRED                                return 'REQUIRED'
RISK                                    return 'RISK'
SECTION                                 return 'SECTION'
SECTIONS                                return 'SECTIONS'
SHARE                                   return 'SHARE'
"SINGLE"\s+"LARGEST"                    return 'SINGLELARGEST'
STANDARD                                 return 'STANDARD'
SUBJECT                                 return 'SUBJECT'
SUBLIMITS                               return 'SUBLIMITS'
"SUBLIMITS"\s+"ARE"						return 'SUBLIMITSARE'
"TOTAL"\s+"SUMS"\s+"INSURED"            return 'TOTALSUMINSURED'
TSI                                     return 'TOTALSUMINSURED'
TYPE                                    return 'TYPE'
USING                                   return 'USING'
XS                                      return 'XS'

AND                             return 'AND'
ARE                             return 'ARE'
"AS"\s+"PER"					return 'ASPER'
BY                              return 'BY'
EACH                            return 'EACH'
FROM                            return 'FROM'
IS                              return 'IS'
NOT								return 'NOT'
OF                              return 'OF'
ON                              return 'ON' 
"ONLY"\s+"ONCE"					return 'ONLYONCE'
PER                             return 'PER'
TO                              return 'TO'
UNTIL                           return 'UNTIL' 
WITH							return 'WITH'

MIN                             return 'MIN'
SUM                             return 'SUM'
MAXIMUM                         return 'MAXIMUM'
MAX                             return 'MAX'

AED								return 'AED'
AFA 							return 'AFA'
AFN 							return 'AFN'
ALL 							return 'ALL'
AMD 							return 'AMD'
ANG 							return 'ANG'
AOA 							return 'AOA'
AON 							return 'AON'
ARS 							return 'ARS'
ATS 							return 'ATS'
AUD 							return 'AUD'
AZM 							return 'AZM'
BAM 							return 'BAM'
BDT 							return 'BDT'
BEF 							return 'BEF'
BGN 							return 'BGN'
BHD 							return 'BHD'
BIF 							return 'BIF'
BMD 							return 'BMD'
BND 							return 'BND'
BOB 							return 'BOB'
BRL 							return 'BRL'
BTN 							return 'BTN'
BWP 							return 'BWP'
BYR 							return 'BYR'
BZD 							return 'BZD'
CAD 							return 'CAD'
CDF 							return 'CDF'
CHF 							return 'CHF'
CLF 							return 'CLF'
CLP 							return 'CLP'
CNY 							return 'CNY'
COP 							return 'COP'
CRC 							return 'CRC'
CVE 							return 'CVE'
CYP 							return 'CYP'
CZK 							return 'CZK'
DEM 							return 'DEM'
DJF 							return 'DJF'
DKK 							return 'DKK'
DZD 							return 'DZD'
EEK 							return 'EEK'
EGP 							return 'EGP'
ERN 							return 'ERN'
ETB 							return 'ETB'
EUR 							return 'EUR'
FJD 							return 'FJD'
FKP 							return 'FKP'
FRF 							return 'FRF'
GBP 							return 'GBP'
GEL 							return 'GEL'
GHC 							return 'GHC'
GIP 							return 'GIP'
GMD 							return 'GMD'
GNF 							return 'GNF'
GRD 							return 'GRD'
GTQ 							return 'GTQ'
GYD 							return 'GYD'
HKD 							return 'HKD'
HNL 							return 'HNL'
HRK 							return 'HRK'
HUF 							return 'HUF'
IDR 							return 'IDR'
IEP 							return 'IEP'
ILS 							return 'ILS'
INR 							return 'INR'
IQD 							return 'IQD'
IRR 							return 'IRR'
ISK 							return 'ISK'
ITL 							return 'ITL'
JMD 							return 'JMD'
JOD 							return 'JOD'
JPY 							return 'JPY'
KES 							return 'KES'
KGS 							return 'KGS'
KHR 							return 'KHR'
KMF 							return 'KMF'
KPW 							return 'KPW'
KRW 							return 'KRW'
KWD 							return 'KWD'
KZT 							return 'KZT'
LAK 							return 'LAK'
LBP 							return 'LBP'
LKR 							return 'LKR'
LRD 							return 'LRD'
LSL 							return 'LSL'
LTL 							return 'LTL'
LUF 							return 'LUF'
LVL 							return 'LVL'
LYD 							return 'LYD'
MAD 							return 'MAD'
MDL 							return 'MDL'
MGA 							return 'MGA'
MKD 							return 'MKD'
MMK 							return 'MMK'
MNT 							return 'MNT'
MRO 							return 'MRO'
MTL 							return 'MTL'
MUR 							return 'MUR'
MVR 							return 'MVR'
MWK 							return 'MWK'
MXN 							return 'MXN'
MXP 							return 'MXP'
MYR 							return 'MYR'
MZM 							return 'MZM'
NAD 							return 'NAD'
NGN 							return 'NGN'
NIO 							return 'NIO'
NLG 							return 'NLG'
NOK 							return 'NOK'
NPR 							return 'NPR'
NZD 							return 'NZD'
OMR 							return 'OMR'
PAB 							return 'PAB'
PEN 							return 'PEN'
PGK 							return 'PGK'
PHP 							return 'PHP'
PKR 							return 'PKR'
PLN 							return 'PLN'
PTE 							return 'PTE'
PYG 							return 'PYG'
QAR 							return 'QAR'
RON 							return 'RON'
RUB 							return 'RUB'
RWF 							return 'RWF'
SAR 							return 'SAR'
SBD 							return 'SBD'
SCR 							return 'SCR'
SDD 							return 'SDD'
SEK 							return 'SEK'
SGD 							return 'SGD'
SHP 							return 'SHP'
SIT 							return 'SIT'
SKK 							return 'SKK'
SLL 							return 'SLL'
SOS 							return 'SOS'
SRD 							return 'SRD'
STD 							return 'STD'
SVC 							return 'SVC'
SYP 							return 'SYP'
SZL 							return 'SZL'
THB 							return 'THB'
TJS 							return 'TJS'
TMM 							return 'TMM'
TND 							return 'TND'
TOP 							return 'TOP'
TRL 							return 'TRL'
TRY 							return 'TRY'
TWD 							return 'TWD'
TZS 							return 'TZS'
UAH 							return 'UAH'
UGX 							return 'UGX'
USD 							return 'USD'
UYU 							return 'UYU'
UZS 							return 'UZS'
VEB 							return 'VEB'
VEF 							return 'VEF'
VND 							return 'VND'
VUV 							return 'VUV'
WST 							return 'WST'
XAF 							return 'XAF'
XOF 							return 'XOF'
XPF 							return 'XPF'
YER 							return 'YER'
ZAR 							return 'ZAR'
ZMK 							return 'ZMK'
ZWD 							return 'ZWD'
AOR 							return 'AOR'
AWG 							return 'AWG'
AZN 							return 'AZN'
BBD 							return 'BBD'
BGL 							return 'BGL'
BOV 							return 'BOV'
BSD 							return 'BSD'
CHE 							return 'CHE'
CHW 							return 'CHW'
COU 							return 'COU'
CUP 							return 'CUP'
DOP 							return 'DOP'
GHS 							return 'GHS'
HTG 							return 'HTG'
KYD 							return 'KYD'
MGF 							return 'MGF'
MOP 							return 'MOP'
MXV 							return 'MXV'
MZN 							return 'MZN'
ROL 							return 'ROL'
RSD 							return 'RSD'
SDG 							return 'SDG'
SRG 							return 'SRG'
SSP 							return 'SSP'
TTD 							return 'TTD'
UD1 							return 'UD1'
UD2 							return 'UD2'
UD3 							return 'UD3'
UD4 							return 'UD4'
UD5 							return 'UD5'
US								return 'US'
USN 							return 'USN'
USS 							return 'USS'
UYI 							return 'UYI'
UYP 							return 'UYP'
XAG 							return 'XAG'
XAU 							return 'XAU'
XBA 							return 'XBA'
XBB 							return 'XBB'
XBC 							return 'XBC'
XBD 							return 'XBD'
XCD 							return 'XCD'
XDR 							return 'XDR'
XPD 							return 'XPD'
XPT 							return 'XPT'
XSU 							return 'XSU'
XTS 							return 'XTS'
XUA 							return 'XUA'
XUF 							return 'XUF'
XXX 							return 'XXX'
YUN 							return 'YUN'
ZWL 							return 'ZWL'

HOURS                           return 'HOUR'
HOUR                            return 'HOUR'
DAYS							return 'DAY'
DAY                             return 'DAY'
WEEKS                           return 'WEEK'
WEEK                            return 'WEEK'
MONTHS                          return 'MONTH'
MONTH                           return 'MONTH'
YEARS                           return 'YEAR'
YEAR                            return 'YEAR'

JAN                             return 'JAN'
FEB                             return 'FEB'
MAR                             return 'MAR'
APR                             return 'APR'
MAY                             return 'MAY'
JUN                             return 'JUN'
JUL                             return 'JUL'
AUG                             return 'AUG'
SEP                             return 'SEP'
OCT                             return 'OCT'
NOV                             return 'NOV'
DEC                             return 'DEC'

//\s*(k|thousand|m|million|b|billion)\s*[^a-z0-9_(){}%,;>+-/*]	return 'MULTIPLIER'
//\s*(k|thousand|m|million|b|billion)(?=\+|\-|\*|\/|\s|\,|\)|\()					return 'MULTIPLIER'
//KnownCurrencies\s+are\s+\{(\s*{phraselistitem}\s*[,}])*		return 'KNOWNCURRENCY'


//(?!CLT[(]){id}+[(]			return 'FUNCBEGIN'
CLT\s*[(]							return 'CLTBEGIN'
{id}[(]							return 'FUNCBEGIN'
//((?!CLT){id}+)[(]	OR (((?!CLT)[a-zA-Z]+)[(]) OR (((?!(CLT[(]))[a-zA-Z]+)[(])			return 'FUNCBEGIN'
//{id}[:]							return 'LABEL'

{id}                            return 'IDENT'

"//".*                          /* ignore */
"{"[^}]*"}"					return 'PHRASE'
//[{,]\s*([^,}]*)(?=\s*[,}])		return 'PHRASELISTITEM'
","                             return 'COMMA'
"%"                             return 'PERCENT'
"+"                             return 'PLUS'
"-"                             return 'MINUS'
"*"                             return 'TIMES'
"/"                             return 'SLASH'
">"                             return 'GREATER'
"."                             return 'DOT'
"."{3,}                         return 'ELLIPSIS'
"{"                             return 'LBRACE'
"}"                             return 'RBRACE'
"("                             return 'LPAREN'
")"                             return 'RPAREN'
"["                             return 'LSQUARE'
"]"                             return 'RSQUARE'
":"                             return 'COLON'
";"                             return 'SEMICOLON'
