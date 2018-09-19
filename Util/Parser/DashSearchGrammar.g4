grammar DashSearchGrammar;

/*
 * PARSER RULES
 */

	// LOGICAL OPERATORS

	and_token				: WHITESPACE 
							;

	or_token				: WHITESPACE? '|' WHITESPACE? 
							;

	operator 				: and_token | or_token ; 

 	term					: ALPHANUM
							| '!' ALPHANUM 
							;

	phrase					: term
							| '"' (term WHITESPACE)+ term? '"' 
							;

	chain					: phrase
							| (phrase operator)+ phrase 
							;

	logical_expr			: chain													// base case
							| logical_expr operator logical_expr		// intermediate recursion
							| '(' logical_expr ')'									// accounts for grouping
							;									

	// FUNCTIONS

	name					: WORD 
							;

	arguments				: (ALPHANUM ',')+ ALPHANUM 
							;

	input					:
							| arguments 
							;

	function_expr			: name '(' input ')' 
							;

	// KEY VALUE

	kv_search				: ALPHANUM ':' ALPHANUM 
							;

/*
 * LEXER RULES
 */

	// TEXT, DIGITS AND WHITESPACE

	fragment LOWERCASE		: [a-z] 
							;

	fragment UPPERCASE 		: [A-Z] 
							;

	fragment WORD			: (LOWERCASE | UPPERCASE | '_')+ 
							;

	fragment NUMBER			: [0-9]+ 
							;

	fragment ALPHANUM 		: WORD | NUMBER 
							;

	fragment WHITESPACE 	: '\t' 
							| ' ' 
							;
