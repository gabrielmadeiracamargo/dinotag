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
    using System.Reflection;
#else
    using UnityEngine;
    using Unity.Burst.CompilerServices;
#endif


using Unity.Burst;
using Unity.Mathematics;

namespace NSS.Blast.SSMD
{
    unsafe public partial struct BlastSSMDInterpretor
    {

        /// <summary>
        /// result vector size == 1 in all cases
        /// </summary>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_dot_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.dot: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif
            switch (vector_size)
            {
                case 1:
                    {
                        float* fpa = (float*)temp;
                        float* fpb = &fpa[ssmd_datacount];

                        pop_fx_into_ref<float>(ref code_pointer, fpa);
                        pop_fx_into_ref<float>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.dot(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        float2* fpa = (float2*)temp;
                        float2* fpb = &fpa[ssmd_datacount];

                        pop_fx_into_ref<float2>(ref code_pointer, fpa);
                        pop_fx_into_ref<float2>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.dot(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 3:
                    {
                        float3* fpa = (float3*)temp;
                        float3* fpb = stackalloc float3[ssmd_datacount];

                        pop_fx_into_ref<float3>(ref code_pointer, fpa);
                        pop_fx_into_ref<float3>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.dot(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        float4* fpa = (float4*)temp;
                        float4* fpb = stackalloc float4[ssmd_datacount];

                        pop_fx_into_ref<float4>(ref code_pointer, fpa);
                        pop_fx_into_ref<float4>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.dot(fpa[i], fpb[i]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.dot: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer--;
            vector_size = 1;
        }

    }
}
