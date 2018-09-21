cd "C:\Users\GFX Lab\Desktop\Hannah\Dash\Util\Parser"
DOSKEY update_grammar = java org.antlr.v4.Tool -visitor -Dlanguage=CSharp DashSearchGrammar.g4 && java org.antlr.v4.Tool -visitor DashSearchGrammar.g4 && javac *.java
DOSKEY grun = java org.antlr.v4.gui.TestRig DashSearchGrammar logical_expr -tokens -gui $*
cls