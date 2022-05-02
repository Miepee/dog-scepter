﻿using System.Collections.Generic;
using static DogScepterLib.Core.Models.GMCode.Bytecode;
using static DogScepterLib.Core.Models.GMCode.Bytecode.Instruction;

namespace DogScepterLib.Project.GML.Compiler;

public static partial class Bytecode
{
    /// <summary>
    /// Represents an instruction to be patched with a function reference after instructions have all been written.
    /// </summary>
    public record FunctionPatch(Instruction Target, TokenFunction Token, FunctionReference Reference);

    /// <summary>
    /// Represents an instruction to be patched with a variable reference after instructions have all been written.
    /// </summary>
    public record VariablePatch(Instruction Target, TokenVariable Token);

    /// <summary>
    /// Represents an instruction to be patched with a string ID after instructions have all been written.
    /// </summary>
    public record StringPatch(Instruction Target, string Content);

    /// <summary>
    /// Represents branch instructions that must be later patched to point to correct addresses.
    /// </summary>
    public interface IJumpPatch
    {
        /// <summary>
        /// Adds a new instruction to be patched by this specific instance.
        /// </summary>
        public void Add(Instruction instr);

        /// <summary>
        /// Patches all of the targeted instructions to point to desired address.
        /// </summary>
        public void Finish(CodeContext ctx);
    }

    /// <summary>
    /// Jump patch where every instruction included will be jumping forwards to the address
    /// reached when calling Finish().
    /// </summary>
    public class JumpForwardPatch : IJumpPatch
    {
        private readonly List<Instruction> _patches;

        public JumpForwardPatch()
        {
            _patches = new();
        }

        public JumpForwardPatch(Instruction firstToPatch)
        {
            _patches = new() { firstToPatch };
        }

        public void Add(Instruction instr)
        {
            _patches.Add(instr);
        }

        public void Finish(CodeContext ctx)
        {
            foreach (var instr in _patches)
                instr.JumpOffset = (ctx.BytecodeLength - instr.Address) / 4;
        }
    }

    /// <summary>
    /// Jump patch where every instruction included will be jumping backwards to the address
    /// at the time of the instance's instantiation.
    /// </summary>
    public class JumpBackwardPatch : IJumpPatch
    {
        private readonly List<Instruction> _patches;
        private readonly int _startAddress;

        public JumpBackwardPatch(CodeContext ctx)
        {
            _startAddress = ctx.BytecodeLength;
            _patches = new();
        }

        public void Add(Instruction instr)
        {
            _patches.Add(instr);
        }

        public void Finish(CodeContext ctx)
        {
            foreach (var instr in _patches)
                instr.JumpOffset = (_startAddress - instr.Address) / 4;
        }
    }

    /// <summary>
    /// Helper record used for generating code for switch statements.
    /// </summary>
    public record SwitchCase(JumpForwardPatch Jump, int ChildIndex);

    /// <summary>
    /// Represents a loop or context where break/continue need special behavior.
    /// </summary>
    public class Context
    {
        public enum ContextKind
        {
            BasicLoop,
            With,
            Switch,
            Repeat,
            // todo: try/catch/finally?
        }

        public ContextKind Kind { get; init; }
        public bool BreakUsed { get; private set; }
        public bool ContinueUsed { get; private set; }
        public DataType DuplicatedType { get; init; }

        // Internal references to break/continue patches
        private readonly IJumpPatch _breakJump;
        private readonly IJumpPatch _continueJump;

        /// <summary>
        /// Instantiates a new context of any kind, given its break/continue jump patches.
        /// </summary>
        public Context(ContextKind kind, IJumpPatch breakJump, IJumpPatch continueJump)
        {
            Kind = kind;
            _breakJump = breakJump;
            _continueJump = continueJump;
        }

        /// <summary>
        /// Instantiates a new switch statement context, given the break/continue jump patches, 
        /// and the type of the data being duplicated by the switch statement.
        /// </summary>
        public Context(IJumpPatch breakJump, IJumpPatch continueJump, DataType duplicatedType)
        {
            Kind = ContextKind.Switch;
            _breakJump = breakJump;
            _continueJump = continueJump;
            DuplicatedType = duplicatedType;
        }

        /// <summary>
        /// Returns the jump patch that should be used for this context, when "break" is used.
        /// Also marks this context to have used "break".
        /// </summary>
        public IJumpPatch UseBreakJump()
        {
            BreakUsed = true;
            return _breakJump;
        }

        /// <summary>
        /// Returns the jump patch that should be used for this context, when "continue" is used.
        /// Also marks this context to have used "continue".
        /// </summary>
        public IJumpPatch UseContinueJump()
        {
            ContinueUsed = true;
            return _continueJump;
        }
    }

    /// <summary>
    /// Emits a basic instruction with no properties.
    /// </summary>
    private static Instruction Emit(CodeContext ctx, Opcode opcode)
    {
        Instruction res = new(ctx.BytecodeLength)
        {
            Kind = opcode
        };
        ctx.Instructions.Add(res);
        ctx.BytecodeLength += 4;
        return res;
    }

    /// <summary>
    /// Emits a basic single-type instruction.
    /// </summary>
    private static Instruction Emit(CodeContext ctx, Opcode opcode, DataType type)
    {
        Instruction res = new(ctx.BytecodeLength)
        {
            Kind = opcode,
            Type1 = type
        };
        ctx.Instructions.Add(res);
        ctx.BytecodeLength += res.GetLength() * 4;
        return res;
    }

    /// <summary>
    /// Emits a basic double-type instruction.
    /// </summary>
    private static Instruction Emit(CodeContext ctx, Opcode opcode, DataType type, DataType type2)
    {
        Instruction res = new(ctx.BytecodeLength)
        {
            Kind = opcode,
            Type1 = type,
            Type2 = type2
        };
        ctx.Instructions.Add(res);
        ctx.BytecodeLength += res.GetLength() * 4;
        return res;
    }

    /// <summary>
    /// Emits a basic comparison instruction.
    /// </summary>
    private static Instruction EmitCompare(CodeContext ctx, ComparisonType type, DataType type1, DataType type2)
    {
        Instruction res = new(ctx.BytecodeLength)
        {
            Kind = Opcode.Cmp,
            ComparisonKind = type,
            Type1 = type1,
            Type2 = type2
        };
        ctx.Instructions.Add(res);
        ctx.BytecodeLength += 4;
        return res;
    }

    /// <summary>
    /// Emits a special "dup swap" instruction, with its parameters and type.
    /// </summary>
    private static Instruction EmitDupSwap(CodeContext ctx, DataType type, byte param1, byte param2)
    {
        Instruction res = new(ctx.BytecodeLength)
        {
            Kind = Opcode.Dup,
            Type1 = type,
            Extra = param1,
            ComparisonKind = (ComparisonType)(param2 | 0x80)
        };
        ctx.Instructions.Add(res);
        ctx.BytecodeLength += 4;
        return res;
    }

    /// <summary>
    /// Emits an instruction that references a variable, with data type.
    /// </summary>
    private static Instruction EmitVariable(CodeContext ctx, Opcode opcode, DataType type, TokenVariable variable, DataType type2 = DataType.Double)
    {
        Instruction res = new(ctx.BytecodeLength)
        {
            Kind = opcode,
            Type1 = type,
            Type2 = type2
        };

        ctx.VariablePatches.Add(new(res, variable));

        ctx.Instructions.Add(res);
        ctx.BytecodeLength += 8;
        return res;
    }

    /// <summary>
    /// Emits an instruction that references a function, with data type, argument count, and optional reference.
    /// </summary>
    private static Instruction EmitCall(CodeContext ctx, Opcode opcode, DataType type, TokenFunction function, int argCount, Token token = null, FunctionReference reference = null)
    {
        Instruction res = new(ctx.BytecodeLength)
        {
            Kind = opcode,
            Type1 = type,
            Value = (short)argCount
        };

        if (function.Builtin != null)
        {
            if (function.Builtin.ArgumentCount != -1 && argCount != function.Builtin.ArgumentCount)
                ctx.Error($"Built-in function \"{function.Builtin.Name}\" expects {function.Builtin.ArgumentCount} arguments; {argCount} supplied.", token?.Index ?? -1);
        }

        ctx.FunctionPatches.Add(new(res, function, reference));

        ctx.Instructions.Add(res);
        ctx.BytecodeLength += 8;
        return res;
    }

    /// <summary>
    /// Emits an instruction that references a string, with data type.
    /// </summary>
    private static Instruction EmitString(CodeContext ctx, Opcode opcode, DataType type, string str)
    {
        Instruction res = new(ctx.BytecodeLength)
        {
            Kind = opcode,
            Type1 = type
        };

        ctx.StringPatches.Add(new(res, str));

        ctx.Instructions.Add(res);
        ctx.BytecodeLength += 8;
        return res;
    }

    /// <summary>
    /// If the top of the type stack isn't the supplied type, emits an instruction to convert to that type.
    /// This removes the type from the top of the type stack in the process.
    /// </summary>
    /// <returns>True if a conversion instruction was emitted, false otherwise</returns>
    private static bool ConvertTo(CodeContext ctx, DataType to)
    {
        DataType from = ctx.TypeStack.Pop();
        if (from != to)
        {
            Emit(ctx, Opcode.Conv, from, to);
            return true;
        }
        return false;
    }
}
