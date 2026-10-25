using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Broiler.Regex.Architecture.Tests;

/// <summary>
/// A static field an assembly declares, with what SL2 asks of it. A static auto-property's backing field,
/// <c>&lt;Name&gt;k__BackingField</c>, is named for its property, as an instance one is.
/// </summary>
internal sealed record StaticField(string Type, string Name, string FieldType, bool IsReadOnly, bool IsLiteral);

/// <summary>
/// An instance field a class of the assembly declares, an auto-property's backing field named for its property:
/// what SL2 reads to tell whether an object a static field holds can change after construction.
/// </summary>
internal sealed record InstanceField(string Type, string Name, string FieldType, bool IsReadOnly);

/// <summary>
/// A place in a source file: the document of the sequence point the compiler wrote for an IL instruction, relative
/// to the repository root, and the lines that sequence point spans, which are one statement's. An instruction the
/// compiler wrote no line for has the place <c>&lt;no source line: Type::Method&gt;</c>, which no citation matches.
/// </summary>
internal sealed record SourcePlace(string Path, int StartLine, int EndLine)
{
    internal bool Contains(string path, int line) => Path == path && line >= StartLine && line <= EndLine;

    public override string ToString() =>
        Path + ":" + StartLine.ToString(CultureInfo.InvariantCulture)
        + (EndLine == StartLine ? string.Empty : "-" + EndLine.ToString(CultureInfo.InvariantCulture));
}

/// <summary>
/// One assembly's metadata tables, read without loading it. Shaped after Broiler.VM's
/// <c>AssemblyFacts</c>, with member references carrying their parameter types, because SL4
/// tells a culture-sensitive overload from an ordinal one by its signature.
/// </summary>
/// <remarks>
/// Two readings go beyond the tables, and both read the method bodies' IL. <see cref="PlacesOf"/> says where in the
/// source an instruction names a member, a type or an assembly, from the portable PDB's sequence points, so that the
/// starting violation list is asserted place by place. <see cref="StaticInitializerCreations"/> says what the static
/// constructor creates for each static field, so that SL2 sees a mutable collection a read-only interface hides.
/// </remarks>
internal sealed class AssemblyFacts
{
    private AssemblyFacts(string name)
    {
        Name = name;
    }

    internal string Name { get; }

    /// <summary>Every assembly named in the AssemblyRef table.</summary>
    internal ImmutableArray<string> AssemblyReferences { get; private init; }

    /// <summary>Every type named in the TypeRef table, as <c>Namespace.Type</c>, nested types as <c>Outer+Inner</c>.</summary>
    internal ImmutableArray<string> TypeReferences { get; private init; }

    /// <summary>
    /// Every member reference: a method as <c>Namespace.Type.Name(ParameterType,...)</c>, a field as
    /// <c>Namespace.Type.Name</c>. A generic type's members are named on its definition,
    /// <c>System.Collections.Generic.List`1.Add(!0)</c>.
    /// </summary>
    internal ImmutableArray<string> MemberReferences { get; private init; }

    /// <summary>Every custom attribute type applied anywhere in the assembly.</summary>
    internal ImmutableArray<string> CustomAttributeTypes { get; private init; }

    /// <summary>Every platform invoke the assembly declares, as <c>module!entryPoint</c>.</summary>
    internal ImmutableArray<string> PlatformInvokes { get; private init; }

    /// <summary>Every static field a type the source declares holds; compiler-generated types are left out.</summary>
    internal ImmutableArray<StaticField> StaticFields { get; private init; }

    /// <summary>Every static property with a setter a type the source declares, as <c>Namespace.Type.Name</c>.</summary>
    internal ImmutableArray<string> SettableStaticProperties { get; private init; }

    /// <summary>Every instance field of every class the source declares, in declaration order; structs, enums and compiler-generated types are left out.</summary>
    internal ImmutableArray<InstanceField> InstanceFields { get; private init; }

    /// <summary>Every class the source declares, by name; structs, enums, interfaces and compiler-generated types are left out.</summary>
    internal ImmutableHashSet<string> Classes { get; private init; } = ImmutableHashSet<string>.Empty;

    /// <summary>The base type of every class the source declares, by name, for the classes whose base is not <c>System.Object</c>.</summary>
    internal ImmutableDictionary<string, string> BaseTypes { get; private init; } = ImmutableDictionary<string, string>.Empty;

    /// <summary>
    /// For each static field a static constructor stores, as <c>Namespace.Type.Field</c>, the types that the
    /// instructions computing the stored value create: every base class library type constructed, every type a base
    /// class library call returns, and every array allocated with a length other than a constant zero, in that span
    /// of the static constructor and in every method of this assembly it calls or constructs through, transitively.
    /// <c>Array.Empty</c>'s array is not one: it has no element to change. It over-approximates what the field holds,
    /// as a temporary is counted too, and it is what SL2 reads where a declared type cannot decide.
    /// </summary>
    internal ImmutableDictionary<string, ImmutableArray<string>> StaticInitializerCreations { get; private init; } =
        ImmutableDictionary<string, ImmutableArray<string>>.Empty;

    /// <summary>Whether a portable PDB was read with the assembly, so that <see cref="PlacesOf"/> can answer.</summary>
    internal bool HasPlaces { get; private init; }

    private ImmutableDictionary<string, ImmutableArray<SourcePlace>> Places { get; init; } =
        ImmutableDictionary<string, ImmutableArray<SourcePlace>>.Empty;

    /// <summary>
    /// The places in the source where an IL instruction names <paramref name="key"/>: <c>member:</c> and a
    /// member reference as <see cref="MemberReferences"/> words it; <c>type:</c> and a type as
    /// <see cref="TypeReferences"/> words it, named by an instruction's type operand or as the declaring type of
    /// its member operand; <c>assembly:</c> and an assembly, whose type an instruction names so. A type that only a
    /// signature or a generic argument names has no IL place. Null when no PDB was read, so that a caller tells "no
    /// place" from "not read".
    /// </summary>
    internal IReadOnlyList<SourcePlace>? PlacesOf(string key) =>
        !HasPlaces ? null : Places.TryGetValue(key, out var places) ? places : [];

    /// <summary>Reads an assembly and, when one is beside it or embedded in it, its portable PDB.</summary>
    internal static AssemblyFacts Read(string path)
    {
        var pdb = Path.ChangeExtension(path, ".pdb");
        return Read(File.ReadAllBytes(path), File.Exists(pdb) ? File.ReadAllBytes(pdb) : null);
    }

    /// <summary>Reads an assembly image and the portable PDB given, or else the one embedded in the image, if any.</summary>
    internal static AssemblyFacts Read(byte[] image, byte[]? pdbImage = null)
    {
        using var reader = new PEReader(ImmutableArray.Create(image));
        var metadata = reader.GetMetadataReader();
        var provider = new SignatureNames(metadata);
        var name = metadata.GetString(metadata.GetAssemblyDefinition().Name);

        var assemblyReferences = metadata.AssemblyReferences
            .Select(h => metadata.GetString(metadata.GetAssemblyReference(h).Name));

        var typeReferences = metadata.TypeReferences
            .Select(h => SignatureNames.Qualified(metadata, h));

        var memberReferences = metadata.MemberReferences
            .Select(h => DescribeMember(metadata, provider, metadata.GetMemberReference(h)));

        var attributes = metadata.CustomAttributes
            .Select(h => AttributeType(metadata, metadata.GetCustomAttribute(h)))
            .OfType<string>();

        var invokes = metadata.MethodDefinitions
            .Select(metadata.GetMethodDefinition)
            .Where(static d => (d.Attributes & MethodAttributes.PinvokeImpl) != 0)
            .Select(d =>
            {
                var import = d.GetImport();
                var module = import.Module.IsNil ? string.Empty : metadata.GetString(metadata.GetModuleReference(import.Module).Name);
                return module + "!" + metadata.GetString(import.Name.IsNil ? d.Name : import.Name);
            });

        var statics = new List<StaticField>();
        var settable = new List<string>();
        var instanceFields = new List<InstanceField>();
        var classes = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        var baseTypes = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var handle in metadata.TypeDefinitions)
        {
            var type = metadata.GetTypeDefinition(handle);
            var typeName = SignatureNames.Qualified(metadata, handle);
            if (IsCompilerGenerated(typeName))
                continue;

            var baseType = type.BaseType.IsNil ? null : BaseTypeName(metadata, provider, type.BaseType);
            var isClass = (type.Attributes & TypeAttributes.Interface) == 0
                && baseType is not ("System.ValueType" or "System.Enum" or null);
            if (isClass)
                classes.Add(typeName);
            if (isClass && baseType is not "System.Object")
                baseTypes[typeName] = baseType!;

            foreach (var fieldHandle in type.GetFields())
            {
                var field = metadata.GetFieldDefinition(fieldHandle);
                var fieldName = BackedProperty(metadata.GetString(field.Name));
                if ((field.Attributes & FieldAttributes.Static) == 0)
                {
                    if (isClass)
                    {
                        instanceFields.Add(new InstanceField(
                            typeName,
                            fieldName,
                            field.DecodeSignature(provider, null),
                            (field.Attributes & FieldAttributes.InitOnly) != 0));
                    }

                    continue;
                }

                // A static auto-property's backing field is a static field like any other: a get-only one of a
                // mutable type is mutable state although no setter exists.
                statics.Add(new StaticField(
                    typeName,
                    fieldName,
                    field.DecodeSignature(provider, null),
                    (field.Attributes & FieldAttributes.InitOnly) != 0,
                    (field.Attributes & FieldAttributes.Literal) != 0));
            }

            foreach (var propertyHandle in type.GetProperties())
            {
                var property = metadata.GetPropertyDefinition(propertyHandle);
                var setter = property.GetAccessors().Setter;
                if (!setter.IsNil && (metadata.GetMethodDefinition(setter).Attributes & MethodAttributes.Static) != 0)
                    settable.Add(typeName + "." + metadata.GetString(property.Name));
            }
        }

        using var pdbProvider = OpenPdb(reader, pdbImage);
        var bodies = new MethodBodies(reader, metadata, provider, pdbProvider?.GetMetadataReader());

        return new AssemblyFacts(name)
        {
            AssemblyReferences = Sorted(assemblyReferences),
            TypeReferences = Sorted(typeReferences),
            MemberReferences = Sorted(memberReferences),
            CustomAttributeTypes = Sorted(attributes),
            PlatformInvokes = Sorted(invokes),
            StaticFields = [.. statics],
            SettableStaticProperties = Sorted(settable),
            InstanceFields = [.. instanceFields],
            Classes = classes.ToImmutable(),
            BaseTypes = baseTypes.ToImmutable(),
            StaticInitializerCreations = bodies.StaticInitializerCreations(),
            HasPlaces = pdbProvider is not null,
            Places = pdbProvider is null ? ImmutableDictionary<string, ImmutableArray<SourcePlace>>.Empty : bodies.Places(),
        };
    }

    private static MetadataReaderProvider? OpenPdb(PEReader reader, byte[]? pdbImage)
    {
        if (pdbImage is not null)
            return MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(pdbImage));
        foreach (var entry in reader.ReadDebugDirectory())
        {
            if (entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb)
                return reader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
        }

        return null;
    }

    /// <summary>An auto-property's backing field, <c>&lt;Name&gt;k__BackingField</c>, as its property's name; any other field as itself.</summary>
    private static string BackedProperty(string fieldName) =>
        fieldName.StartsWith('<') && fieldName.EndsWith(">k__BackingField", StringComparison.Ordinal)
            ? fieldName[1..fieldName.IndexOf('>', StringComparison.Ordinal)]
            : fieldName;

    private static string BaseTypeName(MetadataReader metadata, SignatureNames provider, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.TypeReference => SignatureNames.Qualified(metadata, (TypeReferenceHandle)handle),
        HandleKind.TypeDefinition => SignatureNames.Qualified(metadata, (TypeDefinitionHandle)handle),
        HandleKind.TypeSpecification => GenericDefinition(metadata.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(provider, null)),
        _ => "?",
    };

    /// <summary>A type the compiler made rather than the source: a closure, a lambda cache, a state machine, private implementation details.</summary>
    internal static bool IsCompilerGenerated(string qualifiedName) => qualifiedName.Contains('<', StringComparison.Ordinal);

    private static ImmutableArray<string> Sorted(IEnumerable<string> values) =>
        [.. values.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    private static string DescribeMember(MetadataReader metadata, SignatureNames provider, MemberReference member)
    {
        var name = DescribeParent(metadata, provider, member.Parent) + "." + metadata.GetString(member.Name);
        if (member.GetKind() != MemberReferenceKind.Method)
            return name;
        var signature = member.DecodeMethodSignature(provider, null);
        return name + "(" + string.Join(",", signature.ParameterTypes) + ")";
    }

    /// <summary>A member reference's declaring type, a generic one on its definition.</summary>
    private static string DescribeParent(MetadataReader metadata, SignatureNames provider, EntityHandle parent) => parent.Kind switch
    {
        HandleKind.TypeReference => SignatureNames.Qualified(metadata, (TypeReferenceHandle)parent),
        HandleKind.TypeDefinition => SignatureNames.Qualified(metadata, (TypeDefinitionHandle)parent),
        HandleKind.TypeSpecification => GenericDefinition(metadata.GetTypeSpecification((TypeSpecificationHandle)parent).DecodeSignature(provider, null)),
        _ => "?",
    };

    /// <summary><c>System.Collections.Generic.List`1&lt;System.String&gt;</c> as <c>System.Collections.Generic.List`1</c>.</summary>
    private static string GenericDefinition(string instantiated)
    {
        var bracket = instantiated.IndexOf('<', StringComparison.Ordinal);
        return bracket < 0 ? instantiated : instantiated[..bracket];
    }

    private static string? AttributeType(MetadataReader metadata, CustomAttribute attribute)
    {
        EntityHandle parent;
        if (attribute.Constructor.Kind == HandleKind.MemberReference)
            parent = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
        else if (attribute.Constructor.Kind == HandleKind.MethodDefinition)
            parent = metadata.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
        else
            return null;

        return parent.Kind switch
        {
            HandleKind.TypeReference => SignatureNames.Qualified(metadata, (TypeReferenceHandle)parent),
            HandleKind.TypeDefinition => SignatureNames.Qualified(metadata, (TypeDefinitionHandle)parent),
            _ => null,
        };
    }

    /// <summary>One IL instruction: its offset, its opcode, and its operand when that is a token or an integer constant.</summary>
    private readonly record struct Instruction(int Offset, ILOpCode OpCode, int Operand);

    /// <summary>The method bodies of one assembly, decoded once, and what the two IL readings make of them.</summary>
    private sealed class MethodBodies
    {
        private readonly MetadataReader metadata;
        private readonly SignatureNames provider;
        private readonly MetadataReader? pdb;
        private readonly Dictionary<MethodDefinitionHandle, List<Instruction>> instructions = [];
        private readonly Dictionary<MethodDefinitionHandle, HashSet<string>?> creations = [];
        private readonly Dictionary<TypeReferenceHandle, string?> assemblies = [];

        internal MethodBodies(PEReader reader, MetadataReader metadata, SignatureNames provider, MetadataReader? pdb)
        {
            this.metadata = metadata;
            this.provider = provider;
            this.pdb = pdb;
            foreach (var handle in metadata.MethodDefinitions)
            {
                var method = metadata.GetMethodDefinition(handle);
                if (method.RelativeVirtualAddress != 0)
                    instructions[handle] = Decode(reader.GetMethodBody(method.RelativeVirtualAddress).GetILReader());
            }
        }

        internal ImmutableDictionary<string, ImmutableArray<SourcePlace>> Places()
        {
            var places = new Dictionary<string, HashSet<SourcePlace>>(StringComparer.Ordinal);
            foreach (var (handle, body) in instructions)
            {
                var points = SequencePoints(handle);
                foreach (var instruction in body.Where(static i => CarriesToken(i.OpCode)))
                {
                    var keys = KeysOf(MetadataTokens.EntityHandle(instruction.Operand)).ToList();
                    if (keys.Count == 0)
                        continue;
                    var place = PlaceOf(handle, points, instruction.Offset);
                    foreach (var key in keys)
                    {
                        if (!places.TryGetValue(key, out var set))
                            places[key] = set = [];
                        set.Add(place);
                    }
                }
            }

            return places.ToImmutableDictionary(
                static p => p.Key,
                static p => p.Value.OrderBy(static s => s.Path, StringComparer.Ordinal).ThenBy(static s => s.StartLine).ThenBy(static s => s.EndLine).ToImmutableArray(),
                StringComparer.Ordinal);
        }

        internal ImmutableDictionary<string, ImmutableArray<string>> StaticInitializerCreations()
        {
            var result = ImmutableDictionary.CreateBuilder<string, ImmutableArray<string>>(StringComparer.Ordinal);
            foreach (var (handle, body) in instructions)
            {
                var method = metadata.GetMethodDefinition(handle);
                if (metadata.GetString(method.Name) != ".cctor")
                    continue;

                // The value a stsfld stores is computed by the instructions since the previous one.
                var start = 0;
                for (var i = 0; i < body.Count; i++)
                {
                    if (body[i].OpCode != ILOpCode.Stsfld)
                        continue;
                    var target = MetadataTokens.EntityHandle(body[i].Operand);
                    if (target.Kind == HandleKind.FieldDefinition)
                    {
                        var field = metadata.GetFieldDefinition((FieldDefinitionHandle)target);
                        var key = SignatureNames.Qualified(metadata, field.GetDeclaringType()) + "." + BackedProperty(metadata.GetString(field.Name));
                        var created = new HashSet<string>(StringComparer.Ordinal);
                        Creations(body, start, i, created, []);
                        result[key] = [.. created.Order(StringComparer.Ordinal)];
                    }

                    start = i + 1;
                }
            }

            return result.ToImmutable();
        }

        /// <summary>What instructions <paramref name="from"/> to <paramref name="to"/> of a body create, following this assembly's methods.</summary>
        private void Creations(List<Instruction> body, int from, int to, HashSet<string> created, HashSet<MethodDefinitionHandle> visiting)
        {
            for (var i = from; i < to; i++)
            {
                var instruction = body[i];
                switch (instruction.OpCode)
                {
                    case ILOpCode.Newarr when !(i > 0 && IsConstantZero(body[i - 1])):
                        created.Add(TypeOperand(MetadataTokens.EntityHandle(instruction.Operand)) + "[]");
                        break;
                    case ILOpCode.Newobj or ILOpCode.Call or ILOpCode.Callvirt:
                        var target = MetadataTokens.EntityHandle(instruction.Operand);
                        if (target.Kind == HandleKind.MethodSpecification)
                            target = metadata.GetMethodSpecification((MethodSpecificationHandle)target).Method;
                        if (target.Kind == HandleKind.MethodDefinition)
                        {
                            created.UnionWith(MethodCreations((MethodDefinitionHandle)target, visiting));
                        }
                        else if (target.Kind == HandleKind.MemberReference)
                        {
                            var member = metadata.GetMemberReference((MemberReferenceHandle)target);
                            if (member.GetKind() != MemberReferenceKind.Method)
                                break;
                            if (instruction.OpCode == ILOpCode.Newobj)
                            {
                                created.Add(FullParent(member.Parent));
                            }
                            else if (DescribeParent(metadata, provider, member.Parent) + "." + metadata.GetString(member.Name) != "System.Array.Empty")
                            {
                                var returned = member.DecodeMethodSignature(provider, null).ReturnType;
                                if (returned != "System.Void")
                                    created.Add(returned);
                            }
                        }

                        break;
                }
            }
        }

        private HashSet<string> MethodCreations(MethodDefinitionHandle handle, HashSet<MethodDefinitionHandle> visiting)
        {
            if (creations.TryGetValue(handle, out var known) && known is not null)
                return known;
            if (!instructions.TryGetValue(handle, out var body) || !visiting.Add(handle))
                return [];
            var created = new HashSet<string>(StringComparer.Ordinal);
            Creations(body, 0, body.Count, created, visiting);
            visiting.Remove(handle);
            creations[handle] = created;
            return created;
        }

        private static bool IsConstantZero(Instruction instruction) =>
            instruction.OpCode == ILOpCode.Ldc_i4_0
            || instruction.OpCode is ILOpCode.Ldc_i4_s or ILOpCode.Ldc_i4 && instruction.Operand == 0;

        private string FullParent(EntityHandle parent) => parent.Kind switch
        {
            HandleKind.TypeSpecification => metadata.GetTypeSpecification((TypeSpecificationHandle)parent).DecodeSignature(provider, null),
            _ => DescribeParent(metadata, provider, parent),
        };

        private string TypeOperand(EntityHandle handle) => handle.Kind switch
        {
            HandleKind.TypeReference => SignatureNames.Qualified(metadata, (TypeReferenceHandle)handle),
            HandleKind.TypeDefinition => SignatureNames.Qualified(metadata, (TypeDefinitionHandle)handle),
            HandleKind.TypeSpecification => metadata.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(provider, null),
            _ => "?",
        };

        /// <summary>The keys of <see cref="PlacesOf"/> an operand names: a referenced member, its declaring type and outer types, their assembly.</summary>
        private IEnumerable<string> KeysOf(EntityHandle operand)
        {
            if (operand.Kind == HandleKind.MethodSpecification)
                operand = metadata.GetMethodSpecification((MethodSpecificationHandle)operand).Method;

            EntityHandle type;
            switch (operand.Kind)
            {
                case HandleKind.MemberReference:
                    var member = metadata.GetMemberReference((MemberReferenceHandle)operand);
                    yield return "member:" + DescribeMember(metadata, provider, member);
                    type = member.Parent;
                    break;
                case HandleKind.TypeReference:
                case HandleKind.TypeSpecification:
                    type = operand;
                    break;
                default:
                    yield break;
            }

            if (type.Kind == HandleKind.TypeSpecification)
            {
                if (GenericTypeOf((TypeSpecificationHandle)type) is not { } generic)
                    yield break;
                type = generic;
            }

            if (type.Kind != HandleKind.TypeReference)
                yield break;
            var reference = (TypeReferenceHandle)type;
            for (var current = reference; ;)
            {
                yield return "type:" + SignatureNames.Qualified(metadata, current);
                var scope = metadata.GetTypeReference(current).ResolutionScope;
                if (scope.Kind != HandleKind.TypeReference)
                    break;
                current = (TypeReferenceHandle)scope;
            }

            if (AssemblyOf(reference) is { } assembly)
                yield return "assembly:" + assembly;
        }

        /// <summary>The type reference a type specification is built on: a generic instantiation's type, an array's or a pointer's element type.</summary>
        private EntityHandle? GenericTypeOf(TypeSpecificationHandle handle)
        {
            var blob = metadata.GetBlobReader(metadata.GetTypeSpecification(handle).Signature);
            while (true)
            {
                var code = blob.ReadSignatureTypeCode();
                switch (code)
                {
                    case SignatureTypeCode.GenericTypeInstance:
                        blob.ReadSignatureTypeCode();
                        return blob.ReadTypeHandle();
                    case SignatureTypeCode.SZArray or SignatureTypeCode.Pointer or SignatureTypeCode.ByReference or SignatureTypeCode.Pinned:
                        continue;
                    case SignatureTypeCode.Array:
                        continue;
                    case SignatureTypeCode.TypeHandle:
                        return blob.ReadTypeHandle();
                    default:
                        return null;
                }
            }
        }

        private string? AssemblyOf(TypeReferenceHandle handle)
        {
            if (assemblies.TryGetValue(handle, out var known))
                return known;
            var scope = metadata.GetTypeReference(handle).ResolutionScope;
            var assembly = scope.Kind switch
            {
                HandleKind.TypeReference => AssemblyOf((TypeReferenceHandle)scope),
                HandleKind.AssemblyReference => metadata.GetString(metadata.GetAssemblyReference((AssemblyReferenceHandle)scope).Name),
                _ => null,
            };
            assemblies[handle] = assembly;
            return assembly;
        }

        /// <summary>A method's visible sequence points, in IL order; the hidden ones mark compiler plumbing.</summary>
        private List<SequencePoint> SequencePoints(MethodDefinitionHandle handle)
        {
            if (pdb is null)
                return [];
            var debug = pdb.GetMethodDebugInformation(handle.ToDebugInformationHandle());
            return debug.SequencePointsBlob.IsNil ? [] : [.. debug.GetSequencePoints().Where(static p => !p.IsHidden)];
        }

        /// <summary>
        /// The place of the instruction at <paramref name="offset"/>: the last visible sequence point at or before it,
        /// so that plumbing behind a hidden one is the statement's before it, else the first visible one after it.
        /// </summary>
        private SourcePlace PlaceOf(MethodDefinitionHandle handle, List<SequencePoint> visible, int offset)
        {
            var point = visible.LastOrDefault(p => p.Offset <= offset);
            if (point.Document.IsNil)
                point = visible.FirstOrDefault(p => p.Offset > offset);
            if (point.Document.IsNil)
            {
                var method = metadata.GetMethodDefinition(handle);
                return new SourcePlace("<no source line: " + SignatureNames.Qualified(metadata, method.GetDeclaringType()) + "::" + metadata.GetString(method.Name) + ">", 0, 0);
            }

            var document = pdb!.GetString(pdb.GetDocument(point.Document).Name);
            return new SourcePlace(RepositoryPath(document), point.StartLine, point.EndLine);
        }

        /// <summary>
        /// A PDB document's path relative to the repository root, '/'-separated: a local build writes the full path, a
        /// CI build (ContinuousIntegrationBuild) maps the root to <c>/_/</c>; a witness is compiled under its
        /// repository-relative path already.
        /// </summary>
        private static string RepositoryPath(string document)
        {
            var path = document.Replace('\\', '/');
            var root = SharedTree.Root.Replace('\\', '/').TrimEnd('/') + "/";
            if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return path[root.Length..];
            return path.StartsWith("/_/", StringComparison.Ordinal) ? path[3..] : path;
        }

        private static List<Instruction> Decode(BlobReader il)
        {
            var decoded = new List<Instruction>();
            while (il.RemainingBytes > 0)
            {
                var offset = il.Offset;
                int code = il.ReadByte();
                if (code == 0xFE)
                    code = 0xFE00 | il.ReadByte();
                var opCode = (ILOpCode)code;
                var operand = 0;
                switch (OperandSize(opCode))
                {
                    case 1:
                        operand = il.ReadSByte();
                        break;
                    case 2:
                        operand = il.ReadUInt16();
                        break;
                    case 4:
                        operand = il.ReadInt32();
                        break;
                    case 8:
                        il.ReadInt64();
                        break;
                    case -1:
                        var targets = il.ReadInt32();
                        il.Offset += 4 * targets;
                        break;
                }

                decoded.Add(new Instruction(offset, opCode, operand));
            }

            return decoded;
        }

        /// <summary>The size of an opcode's inline operand in bytes; -1 for <c>switch</c>, whose size its first four bytes give.</summary>
        private static int OperandSize(ILOpCode code)
        {
            if (code == ILOpCode.Switch)
                return -1;
            if (code.IsBranch())
                return code.GetBranchOperandSize();
            if (CarriesToken(code))
                return 4;
            return code switch
            {
                ILOpCode.Ldarg_s or ILOpCode.Ldarga_s or ILOpCode.Starg_s or ILOpCode.Ldloc_s or ILOpCode.Ldloca_s or ILOpCode.Stloc_s
                    or ILOpCode.Ldc_i4_s or ILOpCode.Unaligned => 1,
                ILOpCode.Ldarg or ILOpCode.Ldarga or ILOpCode.Starg or ILOpCode.Ldloc or ILOpCode.Ldloca or ILOpCode.Stloc => 2,
                ILOpCode.Ldc_i4 or ILOpCode.Ldc_r4 or ILOpCode.Ldstr or ILOpCode.Calli => 4,
                ILOpCode.Ldc_i8 or ILOpCode.Ldc_r8 => 8,

                // no. (0xFE19) takes a one-byte mask; System.Reflection.Metadata names no member for it.
                _ => (int)code == 0xFE19 ? 1 : 0,
            };
        }

        /// <summary>Whether an opcode's operand is a metadata token of a type, a method or a field.</summary>
        private static bool CarriesToken(ILOpCode code) => code is ILOpCode.Call or ILOpCode.Callvirt or ILOpCode.Newobj or ILOpCode.Jmp
            or ILOpCode.Ldftn or ILOpCode.Ldvirtftn or ILOpCode.Ldfld or ILOpCode.Ldflda or ILOpCode.Stfld or ILOpCode.Ldsfld
            or ILOpCode.Ldsflda or ILOpCode.Stsfld or ILOpCode.Ldtoken or ILOpCode.Box or ILOpCode.Unbox or ILOpCode.Unbox_any
            or ILOpCode.Castclass or ILOpCode.Isinst or ILOpCode.Newarr or ILOpCode.Ldelema or ILOpCode.Ldelem or ILOpCode.Stelem
            or ILOpCode.Ldobj or ILOpCode.Stobj or ILOpCode.Cpobj or ILOpCode.Initobj or ILOpCode.Sizeof or ILOpCode.Mkrefany
            or ILOpCode.Refanyval or ILOpCode.Constrained;
    }
}

/// <summary>Renders signature types as names: <c>System.Int32</c>, <c>System.String[]</c>, <c>List`1&lt;System.String&gt;</c>, <c>!0</c>, <c>!!0</c>.</summary>
internal sealed class SignatureNames(MetadataReader metadata) : ISignatureTypeProvider<string, object?>
{
    internal static string Qualified(MetadataReader metadata, TypeReferenceHandle handle)
    {
        var reference = metadata.GetTypeReference(handle);
        var name = metadata.GetString(reference.Name);
        if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
            return Qualified(metadata, (TypeReferenceHandle)reference.ResolutionScope) + "+" + name;
        var ns = metadata.GetString(reference.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    internal static string Qualified(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        var definition = metadata.GetTypeDefinition(handle);
        var name = metadata.GetString(definition.Name);
        var declaring = definition.GetDeclaringType();
        if (!declaring.IsNil)
            return Qualified(metadata, declaring) + "+" + name;
        var ns = metadata.GetString(definition.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[" + new string(',', shape.Rank - 1) + "]";

    public string GetByReferenceType(string elementType) => elementType + "&";

    public string GetFunctionPointerType(MethodSignature<string> signature) => "method " + signature.ReturnType + "*(" + string.Join(",", signature.ParameterTypes) + ")";

    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) => genericType + "<" + string.Join(",", typeArguments) + ">";

    public string GetGenericMethodParameter(object? genericContext, int index) => "!!" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public string GetGenericTypeParameter(object? genericContext, int index) => "!" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>A required modifier is part of the signature a caller binds to (an init accessor's <c>IsExternalInit</c>); an optional one is not.</summary>
    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) =>
        isRequired ? unmodifiedType + " modreq(" + modifier + ")" : unmodifiedType;

    public string GetPinnedType(string elementType) => elementType;

    public string GetPointerType(string elementType) => elementType + "*";

    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
    {
        PrimitiveTypeCode.Boolean => "System.Boolean",
        PrimitiveTypeCode.Byte => "System.Byte",
        PrimitiveTypeCode.Char => "System.Char",
        PrimitiveTypeCode.Double => "System.Double",
        PrimitiveTypeCode.Int16 => "System.Int16",
        PrimitiveTypeCode.Int32 => "System.Int32",
        PrimitiveTypeCode.Int64 => "System.Int64",
        PrimitiveTypeCode.IntPtr => "System.IntPtr",
        PrimitiveTypeCode.Object => "System.Object",
        PrimitiveTypeCode.SByte => "System.SByte",
        PrimitiveTypeCode.Single => "System.Single",
        PrimitiveTypeCode.String => "System.String",
        PrimitiveTypeCode.TypedReference => "System.TypedReference",
        PrimitiveTypeCode.UInt16 => "System.UInt16",
        PrimitiveTypeCode.UInt32 => "System.UInt32",
        PrimitiveTypeCode.UInt64 => "System.UInt64",
        PrimitiveTypeCode.UIntPtr => "System.UIntPtr",
        PrimitiveTypeCode.Void => "System.Void",
        _ => typeCode.ToString(),
    };

    public string GetSZArrayType(string elementType) => elementType + "[]";

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => Qualified(reader, handle);

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => Qualified(reader, handle);

    public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    internal MetadataReader Metadata { get; } = metadata;
}
