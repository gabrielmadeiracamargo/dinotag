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

#if STANDALONE_VSBUILD
    using NSS.Blast.Standalone;
    using UnityEngine.Assertions;
#else
    using UnityEngine;
    using UnityEngine.Assertions;
#endif


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace NSS.Blast.Compiler.Stage
{
    /// <summary>
    /// Bytecode Optimizer
    /// - optimizes bytecode based on pattern recognition
    /// </summary>
    public class BlastBytecodeOptimizer : IBlastCompilerStage
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.Version'
        public Version Version => new Version(0, 1, 0);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.Version'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.StageType'
        public BlastCompilerStageType StageType => BlastCompilerStageType.BytecodeOptimizer;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.StageType'


        static bool ReplaceCommonPatterns(CompilationData data, IMByteCodeList code)
        {
            // replace common sequences:       /// maybe do this on tokenizer???? todo  better probably
            // - + => -
            // + - => -
            // + + => +
            // - - => +
            int i = 0;
            while (i < code.Count - 1)
            {
                // +-/-+ becomes - 
                if ((code[i].op == blast_operation.add && code[i + 1].op == blast_operation.substract)
                    ||
                   (code[i].op == blast_operation.substract && code[i + 1].op == blast_operation.add)) // wierd order but replace it anyway
                {
                    // keep the '-'
                    if (!data.code.RemoveAtAndMoveJumpLabels(data, i)) return false;
                    continue;
                }

                // ++ => +
                if (code[i].op == blast_operation.add && code[i + 1].op == blast_operation.add)
                {
                    if (!data.code.RemoveAtAndMoveJumpLabels(data, i)) return false;
                    continue;
                }

                // -- => +
                if (code[i].op == blast_operation.substract && code[i + 1].op == blast_operation.substract)
                {
                    if (!data.code.RemoveAtAndMoveJumpLabels(data, i)) return false;
                    continue;
                }

                i++;
            }

            return true;
        }

        #region bytecode optimizer
        #region optimizer patterns 

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern'
        public class optimizer_pattern
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern'
        {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.name'
            public string name; 
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.name'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.pattern'
            public byte[] pattern;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.pattern'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.replace'
            public byte[] replace;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.replace'

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.ToString()'
            public override string ToString()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.ToString()'
            {
                if (pattern != null && replace != null)
                {
                    return $"{name}, size: {pattern.Length}->{replace.Length}, min_match_next: {min_match_for_next}, next = {(next == null ? "": next.name)}";
                }
                else
                {
                    return base.ToString(); 
                }
            }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.min_match_for_next'
            public int min_match_for_next;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.min_match_for_next'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.next'
            public optimizer_pattern next;   // the next pattern to check if matching minimal count, inverting this might be more efficient 
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.next'

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.has_repeat_in_pattern()'
            public bool has_repeat_in_pattern()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.has_repeat_in_pattern()'
            {
                for (int i = 0; i < pattern.Length; i++)
                    if (pattern[i] == opt_repeat)
                        return true;
                return false;
            }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.repeat_in_pattern_length()'
            public int repeat_in_pattern_length()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.repeat_in_pattern_length()'
            {
                for (int i = 0; i < pattern.Length; i++)
                    if (pattern[i] == opt_repeat)
                        return i;
                return -1;
            }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.has_repeat_in_replace()'
            public bool has_repeat_in_replace()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.has_repeat_in_replace()'
            {
                for (int i = 0; i < replace.Length; i++)
                    if (replace[i] == opt_repeat)
                        return true;
                return false;
            }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.validate()'
            public bool validate()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.validate()'
            {
                // both either repeat or they dont
                if (has_repeat_in_pattern() != has_repeat_in_replace()) return false;

                // todo perform validations 

                return true;
            }


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.replace_match(CompilationData, IMByteCodeList, ref int)'
            public unsafe void replace_match(CompilationData cdata, IMByteCodeList code, ref int i)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastBytecodeOptimizer.optimizer_pattern.replace_match(CompilationData, IMByteCodeList, ref int)'
            {
                // determine if replacing a variable function length 
                var function = cdata.Blast.Data->GetFunction((blast_operation)replace[0]);
                bool variable_param_length = function.IsNotValid ? false : function.MinParameterCount != function.MaxParameterCount;

                byte var_param_count = 0;
                byte vector_size = 1;
                if (variable_param_length)
                {
                    // count params in replace pattern 
                    foreach (byte b in replace)
                    {
                        if (b >= BlastCompiler.opt_value)
                        {
                            var_param_count++;
                            // determine vectorsize of result from parameters 
                            byte write = (byte)code[i + (b - BlastCompiler.opt_value)].code;
                            if (write >= BlastCompiler.opt_ident)
                            {
                                BlastVariable v = cdata.Variables[write - BlastCompiler.opt_ident];
                                // crash hard if v is null at this stage 
                                vector_size = (byte)math.max(vector_size, v.IsVector ? v.VectorSize : 1);
                            }
                        }
                    }

                    if (var_param_count == 0)
                    {
                        cdata.LogError($"optimizer.replace_match: replace pattern variable count {var_param_count} smaller then min variable count {function.MinParameterCount} for function {function.GetFunctionName()}");
                        return;
                    }
                }

                // create array of operations to replace the matched pattern with
                List<blast_operation> replacement = new List<blast_operation>(); 

                // then from the start of the section to replace 
                // - keep replacing operations and copy variables for all j in pattern
                // - when we hit the repeat instruction we have to move back into the pattern 
                int j = 0;
                while (j < replace.Length)
                {
                    byte b = replace[j];
                    if (j == 1 && variable_param_length)
                    {
                        // encode vector sioze in count 
                        // - count never higher then 63
                        // - vectorsize max 15
                        // if 255 then 2 bytes for supporting larger vectors and count?
                        byte data = (byte)((var_param_count << 2) | (byte)vector_size & 0b11);
                        replacement.Add((blast_operation)data);

                        // the byte in pattern must match with size calculated 
                        if (b != var_param_count)
                        {
                            cdata.LogError($"optimizer.replace_match: replace pattern variable count {b} smaller then variable count {var_param_count} calculated from statement");
                            return;
                        }
                    }
                    else
                    {
                        if (b >= BlastCompiler.opt_value)
                        {
                            // direct constant or var
                            b = (byte)code[i + (b - BlastCompiler.opt_value)].code;
                            replacement.Add((blast_operation)b);
                        }
                        else
                        if (b >= BlastCompiler.opt_ident)
                        {
                            // replace parameter by indexing it via b
                            replacement.Add(code[i + (b - BlastCompiler.opt_value)].op);
                        }
                        else
                        {
                            // replace operation 
                            replacement.Add((blast_operation)b);
                        }
                    }
                    j++;
                }

                // we matched at i a pattern of length pattern.length
                // replace from i to i+pattern.length, insert as needed
                int insert_from = i;
                for(j = 0; j < replacement.Count;j++)
                {
                    if (j >= pattern.Length)
                    {
                        // insert at position as we are adding at indices that were not part of the pattern
                        code.Insert(insert_from + j, (byte)replacement[j]);
                    }
                    else
                    {
                        // overwrite position as it was part of matched pattern 
                        code[insert_from + j].UpdateOpCode((byte)replacement[j]);
                    }
                }
                // remove elements that are not used by this command anymore after replacing
                // the matched pattern with a new smaller replacement
                int n_remove = pattern.Length - j; 
                while(n_remove > 0)
                {
                    code.RemoveAtAndMoveJumpLabels(cdata, insert_from + j);
                    n_remove--; 
                }

                // advance optimizer index to the position after the replace pattern 
                i = i + replacement.Count;
            }

            /// <summary>
            /// match pattern at i
            /// </summary>
            /// <param name="result"></param>
            /// <param name="ops"></param>
            /// <param name="i"></param>
            /// <param name="matched_pattern"></param>
            /// <returns>true on match</returns>
            public bool match(CompilationData result, IMByteCodeList ops, int i, ref optimizer_pattern matched_pattern)
            {
                matched_pattern = this;

                if (i < 0) return false;

                if (min_match_for_next == 0 && next == null && i > ops.Count - matched_pattern.pattern.Length) return false;

                int j = 0;
                int c = 0;
                int n = math.min(matched_pattern.pattern.Length, ops.Count - i - 1);
                int c_matched = 0; 
                int vector_size = 0;

                for (; j < n;)
                {
                    byte b = matched_pattern.pattern[j];

                    if (b >= BlastCompiler.opt_value)        
                    {
                        // matched a value or an id in data 
                        byte b_data = (byte)ops[j + i].code;

                        // BUGFIX: MATCHES VALUE ON ASSIGNEE in test 'Minus 2.bs'
                        if (j + i > 1 && ops[j+i-1].code == (byte)blast_operation.assign)
                        {
                            return false;
                        }

                        if (b_data == (byte)blast_operation.ex_op)
                        {
                            // just dont 
                            return false; 
                        }

                        // var or constant if matching a non-param then false 
                        if (b_data < BlastCompiler.opt_value)
                        {
                            if (matched_pattern.next != null && c >= matched_pattern.min_match_for_next)
                            {
                                return matched_pattern.next.match(result, ops, i, ref matched_pattern);
                            }
                            return false;
                        }

                        // v must exist at this point
                        if (b_data >= BlastCompiler.opt_ident)
                        {
                            // if not a constant then update the variable that must exist 
                            BlastVariable v = null; 
                            v = result.GetVariableFromOffset((byte)(b_data - BlastCompiler.opt_ident));

                            if(v == null)
                            {
#if DEVELOPMENT_BUILD || TRACE
                                Debug.Log(result.GetHumanReadableCode());
                                Debug.Log(result.GetHumanReadableBytes());
#endif
                                result.LogError($"Optimizer: pattern: {matched_pattern.name}, could not find variable based on offset: {b_data - BlastCompiler.opt_ident}, this could be due to a packaging failure");
                            }

                       //     if(v == null)
                       //     {
                       //         // var by index ... could be BOEM 
                       //         int var_index = b_data - BlastCompiler.opt_ident;
                       //         if (var_index >= 0 && var_index < result.VariableCount)
                       //         {
                       //             v = result.Variables[var_index];
                       //         }
                       //         else
                       //         {
                       //             result.LogError($"Optimizer: pattern: {matched_pattern.name}, could not find variable based on offset/index: {b_data - BlastCompiler.opt_ident}, this could be due to a packaging failure");
                       //             return false;
                       //         }
                       //     }


                            if (vector_size == 0)
                            {
                                // set 
                                vector_size = math.max(1, v.VectorSize);
                            }
                            else
                            {
                                // vectorsize was set, check it must be the same
                                int new_vector_size = math.max(1, v.VectorSize);
                                if (vector_size != new_vector_size)
                                {
                                    return false;
                                }
                            }
                        }

                        c_matched++; 
                    }
                    else
                    {
                        // operation must match
                        if (ops[j + i].code != b)
                        {
                            if (matched_pattern.next != null && c >= matched_pattern.min_match_for_next)
                            {
                                return matched_pattern.next.match(result, ops, i, ref matched_pattern);
                            }
                            return false;
                        }
                    }

                    c++;
                    j++;
                }

                return c_matched == matched_pattern.pattern.Length;
               // return j == matched_pattern.pattern.Length;
            }
        }

    
#region patterns 

        // pattern matching works very stupid but with the right patterns is very powerfull. 
        const byte opt_repeat = 255; // repeat pattern into replacer until no match, then continue replace after repeat
        const byte opt_rep_all = 0;

        static optimizer_pattern fma1 = new optimizer_pattern()
        {
            name = "fma1", 
            // var + { var * var }
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.add, (byte)blast_operation.begin, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.end },
            replace = new byte[] { (byte)blast_operation.fma, BlastCompiler.opt_value + 3, BlastCompiler.opt_value + 5, BlastCompiler.opt_value + 0 }
        };
        static optimizer_pattern fma2 = new optimizer_pattern()
        {
            name = "fma2",
            // { var * var } + var
            pattern = new byte[] { (byte)blast_operation.begin, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.end, (byte)blast_operation.add, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.fma, BlastCompiler.opt_value + 1, BlastCompiler.opt_value + 3, BlastCompiler.opt_value + 6 }
        };
        static optimizer_pattern nested_mula_4 = new optimizer_pattern()
        {
            name = "nested_mula4",
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, (byte)blast_operation.begin, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.end, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 4, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 3, BlastCompiler.opt_value + 5, BlastCompiler.opt_value + 8 }
        };
        static optimizer_pattern mula_3 = new optimizer_pattern()
        {
            name = "mula3",
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 3, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4 }
        };
        static optimizer_pattern mula_4 = new optimizer_pattern()
        {
            name = "mula4",
            min_match_for_next = 1,
            next = mula_3,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 4, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6 }
        };
        static optimizer_pattern mula_5 = new optimizer_pattern()
        {
            name = "mula5",
            min_match_for_next = 1,
            next = mula_4,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 5, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8 }
        };
        static optimizer_pattern mula_6 = new optimizer_pattern()
        {
            name = "mula6",
            min_match_for_next = 1,
            next = mula_5,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 6, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10 }
        };
        static optimizer_pattern mula_7 = new optimizer_pattern()
        {
            name = "mula7",
            min_match_for_next = 1,
            next = mula_6,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 7, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10, BlastCompiler.opt_value + 12 }
        };
        static optimizer_pattern mula_8 = new optimizer_pattern()
        {
            name = "mula8",
            min_match_for_next = 1,
            next = mula_7,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 8, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10, BlastCompiler.opt_value + 12, BlastCompiler.opt_value + 14 }
        };
        static optimizer_pattern mula_9 = new optimizer_pattern()
        {
            name = "mula9",
            min_match_for_next = 1,
            next = mula_8,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 9, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10, BlastCompiler.opt_value + 12, BlastCompiler.opt_value + 14, BlastCompiler.opt_value + 16 }
        };
        static optimizer_pattern mula_10 = new optimizer_pattern()
        {
            name = "mula10",
            min_match_for_next = 1,
            next = mula_9,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 10, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10, BlastCompiler.opt_value + 12, BlastCompiler.opt_value + 14, BlastCompiler.opt_value + 16, BlastCompiler.opt_value + 18 }
        };
        static optimizer_pattern mula_11 = new optimizer_pattern()
        {
            name = "mula11",
            min_match_for_next = 1,
            next = mula_10,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 11, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10, BlastCompiler.opt_value + 12, BlastCompiler.opt_value + 14, BlastCompiler.opt_value + 16, BlastCompiler.opt_value + 18, BlastCompiler.opt_value + 20 }
        };
        static optimizer_pattern mula_12 = new optimizer_pattern()
        {
            name = "mula12",
            min_match_for_next = 5,
            next = mula_11,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 12, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10, BlastCompiler.opt_value + 12, BlastCompiler.opt_value + 14, BlastCompiler.opt_value + 16, BlastCompiler.opt_value + 18, BlastCompiler.opt_value + 20, BlastCompiler.opt_value + 22 }
        };
        static optimizer_pattern mula_13 = new optimizer_pattern()
        {
            name = "mula13",
            min_match_for_next = 5,
            next = mula_12,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 12, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10, BlastCompiler.opt_value + 12, BlastCompiler.opt_value + 14, BlastCompiler.opt_value + 16, BlastCompiler.opt_value + 18, BlastCompiler.opt_value + 20, BlastCompiler.opt_value + 22, BlastCompiler.opt_value + 24 }
        };
        static optimizer_pattern mula_14 = new optimizer_pattern()
        {
            name = "mula14",
            min_match_for_next = 5,
            next = mula_13,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 12, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10, BlastCompiler.opt_value + 12, BlastCompiler.opt_value + 14, BlastCompiler.opt_value + 16, BlastCompiler.opt_value + 18, BlastCompiler.opt_value + 20, BlastCompiler.opt_value + 22, BlastCompiler.opt_value + 24, BlastCompiler.opt_value + 26 }
        };
        static optimizer_pattern mula_15 = new optimizer_pattern()
        {
            name = "mula15",
            min_match_for_next = 5,
            next = mula_14,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 12, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10, BlastCompiler.opt_value + 12, BlastCompiler.opt_value + 14, BlastCompiler.opt_value + 16, BlastCompiler.opt_value + 18, BlastCompiler.opt_value + 20, BlastCompiler.opt_value + 22, BlastCompiler.opt_value + 24, BlastCompiler.opt_value + 26, BlastCompiler.opt_value + 28 }
        };
        static optimizer_pattern mula_16 = new optimizer_pattern()
        {
            name = "mula16",
            min_match_for_next = 5,
            next = mula_15,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value, (byte)blast_operation.multiply, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.mula, 12, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10, BlastCompiler.opt_value + 12, BlastCompiler.opt_value + 14, BlastCompiler.opt_value + 16, BlastCompiler.opt_value + 18, BlastCompiler.opt_value + 20, BlastCompiler.opt_value + 22, BlastCompiler.opt_value + 24, BlastCompiler.opt_value + 26, BlastCompiler.opt_value + 28, BlastCompiler.opt_value + 30 }
        };
        static optimizer_pattern diva_3 = new optimizer_pattern()
        {
            name = "diva3",
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.divide, BlastCompiler.opt_value, (byte)blast_operation.divide, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.diva, 3, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4 }
        };
        static optimizer_pattern diva_4 = new optimizer_pattern()
        {
            name = "diva4",
            min_match_for_next = 1,
            next = diva_3,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.divide, BlastCompiler.opt_value, (byte)blast_operation.divide, BlastCompiler.opt_value, (byte)blast_operation.divide, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.diva, 4, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6 }
        };
        static optimizer_pattern diva_5 = new optimizer_pattern()
        {
            name = "diva5",
            min_match_for_next = 1,
            next = diva_4,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.divide, BlastCompiler.opt_value, (byte)blast_operation.divide, BlastCompiler.opt_value, (byte)blast_operation.divide, BlastCompiler.opt_value, (byte)blast_operation.divide, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.diva, 5, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8 }
        };
        static optimizer_pattern diva_6 = new optimizer_pattern()
        {
            name = "diva6",
            min_match_for_next = 1,
            next = diva_5,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.divide, BlastCompiler.opt_value, (byte)blast_operation.divide, BlastCompiler.opt_value, (byte)blast_operation.divide, BlastCompiler.opt_value, (byte)blast_operation.divide, BlastCompiler.opt_value, (byte)blast_operation.divide, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.diva, 6, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10 }
        };
        static optimizer_pattern adda_3 = new optimizer_pattern()
        {
            name = "adda3",
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.adda, 3, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4 }
        };
        static optimizer_pattern adda_4 = new optimizer_pattern()
        {
            name = "adda4",
            min_match_for_next = 1,
            next = adda_3,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.adda, 4, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6 }
        };
        static optimizer_pattern adda_5 = new optimizer_pattern()
        {
            name = "adda5",
            min_match_for_next = 1,
            next = adda_4,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.adda, 5, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8 }
        };
        static optimizer_pattern adda_6 = new optimizer_pattern()
        {
            name = "adda6",
            min_match_for_next = 1,
            next = adda_5,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.adda, 6, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10 }
        };
        static optimizer_pattern suba_3 = new optimizer_pattern()
        {
            name = "suba3",
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.substract, BlastCompiler.opt_value, (byte)blast_operation.substract, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.suba, 3, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4 }
        };
        static optimizer_pattern suba_4 = new optimizer_pattern()
        {
            name = "suba4",
            min_match_for_next = 1,
            next = suba_3,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.substract, BlastCompiler.opt_value, (byte)blast_operation.substract, BlastCompiler.opt_value, (byte)blast_operation.substract, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.suba, 4, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6 }
        };
        static optimizer_pattern suba_5 = new optimizer_pattern()
        {
            name = "suba5",
            min_match_for_next = 1,
            next = suba_4,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.substract, BlastCompiler.opt_value, (byte)blast_operation.substract, BlastCompiler.opt_value, (byte)blast_operation.add, BlastCompiler.opt_value, (byte)blast_operation.substract, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.suba, 5, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8 }
        };
        static optimizer_pattern suba_6 = new optimizer_pattern()
        {
            name = "suba6",
            min_match_for_next = 1,
            next = suba_5,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.substract, BlastCompiler.opt_value, (byte)blast_operation.substract, BlastCompiler.opt_value, (byte)blast_operation.substract, BlastCompiler.opt_value, (byte)blast_operation.substract, BlastCompiler.opt_value, (byte)blast_operation.substract, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.suba, 6, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10 }
        };
        static optimizer_pattern all_3 = new optimizer_pattern()
        {
            name = "all3",
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.all, 3, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4 }
        };
        static optimizer_pattern all_4 = new optimizer_pattern()
        {
            name = "all4",
            min_match_for_next = 3,
            next = all_4,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.all, 4, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6 }
        };
        static optimizer_pattern all_5 = new optimizer_pattern()
        {
            name = "all5",
            min_match_for_next = 3,
            next = all_5,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.all, 5, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8 }
        };
        static optimizer_pattern all_6 = new optimizer_pattern()
        {
            name = "all6",
            min_match_for_next = 3,
            next = all_5,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.all, 6, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10 }
        };
        static optimizer_pattern all_7 = new optimizer_pattern()
        {
            name = "all7",
            min_match_for_next = 3,
            next = all_6,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.all, 7, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10, BlastCompiler.opt_value + 12 }
        };
        static optimizer_pattern all_8 = new optimizer_pattern()
        {
            name = "all8",
            min_match_for_next = 3,
            next = all_7,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value, (byte)blast_operation.and, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.all, 8, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10, BlastCompiler.opt_value + 12, BlastCompiler.opt_value + 14 }
        };
        static optimizer_pattern any_3 = new optimizer_pattern()
        {
            name = "any3",
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.any, 3, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4 }
        };
        static optimizer_pattern any_4 = new optimizer_pattern()
        {
            name = "any4",
            min_match_for_next = 3,
            next = any_4,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.any, 4, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6 }
        };
        static optimizer_pattern any_5 = new optimizer_pattern()
        {
            name = "any5",
            min_match_for_next = 3,
            next = any_5,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.any, 5, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8 }
        };
        static optimizer_pattern any_6 = new optimizer_pattern()
        {
            name = "any6",
            min_match_for_next = 3,
            next = any_5,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.any, 6, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10 }
        };
        static optimizer_pattern any_7 = new optimizer_pattern()
        {
            name = "any7",
            min_match_for_next = 3,
            next = any_6,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.any, 7, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10, BlastCompiler.opt_value + 12 }
        };
        static optimizer_pattern any_8 = new optimizer_pattern()
        {
            name = "any8",
            min_match_for_next = 3,
            next = any_7,
            pattern = new byte[] { BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value, (byte)blast_operation.or, BlastCompiler.opt_value },
            replace = new byte[] { (byte)blast_operation.any, 8, BlastCompiler.opt_value + 0, BlastCompiler.opt_value + 2, BlastCompiler.opt_value + 4, BlastCompiler.opt_value + 6, BlastCompiler.opt_value + 8, BlastCompiler.opt_value + 10, BlastCompiler.opt_value + 12, BlastCompiler.opt_value + 14 }
        };

        static optimizer_pattern double_nop_assign = new optimizer_pattern()
        {
            name = "double_nop_assign",
            pattern = new byte[] { 0, 0, (byte)blast_operation.assign },
            replace = new byte[] { 0, (byte)blast_operation.assign } // could remove the other 0 too i think.. needs check changed in interpretor but will lose all 0
        };

        static optimizer_pattern nop_end = new optimizer_pattern()
        {
            name = "nop_end",
            pattern = new byte[] { 0, (byte)blast_operation.end },
            replace = new byte[] { (byte)blast_operation.end }
        };

#endregion

        // validate ternaries       => ensure sel(a,b,c) => math.select

        // replace a | b | c etc. into math.any 
        // replace a & b & c etc. into math.all

        static List<optimizer_pattern> optimizer_patterns = new List<optimizer_pattern>()
        {
            fma1, fma2,
            mula_16, diva_6, adda_6, suba_6,
            nested_mula_4, // analyser removes it everything works correctly
            all_8, any_8,
            
            // we can miss at least one nop, we could add support to the interpretor to lose the second 
            double_nop_assign, 
            
            // drop any nop before an end, it will do the same thing 
            nop_end 
        };

        static byte[] optimizer_skip_table = new byte[]
        {
            (byte)blast_operation.fma, 4,
            (byte)blast_operation.mula, 0,
            (byte)blast_operation.adda, 0,
            (byte)blast_operation.suba, 0,
            (byte)blast_operation.diva, 0
        };

#endregion

        /// <summary>
        /// scan and replace patterns 
        /// </summary>
        /// <param name="data">compiler data</param>
        /// <param name="code">code to optimize</param>
        /// <returns>false on failure, logs errors in compilerdata</returns>
        static unsafe bool OptimizePatterns(CompilationData data, IMByteCodeList code)
        {
            if(data == null || code == null)
            {
                data.LogError("Optimizer called with <null> code or data"); 
                return false;
            }

            // scan and replace patterns that can be optimized 
            int i = 0;
            while (i < code.Count - 1)
            {
                bool matched = false;
                if (code[i].code == (byte)blast_operation.ex_op)
                {
                    if (code[i + 1].code == (byte)extended_blast_operation.call)
                    {
                        i = i + 2 + 4;  // skip instructions and call id
                    }
                    else
                    {
                        i = i + 2; // skip instructions 
                    }
                    continue;
                }
                          
                foreach (optimizer_pattern pattern in optimizer_patterns)
                {
                    optimizer_pattern matched_pattern = null;
                    if (pattern.match(data, code, i, ref matched_pattern))
                    {
                        matched_pattern.replace_match(data, code, ref i);
                        if (!data.IsOK) return false;
                        matched = true;
                        break;
                    }
                    if (i >= code.Count) break;
                }

                if (!matched)
                {
                    // no match was replaced, advance codepointer
                    i++;
                }

                if (i < code.Count)
                {
                    // is still inside code, check if we need to skip part before matching again 
                    blast_operation op = code[i].op;                        

                    // if in skip table
                    for (int skip = 0; skip < optimizer_skip_table.Length / 2; skip++)
                    {
                        if (optimizer_skip_table[skip * 2] == (byte)op)
                        {
                            int iskip = optimizer_skip_table[skip * 2 + 1];
                            if (iskip == 0)
                            {
                                // variable length instruction .. find the next nop 
                                int k = i;
                                while ( code[k + iskip].op != blast_operation.nop && (k + iskip < code.Count)) { iskip++; };
                            }
                            // jump and break
                            i += iskip;
                            break;
                        }
                    }
                }

                // * there is one bit of annoyance to consider:
                // * we could match parts of different commands if we just check every code index
                // * example:   fma 1 2 3 + 4 + 5 + 6
                // * a rule to replace [var + var + var + var] would invalidate the fma instruction
                // * so we use the skip table to skip over parameters of the operation before 
                // all + can be replaced with - var and also matched
                // all divisions on constants can be replaced (with reference counting we could even re-use the constant)
            }

            // remove any extra nop's trailing
/*            while (code.Count > 2 
                && code[code.Count - 1].op == script_op.nop 
                && code[code.Count - 2].op == script_op.nop)
            {
                // we should not do this.. 

                // remove 1 before last allowing jumplabels to all move to that last nop if needed
                if(!code.RemoveAtAndMoveJumpLabels(data, code.Count - 2))
                {
                    data.LogError("ByteCodeOptimizer: failure removing nop duplicate"); 
                    return false; 
                }
            }*/ 

            // write back modified code
            /*for (i = 0; i < ops.Count; i++)
            {
                data.Executable.code[i] = (byte)ops[i];
            }
            data.Executable.code_size = ops.Count;*/

            return true;
        }
#endregion


        
#pragma warning disable CS1572 // XML comment has a param tag for 'segment', but there is no parameter by that name
/// <summary>
        /// optimize all code segments 
        /// </summary>
        /// <param name="cdata"></param>
        /// <param name="segment"></param>
        /// <returns></returns>
        int Optimize(CompilationData cdata)
#pragma warning restore CS1572 // XML comment has a param tag for 'segment', but there is no parameter by that name
        {
            if (cdata.code.IsSegmented && cdata.CompilerOptions.ParallelCompilation)
            {
                int[] res = new int[cdata.code.SegmentCount];
                Parallel.ForEach(cdata.code.segments,
                    segment
                    =>
                    res[cdata.code.segments.IndexOf(segment)] = OptimizeSegment(cdata, segment)
                    );
                return res.FirstOrDefault(x => x > 0);   
            }
            else
            {
                if(cdata.code.IsSegmented)
                {
                    foreach(IMByteCodeList segment in cdata.code.segments)
                    {
                        int ires = OptimizeSegment(cdata, segment);
                        if (ires != 0) return ires; 
                    }
                    return (int)BlastError.success; 
                }
                else
                {
                    return OptimizeSegment(cdata, cdata.code);
                }
            }
        }

        int OptimizeSegment(CompilationData cdata, IMByteCodeList segment)
        { 

            // replace common patterns 
            if (!ReplaceCommonPatterns(cdata, segment))
            {
                cdata.LogError("ByteCodeOptimizer.Segment: failure during replacement of common patterns.");
                return (int)BlastError.error;
            }

            // replace defined patterns 
            if (!OptimizePatterns(cdata, segment))
            {
                cdata.LogError("ByteCodeOptimizer.Segment: failed to optimize bytecode patterns");
                return (int)BlastError.error;
            }

            return (int)BlastError.success; 
        }

      

        /// <summary>
        /// execute the bytecode optimizer 
        /// </summary>
        /// <param name="data">compilationdata with code set</param>
        /// <returns>nonzero exitcode on failure</returns>
        public int Execute(IBlastCompilationData data)
        {
            if (data.CompilerOptions.Optimize)
            {
                CompilationData cdata = (CompilationData)data;
                
                if (cdata.code != null)
                {
                    // make sure data offsets are calculated and set in im data. 
                    // the packager creates this data but it might not have executed yet depending on configuration 
                    if (cdata.HasVariables && !cdata.HasOffsets || cdata.VariableCount != cdata.OffsetCount)
                    {
                        // generate variable offsets 
                        cdata.CalculateVariableOffsets(); 
                    }

                    Optimize(cdata); 
                }
            }
            else
            {
#if TRACE
                data.LogTrace("BlastBytecodeOptimizer.Execute: optimizations skipped dueue to compiler options");
#endif
            }
            return (int)(data.IsOK ? BlastError.success : BlastError.error);
        }
    }
}