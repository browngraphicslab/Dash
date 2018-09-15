grammar Search

/*
 * PARSER RULES
 */

 	LOGICAL_EXPRESSION		: (WORD (AND_TOKEN | OR_TOKEN))+ WORD 

/*
 * LEXER RULES
 */

// TEXT, DIGITS AND WHITESPACE

fragment LOWERCASE		: [a-z] ;

fragment UPPERCASE 		: [A-Z] ;

fragment WORD			: (LOWERCASE | UPPERCASE | '_')+ ;

fragment DIGIT			: [0-9] ;

fragment WHITESPACE 	: '\t' 
						| " " ;


// LOGICAL OPERATORS

fragment A				: 'A' 
						| 'a' ;

fragment N				: 'N'
						: 'n' ;

fragment D				: 'D'
						: 'd' ;

fragment O				: 'O' 
						| 'o' ;

fragment R				: 'R'
						: 'r' ;

fragment N				: 'N' 
						| 'n' ;

fragment T				: 'T'
						: 't' ;

AND 					: A N D ;

OR 						: O R ;

NOT 					: N O T ;

AND_TOKEN				: (WHITESPACE+)? '&' (WHITESPACE+)?
						| (WHITESPACE+)? AND (WHITESPACE+)? ;

OR_TOKEN				: (WHITESPACE+)? '|' (WHITESPACE+)?
						| (WHITESPACE+)? OR (WHITESPACE+)?;

NOT_TOKEN				: (WHITESPACE+)? '!'
						| (WHITESPACE+)? NOT (WHITESPACE+)? ;
