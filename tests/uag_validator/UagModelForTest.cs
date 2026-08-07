using System.Collections.Generic;

namespace QFoldIT.Toolbelt.Uag
{
    public class UagTransform
    {
        public float[] Position { get; set; } = { 0, 0, 0 };
        public float[] RotationEulerDeg { get; set; } = { 0, 0, 0 };
        public float[] Scale { get; set; } = { 1, 1, 1 };
    }

    public class UagNode
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public UagTransform Transform { get; set; } = new UagTransform();
        public object Properties { get; set; }
        public string ParentId { get; set; }
    }

    public class UagConnection
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string FromNode { get; set; }
        public string ToNode { get; set; }
        public object Properties { get; set; }
    }

    public class UagConstraint
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public List<string> TargetNodes { get; set; } = new List<string>();
        public object Properties { get; set; }
    }

    public class UagInteraction
    {
        public string Id { get; set; }
        public string Trigger { get; set; }
        public string TargetNode { get; set; }
        public string Action { get; set; }
    }

    public class UagGraph
    {
        public string UagVersion { get; set; }
        public List<UagNode> Nodes { get; set; } = new List<UagNode>();
        public List<UagConnection> Connections { get; set; } = new List<UagConnection>();
        public List<UagConstraint> Constraints { get; set; } = new List<UagConstraint>();
        public List<UagInteraction> Interactions { get; set; } = new List<UagInteraction>();
    }
}
