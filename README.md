# dotnet-load-native
Dynamic library loader for .NET Standard

This is a prototype or spike for creating a runtime loader for native functions into managed .NET Standard code.
It is a riff off of https://github.com/mellinoe/nativelibraryloader.

At issue is loading libraries based on both platform and 32- and 64-bit-ness. The `DllImport` attribute doesn't get us there.

## Conclusion

In the end, I don't think the solution contained in this repo is something to pursue. I think it's more managable to create an interface of a cohesive set of functions, then create one concrete type for each supported platform/bitness; the concrete type then has it's own `DllImport` allowing it to differentiate on platform and bitness. Finally, a small factory is needed to fetch the appropriate concrete type based on platform and bitness.

> Now that I think of it, that assumes virtualness on p/invoked methods, which seems highly unlikely. So, no proven method yet.

## Related Content

[Handling p/invokes for different platforms and discussions about dllmap](https://github.com/dotnet/coreclr/issues/930)
Discussion of the topic in the dotnet coreclr repo.

[The .NET Native Tool Chain](https://blogs.msdn.microsoft.com/dotnet/2014/05/09/the-net-native-tool-chain/)
A discussion of the Marshaling Code Generator (MCG) exists here.
