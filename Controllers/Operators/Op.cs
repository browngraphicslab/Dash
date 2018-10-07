using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]

    public class Op
    {
        /*
         * TODO: ENUMS ARE DIRECTLY CONVERTED TO STRINGS FOR INTERPRETATION! If you want to rename, use resharper by hitting ctrl r r to update name globally
         */

        public enum Name
        {
            //TODO: MAKE YOUR OWN CATEGORIES PLS

            // ARITHMETIC

            operator_add,
            add,
            concat,

            operator_subtract,
            subtract,
            minus,

            operator_multiply,
            multiply,
            mult,

            operator_divide,
            div,
            divide,

            operator_modulo,
            modulo,
            mod,

            operator_exponential,
            exp,

            // STRING MANIPULATION

            remove_first_exp,
            remove_exp,
            replace,

            substring,
            index,
            to_lower,

            // BINARY COMPARISONS

            operator_equal,
            equal,

            operator_not_equal,
            not_equal,

            operator_negate,
            negate,

            operator_greater_than,
            greater_than,

            operator_greater_than_equals,
            greater_than_equals,

            operator_less_than,
            less_than,

            operator_less_than_equals,
            less_than_equals,

            // LOOPS & CONDITIONALS

            for_in_lp,
            do_while_lp,
            while_lp,
            for_lp,
            if_st,
            let,

            // CONVERTERS

            to_list,
            to_string,
            parse_num,
            contains,

            // DOCUMENTS

            data_document,
            data_doc,
            link,
            link_des_text,

            // collections
            coll,
            coll_title,
            rtf_title,
            inside,

            // images
            image,

            // lists
            zip,
            count,
            len,

            // templates
            apply,
            set_template,
            templates,

            // PDFs
            references,
            regex,
            text,

            // COMMANDS

            // search
            f,
            fs,
            find,
            find_s,
            find_single,

            search,
            intersect_by_value,

            // misc
            map,

            // EXECUTE

            exec,
            exec_to_string,

            // MISC & ACCESSORS

            element_access,
            keys,
            get_keys,
            get_field,

            before,
            after,

            id_to_document,
            rich_document_text,
            alias,
            key_field_query,
            kv,

            // MISC & MUTATORS

            set_field,
            set_list_field,
            var_assign,
            function,
            function_call,

            // Point functions
            point,
            x, y,

            dref,
            pref,

            k,

            date,

            api,

            get_doc,

            // NULL EQUIVALENT

            invalid,

            // HELP

            help,
            print,
        }

        public static Name Parse(string toParse) => Enum.TryParse<Name>(toParse, out var interpretedName) ? interpretedName : Name.invalid;

        public static bool TryParse(string toParse, out Name result) => Enum.TryParse(toParse, out result);

        public static Dictionary<Name, string> FuncDescriptions = new Dictionary<Name, string>()
        {
            [Name.operator_add] =
                "Given an arbitrary number of expressions, computes the sum or concatenates depending on input type.\n            " +
                "Invoked by the + token in the REPL, formatted as A + B + C + ... + n.\n      " +
                "EXAMPLES:\n            \"hello\" + \" \" + \"world\" = \"hello world\"\n            15 + 3 + 2.3 = 20.3",
            [Name.add] =
                "Binary - given two numbers, computes the sum. Overloaded with concatenation for string inputs.\n            " +
                "Invoked by its function call in the REPL, formatted as add(A, B).\n      " +
                "EXAMPLES:\n            add(\"hello\", \" world\")\n            add(15, 3) = 18 = \"hello world\"",
            [Name.concat] =
                "Binary - given two input springs, concatenates or joins inputs and returns the result.\n            " +
                "Invoked by its function call in the REPL, formatted as concat(A, B).\n      " +
                "EXAMPLES:\n            concat(\"string \", \"concatenation\") = \"string concatenation\"\n            concat(\"unchanged\", \"\") = \"unchanged\"",
            [Name.operator_subtract] =
                "Given numerical inputs, computes the difference. Given a target string and a list of string phrases,\n            " +
                "removes the first occurence of each phrase in the list if present\n            " +
                "Invoked by the - token in the REPL, formatted as A - B or \"A\" - [\"B\", \"C\", ... , \"n\"].\n      " +
                "EXAMPLES:\n            36 - 12 = 24\n            \"foo one bar one foo two bar two\" - [\"foo\", \"bar\"] = \" one  one foo two bar two\"",
            [Name.subtract] =
                "Binary - given two numbers, computes the difference.\n            " +
                "Invoked by its function call in the REPL, formatted as subtract(A, B).\n      " +
                "EXAMPLES:\n            subtract(5, 10) = -5\n            subtract(13.9, 0.6) = 13.3",
            [Name.minus] =
                "Binary - given two numbers, computes the difference.\n            " +
                "Invoked by its function call in the REPL, formatted as minus(A, B).\n      " +
                "EXAMPLES:\n            minus(16, 7) = 9\n            minus(13.9, 0.6) = 13.3",
            [Name.operator_multiply] =
                "Given numerical inputs, computes the product. Given a target string A and a numerical input B,\n" +
                "synthesizes a string consisting of the A repeating B times\n            " +
                "Invoked by the * token in the REPL, formatted as A * B or \"A\" * B.\n      " +
                "EXAMPLES:\n            7 * 8 = 56\n            \"repeat\" * 3 = \"repeatrepeatrepeat\"",
            [Name.multiply] =
                "Binary - given two numbers, computes the product.\n            " +
                "Invoked by its function call in the REPL, formatted as multiply(A, B).\n      " +
                "EXAMPLES:\n            multiply(7, 11) = 77\n            2.3 * 3 = 6.9",
            [Name.mult] =
                "Binary - given two numbers, computes the product.\n            " +
                "Invoked by its function call in the REPL, formatted as multiply(A, B).\n      " +
                "EXAMPLES:\n            mult(7, 11) = 77\n            2.3 * 3 = 6.9",
            [Name.operator_divide] =
                "Given numerical inputs, computes the quotient of dividend A and divisor B. Given a target string and a list of string phrases,\n" +
                "removes all occurences of each phrase in the list if present.\n            " +
                "Invoked by the / token in the REPL, formatted as A / B or \"A\" / [\"B\", \"C\", ... , \"n\"].\n      " +
                "EXAMPLES:\n            36 / 12 = 3\n            \"foo one bar one foo two bar two\" / [\"foo\", \"bar\"] = \" one  one  two  two\"",

            [Name.div] =
                "Binary - given two numbers, computes the quotient of divident A and divisor B.\n            " +
                "Invoked by its function call in the REPL, formatted as div(A, B).\n      " +
                "EXAMPLES:\n            div(35, 7) = 5\n            div(4.6, 2) = 2.3",
            [Name.divide] =
                "Binary - given two numbers, computes the quotient of divident A and divisor B.\n            " +
                "Invoked by its function call in the REPL, formatted as divide(A, B).\n      " +
                "EXAMPLES:\n            divide(35, 7) = 5\n            divide(4.6, 2) = 2.3",
            [Name.operator_modulo] =
                "Given numerical inputs, computes the remainder given by the dividend A and divisor B.\n            " +
                "Invoked by the % token in the REPL, formatted as A % B.\n      " +
                "EXAMPLES:\n            10 % 5 = 0\n            11 % 6 = 5",
            [Name.modulo] =
                "Binary - given two numbers, computes the remainder of divident A and divisor B.\n            " +
                "Invoked by its function call in the REPL, formatted as modulo(A, B).\n      " +
                "EXAMPLES:\n            modulo(35, 7) = 0\n            modulo(18, 5) = 3",
            [Name.mod] =
                "Binary - given two numbers, computes the remainder of divident A and divisor B.\n            " +
                "Invoked by its function call in the REPL, formatted as mod(A, B).\n      " +
                "EXAMPLES:\n            mod(35, 7) = 0\n            mod(18, 5) = 3",
            [Name.operator_exponential] =
                "Binary - given two numbers, computes the result given by raising A to the Bth power.\n            " +
                "Invoked by the ^ token in the REPL, formatted as A ^ B.\n      " +
                "EXAMPLES:\n            3 ^ 3 = 27\n            6 ^ 6.3 = 79,864.3",
            [Name.exp] =
                "Binary - given two numbers, computes the result given by raising A to the Bth power.\n            " +
                "Invoked by the ^ token in the REPL, formatted as A ^ B.\n      " +
                "EXAMPLES:\n            3 ^ 3 = 27\n            6 ^ 6.3 = 79,864.3",
            [Name.remove_first_exp] =
                "Given a target string and a list of string phrases,\n            " +
                "removes the first occurence of each phrase in the list if present.\n            " +
                "Invoked by its function call in the REPL, formatted as remove_first_exp(\"A\", [\"B\", \"C\", ... , \"n\"]).\n      " +
                "EXAMPLES:\n            remove_first_exp(\"foo one bar one foo two bar two\", [\"foo\", \"bar\"]) = \" one  one foo two bar two\"" +
                "\n            remove_first_exp(\"foo bar foo bar\", [\"fo\", \"b\"]) = \"o ar foo bar\"",
            [Name.remove_exp] =
                "Given a target string and a list of string phrases,\n            " +
                "removes the all occurences of each phrase in the list if present.\n            " +
                "Invoked by its function call in the REPL, formatted as remove_exp(\"A\", [\"B\", \"C\", ... , \"n\"]).\n      " +
                "EXAMPLES:\n            remove_exp(\"foo one bar one foo two bar two\", [\"foo\", \"bar\"]) = \" one  one  two  two\"" +
                "\n            remove_exp(\"foo bar foo bar\", [\"o\", \"ar\"]) = \"f b f b\"",
            [Name.substring] =
                "Computes and returns the portion of the given string that resides between the given starting index and its end.\n" +
                "            You can optionally specify the length of the computed substring by passing in a third (integer) argument.\n            " +
                "Invoked by its function call in the REPL, formatted as substring(\"A\", startindex) or substring(\"A\", startindex, length).\n      " +
                "EXAMPLES:\n            substring(\"foo bar foo bar\", 2) = \"o bar foo bar\"\n            substring(\"foo bar foo bar\", 4, 3) = \"bar\"",

            [Name.index] =
                "Given a list or a string, returns the element occurring at the specified zero-based index.\n            " +
                "Invoked by bracket notation or its function call in the REPL, formatted as A[index] and index(A, index), respectively.\n      " +
                "EXAMPLES:\n            [a, b, c, d, e][3] = d\n            \"foo bar\"[5] = \"a\"",
            [Name.operator_equal] =
                "Binary, comparative - given two objects, returns true if the two are equal and false if not.\n            " +
                "Invoked by the == token in the REPL, formatted as A == B.\n      " +
                "EXAMPLES:\n            5 == 5 = true\n            \"cat\" == \"dog\" = false",
            [Name.equal] =
                "Binary, comparative - given two objects, returns true if the two are equal and false if not.\n            " +
                "Invoked by its function call in the REPL, formatted as equals(A, B).\n      " +
                "EXAMPLES:\n            equals(5, 5) = true\n            equals(\"cat\", \"dog\") = false",
            [Name.operator_not_equal] =
                "Binary, comparative - given two objects, returns false if the two are equal and true if not.\n            " +
                "Invoked by the != token in the REPL, formatted as A != B.\n      " +
                "EXAMPLES:\n            5 != 5 = false\n            \"cat\" != \"dog\" = true",
            [Name.not_equal] =
                "Binary, comparative - given two objects, returns false if the two are equal and true if not.\n            " +
                "Invoked by its function call in the REPL, formatted as not_equal(A, B).\n      " +
                "EXAMPLES:\n            not_equals(5, 5) = false\n            not_equals(\"cat\", \"dog\") = true",
            [Name.operator_negate] =
                "Unary - negates, or inverts the sign, of its sole numerical input\n            " +
                "Invoked by preceeding any number in the REPL, formatted as -A.\n      " +
                "EXAMPLES:\n            -5 = -5 \n            var a = -8; -a = 8",
            [Name.negate] =
                "Unary - negates, or inverts the sign, of its sole numerical input\n            " +
                "Invoked by its functio call in the REPL, formatted as negate(A).\n      " +
                "EXAMPLES:\n            negate(5) = -5 \n            var a = -8; negate(a) = 8",
            [Name.operator_greater_than] =
                "Binary, comparative - given two numbers, returns true if A is greater than B and returns false if not.\n            " +
                "Invoked by the > in the REPL, formatted as A > B.\n      " +
                "EXAMPLES:\n            2 > 0 = true\n            3 > 3 = false",
            [Name.greater_than] =
                "Binary, comparative - given two numbers, returns true if A is greater than B and returns false if not.\n            " +
                "Invoked by its function call in the REPL, formatted as greater_than(A, B).\n      " +
                "EXAMPLES:\n            greater_than(2, 0) = true\n            greater_than(3, 3) = false",
            [Name.operator_greater_than_equals] =
                "Binary, comparative - given two numbers, returns true if A is greater than or equivalent to B and returns false if not.\n            " +
                "Invoked by the >= in the REPL, formatted as A >= B.\n      " +
                "EXAMPLES:\n            2^3 >= 3^2 = false\n            3 >= 3 = true",
            [Name.greater_than_equals] =
                "Binary, comparative - given two numbers, returns true if A is greater than or equivalent to B and returns false if not.\n            " +
                "Invoked by its function call in the REPL, formatted as greater_than_equals(A, B).\n      " +
                "EXAMPLES:\n            greater_than_equals(2^3, 3^2) = false\n            greater_than_equals(3, 3) = true",
            [Name.operator_less_than] =
                "Binary, comparative - given two numbers, returns true if A is less than B and returns false if not.\n            " +
                "Invoked by the < in the REPL, formatted as A < B.\n      " +
                "EXAMPLES:\n            2 < 0 = false\n            18 < 6 * 3 = false",
            [Name.less_than] =
                "Binary, comparative - given two numbers, returns true if A is less than B and returns false if not.\n            " +
                "Invoked by its function call in the REPL, formatted as less_than(A, B).\n      " +
                "EXAMPLES:\n            less_than(2, 0) = false\n            less_than(3, 3) = false",
            [Name.operator_less_than_equals] =
                "Binary, comparative - given two numbers, returns true if A is less than or equivalent to B and returns false if not.\n            " +
                "Invoked by the <= in the REPL, formatted as A >= B.\n      " +
                "EXAMPLES:\n            2^3 <= 3^2 = true\n            3 <= 3 = true",
            [Name.less_than_equals] =
                "Binary, comparative - given two numbers, returns true if A is less than than or equivalent to B and returns false if not.\n            " +
                "Invoked by its function call in the REPL, formatted as less_than_equals(A, B).\n      " +
                "EXAMPLES:\n            less_than_equals(2^3, 3^2) = true\n            less_than_equals(3, 3) = true",
            [Name.for_in_lp] =
                "A specialty, indexless for-loop, a 'for in' loop sequentially iterates through a list allowing repetitive operations.\n            " +
                "Invoked by its function call in the REPL, formatted as for (var <arbitrary_name> in [1, 2, 3, 4, 5]) { <perform_task> }.\n      " +
                "EXAMPLE:\n            var scores = [35, 49, 18, 72]; for (var sc in scores) { sc *= 2 } return scores = [70, 98, 36, 144]",
            [Name.do_while_lp] = "",
            [Name.while_lp] = "",
            [Name.for_lp] = "",
            [Name.if_st] = "",
            [Name.let] = "",
            [Name.to_list] = "",
            [Name.to_string] = "",
            [Name.data_document] = "",
            [Name.data_doc] = "",
            [Name.coll] = "",
            [Name.coll_title] = "",
            [Name.rtf_title] = "",
            [Name.inside] = "",
            [Name.image] = "",
            [Name.zip] = "",
            [Name.f] = "",
            [Name.fs] = "",
            [Name.find] = "",
            [Name.find_s] = "",
            [Name.find_single] = "",
            [Name.search] = "",
            [Name.intersect_by_value] = "",
            [Name.map] = "",
            [Name.exec] = "",
            [Name.exec_to_string] = "",
            [Name.element_access] = "",
            [Name.keys] = "",
            [Name.get_keys] = "",
            [Name.get_field] = "",
            [Name.before] = "",
            [Name.after] = "",
            [Name.id_to_document] = "",
            [Name.rich_document_text] = "",
            [Name.alias] = "",
            [Name.key_field_query] = "",
            [Name.set_field] = "",
            [Name.set_list_field] = "",
            [Name.var_assign] = "",
            [Name.function] = "",
            [Name.function_call] = "",
            [Name.invalid] = "",
            [Name.help] = "",
            [Name.print] = "",
        };
    }
}
