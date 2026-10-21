using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class RuntimeGcAudit
{
    internal static void Validate(PEReader pe,MetadataReader metadata)
    {
        var collectTokens=new HashSet<int>();
        foreach(var handle in metadata.MemberReferences)
        {
            var member=metadata.GetMemberReference(handle);
            if(member.Parent.Kind!=HandleKind.TypeReference)continue;
            var parent=metadata.GetTypeReference((TypeReferenceHandle)member.Parent);
            if(metadata.GetString(parent.Namespace)!="System" || metadata.GetString(parent.Name)!="GC")continue;
            if(metadata.GetString(member.Name)=="WaitForPendingFinalizers")throw new Exception("Runtime navigation cannot wait on finalizers.");
            if(metadata.GetString(member.Name)=="Collect")collectTokens.Add(MetadataTokens.GetToken(handle));
        }
        var opcodes=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static)
            .Where(field=>field.FieldType==typeof(OpCode)).Select(field=>(OpCode)field.GetValue(null)!).ToDictionary(op=>op.Value);
        int sites=0;
        foreach(var typeHandle in metadata.TypeDefinitions)
        {
            var type=metadata.GetTypeDefinition(typeHandle);
            foreach(var methodHandle in type.GetMethods())
            {
                var method=metadata.GetMethodDefinition(methodHandle);if(method.RelativeVirtualAddress==0)continue;
                var il=pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()??Array.Empty<byte>();
                for(int position=0;position<il.Length;)
                {
                    short value=il[position++];if(value==0xfe)value=unchecked((short)(0xfe00|il[position++]));
                    var op=opcodes[value];
                    if(op.OperandType==OperandType.InlineMethod && collectTokens.Contains(BitConverter.ToInt32(il,position)))
                    {
                        if(metadata.GetString(type.Name)!="NavigationCollection" || metadata.GetString(method.Name)!="Release")
                            throw new Exception("Induced GC escaped the single gated navigation primitive.");
                        sites++;
                    }
                    position+=op.OperandType switch {
                        OperandType.InlineNone=>0,
                        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,
                        OperandType.InlineVar=>2,
                        OperandType.InlineI8 or OperandType.InlineR=>8,
                        OperandType.InlineSwitch=>4+4*BitConverter.ToInt32(il,position),
                        _=>4
                    };
                }
            }
        }
        if(sites!=1)throw new Exception("The launcher must have exactly one gated induced-collection site; found "+sites);
        Console.WriteLine("PASS: actual launcher IL contains one induced GC site, exclusively in NavigationCollection.Release; no finalizer waits.");
    }
}
