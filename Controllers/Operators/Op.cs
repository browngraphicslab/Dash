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
            ambiguous_add_test,

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

            char_search,
            operator_char_search,

            remove_first_char,
            remove_char,

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

            _for_in,
            _do_while,
            _while,
            _for,
            _if,
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
        }

        public static Name Parse(string toParse) => Enum.TryParse<Name>(toParse, out var interpretedName) ? interpretedName : Name.invalid;

        public static Dictionary<Name, string> FuncDescriptions = new Dictionary<Name, string>()
        {
            [Name.operator_add] = "",
            [Name.add] = "",
            [Name.concat] = "",
            [Name.ambiguous_add_test] = "",

            [Name.operator_subtract] = "",
            [Name.subtract] = "",
            [Name.minus] = "",

            [Name.operator_multiply] = "",
            [Name.multiply] = "",
            [Name.mult] = "",

            [Name.operator_divide] = "",
            [Name.div] = "",
            [Name.divide] = "",

            [Name.operator_modulo] = 
                "Computes the remainder given by the dividend A and divisor B.\n            " +
                "Invoked by the % token in the REPL, formatted as A % B.\n      " +
                "EXAMPLES:\n            10 % 5 = 0\n            11 % 6 = 5",
            [Name.modulo] = "",
            [Name.mod] = ""
        };
    }
}