cd "C:\Users\GFX Lab\Desktop\Hannah\Dash\Util\Parser"
java org.antlr.v4.Tool -visitor -Dlanguage=CSharp DashSearchGrammar.g4
java org.antlr.v4.Tool -visitor DashSearchGrammar.g4
del *class
javac *.java