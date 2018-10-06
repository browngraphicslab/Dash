grammar SearchGrammar;

/*
 * Parser Rules
 */

 	value					: WORD
 							| STRING
 							;

 	keylist					: value
 							| '{' WS* (value WS* ',' WS*)* value WS* '}'
 							;

 	kvsearch				: '-'? keylist ':' value
 							;

 	args					: (value WS* ',' WS*)* value?
 							;

 	function				: WORD '(' WS* args WS* ')'
 							;

 	term					: kvsearch
 							| value
 							| function
 							| '(' WS* query WS* ')'
 							;

 	negation				: term
 							| '!' term
 							;

	or						: (negation WS* '|' WS*)* negation
 							;

 	and						: (or WS)* or
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