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
#else
#define USE_BURST_JOBS
using UnityEngine;
using Unity.Jobs;
#endif

using NSS.Blast.Interpretor;

using System;
using System.Text;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System.Collections.Generic;

namespace NSS.Blast
{

    /// <summary>
    /// Blast script packaging types 
    /// </summary>
    public enum BlastPackageMode : byte
    {
        /// <summary>
        /// Compile all into a single package, code data and stack combined  
        /// <code>
        /// [----CODE----|----METADATA----|----DATA----|----STACK----]
        ///              1                2            3             4  
        /// 
        /// ** ALL OFFSETS IN PACKAGE IN BYTES ***
        /// 
        /// 1 = metadata offset 
        /// 2 = data_offset 
        /// 3 = stack_offset 
        /// 4 = package_size  
        /// </code>
        /// </summary>
        Normal,

        /// <summary>
        /// SSMD Package with code and metadata seperate from datastack
        /// <code>
        /// - SSMD requirement: while data changes, its intent does not so metadata is shared
        /// 
        /// - SSDM in V2/BS2 allows for (nested) branching by setting sync points on non constant jumps: 
        /// -- loop ends
        /// -- if then else jumps 
        /// 
        /// [----CODE----|----METADATA----]       [----DATA----|----STACK----]
        ///              1                2                    3             4
        /// 
        /// 1 = metadata offset 
        /// 2 = codesegment size 
        /// 3 = stack offset 
        /// 4 = datasegment size 
        /// 
        /// prop stacksize (in elements) => (datasegment size - stack_offset) / 4
        /// </code>
        /// </summary>
        SSMD,

        /// <summary>
        /// Entity Package: the script's code is seperated from all data
        /// <code>
        /// [----CODE----]      [----METADATA----|----DATA----|----STACK----]
        ///              1                       2            3             4
        /// 
        /// 1 = codesegment size 
        /// 2 = metadata size 
        /// 3 = stack offset 
        /// 4 = datasegment size 
        /// </code>
        /// </summary>
        Entity,

        /// <summary>
        /// <code>
        /// package type used by compiler 
        /// 
        /// - THE POINTERS ARE INVALID
        /// 
        /// [----CODE----]      [----METADATA----]     [----DATA----|----STACK----]
        ///              1                       2                  3             4
        /// 
        /// 1 = codesegment size 
        /// 2 = metadata size 
        /// 3 = datasize in bytes / stack offset in bytes 
        /// 4 = datasegment size = stackcapacity = 4 - 3  
        /// </code>
        /// </summary>
        Compiler
    }

    /// <summary>
    /// Package Flags
    /// </summary>
    [Flags]
    public enum BlastPackageFlags : byte
    {
        /// <summary>
        /// full data allocated, package can be directly executed 
        /// </summary>
        None = 0,

        /// <summary>
        /// package is allocated without stack data (template for SSMD) 
        /// </summary>
        NoStack = 1,

        /// <summary>
        /// align data offsets on a 4 byte boundary
        /// </summary>
        Aligned4 = 2,

        /// <summary>
        /// align data offsets on a 4 byte boundary
        /// </summary>
        Aligned8 = 4,

        /// <summary>
        /// true if the script calls random at some point in its execution 
        /// </summary>
        NeedsRandom = 8
    }


    /// <summary>
    /// a pointer to meta-data-stack memory
    /// </summary>
    [BurstCompile]
    unsafe public struct BlastMetaDataStack
    {
    }

    /// <summary>
    /// data for ssmd operation modes: data-stack only
    /// - only used as pointer, fields handy in debugger
    /// </summary>
    [BurstCompile]
    unsafe public struct BlastSSMDDataStack {
#if DEVELOPMENT_BUILD || TRACE
        /// <summary>
        /// fictional - for debug view only
        /// </summary>
        public float4 a;
        /// <summary>
        /// fictional - for debug view only
        /// </summary>
        public float4 b;
        /// <summary>
        /// fictional - for debug view only
        /// </summary>
        public float4 c;
        /// <summary>
        /// fictional - for debug view only
        /// </summary>
        public float4 d;        
#endif 
    }

    /// <summary>
    /// the bare minimum data (28 bytes) to package a script for execution 
    /// </summary>
    [BurstCompile]
    unsafe public struct BlastPackageData
    {
        /// <summary>
        /// points to codesegment, may be shared with other segments, see packagemode
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr CodeSegmentPtr;

        /// <summary>
        /// points to the other segment, see packagemode
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr P2;
        //-- 16 bytes


        /// <summary>
        /// Script packaging mode
        /// </summary>
        /// <remarks>
        /// <code>
        /// Packaging mode 
        /// 
        /// NORMAL: 
        ///         [----CODE----|----METADATA----|----DATA----|----STACK----]
        ///                      1                2            3             4  
        /// 
        /// SSMD:
        ///         [----CODE----|----METADATA----]       [----DATA----|----STACK----]
        ///                      1                2                    3             4
        /// 
        /// ENTITY:
        ///         [----CODE----]      [----METADATA----]     [----DATA----|----STACK----]
        ///                      1                       2                  3             4
        /// </code>
        /// </remarks>
        public BlastPackageMode PackageMode;

        /// <summary>
        /// Package languageversion, determines feature support 
        /// </summary>
        public BlastLanguageVersion LanguageVersion;

        /// <summary>
        /// Package flags, see flags enumeration  
        /// </summary>
        public BlastPackageFlags Flags;

        /// <summary>
        /// Package memory allocator 
        /// </summary>
        public byte Allocator;
        //-- 20 bytes

        /// <summary>
        /// Offset 1, intent depends on packagemode
        /// </summary>
        public ushort O1;

        /// <summary>
        /// Offset 2, intent depends on packagemode
        /// </summary>
        public ushort O2;

        /// <summary>
        /// Offset 3, intent depends on packagemode
        /// </summary>
        public ushort O3;

        /// <summary>
        /// Offset 4, intent depends on packagemode
        /// </summary>
        public ushort O4;
                            //-- 28 bytes

        #region Properties deduced from fields 

        /// <summary>
        /// size of code in bytes, maps to O1 in all package modes 
        /// </summary>
        public readonly ushort CodeSize
        {
            get
            {
                // in every packagemode this is O1
                return O1;
            }
        }

        /// <summary>
        /// metadata size in bytes, may include alignment bytes in normal and SSMD modes  
        /// </summary>
        public readonly ushort MetadataSize
        {
            get
            {

#if DEVELOPMENT_BUILD || TRACE
                switch (PackageMode)
                {
                    default:
                        Debug.LogError($"BlastPackageData.MetaDataSize: packagemode {PackageMode} not supported");
                        return 0;

                    case BlastPackageMode.Normal:
                    case BlastPackageMode.SSMD: return (ushort)(O2 - O1);

                    case BlastPackageMode.Compiler: 
                    case BlastPackageMode.Entity: return O2;
                }
#else 
                // less handy to read, no debug, but a lot faster and NO branch 
                return (ushort)math.select(O2 - O1, O2, (PackageMode == BlastPackageMode.Entity) || (PackageMode == BlastPackageMode.Compiler));
#endif 
            }
        }


        /// <summary>
        /// data size in bytes, may include alignment bytes 
        /// </summary>
        public readonly ushort DataSize
        {
            get
            {
#if DEVELOPMENT_BUILD || TRACE
                switch (PackageMode)
                {
                    default:
                        Debug.LogError($"BlastPackageData.DataSize: packagemode {PackageMode} not supported");
                        return 0;

                    case BlastPackageMode.Normal:
                    case BlastPackageMode.Entity: return (ushort)(O3 - O2);

                    case BlastPackageMode.Compiler: return O3; 
                    case BlastPackageMode.SSMD: return O3;
                }
#else 
                return (ushort)math.select(O3 - O1, O3, (PackageMode == BlastPackageMode.SSMD) || (PackageMode == BlastPackageMode.Compiler)); 
#endif
            }
        }

        /// <summary>
        /// stack size in bytes, may not be a multiple of elementsize/4bytes
        /// </summary>
        public readonly ushort StackSize
        {
            get
            {
#if DEVELOPMENT_BUILD || TRACE
                switch (PackageMode)
                {
                    default:
                        Debug.LogError($"BlastPackageData.StackSize: packagemode {PackageMode} not supported");
                        return 0;

                    case BlastPackageMode.Normal:
                    case BlastPackageMode.Entity:
                    case BlastPackageMode.Compiler:
                    case BlastPackageMode.SSMD: return (ushort)(O4 - O3);
                }
#else
                return (ushort)(O4 - O3);
#endif
            }
        }


        /// <summary>
        /// the allocated datasize in bytes of the codesegment pointer 
        /// </summary>
        public readonly ushort CodeSegmentSize
        {
            get
            {
                switch (PackageMode)
                {
                    default:
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"BlastPackageData.CodeSegmentSize: packagemode {PackageMode} not supported");
#endif 
                        return 0;
                    case BlastPackageMode.Normal: return O4;
                    case BlastPackageMode.SSMD: return O2;
                    case BlastPackageMode.Entity: return O1;
                }
            }
        }

        /// <summary>
        /// the size if stack would be included, while on normal packaging mode there is no seperate buffer there is a size
        /// </summary>
        public readonly ushort DataSegmentSize
        {
            get
            {
                switch (PackageMode)
                {
                    default:
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"BlastPackageData.DataSegmentSize: packagemode {PackageMode} not supported");
#endif 
                        return 0;

                    case BlastPackageMode.Compiler:
                    case BlastPackageMode.Normal: return (ushort)(O4 - O1);
                    case BlastPackageMode.SSMD: return (ushort)math.select((int)O3, (int)O4, HasStackPackaged);
                    case BlastPackageMode.Entity: return O4;
                }
            }
        }

        /// <summary>
        /// bytesize for needed BlastSSMDDataStack records
        /// </summary>
        public readonly int SSMDDataSize => DataSegmentSize;

        /// <summary>
        /// the size with stack included depending on flags
        /// </summary>
        public readonly ushort AllocatedDataSegmentSize
        {
            get
            {
                switch (PackageMode)
                {
                    default:
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"BlastPackageData.AllocatedDataSegmentSize: packagemode {PackageMode} not supported");
#endif 
                        return 0;

                    case BlastPackageMode.Normal: return 0;
                    case BlastPackageMode.SSMD: return HasStackPackaged ? O4 : O3;
                    case BlastPackageMode.Entity: return HasStackPackaged ? O4 : O3;
                }
            }
        }

        /// <summary>
        /// the maxiumum stack capacity in elements (1 element == 32 bit)
        /// - looks at flags, if == nostack then returns a capacity of 0
        /// - IMPORTANT NOTE: looking at profiler this really should be cached if asked a lot 
        /// </summary>
        public readonly ushort StackCapacity
        {
            get
            {
                return (ushort)((O4 - O3) >> 2);  // HasStack ? (ushort)((O4 - O3) / 4) : (ushort)0;
                //
                //                switch (PackageMode)
                //                {
                //                    default:
                //#if DEVELOPMENT_BUILD || TRACE
                //                        Standalone.Debug.LogError($"packagemode {PackageMode} not supported");
                //#endif 
                //                        return 0;
                //
                //                    case BlastPackageMode.Normal: return (ushort)((O4 - O3) / 4);
                //                    case BlastPackageMode.SSMD: return (ushort)((O4 - O3) / 4);
                //                    case BlastPackageMode.Entity: return (ushort)((O4 - O3) / 4);
                //                }
            }
        }

        /// <summary>
        /// stack offset in bytes as seen from the start of datasegment
        /// - stack and datasegment MUST ALWAYS be in the same segment so that Data[last] == Stack[-1] 
        /// - when using .Stack[] offset is 0
        /// </summary>
        public readonly ushort DataSegmentStackOffset
        {
            get
            {
                return (ushort)math.select(O3, O3 - O2, PackageMode == BlastPackageMode.Normal); 

                //return O3;
            }
        }

        /// <summary>
        /// Calculated pointer to metadata 
        /// </summary>
        public readonly IntPtr MetadataPtr
        {
            get
            {
#if DEVELOPMENT_BUILD || TRACE
                switch (PackageMode)
                {
                    default:
                        Debug.LogError($"BlastPackageData.MetadataPtr: packagemode {PackageMode} not supported");
                        return IntPtr.Zero;

                    case BlastPackageMode.Normal: return CodeSegmentPtr + O1;
                    case BlastPackageMode.SSMD: return CodeSegmentPtr + O1;
                    case BlastPackageMode.Entity: return P2;
                }
#else
                return PackageMode == BlastPackageMode.Entity ? P2 : CodeSegmentPtr + O1; 
#endif
            }
        }
        /// <summary>
        /// pointer to data segment as calculated from package configuration 
        /// </summary>
        public readonly IntPtr DataSegmentPtr
        {
            get
            {
                switch (PackageMode)
                {
                    default:
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"BlastPackageData.DataSegmentPtr: packagemode {PackageMode} not supported");
#endif                        
                        return IntPtr.Zero;

                    case BlastPackageMode.Compiler: 
                    case BlastPackageMode.Normal: return CodeSegmentPtr + O2;
                    case BlastPackageMode.Entity: return P2 + O2;
                    case BlastPackageMode.SSMD: return P2;
                }
            }
        }

        /// <summary>
        /// pointer to stack segment as calculated from package configuration 
        /// </summary>
        public readonly IntPtr StackSegmentPtr
        {
            get
            {
                switch (PackageMode)
                {
                    default:
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"BlastPackageData.StackSegmentPtr: packagemode {PackageMode} not supported");
#endif 
                        return IntPtr.Zero;

                    case BlastPackageMode.Normal: return CodeSegmentPtr + O3;
                    case BlastPackageMode.SSMD: return DataSegmentPtr + O3;
                    case BlastPackageMode.Entity: return DataSegmentPtr + O3;
                }
            }
        }

        /// <summary>
        /// Code[] pointer 
        /// </summary>
        public readonly unsafe byte* Code => (byte*)CodeSegmentPtr.ToPointer();

        /// <summary>
        /// metadata[], 1 byte of metadata for each dataelement in stack and datasegment 
        /// </summary>
        public readonly unsafe byte* Metadata => (byte*)MetadataPtr.ToPointer();

        /// <summary>
        /// data[] pointer 
        /// </summary>
        public readonly unsafe float* Data => (float*)DataSegmentPtr.ToPointer();

        /// <summary>
        /// Stack[] pointer -> Data[last] == Stack[-1]
        /// </summary>
        public readonly unsafe float* Stack => (float*)StackSegmentPtr.ToPointer();

        /// <summary>
        /// true if memory for stack is allocated, 
        /// false if not: 
        /// - yield not possible without persistant stack 
        /// - we call it TLS, thread level stack/storage 
        /// - Faster in small multithreaded bursts, benefit fades in very large bursts 
        /// </summary>
        public readonly bool HasStackPackaged => (Flags & BlastPackageFlags.NoStack) != BlastPackageFlags.NoStack;

        /// <summary>
        /// true if memory has been allocated for this object
        /// </summary>
        public readonly bool IsAllocated => (Allocator)Allocator != Unity.Collections.Allocator.None && CodeSegmentPtr != IntPtr.Zero;

        #endregion

        #region Cloning Util

        /// <summary>
        /// clone package with currently set allocator 
        /// </summary>
        /// <returns></returns>
        public BlastPackageData Clone()
        {
            return Clone((Unity.Collections.Allocator)Allocator); 
        }

        /// <summary>
        /// clone package with given allocator 
        /// </summary>
        public BlastPackageData Clone(Allocator allocator)
        {
            BlastPackageData clone = default;
            clone.PackageMode = PackageMode;
            clone.LanguageVersion = LanguageVersion;
            clone.Flags = Flags;
            clone.Allocator = (byte)allocator;
            clone.O1 = O1;
            clone.O2 = O2;
            clone.O3 = O3;
            clone.O4 = O4;

            if(IsAllocated)
            {
                switch (PackageMode)
                {
                    case BlastPackageMode.Normal:
                        clone.CodeSegmentPtr = new IntPtr(UnsafeUtils.Malloc(CodeSegmentSize, 8, allocator));
                        UnsafeUtils.MemCpy(clone.Code, Code, CodeSegmentSize);

                        clone.P2 = IntPtr.Zero;
                        break;
                    case BlastPackageMode.SSMD:
                    case BlastPackageMode.Entity:
                        clone.CodeSegmentPtr = new IntPtr(UnsafeUtils.Malloc(CodeSegmentSize, 8, allocator));
                        UnsafeUtils.MemCpy(clone.Code, Code, CodeSegmentSize);

                        clone.P2 = new IntPtr(UnsafeUtils.Malloc(clone.DataSegmentSize, 8, allocator));
                        UnsafeUtils.MemCpy(clone.P2.ToPointer(), P2.ToPointer(), clone.DataSegmentSize); 
                        break;

                    default:
                        Debug.LogError($"BlastPackageData.Clone: PackageMode {PackageMode} is not supported");
                        return default; 
                }
            }
            return clone; 
        }


        /// <summary>
        /// clone n data segments into 1 block and index it with pointers 
        /// - the first block contains the root pointer which can be freed 
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        unsafe public BlastMetaDataStack* CloneData(int n = 1)
        {
            return CloneData(n, (Unity.Collections.Allocator)Allocator);
        }

        /// <summary>
        /// clone n data segments into 1 block and index it with pointers 
        /// - the first block contains the root pointer which can be freed 
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        unsafe public BlastSSMDDataStack* CloneDataStack(int n = 1)
        {
            return CloneDataStack(n, (Unity.Collections.Allocator)Allocator);
        }

        bool CheckPackageModeForDataCloning()
        {
            if (PackageMode == BlastPackageMode.Compiler)
            {
#if TRACE
                Debug.LogError($"CloneData: packagemode {PackageMode} not supported");
#endif 
                return false;
            }
            return true; 
        }

        /// <summary>
        /// clone n segments into 1 memory block and index it with pointers 
        /// - the first block contains the root pointer to free later
        /// </summary>
        unsafe public BlastMetaDataStack* CloneData(int n, Allocator allocator)
        {
            if (!CheckPackageModeForDataCloning()) return null; 

            int byte_size = DataSegmentSize;
            int total_byte_size = byte_size * n;

            BlastMetaDataStack* data = (BlastMetaDataStack*)UnsafeUtils.Malloc(total_byte_size, 8, (Unity.Collections.Allocator)Allocator);

            return CloneData(n, data); 
        }


        /// <summary>
        /// clone n segments into 1 memory block and index it with pointers 
        /// - the first block contains the root pointer to free later
        /// </summary>
        unsafe public BlastSSMDDataStack* CloneDataStack(int n, Allocator allocator)
        {
            if (!CheckPackageModeForDataCloning()) return null;

            int byte_size = DataSegmentSize - MetadataSize;
            int total_byte_size = byte_size * n;

            BlastSSMDDataStack* data = (BlastSSMDDataStack*)UnsafeUtils.Malloc(total_byte_size, 8, (Unity.Collections.Allocator)Allocator);

            return CloneDataStack(n, data);
        }
        /// <summary>
        /// clone n segments into 1 memory block and index it with pointers 
        /// - the first block contains the root pointer to free later
        /// </summary>
        unsafe public BlastMetaDataStack* CloneData(int n, BlastMetaDataStack* data)
        {
            if (!CheckPackageModeForDataCloning()) return null;

            int byte_size = DataSegmentSize;

            // replicate memory 
            switch (PackageMode)
            {
                case BlastPackageMode.Normal:
                case BlastPackageMode.Entity:
                    // normal: continues pointer from O1 -> O4 (which is start of metadata)
                    // entity: continues pointer from 0 -> O4 (again from metadata start)
                    UnsafeUtils.MemCpyReplicate(data, Metadata, byte_size, n);
                    break;

                case BlastPackageMode.SSMD:

                    // copy 1st meta data & data/stack
                    // - UnsafeUtils.MemCpy(data, Metadata, MetadataSize);
                    UnsafeUtils.MemCpy(data, Data, byte_size);

                    // replicate from there 
                    if (n > 1)
                    {
                        UnsafeUtils.MemCpyReplicate(data + byte_size, data, byte_size, n - 1);
                    }
                    break;
            }

            return (BlastMetaDataStack*)data;
        }

        /// <summary>
        /// clone n segments into 1 memory block and index it with pointers 
        /// - the first block contains the root pointer to free later
        /// </summary>
        unsafe public BlastSSMDDataStack* CloneDataStack(int n, BlastSSMDDataStack* data)
        {
            if (!CheckPackageModeForDataCloning()) return null;

            int byte_size = SSMDDataSize;

            // replicate memory 
            switch (PackageMode)
            {
                case BlastPackageMode.SSMD:
                case BlastPackageMode.Normal:
                case BlastPackageMode.Entity:
                    // normal: continues pointer from O1 -> O4 (which is start of metadata)
                    // entity: continues pointer from 0 -> O4 (again from metadata start)
                    UnsafeUtils.MemCpyReplicate(data, DataSegmentPtr.ToPointer(), byte_size, n);
                    break;
            }

            return data;
        }


        /// <summary>
        /// free memory used by an ssmd data block
        /// </summary>
        unsafe public static void FreeData(BlastMetaDataStack* data, Allocator allocator)
        {
            if (data != null)
            {
                UnsafeUtils.Free(data, allocator);
                data = null;
            }
        }
        /// <summary>
        /// free memory used by an ssmd data block
        /// </summary>
        unsafe public static void FreeData(BlastSSMDDataStack* data, Allocator allocator)
        {
            if (data != null)
            {
                UnsafeUtils.Free(data, allocator);
                data = null;
            }
        }

        #endregion

        #region CDATA updates | update of scripted constant data without recompilation

        public BlastError SetCDATA(int cdata_reference, int cdata_index, float data)
        {
            if (!IsAllocated) return BlastError.error_package_not_allocated; 
            if (CodeSegmentPtr == null) return BlastError.error_codesegment_null;
            if (cdata_reference < 0 || cdata_reference >= CodeSize - 2) return BlastError.error_constant_reference_error;
            unsafe
            {
                return NSS.Blast.Interpretor.BlastInterpretor.WriteCDATA((byte*)CodeSegmentPtr, cdata_reference, cdata_index, data); 
            }
        }

        public BlastError SetCDATA(int cdata_reference, int cdata_index, byte data)
        {
            if (!IsAllocated) return BlastError.error_package_not_allocated;
            if (CodeSegmentPtr == null) return BlastError.error_codesegment_null;
            if (cdata_reference < 0 || cdata_reference >= CodeSize - 2) return BlastError.error_constant_reference_error;
            unsafe
            {
                return NSS.Blast.Interpretor.BlastInterpretor.WriteCDATA((byte*)CodeSegmentPtr, cdata_reference, cdata_index, data);
            }
        }
        public BlastError SetCDATA(int cdata_reference, int cdata_index, int data)
        {
            if (!IsAllocated) return BlastError.error_package_not_allocated;
            if (CodeSegmentPtr == null) return BlastError.error_codesegment_null;
            if (cdata_reference < 0 || cdata_reference >= CodeSize - 2) return BlastError.error_constant_reference_error;
            unsafe
            {
                return NSS.Blast.Interpretor.BlastInterpretor.WriteCDATA((byte*)CodeSegmentPtr, cdata_reference, cdata_index, data);
            }
        }

        #endregion 


        /// <summary>
        /// free any memory allocated 
        /// </summary>
        public void Free()
        {
            if (IsAllocated)
            {
                unsafe
                {
                    UnsafeUtils.Free(CodeSegmentPtr.ToPointer(), (Unity.Collections.Allocator)Allocator);
                    if (P2 != IntPtr.Zero)
                    {
                        UnsafeUtils.Free(P2.ToPointer(), (Unity.Collections.Allocator)Allocator);
                    }
                }
#if DEVELOPMENT_BUILD || TRACE
                CodeSegmentPtr = IntPtr.Zero;
                P2 = IntPtr.Zero;
                Allocator = (byte)Unity.Collections.Allocator.None;
#endif
            }
        }
    }




    /// <summary>
    /// description mapping a variable to an input or output
    /// </summary>
    public class BlastVariableMapping
    {
        /// <summary>
        /// the variable that is mapped 
        /// </summary>
        public BlastVariable Variable;

        /// <summary>
        /// Id of variable 
        /// </summary>
        public int VariableId => Variable.Id;

        /// <summary>
        /// Datatype of variable 
        /// </summary>
        public BlastVariableDataType DataType => Variable.DataType;

        /// <summary>
        /// Offset of variable into datasegment, in bytes 
        /// </summary>
        public int Offset;

        /// <summary>
        /// Bytesize of variable  
        /// </summary>
        public int ByteSize;


        /// <summary>
        /// for now, default is stored in max sized datatype, this will change in the future
        /// </summary>
        public float4 Default; 


        /// <summary>
        /// ToString override providing a more usefull description while debugging 
        /// </summary>
        /// <returns>a formatted string</returns>
        public override string ToString()
        {
            if (Variable == null)
            {
                return $"variable mapping: var = null, bytes: {ByteSize}, offset: {Offset}";
            }
            else
            {
                return $"variable mapping: var: {VariableId} {Variable.Name} {Variable.DataType}, vectorsize: {Variable.VectorSize}, bytes: {ByteSize}, offset: {Offset}, default: {Default}";
            }
        }
    }



    /// <summary>
    /// Description of a variable as determined during compilation of script. It is not needed for execution
    /// and is thus not available through the native BlastPackageData. Native code can query metadata.
    /// </summary>
    public class BlastVariable
    {
        /// <summary>
        /// the unique id of the variable as used within the script
        /// </summary>
        public int Id;

        /// <summary>
        /// The name of the variable as it was used in the script 
        /// </summary>
        public string Name;

        /// <summary>
        /// Number of times blast references the variable
        /// </summary>
        public int ReferenceCount;

        /// <summary>
        /// The datatype of the variable, all data is of type NUMERIC initially and may be reinterpretted as either
        /// current support: BOOL32
        /// future support:  ID, PTR or HALF 
        /// </summary>
        public BlastVariableDataType DataType = BlastVariableDataType.Numeric;

        /// <summary>
        /// used during compilation, compile can update type during compilation 
        /// </summary>
        public BlastVectorSizes DataTypeOverride = BlastVectorSizes.none; 

        /// <summary>
        /// is this variable constant? 
        /// </summary>
        public bool IsConstant = false;


        /// <summary>
        /// true if this is a vector of size > 1
        /// </summary>
        public bool IsVector = false;

        /// <summary>
        /// true if a constant data array
        /// </summary>
        public bool IsCData => DataType == BlastVariableDataType.CData; 

        /// <summary>
        /// vectorsize of the variable, default to size 1
        /// </summary>
        public int VectorSize = 1;

        /// <summary>
        /// true if specified in inputs 
        /// </summary>
        public bool IsInput = false;

        /// <summary>
        /// true if specified in outputs 
        /// </summary>
        public bool IsOutput = false;


        /// <summary>
        /// ToString override for a more detailed view during debugging 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{(IsConstant ? "Constant: " : "Variable: ")} {Id}, '{Name}', ref {ReferenceCount}, size {VectorSize}, type {DataType} {(IsInput ? "[INPUT]" : "")} {(IsOutput ? "[OUTPUT]" : "")}";
        }

        /// <summary>
        /// if datatype == cdata this might hold constant data
        /// 
        /// if is_constant then this data should end up in de codesegment
        /// otherwise in a variable on the datasegment 
        /// </summary>
        public byte[] ConstantData = null;


        /// <summary>
        /// the encoding of the cdata segment described by this node if known
        /// </summary>
        public CDATAEncodingType ConstantDataEncoding = CDATAEncodingType.None;

        /// <summary>
        /// Add a reference to this variable 
        /// </summary>
        internal void AddReference()
        {
            Interlocked.Increment(ref ReferenceCount);
        }
  
    }


    /// <summary>
    /// BlastScriptPackage contains all data needed to use and execute scriptcode, it consists of: 
    /// 
    /// - BlastPackage -> contains the native bytecode 
    /// - Variable information 
    /// - IO mapping 
    /// - Bursted functionpointer mapping 
    /// 
    /// </summary>
    public class BlastScriptPackage
    {
        /// <summary>
        /// A functionpointer to burst transpiled bytecode, possible if the package was known at compile time. 
        /// SSMD mode will still use the bytecode package. 
        /// </summary>
        public IntPtr Bursted;

        /// <summary>
        /// the bytecode package 
        /// </summary>
        public BlastPackageData Package;

        /// <summary>
        /// True if this package has also been burstcompiled and can be executed with a native function pointer 
        /// </summary>
        public bool IsBurstCompiled { get; private set; } = false;

        /// <summary>
        /// defined inputs 
        /// </summary>
        public BlastVariableMapping[] Inputs;

        /// <summary>
        /// defined outputs
        /// </summary>
        public BlastVariableMapping[] Outputs;

        /// <summary>
        /// Variable information
        /// </summary>
        public BlastVariable[] Variables;

        /// <summary>
        /// Offsets for variables in element count into datasegment, an elements is 4 bytes large == 1 float
        /// </summary>
        public byte[] VariableOffsets;

        /// <summary>
        /// Returns true if native memory is allocated for the package 
        /// </summary>
        public bool IsAllocated => Package.IsAllocated;


        /// <summary>
        /// size of package data 
        /// </summary>
        public int PackageSize
        {
            get
            {
                if (!IsAllocated) return 0;
                return Package.CodeSize + Package.MetadataSize + Package.AllocatedDataSegmentSize;
            }
        }

        /// <summary>
        /// packaging mode set by attached package data
        /// </summary>
        public BlastPackageMode PackageMode => Package.PackageMode;


        /// <summary>
        /// package flags as set by attached package
        /// </summary>
        public BlastPackageFlags Flags => Package.Flags;


        /// <summary>
        /// Destroy any allocated native memory 
        /// </summary>
        public void Destroy()
        {
            if (Package.IsAllocated)
            {
                Package.Free();
#if DEVELOPMENT_BUILD || TRACE
                Package = default; 
#endif
            }
            Inputs = null;
            Outputs = null;
            Variables = null;
            VariableOffsets = null;
            Package = default;
            IsBurstCompiled = false;
        }

#region ToString + getxxxxText()

        /// <summary>
        /// ToString overload for more information during debugging
        /// </summary>
        public override string ToString()
        {
            if (IsAllocated)
            {
                unsafe
                {
                    return $"BlastScriptPackage, package size = {Package.CodeSegmentSize + Package.DataSegmentSize}";
                }
            }
            else
            {
                return $"BlastScriptPackage [not allocated]";
            }
        }

        /// <summary>
        /// Get a string representation of the bytecode, example output: 
        ///
        /// 000| push compound 1 + 2 nop push function max ^ pop 2 
        /// 010| debug pop nop 
        /// 
        /// 000| 030 085 002 086 000 029 042 009 025 086 
        /// 010| 255 253 025 000 
        ///
        /// </summary>
        /// <param name="width">number of columns to render</param>
        /// <returns>A formatted string</returns>
#pragma warning disable CS1573 // Parameter 'show_index' has no matching param tag in the XML comment for 'BlastScriptPackage.GetCodeSegmentText(int, bool)' (but other parameters do)
        public string GetCodeSegmentText(int width = 16, bool show_index = true)
#pragma warning restore CS1573 // Parameter 'show_index' has no matching param tag in the XML comment for 'BlastScriptPackage.GetCodeSegmentText(int, bool)' (but other parameters do)
        {
            if (IsAllocated)
            {
                unsafe
                {
                    byte* code = Package.Code;

                    StringBuilder sb = new StringBuilder();
                    sb.Append($"Code Segment: { Package.CodeSize} bytes\n\n");
                    sb.Append(GetPackageCodeBytesText(width, show_index));
                    sb.Append("\n");
                    sb.Append(Blast.GetReadableByteCode(code, Package.CodeSize));
                    sb.Append("\n");
                    return sb.ToString();
                }
            }
            else
            {
                return "BlastScriptPackage.CodeSegment [not allocated]";
            }
        }


        /// <summary>
        /// Get a string representation of the datasegement
        /// </summary>
        public string GetDataSegmentText()
        {
            if (IsAllocated)
            {

                unsafe
                {
                    StringBuilder sb = StringBuilderCache.Acquire(); 
                    sb.Append($"Data Segment, data = {Package.DataSize } bytes, metadata = {Package.MetadataSize} bytes, stack = {Package.StackSize} bytes \n");

                    float* data_segment = Package.Data;

                    for (int i = 0; i < Variables.Length; i++)
                    {
                        var v = Variables[i];
                        int o = VariableOffsets[i];

                        string name = v.Name;
                        name = char.IsDigit(name[0]) ? "constant" : "name = " + name;

                        string value = string.Empty;

                        name += $" [{v.VectorSize}]";
                        for (int j = 0; j < v.VectorSize; j++)
                        {
                            value += $"{data_segment[o + j].ToString("R")} ";
                        }

                        sb.Append($" var {(i + 1).ToString().PadRight(4, ' ')} {name.PadRight(40, ' ')} value = {value.PadRight(32, ' ')} refcount = {(i < Variables.Length ? Variables[i].ReferenceCount.ToString() : "")}\n");
                    }

                    return StringBuilderCache.GetStringAndRelease(ref sb); 
                }
            }
            else
            {
                return "BlastScriptPackage.DataSegment [not allocated]";
            }
        }


        /// <summary>
        /// get code as 000| 000 000 000 000 
        /// </summary>
        /// <returns></returns>
        public string GetPackageCodeBytesText(int column_count = 8, bool show_index = false)
        {
            if (IntPtr.Zero == Package.CodeSegmentPtr) return string.Empty;
            unsafe
            {
                return CodeUtils.GetBytesView((byte*)(void*)Package.Code, Package.CodeSize, column_count, show_index);
            }
        }

        /// <summary>
        /// get datasegment as 000| 000 000 000 000 
        /// </summary>
        /// <returns></returns>
        public unsafe string GetPackageDataBytesText(int column_count = 8, bool show_index = false)
        {
            if (IntPtr.Zero == Package.DataSegmentPtr) return string.Empty;
            unsafe
            {
                return CodeUtils.GetBytesView((byte*)(void*)Package.Data, Package.DataSegmentSize, column_count, show_index);
            }
        }

        /// <summary>
        /// return overview of package information 
        /// </summary>
        /// <returns></returns>
        public string GetPackageInfoText()
        {
            if (IsAllocated)
            {
                unsafe
                {
                    StringBuilder sb = StringBuilderCache.Acquire();
                    sb.Append("BlastScriptPackage Information\n");
                    sb.Append($"- language        = {Package.LanguageVersion.ToString()}\n");
                    sb.Append($"- mode            = {Package.PackageMode}\n");
                    sb.Append($"- flags           = {Package.PackageMode}\n");

                    switch (Package.PackageMode)
                    {
                        case BlastPackageMode.Normal:
                            sb.Append($"- package size    = {Package.CodeSegmentSize + Package.DataSegmentSize} bytes\n");
                            break;

                        default:
                            sb.Append("please implement me TODO");
                            break;
                    }

                    sb.Append($"- code size       = {Package.CodeSize} bytes\n");
                    sb.Append($"- meta size       = {Package.MetadataSize} bytes\n");
                    sb.Append($"- data size       = {Package.DataSize} bytes\n");
                    sb.Append($"- stack size      = {Package.StackSize} bytes\n");
                    sb.Append($"- stack capacity  = {Package.StackCapacity} elements\n");
                    sb.Append($"- stack offset    = {Package.DataSegmentStackOffset}\n\n");

                    return StringBuilderCache.GetStringAndRelease(ref sb);
                }
            }
            else
            {
                return "BlastScriptPackage [not allocated]";
            }
        }

#endregion

        /// <summary>
        /// true if the script uses variables 
        /// </summary>
        public bool HasVariables { get { return Variables != null && Variables.Length > 0; } }

        /// <summary>
        /// true if the package has outputs defined
        /// </summary>
        public bool HasOutputs { get { return Outputs != null && Outputs.Length > 0; } }

        /// <summary>
        /// true if package has inputs defined
        /// </summary>
        public bool HasInputs { get { return Inputs != null && Inputs.Length > 0; } }


        /// <summary>
        /// true if constant data is inlined into the code stream 
        /// </summary>
        public bool HasInlinedConstants { get { return PackageMode == BlastPackageMode.SSMD; } }


        /// <summary>
        /// execute the script in the given environment with the supplied data
        /// </summary>
        /// <param name="blast">blastengine data</param>
        /// <param name="environment">[optional] pointer to environment data</param>
        /// <param name="caller">[ooptional] caller data</param>
        /// <returns>success if all is ok</returns>
        public BlastError Execute(IntPtr blast, IntPtr environment, IntPtr caller)
        {
            if (blast == IntPtr.Zero) return BlastError.error_blast_not_initialized;
            if (!IsAllocated) return BlastError.error_package_not_allocated;

            switch (Package.PackageMode)
            {
                case BlastPackageMode.Normal:
#if USE_BURST_JOBS 
                    var job = new Jobs.blast_execute_package_burst_job()
                    {
                        engine = blast,
                        package = Package,
                        environment = environment,
                        caller = caller
                    };
                    job.Run();
                    return (BlastError)job.exitcode;
#else 
                    Blast.blaster.SetPackage(Package);
                    return (BlastError)Blast.blaster.Execute(blast, environment, caller);
#endif

                case BlastPackageMode.Entity:
                case BlastPackageMode.Compiler:
                case BlastPackageMode.SSMD:
                    return BlastError.error_packagemode_not_supported_for_direct_execution;
            }

            return BlastError.success;
        }


        /// <summary>
        /// execute the script using the data pointer as datasegment   
        /// </summary>
        /// <param name="blast"></param>
        /// <param name="environment"></param>
        /// <param name="caller"></param>
        /// <param name="data"></param>
        /// <param name="reset_defaults"></param>
        /// <returns></returns>
        unsafe public BlastError Execute(IntPtr blast, IntPtr environment, IntPtr caller, void* p_data_segment)
        {
            if (blast == IntPtr.Zero) return BlastError.error_blast_not_initialized;
            if (!IsAllocated) return BlastError.error_package_not_allocated;

            switch (Package.PackageMode)
            {
                case BlastPackageMode.Normal:
#if USE_BURST_JOBS
                    int* exitstate = stackalloc int[1];
                    var job = new Jobs.blast_execute_package_with_multiple_data_burst_job()
                    {
                        engine = blast,
                        package = Package,
                        data = new IntPtr(p_data_segment),
                        data_count = 1,
                        data_size = 0,
                        environment = environment,
                        caller = caller,
                        exitstate = new IntPtr(exitstate)
                    };
                    job.Run();
                    return (BlastError)job.GetExitCode();
#else
                    Blast.blaster.SetPackage(Package, p_data_segment);
                    return (BlastError)Blast.blaster.Execute(blast, environment, caller);
#endif


                case BlastPackageMode.Entity:
                case BlastPackageMode.Compiler:
                case BlastPackageMode.SSMD:
                    return BlastError.error_packagemode_not_supported_for_direct_execution;
            }

            return BlastError.success;
        }

        unsafe public BlastError Execute<T>(IntPtr blast, IntPtr environment, IntPtr caller, NativeArray<T> data, bool is_referenced = false, int max_ssmd_count = 1024 * 2) where T : unmanaged
        {
            return Execute(blast, environment, caller, data.Slice(0, data.Length), is_referenced, max_ssmd_count); 
        }


        /// <summary>
        /// ssmd execution 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="blast"></param>
        /// <param name="environment"></param>
        /// <param name="caller"></param>
        /// <param name="data">data array</param>
        /// <param name="is_referenced">if true data contains references to the actual data</param>
        /// <param name="max_ssmd_count">max per single run</param>
        /// <returns></returns>
        unsafe public BlastError Execute<T>(IntPtr blast, IntPtr environment, IntPtr caller, NativeSlice<T> data, bool is_referenced = false, int max_ssmd_count = 1024 * 2) where T : unmanaged
        {
            if (blast == IntPtr.Zero) return BlastError.error_blast_not_initialized;
            if (!IsAllocated) return BlastError.error_package_not_allocated;

            switch (Package.PackageMode)
            {
                case BlastPackageMode.SSMD:
                case BlastPackageMode.Normal:
#if !USE_BURST_JOBS
                    BlastError res = BlastError.success;
                    Blast.ssmd_blaster.SetPackage(in Package);

                    // execute in SSMD if packaged as such
                    if (Package.PackageMode == BlastPackageMode.SSMD)
                    {
                        if (is_referenced)
                        {
                            int i = 0;
                            void** pdata = (void**)data.GetUnsafeReadOnlyPtr();
                            while (i < data.Length && res == BlastError.success)
                            {
                                int j = math.min(data.Length, i + max_ssmd_count);
                                if (j > 0)
                                {
                                    res = (BlastError)Blast.ssmd_blaster.Execute(in Package, blast, environment, (BlastSSMDDataStack*)&pdata[i], j - i, true);
                                    i = j;
                                }
                            }
                        }
                        else
                        {
                            byte** dataptrs = stackalloc byte*[max_ssmd_count];
                            byte* p = (byte*)data.GetUnsafeReadOnlyPtr();
                            int datasize = sizeof(T);

                            int i = 0;

                            while (i < data.Length && res == BlastError.success)
                            {
                                int j = 0;
                                while (i < data.Length && j < max_ssmd_count)
                                {
                                    dataptrs[j] = (byte*)&p[i * datasize];
                                    i++;
                                    j++;
                                }
                                if (j > 0)
                                {
                                    // execute portion 
                                    res = (BlastError)Blast.ssmd_blaster.Execute(in Package, blast, environment, (BlastSSMDDataStack*)(void*)dataptrs, j);
                                }
                            }
                        }
                    }
                    else
                    {
                        // execute all data elements as datasegments 
                        T* p = (T*)data.GetUnsafeReadOnlyPtr();
                        for (int i = 0; i < data.Length && res == BlastError.success; i++)
                        {
                            Blast.blaster.SetPackage(Package, &p[i]);
                            res = (BlastError)Blast.blaster.Execute(blast, environment, caller);
                        }
                    }
                    return res;
#else
                    // allocate an exitstate on the stack 
                    int* exitstate = stackalloc int[1];
                    exitstate[0] = 0; 

                    if (Package.PackageMode == BlastPackageMode.SSMD)
                    {
                        var ssmd_job = new Jobs.blast_execute_package_ssmd_array_burst_job()
                        {
                            engine = blast,
                            package = Package,
                            environment = environment,
                            data_count = data.Length,
                            data_buffer = new IntPtr(data.GetUnsafeReadOnlyPtr()),
                            data_size = sizeof(T),
                            data_is_referenced = is_referenced, 
                            max_ssmd_count = 1024,
                            exitstate = new IntPtr(exitstate)
                        };
                        ssmd_job.Run();
                        return (BlastError)exitstate[0];
                    }
                    else
                    {
                        if (is_referenced)
                        {
                            Debug.LogError("BlastScriptPackage.Execute: data_is_referenced is only allowed for ssmd packages");
                            return BlastError.error_execute_referenced_datasegments_not_supported;
                        }

                        var job = new Jobs.blast_execute_package_with_multiple_data_burst_job()
                        {
                            engine = blast,
                            package = Package,
                            data = new IntPtr(data.GetUnsafeReadOnlyPtr()),
                            data_count = (data.Length * 4) / Package.DataSize,
                            data_size = Package.DataSize,
                            environment = environment,
                            caller = caller,
                            exitstate = new IntPtr(exitstate)

                        };
                        job.Run();
                        return (BlastError)job.GetExitCode();
                    }                                              
#endif

                case BlastPackageMode.Entity:
                case BlastPackageMode.Compiler:
                    return BlastError.error_packagemode_not_supported_for_direct_execution;
            }

            return BlastError.success;
        }


#region Default Data  

        /// <summary>
        /// set default data in external datasegments from input or output data found in script 
        /// </summary>
        unsafe static public void SetDefaultData(BlastVariableMapping[] inout, float* datasegment)
        {
            for (int i = 0; i < inout.Length; i++)
            {
                // we cant know if its actually set so we just write it at compilation 
                BlastVariableMapping map = inout[i];
                for (int j = 0; j < map.Variable.VectorSize; j++)
                {
                   datasegment[(map.Offset >> 2) + j] = map.Default[j];
                }
            }
        }

        /// <summary>
        /// set default data in external datasegments as set by input/output definitions, to be used to reset data in place  
        /// - does not initialize constant data fields 
        /// </summary>
        /// <param name="datasegments">float array with enough room for all data</param>
        /// <param name="data_segment_count">number of datasegments</param>
        /// <param name="data_segment_stride">size of a single datasegment including any reserved space or alignment</param>
        public void SetDefaultData(ref NativeArray<float> datasegments, int data_segment_count, int data_segment_stride = -1)
        {
            if (data_segment_stride <= 0)
            {
                // default to package datasize, both are specified in bytes 
                data_segment_stride = Package.DataSize;
            }

            List<BlastVariableMapping> inout = new List<BlastVariableMapping>(
                (HasInputs ? Inputs.Length : 0) + (HasOutputs ? Outputs.Length : 0)
            );

            if (HasInputs) inout.AddRange(Inputs);   // everything is an input currently but that will probably change
            if (HasOutputs) inout.AddRange(Outputs);

            int offset = 0;
            for (int c = 0; c < data_segment_count; c++)
            {
                // run over inputs 
                for (int i = 0; i < inout.Count; i++)
                {
                    BlastVariableMapping map = inout[i];
                    for (int j = 0; j < map.Variable.VectorSize; j++)
                    {
                        datasegments[offset + (map.Offset >> 2) + j] = map.Default[j];
                    }
                }

                offset += (data_segment_stride >> 2);
            }
        }

        /// <summary>
        /// initialize data in datasegments, sets up the first from variable and constant data then clones the data to all others
        /// - in normal packagemode this also includes setting all constant data 
        /// </summary>
        /// <param name="datasegments"></param>
        /// <param name="data_segment_count"></param>
        /// <param name="data_segment_stride"></param>
        public void InitializeDataSegments(ref NativeArray<float> datasegments, int data_segment_count, int data_segment_stride = -1)
        {
            unsafe
            {
                if (data_segment_stride <= 0)
                {
                    // default to package datasize, both are specified in bytes 
                    data_segment_stride = Package.DataSize;
                }

                // fill first segment with constant data
                if (!HasInlinedConstants)
                {
                    for (int i = 0; i < Variables.Length; i++)
                    {
                        if (Variables[i].IsConstant)
                        {
                            // constants always have size 1 
                            int offset = VariableOffsets[i];

                            //  this could be more direct without the parsing we could clone the datasegment but we have no guarantee its set 
                            //
                            //switch(Variables[i].DataType)
                            //{
                            //    case BlastVariableDataType.Bool32:
                            //        datasegments[offset] = Bool32.From(Variables[i].Name).Single;
                            //        break;
                            //
                            //    case BlastVariableDataType.Numeric:
                            //        datasegments[offset] = Variables[i].Name.AsFloat();
                            //        break; 
                            //}

                            datasegments[offset] = Package.Data[offset];
                        }
                    }
                }

                // set mapped fields on the first segment 
                // - dont use data from package as source because it might have changed because of executions 
                if (HasInputs)
                {
                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        BlastVariableMapping map = Inputs[i];
                        for (int j = 0; j < map.Variable.VectorSize; j++)
                        {
                            datasegments[(map.Offset >> 2) + j] = map.Default[j];
                        }
                    }
                }
                if (HasOutputs)
                {
                    for (int i = 0; i < Outputs.Length; i++)
                    {
                        BlastVariableMapping map = Outputs[i];
                        for (int j = 0; j < map.Variable.VectorSize; j++)
                        {
                            datasegments[(map.Offset >> 2) + j] = map.Default[j];
                        }
                    }
                }

                // clone data for all other segments 
                byte* p = (byte*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(datasegments);
                
                UnsafeUtility.MemCpyReplicate(&p[data_segment_stride], p, data_segment_stride, data_segment_count - 1);
            }
        }

        /// <summary>
        /// reset datasegment to default data set by input and output defines 
        /// </summary>
        public void ResetDefaults()
        {
            if (IsAllocated && (HasInputs || HasOutputs))
            {
                unsafe
                {
                    if (HasInputs)
                    {
                        BlastScriptPackage.SetDefaultData(Inputs, Package.Data);
                    }
                    if (HasOutputs)
                    {
                        BlastScriptPackage.SetDefaultData(Outputs, Package.Data);
                    }
                }
            }
        }
        /// <summary>
        /// define inputs from variables 
        /// </summary>
        /// <returns></returns>
        internal BlastError DefineInputsFromVariables()
        { 
            if (HasVariables)
            {
                Inputs = new BlastVariableMapping[Variables.Length];
                int offset = 0;
                for (int i = 0; i < Variables.Length; i++)
                {
                    int bytesize = Variables[i].VectorSize * 4;

                    Inputs[i] = new BlastVariableMapping()
                    {
                        ByteSize = bytesize,
                        Offset = offset,
                        Variable = Variables[i]
                    };

                    offset += bytesize;
                }

                return BlastError.success;
            }
            return BlastError.compile_packaging_error;
        }

        public bool GetMetaDataInfo(int i, out BlastVariableDataType datatype, out byte vector_size)
        {
            unsafe
            {
                if (Package.Metadata != null && i >= 0 && i < Package.MetadataSize)
                {
                    byte b = Package.Metadata[i];
                    vector_size = (byte)(b & 0b0000_1111);
                    datatype = (BlastVariableDataType)((b & 0b1111_0000) >> 4);
                    return true; 
                }
            }
            vector_size = 0;
            datatype = BlastVariableDataType.Numeric;
            return false;
        }


        #endregion

    }

}
