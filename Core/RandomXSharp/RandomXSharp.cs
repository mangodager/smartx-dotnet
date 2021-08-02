using System;
using System.Runtime.InteropServices;
using System.Text;
using ETModel;
using System.Threading;
using System.Collections.Generic;

namespace RandomXSharp
{

    [System.Flags]
    public enum Flags
    {
        Default = 0,
        LargePages = 1,
        HardAes = 2,
        FullMem = 4,
        Jit = 8,
        Secure = 16,
        Argon2Ssse3 = 32,
        Argon2Avx2 = 64,
        Argon2 = 96,
    };

    public partial class RandomXDLL
    {
#if (UNITY_IPHONE || UNITY_WEBGL || UNITY_SWITCH) && !UNITY_EDITOR
        const string __DLL = "__Internal";
#else
        //const string __DLL = "libxlua.so";
        const string __DLL = "randomx.dll";
#endif

        [DllImport(__DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern Flags randomx_get_flags();

        [DllImport(__DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr randomx_alloc_cache(Flags flags);

        [DllImport(__DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void randomx_init_cache(
            IntPtr cache,
            byte[] key,
            uint keySize
        );

        [DllImport(__DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void randomx_release_cache(IntPtr cache);

        [DllImport(__DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr randomx_create_vm(Flags flags, IntPtr cache, IntPtr dataset);

        [DllImport(__DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void randomx_vm_set_cache(IntPtr machine, IntPtr cache);

        [DllImport(__DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void randomx_destroy_vm(IntPtr machine);

        [DllImport(__DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr randomx_alloc_dataset(Flags flags);

        [DllImport(__DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void randomx_init_dataset(
            IntPtr dataset,
            IntPtr cache,
            uint startItem,
            uint itemCount
        );

        [DllImport(__DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint randomx_dataset_item_count();

        [DllImport(__DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void randomx_calculate_hash(
            IntPtr machine,
            byte[] input,
            uint inputSize,
            byte[] output
        );

        [DllImport(__DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void randomx_calculate_hash_first(
            IntPtr machine,
            byte[] input,
            uint inputSize
        );

        [DllImport(__DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void randomx_calculate_hash_next(
            IntPtr machine,
            byte[] nextInput,
            uint nextInputSize,
            byte[] output
        );

        [DllImport(__DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void randomx_calculate_hash_last(
            IntPtr machine,
            byte[] output
        );

    }

    public class RandomX
    {
        public const int HashSize = 32;
        static readonly byte[] myKey = Encoding.ASCII.GetBytes("SmarTxfLL7p53gdBh463ADWWRoJHFZhQH\0");

        private static Flags  _Flags;
        private static IntPtr _Cache   = IntPtr.Zero;
        private static IntPtr _Dataset = IntPtr.Zero;
        private static IntPtr _MainMachine = IntPtr.Zero;

        private static ThreadLocalID<IntPtr> _Machine = new ThreadLocalID<IntPtr>(() =>
        {
            //Console.WriteLine($"create_vm: {Thread.CurrentThread.Name} {Thread.CurrentThread.ManagedThreadId}");

            IntPtr myMachine = RandomXDLL.randomx_create_vm(RandomX._Flags, RandomX._Cache, RandomX._Dataset);
            return myMachine;
        });

        public static void randomx_init(bool miningMode=false)
        {
            if (_Cache != IntPtr.Zero)
                return;
            lock (myKey)
            {
                if (_Cache == IntPtr.Zero)
                {
                    //Log.Debug("randomx_init ...");

                    if (!miningMode)
                    {
                        _Flags = RandomXDLL.randomx_get_flags();
                        _Cache = RandomXDLL.randomx_alloc_cache(_Flags);
                        RandomXDLL.randomx_init_cache(_Cache, myKey, Convert.ToUInt32(myKey.Length));
                    }
                    else
                    {
                        Log.Debug("randomx_init FullMem");
                        _Flags = RandomXDLL.randomx_get_flags() | Flags.FullMem;
                        _Cache = RandomXDLL.randomx_alloc_cache(_Flags);
                        RandomXDLL.randomx_init_cache(_Cache, myKey, Convert.ToUInt32(myKey.Length));
                        _Dataset = RandomXDLL.randomx_alloc_dataset(_Flags);
                        var datasetItemCount = RandomXDLL.randomx_dataset_item_count();

                        uint initThreadCount = Math.Max((uint)Environment.ProcessorCount - 1, 1);
                        uint finishCount = 0;
                        uint startItem = 0;
                        uint perThread = datasetItemCount / initThreadCount;
                        uint remainder = datasetItemCount % initThreadCount;
                        for (uint ii = 0; ii < initThreadCount; ii++)
                        {
                            var count = perThread + (ii == initThreadCount - 1 ? remainder : 0);
                            uint start = startItem;

                            Thread thread = new Thread(new ParameterizedThreadStart(
                            (object data) =>
                            {
                                RandomXDLL.randomx_init_dataset(_Dataset, _Cache, start, count);
                                finishCount++;
                            }
                            )
                            );
                            startItem += count;
                            thread.Start(null);
                        }
                        while (finishCount < initThreadCount)
                        {
                            Thread.Sleep(10);
                        }
                    }

                    //Log.Debug("randomx_init ok");
                }
            }
        }

        private static ThreadLocalID<byte[]> _buffer = new ThreadLocalID<byte[]>( ()=> { return new byte[HashSize]; } );

        public static byte[] CaculateHash(byte[] input)
        {
            randomx_init();

            var buffer = _buffer.Value;
            RandomXDLL.randomx_calculate_hash(_Machine.Value, input, Convert.ToUInt32(input.Length), buffer);
            return buffer;
        }

        public static string CaculateHash(string text)
        {
            randomx_init();

            var input = text.HexToBytes();
            var buffer = _buffer.Value;
            RandomXDLL.randomx_calculate_hash( _Machine.Value, input, Convert.ToUInt32(input.Length), buffer );
            return buffer.ToHexString();
        }

        public static void Test1(string[] args)
        {
            var hash1 = "30733122ab51a9dee9ad688d4f5e9d239ef326ba05fc35d1310f2d43833d3c32";
            var hash2 = "d86ad5be987fa577f8ce77120b424ab3dc91893810bd6cd03407f75fc89339e2";

            for (uint ii = 0; ii < 150000; ii++)
            {
                Thread thread = new Thread(new ParameterizedThreadStart(
                (object data) =>
                {
                    hash2 = BlockDag.ToHash(1, hash1, hash2);
                    hash2 = BlockDag.ToHash(1+ 141222, hash1, hash2);

                    // 内存泄漏
                    hash2 = BlockDag.ToHash(1, hash1, hash2);
                    hash2 = BlockDag.ToHash(1 + 141222, hash1, hash2);

                    Console.WriteLine(hash2);
                }
                )
                );
                thread.Start(null);
                Thread.Sleep(30);
                GC.Collect();
            }

            while (true)
            {
                Thread.Sleep(10);
            }

        }
    }


}
