#GroBuf

GroBuf is a fast binary serializer for .NET.

##Example

Suppose some simple class hierarchy:

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

##Creating serializer
In order to obtain maximum speed it is strongly recommended to create serializer once since it uses dynamic code generation for serializers/deserializers.

```
var serializer = new Serializer(new PropertiesExtractor(), options : GroBufOptions.WriteEmptyObjects);
```

Here we create serializer to read/write all public properties.
By default GroBuf skips objects which are empty (an object is considered empty if it is an array with zero length or if all its members are empty). The [GroBufOptions.WriteEmptyObjects](https://github.com/homuroll/GroBuf/blob/master/GroBuf/GroBuf/GroBufOptions.cs) options says GroBuf to write all data as is.

##Serializing/Deserializing
GroBuf serializes objects to binary format and return byte[] and deserializes from byte[]
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

##Selecting members to serialize
It is possible to create serializer with custom data members selection.
These are predefined extractors:
 - PropertiesExtractor - selects all public properties
 - FieldsExtractor - selects all public fields
 - AllPropertiesExtractor - selects both public and private properties
 - AllFieldsExtractor - selects both public and private fields
 - DataMembersByAttributeExtractor - selects all members marked with [DataMember] attribute

##Notes on types
Supported:
 - custom classes or structs
 - primitive types
 - single dimension arrays
 - List<>, HashSet<>, Hashtable, Dictionary<,>
Names of serialized types are not used and therefore can be safely renamed without any loss of data.

All primitive types are convertible to one another. For example, if a data contract member had type int and has been changed to long than no old data will be lost.

##Notes on members
The members's names are important for GroBuf because it stores hash codes of all serialized members and uses them during deserialization. But it is possible to tell GroBuf what hash code to use for a particular member using [GroboMember] attribute.
If a member's name changes (and there is no [GroboMember] attribute at it) or a member has been deleted, old data still will be able to be deserialized but the data of that particular member will be skipped and lost.
If a member has been added than after deserializing old data the value of this member will be set to its default value.

##Performance
GroBuf is faster than well-known serializer ProtoBuf:

Here example of benchmarking on some realistic scenario:

```ini

BenchmarkDotNet-Dev=v0.9.6.0+
OS=Microsoft Windows NT 6.1.7601 Service Pack 1
Processor=Intel(R) Core(TM) i7-2600K CPU 3.40GHz, ProcessorCount=8
Frequency=3312861 ticks, Resolution=301.8539 ns, Timer=TSC
HostCLR=MS.NET 4.0.30319.42000, Arch=64-bit RELEASE [RyuJIT]
JitModules=clrjit-v4.6.1076.0

Type=ProtoBufvsGroBufRunner  Mode=Throughput  

```
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

The downside is: because of simpler format the size of data produced by GroBuf is 1.5-2 times larger than ProtoBuf's. But this may be optimized in the future.
