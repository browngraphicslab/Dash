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

            // STRING MANIPULATION

            remove_first_exp,
            remove_exp,

            substring,
            index,

            // BINARY COMPARISONS

            operator_equal, //needed?
            equal,

            operator_not_equal, //needed?
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

            // DOCUMENTS

            data_document,
            data_doc,

                // collections
                coll,
                coll_title,
                rtf_title,
                inside,

                // images
                image,

                // lists
                zip,

            // COMMANDS

                // search
                f,
                fs,
                find,
                find_s,
                find_single,

                search,
                union_search,
                intersect_search,
                negation_search,

                intersect_by_value,
                process_search_results,
                parse_search_string,

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

            // MISC & MUTATORS

            set_field,
            set_list_field,
            var_assign,

            // NULL EQUIVALENT

            invalid,

            // HELP

            help,
            print
        }

        public static Name Parse(string toParse) => Enum.TryParse<Name>(toParse, out var interpretedName) ? interpretedName : Name.invalid;

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
            [Name.remove_first_exp] =
                "Given a target string and a list of string phrases,\n" +
                "removes the first occurence of each phrase in the list if present\n            " +
                "Invoked by its function call in the REPL, formatted as remove_first_exp(\"A\", [\"B\", \"C\", ... , \"n\"]).\n            " +
                "EXAMPLES:\n            remove_first_exp(\"foo one bar one foo two bar two\", [\"foo\", \"bar\"]) = \" one  one foo two bar two\"" +
                "\n            remove_first_exp(\"foo bar foo bar\", [\"fo\", \"b\"]) = \"o ar foo bar\"",
            [Name.remove_exp] =
                "Given a target string and a list of string phrases, removes the all occurences of each phrase in the list if present\n            " +
                "Invoked by its function call in the REPL, formatted as remove_exp(\"A\", [\"B\", \"C\", ... , \"n\"]).\n      " +
                "EXAMPLES:\n            remove_exp(\"foo one bar one foo two bar two\", [\"foo\", \"bar\"]) = \" one  one  two  two\"" +
                "\n            remove_exp(\"foo bar foo bar\", [\"o\", \"ar\"]) = \"f b f b\"",
            [Name.substring] =
                "Binary - given two numbers, computes the remainder of divident A and divisor B.\n            " +
                "Invoked by its function call in the REPL, formatted as mod(A, B).\n      " +
                "EXAMPLES:\n            mod(35, 7) = 0\n            mod(18, 5) = 3",
            [Name.index] =
                "Binary - given two numbers, computes the remainder of divident A and divisor B.\n            " +
                "Invoked by its function call in the REPL, formatted as mod(A, B).\n      " +
                "EXAMPLES:\n            mod(35, 7) = 0\n            mod(18, 5) = 3",
        };
    }
}