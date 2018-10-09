grammar DashSearchGrammar2;

/*
 * Parser Rules
 */

 	value					: WORD
 							| STRING
 							;

 	keylist					: value
 							| '{' WS* (value WS* ',' WS*)* value WS* '}'
 							;

 	valuesearch				: value
 							;

 	kvsearch				: '-'? keylist ':' value
 							;

 	args					: (value WS* ',' WS*)* value?
 							;

 	function				: WORD '(' WS* args WS* ')'
 							;

 	term					: kvsearch
 							| valuesearch
 							| function
 							| '(' WS* query WS* ')'
 							;

 	negation				: term
 							| '!' term
 							;

	or						: negation
							| or WS* '|' WS* negation
 							;

 	and						: or
 							| and WS or
 							;

 	query					: and
 							;

/*
 * Lexer Rules
 */

	fragment ESCAPED_QUOTE 	: '\\"'
							;

	NEWLINE					: [\n\r]+ -> skip
							;

	WORD					: [a-zA-Z0-9_]+
							;

	STRING					: '"' ( ESCAPED_QUOTE | ~('\n'|'\r') )*? '"'
							;

	WS						: [ ]+
							;