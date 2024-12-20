﻿//############################################################################################################################
//                                                                                                                           #
//  ██████╗ ██╗      █████╗ ███████╗████████╗                           Copyright © 2022 Rob Lemmens | NijnStein Software    #
//  ██╔══██╗██║     ██╔══██╗██╔════╝╚══██╔══╝                                    <rob.lemmens.s31 gmail com>                 #
//  ██████╔╝██║     ███████║███████╗   ██║                                           All Rights Reserved                     #
//  ██╔══██╗██║     ██╔══██║╚════██║   ██║                                                                                   #
//  ██████╔╝███████╗██║  ██║███████║   ██║     V1.0.4e                                                                       #
//  ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝                                                                                   #
//                                                                                                                           #
//############################################################################################################################
//                                                                                                                     (__)  #
//       Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential         (oo)  #
//                                                                                                                     (__)  #
//############################################################################################################################
#pragma warning disable CS1591
#pragma warning disable CS0162

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using Unity.Mathematics;

namespace NSS.Blast.Compiler.Stage
{

    /// <summary>
    /// The Parser: 
    /// 
    /// - Parses list of tokens into a tree of nodes representing the flow of operations  
    /// - Identifies unique parameters 
    /// - Spaghetti warning - handcrafted parser ahead..
    /// 
    /// </summary>
    public class BlastParser : IBlastCompilerStage
    {
        /// <summary>
        /// version 0.2.1
        /// </summary>
        public System.Version Version => new System.Version(0, 2, 1);

        /// <summary>
        /// parsing stage: transforms token list into a node tree
        /// </summary>
        public BlastCompilerStageType StageType => BlastCompilerStageType.Parser;


        /// <summary>
        /// scan for the next token of type 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tokens">token list to search in</param>
        /// <param name="token">token to look for</param>
        /// <param name="idx">idx to start looking from</param>
        /// <param name="max">max idx to look into</param>
        /// <param name="i1">idx of token</param>
        /// <returns>true if found</returns>
        bool find_next(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokens, BlastScriptToken token, int idx, in int max, out int i1)
        {
            while (idx <= max)
            {
                if (tokens[idx].Item1 != token)
                {
                    // as its more likely to not match to token take this branch first 
                    idx++;
                }
                else
                {
                    i1 = idx;
                    return true;
                }
            }
            i1 = -1;
            return false;
        }

        
        /// <summary>
        /// scan for the next token of type 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tokens">token to look for</param>
        /// <param name="idx">idx to start looking from</param>
        /// <param name="max">max idx to look into</param>
        /// <param name="i1">idx of token</param>
        /// <returns>true if found</returns>
        bool find_next(IBlastCompilationData data, BlastScriptToken[] tokens, int idx, in int max, out int i1)
        {
            while (idx <= max)
            {
                if (!tokens.Contains(data.Tokens[idx].Item1))
                {
                    // as its more likely to match to token take this branch first 
                    idx++;
                }
                else
                {
                    i1 = idx;
                    return true;
                }
            }
            i1 = -1;
            return false;
        }

        /// <summary>
        /// search for the next token skipping over compounds 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tokens">token list to use</param>
        /// <param name="token">token to look for</param>
        /// <param name="idx">idx to start looking from</param>
        /// <param name="max">max idx to look into</param>
        /// <param name="i1">idx of token</param>
        /// <param name="accept_eof"></param>
        /// <returns>true if found</returns>
        bool find_next_skip_compound(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokens, BlastScriptToken token, int idx, in int max, out int i1, bool accept_eof = false)
        {
            int size = max - idx;
            int open = 0;

            while (idx <= max)
            {
                BlastScriptToken t = tokens[idx].Item1;

                if (t == BlastScriptToken.OpenParenthesis)
                {
                    open++;
                    idx++;
                    continue;
                }

                if (t == BlastScriptToken.CloseParenthesis)
                {
                    if (open > 0)
                    {
                        open--;
                        idx++;
                        continue;
                    }
                }

                if (t == token)
                {
                    if (open != 0)
                    {
                        data.LogError($"parser.find_next_skip_compound: malformed parenthesis between idx {idx} and {max}");
                        i1 = -1;
                        return false;
                    }

                    i1 = idx;
                    return true;
                }
                else
                {
                    idx++;
                }
            }
            if (accept_eof)
            {
                if (size > 1)
                {
                    i1 = max;
                    return true;
                }
            }
            i1 = -1;
            return false;
        }


        /// <summary>
        /// search for the next token skipping over compounds 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tokenlist"></param>
        /// <param name="tokens">token to look for</param>
        /// <param name="idx">idx to start looking from</param>
        /// <param name="max">max idx to look into</param>
        /// <param name="i1">idx of token</param>
        /// <param name="accept_eof"></param>
        /// <returns>true if found</returns>
        bool find_next_skip_compound(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokenlist, BlastScriptToken[] tokens, int idx, in int max, out int i1, bool accept_eof = false)
        {
            // HMMZ     should remove this copy TODO 
            int size = max - idx;
            int open = 0;

            while (idx <= max)
            {
                BlastScriptToken t = tokenlist[idx].Item1;

                if (t == BlastScriptToken.OpenParenthesis)
                {
                    open++;
                    idx++;
                    continue;
                }

                if (t == BlastScriptToken.CloseParenthesis)
                {
                    if (open > 0)
                    {
                        open--;
                        idx++;
                        continue;
                    }
                }

                if (tokens.Contains(t))
                {
                    if (open != 0)
                    {
                        data.LogError($"parser.find_next_skip_compound: malformed parenthesis between idx {idx} and {max}");
                        i1 = -1;
                        return false;
                    }

                    i1 = idx;
                    return true;
                }
                else
                {
                    idx++;
                }
            }
            if(accept_eof)
            {
                if(size > 1)
                {
                    i1 = max; 
                    return true;
                }
            }
            i1 = -1;
            return false;
        }

        /// <summary>
        /// find next token from idx 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tokens"></param>
        /// <param name="token"></param>
        /// <param name="idx">idx to start looking from</param>
        /// <param name="max">max index to check into</param>
        /// <param name="i1">token location or -1 if not found</param>
        /// <param name="skip_over_compounds">skip over ( ) not counting any token inside the (compound)</param>
        /// <param name="accept_eof">accept eof as succesfull end of search</param>
        /// <returns></returns>
        bool find_next(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokens, BlastScriptToken token, int idx, in int max, out int i1, bool skip_over_compounds = true, bool accept_eof = true)
        {
            bool found;

            if (skip_over_compounds)
            {
                found = find_next_skip_compound(data, tokens, token, idx, max, out i1, accept_eof);
            }
            else
            {
                found = find_next(data, tokens, token, idx, max, out i1, false, accept_eof);
            }

            // accepting eof?? 
            if (!found && idx >= max && accept_eof)
            {
                i1 = max;
                found = true;
            }

            return found;
        }

        /// <summary>
        /// find next match in token array 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tokenlist"></param>
        /// <param name="tokens"></param>
        /// <param name="idx">idx to start looking from</param>
        /// <param name="max">max index to check into</param>
        /// <param name="i1">token location or -1 if not found</param>
        /// <param name="skip_over_compounds">skip over ( ) not counting any token inside the (compound)</param>
        /// <param name="accept_eof">accept eof as succesfull end of search</param>        /// <returns></returns>
        bool find_next(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokenlist, BlastScriptToken[] tokens, int idx, in int max, out int i1, bool skip_over_compounds = true, bool accept_eof = true)
        {
            bool found;

            if (skip_over_compounds)
            {
                found = find_next_skip_compound(data, tokenlist, tokens, idx, max, out i1, accept_eof);
            }
            else
            {
                found = find_next(data, tokenlist, tokens, idx, max, out i1, false, accept_eof);
            }

            // accepting eof?? 
            if (!found && idx >= max && accept_eof)
            {
                i1 = max;
                found = true;
            }

            return found;
        }


        /// <summary>
        /// skip the closure () starting with idx at the (, if true ends with idx at position after ) 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tokens"></param>
        /// <param name="idx"></param>
        /// <param name="idx_max"></param>
        /// <returns></returns>
        bool skip_closure(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokens, ref int idx, int idx_max)
        {
            if (idx > idx_max || data.Tokens[idx].Item1 != BlastScriptToken.OpenParenthesis)
            {
                data.LogError($"parser.skip_closure: expecting parenthesis () but found {data.Tokens[idx].Item1}");
                return false;
            }

            // find IF closure )
            if (!find_next(data, tokens, BlastScriptToken.CloseParenthesis, idx + 1, idx_max, out idx, true, false))
            {
                data.LogError($"parser.skip_closure: malformed parenthesis");
                return false;
            }

            // read 1 past if possible
            if (idx <= idx_max)
            {
                if (idx < idx_max) idx++;
                return true;
            }
            else
            {
                idx++;
                return false;
            }
        }

        /// <summary>
        /// scan token tree and find start and end index of next statement in token list
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tokens"></param>
        /// <param name="idx">current index into token list</param>
        /// <param name="idx_max"></param>
        /// <param name="i1">start index of next statement</param>
        /// <param name="i2">end index of next statement</param>
        bool find_next_statement(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokens, ref int idx, int idx_max, out int i1, out int i2)
        {
            i1 = i2 = -1;
            if (!(idx <= idx_max))
            {
                return false;
            }

            if (idx == idx_max)
            {
                // either single token or could not go past 

            }

            // find start of statement 
            //bool end_on_id = false;
            while (idx <= idx_max && i1 < 0)
            {
                switch (tokens[idx].Item1)
                {
                    case BlastScriptToken.DotComma:
                        data.LogWarning($"parser.FindNextStatement: skipping ';'");
                        idx++;
                        continue;

                    case BlastScriptToken.Nop:
                        idx++;
                        continue;

                    case BlastScriptToken.CloseParenthesis:
                         idx++;
                         data.LogWarning($"TODO REMOVE parser.findnextstatement debug close )");
                        return false;

                    case BlastScriptToken.Switch:
                    case BlastScriptToken.While:
                    case BlastScriptToken.For:
                    case BlastScriptToken.If:
                    case BlastScriptToken.Identifier:
                        i1 = idx;
                        //if(idx == idx_max)
                       // {
                       //     // allow to end on a identifier 
                        //    end_on_id = true; 
                        //}
                        break;

                    default:
                        // error: token not valid in currenct context
                        data.LogError($"parser.FindNextStatement: found invalid token '{tokens[idx].Item1}' while looking for statement start.");
                        i1 = i2 = -1;
                        idx = idx_max + 1;
                        return false;
                }
            }

            // if we reached the end nothing usefull was found
            if (idx >= idx_max)
            {
                i1 = i2 = -1;
                return false;
            }

            // depending on the starting token of the statement we can end up with different flows 
            switch (tokens[i1].Item1)
            {

                case BlastScriptToken.Identifier:
                    // any statement starting with an identifier can be 2 things:
                    // - function called from root returning nothing
                    // - assignment of identifier
                    // - flow is terminated with a ';' or eof
                    // - a statement wont ever start with a -
                    // - a statement can contain (compounds)

                    if (find_next(data, tokens, new BlastScriptToken[] { BlastScriptToken.DotComma, BlastScriptToken.CloseParenthesis }, i1 + 1, idx_max, out i2, true, true))
                    {
                        // read the currentindex past the last part of the statement which is now
                        // contained in tokens[i1-i2]  
                        idx = i2 + 1;

                        // allow ;) read past the parenthesiss
                        if (idx > 0
                            &&
                            idx < tokens.Count - 1
                            &&
                            tokens[idx - 1].Item1 == BlastScriptToken.DotComma
                            &&
                            tokens[idx].Item1 == BlastScriptToken.CloseParenthesis)
                        {
                            idx++;
                        }
                        return true;
                    }
                    break;


                case BlastScriptToken.If:
                    // if then else statements: 
                    // - they always start the statement
                    // - then and else optional but need at least 1  

                    // next token should open IF closure with (
                    idx++;
                    if (idx > idx_max || tokens[idx].Item1 != BlastScriptToken.OpenParenthesis)
                    {
                        data.LogError($"parser.FindNextStatement: expecting parenthesis () after IF but found {tokens[idx].Item1}");
                        return false;
                    }

                    // find IF closure )
                    if (!find_next(data, tokens, BlastScriptToken.CloseParenthesis, idx + 1, tokens.Count - 1, out idx, true, false))
                    {
                        data.LogError($"parser.FindNextStatement: malformed parenthesis in IF condition");
                        return false;
                    }

                    bool have_then_or_else = false;

                    // at i + 1 we either have THEN OR ELSE we could accept a statement..... 
                    idx++;
                    if (idx <= idx_max && tokens[idx].Item1 == BlastScriptToken.Then)
                    {
                        have_then_or_else = true;
                        idx++;
                        // skip the closure 
                        if (!skip_closure(data, tokens, ref idx, idx_max)) return false;
                    }
                    if (idx <= idx_max && tokens[idx].Item1 == BlastScriptToken.Else)
                    {
                        have_then_or_else = true;
                        idx++;
                        // skip the closure 
                        if (!skip_closure(data, tokens, ref idx, idx_max)) return false;
                    }

                    // check if we either have a then or else 
                    if (!have_then_or_else)
                    {
                        data.LogError("parser.FindNextStatement: malformed IF statement, expecting a THEN and/or ELSE compound");
                        return false;
                    }

                    // set the end of the statement and advance to next token 
                    i2 = idx;

                    // take any ; with this statement
                    while (idx <= idx_max && tokens[idx].Item1 == BlastScriptToken.DotComma)
                    {
                        idx++;
                    }

                    if (idx == idx_max && tokens[idx].Item1 == BlastScriptToken.CloseParenthesis)
                    {
                        // read past end 
                        idx++;
                    }

                    // found: ifthenelse statement between i1 and i2 with idx directly after it 
                    return true;

                    // for loops have the same form: [while/for](compound)(compound)
                case BlastScriptToken.For: 
                case BlastScriptToken.While:
                    idx++;

                    // skip the while condition closure 
                    if (!skip_closure(data, tokens, ref idx, idx_max)) return false;

                    // next should be a new closure or a single statement 
                    if (tokens[idx].Item1 == BlastScriptToken.OpenParenthesis)
                    {
                        // TODO : could allow single statement 
                        if (!skip_closure(data, tokens, ref idx, idx_max)) return false;

                        // set the end of the statement and advance to next token 
                        i2 = idx;

                        // take any ; with this statement
                        while (idx <= idx_max && tokens[idx].Item1 == BlastScriptToken.DotComma)
                        {
                            idx++;
                        }

                        if (idx == idx_max && tokens[idx].Item1 == BlastScriptToken.CloseParenthesis)
                        {
                            // read past end 
                            idx++;
                        }

                        // found the while
                        return true;
                    }
                    else
                    {
                        // single statement 
                        data.LogError("find_next_statement: single statement in FOR/WHILE not supported yet use a closure");
                        data.LogToDo("find_next_statement: single statement in FOR/WHILE not supported yet use a closure");
                        return false;
                    }

                // switch(a > b)
                //(
                // case 1:
                //  (
                // e = result_1;  
                //  )
                //  default:
                //  (
                // e = result_2;
                //  )
                //);
                case BlastScriptToken.Switch:
                    idx++;

                    // skip the switch condition closure 
                    if (!skip_closure(data, tokens, ref idx, idx_max)) return false;

                    // skip over cases
                    if (!skip_closure(data, tokens, ref idx, idx_max))
                    {
                        data.LogError("find_next_statement: failed to scan over switch case/default closure");
                        return false;
                    }
                    else
                    {
                        i2 = idx;

                        // take any ; with this statement
                        while (idx <= idx_max && tokens[idx].Item1 == BlastScriptToken.DotComma)
                        {
                            idx++;
                        }

                        return true;
                    }
            }

            // not found 
            return false;
        }


        //
        // statements:
        //
        // identifier; 
        // identifier();
        // identifier[3].x;
        // identifier = identifier op function;
        // 
        // if( statements ) then ( statements ) else ( statements );
        // while (statements ) then (statements);
        // switch(statement) case: [statement;]* default: [statement]*
        //
        node parse_statement(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokens, int idx_start, in int idx_end)
        {
            int i, idx_condition;

            // determine statement type and process accordingly 
            switch (tokens[idx_start].Item1)
            {
                // either an assignment or a function call with no use for result
                case BlastScriptToken.Identifier:

                    // an assignment is a sequence possibly containing compounds ending with ; or eof
                    return parse_sequence(data, tokens, ref idx_start, idx_end);

                case BlastScriptToken.If:
                    {
                        node n_if = new node(nodetype.ifthenelse, BlastScriptToken.If);
                        // parse a statement in the form:
                        //
                        //  if( condition-sequence ) then ( statement list ) else (statement list);
                        //
                        int idx_if = idx_start, idx_then, idx_else;

                        // scan to: then  
                        if (!find_next(data, tokens, BlastScriptToken.Then, idx_start, idx_end, out idx_then, true, false))
                        {
                            data.LogError($"parser.parse_statement: failed to locate matching THEN for IF statement found at token {idx_start} in statement from {idx_start} to {idx_end}");
                            return null;
                        }

                        // get IF condition sequence
                        i = idx_if + 1;
                        node n_condition = parse_sequence(data, tokens, ref i, idx_then - 1);
                        if (n_condition == null || !n_condition.HasChildren)
                        {
                            data.LogError($"parser.parse_statement: failed to parse IF condition or empty condition in statement from {idx_start} to {idx_end}");
                            return null;
                        }
                        // force node to be a condition, validation should later trip on it if its not 
                        n_condition.type = nodetype.condition;
                        n_condition.identifier = node.GenerateUniqueId("ifcond");
                        n_if.SetChild(n_condition);

                        // get THEN compound 
                        // - if first token: ( then compound 
                        // - if first token: IF then nested if then else ........     todo for now force it to be a compound
                        // - else simple sequence
                        if (tokens[idx_then + 1].Item1 == BlastScriptToken.OpenParenthesis)
                        {
                            // get matching parenthesis 
                            if (find_next(data, tokens, BlastScriptToken.CloseParenthesis, idx_then + 2, idx_end, out i, true, false))
                            {
                                // parse statement list between the IFTHEN() 
                                node n_then = n_if.CreateChild(nodetype.ifthen, BlastScriptToken.Then, "then");
                                n_then.identifier = node.GenerateUniqueId("ifthen");
                                int exitcode = parse_statements(data, tokens, n_then, idx_then + 2, i);
                                if (exitcode != (int)BlastError.success)
                                {
                                    data.LogError($"parser.parse_statement: failed to parse IFTHEN statement list in statement from {idx_start} to {idx_end}, exitcode: {exitcode}");
                                    return null;
                                }
                            }
                            else
                            {
                                // could not find closing )
                                data.LogError($"parser.parse_statement: failed to parse IFTHEN statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for IFTHEN closure");
                                return null;
                            }
                        }
                        else
                        {
                            // no IFTHEN () 
                            data.LogError($"parser.parse_statement: failed to parse IFTHEN statement list in statement from {idx_start} to {idx_end}, the statement list inside the THEN closure should be encapsulated by parenthesis.");
                            return null;
                        }

                        // get ELSE, by forcing THEN to have () we can get ELSE simply by skipping compound on search
                        idx_else = i + 1; // token after current must be ELSE for an else statement to be correct

                        if (idx_else <= idx_end && tokens[idx_else].Item1 == BlastScriptToken.Else)
                        {
                            // read else, forse IFELSE with ()  
                            if (idx_else + 1 <= idx_end && tokens[idx_else + 1].Item1 == BlastScriptToken.OpenParenthesis)
                            {
                                // get matching parenthesis 
                                if (find_next(data, tokens, BlastScriptToken.CloseParenthesis, idx_else + 2, idx_end, out i, true, false))
                                {
                                    // parse statement list between the IFTHEN() 
                                    node n_else = n_if.CreateChild(nodetype.ifelse, BlastScriptToken.Else, "else");
                                    n_else.identifier = node.GenerateUniqueId("ifelse");
                                    int exitcode = parse_statements(data, tokens, n_else, idx_else + 2, i);
                                    if (exitcode != (int)BlastError.success)
                                    {
                                        data.LogError($"parser.parse_statement: failed to parse IFELSE statement list in statement from {idx_start} to {idx_end}, exitcode: {exitcode}");
                                        return null;
                                    }
                                }
                                else
                                {
                                    // could not find closing )
                                    data.LogError($"parser.parse_statement: failed to parse IFTHENELSE statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for IFELSE closure");
                                    return null;
                                }
                            }
                            else
                            {
                                // no IFTHEN () 
                                data.LogError($"parser.parse_statement: failed to parse IFTHENELSE statement list in statement from {idx_start} to {idx_end}, the statement list inside the ELSE closure should be encapsulated by parenthesis.");
                                return null;
                            }
                        }
                        return n_if;
                    }

                case BlastScriptToken.While:
                    {
                        node n_while = new node(nodetype.whileloop, BlastScriptToken.While);
                        n_while.identifier = node.GenerateUniqueId("while"); 

                        // next token MUST be ( 
                        i = idx_start + 1;
                        if (tokens[i].Item1 == BlastScriptToken.OpenParenthesis)
                        {
                            // get matching parenthesis 
                            idx_condition = i + 1;
                            if (find_next(data, tokens, BlastScriptToken.CloseParenthesis, idx_condition, idx_end, out i, true, false))
                            {
                                // from here read the while condition sequence 
                                node n_condition = parse_sequence(data, tokens, ref idx_condition, i - 1);
                                if (n_condition == null || !n_condition.HasChildren)
                                {
                                    data.LogError($"parser.parse_statement: failed to parse WHILE condition or empty condition in statement from {idx_start} to {idx_end}");
                                    return null;
                                }
                                else
                                {
                                    n_condition.identifier = node.GenerateUniqueId("while_condition");  
                                    n_condition.type = nodetype.condition;
                                    n_while.SetChild(n_condition);
                                }
                            }
                            else
                            {
                                // could not find closing )
                                data.LogError($"parser.parse_statement: failed to parse WHILE statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for WHILE condition");
                                return null;
                            }

                            // now read while statement list
                            i = i + 1;
                            if (tokens[i].Item1 == BlastScriptToken.OpenParenthesis)
                            {
                                // should have matching statement list 
                                int idx_compound = i + 1;
                                if (find_next(data, tokens, BlastScriptToken.CloseParenthesis, idx_compound, idx_end, out i, true, false))
                                {
                                    // parse statement list between the IFTHEN() 
                                    node n_compound = n_while.CreateChild(nodetype.whilecompound, BlastScriptToken.While, node.GenerateUniqueId("while_compound"));
                                    int exitcode = parse_statements(data, tokens, n_compound, idx_compound, i - 1);
                                    if (exitcode != (int)BlastError.success)
                                    {
                                        data.LogError($"parser.parse_statement: failed to parse WHILE statement list in statement from {idx_start} to {idx_end}, exitcode: {exitcode}");
                                        return null;
                                    }
                                }
                                else
                                {
                                    // could not find closing )
                                    data.LogError($"parser.parse_statement: failed to parse WHILE statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for WHILE compound closure");
                                    return null;
                                }
                            }
                            else
                            {
                                // no WHILE compound () 
                                data.LogToDo("Allow while statements to omit () on compound statement list if 1 statement");
                                data.LogError($"parser.parse_statement: failed to parse WHILE statement in statement from {idx_start} to {idx_end}, the while compound statement list must be encapsulated in parenthesis.");
                                return null;
                            }
                        }
                        else
                        {
                            // no WHILE condition () 
                            data.LogError($"parser.parse_statement: failed to parse WHILE statement in statement from {idx_start} to {idx_end}, the while condition must be encapsulated in parenthesis.");
                            return null;
                        }

                        return n_while;
                    }

                case BlastScriptToken.For:
                    {
                        node n_for = new node(nodetype.forloop, BlastScriptToken.For);
                        n_for.identifier = node.GenerateUniqueId("for");
                        i = idx_start + 1;
                        if (tokens[i].Item1 == BlastScriptToken.OpenParenthesis)
                        {
                            // get matching parenthesis 
                            idx_condition = i + 1;
                            if (find_next(data, tokens, BlastScriptToken.CloseParenthesis, idx_condition, idx_end, out i, true, false))
                            {
                                // should be 3 ; seperated statements 
                                int exitcode = parse_statements(data, tokens, n_for, idx_condition, i - 1);
                                if (exitcode != (int)BlastError.success)
                                {
                                    data.LogError($"Parser.ParseStatement: failed to parse FOR statement list in statement from {idx_start} to {idx_end}, exitcode: {exitcode}");
                                    return null;
                                }

                                // should be 3 statements... we wont allow omitting one
                                if (n_for.children.Count != 3)
                                {
                                    data.LogError($"Parser.ParseStatement: failed to read FOR statement, found {n_for.children.Count} statements from token {idx_condition} to {i - 1} but expeced 3");
                                }
                            }
                            else
                            {
                                // could not find closing )
                                data.LogError($"Parser.ParseStatement: failed to parse FOR condition statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for FOR conditions");
                                return null;
                            }

                            // get the compound 
                            i = i + 1;
                            if (tokens[i].Item1 == BlastScriptToken.OpenParenthesis)
                            {
                                // should have matching statement list 
                                int idx_compound = i + 1;
                                if (find_next(data, tokens, BlastScriptToken.CloseParenthesis, idx_compound, idx_end, out i, true, false))
                                {
                                    // parse statement list between the IFTHEN() 
                                    node n_compound = n_for.CreateChild(nodetype.compound, BlastScriptToken.While, node.GenerateUniqueId("for_compound"));
                                    int exitcode = parse_statements(data, tokens, n_compound, idx_compound, i - 1);
                                    if (exitcode != (int)BlastError.success)
                                    {
                                        data.LogError($"Parser.ParseStatement: failed to parse FOR statement list in statement from {idx_start} to {idx_end}, exitcode: {exitcode}");
                                        return null;
                                    }
                                }
                                else
                                {
                                    // could not find closing )
                                    data.LogError($"parser.parse_statement: failed to parse FOR statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for FOR compound closure");
                                    return null;
                                }
                            }
                            else
                            {
                                // no WHILE compound () 
                                data.LogToDo("Allow FOR statements to omit () on compound statement list if 1 statement");
                                data.LogError($"Parser.ParseStatement: failed to parse FOR statement in statement from {idx_start} to {idx_end}, the FOR compound statement list must be encapsulated in parenthesis.");
                                return null;
                            }

                            return n_for;
                        }
                        else
                        {
                            data.LogError($"Parser.ParseStatement: failed to parse FOR statement, the for is not followed by ( but by an unsupported token: '{tokens[i].Item1}'");
                            return null;
                        }
                    }


                case BlastScriptToken.Switch:
                    {
                        node n_switch = new node(nodetype.switchnode, BlastScriptToken.Switch);

                        // next token MUST be ( 
                        i = idx_start + 1;
                        if (tokens[i].Item1 != BlastScriptToken.OpenParenthesis)
                        {
                            data.LogError($"parser.parse_statement: failed to parse switch statement in statement from {idx_start} to {idx_end}, the switch condition must be encapsulated in parenthesis.");
                            return null;
                        }

                        idx_condition = i + 1;
                        if (find_next(data, tokens, BlastScriptToken.CloseParenthesis, idx_condition, idx_end, out i, true, false))
                        {
                            // from here read the switch condition sequence 
                            node n_condition = parse_sequence(data, tokens, ref idx_condition, i - 1);
                            if (n_condition == null || !n_condition.HasChildren)
                            {
                                data.LogError($"parser.parse_statement: failed to parse switch condition or empty condition in statement from {idx_start} to {idx_end}");
                                return null;
                            }
                            else
                            {
                                n_condition.type = nodetype.condition;
                                n_condition.identifier = node.GenerateUniqueId("ifcond");
                                n_switch.SetChild(n_condition);
                            }
                        }
                        else
                        {
                            // could not find closing )
                            data.LogError($"parser.parse_statement: failed to parse SWITCH statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for SWITCH condition");
                            return null;
                        }

                        // read switch cases
                        idx_start = i + 1;
                        if (tokens[idx_start].Item1 != BlastScriptToken.OpenParenthesis)
                        {
                            data.LogError($"parser.parse_statement: malformed parentheses in switch compound from {idx_start} to {idx_end}");
                            return null;
                        }

                        // find matching end of compound 
                        if (find_next(data, tokens, BlastScriptToken.CloseParenthesis, idx_start + 1, idx_end, out i, true, false))
                        {
                            int n_cases = 0;

                            // read all cases + default
                            while (true)
                            {
                                int idx_case;
                                idx_start = idx_start + 1;

                                // make sure there is at least 1 case or 1 default 
                                if (find_next(data, new BlastScriptToken[] { BlastScriptToken.Case, BlastScriptToken.Default }, idx_start, idx_end, out idx_case))
                                {
                                    // case or default ? 
                                    bool is_default = tokens[idx_case].Item1 == BlastScriptToken.Default;
                                    bool is_case = tokens[idx_case].Item1 == BlastScriptToken.Case;
                                    if (!(is_default || is_case))
                                    {
                                        data.LogError($"parser.parse_statement: failed to locate case or default statements in switch compound from {idx_start} to {idx_end}");
                                        return null;
                                    }
                                    n_cases = n_cases + 1;

                                    // read the b statements for the case/default 

                                    // here there must be an identifier sequence followed by :
                                    // directly after the case/default we must find ':' followed by either 1 statement or a compound with multiple statements 
                                    int idx_ternary;
                                    idx_case = idx_case + 1;
                                    if (!find_next(data, tokens, BlastScriptToken.TernaryOption, idx_case, idx_end, out idx_ternary, true, false))
                                    {
                                        data.LogError($"parser.parse_statement: failed to parse switch statement, malformed case/default compound from {idx_start} to {idx_end}");
                                        return null;
                                    }

                                    node n_case = new node(null);
                                    n_case.type = is_default ? nodetype.switchdefault : nodetype.switchcase;
                                    n_case.token = is_default ? BlastScriptToken.Default : BlastScriptToken.Case;
                                    n_switch.SetChild(n_case);

                                    if (!is_default)
                                    {
                                        node n_case_condition = parse_sequence(data, tokens, ref idx_case, idx_ternary - 1);
                                        n_case_condition.type = nodetype.condition;
                                        n_case.SetChild(n_case_condition);
                                    }

                                    // either 1 statement or a list surrounded in a compound. 
                                    if (tokens[idx_ternary + 1].Item1 == BlastScriptToken.OpenParenthesis)
                                    {
                                        // a list in compound 
                                        idx_case = idx_ternary + 2;
                                        if (find_next(data, tokens, BlastScriptToken.CloseParenthesis, idx_case, idx_end, out i, true, false))
                                        {
                                            // parse statement list in the CASE: ()  
                                            int exitcode = parse_statements(data, tokens, n_case, idx_case, i - 1);
                                            if (exitcode != (int)BlastError.success)
                                            {
                                                data.LogError($"parser.parse_statement: failed to parse CASE/DEFAULT statement list in SWITCH statement from {idx_start} to {idx_end}, exitcode: {exitcode}");
                                                return null;
                                            }
                                            else
                                            {
                                                idx_start = i;
                                            }
                                        }
                                        else
                                        {
                                            // could not find closing )
                                            data.LogError($"parser.parse_statement: failed to parse CASE/DEFAULT statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for CASE compound closure");
                                            return null;
                                        }

                                    }
                                    else
                                    {
                                        // a list until the next case / default 
                                        data.LogToDo("single statement switch case / default ");
                                        data.LogError($"parser.parse_statement: failed to parse CASE/DEFAULT statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for CASE compound closure");
                                        return null;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }

                            // verify: need at least 1 case or 1 default 
                            if (n_cases <= 0)
                            {
                                data.LogError("parser.parse_statement: found switch statement without case or default compound, this is not allowed");
                                return null;
                            }

                            return n_switch;
                        }
                        else
                        {
                            data.LogError($"parser.parse_statement: malformed parentheses in switch compound from {idx_start} to {idx_end}, could not locate closing parenthesis");
                            return null;
                        }
                    }
            }

            return null;
        }


        //
        // identifiers: 
        // 
        //      Identifier,     // [a..z][0..9|a..z]*[.|[][a..z][0..9|a..z]*[]]
        //      Indexer,        // .        * using the indexer on a numeric will define its fractional part 
        //      IndexOpen,      // [
        //      IndexClose,     // ]
        //
        //
        //    23423.323
        //    1111
        //    a234.x
        //    a123[3].x
        //    a123[a - 3].x[2].x
        //
        //    * in a later stage : (1 2 3 4).x          => vector with index 
        //    * thus we must also allow an indexer after compounds and functions
        //
        // * whitespace is allowed in between indexers when not numeric 
        // * parser should parse identifier into compounds if needed (flatten should then flatten that later :p)



        /// <summary>
        /// scan and parse a numeric from the token list in the form:
        /// 
        /// -100.23
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="idx">indext to start scan from, on success wil be at position after last token of identifier</param>
        /// <param name="idx_max">the max index to scan into</param>
        /// <returns>null on failure, a node with the value on success</returns>
        unsafe node scan_and_parse_numeric(IBlastCompilationData data, ref int idx, in int idx_max)
        {
            return scan_and_parse_numeric(data, data.Tokens, ref idx, idx_max);
        }
        unsafe node scan_and_parse_numeric(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokens, ref int idx, in int idx_max)
        {
            if (idx == idx_max && idx < tokens.Count && tokens[idx].Item1 == BlastScriptToken.Identifier)
            {
                idx++; 
                return new node(null)
                {
                    is_constant = true,
                    identifier = tokens[idx-1].Item2,
                    type = nodetype.parameter
                };
            }

            // we wont ever have minus = true in current situation because of tokenizer and how we read assignments
            // but leave it here just in case 
            bool minus = tokens[idx].Item1 == BlastScriptToken.Substract;
            

            bool has_data = !minus; // if first token is minus sign then we dont have data yet
            bool has_fraction = false;
            bool has_indexer = false;
            
            int vector_size = 1;

            string value = tokens[idx].Item2;
            idx++;

            while (idx <= idx_max && idx < tokens.Count)
            {
                switch (tokens[idx].Item1)
                {
                    case BlastScriptToken.Identifier:
                        {
                            if (!has_data)
                            {
                                // first part of value
                                value += tokens[idx].Item2;
                                idx++;
                                has_data = true;
                                break;
                            }

                            //if (has_indexer || (idx == idx_max))
                            {
                                if (has_data && !has_fraction && has_indexer)
                                {
                                    // last part of value
                                    value += tokens[idx].Item2;
                                    has_fraction = true;
                                    idx++;
                                    // retrn a 'parameter' node... bad name.. 
                                    return new node(null)
                                    {
                                        is_constant = true,
                                        identifier = value,
                                        vector_size = 1, 
                                        datatype = BlastVariableDataType.Numeric,
                                        type = nodetype.parameter
                                    };
                                }
                            }

                            //
                            // not a valid numeric  OR  a vector 
                            //
                            // - could grow vector here
                            //
                            if(has_data)
                            {
                                // todo, not sure - COULD GROW VECTOR HERE AND RETURN THAT IN NODE
                                return new node(null)
                                {
                                    is_constant = true,
                                    identifier = value,
                                    vector_size = vector_size,
                                    datatype = BlastVariableDataType.Numeric,
                                    type = nodetype.parameter
                                };
                            }

                            //idx++;

                            data.LogError($"scan_and_parse_numeric: sequence of operations not valid for a numeric value: {tokens[idx].Item2} in section {idx} - {idx_max} => {Blast.VisualizeTokens(tokens, idx, idx_max)}");
                            return null;

                            // break;
                        }

                    default:
                    case BlastScriptToken.Nop:
                        // ok after whole number and after number with indexer and fraction
                        if (has_data && !has_fraction && !has_indexer)
                        {
                            // ok, return value, a fractional would have returned in the above case
                            // idx++; dont inrease index, we stop on the next char after identifier 
                            return new node(null)
                            {
                                is_constant = true,
                                identifier = value,
                                vector_size = vector_size,
                                datatype = BlastVariableDataType.Numeric,
                                type = nodetype.parameter
                            };
                        }

                        data.LogError("scan_and_parse_numeric: sequence of operations not valid for a numeric value, whitespace is not allowed in the fractional part");
                        return null;

                    case BlastScriptToken.Indexer:
                        if (has_data && !has_indexer)
                        {
                            value = value + ".";
                            has_indexer = true;
                            idx++;
                            break;
                        }
                        data.LogError("scan_and_parse_numeric: sequence of operations not valid for a numeric value, error defining fractional part");
                        return null;
                }
            }

            // if we at this point have data (and only data) then return it 
            if (has_data && !has_fraction && !has_indexer)
            {
                return new node(null)
                {
                    is_constant = true,
                    identifier = value,
                    vector_size = vector_size,
                    datatype = BlastVariableDataType.Numeric,
                    type = nodetype.parameter
                };
            }

            // all other cases are an error
            data.LogError("scan_and_parse_numeric: sequence of operations not valid for a numeric value, error defining fractional part");
            return null;
        }

        /// <summary>
        /// scan input from idx building up 1 identifier as we go, after returning a valid node 
        /// the scan index will be on the token directly after the identifier 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="idx">the index starting the scan from and the must be on the first token of the identifier to parse. on succes it will be on the token directly after the identifier</param>
        /// <param name="idx_max">the max index to scan into (max including)</param>
        /// <param name="add_minus"></param>
        /// <returns>null on errors, a valid node on success
        /// - can return nodes: function() and identifier[34].x
        /// </returns>
        unsafe node scan_and_parse_identifier(IBlastCompilationData data, ref int idx, in int idx_max, bool add_minus = false)
        {
            return scan_and_parse_identifier(data, data.Tokens, ref idx, in idx_max, add_minus); 
        }
        unsafe node scan_and_parse_identifier(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokens, ref int idx, in int idx_max, bool add_minus = false)
        {
            Assert.IsNotNull(tokens);

            // step 1> we read a number if

            // - first char is number 
            // - first token is negative sign second token first char is digit 
            bool minus = tokens[idx].Item1 == BlastScriptToken.Substract;

            if(add_minus) 
            {
                // toggle minus sign if requested 
                minus = !minus;
            }

            bool is_number =
                // first minus and second char is digit 
                (minus && (idx + 1 <= idx_max) && tokens[idx + 1].Item2 != null && tokens[idx + 1].Item2.Length > 0 && char.IsDigit(tokens[idx + 1].Item2[0]))
                ||
                // first char is digit 
                (tokens[idx].Item2 != null && tokens[idx].Item2.Length > 0 && char.IsDigit(tokens[idx].Item2[0]));

            is_number = is_number && (tokens[idx].Item2.Length < 14) && CodeUtils.OnlyNumericChars(tokens[idx].Item2); 

            if (is_number)
            {
                // just return a numeric constant
                node n_var_or_constant = scan_and_parse_numeric(data, tokens, ref idx, in idx_max);
                
                if(n_var_or_constant != null && minus)
                {
                    // add back the negative sign, in a later stage it will get stripped when determining wether to load value from constants 
                    n_var_or_constant.identifier = "-" + n_var_or_constant.identifier;
                }
                return n_var_or_constant; 
            }

            // binary?
            if (CodeUtils.TryAsBool32(tokens[idx].Item2, true, out uint uintb32))
            {
                // read binary value from token 
                node n_constant = new node(null);
                n_constant.is_constant = true;
                n_constant.constant_op = blast_operation.nop; 
                n_constant.type = nodetype.parameter;
                n_constant.datatype = BlastVariableDataType.Bool32;
                n_constant.identifier = tokens[idx].Item2;
                // increase token index reference 
                idx += 1;
                return n_constant; 
            }

            // is is a named constant?
            if (Blast.TryGetNamedConstant(tokens[minus ? idx + 1 : idx].Item2, out blast_operation constant_op))
            {
                node n_constant = new node(null);

                if (minus) idx++; 

                n_constant.is_constant = true;
                n_constant.identifier = tokens[idx].Item2;
                n_constant.vector_size = 1;
                n_constant.type = nodetype.parameter;
                n_constant.datatype = BlastVariableDataType.Numeric; // any defined constant is always of type NUMERIC 
                n_constant.constant_op = constant_op;

                idx++; 

                if (minus)
                {
                    if (constant_op == blast_operation.infinity)
                    {
                        n_constant.identifier = "negative_" + n_constant.identifier;
                        n_constant.constant_op = blast_operation.negative_infinity;
                        return n_constant;
                    }
                    else
                    {
                        // encapsulate constant in (-constant) 
                        return NegateNodeInCompound(true, n_constant);
                    }
                }
                else
                {
                    return n_constant;
                }
            }

            
            // is it a function defined by blast? 
            if(data.Blast.Data->TryGetFunctionByName(tokens[idx].Item2, out BlastScriptFunction function))
            {
                // parse out function
                node n_function = scan_and_parse_function(data, tokens, function, ref idx, in idx_max);

                return NegateNodeInCompound(minus, n_function);
            }
            else
            {
                // is it an inlined function? 
                if (((CompilationData)data).TryGetInlinedFunction(tokens[idx].Item2, out BlastScriptInlineFunction inlined_function))
                {

                    node n_function = scan_and_parse_function(data, tokens, inlined_function, ref idx, in idx_max);
                    return NegateNodeInCompound(minus, n_function); 
                }
                else
                {
                    // now n_id should end up being a parameter with possible an index chain 
                    node n_id = new node(null);
                    n_id.identifier = tokens[idx].Item2;
                    n_id.type = nodetype.parameter;
                    idx++;

                    // grow a chain of indices 
                    return NegateNodeInCompound(minus, grow_index_chain(data, tokens, n_id, ref idx, in idx_max));
                }
            }
        }


    

        /// <summary>
        /// if minus:
        /// - insert a parent compound: parent.function => parent.compound.function 
        /// - insert sibling with substract opcode
        /// </summary>
        /// <param name="minus"></param>
        /// <param name="n_function"></param>
        /// <returns></returns>
        private static node NegateNodeInCompound(bool minus, node n_function)
        {
            // negate function result ? 
            if (minus)
            {
                // set a compound as parent 
                n_function.InsertParent(new node(nodetype.compound, BlastScriptToken.Nop));

                // add a substract op 
                n_function.InsertChild(0, new node(nodetype.operation, BlastScriptToken.Substract));

                // return the compound
                return n_function.parent;
            }
            else
            {
                // return the function node 
                return n_function;
            }
        }

        /// <summary>
        /// scan and parse out the next function. 
        /// 
        ///  function;
        ///  function();
        ///  function(function(a, b), c); 
        ///  function(function(a, b)[2].x, c); 
        /// 
        /// </summary>
        /// <param name="data">general compiler data</param>
        /// <param name="function">function to parse</param>
        /// <param name="idx">idx, starting at function, ending directly after</param>
        /// <param name="idx_max">max index to scan into</param>
        /// <returns>a node containing the function or null on failure</returns>
        node scan_and_parse_function(IBlastCompilationData data, BlastScriptFunction function, ref int idx, in int idx_max)
        {
            return scan_and_parse_function(data, data.Tokens, function, ref idx, in idx_max);         
        }
        node scan_and_parse_function(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokens, BlastScriptFunction function, ref int idx, in int idx_max)
        {
            node n_function = new node(null)
            {
                type = nodetype.function,
                identifier = function.GetFunctionName(),
                function = function
            };

            // parse parameters, which possibly are statements too
            // idx should be at ( directly after function, any other token then ,; is an error. 

            //
            // for 1 function we apply some sugar: return, we dont want to have to do return(a); but return a; 
            // 
            bool is_return = function.ScriptOp == blast_operation.ret; 

            idx++;
            bool parameter_less = false;

            // end reached? parameterless function 
            if (idx < idx_max)
            {
                if (tokens[idx].Item1 != BlastScriptToken.OpenParenthesis && !is_return)
                {
                    if (tokens[idx].Item1 == BlastScriptToken.DotComma
                        ||
                        tokens[idx].Item1 == BlastScriptToken.Comma)
                    {
                        // parameter less function 
                        idx++;
                        parameter_less = true;
                    }
                    else
                    {
                        // error condition 
                        data.LogError($"parse.parse_function: scanned function {function.GetFunctionName()}, malformed parameter compound, expecting parameters, empty compound or statement termination");
                        return null;
                    }
                }
                else
                {
                    if (is_return)
                    {
                        // return used with (), just allow it 
                        // data.LogError($"parse.parse_function: scanned function {function.GetFunctionName()}, malformed parameter compound, expecting 1 parameter without parenthesis");
                        // return null;
                    }
                }
            }
            else
            {
                parameter_less = true;
                idx++;
            }

            // scan for parameters and indexer
            if (!parameter_less)
            {
                int idx_end;

                // at '(', find next ) skipping over compounds
                if (!is_return)
                {
                    idx++;
                }
                if (find_next(data, tokens, is_return ? BlastScriptToken.DotComma : BlastScriptToken.CloseParenthesis, idx, idx_max, out idx_end, true, false))
                {
                    int end_function_parenthesis = idx_end;
                    List<node> current_nodes = new List<node>(); 

                    while (idx < end_function_parenthesis)
                    {
                        BlastScriptToken token = tokens[idx].Item1;
                        switch (token)
                        {
                            // identifier, possibly a nested function 
                            case BlastScriptToken.Identifier:
                                current_nodes.Add(n_function.SetChild(scan_and_parse_identifier(data, tokens, ref idx, end_function_parenthesis)));
                                break;

                            // seperator 
                            case BlastScriptToken.Comma:
                            case BlastScriptToken.Nop:

                                if (current_nodes.Count > 0)
                                {
                                    if (current_nodes.Count > 1)
                                    {
                                        // create a child node with the current nodes as children 
                                        // foreach (node cnc in current_nodes) n_function.children.Remove(cnc); // this is not needed
                                        n_function.CreateChild(nodetype.compound, BlastScriptToken.Nop, "").SetChildren(current_nodes);
                                    }
                                    else
                                    {
                                       // it is already a child in this case and nothing needs to happen
                                    }

                                    current_nodes.Clear(); 
                                }
                                idx++;
                                break;

                            // compounds embedded in parameters -> statements resulting in values : 'sequences'
                            case BlastScriptToken.OpenParenthesis:

                                if(tokens[idx - 1].Item1 == BlastScriptToken.CloseParenthesis)
                                {
                                    if (current_nodes.Count > 0)
                                    {
                                        //
                                        // this is a sequence within the parameter sequence 
                                        //

                                        // it is either a list of operations or a vector, in both case push it
                                        n_function.CreateChild(nodetype.compound, BlastScriptToken.Nop, "").SetChildren(current_nodes);
                                        current_nodes.Clear();
                                    }
                                }

                                if (find_next(data, tokens, BlastScriptToken.CloseParenthesis, idx + 1, end_function_parenthesis, out idx_end, true, false))
                                {
                                    current_nodes.Add(n_function.SetChild(parse_sequence(data, tokens, ref idx, idx_end)));
                                }
                                else
                                {
                                    data.LogError($"parser.parse_function: malformed compounded statement in function parameter list, found openparenthesis at tokenindex {idx+1} but no matching close before {idx_max}");
                                    return null;
                                }
                                break;

                            // operations
                            case BlastScriptToken.Add:
                            case BlastScriptToken.Substract:
                            case BlastScriptToken.Divide:
                            case BlastScriptToken.Multiply:
                            case BlastScriptToken.Equals:
#if SUPPORT_TERNARY
                            case BlastScriptToken.Ternary:
                            case BlastScriptToken.TernaryOption:
#endif
                            case BlastScriptToken.SmallerThen:
                            case BlastScriptToken.GreaterThen:
                            case BlastScriptToken.SmallerThenEquals:
                            case BlastScriptToken.GreaterThenEquals:
                            case BlastScriptToken.NotEquals:
                            case BlastScriptToken.And:
                            case BlastScriptToken.Or:
                            case BlastScriptToken.Xor:
                            case BlastScriptToken.Not:
                                // we are scanning inside a parameter list and are dropping the comma;s   (somewhere)
                                // the minus should be added to the next identifier before the next comma
                                current_nodes.Add(n_function.CreateChild(nodetype.operation, token, tokens[idx].Item2));
                                idx++;
                                break;

                            case BlastScriptToken.CloseParenthesis:
                                // this is ok if we have a single compound in current_nodes representing a parameter 
                                if (current_nodes.Count == 1 && current_nodes[0].type == nodetype.compound)
                                {
                                    idx++;
                                }
                                else
                                {
                                    data.LogError($"parse.parse_function: found unexpected token '{token}' in function '{function.GetFunctionName()}' while parsing function parameters`, token index: {idx} => {Blast.VisualizeTokens(tokens, idx, idx_end)}  => \n{n_function.ToNodeTreeString()}");
                                    return null;
                                }
                                break; 
                            case BlastScriptToken.Indexer:
                            case BlastScriptToken.IndexOpen:
                            case BlastScriptToken.IndexClose:
                            case BlastScriptToken.If:
                            case BlastScriptToken.Then:
                            case BlastScriptToken.Else:
                            case BlastScriptToken.While:
                            case BlastScriptToken.Switch:
                            case BlastScriptToken.Case:
                            case BlastScriptToken.Default:
                                data.LogError($"parse.parse_function: found unexpected token '{token}' in function '{function.GetFunctionName()}' while parsing function parameters`, token index: {idx} => {Blast.VisualizeTokens(tokens, idx, idx_end)}  => \n{n_function.ToNodeTreeString()}");
                                return null;
                        }
                    }

                    //if we have nodes in this list, then we need to child them so we create a compounded param asif encountering a comma
                    if (current_nodes.Count > 0)
                    {
                        // create a child node with the current nodes as children 
                        // - create a compound if more then one child 
                        if (current_nodes.Count > 1)
                        {
                            // check if its an negated constant, if so depending on options inline it
                            if (current_nodes.Count == 2
                                &&
                                data.CompilerOptions.InlineConstantData
                                &&
                                current_nodes[0].token == BlastScriptToken.Substract
                                &&
                                current_nodes[1].is_constant)
                            {
                                // instead of pushing the sequence (- constant) we could inline the consant value 
                                if (!float.IsNaN(CodeUtils.AsFloat(current_nodes[1].identifier)))
                                {
                                    node n = n_function.CreateChild(nodetype.parameter, BlastScriptToken.Nop, "-" + current_nodes[1].identifier);
                                    n_function.children.Remove(current_nodes[0]);
                                    n_function.children.Remove(current_nodes[1]);
                                    n.variable = ((CompilationData)data).GetOrCreateVariable(n.identifier);
                                    n.variable.IsConstant = true;
                                    n.variable.VectorSize = 1;
                                }
                            }
                            else
                            {
                                n_function.CreateChild(nodetype.compound, BlastScriptToken.Nop, "").SetChildren(current_nodes);
                            }
                        }
                        else
                        {
                            n_function.SetChild(current_nodes[0]); 
                        }
                    }


                    idx = end_function_parenthesis + 1; 
                }
                else
                {
                    data.LogError($"parse.parse_function: scanned function {function.GetFunctionName()} has malformed parenthesis");
                }

                // validate what we can: determine parameter count from nr of seperators, other validations in later stages
                int param_count = n_function.children.Count;
                if (param_count < function.MinParameterCount || param_count > function.MaxParameterCount)
                {
                    //  fails in case: 
                    //  reads:          clamp((1 2), 3, 4)    
                    //  produces:       clamp(((1 2) 3) 4)
                    //  -> dueue to tokenizer removing the , after the )

                    data.LogError($"parser.parse_function: found function {function.GetFunctionName()} with {param_count} parameters while it should have {function.MinParameterCount} to {function.MaxParameterCount} parameters => \n{n_function.ToNodeTreeString()}");
                    return null;
                }

                // allow a chain of indices to follow
                return grow_index_chain(data, tokens, n_function, ref idx, in idx_max);
            }
            // Function without indexers or parameters 
            else
            {
                // validate function can be parameterless 
                if (function.MinParameterCount == 0)
                {
                    return n_function;
                }
                else
                {
                    data.LogError($"parser.parse_function: found function {function.GetFunctionName()} without parameters while at minimum {function.MinParameterCount} parameters are expected => \n{n_function.ToNodeTreeString()}");
                    return null;
                }
            }
        }


        /// <summary>
        /// scan an inlined function 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tokens"></param>
        /// <param name="function"></param>
        /// <param name="idx"></param>
        /// <param name="idx_max"></param>
        /// <returns></returns>
        node scan_and_parse_function(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokens, BlastScriptInlineFunction function, ref int idx, in int idx_max)
        {
            return scan_and_parse_function(data, tokens, function.GenerateDummyScriptFunction(),  ref idx, in idx_max);
        }


        /// <summary>
        /// parse a sequence of tokens between () into a list of nodes 
        /// 
        /// -> start with idx on the ( of the enclosing compound 
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tokens">tokens to index, can be a slice of data.Tokens</param>
        /// <param name="idx">index directly after the opening ( or at first token of sequence within () </param>
        /// <param name="idx_max"></param>
        /// <param name="as_parameters"></param>
        /// <returns></returns>
        node parse_sequence(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokens, ref int idx, in int idx_max, bool as_parameters = false)
        {
            int idx_end = -1;
            node n_sequence = new node(null) { type = nodetype.compound };

            // if starting on parenthesis assume sequence compounded with parenthesis
            bool has_parenthesis = tokens[idx].Item1 == BlastScriptToken.OpenParenthesis;
            if (has_parenthesis) idx++;

            BlastScriptToken prev_token = BlastScriptToken.Nop;
            bool minus = false;
            bool if_sure_its_vector_define = false; 

            // read all tokens 
            while (idx <= idx_max)
            {
                BlastScriptToken token = tokens[idx].Item1;
                if (token == BlastScriptToken.CloseParenthesis)
                {
                    if (has_parenthesis || idx == idx_max)
                    {
                        // closing 
                        break;
                    }
                }

                if(prev_token == BlastScriptToken.Comma)
                {
                    if(token != BlastScriptToken.Substract)
                    {
                        // should we raise error ?  or just allow it TODO 
                        if (!as_parameters)
                        {
                            data.LogTrace(", used in non parameter sequence");
                        }
                    }
                }

                switch (token)
                {
                    // identifier, possibly a nested function 
                    case BlastScriptToken.Identifier:
                        
                        // this cannot be anything else when 2 identifiers follow 
                        if (prev_token == BlastScriptToken.Identifier) if_sure_its_vector_define = true; 

                        node ident = n_sequence.SetChild(scan_and_parse_identifier(data, tokens, ref idx, idx_max, minus));

                        // reset minus sign after reading an identifier 
                        minus = false; 
                        break;

                    case BlastScriptToken.Nop:
                        idx++;
                        break;

                    // seperator not allowed in sequence 
                    case BlastScriptToken.Comma:
                        
                        // only allow in sequence if sure its a vector define ??  
                        // data.LogError($"parser.parse_sequence: seperator token '{token}' not allowed inside a sequence");
                        idx++;
                        break; 

                    case BlastScriptToken.DotComma:
                        if (idx == idx_max)
                        {
                            // allow ; on end of of range 
                            return n_sequence;
                        }
                        else
                        {
                            data.LogError($"parser.parse_sequence: seperator token '{token}' not allowed inside a sequence");
                            return null;
                        }

                    // compounds embedded in parameters -> statements resulting in values : 'sequences'
                    case BlastScriptToken.OpenParenthesis:
                        if (find_next(data, tokens, BlastScriptToken.CloseParenthesis, idx + 1, idx_max, out idx_end, true, false))
                        {
                            node n_compound = parse_sequence(data, tokens, ref idx, idx_end);
                            if (n_compound != null)
                            {
                                n_sequence.SetChild(n_compound);
                                idx++;
                            }
                            else
                            {
                                data.LogError("parser.parse_sequence: error parsing nested sequence");
                                return null;
                            }
                        }
                        else
                        {
                            data.LogError("parser.parse_sequence: malformed nested sequence, failed to located closing parenthesis");
                            return null;
                        }
                        break;

                    // operations
                    case BlastScriptToken.Add:
                    case BlastScriptToken.Divide:
                    case BlastScriptToken.Multiply:
                    case BlastScriptToken.Equals:
#if SUPPORT_TERNARY
                    case BlastScriptToken.Ternary:
                    case BlastScriptToken.TernaryOption:
#endif
                    case BlastScriptToken.SmallerThen:
                    case BlastScriptToken.GreaterThen:
                    case BlastScriptToken.SmallerThenEquals:
                    case BlastScriptToken.GreaterThenEquals:
                    case BlastScriptToken.NotEquals:
                    case BlastScriptToken.And:
                    case BlastScriptToken.Or:
                    case BlastScriptToken.Xor:
                    case BlastScriptToken.Not:
                        n_sequence.CreateChild(nodetype.operation, token, tokens[idx].Item2);
                        idx++;
                        break;

                    case BlastScriptToken.Substract:
                        // if its the first, then its NOT an operation
                        if (prev_token == BlastScriptToken.Nop
                            ||
                            // if the previous was a seperator 
                            prev_token == BlastScriptToken.Comma
                            || 
                            if_sure_its_vector_define)
                        {
                            // no an operation, but minus sign 
                            minus = true;
                            idx++;
                        }
                        else
                        {
                            // handle as a mathamatical operation 
                            n_sequence.CreateChild(nodetype.operation, token, tokens[idx].Item2);
                            idx++;
                        }
                        break; 

                    case BlastScriptToken.CloseParenthesis:
                    case BlastScriptToken.Indexer:
                    case BlastScriptToken.IndexOpen:
                    case BlastScriptToken.IndexClose:
                    case BlastScriptToken.If:
                    case BlastScriptToken.Then:
                    case BlastScriptToken.Else:
                    case BlastScriptToken.While:
                    case BlastScriptToken.Switch:
                    case BlastScriptToken.Case:
                    case BlastScriptToken.Default:
                    default:
                        data.LogError($"parse.parse_sequence: found unexpected token '{token}' while parsing sequence");
                        return null;
                }

                prev_token = token;
            }

            return n_sequence;
        }

        /// <summary>
        /// grow a chain of indices after some other node/token
        /// </summary>
        /// <param name="data"></param>
        /// <param name="chain_root">node to index</param>
        /// <param name="idx">token index to start reading chain from</param>
        /// <param name="idx_max">max index to grow into</param>
        /// <returns>chain root node with chain as children, or null on failure</returns>
        node grow_index_chain(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokens, node chain_root, ref int idx, in int idx_max)
        {
            // STEP 3> grow identifier indexing chain (functions can be indexed)
            node n_id = chain_root;
            bool has_open_indexer = false;
            bool last_is_dot_indexer = false;

            while (idx <= idx_max && idx < tokens.Count)
            {
                switch (tokens[idx].Item1)
                {
                    case BlastScriptToken.Indexer:
                        if (last_is_dot_indexer || has_open_indexer)
                        {
                            data.LogError("parser.grow_index_chain: double indexer found");
                            return null;
                        }
                        n_id = n_id.AppendIndexer(BlastScriptToken.Indexer, ".");
                        last_is_dot_indexer = true;
                        idx++;
                        continue;

                    case BlastScriptToken.IndexOpen:
                        if (last_is_dot_indexer || has_open_indexer)
                        {
                            data.LogError("parser.grow_index_chain: double indexer found");
                            return null;
                        }
                        n_id = n_id.AppendIndexer(BlastScriptToken.IndexOpen, "[");
                        last_is_dot_indexer = false;
                        has_open_indexer = true;
                        idx++;
                        continue;

                    case BlastScriptToken.IndexClose:
                        if (!has_open_indexer)
                        {
                            data.LogError("Blast.Parser.grow_index_chain: indexer mismatch, found closing index without opening [");
                            return null;
                        }
                        n_id = n_id.parent.AppendIndexer(BlastScriptToken.IndexClose, "]");
                        has_open_indexer = false;
                        last_is_dot_indexer = false;
                        idx++;
                        continue;


                    case BlastScriptToken.Identifier:
                        if (has_open_indexer || last_is_dot_indexer)
                        {
                            data.LogToDo("parser.grow_index_chain - parse sequence on open indexers");
                            //
                            // ! possible sequence in open indexer 
                            // 
                            n_id = n_id.parent.AppendIndexer(BlastScriptToken.Identifier, tokens[idx].Item2);
                            idx++;
                            last_is_dot_indexer = false;
                        }
                        else
                        {
                            // not part of this identifier, done growing 
                            return chain_root;
                        }
                        break;

                    default:
                        if (last_is_dot_indexer)
                        {
                            data.LogError("parser.grow_index_chain: index identifier mismatch");
                            return null;
                        }

                        if (has_open_indexer)
                        {
                            data.LogToDo("parser.grow_index_chain -  sequence on open indexers");
                            //
                            // ! possible sequence in open indexer 
                            // 
                            n_id.AppendIndexer(tokens[idx].Item1, tokens[idx].Item2);
                            idx++;
                            last_is_dot_indexer = false;
                        }
                        else
                        {
                            // done growing identifier 
                            return chain_root;
                        }
                        break;
                }
            }

            // nothing after initial part within range 
            return chain_root;
        }

        
        /// <summary>
        /// check if the node is an assignment 
        /// </summary>
        bool check_if_assignment_node(IBlastCompilationData data, node ast_node, bool inlined)
        {
            // check if this is an assignment 
            // [identifier][indexchain] = [sequence] ;/nop
            if (ast_node.children.Count > 1)
            {
                //
                // Standard assignment form:  a = b .... 
                //
                // first child must be a non constant parameter 
                // the second must be an assignment operator 
                if (ast_node.children[0].type == nodetype.parameter
                    &&
                    ast_node.children[1].type == nodetype.operation && ast_node.children[1].token == BlastScriptToken.Equals)
                {
                    // looks like an assignment, check out the parameter. it must be non constant 
                    node param = ast_node.children[0];

                    // check if the assignment is rooted in an inlinefunction, if so it may not create variables
                    if (inlined)
                    {
                        if (!data.ExistsVariable(param.identifier))
                        {
                            data.LogError($"blast.parser: variable declarations are not allowed inside functions", (int)BlastError.error_inlinefunction_may_not_declare_variables);
                            return false;
                        }
                    }

                    if (param.variable == null)
                    {
                        param.variable = ((CompilationData)data).GetOrCreateVariable(param.identifier);
                    }
                    if (param.variable == null)
                    {
                        // failed to create variable  ?? 
                        data.LogError($"parser: failed to get or create variable '{param.identifier}'");
                    }
                    else
                    {
                        if (param.variable.IsConstant)
                        {
                            // any cdata defined with data is assumed to be constant 
                            // - it can be allowed to be overwritten with compiler options 
                            //   or defines 
                            if (param.variable.DataType != BlastVariableDataType.CData
                               ||
                               (param.variable.DataType != BlastVariableDataType.CData && !data.DefinesOrConfiguresSharedCData))
                            {
                                data.LogError($"Blast.parser: the subject '{param.variable}' of an assignment cannot be constant.");
                            }
                        }
                    }

                    // set node to be assignment type 
                    if (data.IsOK)
                    {
                        ast_node.type = nodetype.assignment;
                        // set the identifier of the assignee
                        ast_node.identifier = param.identifier;
                        ast_node.variable = param.variable;
                        // take indexers from the first child / assignee if any 
                        if (ast_node.FirstChild.HasIndexers)
                        {
                            ast_node.indexers = ast_node.FirstChild.indexers;
                        }
                        // remove the first 2 child nodes, these are the id = and can now be omitted
                        ast_node.children.RemoveRange(0, 2);
                    }
                }
                else
                //
                // check for the most simple patterns of increment/decrement
                //
                if (ast_node.ChildCount == 3)
                {
                    // 
                    // Incrementor, after: a++; 
                    //
                    if (ast_node.ChildCount == 3 &&
                        ast_node.children[1].token == BlastScriptToken.Add &&
                        ast_node.children[2].token == BlastScriptToken.Add)
                    {
                        node id = ast_node.children[0];
                        ast_node.children.Clear();
                        //ast_node.SetChild(id);
                        ast_node.CreateChild(nodetype.increment, BlastScriptToken.Increment, id.identifier);
                    }
                    else
                    // 
                    // Incrementor, after: a--; 
                    //
                    if (ast_node.ChildCount == 3 &&
                        ast_node.children[1].token == BlastScriptToken.Substract &&
                        ast_node.children[2].token == BlastScriptToken.Substract)
                    {
                        node id = ast_node.children[0];
                        ast_node.children.Clear();
                        //ast_node.SetChild(id);
                        ast_node.CreateChild(nodetype.decrement, BlastScriptToken.Decrement, id.identifier);
                    }
                }
                //else
                //{

                //}

                //
                // INCREMENTAL assignment: +=    -=
                //


            }

            return data.IsOK;
        }

        /// <summary>
        /// parse a statement list
        /// - depending on defines this may execute multithreaded 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tokens"></param>
        /// <param name="parent">the parent node</param>
        /// <param name="idx_start">starting index into tokens </param>
        /// <param name="idx_max">max index in tokens to scan into</param>
        /// <returns>exitcode - blasterror</returns>
        int parse_statements(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokens, node parent, int idx_start, int idx_max)
        {
            int idx_token = idx_start, idx_end = -1;

            List<int4> idx_statements = new List<int4>();

            // first scan&extract all statement indices from the token array 

            while (idx_token <= idx_max)
            {
                if (find_next_statement(data, tokens, ref idx_token, idx_max, out idx_start, out idx_end))
                {
                    idx_statements.Add(new int4(idx_start, idx_end, idx_statements.Count, 0));

                    if (idx_token == idx_max) break; 
                }
                else
                {
                    break;
                }
            }

            if (idx_statements.Count == 0 && idx_max > idx_start)
            {
                data.LogError("parser.parse_statements: failed to find complete statements in token list");
                return (int)BlastError.error;
            }

            node[] nodes = new node[idx_statements.Count];

            // if parallel enabled and running from root node then run multithreaded
            if (parent == data.AST && data.CompilerOptions.ParallelCompilation)
            {
                Parallel.ForEach(idx_statements, idx =>
                {
                    // parse the statement from the token array
                    node ast_node = parse_statement(data, tokens, idx[0], idx[1]);
                    if (ast_node != null)
                    {
                        nodes[idx[2]] = ast_node;
                       
                        // check if the parsed statement is an assignment 
                        check_if_assignment_node(data, ast_node, IsParsingInlineFunction(data, tokens));
                    }
                });
            }
            else
            {
                // single threaded debug friendly
                foreach (int4 idx in idx_statements)
                {
                    // parse the statement from the token array
                    node ast_node = parse_statement(data, tokens, idx[0], idx[1]);
                    if (ast_node != null)
                    {
                        nodes[idx[2]] = ast_node;

                        // check if the parsed statement is an assignment 
                        check_if_assignment_node(data, ast_node, IsParsingInlineFunction(data, tokens));

                        // get all function nodes 
                        // node[] functions = ast_node.GetChildren(nodetype.function);
                    }
                }
            }

            // check each is set, report errors otherwise
            foreach (int4 idx in idx_statements)
            {
                node ast_node = nodes[idx[2]];
                if (ast_node == null)
                {
                    data.LogError($"parser.parse_statements: failed to parse statement from token {idx[0]} to {idx[1]}");
                }
                
                // make it part of the ast tree
                parent.SetChild(ast_node);
            }

            if (data.IsOK)
            {
                return (int)BlastError.success;
            }
            else
            {
                return (int)BlastError.error;
            }
        }


        /// <summary>
        /// check if the stream we are parsing is an inlined function 
        /// </summary>
        bool IsParsingInlineFunction(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> tokens)
        {
            if (tokens == data.Tokens) return false;
            if (tokens.Count == 0) return false;
            return tokens[0].Item1 == BlastScriptToken.Function; 
        }


        BlastError IdentifyFunctionRoots(IBlastCompilationData data, List<List<Tuple<BlastScriptToken, string>>> roots)
        {
            Assert.IsNotNull(roots);
            foreach (var ftokens in roots)
            {

#if DEVELOPMENT_BUILD || TRACE
                Assert.IsNotNull(ftokens);
                Assert.IsTrue(ftokens.Count > 3);
                Assert.IsTrue(ftokens[0].Item1 == BlastScriptToken.Function);

                StringBuilder sbc = StringBuilderCache.Acquire();
                sbc.Append("Found Function: ");
                foreach (var t in ftokens)
                {
                    sbc.Append("  " + t.Item1.ToString());
                }
                sbc.AppendLine();

                data.LogTrace(StringBuilderCache.GetStringAndRelease(ref sbc));
#endif

                // the first node will be 'function'
                node n = new node(nodetype.inline_function, BlastScriptToken.Function).SkipCompilation();

                // get identifier|name => it should be unique at this point 
                if (ftokens[1].Item1 != BlastScriptToken.Identifier)
                {
                    data.LogError("blast.parser.parseinlinefunction: failed to parse functionname");
                    return BlastError.error_inlinefunction_declaration_syntax;
                }

                n.identifier = ftokens[1].Item2.ToLower().Trim();

                // check if another function exists at this point with this name, all other identifiers get
                // mapped out in the next stage 
                if (data.AST.HasInlineFunction(n.identifier))
                {
                    data.LogError($"blast.parser.parseinlinefunction: a function with the name '{n.identifier}' is already defined elsewhere");
                    return BlastError.error_inlinefunction_already_exists;
                }
                              
                unsafe
                {
                    if (data.Blast.Data->TryGetFunctionByName(n.identifier, out BlastScriptFunction existing))
                    {
                        data.LogError($"blast.parser.parseinlinefunction: a function with the name '{n.identifier}' is already defined in the script api with id: {existing.FunctionId}");
                        return BlastError.error_inlinefunction_already_exists;
                    }
                }

                // get the parameters 
                // - declaration is very rigid so we should expect an opening ( at the next token 
                int idx = 2;

                if (ftokens[idx].Item1 != BlastScriptToken.OpenParenthesis)
                {
                    // syntax error, although it is checked earlier, maybe this function will have different
                    // callstacks in the future 
                    return BlastError.error_inlinefunction_parameter_list_syntax;
                }


                node parameters = parse_sequence(data, ftokens, ref idx, ftokens.Count - 1, true);

                // put each in dependancies of the function 
                if (parameters.ChildCount > 0)
                {
                    for (int i = 0; i < parameters.ChildCount; i++)
                        n.AppendDependency(parameters.children[i]);
                }
                else
                {
                    // must be empty compound then 
                    if (parameters.type != nodetype.compound)
                    {
                        return BlastError.error_inlinefunction_parameter_list_syntax;
                    }
                }

                idx++;
                if (ftokens[idx].Item1 != BlastScriptToken.OpenParenthesis)
                {
                    return BlastError.error_inlinefunction_body_syntax;
                }

                // cut out parameter section leaving only the body 
                ftokens.RemoveRange(2, idx - 2);

                // add function to the ast root, insert it at the root directly after the other inlines 
                data.AST.children.Insert(data.AST.CountChildren(nodetype.inline_function), n);

                //determine parametercount
                int c = n.DependencyCount; 

                // add function definition 
                BlastScriptInlineFunction f = new BlastScriptInlineFunction()
                {
                    Name = n.identifier,
                    Node = n,
                    ParameterCount = c
                };

                ((CompilationData)data).AddInlinedFunction(f);
            }

            return BlastError.success;
        }



        /// <summary>
        /// parse a single inlined function from the tokens
        /// </summary>
        /// <param name="data">compilerdata</param>
        /// <param name="ftokens">the tokens defining the function</param>
        /// <returns>success if all went ok, errorcodes otherwise</returns>
        BlastError ParseInlineFunction(IBlastCompilationData data, List<Tuple<BlastScriptToken, string>> ftokens)
        {
            // get identifier|name
            if(ftokens[1].Item1 != BlastScriptToken.Identifier)
            {
                data.LogError("blast.parser.parseinlinefunction: failed to parse functionname");
                return BlastError.error_inlinefunction_declaration_syntax;
            }


            BlastScriptInlineFunction function; 
            if(!data.TryGetInlinedFunction(ftokens[1].Item2, out function))
            {
                data.LogError($"blast.parser.parseinlinefunction: failed to parse body statement, could not locate identified inline function node with functionname: {ftokens[1].Item2}");
                return BlastError.error_inlinefunction_declaration_syntax; 
            }
            node n = function.Node;

            Assert.IsNotNull(n); 

            // read body statement list
            int idx = 3; 

            BlastError res = (BlastError)parse_statements(data, ftokens, n, idx, ftokens.Count - 1); 
            if(res != 0)
            {
                data.LogError("blast.parser.parseinlinefunction: failed to parse body statements");
                return res;
            }

            // function parsed correctly, register it so we can recognize calls to it 

            return BlastError.success; 
        }


        /// <summary>
        /// scan for functions and parse them into seperate nodes 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        BlastError FindAndExtractFunctionRoots(IBlastCompilationData data, out List<List<Tuple<BlastScriptToken, string>>> roots)
        {
            int i = 0;
            roots = null; 

            while (i < data.Tokens.Count - 1)
            {
                BlastScriptToken token = data.Tokens[i].Item1;
                if (token == BlastScriptToken.Function)
                {
                    // we need to find the last token of the function 
                    // 
                    // a function MUST have a parameter list () even if there are no parameters 
                    // a function can only return 1 value
                    //

                    if (data.Tokens[i + 1].Item1 != BlastScriptToken.Identifier)
                    {                   
                        // the tokenizer should have picked up the function name as an identifier 
                        // = syntax error 
                        return BlastError.error_inlinefunction_declaration_syntax; 
                    }


                    // the next token MUST be open 
                    if (data.Tokens[i + 2].Item1 != BlastScriptToken.OpenParenthesis)
                    {
                        // syntax error 
                        return BlastError.error_inlinefunction_parameter_list_syntax;
                    }

                    // store locations: 
                    int i_start_paramlist = i + 1;
                    int i_start_body = 0;
                    int i_end_body = 0;

                    if (!find_next(data, data.Tokens, BlastScriptToken.OpenParenthesis, i + 3, data.Tokens.Count - 1, out i_start_body))
                    {
                        return BlastError.error_inlinefunction_body_syntax;
                    }

                    if (!find_next_skip_compound(data, data.Tokens, BlastScriptToken.CloseParenthesis, i_start_body+1, data.Tokens.Count - 1, out i_end_body))
                    {
                        return BlastError.error_inlinefunction_body_syntax;
                    }

                    // read past any trailing ;'s
                    int j = i_end_body; 
                    while (data.Tokens[j].Item1 == BlastScriptToken.DotComma && j < data.Tokens.Count - 1) j++;

                    // should have our function between i and i_end_body 
                    List<Tuple<BlastScriptToken, string>> ftokens = data.Tokens.GetRange(i, i_end_body - i + 2);

                    // remove that range including any trailing ;'s from the rest of the tokens
                    data.Tokens.RemoveRange(i, j - i + 2);


                    if (roots == null)
                    {
                        roots = new List<List<Tuple<BlastScriptToken, string>>>();
                    }
                    roots.Add(ftokens); 

                }
                else
                {
                    i++;
                }
            }

            return BlastError.success; 
        }

        BlastError ParseFunctionRoots(IBlastCompilationData data, List<List<Tuple<BlastScriptToken, string>>> roots)
        {
            Assert.IsNotNull(roots); 
            foreach(var root in roots)
            {
                // parse inline function node and insert it into the ast
                BlastError res = ParseInlineFunction(data, root);
                if (res != BlastError.success)
                {
#if DEVELOPMENT_BUILD || TRACE
                    data.LogError($"blast.parser.FindAndExtractFunctions: failed to parse function, error {res}");
#endif
                    return res;
                }
            }

            return BlastError.success; 
        }



        /// <summary>
        /// execute the parser stage:
        /// - parse tokens into node tree
        /// - map identifiers (indexers, functions, constants) 
        /// </summary>
        /// <param name="data">compilation data</param>
        /// <returns>exitcode, 0 == success</returns>
        public int Execute(IBlastCompilationData data)
        {
            // find all roots for parsing:
            // 
            // - functions 
            // - body 
            //

            List<List<Tuple<BlastScriptToken, string>>> roots;
            BlastError res = FindAndExtractFunctionRoots(data, out roots);
            if (res != BlastError.success)
            {
#if DEVELOPMENT_BUILD || TRACE 
                data.LogError($"blast.parser: failed to locate and extract function scopes, error {res}");
#endif
                return (int)res;
            }

            // scan the roots and add identifiers for the inline functionnames (and nothing else) 
            if (roots != null && roots.Count > 0)
            {
                res = IdentifyFunctionRoots(data, roots);
                if (res != BlastError.success)
                {
#if DEVELOPMENT_BUILD || TRACE
                    data.LogError($"blast.parser: failed to locate and extract function scopes, error {res}");
#endif
                    return (int)res;
                }
            }


            // parse body tokens 
            res = (BlastError)parse_statements(data, data.Tokens, data.AST, 0, data.Tokens.Count - 1);
            if (res != BlastError.success)
            {
#if DEVELOPMENT_BUILD || TRACE 
                data.LogError($"blast.parser: failed to locate and extract function scopes, error {res}");
#endif
                return (int)res;
            }



            // parse function trees 
            if (roots != null && roots.Count > 0)
            {
                res = ParseFunctionRoots(data, roots);
                if (res != BlastError.success)
                {
#if DEVELOPMENT_BUILD || TRACE
                    data.LogError($"blast.parser: failed to locate and extract function scopes, error {res}");
#endif
                    return (int)res;
                }
            }

            return (int)BlastError.success; 
        }
    }

}