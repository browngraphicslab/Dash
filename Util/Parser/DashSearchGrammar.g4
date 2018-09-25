grammar DashSearchGrammar;

// EXAMPLE logical_expr: ((a b) | ("cat dog" | (dog cat)) | inside(collection1, collection2, collection3))

// DOUBLE CHECK! Is grun testing the proper rule?? If you've updated the rule, have you opened a new command prompt??

/*
 * PARSER RULES
 */

	// FUNCTIONS

	argument 				: ALPHANUM 
							| STRING
							;

	arguments				: 
							| ((argument ',' WHITESPACE?)+)? argument 
							;

	function_expr			: ALPHANUM '(' arguments ')' 
							;

	// LOGICAL OPERATORS

	and_token				: WHITESPACE 
							;

	or_token				: WHITESPACE? '|' WHITESPACE? 
							;

	operator 				: and_token 
							| or_token 
							; 

	phrase					: ALPHANUM
							| STRING
							;

/*
	chain					: '!' phrase
							| (phrase operator)+ phrase 
							;

	logical_expr			: chain													// base case
							| logical_expr operator logical_expr					// intermediate recursion
							| '(' logical_expr ')'									// accounts for grouping
							;
*/									

	// KEY VALUE

	kv_search				: phrase ':' phrase
							;

	search_term				: function_expr
							| '(' query ')'
							| phrase
							| kv_search
							;

	not_search_term			: '!'? search_term
							;

	query					: (not_search_term operator)* not_search_term
							;

/*
 * LEXER RULES
 */

	// TEXT, DIGITS AND WHITESPACE

	fragment LOWERCASE		: [a-z] 
							;

	fragment UPPERCASE 		: [A-Z] 
							;

	fragment ESCAPED_QUOTE : '\\"';

	NEWLINE					: [\n\r]+ -> skip 
							;

	ALPHANUM		 		:  (LOWERCASE | UPPERCASE | '_' | [0-9])+
							;

	STRING					: '"' ( ESCAPED_QUOTE | ~('\n'|'\r') )*? '"'
							;

	WHITESPACE 				: [ \t]+ 
							;
