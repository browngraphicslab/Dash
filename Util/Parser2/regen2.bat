cd "C:\Users\GFX Lab\Desktop\Hannah\Dash\Util\Parser2"
java org.antlr.v4.Tool -visitor -Dlanguage=CSharp SearchGrammar.g4
java org.antlr.v4.Tool -visitor SearchGrammar.g4
del *class
javac *.java