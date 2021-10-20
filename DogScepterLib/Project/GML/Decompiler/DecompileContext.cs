﻿using DogScepterLib.Core;
using DogScepterLib.Core.Chunks;
using DogScepterLib.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogScepterLib.Project.GML.Decompiler
{
    public class DecompileSettings
    {
        public string Indent { get; set; } = "    ";
    }

    public class DecompileContext
    {
        public ProjectFile Project { get; set; }
        public GMData Data { get; set; }
        public DecompileSettings Settings { get; set; }
        public IList<GMString> Strings { get; set; }
        public BlockList Blocks { get; set; }
        public List<Loop> LoopNodes { get; set; }
        public List<ShortCircuit> ShortCircuitNodes { get; set; }
        public List<IfStatement> IfStatementNodes { get; set; }
        public List<SwitchStatement> SwitchStatementNodes { get; set; }
        public List<Node> PredecessorsToClear { get; set; }
        public Node BaseNode { get; set; }
        public ASTBlock BaseASTBlock { get; set; }
        public HashSet<string> RemainingLocals { get; set; }
        public HashSet<string> AllLocals { get; set; }

        private int indentationLevel = 0;
        public int IndentationLevel
        {
            get => indentationLevel;
            set { indentationLevel = value; Indentation = new StringBuilder().Insert(0, Settings.Indent, indentationLevel).ToString(); }
        }
        public string Indentation = "";

        public DecompileContext(ProjectFile pf, DecompileSettings settings = null)
        {
            Project = pf;
            Data = pf.DataHandle;
            Strings = Data.GetChunk<GMChunkSTRG>().List;
            Settings = settings ?? new DecompileSettings();
        }

        public string DecompileWholeEntry(GMCode codeEntry)
        {
            // Find all fragments
            List<Fragment> fragments = Fragments.FindAndProcess(codeEntry);

            StringBuilder sb = new StringBuilder();

            foreach (var fragment in fragments)
            {
                sb.Append("// New fragment");
                sb.AppendLine();
                sb.Append(DecompileSegment(codeEntry, fragment.Blocks));
                sb.AppendLine();
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public string DecompileSegment(GMCode codeEntry, BlockList existingList = null)
        {
            Blocks = existingList ?? Block.GetBlocks(codeEntry);
            Blocks.FindUnreachables();

            // Add node to beginning
            BaseNode = new Block(-1, -1);
            BaseNode.Branches.Add(Blocks.List[0]);
            Blocks.List[0].Predecessors.Add(BaseNode);

            // Find loops first to eliminate backwards branches (mostly), and differentiate `bt` instructions
            LoopNodes = Loops.Find(Blocks);
            ShortCircuitNodes = ShortCircuits.Find(Blocks);

            // Now actually insert the nodes for them
            Loops.InsertNodes(this);
            ShortCircuits.InsertNodes(this);

            // Find all of the switch/if statements, then insert them in nested order together
            SwitchStatementNodes = SwitchStatements.Find(this);
            IfStatementNodes = IfStatements.Find(this);
            BranchStatements.InsertNodes(this);

            // Now build the AST, clean it, and write it to a string
            RemainingLocals = new HashSet<string>();
            BaseASTBlock = ASTBuilder.FromContext(this);
            AllLocals = new HashSet<string>(RemainingLocals);
            BaseASTBlock.Clean(this);
            return ASTNode.WriteFromContext(this);
        }
    }
}
