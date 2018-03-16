namespace dotnet_load_native

open System
open System.Runtime.InteropServices

type LibraryPath    = string
type LibraryHandle  = System.IntPtr

// Assumptions:
//
//      - the over-loaded libraries are controlled by us and have the same names.
//      - we're okay with loading once and holding the libs and functions forever.
//      - holding multiple copies of libs open with, say, a half dozen functions is fine.

module internal Platform =

    let Key = (Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture)

module internal PlatformLoaders =

    type FunctionName   = string
    type FunctionPtr    = IntPtr

    module internal Win32 =

        [<DllImport("Kernel32.dll", EntryPoint="LoadLibrary", CharSet = CharSet.Unicode)>]
        extern IntPtr loadLibrary(string path);

        [<DllImport("Kernel32.dll", EntryPoint="GetProcAddress", CharSet = CharSet.Ansi)>]
        extern IntPtr loadFunction(IntPtr libHandle, string funName);

    module internal Nix =

        let loadLibrary path = failwith "nyi Nix.loadLibrary"
        let loadFunction (libHandle, funName) = failwith "nyi Nix.loadFunction"

    // Lookup table for getting a loader for the specified platform/architecture.
    // Logically, the key is the tuple
    //
    //      (System.PlatformID, System.Runtime.InteropServices.Architecture),
    //
    // and the value is a function that returns the loaders tuple, or
    //
    //      (LoadLibraryFn, LoadFunctionFn).
    //
    let loaderLookup =
        [|
            (PlatformID.Win32NT, Architecture.X86), (Win32.loadLibrary, Win32.loadFunction)
            (PlatformID.Win32NT, Architecture.X64), (Win32.loadLibrary, Win32.loadFunction)
            (PlatformID.Unix,    Architecture.X64), (Nix.loadLibrary,   Nix.loadFunction)
        |]
        |> Map.ofArray

    let getPlatformLoaders () =
        if loaderLookup.ContainsKey(Platform.Key) then
            loaderLookup.[Platform.Key]
        else
            failwithf "Unsupported platform/architecture: %A" Platform.Key

    let platformLoaders = lazy (getPlatformLoaders())

module Loader =

    type FunctionVariants = Map<PlatformID * Architecture, string>

    let loadFunction<'F> funName (variants : FunctionVariants) : 'F =

        let getVariantLibPath () : string =

            if variants.ContainsKey(Platform.Key) then
                variants.[Platform.Key]
            else
                failwithf "Unsupported platform/architecture: %A" Platform.Key

        let (loadLib, loadFn) = PlatformLoaders.platformLoaders.Value
        let libPath = getVariantLibPath()
        let libHandle = loadLib libPath
        let funPtr = loadFn(libHandle, funName)
        let dele = Marshal.GetDelegateForFunctionPointer<'F>(funPtr)

        dele

module Example =

    let exFunctionVariants =
        [|
            (PlatformID.Win32NT, Architecture.X86), "Kernel32.dll"
            (PlatformID.Win32NT, Architecture.X64), "Kernel32.dll"
            (PlatformID.Unix,    Architecture.X64), "x64-nux/lib.so"
        |]
        |> Map.ofArray

    type FuncSig = delegate of [<MarshalAs(UnmanagedType.LPWStr)>] msg : string -> unit

    let f = Loader.loadFunction<FuncSig> "OutputDebugStringW" exFunctionVariants
    f.Invoke("Hello, ods")
