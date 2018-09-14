# GroBuf

[![NuGet Status](https://img.shields.io/nuget/v/GroBuf.svg)](https://www.nuget.org/packages/GroBuf/)
[![Build Status](https://travis-ci.org/skbkontur/GroBuf.svg?branch=master)](https://travis-ci.org/skbkontur/GroBuf)
[![Build status](https://ci.appveyor.com/api/projects/status/xbq92fnsfbo8946k?svg=true)](https://ci.appveyor.com/project/skbkontur/grobuf)

GroBuf is a fast binary serializer for .NET.

## Example

Imagine a simple class hierarchy:

```
public class Car
{
    public Guid? Id { get; set; }
    public string Manufacturer { get; set; }
    public CarKind Kind { get; set; }
    public Wheel[] Wheels { get; set; }
}
public class Wheel
{
    public double Radius { get; set; }
    public double Width { get; set; }
    public double Weight { get; set; }
}
public enum CarKind : byte
{
    Sedan,
    Hatchback,
    Limousine,
    Van
}
```

### Creating a serializer
In order to obtain maximum speed it is strongly recommended to once create a serializer as it uses dynamic code generation for serializers/deserializers.

```
var serializer = new Serializer(new PropertiesExtractor(), options : GroBufOptions.WriteEmptyObjects);
```

Here we create serializer in order to read/write all public properties.
By default GroBuf skips objects which are empty (an object is considered empty if it is an array with zero length or if all its members are empty). The [GroBufOptions.WriteEmptyObjects](https://github.com/homuroll/GroBuf/blob/master/GroBuf/GroBuf/GroBufOptions.cs) options says GroBuf to write all data as is.

### Serializing/Deserializing
GroBuf serializes objects to binary format and returns byte[], deserializes from byte[]:
```
var car = new Car
              {
                  Id = Guid.NewGuid(),
                  Manufacturer = "zzz",
                  Kind = CarKind.Limousine,
                  Wheels = new[] { new Wheel {Radius = 19.1, Width = 5.2, Weight = 16.9} }
              };
byte[] data = serializer.Serialize(car);
var zcar = serializer.Deserialize<Car>(data);
```

## Selecting members to serialize
It is possible to create a serializer with custom data members selection.
These are predefined extractors:
 - PropertiesExtractor - selects all public properties
 - FieldsExtractor - selects all public fields
 - AllPropertiesExtractor - selects both public and private properties
 - AllFieldsExtractor - selects both public and private fields
 - DataMembersByAttributeExtractor - selects all members marked with [DataMember](http://msdn.microsoft.com/en-us/library/system.runtime.serialization.datamemberattribute.aspx) attribute

## Notes on types
Supports:
 - custom classes or structs
 - primitive types
 - single dimension arrays
 - List<>, HashSet<>, Hashtable, Dictionary<,>
 - Lazy<> (it will not be deserialized untill Value is actually demanded)

Serialized types names are not used and therefore the types can be safely renamed without any loss of data.

All primitive types are convertible into ecch other. For example, if a data contract member had type int and has been changed to long than no old data will be lost.

## Notes on members
The members's names are important for GroBuf because it stores hash codes of all serialized members and uses them during deserialization. But it is possible to tell GroBuf what hash code is to be used for a particular member using [GroboMember](https://github.com/homuroll/GroBuf/blob/master/GroBuf/GroBuf/DataMembersExtracters/GroboMemberAttribute.cs) attribute.
If a member's name changes (and there is no [GroboMember](https://github.com/homuroll/GroBuf/blob/master/GroBuf/GroBuf/DataMembersExtracters/GroboMemberAttribute.cs) attribute at it) or a member has been deleted, old data still may be deserialized but the data of that particular member will be skipped and lost.
If a member has been added than after deserializing old data, the value of this member will be set to its default value.

## Notes on enums
Enums are stored not as ints but as hash codes for their string representation. Thus, one can safely change the value of enum, but change fo a name will result in loss of data (soon it will be possibile to specify the hash code of a enum member manually).

## Performance
GroBuf is faster than a well-known serializer ProtoBuf:
 - about 2-2.5 times on average on serialization
 - about 4-5 times on average on deserialization

Here you can see an example of benchmarking in a realistic scenario:

```ini
BenchmarkDotNet-Dev=v0.9.6.0+
OS=Microsoft Windows NT 6.1.7601 Service Pack 1
Processor=Intel(R) Core(TM) i7-2600K CPU 3.40GHz, ProcessorCount=8
Frequency=3312861 ticks, Resolution=301.8539 ns, Timer=TSC
HostCLR=MS.NET 4.0.30319.42000, Arch=64-bit RELEASE [RyuJIT]
JitModules=clrjit-v4.6.1076.0

Type=ProtoBufvsGroBufRunner  Mode=Throughput  

              Method | Platform |       Jit | Runtime |      Median |    StdDev |
-------------------- |--------- |---------- |-------- |------------ |---------- |
     GroBufSerialize |     Host |      Host |    Mono |  79.0364 us | 0.6419 us |
   GroBufDeserialize |     Host |      Host |    Mono |  17.6128 us | 0.1588 us |
   ProtoBufSerialize |     Host |      Host |    Mono | 184.1507 us | 1.7764 us |
 ProtoBufDeserialize |     Host |      Host |    Mono |  72.3540 us | 2.2473 us |
     GroBufSerialize |      X64 | LegacyJit |    Host |  52.2340 us | 2.1481 us |
   GroBufDeserialize |      X64 | LegacyJit |    Host |   8.9056 us | 0.5137 us |
   ProtoBufSerialize |      X64 | LegacyJit |    Host | 136.6818 us | 5.9596 us |
 ProtoBufDeserialize |      X64 | LegacyJit |    Host |  38.0563 us | 1.5057 us |
     GroBufSerialize |      X64 |    RyuJit |    Host |  49.3948 us | 1.5682 us |
   GroBufDeserialize |      X64 |    RyuJit |    Host |   9.4304 us | 0.3034 us |
   ProtoBufSerialize |      X64 |    RyuJit |    Host | 136.9129 us | 5.1180 us |
 ProtoBufDeserialize |      X64 |    RyuJit |    Host |  37.7057 us | 0.5454 us |
     GroBufSerialize |      X86 | LegacyJit |    Host |  60.7610 us | 0.5057 us |
   GroBufDeserialize |      X86 | LegacyJit |    Host |  12.2245 us | 0.1467 us |
   ProtoBufSerialize |      X86 | LegacyJit |    Host | 156.2833 us | 3.5322 us |
 ProtoBufDeserialize |      X86 | LegacyJit |    Host |  41.5833 us | 0.5682 us |
```

The disadvantages are:
 - because of simpler format the size of data produced by GroBuf is 1.5-2 times larger than ProtoBuf's. But it is planned to be optimized in the future
 - lack of ProtoBuf's extensions

## Release Notes

See [CHANGELOG](CHANGELOG.md).
